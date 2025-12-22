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
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class FarmService : Service, IFarmService
{
    private readonly ILogger<FarmService> _logger;
    public FarmService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        _logger = logger;
    }
    public async Task<(List<Farm>, Error)> FetchFarmByOrgIdAsync(Guid orgId)
    {
        List<Farm> farmList = new List<Farm>();
        Error error = new Error();
        string url = string.Format(APIURLHelper.FetchFarmByOrgIdAPI, orgId);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            List<Farm> farms = responseWrapper.Data.Farms.ToObject<List<Farm>>();
            if (farms != null && farms.Count > 0)
            {
                farmList.AddRange(farms);
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }
        return (farmList, error);
    }
    public async Task<(Farm, Error)> AddFarmAsync(FarmData farmData)
    {
        string jsonData = JsonConvert.SerializeObject(farmData);
        Farm farm = null;
        Error error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();

        // check if farm already exists or not
        bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID);
        if (!IsFarmExist)
        {
            // if new farm then save farm data
            var response = await httpClient.PostAsync(APIURLHelper.AddFarmAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
            {

                JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                if (farmData != null)
                {
                    farm = farmDataJObject.ToObject<Farm>();
                }

            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        else
        {
            error.Message =
                string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode);
        }

        return (farm, error);
    }
    public async Task<(Farm, Error)> FetchFarmByIdAsync(int farmId)
    {
        Farm farm = new Farm();
        Error error = new Error();
        string url = string.Format(APIURLHelper.FetchFarmByIdAPI, farmId);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
            if (farmDataJObject != null)
            {
                farm = farmDataJObject.ToObject<Farm>();
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (farm, error);
    }

    public async Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id)
    {
        bool isFarmExist = false;
        string url = string.Format(APIURLHelper.IsFarmExist, farmName, postcode.Trim(), Id);
        HttpClient httpClient = await GetNMPAPIClient();
        var farmExist = await httpClient.GetAsync(url);
        farmExist.EnsureSuccessStatusCode();
        string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
        if (responseWrapperFarmExist.Data["exists"] == true)
        {
            isFarmExist = true;
        }

        return isFarmExist;
    }

    public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
    {
        decimal rainfallAverage = 0;
        Error error = new Error();
        string url = string.Format(APIURLHelper.FetchMannerRainfallAverageAsyncAPI, firstHalfPostcode);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            rainfallAverage = responseWrapper.Data.avarageAnnualRainfall != null ? responseWrapper.Data.avarageAnnualRainfall.value : 0;
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return rainfallAverage;
    }

    public async Task<(Farm, Error)> UpdateFarmAsync(FarmData farmData)
    {
        string jsonData = JsonConvert.SerializeObject(farmData);
        Farm farm = null;
        Error error = new Error();

        //check if Updated farm Name already exist or not in the Postcode...
        bool IsFarmNameWithInPostCodeAlreadyExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID);
        if (!IsFarmNameWithInPostCodeAlreadyExist)
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(APIURLHelper.UpdateFarmAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
            {

                JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                if (farmData != null)
                {
                    farm = farmDataJObject.ToObject<Farm>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        else
        {
            error.Message =
                string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode);
        }

        return (farm, error);
    }

    public async Task<(string, Error)> DeleteFarmByIdAsync(int farmId)
    {
        Error error = new Error();
        string message = string.Empty;
        string url = string.Format(APIURLHelper.DeleteFarmByIdAPI, farmId);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            message = responseWrapper.Data["message"].Value;
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (message, error);
    }
    public async Task<(List<Country>, Error)> FetchCountryAsync()
    {
        List<Country> countryList = new List<Country>();
        Error error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchCountryListAsyncAPI);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            List<Country> countries = responseWrapper.Data.Countries.ToObject<List<Country>>();
            if (countries != null && countries.Count > 0)
            {
                countryList.AddRange(countries);
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (countryList, error);
    }
    public async Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year)
    {
        ExcessRainfalls excessRainfalls = new ExcessRainfalls();
        Error error = new Error();
        string url = string.Format(APIURLHelper.FetchExcessRainfallByFarmIdAndYearAPI, farmId, year);
        HttpClient httpClient = await GetNMPAPIClient();

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessRainfalls = responseWrapper.Data.ExcessRainfall.ToObject<ExcessRainfalls>();

        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (excessRainfalls, error);
    }
    public async Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync()
    {
        List<CommonResponse> excessWinterRainfallOption = new List<CommonResponse>();
        Error error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchExcessWinterRainfallOptionAPI);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessWinterRainfallOption = responseWrapper.Data.ExcessWinterRainFallOptions.ToObject<List<CommonResponse>>();

        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (excessWinterRainfallOption, error);
    }
    public async Task<(ExcessRainfalls, Error)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated)
    {
        ExcessRainfalls excessRainfalls = null;
        Error error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Empty;
        HttpResponseMessage response = null;
        url = string.Format(APIURLHelper.AddOrUpdateExcessWinterRainfallAPI, farmId, year);
        if (isUpdated != null && isUpdated)
        {
            response = await httpClient.PutAsync(url, new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
        }
        else
        {
            response = await httpClient.PostAsync(url, new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
        }
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
        {

            JObject excessRainfallObj = responseWrapper.Data["ExcessRainfall"] as JObject;
            if (excessRainfallObj != null)
            {
                excessRainfalls = excessRainfallObj.ToObject<ExcessRainfalls>();
            }

        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }
        return (excessRainfalls, error);
    }
    public async Task<(CommonResponse, Error)> FetchExcessWinterRainfallOptionByIdAsync(int id)
    {
        CommonResponse excessWinterRainfallOption = new CommonResponse();
        Error error = new Error();

        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Format(APIURLHelper.FetchExcessWinterRainfallOptionByIdAPI, id);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            excessWinterRainfallOption = responseWrapper.Data.records.ToObject<CommonResponse>();

        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper.Error.ToObject<Error>();
                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
            }
        }

        return (excessWinterRainfallOption, error);
    }
}
