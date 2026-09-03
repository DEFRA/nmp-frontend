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
public class Rb209Service(ILogger<Rb209Service> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IRb209Service
{
    private readonly ILogger<Rb209Service> _logger = logger;
    private const string _applicationJson = "application/json";
    public async Task<List<SoilTypesResponse>> FetchSoilTypesAsync()
    {
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
            }
        }
        return soilTypes;
    }
    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
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
        List<CropGroupResponse> soilTypes = new List<CropGroupResponse>();
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
                var soiltypeslist = data.ToObject<List<CropGroupResponse>>()
                    ?? new List<CropGroupResponse>();
                soilTypes.AddRange(soiltypeslist);
            }
        }
        else
        {
            _logger.ExtractError(responseWrapper, error);
        }
        return soilTypes;
    }

    public async Task<List<CropTypeResponse>> FetchCropTypesAsync(int cropGroupId)
    {
        List<CropTypeResponse> soilTypes = new List<CropTypeResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypesAPI, cropGroupId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    var soiltypeslist = data.ToObject<List<CropTypeResponse>>() ?? new List<CropTypeResponse>();
                    soilTypes.AddRange(soiltypeslist);
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

        return soilTypes;
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
        Error? error = null;
        string cropGroup = string.Empty;
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropGroupByIdAPI, cropGroupId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
            {
                cropGroup = data["cropGroupName"]?.Value<string>() ?? string.Empty;
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
        return cropGroup;
    }

    public async Task<string> FetchCropTypeByIdAsync(int cropTypeId)
    {
        Error? error = null;
        string cropType = string.Empty;
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeByIdAPI, cropTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
            {
                cropType = data["cropTypeName"]?.Value<string>() ?? string.Empty;
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
        return cropType;
    }
    public async Task<List<PotatoVarietyResponse>> FetchPotatoVarietiesAsync()
    {

        List<PotatoVarietyResponse> potatoVarieties = [];
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
        List<CropInfoOneResponse> cropInfoOneList = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropInfoOneByCropTypeIdAPI, HttpUtility.UrlEncode(cropTypeId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropInfoOneResponses = responseWrapper?.Data?.ToObject<List<CropInfoOneResponse>>();
                    cropInfoOneList.AddRange(cropInfoOneResponses);
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
        return cropInfoOneList;
    }
    public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeIdAsync()
    {
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
        string? cropInfo1Name = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAPI, HttpUtility.UrlEncode(cropTypeId.ToString()), HttpUtility.UrlEncode(cropInfo1Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                cropInfo1Name = responseWrapper?.Data["cropInfo1Name"];
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
        return cropInfo1Name ?? string.Empty;
    }

    public async Task<string> FetchCropInfo2NameByCropInfo2IdAsync(int cropInfo2Id)
    {
        string? cropInfo2Name = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfo2NameByCropInfo2IdAPI, HttpUtility.UrlEncode(cropInfo2Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                cropInfo2Name = responseWrapper?.Data["cropInfo2Name"];
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
        return cropInfo2Name ?? string.Empty;
    }

    public async Task<List<CropTypeResponse>> FetchAllCropTypesAsync()
    {
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
        Error? error = null;
        string soilType = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilTypeBySoilTypeIdAPI, soilTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
            {
                soilType = data["soilType"]?.ToString() ?? string.Empty;
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
        return soilType;
    }

    public async Task<List<SeasonResponse>> FetchSeasonsAsync()
    {
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
        List<SwardTypeResponse> swardTypeResponses = new List<SwardTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSwardTypesAPI, countryId));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var swardTypeResponseList = responseWrapper?.Data?.ToObject<List<SwardTypeResponse>>();
                    swardTypeResponses.AddRange(swardTypeResponseList);
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
        return (swardTypeResponses, error);
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
        Error? error = null;
        SwardTypeResponse? swardTypeResponse = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardTypeBySwardTypeIdAPI, HttpUtility.UrlEncode(swardTypeId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper?.Data != null)
            {
                swardTypeResponse = responseWrapper?.Data?.ToObject<SwardTypeResponse>();
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
        return (swardTypeResponse, error);
    }
    public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementIdAsync(int swardManagementId)
    {
        Error? error = null;
        SwardManagementResponse? swardManagementResponse = new SwardManagementResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardManagementBySwardManagementIdAPI, HttpUtility.UrlEncode(swardManagementId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            var data = responseWrapper?.Data;
            if (data != null)
            {
                swardManagementResponse = data.ToObject<SwardManagementResponse>();
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
}

