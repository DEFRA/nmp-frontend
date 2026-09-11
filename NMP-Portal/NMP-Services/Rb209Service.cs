using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class Rb209Service(ILogger<Rb209Service> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : CacheableService(httpContextAccessor, clientFactory, tokenRefreshService, redisCacheService, memoryCacheService), IRb209Service
{
    private readonly ILogger<Rb209Service> _logger = logger;
    private const string _applicationJson = "application/json";
    public async Task<List<SoilTypesResponse>> FetchSoilTypesAsync()
    {
        string cacheKey = "FetchSoilTypesAsync";
        var cached = await GetFromCacheAsync<List<SoilTypesResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchSoilTypesAPI);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data is JToken data)
            {
                var soiltypeslist = data.ToObject<List<SoilTypesResponse>>() ?? new List<SoilTypesResponse>();
                soilTypes.AddRange(soiltypeslist);
                await SetCacheAsync(cacheKey, soilTypes);

            }
        }
        return soilTypes;
    }
    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
        string cacheKey = "FetchNutrientsAsync";
        var cached = await GetFromCacheAsync<List<NutrientResponseWrapper>>(cacheKey);
        if (cached != null)
        {
            return (cached, null);
        }

        List<NutrientResponseWrapper> nutrients = new List<NutrientResponseWrapper>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchNutrientsAPI);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data is JToken data)
            {
                var nutrientResponseWrapper = data.ToObject<List<NutrientResponseWrapper>>() ?? new List<NutrientResponseWrapper>();
                nutrients.AddRange(nutrientResponseWrapper);
                await SetCacheAsync(cacheKey, nutrients);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (nutrients, error);
    }

    public async Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, decimal? nutrientValue, int methodologyId, int countryId)
    {
        Error? error = null;
        string nutrientIndex = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilNutrientIndexAPI, HttpUtility.UrlEncode(nutrientId.ToString()), HttpUtility.UrlEncode(nutrientValue.ToString()), HttpUtility.UrlEncode(methodologyId.ToString()), HttpUtility.UrlEncode(countryId.ToString()));
        var response = await httpClient.GetAsync(requestUrl);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (responseWrapper?.Data is JObject data)
        {
            nutrientIndex = data["index"]?.Value<string>() ?? string.Empty;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (nutrientIndex, error);
    }

    public async Task<List<CropGroupResponse>> FetchCropGroupsAsync()
    {
        string cacheKey = "FetchCropGroupsAsync";
        var cached = await GetFromCacheAsync<List<CropGroupResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
        List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();
        Error error = new Error();
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchCropGroupsAPI);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        var wrapper = responseWrapper;
        if (response.IsSuccessStatusCode)
        {
            if (wrapper?.Data is JToken data)
            {
                var cropGroupsList = data.ToObject<List<CropGroupResponse>>()
                    ?? new List<CropGroupResponse>();
                cropGroups.AddRange(cropGroupsList);
                await SetCacheAsync(cacheKey, cropGroups);

            }
        }
        else
        {
            _logger.ExtractError(responseWrapper, error);
        }
        return cropGroups;
    }

    public async Task<List<CropTypeResponse>> FetchCropTypesAsync(int cropGroupId)
    {
        List<CropTypeResponse> allCropTypes = await FetchAllCropTypesAsync();
        if (allCropTypes != null && allCropTypes.Count > 0)
        {
            return allCropTypes.Where(c => c.CropGroupId == cropGroupId).ToList();
        }
        return new List<CropTypeResponse>();
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        string soilType = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilTypeByIdAPI, HttpUtility.UrlEncode(soilTypeId.ToString()));
        var response = await httpClient.GetAsync(requestUrl);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (responseWrapper?.Data is JObject data)
        {
            soilType = data["soilType"]?.Value<string>() ?? string.Empty;
        }

        return soilType;
    }

    public async Task<string> FetchCropGroupByIdAsync(int cropGroupId)
    {
        List<CropGroupResponse> cropGroupList= await FetchCropGroupsAsync();
        if (cropGroupList != null && cropGroupList.Count > 0)
        {
            return cropGroupList.Where(c => c.CropGroupId == cropGroupId).Select(c => c.CropGroupName).FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;

    }

    public async Task<string> FetchCropTypeByIdAsync(int cropTypeId)
    {
        List<CropTypeResponse> allCropTypes = await FetchAllCropTypesAsync();
        if (allCropTypes != null && allCropTypes.Count > 0)
        {
            return allCropTypes.Where(c => c.CropTypeId == cropTypeId).Select(c => c.CropType).FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;
    }
    public async Task<List<PotatoVarietyResponse>> FetchPotatoVarietiesAsync()
    {
        string cacheKey = "FetchPotatoVarietiesAsync";
        var cached = await GetFromCacheAsync<List<PotatoVarietyResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
        List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchPotatoVarietiesAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var potatoVarietyList = responseWrapper?.Data?.ToObject<List<PotatoVarietyResponse>>();
                    potatoVarieties.AddRange(potatoVarietyList);
                    await SetCacheAsync(cacheKey, potatoVarieties);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, null);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, null);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, null);
        }
        return potatoVarieties;
    }

    public async Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeIdAsync(int cropTypeId)
    {

        List<CropInfoOneResponse> allCropInfoOnes = await FetchCropInfoOneListAsync();
        if (allCropInfoOnes != null && allCropInfoOnes.Count > 0)
        {
            return allCropInfoOnes.Where(c => c.CropTypeId == cropTypeId).ToList();
        }
        return new List<CropInfoOneResponse>();
    }
    public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoListAsync()
    {
        string cacheKey = "FetchCropInfoTwoListAsync";
        var cached = await GetFromCacheAsync<List<CropInfoTwoResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<CropInfoTwoResponse> cropInfoTwoList = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropInfoTwoByCropTypeIdAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropInfoTwoResponses = responseWrapper?.Data?.ToObject<List<CropInfoTwoResponse>>();
                    cropInfoTwoList.AddRange(cropInfoTwoResponses);
                    await SetCacheAsync(cacheKey, cropInfoTwoList);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, null);
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
        return cropInfoTwoList;
    }

    public async Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAsync(int cropTypeId, int cropInfo1Id)
    {
        List<CropInfoOneResponse> allCropInfoOnes = await FetchCropInfoOneListAsync();
        if (allCropInfoOnes != null && allCropInfoOnes.Count > 0)
        {
            return allCropInfoOnes.Where(c => c.CropTypeId == cropTypeId && c.CropInfo1Id == cropInfo1Id).Select(c => c.CropInfo1Name).FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;
    }

    public async Task<string> FetchCropInfo2NameByCropInfo2IdAsync(int cropInfo2Id)
    {
        List<CropInfoTwoResponse> allCropInfoTwos = await FetchCropInfoTwoListAsync();
        if (allCropInfoTwos != null && allCropInfoTwos.Count > 0)
        {
            return allCropInfoTwos.Where(c => c.CropInfo2Id == cropInfo2Id).Select(c => c.CropInfo2).FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;
    }

    public async Task<List<CropTypeResponse>> FetchAllCropTypesAsync()
    {
        string cacheKey = "FetchAllCropTypesAsync";
        var cached = await GetFromCacheAsync<List<CropTypeResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchAllCropTypeAPI);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropTypesList = responseWrapper?.Data?.ToObject<List<CropTypeResponse>>();
                    cropTypes.AddRange(cropTypesList);
                    await SetCacheAsync(cacheKey, cropTypes);

                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return cropTypes;
    }
    public async Task<string> FetchSoilTypeByIdAsync(int soilTypeId)
    {
        //call all soiltype list method and filter by soilTypeId to avoid multiple api calls for each soilTypeId
        List<SoilTypesResponse> allSoilTypes = await FetchSoilTypesAsync();
        if (allSoilTypes != null && allSoilTypes.Count > 0)
        {
            return allSoilTypes.Where(c => c.SoilTypeId == soilTypeId).Select(c => c.SoilType).FirstOrDefault() ?? string.Empty;
        }
        return string.Empty;
        
    }

    public async Task<List<SeasonResponse>> FetchSeasonsAsync()
    {
        string cacheKey = "FetchSeasonsAsync";
        var cached = await GetFromCacheAsync<List<SeasonResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<SeasonResponse> seasons = new List<SeasonResponse>();
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSeasonsAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var seasonlist = responseWrapper?.Data?.ToObject<List<SeasonResponse>>();
                    seasons.AddRange(seasonlist);
                    await SetCacheAsync(cacheKey, seasons);

                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return seasons;
    }

    public async Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData)
    {
        string jsonData = JsonConvert.SerializeObject(measurementData);
        SnsResponse snsResponse = new SnsResponse();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.FetchSNSIndexByMeasurementMethodAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject farmDataJObject)
            {
                snsResponse = farmDataJObject.ToObject<SnsResponse>() ?? new SnsResponse();
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
            }

        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (snsResponse, error);
    }
    public async Task<(SnsResponseForScotland, Error)> FetchSNSIndexByMeasurementMethodForScotlandAsync(MeasurementDataForScotland measurementDataForScotland)
    {
        string jsonData = JsonConvert.SerializeObject(measurementDataForScotland);
        SnsResponseForScotland snsResponse = new SnsResponseForScotland();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.FetchSNSIndexByMeasurementMethodForScotlandAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject farmDataJObject)
            {
                snsResponse = farmDataJObject.ToObject<SnsResponseForScotland>() ?? new SnsResponseForScotland();
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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
        return (snsResponse, error);
    }
    public async Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId)
    {
        Error? error = null;
        List<SoilNutrientStatusResponse>? statusList = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilNutrientStatusListAPI, HttpUtility.UrlEncode(HttpUtility.UrlEncode(methodologyId.ToString())));
        var response = await httpClient.GetAsync(requestUrl);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data != null)
            {
                List<SoilNutrientStatusResponse>? soilNutrientIndiceResponse = responseWrapper?.Data?.ToObject<List<SoilNutrientStatusResponse>>();

                if (soilNutrientIndiceResponse != null)
                {
                    statusList = soilNutrientIndiceResponse;
                }
            }

        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (statusList, error);
    }

    public async Task<(List<SoilMethologiesResponse>?, Error?)> FetchSoilMethodologies(int nutrientId, int countryId)
    {
        List<SoilMethologiesResponse>? soilMethodologyList = null;
        Error? error = null;

        _logger.LogTrace("Soil Service: soil-analyses-methods called.");
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilMethodologiesByNutrientAndCountryIdAPI, nutrientId, countryId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                soilMethodologyList = responseWrapper?.Data?.ToObject<List<SoilMethologiesResponse>>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (soilMethodologyList, error);
    }

    public async Task<(SoilMethologiesResponse?, Error?)> FetchSoilMethodologyNameByNutrientIdAndMethodologyId(int nutrientId, int methodologyId)
    {
        SoilMethologiesResponse? soilAnalysesMethod = null;
        Error? error = null;

        _logger.LogTrace("Soil Service: soil-analyses-methods called.");
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchAllSoilMethodologyNameAPI, nutrientId, methodologyId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                soilAnalysesMethod = responseWrapper?.Data?.ToObject<SoilMethologiesResponse>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (soilAnalysesMethod, error);
    }
    public async Task<List<GrassSeasonResponse>> FetchGrassSeasonsAsync()
    {
        List<GrassSeasonResponse> grassSeasons = new List<GrassSeasonResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchGrassSeasonsAPI, 3);//3 is country id
            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var grassSeasonsList = responseWrapper?.Data?.ToObject<List<GrassSeasonResponse>>();
                    grassSeasons.AddRange(grassSeasonsList);
                }
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
        return grassSeasons;
    }
    public async Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCutAsync(int swardTypeId, int swardManagementId, int numberOfCut, bool isNewSward, int countryId)
    {
        Error? error = null;
        List<DefoliationSequenceResponse> defoliationSequenceResponses = new List<DefoliationSequenceResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchDefoliationSequencesBySwardTypeIdAndNumberOfCutAPI, HttpUtility.UrlEncode(swardTypeId.ToString()), HttpUtility.UrlEncode(swardManagementId.ToString()), HttpUtility.UrlEncode(numberOfCut.ToString()), HttpUtility.UrlEncode(isNewSward.ToString()), countryId);
            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                var defoliationSequenceList = responseWrapper?.Data?.ToObject<List<DefoliationSequenceResponse>>();
                defoliationSequenceResponses.AddRange(defoliationSequenceList);
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
        return (defoliationSequenceResponses, error);
    }

    public async Task<(List<PotentialCutResponse>, Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementIdAsync(int swardTypeId, int swardManagementId)
    {
        Error? error = null;
        List<PotentialCutResponse> potentialCuts = new List<PotentialCutResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchPotentialCutsBySwardTypeIdAndSwardManagementIdAPI, HttpUtility.UrlEncode(swardTypeId.ToString()), HttpUtility.UrlEncode(swardManagementId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                var potentialCutList = responseWrapper?.Data?.ToObject<List<PotentialCutResponse>>();
                potentialCuts.AddRange(potentialCutList);
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
        return (potentialCuts, error);
    }

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementsAsync()
    {
        string cacheKey = "FetchSwardManagementsAsync";
        var cached = await GetFromCacheAsync<List<SwardManagementResponse>>(cacheKey);
        if (cached != null)
        {
            return (cached, null);
        }
        List<SwardManagementResponse> swardManagementResponses = new List<SwardManagementResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSwardManagementsAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var swardManagementList = responseWrapper?.Data?.ToObject<List<SwardManagementResponse>>();
                    swardManagementResponses.AddRange(swardManagementList);
                    await SetCacheAsync(cacheKey, swardManagementResponses);
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
        return (swardManagementResponses, error);
    }
    public async Task<(List<SwardTypeResponse>, Error)> FetchSwardTypesServiceByCountryAsync(int countryId)
    {
        List<SwardTypeResponse> swardTypesList =await FetchSwardTypesListAsync();

        if(swardTypesList.Count > 0)
        {
            return (swardTypesList.Where(c => c.CountryId == countryId).ToList(), null);
        }
        return (new List<SwardTypeResponse>(), new Error());

    }
    public async Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdAsync(int sequenceId, int grassGrowthClassId)
    {
        Error? error = null;
        List<YieldRangesEnglandAndWalesResponse> yieldRanges = new List<YieldRangesEnglandAndWalesResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdAPI, HttpUtility.UrlEncode(sequenceId.ToString()), HttpUtility.UrlEncode(grassGrowthClassId.ToString()));
        var response = await httpClient.GetAsync(requestUrl);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var yieldRangesList = responseWrapper?.Data?.ToObject<List<YieldRangesEnglandAndWalesResponse>>();
            yieldRanges.AddRange(yieldRangesList);
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (yieldRanges, error);
    }
    public async Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesByIdAsync(int defoliationId)
    {
        Error? error = null;
        DefoliationSequenceResponse? defoliationSequenceResponse = new DefoliationSequenceResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchDefoliationSequencesByIdAPI, HttpUtility.UrlEncode(defoliationId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper?.Data != null)
            {
                defoliationSequenceResponse = responseWrapper?.Data?.ToObject<DefoliationSequenceResponse>();
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
        return (defoliationSequenceResponse, error);
    }
    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeIdAsync(int swardTypeId)
    {
        Error? error = null;
        List<SwardManagementResponse>? swardManagementResponse = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardManagementBySwardTypeIdAPI, HttpUtility.UrlEncode(swardTypeId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper?.Data != null)
            {
                swardManagementResponse = responseWrapper?.Data?.ToObject<List<SwardManagementResponse>>();
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
        return (swardManagementResponse, error);
    }
    public async Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeIdAsync(int swardTypeId)
    {
        List<SwardTypeResponse> swardTypeList = await FetchSwardTypesListAsync();

        if (swardTypeList.Count > 0)
        {
            return (swardTypeList.FirstOrDefault(c => c.SwardTypeId == swardTypeId), null);
        }
        return (new SwardTypeResponse(), new Error());
        
    }
    public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementIdAsync(int swardManagementId)
    {
        List<SwardManagementResponse> swardManagementList = await FetchSwardManagementListAsync();

        if (swardManagementList.Count > 0)
        {
            return (swardManagementList.FirstOrDefault(c => c.SwardManagementId == swardManagementId), null);
        }
        return (new SwardManagementResponse(), new Error());
    }
    public async Task<List<NvzActionProgramResponse>> FetchNvzActionProgramsByCountryIdAsync(int countryId)
    {
        List<NvzActionProgramResponse> nvzActionProgramResponses = new List<NvzActionProgramResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchNvzActionProgramsByCountryIdAPI, countryId);
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
    public async Task<List<CropInfoOneResponse>> FetchCropInfoOneListAsync()
    {
        string cacheKey = "FetchCropInfoOneListAsync";
        var cached = await GetFromCacheAsync<List<CropInfoOneResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<CropInfoOneResponse> cropInfoOneList = new List<CropInfoOneResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropInfoOneListAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropInfoOneListFromResponse = responseWrapper?.Data?.ToObject<List<CropInfoOneResponse>>();
                    cropInfoOneList.AddRange(cropInfoOneListFromResponse);
                    await SetCacheAsync(cacheKey, cropInfoOneList);

                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return cropInfoOneList;
    }
    public async Task<List<SwardTypeResponse>> FetchSwardTypesListAsync()
    {
        string cacheKey = "FetchSwardTypesListAsync";
        var cached = await GetFromCacheAsync<List<SwardTypeResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<SwardTypeResponse> swardTypeList = new List<SwardTypeResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSwardTypesListAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var swardTypeListFromResponse = responseWrapper?.Data?.ToObject<List<SwardTypeResponse>>();
                    swardTypeList.AddRange(swardTypeListFromResponse);
                    await SetCacheAsync(cacheKey, swardTypeList);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return swardTypeList;
    }

    public async Task<List<SwardManagementResponse>> FetchSwardManagementListAsync()
    {
        string cacheKey = "FetchSwardManagementListAsync";
        var cached = await GetFromCacheAsync<List<SwardManagementResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        List<SwardManagementResponse> swardManagementList = new List<SwardManagementResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSwardTypesListAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var swardManagementListFromResponse = responseWrapper?.Data?.ToObject<List<SwardManagementResponse>>();
                    swardManagementList.AddRange(swardManagementListFromResponse);
                    await SetCacheAsync(cacheKey, swardManagementList);

                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return swardManagementList;
    }
}

