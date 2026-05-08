using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class StorageCapacityService(
    ILogger<StorageCapacityService> logger,
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory clientFactory,
    TokenRefreshService tokenRefreshService)
    : Service(httpContextAccessor, clientFactory, tokenRefreshService), IStorageCapacityService
{
    private readonly ILogger<StorageCapacityService> _logger = logger;

    private const string _recordsKey = "records";
    private const string _existsKey = "exists";
    private const string _messageKey = "message";
    private const string _applicationJson = "application/json";

    public async Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes()
    {
        return await GetListResponseAsync<StorageTypeResponse>(
            ApiurlHelper.FetchStorageTypesAsyncAPI,
            useRecordsNode: true);
    }

    public async Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId)
    {
        return await GetListResponseAsync<StoreCapacityResponse>(
            string.Format(ApiurlHelper.FetchStoreCapacityAsyncAPI, farmId));
    }

    public async Task<(List<CommonResponse>, Error)> FetchMaterialStates()
    {
        return await GetListResponseAsync<CommonResponse>(
            ApiurlHelper.FetchMaterialStatesListAsyncAPI,
            useRecordsNode: true);
    }

    public async Task<(CommonResponse, Error)> FetchMaterialStateById(int id)
    {
        return await GetSingleResponseAsync<CommonResponse>(
            string.Format(ApiurlHelper.FetchMaterialStatesListByIDAsyncAPI, id));
    }

    public async Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id)
    {
        return await GetSingleResponseAsync<StorageTypeResponse>(
            string.Format(ApiurlHelper.FetchStorageTypeByIdAsyncAPI, id));
    }

    public async Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType()
    {
        return await GetListResponseAsync<SolidManureTypeResponse>(
            ApiurlHelper.FetchSolidManureTypeAsyncAPI,
            useRecordsNode: true);
    }

    public async Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id)
    {
        return await GetSingleResponseAsync<SolidManureTypeResponse>(
            string.Format(ApiurlHelper.FetchSolidManureTypeByIdAsyncAPI, id));
    }

    public async Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles()
    {
        return await GetListResponseAsync<BankSlopeAnglesResponse>(
            ApiurlHelper.FetchBankSlopeAnglesAsyncAPI,
            useRecordsNode: true);
    }

    public async Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id)
    {
        return await GetSingleResponseAsync<BankSlopeAnglesResponse>(
            string.Format(ApiurlHelper.FetchBankSlopeAngleByIdAsyncAPI, id));
    }

    public async Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        string jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync(
            client => client.PostAsync(
                ApiurlHelper.AddStoreCapacityAsyncAPI,
                CreateJsonContent(jsonData)),
            MapObject<StoreCapacity>);

        return (data ?? new StoreCapacity(), error);
    }

    public async Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(
                string.Format(
                    ApiurlHelper.IsStoreNameExistByFarmIdYearAndNameAsyncAPI,
                    farmId,
                    storeName,
                    ID ?? 0)),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj[_existsKey]?.Value<bool>() ?? false;
                }

                return false;
            });

        return (data, error);
    }

    public async Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id)
    {
        return await GetSingleResponseAsync<StoreCapacity>(
            string.Format(ApiurlHelper.FetchStoreCapacityByIdAsyncAPI, id));
    }

    public async Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData)
    {
        var (data, error) = await SendRequestAsync(
            client => client.PostAsync(
                ApiurlHelper.CopyStoreManureCapacityAsyncAPI,
                CreateJsonContent(copyStorageManureCapacityData)),
            wrapper => MapList<StoreCapacityResponse>(wrapper));

        return (data ?? new List<StoreCapacityResponse>(), error);
    }

    public async Task<(string, Error)> RemoveStorageCapacity(int id)
    {
        var (data, error) = await SendRequestAsync(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteStorageCapacityByIdAPI, id)),
            MapMessage);

        return (data ?? string.Empty, error);
    }

    public async Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        string jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync(
            client => client.PutAsync(
                ApiurlHelper.UpdateStoreCapacityAsyncAPI,
                CreateJsonContent(jsonData)),
            MapObject<StoreCapacity>);

        return (data ?? new StoreCapacity(), error);
    }

    private async Task<(List<T>, Error)> GetListResponseAsync<T>(
        string url,
        bool useRecordsNode = false)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(url),
            wrapper => MapList<T>(wrapper, useRecordsNode));

        return (data ?? new List<T>(), error);
    }

    private async Task<(T, Error)> GetSingleResponseAsync<T>(string url)
        where T : new()
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(url),
            wrapper => MapSingle<T>(wrapper));

        return (data ?? new T(), error);
    }

    private static List<T> MapList<T>(
        ResponseWrapper? wrapper,
        bool useRecordsNode = false)
    {
        JToken? token = useRecordsNode
            ? wrapper?.Data?[_recordsKey]
            : wrapper?.Data;

        return token?.ToObject<List<T>>() ?? new List<T>();
    }

    private static T MapSingle<T>(ResponseWrapper? wrapper)
        where T : new()
    {
        if (wrapper?.Data?[_recordsKey] is JToken token)
        {
            return token.ToObject<T>() ?? new T();
        }

        return new T();
    }

    private static T MapObject<T>(ResponseWrapper? wrapper)
        where T : new()
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj.ToObject<T>() ?? new T();
        }

        return new T();
    }

    private static string MapMessage(ResponseWrapper? wrapper)
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj[_messageKey]?.Value<string>() ?? string.Empty;
        }

        return string.Empty;
    }

    private static StringContent CreateJsonContent(string jsonData)
    {
        return new StringContent(jsonData, Encoding.UTF8, _applicationJson);
    }

    private async Task<(T?, Error)> SendRequestAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> httpCall,
        Func<ResponseWrapper?, T?> mapData)
    {
        Error error = new();
        T? resultData = default;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            HttpResponseMessage response = await httpCall(httpClient);

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper =
                JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                resultData = mapData(responseWrapper);
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }

        return (resultData, error);
    }
}
