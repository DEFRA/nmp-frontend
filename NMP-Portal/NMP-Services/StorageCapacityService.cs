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
        List<StorageTypeResponse> storageTypeList = new List<StorageTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchStorageTypesAsyncAPI);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var storageTypes = responseWrapper?.Data?.records.ToObject<List<StorageTypeResponse>>();
                    storageTypeList.AddRange(storageTypes);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storageTypeList, error);
    }
    public async Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId)
    {
        Error error = new Error();
        List<StoreCapacityResponse> storeCapacityList = new List<StoreCapacityResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = string.Empty;

            url = string.Format(ApiurlHelper.FetchStoreCapacityAsyncAPI, farmId);
            var response = await httpClient.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    storeCapacityList = data.ToObject<List<StoreCapacityResponse>>() ?? new List<StoreCapacityResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storeCapacityList, error);
    }
    public async Task<(List<CommonResponse>, Error)> FetchMaterialStates()
    {
        List<CommonResponse> materialStatesList = new List<CommonResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMaterialStatesListAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    var materialStates = records.ToObject<List<CommonResponse>>() ?? new List<CommonResponse>();
                    materialStatesList.AddRange(materialStates);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (materialStatesList, error);
    }
    public async Task<(CommonResponse, Error)> FetchMaterialStateById(int id)
    {
        CommonResponse materialState = new CommonResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMaterialStatesListByIDAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    materialState = records.ToObject<CommonResponse>() ?? new CommonResponse();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (materialState, error);
    }
    public async Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id)
    {
        StorageTypeResponse storageType = new StorageTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchStorageTypeByIdAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    storageType = records.ToObject<StorageTypeResponse>() ?? new StorageTypeResponse();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storageType, error);
    }
    public async Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType()
    {
        List<SolidManureTypeResponse> solidManureTypeList = new List<SolidManureTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSolidManureTypeAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    solidManureTypeList = records.ToObject<List<SolidManureTypeResponse>>() ?? new List<SolidManureTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (solidManureTypeList, error);
    }
    public async Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id)
    {
        SolidManureTypeResponse solidManureType = new SolidManureTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSolidManureTypeByIdAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    solidManureType = records.ToObject<SolidManureTypeResponse>() ?? new SolidManureTypeResponse();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (solidManureType, error);
    }

    public async Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles()
    {
        List<BankSlopeAnglesResponse> bankSlopeAngleList = new List<BankSlopeAnglesResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchBankSlopeAnglesAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    bankSlopeAngleList = records.ToObject<List<BankSlopeAnglesResponse>>() ?? new List<BankSlopeAnglesResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (bankSlopeAngleList, error);
    }

    public async Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id)
    {
        BankSlopeAnglesResponse bankSlopeAngle = new BankSlopeAnglesResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchBankSlopeAngleByIdAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    bankSlopeAngle = records.ToObject<BankSlopeAnglesResponse>() ?? new BankSlopeAnglesResponse();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (bankSlopeAngle, error);
    }

    public async Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        string jsonData = JsonConvert.SerializeObject(storeCapacityData);
        StoreCapacity storeCapacity = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.AddStoreCapacityAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject storeCapacityJObject)
                {
                    storeCapacity = storeCapacityJObject.ToObject<StoreCapacity>() ?? new StoreCapacity();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error)?? new Error();
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storeCapacity, error);
    }

    public async Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID)
    {
        bool isExist = false;
        Error? error = null;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.IsStoreNameExistByFarmIdYearAndNameAsyncAPI, farmId, storeName, ID ?? 0));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    isExist = data["exists"]?.Value<bool>() ?? false;
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (isExist, error);
    }

    public async Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id)
    {
        StoreCapacity? storeCapacity = new StoreCapacity();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchStoreCapacityByIdAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    storeCapacity = responseWrapper?.Data?.records.ToObject<StoreCapacity>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storeCapacity, error);
    }

    public async Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData)
    {
        List<StoreCapacityResponse> storeCapacities = new List<StoreCapacityResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.CopyStoreManureCapacityAsyncAPI, new StringContent(copyStorageManureCapacityData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JToken data)
                {
                    storeCapacities = data.ToObject<List<StoreCapacityResponse>>() ?? new List<StoreCapacityResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storeCapacities, error);
    }

    public async Task<(string, Error)> RemoveStorageCapacity(int id)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteStorageCapacityByIdAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject data)
                {
                    message = data["message"]?.Value<string>() ?? string.Empty;
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }

        return (message, error);
    }

    public async Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        string jsonData = JsonConvert.SerializeObject(storeCapacityData);
        StoreCapacity storeCapacity = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(ApiurlHelper.UpdateStoreCapacityAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject storeCapacityJObject)
                {
                    storeCapacity = storeCapacityJObject.ToObject<StoreCapacity>() ?? new StoreCapacity();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (storeCapacity, error);
    }
}
