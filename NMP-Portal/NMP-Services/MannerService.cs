using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
public class MannerService(ILogger<MannerService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IMannerService
{
    private readonly ILogger<MannerService> _logger = logger;
    private List<ManureType>? _manureTypeList = null;

    private readonly Dictionary<int, int> cropTypeToCategoryId = new Dictionary<int, int>
    {
        { 0, 2 },
        { 1, 2 },
        { 2, 6 },
        { 3, 6 },
        { 4, 2 },
        { 5, 6 },
        { 6, 2 },
        { 7, 6 },
        { 8, 2 },
        { 9, 6 },
        { 171, 6 },
        { 172, 6 },
        { 173, 6 },
        { 174, 6 },
        { 20, 4 },
        { 21, 6 },
        { 22, 9 },
        { 23, 9 },
        { 24, 9 },
        { 25, 9 },
        { 26, 8 },
        { 28, 9 },
        { 175, 9 },
        { 176, 9 },
        { 187, 9 },
        { 27, 9 },
        { 40, 9 },
        { 41, 9 },
        { 43, 9 },
        { 44, 9 },
        { 45, 9 },
        { 50, 6 },
        { 51, 6 },
        { 52, 2 },
        { 53, 2 },
        { 54, 6 },
        { 55, 6 },
        { 56, 6 },
        { 57, 2 },
        { 58, 2 },
        { 59, 2 },
        { 188, 9 },
        { 189, 9 },
        { 191, 9 },
        { 194, 9 },
        { 195, 9 },
        { 60, 9 },
        { 61, 9 },
        { 62, 9 },
        { 63, 9 },
        { 64, 9 },
        { 65, 9 },
        { 66, 9 },
        { 67, 9 },
        { 68, 9 },
        { 69, 9 },
        { 70, 9 },
        { 71, 9 },
        { 72, 9 },
        { 73, 9 },
        { 74, 9 },
        { 75, 9 },
        { 77, 9 },
        { 78, 9 },
        { 79, 9 },
        { 181, 9 },
        { 90, 8 },
        { 91, 9 },
        { 92, 9 },
        { 93, 9 },
        { 94, 9 },
        { 182, 9 },
        { 110, 9 },
        { 111, 9 },
        { 112, 9 },
        { 113, 9 },
        { 114, 9 },
        { 115, 9 },
        { 116, 9 },
        { 117, 9 },
        { 118, 9 },
        { 119, 9 },
        { 120, 9 },
        { 121, 9 },
        { 122, 9 },
        { 123, 9 },
        { 124, 9 },
        { 125, 9 },
        { 177, 9 },
        { 178, 9 },
        { 140, 1 },
        { 160, 7 },
        { 161, 7 },
        { 162, 7 },
        { 163, 7 },
        { 170, 9 },
        { 184, 9 },
        { 185, 9 },
        { 192, 9 },
        { 193, 9 },
        { 76, 9 },
        { 179, 9 },
        { 180, 9 }
    };
    public async Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId)
    {
        if (cropTypeToCategoryId.TryGetValue(cropTypeId, out int categoryId))
        {
            return categoryId;
        }
        else
        {
            return 0;
        }
    }

    public async Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId)
    {
        _logger.LogTrace("MannerService: FetchCropNUptakeDefaultAsync called for CropCategoryId: {CropCategoryId}", cropCategoryId);
        int cropUptakeFactor;

        switch (cropCategoryId)
        {
            case (int)NMP.Commons.Enums.CropCategory.Grass:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Grass;
                break;
            case (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.EarlySownWinterCereal;
                break;
            case (int)NMP.Commons.Enums.CropCategory.LateSownWinterCereal:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.LateSownWinterCereal;
                break;
            case (int)NMP.Commons.Enums.CropCategory.EarlyStablishedWinterOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.EarlyStablishedWinterOilseedRape;
                break;
            case (int)NMP.Commons.Enums.CropCategory.LateStablishedWinterOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.LateStablishedWinterOilseedRape;
                break;
            case (int)NMP.Commons.Enums.CropCategory.Other:
            case (int)NMP.Commons.Enums.CropCategory.Potatoes:
            case (int)NMP.Commons.Enums.CropCategory.Sugerbeet:
            case (int)NMP.Commons.Enums.CropCategory.SpringCerealOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Other;
                break;
            default:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Other;
                break;
        }

        return cropUptakeFactor;
    }

    public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
    {
        decimal rainfallAverage = 0;
        string url = string.Format(ApiurlHelper.FetchMannerRainfallAverageAPI, firstHalfPostcode);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            rainfallAverage = responseWrapper?.Data?.avarageAnnualRainfall == null ? 0 : responseWrapper?.Data?.avarageAnnualRainfall.value;
        }

        return rainfallAverage;
    }



    public async Task<Country?> FetchCountryById(int id)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCountryByIdAPI, id));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper =
            JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (responseWrapper?.Data?.records is { } records)
        {
            return records.ToObject<Country>();
        }


        return null;
    }

    public async Task<(List<CommonResponse>, Error?)> FetchManureGroupList()
    {
        List<CommonResponse> manureGroupList = new List<CommonResponse>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerManureGroupListAPI);

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

    private async Task PopulateManureTypeList()
    {
        List<ManureType> manureTypeList = new List<ManureType>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(ApiurlHelper.FetchMannerManureTypesAPI);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var manureTypes = responseWrapper?.Data?.ToObject<List<ManureType>>();
            manureTypeList.AddRange(manureTypes);
            _manureTypeList = manureTypeList;
        }
        else
        {
            _logger.ExtractError(responseWrapper, error);
        }
    }

    public async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId)
    {
        if(_manureTypeList == null || !_manureTypeList.Any())
        {
            await PopulateManureTypeList();
        }        
        return (_manureTypeList.Where(m=>m.ManureGroupId == manureGroupId && (m.CountryId == countryId || m.CountryId == 3)).ToList(), null);
    }
    public async Task<(CommonResponse?, Error?)> FetchManureGroupById(int manureGroupId)
    {
        CommonResponse? manureGroup = new CommonResponse();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerManureGroupByIdAPI, HttpUtility.UrlEncode(manureGroupId.ToString())));

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
        if (_manureTypeList == null || !_manureTypeList.Any())
        {
            await PopulateManureTypeList();
        }

        return (_manureTypeList?.FirstOrDefault(m => m.Id == manureTypeId), null);
    }

    public async Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid)
    {
        List<ApplicationMethodResponse> applicationMethodList = new List<ApplicationMethodResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerApplicationMethodsByApplicableForAPI, HttpUtility.UrlEncode(isLiquid.ToString()), HttpUtility.UrlEncode(fieldType.ToString())));
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
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationMethodsByApplicationIdAPI, HttpUtility.UrlEncode(appId.ToString()), applicableFor));
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

    public async Task<(List<IncorprationDelaysResponse>?, Error?)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
    {
        List<IncorprationDelaysResponse> incorporationDelays = new List<IncorprationDelaysResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationDelaysByMethodIdAndApplicableForAPI, HttpUtility.UrlEncode(methodId.ToString()), HttpUtility.UrlEncode(applicableFor)));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var delays = responseWrapper?.Data?.ToObject<List<IncorprationDelaysResponse>>();
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
    public async Task<(string?, Error?)> FetchApplicationMethodById(int Id)
    {
        string? applicationMethod = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerApplicationMethodByIdAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    applicationMethod = responseWrapper?.Data?.name;

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
    public async Task<(string?, Error?)> FetchIncorporationMethodById(int Id)
    {
        string? incorporationMethod = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationMethodByIdAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    incorporationMethod = responseWrapper?.Data?.name;

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
    public async Task<(string?, Error?)> FetchIncorporationDelayById(int Id)
    {
        string? incorporationDelay = string.Empty;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerIncorporationDelaysByIdAPI, HttpUtility.UrlEncode(Id.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    incorporationDelay = responseWrapper?.Data?.name;

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
    public async Task<(List<CommonResponse>?, Error?)> FetchTopsoilList()
    {
        Error? error = null;
        List<CommonResponse>? topSoilList = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = ApiurlHelper.FetchAllMannerTopSoilListAPI;
        var response = await httpClient.GetAsync(requestUrl);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data != null)
            {
                topSoilList = responseWrapper?.Data?.ToObject<List<CommonResponse>>();

            }

        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (topSoilList, error);
    }
    public async Task<(List<CommonResponse>?, Error?)> FetchSubsoilList()
    {
        Error? error = null;
        List<CommonResponse>? subSoilList = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = ApiurlHelper.FetchAllMannerSubSoilListAPI;
        var response = await httpClient.GetAsync(requestUrl);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data != null)
            {
                subSoilList = responseWrapper?.Data?.ToObject<List<CommonResponse>>();

            }

        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (subSoilList, error);
    }
    public async Task<(ManureNutrientResponse?, Error?)> CalculateDefaultNutrientValueBasedOnDryMatter(ManureNutrientResponse manureNutrientResponse)
    {
        Error? error = null;
        ManureNutrientResponse? manureNutrientResponseResult = null;
        HttpClient httpClient = await GetNMPAPIClient();
        string jsonData = JsonConvert.SerializeObject(manureNutrientResponse);
        var requestUrl = ApiurlHelper.CalculateNutrientValueBasedOnDryMatterAPI;
        var response = await httpClient.PostAsync(requestUrl, new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json"));
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data != null)
            {
                manureNutrientResponseResult = responseWrapper?.Data?.ToObject<ManureNutrientResponse>();

            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (manureNutrientResponseResult, error);
    }
}
