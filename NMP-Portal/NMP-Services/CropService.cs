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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Commons.ServiceResponses.Error;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class CropService(ILogger<CropService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), ICropService
{
    private readonly ILogger<CropService> _logger = logger;

    public async Task<List<PotatoVarietyResponse>> FetchPotatoVarieties()
    {

        List<PotatoVarietyResponse> potatoVarieties = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchPotatoVarietiesAsyncAPI);
            
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
    public async Task<int> FetchCropTypeByGroupId(int cropGroupId)
    {
        int cropTypeId = 0;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropTypesAsyncAPI, HttpUtility.UrlEncode(cropGroupId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropTypeResponse = responseWrapper?.Data?.ToObject<List<CropTypeResponse>>();
                    if (cropTypeResponse != null)
                    {
                        cropTypeId = cropTypeResponse[0].CropTypeId;
                    }
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
        return cropTypeId;
    }
    public async Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId)
    {
        List<CropInfoOneResponse> cropInfoOneList = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropInfoOneByCropTypeIdAsyncAPI, HttpUtility.UrlEncode(cropTypeId.ToString()));
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
    public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeId()
    {
        List<CropInfoTwoResponse> cropInfoTwoList = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropInfoTwoByCropTypeIdAsyncAPI);
            
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
    public async Task<(bool, Error?)> AddCropNutrientManagementPlan(CropDataWrapper cropData)
    {
        string jsonData = JsonConvert.SerializeObject(cropData);
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddCropNutrientManagementPlanAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                var cropResponsss = responseWrapper?.Data?.Recommendations;
                if (cropResponsss != null)
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

    public async Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId, int type)
    {
        List<PlanSummaryResponse> planSummaryList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchPlanSummaryByFarmIdAsyncAPI, HttpUtility.UrlEncode(farmId.ToString()), HttpUtility.UrlEncode(type.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var planSummaryResponses = responseWrapper?.Data?.ToObject<List<PlanSummaryResponse>>();
                    planSummaryList.AddRange(planSummaryResponses);
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
        return planSummaryList;
    }

    public async Task<(List<HarvestYearPlanResponse>, Error?)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId)
    {
        List<HarvestYearPlanResponse> harvestYearPlanList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchHarvestYearPlansByFarmIdAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var harvestYearPlanResponses = responseWrapper?.Data?.ToObject<List<HarvestYearPlanResponse>>();
                    harvestYearPlanList.AddRange(harvestYearPlanResponses);
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
        return (harvestYearPlanList, error);
    }
    public async Task<(List<RecommendationHeader>, Error?)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear)
    {
        List<RecommendationHeader> recommendationList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchRecommendationByFieldIdAndYearAsyncAPI, HttpUtility.UrlEncode(fieldId.ToString()), HttpUtility.UrlEncode(harvestYear.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var recommendationsList = responseWrapper?.Data?.Recommendations.ToObject<List<RecommendationHeader>>();
                    if(recommendationsList != null)
                    {
                        recommendationList.AddRange(recommendationsList);
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
        return (recommendationList, error);
    }

    public async Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(int cropTypeId, int cropInfo1Id)
    {        
        string? cropInfo1Name = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAsyncAPI, HttpUtility.UrlEncode(cropTypeId.ToString()), HttpUtility.UrlEncode(cropInfo1Id.ToString())));
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
        return cropInfo1Name?? string.Empty;
    }
    public async Task<string> FetchCropInfo2NameByCropInfo2Id(int cropInfo2Id)
    {        
        string? cropInfo2Name = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfo2NameByCropInfo2IdAsyncAPI, HttpUtility.UrlEncode(cropInfo2Id.ToString())));
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
        return cropInfo2Name?? string.Empty;
    }
    public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
    {
        List<Crop> cropList = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropsByFieldIdAsyncAPI, HttpUtility.UrlEncode(fieldId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var crops = responseWrapper?.Data?.Crops.records.ToObject<List<Crop>>();
                    cropList.AddRange(crops);
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
        return cropList;
    }

    public async Task<decimal> FetchCropTypeDefaultYieldByCropTypeId(int cropTypeId, bool isScotland)
    {
        decimal? defaultYield = 0;
        Error? error = null;
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
                    CropTypeLinkingResponse cropTypeLinkingResponse = responseWrapper.Data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();

                    defaultYield = isScotland ? cropTypeLinkingResponse.DefaultYieldScotland : cropTypeLinkingResponse.DefaultYield;
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
        return defaultYield ?? 0;
    }

    public async Task<List<int>> FetchSecondCropListByFirstCropId(int firstCropTypeId, int rb209CountryId)
    {
        List<int> secondCropList = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSecondCropListByFirstCropIdAsyncAPI, HttpUtility.UrlEncode(firstCropTypeId.ToString()), rb209CountryId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var secondCrops = responseWrapper?.Data?.SecondCropID.ToObject<List<int>>();
                    secondCropList.AddRange(secondCrops);
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
        return secondCropList;
    }
    public async Task<(HarvestYearResponseHeader?, Error?)> FetchHarvestYearPlansDetailsByFarmId(int harvestYear, int farmId)
    {
        HarvestYearResponseHeader? harvestYearPlan = new();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropsOrganicinorganicdetailsByYearFarmIdAsyncAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    harvestYearPlan = responseWrapper?.Data?.ToObject<HarvestYearResponseHeader>();
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
        return (harvestYearPlan, error);
    }

    public async Task<string?> FetchCropInfoOneQuestionByCropTypeId(int cropTypeId, int countryId)
    {
        string? cropInfoOneQuestion = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfoOneQuestionByCropTypeIdAsyncAPI, HttpUtility.UrlEncode(cropTypeId.ToString()),countryId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    cropInfoOneQuestion = responseWrapper?.Data?.CropTypeQuestion.ToObject<string>();
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
        return cropInfoOneQuestion;
    }
    public async Task<(ManagementPeriod?, Error?)> FetchManagementperiodById(int id)
    {
        ManagementPeriod? managementPeriod = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchManagementperiodByIdAsyncAPI, HttpUtility.UrlEncode(id.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    managementPeriod = responseWrapper?.Data?.ManagementPeriods.ToObject<ManagementPeriod>();
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
        return (managementPeriod, error);
    }
    public async Task<(Crop?, Error?)> FetchCropById(int id)
    {
        Crop? crop = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropByIdAsyncAPI, HttpUtility.UrlEncode(id.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    crop = responseWrapper?.Data?.ToObject<Crop>();
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

        return (crop, error);
    }
    public async Task<(string, Error?)> RemoveCropPlan(List<int> cropIds)
    {
        var cropIdsRequest = new { cropIds };
        Error? error = null;
        string? message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var jsonContent = JsonConvert.SerializeObject(cropIdsRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = string.Format(ApiurlHelper.DeleteCropPlanByIdsAPI, "");
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };

            var response = await httpClient.SendAsync(requestMessage);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                message = responseWrapper?.Data["message"].Value;
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
        return (message?? string.Empty, error);
    }
    public async Task<(bool, Error?)> IsCropsGroupNameExistForUpdate(string cropIds, string cropGroupName, int year, int farmId)
    {
        bool isCropsGroupNameExist = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropGroupNameByCropIdGroupNameAndYearAPI, HttpUtility.UrlEncode(cropIds), HttpUtility.UrlEncode(cropGroupName), HttpUtility.UrlEncode(year.ToString()), HttpUtility.UrlEncode(farmId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data == true)
            {
                isCropsGroupNameExist = true;
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

        return (isCropsGroupNameExist, error);
    }
    public async Task<(List<Crop>, Error)> UpdateCrop(string cropData)
    {
        List<Crop> crops = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateCropAPI, new StringContent(cropData, Encoding.UTF8, "application/json"));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                var cropResponse = responseWrapper?.Data?.updatedCrops.ToObject<List<Crop>>();
                if (cropResponse != null)
                {
                    crops = cropResponse;
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
        return (crops, error);
    }

    public async Task<List<GrassSeasonResponse>> FetchGrassSeasons()
    {
        List<GrassSeasonResponse> grassSeasons = new List<GrassSeasonResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchGrassSeasonsAsyncAPI, 3);//3 is country id
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

    public async Task<(List<GrassGrowthClassResponse>, Error?)> FetchGrassGrowthClass(List<int> fieldIds)
    {
        var fieldIdsRequest = new { fieldIds };
        Error? error = null;        
        List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var jsonContent = JsonConvert.SerializeObject(fieldIdsRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = ApiurlHelper.FetchGrassGrowthClassesAsyncAPI;
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            var response = await httpClient.SendAsync(requestMessage);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var grassGrowthClassList = responseWrapper?.Data?.ToObject<List<GrassGrowthClassResponse>>();
                    grassGrowthClasses.AddRange(grassGrowthClassList);
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
        return (grassGrowthClasses, error);
    }

    public async Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropId(int cropId, bool isShortSummary)
    {
        List<ManagementPeriod>? managementPeriodList = new List<ManagementPeriod>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchManagementPeriodByCropIdAsyncAPI, HttpUtility.UrlEncode(cropId.ToString()), HttpUtility.UrlEncode(isShortSummary.ToString()));
            var response = await httpClient.GetAsync(requestUrl);            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {                
                managementPeriodList.AddRange(responseWrapper?.Data?.ManagementPeriods?.ToObject<List<ManagementPeriod>>());               
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
        return (managementPeriodList, error);
    }

    //grass
    public async Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(int swardTypeId, int swardManagementId, int numberOfCut, bool isNewSward)
    {
        Error? error = null;
        List<DefoliationSequenceResponse> defoliationSequenceResponses = new List<DefoliationSequenceResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchDefoliationSequencesBySwardTypeIdAndNumberOfCutAsyncAPI, HttpUtility.UrlEncode(swardTypeId.ToString()), HttpUtility.UrlEncode(swardManagementId.ToString()), HttpUtility.UrlEncode(numberOfCut.ToString()), HttpUtility.UrlEncode(isNewSward.ToString()));
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

    public async Task<(List<PotentialCutResponse>, Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementId(int swardTypeId, int swardManagementId)
    {
        Error? error = null;
        List<PotentialCutResponse> potentialCuts = new List<PotentialCutResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchPotentialCutsBySwardTypeIdAndSwardManagementIdAsyncAPI, HttpUtility.UrlEncode(swardTypeId.ToString()), HttpUtility.UrlEncode(swardManagementId.ToString()));
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

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagements()
    {
        List<SwardManagementResponse> swardManagementResponses = new List<SwardManagementResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSwardManagementsAsyncAPI);
            
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

    public async Task<(List<SwardTypeResponse>, Error)> FetchSwardTypes()
    {
        List<SwardTypeResponse> swardTypeResponses = new List<SwardTypeResponse>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSwardTypesAsyncAPI);
            
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

    public async Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(int sequenceId, int grassGrowthClassId)
    {
        Error? error = null;
        List<YieldRangesEnglandAndWalesResponse> yieldRanges = new List<YieldRangesEnglandAndWalesResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdAsyncAPI, HttpUtility.UrlEncode(sequenceId.ToString()), HttpUtility.UrlEncode(grassGrowthClassId.ToString()));
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

    public async Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesById(int defoliationId)
    {
        Error? error = null;
        DefoliationSequenceResponse? defoliationSequenceResponse = new DefoliationSequenceResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchDefoliationSequencesByIdAsyncAPI, HttpUtility.UrlEncode(defoliationId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
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

    public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementId(int swardManagementId)
    {
        Error? error = null;
        SwardManagementResponse? swardManagementResponse = new SwardManagementResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardManagementBySwardManagementIdAsyncAPI, HttpUtility.UrlEncode(swardManagementId.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
            {
                swardManagementResponse = responseWrapper?.Data?.ToObject<SwardManagementResponse>();
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

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeId(int swardTypeId)
    {
        Error? error = null;
        List<SwardManagementResponse>? swardManagementResponse = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardManagementBySwardTypeIdAsyncAPI, HttpUtility.UrlEncode(swardTypeId.ToString()));
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

    public async Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeId(int swardTypeId)
    {
        Error? error = null;
        SwardTypeResponse? swardTypeResponse = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchSwardTypeBySwardTypeIdAsyncAPI, HttpUtility.UrlEncode(swardTypeId.ToString()));
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
    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinking()
    {
        Error? error = null;
        List<CropTypeLinkingResponse>? cropTypeLinkingResponse = new List<CropTypeLinkingResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropTypeLinkingsAsyncAPI);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                cropTypeLinkingResponse = responseWrapper?.Data?.CropTypeLinking.records.ToObject<List<CropTypeLinkingResponse>>();
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
        return (cropTypeLinkingResponse, error);
    }

    public async Task<(bool, Error)> CopyCropNutrientManagementPlan(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser)
    {
        bool success = false;
        Error? error = null;
        try
        {
            var requestData = new
            {
                farmID,
                harvestYear,
                copyYear,
                isOrganic,
                isFertiliser
            };

            string jsonData = JsonConvert.SerializeObject(requestData);
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.CopyCropNutrientManagementPlanAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                var cropResponses = responseWrapper?.Data?.Recommendations;
                if (cropResponses != null)
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

    public async Task<(bool, Error)> MergeCrop(string cropData)
    {
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.MergeCropAPI, new StringContent(cropData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                success = responseWrapper?.Data;
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
    public async Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYear(int fieldId, int year)
    {
        List<Crop> crops = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropPlanByFieldIdAndYearAsyncAPI, HttpUtility.UrlEncode(fieldId.ToString()), HttpUtility.UrlEncode(year.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropList = responseWrapper?.Data?.ToObject<List<Crop>>();
                    crops.AddRange(cropList);
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
        return (crops, error);
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
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return isPerennial;
    }

}
