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
    private const string _applicationJson = "application/json";
    private const string _string = "string";
    public async Task<(bool, Error?)> AddCropNutrientManagementPlanAsync(CropDataWrapper cropData)
    {
        string jsonData = JsonConvert.SerializeObject(cropData);
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddCropNutrientManagementPlanAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != _string)
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

    public async Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmIdAsync(int farmId, int type)
    {
        List<PlanSummaryResponse> planSummaryList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchPlanSummaryByFarmIdAPI, HttpUtility.UrlEncode(farmId.ToString()), HttpUtility.UrlEncode(type.ToString())));
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

    public async Task<(List<HarvestYearPlanResponse>, Error?)> FetchHarvestYearPlansByFarmIdAsync(int harvestYear, int farmId)
    {
        List<HarvestYearPlanResponse> harvestYearPlanList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchHarvestYearPlansByFarmIdAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));
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
    public async Task<(List<RecommendationHeader>, Error?)> FetchRecommendationByFieldIdAndYearAsync(int fieldId, int harvestYear)
    {
        List<RecommendationHeader> recommendationList = [];
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchRecommendationByFieldIdAndYearAPI, HttpUtility.UrlEncode(fieldId.ToString()), HttpUtility.UrlEncode(harvestYear.ToString())));
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

    public async Task<List<Crop>> FetchCropsByFieldIdAsync(int fieldId)
    {
        List<Crop> cropList = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropsByFieldIdAPI, HttpUtility.UrlEncode(fieldId.ToString())));
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

    public async Task<decimal> FetchCropTypeDefaultYieldByCropTypeIdAsync(int cropTypeId, bool isScotland)
    {
        decimal? defaultYield = 0;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingsByCropTypeIdAPI, HttpUtility.UrlEncode(cropTypeId.ToString())));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                var data = responseWrapper?.Data;
                if (data?.CropTypeLinking != null)
                {
                    var cropTypeLinkingResponse = data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
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

    public async Task<List<int>> FetchSecondCropListByFirstCropIdAsync(int firstCropTypeId, int rb209CountryId)
    {
        List<int> secondCropList = new List<int>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSecondCropListByFirstCropIdAPI, HttpUtility.UrlEncode(firstCropTypeId.ToString()), rb209CountryId));
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
    public async Task<(HarvestYearResponseHeader?, Error?)> FetchHarvestYearPlansDetailsByFarmIdAsync(int harvestYear, int farmId)
    {
        HarvestYearResponseHeader? harvestYearPlan = new();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropsOrganicinorganicdetailsByYearFarmIdAPI, HttpUtility.UrlEncode(harvestYear.ToString()), HttpUtility.UrlEncode(farmId.ToString())));
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

    public async Task<string?> FetchCropInfoOneQuestionByCropTypeIdAsync(int cropTypeId, int countryId)
    {
        string? cropInfoOneQuestion = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropInfoOneQuestionByCropTypeIdAPI, HttpUtility.UrlEncode(cropTypeId.ToString()),countryId));
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
    public async Task<(ManagementPeriod?, Error?)> FetchManagementperiodByIdAsync(int id)
    {
        ManagementPeriod? managementPeriod = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchManagementperiodByIdAPI, HttpUtility.UrlEncode(id.ToString()));
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
    public async Task<(Crop?, Error?)> FetchCropByIdAsync(int id)
    {
        Crop? crop = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropByIdAPI, HttpUtility.UrlEncode(id.ToString()));
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
    public async Task<(string, Error?)> RemoveCropPlanAsync(List<int> cropIds)
    {
        var cropIdsRequest = new { cropIds };
        Error? error = null;
        string? message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var jsonContent = JsonConvert.SerializeObject(cropIdsRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, _applicationJson);
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
    public async Task<(bool, Error?)> IsCropsGroupNameExistForUpdateAsync(string cropIds, string cropGroupName, int year, int farmId)
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
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data)
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
    public async Task<(List<Crop>, Error)> UpdateCropAsync(string cropData)
    {
        List<Crop> crops = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateCropAPI, new StringContent(cropData, Encoding.UTF8, _applicationJson));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != _string)
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


    public async Task<(List<GrassGrowthClassResponse>, Error?)> FetchGrassGrowthClassAsync(List<int> fieldIds)
    {
        var fieldIdsRequest = new { fieldIds };
        Error? error = null;        
        List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var jsonContent = JsonConvert.SerializeObject(fieldIdsRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, _applicationJson);
            var url = ApiurlHelper.FetchGrassGrowthClassesAPI;
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

    public async Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropIdAsync(int cropId, bool isShortSummary)
    {
        List<ManagementPeriod>? managementPeriodList = new List<ManagementPeriod>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchManagementPeriodByCropIdAPI, HttpUtility.UrlEncode(cropId.ToString()), HttpUtility.UrlEncode(isShortSummary.ToString()));
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

    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinkingAsync()
    {
        Error? error = null;
        List<CropTypeLinkingResponse>? cropTypeLinkingResponse = new List<CropTypeLinkingResponse>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchCropTypeLinkingsAPI);
            
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

    public async Task<(bool, Error)> CopyCropNutrientManagementPlanAsync(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser)
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
            var response = await httpClient.PostAsync(ApiurlHelper.CopyCropNutrientManagementPlanAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != _string)
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

    public async Task<(bool, Error)> MergeCropAsync(string cropData)
    {
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.MergeCropAPI, new StringContent(cropData, Encoding.UTF8, _applicationJson));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != _string)
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
    public async Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYearAsync(int fieldId, int year)
    {
        List<Crop> crops = new List<Crop>();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requestUrl = string.Format(ApiurlHelper.FetchCropPlanByFieldIdAndYearAPI, HttpUtility.UrlEncode(fieldId.ToString()), HttpUtility.UrlEncode(year.ToString()));
            var response = await httpClient.GetAsync(requestUrl);
            
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
    public async Task<bool> FetchIsPerennialByCropTypeIdAsync(int cropTypeId)
    {
        Error? error = null;
        bool isPerennial = false;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingsByCropTypeIdAPI, HttpUtility.UrlEncode(cropTypeId.ToString())));
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
