using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using NMP.Commons.Helpers;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class StorageCapacityService(ILogger<StorageCapacityService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IStorageCapacityService
{
    private readonly ILogger<StorageCapacityService> _logger = logger;
    private const string _recordsKey = "records";

    public async Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes()
    {
        var (data, error) = await SendRequestAsync<List<StorageTypeResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchStorageTypesAsyncAPI),
            wrapper =>
            ExtractList<StorageTypeResponse>(wrapper), _logger);

        return (data ?? new List<StorageTypeResponse>(), error);
    }
    public async Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(string.Format(ApiurlHelper.FetchStoreCapacityAsyncAPI, farmId)),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                    return token.ToObject<List<StoreCapacityResponse>>() ?? new List<StoreCapacityResponse>();

                return new List<StoreCapacityResponse>();
            }, _logger);

        return (data ?? new List<StoreCapacityResponse>(), error);
    }
    public async Task<(List<CommonResponse>, Error)> FetchMaterialStates()
    {
        var (data, error) = await SendRequestAsync<List<CommonResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchMaterialStatesListAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<List<CommonResponse>>()
                           ?? new List<CommonResponse>();
                }

                return new List<CommonResponse>();
            }, _logger);

        return (data ?? new List<CommonResponse>(), error);
    }
    public async Task<(CommonResponse, Error)> FetchMaterialStateById(int id)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(string.Format(ApiurlHelper.FetchMaterialStatesListByIDAsyncAPI, id)),
            wrapper => ExtractSingle<CommonResponse>(wrapper)
        , _logger);

        return (data ?? new CommonResponse(), error);
    }

    public async Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(string.Format(ApiurlHelper.FetchStorageTypeByIdAsyncAPI, id)),
            wrapper => ExtractSingle<StorageTypeResponse>(wrapper)
        , _logger);

        return (data ?? new StorageTypeResponse(), error);
    }

    public async Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType()
    {
        var (data, error) = await SendRequestAsync<List<SolidManureTypeResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchSolidManureTypeAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<List<SolidManureTypeResponse>>()
                           ?? new List<SolidManureTypeResponse>();
                }

                return new List<SolidManureTypeResponse>();
            }, _logger);

        return (data ?? new List<SolidManureTypeResponse>(), error);
    }
    public async Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id)
    {
        var (data, error) = await SendRequestAsync<SolidManureTypeResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchSolidManureTypeByIdAsyncAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<SolidManureTypeResponse>()
                           ?? new SolidManureTypeResponse();
                }

                return new SolidManureTypeResponse();
            }, _logger);

        return (data ?? new SolidManureTypeResponse(), error);
    }

    public async Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles()
    {
        var (data, error) = await SendRequestAsync<List<BankSlopeAnglesResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchBankSlopeAnglesAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<List<BankSlopeAnglesResponse>>()
                           ?? new List<BankSlopeAnglesResponse>();
                }

                return new List<BankSlopeAnglesResponse>();
            }, _logger);

        return (data ?? new List<BankSlopeAnglesResponse>(), error);
    }

    public async Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id)
    {
        var (data, error) = await SendRequestAsync<BankSlopeAnglesResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchBankSlopeAngleByIdAsyncAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<BankSlopeAnglesResponse>()
                           ?? new BankSlopeAnglesResponse();
                }

                return new BankSlopeAnglesResponse();
            }, _logger);

        return (data ?? new BankSlopeAnglesResponse(), error);
    }

    public async Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        var jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync(
            client => client.PostAsync(
                ApiurlHelper.AddStoreCapacityAsyncAPI,
                new StringContent(jsonData, Encoding.UTF8, "application/json")
            ),
            wrapper => ExtractFromObject<StoreCapacity>(wrapper)
        , _logger);

        return (data ?? new StoreCapacity(), error);
    }

    public async Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID)
    {
        var (data, error) = await SendRequestAsync(
            client => client.GetAsync(
                string.Format(ApiurlHelper.IsStoreNameExistByFarmIdYearAndNameAsyncAPI, farmId, storeName, ID ?? 0)
            ),
            wrapper => ExtractBoolean(wrapper, "exists")
        , _logger);

        return (data, error);
    }


    public async Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id)
    {
        var (data, error) = await SendRequestAsync<StoreCapacity>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchStoreCapacityByIdAsyncAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?[_recordsKey] is JToken token)
                {
                    return token.ToObject<StoreCapacity>()
                           ?? new StoreCapacity();
                }

                return new StoreCapacity();
            }, _logger);

        return (data ?? new StoreCapacity(), error);
    }

    public async Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData)
    {
        var (data, error) = await SendRequestAsync<List<StoreCapacityResponse>>(
            client => client.PostAsync(
                ApiurlHelper.CopyStoreManureCapacityAsyncAPI,
                new StringContent(copyStorageManureCapacityData, Encoding.UTF8, "application/json")
            ),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                {
                    return token.ToObject<List<StoreCapacityResponse>>()
                           ?? new List<StoreCapacityResponse>();
                }

                return new List<StoreCapacityResponse>();
            }, _logger);

        return (data ?? new List<StoreCapacityResponse>(), error);
    }

    public async Task<(string, Error)> RemoveStorageCapacity(int id)
    {
        var (data, error) = await SendRequestAsync(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteStorageCapacityByIdAPI, id)
            ),
            wrapper => ExtractMessage(wrapper)
        , _logger);

        return (data ?? string.Empty, error);
    }

    public async Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        var jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync(
            client => client.PutAsync(
                ApiurlHelper.UpdateStoreCapacityAsyncAPI,
                new StringContent(jsonData, Encoding.UTF8, "application/json")
            ),
            wrapper => ExtractFromObject<StoreCapacity>(wrapper)
        , _logger);

        return (data ?? new StoreCapacity(), error);
    }
    private static List<T> ExtractList<T>(ResponseWrapper? wrapper, string key = _recordsKey)
    {
        if (wrapper?.Data?[key] is JToken token)
        {
            return token.ToObject<List<T>>() ?? new List<T>();
        }
        return new List<T>();
    }

    private static T ExtractSingle<T>(ResponseWrapper? wrapper, string key = _recordsKey) where T : new()
    {
        if (wrapper?.Data?[key] is JToken token)
        {
            return token.ToObject<T>() ?? new T();
        }
        return new T();
    }

    private static T ExtractFromObject<T>(ResponseWrapper? wrapper) where T : new()
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj.ToObject<T>() ?? new T();
        }
        return new T();
    }

    private static bool ExtractBoolean(ResponseWrapper? wrapper, string key)
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj[key]?.Value<bool>() ?? false;
        }
        return false;
    }

    private static string ExtractMessage(ResponseWrapper? wrapper)
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj["message"]?.Value<string>() ?? string.Empty;
        }
        return string.Empty;
    }
}
