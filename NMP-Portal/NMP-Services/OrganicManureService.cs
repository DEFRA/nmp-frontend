using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Models;
using NMP.Commons.Helpers;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class OrganicManureService(ILogger<OrganicManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IOrganicManureService
{
    private readonly ILogger<OrganicManureService> _logger = logger;
    private const string _errorLogTemplate = "{Code} : {Message} : {Stack} : {Path}";
    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        List<ManureCropTypeResponse> cropTypeList = new List<ManureCropTypeResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeByFarmIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var cropTypeResponseList = responseWrapper?.Data?.ToObject<List<ManureCropTypeResponse>>();
                cropTypeList.AddRange(cropTypeResponseList);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (cropTypeList, error);
    }
    public async Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        List<CommonResponse> fieldResponses = new List<CommonResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName), HttpUtility.UrlEncode(farmId.ToString()));
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString()));
        }
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var fieldResponseList = responseWrapper?.Data?.ToObject<List<CommonResponse>>();
                fieldResponses.AddRange(fieldResponseList);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (fieldResponses, error);
    }

    public async Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        List<int> managementIds = new List<int>();
        Error? error = null;
        cropOrder ??= 1;
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()), HttpUtility.UrlEncode(cropOrder.ToString()));
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()));
        }
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            List<CommonResponse>? managementIdsList = responseWrapper?.Data?.ManagementPeriods?.ToObject<List<CommonResponse>>();
            if (managementIdsList != null)
            {
                managementIds.AddRange(managementIdsList.Select(x => x.Id));
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (managementIds, error);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchManureGroupList()
    {
        List<CommonResponse> manureGroupList = new List<CommonResponse>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerManureGroupListAsyncAPI);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var manureGroups = responseWrapper?.Data?.ToObject<List<CommonResponse>>();
            manureGroupList.AddRange(manureGroups);
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (manureGroupList, error);
    }
    public async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId)
    {
        List<ManureType> manureTypeList = new List<ManureType>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerManureTypeListByGroupIdAndCountryAsyncAPI, HttpUtility.UrlEncode(manureGroupId.ToString()), HttpUtility.UrlEncode(countryId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var manureTypes = responseWrapper?.Data?.ToObject<List<ManureType>>();
                manureTypeList.AddRange(manureTypes);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (manureTypeList, error);
    }
    public async Task<(CommonResponse, Error?)> FetchManureGroupById(int manureGroupId)
    {
        CommonResponse? manureGroup = new CommonResponse();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerManureGroupByIdAsyncAPI, HttpUtility.UrlEncode(manureGroupId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                manureGroup = responseWrapper?.Data?.ToObject<CommonResponse>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (manureGroup, error);
    }

    public async Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId)
    {
        ManureType? manureType = null;
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerManureTypeByManureTypeIdAsyncAPI, HttpUtility.UrlEncode(manureTypeId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                manureType = responseWrapper?.Data?.ToObject<ManureType>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (manureType, error);
    }

    public async Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid)
    {
        List<ApplicationMethodResponse> applicationMethodList = new List<ApplicationMethodResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerApplicationMethodsByApplicableForAsyncAPI, HttpUtility.UrlEncode(isLiquid.ToString()), HttpUtility.UrlEncode(fieldType.ToString())));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var applicationMethods = responseWrapper?.Data?.ToObject<List<ApplicationMethodResponse>>();
                applicationMethodList.AddRange(applicationMethods);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (applicationMethodList, error);
    }

    public async Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor)
    {
        List<IncorporationMethodResponse> incorporationMethods = new List<IncorporationMethodResponse>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationMethodsByApplicationIdAsyncAPI, HttpUtility.UrlEncode(appId.ToString()), applicableFor));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var methods = responseWrapper?.Data?.ToObject<List<IncorporationMethodResponse>>();
                incorporationMethods.AddRange(methods);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (incorporationMethods, error);
    }

    public async Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
    {
        List<IncorprationDelaysResponse> incorporationDelays = new List<IncorprationDelaysResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationDelaysByMethodIdAndApplicableForAsyncAPI, HttpUtility.UrlEncode(methodId.ToString()), HttpUtility.UrlEncode(applicableFor)));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var delays = responseWrapper.Data.ToObject<List<IncorprationDelaysResponse>>();
                    incorporationDelays.AddRange(delays);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (incorporationDelays, error);
    }
    public async Task<(string, Error)> FetchApplicationMethodById(int Id)
    {
        string applicationMethod = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerApplicationMethodByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    applicationMethod = responseWrapper.Data.name;

                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (applicationMethod, error);
    }
    public async Task<(string, Error)> FetchIncorporationMethodById(int Id)
    {
        string incorporationMethod = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationMethodByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    incorporationMethod = responseWrapper.Data.name;

                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (incorporationMethod, error);
    }
    public async Task<(string, Error)> FetchIncorporationDelayById(int Id)
    {
        string incorporationDelay = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationDelaysByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)// && responseWrapper.Data.IncorporationDelay != null
                {
                    incorporationDelay = responseWrapper?.Data?.name;//.IncorporationDelay.Name;

                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (incorporationDelay, error);
    }
    public async Task<(bool, Error)> AddOrganicManuresAsync(string organicManureData)
    {
        bool success = false;
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddOrganicManuresAsyncAPI, new StringContent(organicManureData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                var organicManures = responseWrapper.Data.OrganicManures;

                if (organicManures != null)
                {
                    success = true;
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
        return (success, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeDefault()
    {
        RainTypeResponse rainType = new RainTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchRainTypeDefault);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    rainType = responseWrapper.Data.ToObject<RainTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (rainType, error);
    }
    public async Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString)
    {
        int totalRainfall = 0;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerRainfallByPostcodeAndDateRangeAsyncAPI, new StringContent(jsonString, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalRainfall = responseWrapper?.Data?.rainfallPostApplication != null ? responseWrapper.Data.rainfallPostApplication.value.ToObject<int>() : 0;
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return totalRainfall;
    }

    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefault()
    {
        WindspeedResponse? windSpeed = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchWindspeedDataDefault);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    windSpeed = responseWrapper?.Data?.ToObject<WindspeedResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (windSpeed, error);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate)
    {
        MoistureTypeResponse moisterType = new MoistureTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMoisterTypeDefaultByApplicationDate, HttpUtility.UrlEncode(applicationDate.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    moisterType = responseWrapper.Data.MoistureType.ToObject<MoistureTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (moisterType, error);
    }

    public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeList()
    {
        List<RainTypeResponse> rainType = new List<RainTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerRainTypesAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var rainTypes = responseWrapper.Data.ToObject<List<RainTypeResponse>>();
                    rainType.AddRange(rainTypes);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (rainType, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId)
    {
        RainTypeResponse rainType = new RainTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerRainTypeByIdAsyncAPI, HttpUtility.UrlEncode(rainTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    rainType = responseWrapper.Data.ToObject<RainTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (rainType, error);
    }

    public async Task<(List<WindspeedResponse>, Error?)> FetchWindspeedList()
    {
        List<WindspeedResponse> windspeeds = new List<WindspeedResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerWindspeedsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var windspeed = responseWrapper?.Data?.ToObject<List<WindspeedResponse>>();
                    windspeeds.AddRange(windspeed);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (windspeeds, error);
    }
    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedById(int windspeedId)
    {
        WindspeedResponse? windspeedResponse = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerWindspeedByIdAsyncAPI, HttpUtility.UrlEncode(windspeedId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    windspeedResponse = responseWrapper?.Data?.ToObject<WindspeedResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (windspeedResponse, error);
    }
    public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList()
    {
        List<MoistureTypeResponse> moisterTypes = new List<MoistureTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerMoistureTypesAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var moisterType = responseWrapper.Data.ToObject<List<MoistureTypeResponse>>();
                    moisterTypes.AddRange(moisterType);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);

        }
        return (moisterTypes, error);
    }
    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId)
    {
        MoistureTypeResponse moistureTypeResponse = new MoistureTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerMoistureTypeByIdAsyncAPI, HttpUtility.UrlEncode(moisterTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    moistureTypeResponse = responseWrapper.Data.ToObject<MoistureTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (moistureTypeResponse, error);
    }
    public async Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId)
    {
        Error? error = null;
        bool isPerennial = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingsByCropTypeIdAsyncAPI, HttpUtility.UrlEncode(cropTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    CropTypeLinkingResponse? cropTypeLinkingResponse = responseWrapper?.Data?.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
                    isPerennial = cropTypeLinkingResponse?.IsPerennial ?? false;
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return isPerennial;
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");

        HttpClient httpClient = await GetNMPAPIClient();
        string url = ApiurlHelper.FetchTotalNByManagementIdAndAppDateAsyncAPI;

        if (organicManureId.HasValue)
        {
            url += $"&organicManureID={organicManureId.Value}";
        }

        url = string.Format(url, HttpUtility.UrlEncode(managementId.ToString()), HttpUtility.UrlEncode(fromdate.ToString()), HttpUtility.UrlEncode(toDate.ToString()), HttpUtility.UrlEncode(confirm.ToString()));
        var response = await httpClient.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                totalN = responseWrapper.Data.TotalN != null ? responseWrapper.Data.TotalN.ToObject<decimal>() : 0;
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (totalN, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdAndAppDate(int cropId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = ApiurlHelper.FetchTotalNByCropIdAndAppDateAsyncAPI;

            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }

            url = string.Format(url, HttpUtility.UrlEncode(cropId.ToString()), HttpUtility.UrlEncode(fromdate.ToString()), HttpUtility.UrlEncode(toDate.ToString()), HttpUtility.UrlEncode(confirm.ToString()));
            var response = await httpClient.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalN = responseWrapper?.Data?.TotalN?.ToObject<decimal>() ?? 0;
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (totalN, error);
    }

    public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        CropTypeResponse cropType = new CropTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeByFieldIdAndHarvestYearAsyncAPI, fieldId, year, confirm));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    cropType = responseWrapper.Data.ToObject<CropTypeResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (cropType, error);
    }
    public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId)
    {
        CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingByCropTypeIdAsyncAPI, cropTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    cropTypeLinking = responseWrapper.Data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);

        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (cropTypeLinking, error);
    }
    public async Task<(List<int>, Error)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year, bool confirm)
    {
        List<int> manureTypeIds = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsyncAPI, fieldId, year, confirm));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    manureTypeIds = responseWrapper.Data.ManureTypeIds.ToObject<List<int>>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (manureTypeIds, error);
    }

    public async Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManure(int managementId)
    {
        List<int> manureTypeIds = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchManureTypeIdsByManIdFromOrgManureAsyncAPI, managementId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    manureTypeIds = responseWrapper.Data.ManureTypeIds.ToObject<List<int>>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (manureTypeIds, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = ApiurlHelper.FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsyncAPI;

            if (fertiliserId.HasValue)
            {
                url += $"&fertiliserID={fertiliserId.Value}";
            }
            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }

            url = string.Format(url, managementId, confirm);
            var response = await httpClient.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalN = responseWrapper.Data.TotalN != null ? responseWrapper.Data.TotalN.ToObject<decimal>() : 0;
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
        return (totalN, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(int cropId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string url = string.Format(ApiurlHelper.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserAsyncAPI, cropId, confirm);

            if (fertiliserId.HasValue)
                url += $"&fertiliserID={fertiliserId.Value}";

            if (organicManureId.HasValue)
                url += $"&organicManureID={organicManureId.Value}";

            var response = await httpClient.GetAsync(url);


            var result = await response.Content.ReadAsStringAsync();
            var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                totalN = responseWrapper?.Data?.TotalN?.ToObject<decimal>() ?? 0;
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
        return (totalN, error);
    }

    public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly)
    {
        Error? error = null;
        bool isOrganicManureExist = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string requestUrl = ApiurlHelper.FetchOrganicManureExistanceByDateRangeAsyncAPI;

            if (organicManureId.HasValue)
            {
                requestUrl += $"&organicManureID={organicManureId.Value}";
            }

            requestUrl = string.Format(requestUrl, managementId, dateFrom, dateTo, isConfirm, isSlurryOnly);

            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    isOrganicManureExist = responseWrapper.Data.exists.ToObject<bool>();
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
        return (isOrganicManureExist, error);
    }

    public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString)
    {
        Error? error = null;
        NitrogenUptakeResponse nitrogenUptakeResponse = new NitrogenUptakeResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerAutumnCropNitrogenUptakeAsyncAPI, new StringContent(jsonString, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    nitrogenUptakeResponse = responseWrapper.Data.nitrogenUptake != null ? responseWrapper.Data.nitrogenUptake.ToObject<NitrogenUptakeResponse>() : 0;
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
        return (nitrogenUptakeResponse, error);
    }
    public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmId(int farmId)
    {
        List<FarmManureTypeResponse> farmManureTypes = new List<FarmManureTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFarmManureTypesByFarmIdAsyncAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var farmManureType = responseWrapper.Data.FarmManureTypes.ToObject<List<FarmManureTypeResponse>>();
                    farmManureTypes.AddRange(farmManureType);
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
        return (farmManureTypes, error);
    }
    public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrient(string jsonData)
    {
        MannerCalculateNutrientResponse mannerCalculateNutrientResponse = new MannerCalculateNutrientResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerCalculateNutrientAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    mannerCalculateNutrientResponse = responseWrapper.Data.ToObject<MannerCalculateNutrientResponse>();
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
        return (mannerCalculateNutrientResponse, error);
    }

    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeId(int soilTypeId)
    {
        SoilTypeSoilTextureResponse soilTypeSoilTexture = new SoilTypeSoilTextureResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilTypeSoilTextureBySoilTypeIdAsyncAPI, soilTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    soilTypeSoilTexture = responseWrapper.Data.SoilTypeSoilTexture.ToObject<SoilTypeSoilTextureResponse>();
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
        return (soilTypeSoilTexture, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(int fieldId, DateTime startDate, DateTime endDate, bool confirm, bool isGreenFoodCompost, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = ApiurlHelper.FetchTotalNBasedByManIdAppDateAndIsGreenCompostAsyncAPI;

            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }

            url = string.Format(url, fieldId, fromdate, toDate, confirm, isGreenFoodCompost);
            var response = await httpClient.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalN = responseWrapper.Data.TotalN != null ? responseWrapper.Data.TotalN.ToObject<decimal>() : 0;
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
        return (totalN, error);
    }
    public async Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureById(int id)
    {
        OrganicManureDataViewModel organicManure = new OrganicManureDataViewModel();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchOrganicManureByIdAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    organicManure = responseWrapper.Data.ToObject<OrganicManureDataViewModel>();
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
        return (organicManure, error);
    }
    public async Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYear(int farmId, int year)
    {
        List<OrganicManure> organicManures = new List<OrganicManure>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchOrganicManureByFarmIdAndYearAPI, farmId, year));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    organicManures = responseWrapper.Data.ToObject<OrganicManure>();
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
        return (organicManures, error);
    }

    public async Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds)
    {
        Error? error = null;
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var content = new StringContent(orgManureIds, Encoding.UTF8, "application/json");
            var url = ApiurlHelper.DeleteOrganicManureByAPI;
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };
            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
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

        return (message, error);
    }
    public async Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeId(int farmId, int ManureTypeId, string ManureTypeName)
    {
        bool isFarmManureTypeExist = false;
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFarmManureTypeCheckByFarmIdAndManureTypeIdAPI, farmId, ManureTypeId, ManureTypeName.Trim()));
            string resultFarmExist = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                if (responseWrapper?.Data["exists"] == true)
                {
                    isFarmManureTypeExist = true;
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

        return (isFarmManureTypeExist, error);
    }
    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureType(int fertiliserId, int farmId, int harvestYear)
    {
        Error? error = new Error();
        List<FertiliserAndOrganicManureUpdateResponse> organicResponse = new List<FertiliserAndOrganicManureUpdateResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFieldWithSameDateAndManureTypeAPI, fertiliserId, farmId, harvestYear));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                organicResponse.AddRange(responseWrapper?.Data?.ToObject<List<FertiliserAndOrganicManureUpdateResponse>>());
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

        return (organicResponse, error);
    }

    public async Task<(List<OrganicManure>, Error)> UpdateOrganicManure(string organicManureData)
    {
        Error? error = null;
        List<OrganicManure> organicManures = new List<OrganicManure>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateOrganicManureAsyncAPI, new StringContent(organicManureData, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                List<OrganicManure>? organics = responseWrapper?.Data?.OrganicManure.ToObject<List<OrganicManure>>();
                if (organics != null && organics.Any())
                {
                    organicManures.AddRange(organics);
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
        return (organicManures, error);
    }

    public async Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodID(int managementPeriodID)
    {
        Error? error = null;
        decimal? totalN = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchOragnicManureAvailableNByManagementPeriodIDAPI, HttpUtility.UrlEncode(managementPeriodID.ToString())));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalN = responseWrapper?.Data?.TotalN;
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
        return (totalN, error);
    }

    public async Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeById(int id)
    {
        FarmManureTypeResponse? farmManureType = new FarmManureTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFarmManureTypeByIdAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.records != null)
            {
                farmManureType = responseWrapper?.Data?.records?.ToObject<FarmManureTypeResponse>();
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
        return (farmManureType, error);
    }
}
