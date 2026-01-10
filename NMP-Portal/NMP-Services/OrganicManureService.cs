using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Models;
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
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeByFarmIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
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
            url = string.Format(APIURLHelper.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName), HttpUtility.UrlEncode(farmId.ToString()));
        }
        else
        {
            url = string.Format(APIURLHelper.FetchFieldByFarmIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString()));
        }
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
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
            url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()), HttpUtility.UrlEncode(cropOrder.ToString()));
        }
        else
        {
            url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()));
        }
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (managementIds, error);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchManureGroupList()
    {
        List<CommonResponse> manureGroupList = new List<CommonResponse>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchMannerManureGroupListAsyncAPI);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var manureGroups = responseWrapper?.Data?.ToObject<List<CommonResponse>>();
            manureGroupList.AddRange(manureGroups);
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (manureGroupList, error);
    }
    public async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId)
    {
        List<ManureType> manureTypeList = new List<ManureType>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureTypeListByGroupIdAndCountryAsyncAPI, HttpUtility.UrlEncode(manureGroupId.ToString()), HttpUtility.UrlEncode(countryId.ToString())));
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (manureTypeList, error);
    }
    public async Task<(CommonResponse, Error?)> FetchManureGroupById(int manureGroupId)
    {
        CommonResponse? manureGroup = new CommonResponse();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureGroupByIdAsyncAPI, HttpUtility.UrlEncode(manureGroupId.ToString())));
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (manureGroup, error);
    }

    public async Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId)
    {
        ManureType? manureType = null;
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureTypeByManureTypeIdAsyncAPI, HttpUtility.UrlEncode(manureTypeId.ToString())));
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (manureType, error);
    }

    public async Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid)
    {
        List<ApplicationMethodResponse> applicationMethodList = new List<ApplicationMethodResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerApplicationMethodsByApplicableForAsyncAPI, HttpUtility.UrlEncode(isLiquid.ToString()), HttpUtility.UrlEncode(fieldType.ToString())));
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (applicationMethodList, error);
    }

    public async Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor)
    {
        List<IncorporationMethodResponse> incorporationMethods = new List<IncorporationMethodResponse>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationMethodsByApplicationIdAsyncAPI, HttpUtility.UrlEncode(appId.ToString()), applicableFor));
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (incorporationMethods, error);
    }

    public async Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
    {
        List<IncorprationDelaysResponse> incorporationDelays = new List<IncorprationDelaysResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationDelaysByMethodIdAndApplicableForAsyncAPI, HttpUtility.UrlEncode(methodId.ToString()), HttpUtility.UrlEncode(applicableFor)));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (incorporationDelays, error);
    }
    public async Task<(string, Error)> FetchApplicationMethodById(int Id)
    {
        string applicationMethod = string.Empty;
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerApplicationMethodByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (applicationMethod, error);
    }
    public async Task<(string, Error)> FetchIncorporationMethodById(int Id)
    {
        string incorporationMethod = string.Empty;
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationMethodByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (incorporationMethod, error);
    }
    public async Task<(string, Error)> FetchIncorporationDelayById(int Id)
    {
        string incorporationDelay = string.Empty;
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationDelaysByIdAsyncAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)// && responseWrapper.Data.IncorporationDelay != null
                {
                    incorporationDelay = responseWrapper.Data.name;//.IncorporationDelay.Name;

                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
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

            var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddOrganicManuresAsyncAPI), new StringContent(organicManureData, Encoding.UTF8, "application/json"));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (success, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeDefault()
    {
        RainTypeResponse rainType = new RainTypeResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchRainTypeDefault);
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (rainType, error);
    }
    public async Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString)
    {
        int totalRainfall = 0;
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(APIURLHelper.FetchMannerRainfallByPostcodeAndDateRangeAsyncAPI, new StringContent(jsonString, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    totalRainfall = responseWrapper.Data.rainfallPostApplication != null ? responseWrapper.Data.rainfallPostApplication.value.ToObject<int>() : 0;
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return totalRainfall;
    }

    public async Task<(WindspeedResponse, Error)> FetchWindspeedDataDefault()
    {
        WindspeedResponse windSpeed = new WindspeedResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchWindspeedDataDefault);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    windSpeed = responseWrapper.Data.ToObject<WindspeedResponse>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (windSpeed, error);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate)
    {
        MoistureTypeResponse moisterType = new MoistureTypeResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMoisterTypeDefaultByApplicationDate, HttpUtility.UrlEncode(applicationDate.ToString())));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (moisterType, error);
    }

    public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeList()
    {
        List<RainTypeResponse> rainType = new List<RainTypeResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchMannerRainTypesAsyncAPI);
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (rainType, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId)
    {
        RainTypeResponse rainType = new RainTypeResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerRainTypeByIdAsyncAPI, HttpUtility.UrlEncode(rainTypeId.ToString())));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (rainType, error);
    }

    public async Task<(List<WindspeedResponse>, Error)> FetchWindspeedList()
    {
        List<WindspeedResponse> windspeeds = new List<WindspeedResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchMannerWindspeedsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var windspeed = responseWrapper.Data.ToObject<List<WindspeedResponse>>();
                    windspeeds.AddRange(windspeed);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (windspeeds, error);
    }
    public async Task<(WindspeedResponse, Error)> FetchWindspeedById(int windspeedId)
    {
        WindspeedResponse windspeedResponse = new WindspeedResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerWindspeedByIdAsyncAPI, HttpUtility.UrlEncode(windspeedId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    windspeedResponse = responseWrapper.Data.ToObject<WindspeedResponse>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (windspeedResponse, error);
    }
    public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList()
    {
        List<MoistureTypeResponse> moisterTypes = new List<MoistureTypeResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchMannerMoistureTypesAsyncAPI);
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (moisterTypes, error);
    }
    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId)
    {
        MoistureTypeResponse moistureTypeResponse = new MoistureTypeResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerMoistureTypeByIdAsyncAPI, HttpUtility.UrlEncode(moisterTypeId.ToString())));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (moistureTypeResponse, error);
    }
    public async Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId)
    {
        Error error = new Error();
        bool isPerennial = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeLinkingsByCropTypeIdAsyncAPI, HttpUtility.UrlEncode(cropTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    CropTypeLinkingResponse cropTypeLinkingResponse = responseWrapper.Data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
                    isPerennial = cropTypeLinkingResponse.IsPerennial ?? false;
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
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
        string url = APIURLHelper.FetchTotalNByManagementIdAndAppDateAsyncAPI;

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
            if (responseWrapper != null && responseWrapper.Error != null)
            {

                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (totalN, error);
    }

    public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        CropTypeResponse cropType = new CropTypeResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeByFieldIdAndHarvestYearAsyncAPI, fieldId, year, confirm));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (cropType, error);
    }
    public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId)
    {
        CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeLinkingByCropTypeIdAsyncAPI, cropTypeId));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (cropTypeLinking, error);
    }
    public async Task<(List<int>, Error)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year, bool confirm)
    {
        List<int> manureTypeIds = new List<int>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsyncAPI, fieldId, year, confirm));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (manureTypeIds, error);
    }

    public async Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManure(int managementId)
    {
        List<int> manureTypeIds = new List<int>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureTypeIdsByManIdFromOrgManureAsyncAPI, managementId));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (manureTypeIds, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        Error error = null;
        decimal totalN = 0;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = APIURLHelper.FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsyncAPI;

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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (totalN, error);
    }

    public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly)
    {
        Error error = null;
        bool isOrganicManureExist = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string requestUrl = APIURLHelper.FetchOrganicManureExistanceByDateRangeAsyncAPI;

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
                if (responseWrapper != null && responseWrapper.Error != null)
                {

                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (isOrganicManureExist, error);
    }

    public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString)
    {
        Error error = null;
        NitrogenUptakeResponse nitrogenUptakeResponse = new NitrogenUptakeResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(string.Format(APIURLHelper.FetchMannerAutumnCropNitrogenUptakeAsyncAPI), new StringContent(jsonString, Encoding.UTF8, "application/json"));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (nitrogenUptakeResponse, error);
    }
    public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmId(int farmId)
    {
        List<FarmManureTypeResponse> farmManureTypes = new List<FarmManureTypeResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmManureTypesByFarmIdAsyncAPI, farmId));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (farmManureTypes, error);
    }
    public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrient(string jsonData)
    {
        MannerCalculateNutrientResponse mannerCalculateNutrientResponse = new MannerCalculateNutrientResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(APIURLHelper.FetchMannerCalculateNutrientAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (mannerCalculateNutrientResponse, error);
    }

    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeId(int soilTypeId)
    {
        SoilTypeSoilTextureResponse soilTypeSoilTexture = new SoilTypeSoilTextureResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilTypeSoilTextureBySoilTypeIdAsyncAPI, soilTypeId));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (soilTypeSoilTexture, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(int fieldId, DateTime startDate, DateTime endDate, bool confirm, bool isGreenFoodCompost, int? organicManureId)
    {
        Error error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = APIURLHelper.FetchTotalNBasedByManIdAppDateAndIsGreenCompostAsyncAPI;

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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = new Error();
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        catch (HttpRequestException hre)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            if (error == null)
            {
                error = new Error();
            }
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (totalN, error);
    }
    public async Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureById(int id)
    {
        OrganicManureDataViewModel organicManure = new OrganicManureDataViewModel();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchOrganicManureByIdAPI, id));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
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
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchOrganicManureByFarmIdAndYearAPI, farmId, year));
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (organicManures, error);
    }
    public async Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var content = new StringContent(orgManureIds, Encoding.UTF8, "application/json");
            var url = APIURLHelper.DeleteOrganicManureByAPI;
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
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }

        return (message, error);
    }
    public async Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeId(int farmId, int ManureTypeId, string ManureTypeName)
    {
        bool isFarmManureTypeExist = false;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var farmExist = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmManureTypeCheckByFarmIdAndManureTypeIdAPI, farmId, ManureTypeId, ManureTypeName.Trim()));
            string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
            if (responseWrapperFarmExist.Data["exists"] == true)
            {
                isFarmManureTypeExist = true;
            }

        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
        }

        return (isFarmManureTypeExist, error);
    }
    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureType(int fertiliserId, int farmId, int harvestYear)
    {
        Error error = new Error();
        List<FertiliserAndOrganicManureUpdateResponse> organicResponse = new List<FertiliserAndOrganicManureUpdateResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldWithSameDateAndManureTypeAPI, fertiliserId, farmId, harvestYear));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                organicResponse = responseWrapper.Data.ToObject<List<FertiliserAndOrganicManureUpdateResponse>>();
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }

        return (organicResponse, error);
    }

    public async Task<(List<OrganicManure>, Error)> UpdateOrganicManure(string organicManureData)
    {
        Error? error = new Error();
        List<OrganicManure> organicManures = new List<OrganicManure>();

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PutAsync(APIURLHelper.UpdateOrganicManureAsyncAPI, new StringContent(organicManureData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            List<OrganicManure> organics = responseWrapper.Data.OrganicManure.ToObject<List<OrganicManure>>();
            if (organics != null && organics.Count > 0)
            {
                organicManures.AddRange(organics);
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (organicManures, error);
    }

    public async Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodID(int managementPeriodID)
    {
        Error? error = null;
        decimal? totalN = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchOragnicManureAvailableNByManagementPeriodIDAPI, HttpUtility.UrlEncode(managementPeriodID.ToString())));
        response.EnsureSuccessStatusCode();
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
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLogTemplate, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (totalN, error);
    }
}
