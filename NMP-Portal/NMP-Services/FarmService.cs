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
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class FarmService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IFarmService
{
    private readonly ILogger<FarmService> _logger = logger;

    public async Task<(List<Farm>, Error?)> FetchFarmByOrgIdAsync(Guid orgId)
    {
        List<Farm> farmList = new List<Farm>();
        Error? error = null;
        try
        {
            string url = string.Format(ApiurlHelper.FetchFarmByOrgIdAPI, orgId);
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(url);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                List<Farm>? farms = responseWrapper?.Data?.Farms?.ToObject<List<Farm>>();
                if (farms != null && farms.Count > 0)
                {
                    farmList.AddRange(farms);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error ??= new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error ??= new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (farmList, error);
    }
    public async Task<(Farm?, Error?)> AddFarmAsync(FarmData farmData, Guid orgId)
    {
        Farm? farm = null;
        Error? error = null;
        try
        {
            if (farmData == null || farmData.Farm == null)
            {
                error ??= new Error();
                error.Message = Resource.MsgInvalidFarmData;
                return (farm, error);
            }

            // check if farm already exists or not
            bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID, orgId);
            if (IsFarmExist)
            {
                error ??= new Error();
                error.Message = string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode);
                return (farm, error);
            }

            string jsonData = JsonConvert.SerializeObject(farmData);
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddFarmAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                if (responseWrapper?.Data["Farm"] is JObject farmDataJObject)
                {
                    farm = farmDataJObject.ToObject<Farm>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error ??= new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error ??= new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (farm, error);
    }
    public async Task<(FarmResponse?, Error?)> FetchFarmByIdAsync(int farmId)
    {
        FarmResponse? farm = null;
        Error? error = null;
        try
        {
            string url = string.Format(ApiurlHelper.FetchFarmByIdAPI, farmId);
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(url);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                JObject? farmDataJObject = responseWrapper?.Data["Farm"] as JObject;
                if (farmDataJObject != null)
                {
                    farm = farmDataJObject.ToObject<FarmResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error ??= new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error ??= new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }

        return (farm, error);
    }

    public async Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id, Guid orgId)
    {
        bool isFarmExist = false;
        string url = string.Format(ApiurlHelper.IsFarmExist, HttpUtility.UrlEncode(farmName), postcode.Trim(), Id, orgId);
        HttpClient httpClient = await GetNMPAPIClient();
        var farmExist = await httpClient.GetAsync(url);

        string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
        if (responseWrapper?.Data["exists"] == true)
        {
            isFarmExist = true;
        }

        return isFarmExist;
    }

    public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
    {
        decimal rainfallAverage = 0;
        string url = string.Format(ApiurlHelper.FetchMannerRainfallAverageAsyncAPI, firstHalfPostcode);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            rainfallAverage = responseWrapper?.Data?.avarageAnnualRainfall != null ? responseWrapper.Data.avarageAnnualRainfall.value : 0;
        }

        return rainfallAverage;
    }

    public async Task<(Farm?, Error?)> UpdateFarmAsync(FarmData farmData, Guid orgId)
    {
        string jsonData = JsonConvert.SerializeObject(farmData);
        Farm? farm = null;
        Error? error = null;

        //check if Updated farm Name already exist or not in the Postcode...
        bool IsFarmNameWithInPostCodeAlreadyExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID, orgId);
        if (IsFarmNameWithInPostCodeAlreadyExist)
        {
            error = new Error
            {
                Message = string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode)
            };
            return (farm, error);
        }

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PutAsync(ApiurlHelper.UpdateFarmAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
        {
            JObject? farmDataJObject = responseWrapper?.Data["Farm"] as JObject;
            if (farmDataJObject != null)
            {
                farm = farmDataJObject.ToObject<Farm>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }


        return (farm, error);
    }

    public async Task<(string, Error)> DeleteFarmByIdAsync(int farmId)
    {
        Error? error = new Error();
        string message = string.Empty;
        string url = string.Format(ApiurlHelper.DeleteFarmByIdAPI, farmId);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.DeleteAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            message = responseWrapper.Data["message"].Value;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (message, error);
    }
    public async Task<(List<Country>, Error)> FetchCountryAsync()
    {
        List<Country> countryList = new List<Country>();
        Error? error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchCountryListAsyncAPI);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            List<Country>? countries = responseWrapper?.Data?.Countries.ToObject<List<Country>>();
            if (countries != null && countries.Count > 0)
            {
                countryList.AddRange(countries);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (countryList, error);
    }
    public async Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year)
    {
        ExcessRainfalls excessRainfalls = new ExcessRainfalls();
        Error? error = new Error();
        string url = string.Format(ApiurlHelper.FetchExcessRainfallByFarmIdAndYearAPI, farmId, year);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessRainfalls = responseWrapper.Data.ExcessRainfall.ToObject<ExcessRainfalls>();
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (excessRainfalls, error);
    }
    public async Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync()
    {
        List<CommonResponse> excessWinterRainfallOption = new List<CommonResponse>();
        Error? error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchExcessWinterRainfallOptionAPI);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessWinterRainfallOption = responseWrapper.Data.ExcessWinterRainFallOptions.ToObject<List<CommonResponse>>();

        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (excessWinterRainfallOption, error);
    }
    public async Task<(ExcessRainfalls?, Error?)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated)
    {
        ExcessRainfalls? excessRainfalls = null;
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Empty;
        HttpResponseMessage response = null;
        url = string.Format(ApiurlHelper.AddOrUpdateExcessWinterRainfallAPI, farmId, year);
        if (isUpdated != null && isUpdated)
        {
            response = await httpClient.PutAsync(url, new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
        }
        else
        {
            response = await httpClient.PostAsync(url, new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
        }

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
        {
            JObject? excessRainfallObj = responseWrapper?.Data["ExcessRainfall"] as JObject;
            if (excessRainfallObj != null)
            {
                excessRainfalls = excessRainfallObj.ToObject<ExcessRainfalls>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (excessRainfalls, error);
    }
    public async Task<(CommonResponse?, Error?)> FetchExcessWinterRainfallOptionByIdAsync(int id)
    {
        CommonResponse? excessWinterRainfallOption = new CommonResponse();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Format(ApiurlHelper.FetchExcessWinterRainfallOptionByIdAPI, id);
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessWinterRainfallOption = responseWrapper?.Data?.records.ToObject<CommonResponse>();
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (excessWinterRainfallOption, error);
    }
    public async Task<List<NvzActionProgramResponse>> FetchNvzActionProgramsByCountryIdAsync(int countryId)
    {
        List<NvzActionProgramResponse> nvzActionProgramResponses = new List<NvzActionProgramResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchNvzActionProgramsByCountryIdAsyncAPI, countryId);
        var response = await httpClient.GetAsync(requestUrl);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                nvzActionProgramResponses.AddRange(responseWrapper?.Data.ToObject<List<NvzActionProgramResponse>>());
            }
        }

        return nvzActionProgramResponses;
    }
}
