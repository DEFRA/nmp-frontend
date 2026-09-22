using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class OrganicManureService(ILogger<OrganicManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IOrganicManureService
{
    private readonly ILogger<OrganicManureService> _logger = logger;
    private const string _applicationJson = "application/json";
    private const string _dateFormat = "yyyy-MM-dd";
    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYearAsync(int farmId, int harvestYear)
    {
        List<ManureCropTypeResponse> cropTypeList = new List<ManureCropTypeResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeByFarmIdAndHarvestYearAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));

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
    public async Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, int farmId, string? cropGroupName)
    {
        List<CommonResponse> fieldResponses = new List<CommonResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName), HttpUtility.UrlEncode(farmId.ToString()));
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString()));
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
    public async Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        List<int> managementIds = new List<int>();
        Error? error = null;
        cropOrder ??= 1;
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(cropGroupName.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()), HttpUtility.UrlEncode(cropOrder.ToString()));
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(fieldIds.ToString()));
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
    public async Task<(bool, Error?)> AddOrganicManuresAsync(string organicManureData)
    {
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddOrganicManuresAPI, new StringContent(organicManureData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                var organicManures = responseWrapper?.Data?.OrganicManures;

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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }

        return (success, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeDefaultAsync()
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
                if(responseWrapper?.Data is JObject data)
                {
                    rainType = data.ToObject<RainTypeResponse>() ?? new RainTypeResponse();
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
        return (rainType, error);
    }
    public async Task<int> FetchRainfallByPostcodeAndDateRangeAsync(string jsonString)
    {
        int totalRainfall = 0;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerRainfallByPostcodeAndDateRangeAPI, new StringContent(jsonString, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.rainfallPostApplication?.value is JToken rainfallValue)
                {
                    totalRainfall = rainfallValue.ToObject<int?>() ?? 0;
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
    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefaultAsync()
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (windSpeed, error);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDateAsync(string applicationDate)
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
                if (responseWrapper?.Data?.MoistureType is JToken moistureTypeToken)
                {
                    moisterType = moistureTypeToken.ToObject<MoistureTypeResponse>() ?? new MoistureTypeResponse();
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
        return (moisterType, error);
    }

    public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeListAsync()
    {
        List<RainTypeResponse> rainType = new List<RainTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerRainTypesAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    var rainTypes = data.ToObject<List<RainTypeResponse>>() ?? new List<RainTypeResponse>();
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (rainType, error);
    }
    public async Task<(RainTypeResponse, Error)> FetchRainTypeByIdAsync(int rainTypeId)
    {
        RainTypeResponse rainType = new RainTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerRainTypeByIdAPI, HttpUtility.UrlEncode(rainTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    rainType = data.ToObject<RainTypeResponse>() ?? new RainTypeResponse();
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
        return (rainType, error);
    }

    public async Task<(List<WindspeedResponse>, Error?)> FetchWindspeedListAsync()
    {
        List<WindspeedResponse> windspeeds = new List<WindspeedResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerWindspeedsAPI);
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (windspeeds, error);
    }
    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedByIdAsync(int windspeedId)
    {
        WindspeedResponse? windspeedResponse = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerWindspeedByIdAPI, HttpUtility.UrlEncode(windspeedId.ToString())));
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (windspeedResponse, error);
    }
    public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeListAsync()
    {
        List<MoistureTypeResponse> moisterTypes = new List<MoistureTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerMoistureTypesAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    var moistureType = data.ToObject<List<MoistureTypeResponse>>() ?? new List<MoistureTypeResponse>();
                    moisterTypes.AddRange(moistureType);
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
        return (moisterTypes, error);
    }
    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeByIdAsync(int moisterTypeId)
    {
        MoistureTypeResponse moistureTypeResponse = new MoistureTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerMoistureTypeByIdAPI, HttpUtility.UrlEncode(moisterTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    moistureTypeResponse = data.ToObject<MoistureTypeResponse>() ?? new MoistureTypeResponse();
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
        return (moistureTypeResponse, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDateAsync(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString(_dateFormat);
        string toDate = endDate.ToString(_dateFormat);

        string url = ApiurlHelper.FetchTotalNByManagementIdAndAppDateAPI;

        if (organicManureId.HasValue)
        {
            url += $"&organicManureID={organicManureId.Value}";
        }
        url = string.Format(url, HttpUtility.UrlEncode(managementId.ToString()), HttpUtility.UrlEncode(fromdate.ToString()), HttpUtility.UrlEncode(toDate.ToString()), HttpUtility.UrlEncode(confirm.ToString()));
        (totalN, error) = await GetTotalN(url);
        return (totalN, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdAndAppDateAsync(int cropId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString(_dateFormat);
        string toDate = endDate.ToString(_dateFormat);
        try
        {
            string url = ApiurlHelper.FetchTotalNByCropIdAndAppDateAPI;
            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }
            url = string.Format(url, HttpUtility.UrlEncode(cropId.ToString()), HttpUtility.UrlEncode(fromdate.ToString()), HttpUtility.UrlEncode(toDate.ToString()), HttpUtility.UrlEncode(confirm.ToString()));
            (totalN, error) = await GetTotalN(url);

        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }

    public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYearAsync(int fieldId, int year, bool confirm)
    {
        CropTypeResponse cropType = new CropTypeResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeByFieldIdAndHarvestYearAPI, fieldId, year, confirm));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    cropType = data.ToObject<CropTypeResponse>() ?? new CropTypeResponse();
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
        return (cropType, error);
    }
    public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeIdAsync(int cropTypeId)
    {
        CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingByCropTypeIdAPI, cropTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.CropTypeLinking is JToken cropTypeLinkingToken)
                {
                    cropTypeLinking = cropTypeLinkingToken.ToObject<CropTypeLinkingResponse>() ?? new CropTypeLinkingResponse();
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
        return (cropTypeLinking, error);
    }
    public async Task<(List<int>, Error?)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsync(int fieldId, int year, bool confirm)
    {
        List<int> manureTypeIds = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAPI, fieldId, year, confirm));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var ids = responseWrapper?.Data?.ManureTypeIds.ToObject<List<int>>();
                    if (ids != null)
                    {
                        manureTypeIds.AddRange(ids);
                    }
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
        return (manureTypeIds, error);
    }

    public async Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManureAsync(int managementId)
    {
        List<int> manureTypeIds = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchManureTypeIdsByManIdFromOrgManureAPI, managementId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.ManureTypeIds is JToken manureTypeIdsToken)
                {
                    manureTypeIds = manureTypeIdsToken.ToObject<List<int>>() ?? new List<int>();
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
        return (manureTypeIds, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsync(int managementId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        try
        {
            string url = ApiurlHelper.FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAPI;
            if (fertiliserId.HasValue)
            {
                url += $"&fertiliserID={fertiliserId.Value}";
            }
            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }
            url = string.Format(url, managementId, confirm);
            (totalN, error) = await GetTotalN(url);

        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserAsync(int cropId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        try
        {
            string url = string.Format(ApiurlHelper.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserAPI, cropId, confirm);
            if (fertiliserId.HasValue)
                url += $"&fertiliserID={fertiliserId.Value}";

            if (organicManureId.HasValue)
                url += $"&organicManureID={organicManureId.Value}";

            (totalN, error) = await GetTotalN(url);
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }

    public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRangeAsync(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly)
    {
        Error? error = null;
        bool isOrganicManureExist = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string requestUrl = ApiurlHelper.FetchOrganicManureExistanceByDateRangeAPI;

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
                if (responseWrapper?.Data?.exists is JToken existsToken)
                {
                    isOrganicManureExist = existsToken.ToObject<bool>();
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
        return (isOrganicManureExist, error);
    }

    public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptakeAsync(string jsonString)
    {
        Error? error = null;
        NitrogenUptakeResponse? nitrogenUptakeResponse = new NitrogenUptakeResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerAutumnCropNitrogenUptakeAPI, new StringContent(jsonString, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    nitrogenUptakeResponse = responseWrapper?.Data?.nitrogenUptake != null ? responseWrapper?.Data?.nitrogenUptake.ToObject<NitrogenUptakeResponse>() : 0;
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
        return (nitrogenUptakeResponse, error);
    }
    public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmIdAsync(int farmId)
    {
        List<FarmManureTypeResponse> farmManureTypes = new List<FarmManureTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFarmManureTypesByFarmIdAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var farmManureType = responseWrapper?.Data?.FarmManureTypes.ToObject<List<FarmManureTypeResponse>>();
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (farmManureTypes, error);
    }
    public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrientAsync(string jsonData)
    {
        MannerCalculateNutrientResponse mannerCalculateNutrientResponse = new MannerCalculateNutrientResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.FetchMannerCalculateNutrientAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    mannerCalculateNutrientResponse = data.ToObject<MannerCalculateNutrientResponse>()
                        ?? new MannerCalculateNutrientResponse();
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
        return (mannerCalculateNutrientResponse, error);
    }

    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeIdAsync(int soilTypeId)
    {
        SoilTypeSoilTextureResponse? soilTypeSoilTexture = new SoilTypeSoilTextureResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilTypeSoilTextureBySoilTypeIdAPI, soilTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    soilTypeSoilTexture = responseWrapper?.Data?.SoilTypeSoilTexture.ToObject<SoilTypeSoilTextureResponse>();
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
        return (soilTypeSoilTexture, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompostAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm, bool isGreenFoodCompost, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString(_dateFormat);
        string toDate = endDate.ToString(_dateFormat);
        try
        {
            string url = ApiurlHelper.FetchTotalNBasedByManIdAppDateAndIsGreenCompostAPI;
            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }
            url = string.Format(url, fieldId, fromdate, toDate, confirm, isGreenFoodCompost);
            (totalN, error) = await GetTotalN(url);
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        Error? error = null;
        decimal totalN = 0;
        string fromdate = startDate.ToString(_dateFormat);
        string toDate = endDate.ToString(_dateFormat);
        try
        {
            string url = ApiurlHelper.FetchTotalNBasedOnFieldIdAndAppDateAPI;
            if (organicManureId.HasValue)
            {
                url += $"&organicManureID={organicManureId.Value}";
            }
            url = string.Format(url, fieldId, fromdate, toDate, confirm);
            (totalN, error) = await GetTotalN(url);

        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }

    public async Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureByIdAsync(int id)
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
                if (responseWrapper?.Data is JObject data)
                {
                    organicManure = data.ToObject<OrganicManureDataViewModel>() ?? new OrganicManureDataViewModel();
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
        return (organicManure, error);
    }
    public async Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYearAsync(int farmId, int year)
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
                    organicManures = responseWrapper?.Data?.ToObject<List<OrganicManure>>() ?? new List<OrganicManure>();
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
        return (organicManures, error);
    }

    public async Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds)
    {
        Error? error = null;
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var content = new StringContent(orgManureIds, Encoding.UTF8, _applicationJson);
            var url = ApiurlHelper.DeleteOrganicManureByAPI;
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };
            var response = await httpClient.SendAsync(requestMessage);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    message = data["message"]?.Value<string>() ?? string.Empty;
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

        return (message, error);
    }
    public async Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeIdAsync(int farmId, int ManureTypeId, string ManureTypeName)
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
                isFarmManureTypeExist = (bool?)responseWrapper?.Data?["exists"] == true;
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

        return (isFarmManureTypeExist, error);
    }
    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureTypeAsync(int fertiliserId, int farmId, int harvestYear)
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }

        return (organicResponse, error);
    }

    public async Task<(List<OrganicManure>, Error)> UpdateOrganicManureAsync(string organicManureData)
    {
        Error? error = null;
        List<OrganicManure> organicManures = new List<OrganicManure>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateOrganicManureAPI, new StringContent(organicManureData, Encoding.UTF8, _applicationJson));

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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (organicManures, error);
    }

    public async Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodIDAsync(int managementPeriodID)
    {
        Error? error = null;
        decimal? totalN = null;
        try
        {
            string url = string.Format(ApiurlHelper.FetchOragnicManureAvailableNByManagementPeriodIDAPI, HttpUtility.UrlEncode(managementPeriodID.ToString()));
            (totalN, error) = await GetTotalN(url);

        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (totalN, error);
    }

    public async Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeByIdAsync(int id)
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
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (farmManureType, error);
    }
    public async Task<(string?, Error?)> FetchOrganicManureClosedPeriodAsync(OrganicClosedPeriodRequest organicClosedPeriodRequest)
    {
        string? closedPeriod = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = string.Empty;
            if (organicClosedPeriodRequest.SowingDate != null)
            {
                url = string.Format(ApiurlHelper.FetchOrganicManureClosedPeriodAPI, organicClosedPeriodRequest.SoilTypeId, organicClosedPeriodRequest.FieldType, organicClosedPeriodRequest.HarvestYear, organicClosedPeriodRequest.SowingDate, organicClosedPeriodRequest.CountryId, organicClosedPeriodRequest.CropGroupId, organicClosedPeriodRequest.CropTypeId, organicClosedPeriodRequest.IsPerennial);

            }
            else
            {
                url = string.Format(ApiurlHelper.FetchOrganicManureClosedPeriodNoSowingDateAPI, organicClosedPeriodRequest.SoilTypeId, organicClosedPeriodRequest.FieldType, organicClosedPeriodRequest.HarvestYear, organicClosedPeriodRequest.CountryId, organicClosedPeriodRequest.CropGroupId, organicClosedPeriodRequest.CropTypeId, organicClosedPeriodRequest.IsPerennial);
            }
                var response = await httpClient.GetAsync(url);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.ClosedPeriod != null)
            {
                closedPeriod = responseWrapper?.Data?.ClosedPeriod?.ToObject<string>();
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
        return (closedPeriod, error);
    }

    public async Task<(bool, Error)> FetchLivestockManureExistanceByDateRangeAsync(int cropId, string dateFrom, string dateTo, int? organicManureId)
    {
        Error? error = null;
        bool isLivestockcManureExist = false;
        try
        {
            string requestUrl = ApiurlHelper.FetchLivestockManureExistanceByDateRangeAPI;
            if (organicManureId.HasValue)
            {
                requestUrl += $"&organicManureID={organicManureId.Value}";
            }
            requestUrl = string.Format(requestUrl, cropId, dateFrom, dateTo);
            (isLivestockcManureExist, error) = await CheckExistence(requestUrl);
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (isLivestockcManureExist, error);
    }

    public async Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRangeAsync(int cropId, string dateFrom, string dateTo, int? organicManureId, bool isPoultry)
    {
        Error? error = null;
        decimal? totalRate = (decimal?)null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string requestUrl = ApiurlHelper.FetchTotalApplicationRateByDateRangeAPI;

            if (organicManureId.HasValue)
            {
                requestUrl += $"&organicManureID={organicManureId.Value}";
            }
            requestUrl += $"&isPoultry={isPoultry}";

            requestUrl = string.Format(requestUrl, cropId, dateFrom, dateTo);

            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                totalRate = Convert.ToDecimal(responseWrapper?.Data);
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
        return (totalRate, error);
    }

    public async Task<(bool, Error)> CheckGreenCompostExistanceByDateRangeAsync(int fieldId, string dateFrom, string dateTo, int? organicManureId)
    {
        Error? error = null;
        bool isLivestockcManureExist = false;
        try
        {
            string requestUrl = ApiurlHelper.CheckGreenCompostExistanceByDateRangeAPI;
            if (organicManureId.HasValue)
            {
                requestUrl += $"&organicManureID={organicManureId.Value}";
            }
            requestUrl = string.Format(requestUrl, fieldId, dateFrom, dateTo);

            (isLivestockcManureExist, error) = await CheckExistence(requestUrl);
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }
        return (isLivestockcManureExist, error);
    }

    private async Task<(bool, Error?)> CheckExistence(string requestUrl)
    {
        bool isLivestockcManureExist = false;
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(requestUrl);
        string result = await response.Content.ReadAsStringAsync();
        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            isLivestockcManureExist =
                responseWrapper?.Data?.exists?.ToObject<bool>() ?? false;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper ?? new ResponseWrapper(), error);
        }
        return (isLivestockcManureExist, error);
    }

    public async Task<(decimal, Error?)> GetTotalN(string requestUrl)
    {
        Error? error = null;
        decimal totalN = 0;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(requestUrl);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                totalN = responseWrapper?.Data?.TotalN != null ? responseWrapper?.Data?.TotalN.ToObject<decimal>() : 0;
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (totalN, error);
    }

    public async Task<(int?, Error?)> FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroupAsync(int cropTypeId, int soilTypeId, int residueGroup)
    {
        Error? error = null;
        int? scotlandNmax = 0;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            string requestUrl = string.Format(ApiurlHelper.FetchScotlandNMaxValueByCropTypeIdSoilTypeIdResidueAPI, cropTypeId, soilTypeId, residueGroup);


            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                scotlandNmax = responseWrapper?.Data != null ? responseWrapper?.Data?.ScotlandNMax : (decimal?)null;
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
        return (scotlandNmax, error);
    }
    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchAllCropTypeLinkingAsync()
    {
        List<CropTypeLinkingResponse> cropTypeLinkingList = new List<CropTypeLinkingResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropTypeLinkingsAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.CropTypeLinking is JToken cropTypeLinkingToken)
                {
                    cropTypeLinkingList =
                        cropTypeLinkingToken["records"]?.ToObject<List<CropTypeLinkingResponse>>()
                        ?? new List<CropTypeLinkingResponse>();
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
        return (cropTypeLinkingList, error);
    }


}
