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

    public async Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes()
    {
        var (data, error) = await SendRequestAsync<List<StorageTypeResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchStorageTypesAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<List<StorageTypeResponse>>()
                           ?? new List<StorageTypeResponse>();
                }

                return new List<StorageTypeResponse>();
            });

        return (data ?? new List<StorageTypeResponse>(), error);
    }
    public async Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId)
    {
        var (data, error) = await SendRequestAsync<List<StoreCapacityResponse>>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchStoreCapacityAsyncAPI, farmId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                {
                    return token.ToObject<List<StoreCapacityResponse>>()
                           ?? new List<StoreCapacityResponse>();
                }

                return new List<StoreCapacityResponse>();
            });

        return (data ?? new List<StoreCapacityResponse>(), error);
    }
    public async Task<(List<CommonResponse>, Error)> FetchMaterialStates()
    {
        var (data, error) = await SendRequestAsync<List<CommonResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchMaterialStatesListAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<List<CommonResponse>>()
                           ?? new List<CommonResponse>();
                }

                return new List<CommonResponse>();
            });

        return (data ?? new List<CommonResponse>(), error);
    }
    public async Task<(CommonResponse, Error)> FetchMaterialStateById(int id)
    {
        var (data, error) = await SendRequestAsync<CommonResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchMaterialStatesListByIDAsyncAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<CommonResponse>()
                           ?? new CommonResponse();
                }

                return new CommonResponse();
            });

        return (data ?? new CommonResponse(), error);
    }
    public async Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id)
    {
        var (data, error) = await SendRequestAsync<StorageTypeResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchStorageTypeByIdAsyncAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<StorageTypeResponse>()
                           ?? new StorageTypeResponse();
                }

                return new StorageTypeResponse();
            });

        return (data ?? new StorageTypeResponse(), error);
    }
    public async Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType()
    {
        var (data, error) = await SendRequestAsync<List<SolidManureTypeResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchSolidManureTypeAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<List<SolidManureTypeResponse>>()
                           ?? new List<SolidManureTypeResponse>();
                }

                return new List<SolidManureTypeResponse>();
            });

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
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<SolidManureTypeResponse>()
                           ?? new SolidManureTypeResponse();
                }

                return new SolidManureTypeResponse();
            });

        return (data ?? new SolidManureTypeResponse(), error);
    }

    public async Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles()
    {
        var (data, error) = await SendRequestAsync<List<BankSlopeAnglesResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchBankSlopeAnglesAsyncAPI),
            wrapper =>
            {
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<List<BankSlopeAnglesResponse>>()
                           ?? new List<BankSlopeAnglesResponse>();
                }

                return new List<BankSlopeAnglesResponse>();
            });

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
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<BankSlopeAnglesResponse>()
                           ?? new BankSlopeAnglesResponse();
                }

                return new BankSlopeAnglesResponse();
            });

        return (data ?? new BankSlopeAnglesResponse(), error);
    }

    public async Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        var jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync<StoreCapacity>(
            client => client.PostAsync(
                ApiurlHelper.AddStoreCapacityAsyncAPI,
                new StringContent(jsonData, Encoding.UTF8, "application/json")
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj.ToObject<StoreCapacity>() ?? new StoreCapacity();
                }

                return new StoreCapacity();
            });

        return (data ?? new StoreCapacity(), error);
    }

    public async Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID)
    {
        var (data, error) = await SendRequestAsync<bool>(
            client => client.GetAsync(
                string.Format(
                    ApiurlHelper.IsStoreNameExistByFarmIdYearAndNameAsyncAPI,
                    farmId,
                    storeName,
                    ID ?? 0
                )
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj["exists"]?.Value<bool>() ?? false;
                }

                return false;
            });

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
                if (wrapper?.Data?["records"] is JToken token)
                {
                    return token.ToObject<StoreCapacity>()
                           ?? new StoreCapacity();
                }

                return new StoreCapacity();
            });

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
            });

        return (data ?? new List<StoreCapacityResponse>(), error);
    }

    public async Task<(string, Error)> RemoveStorageCapacity(int id)
    {
        var (data, error) = await SendRequestAsync<string>(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteStorageCapacityByIdAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj["message"]?.Value<string>() ?? string.Empty;
                }

                return string.Empty;
            });

        return (data ?? string.Empty, error);
    }

    public async Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        var jsonData = JsonConvert.SerializeObject(storeCapacityData);

        var (data, error) = await SendRequestAsync<StoreCapacity>(
            client => client.PutAsync(
                ApiurlHelper.UpdateStoreCapacityAsyncAPI,
                new StringContent(jsonData, Encoding.UTF8, "application/json")
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj.ToObject<StoreCapacity>() ?? new StoreCapacity();
                }

                return new StoreCapacity();
            });

        return (data ?? new StoreCapacity(), error);
    }

    private async Task<(T?, Error)> SendRequestAsync<T>(Func<HttpClient, Task<HttpResponseMessage>> httpCall,
   Func<ResponseWrapper?, T?> mapData)
    {
        Error error = new Error();
        T? resultData = default;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpCall(httpClient);

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
