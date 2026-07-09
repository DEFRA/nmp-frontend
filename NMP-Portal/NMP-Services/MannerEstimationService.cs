using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class MannerEstimationService(ILogger<MannerEstimationService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IMannerEstimationService
{
    private readonly ILogger<MannerEstimationService> _logger = logger;
    private const string _dateFormat = "yyyy-MM-dd";
    private const string _contentType = "application/json";
    public async Task<(List<MannerEstimationDetailsViewModel>, Error?)> FetchMannerEstimationsList(Guid orgId)
    {
        List<MannerEstimationDetailsViewModel> mannerEstimationsList = new List<MannerEstimationDetailsViewModel>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Format(ApiurlHelper.FetchAllMannerEstimationsAsyncAPI, orgId);
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var mannerEstimations = responseWrapper?.Data?.ToObject<List<MannerEstimationDetailsViewModel>>();
            mannerEstimationsList.AddRange(mannerEstimations);
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimationsList, error);
    }
    public async Task<bool> FetchIsExistMannerEstimationsByOrgIdAndNameAsyncAPI(Guid organisationId, string name)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchIsExistMannerEstimationsByOrgIdAndNameAsyncAPI, organisationId, name));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        bool isExist = responseWrapper?.Data?["exists"] ?? false;

        return isExist;
    }
    public async Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationServiceAsync(string MannerData)
    {
        MannerEstimationApplication? mannerEstimationApplication = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(
                ApiurlHelper.AddMannerEstimationAsyncAPI,
                new StringContent(MannerData, Encoding.UTF8, _contentType));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is not null)
                {
                    mannerEstimationApplication = responseWrapper.Data?.ToObject<MannerEstimationApplication>();
                }
            }
            else
            {
                error = new Error();
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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

        return (mannerEstimationApplication, error);

    }

    public async Task<(int?, Error?)> FetchSoilTypeSoilTextureByTopSoilSubSoilId(int topSoilId, int subSoilId)
    {
        int? soilTypeId = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = string.Empty;

            url = string.Format(ApiurlHelper.FetchSoilTypeIdByTopSoilIdAndSubSoilIdAsyncAPI, topSoilId, subSoilId);

            var response = await httpClient.GetAsync(url);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                soilTypeId = responseWrapper?.Data?.SoilTypeId?.ToObject<int?>();
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
        return (soilTypeId, error);
    }
    public async Task<(List<MannerEstimationApplication>, Error?)> FetchMannerApplicationsByMannerEstimationId(int mannerEstimationId)
    {
        List<MannerEstimationApplication> mannerEstimationApplications = new List<MannerEstimationApplication>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerApplicationsByEstimationIdAsyncAPI, mannerEstimationId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var applications = responseWrapper?.Data?.MannerEstimationApplications.ToObject<List<MannerEstimationApplication>>();
                mannerEstimationApplications.AddRange(applications);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimationApplications, error);
    }
    public async Task<(MannerEstimationApplication, Error?)> FetchMannerApplicationById(int mannerApplicationId)
    {
        MannerEstimationApplication mannerEstimationApplication = new MannerEstimationApplication();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerManureTypeByManureTypeIdAsyncAPI, HttpUtility.UrlEncode(mannerApplicationId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                mannerEstimationApplication = responseWrapper?.Data?.ToObject<MannerEstimationApplication>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimationApplication, error);
    }
    public async Task<(MannerEstimationResultResponse?, Error?)> FetchMannerApplicationResultById(int mannerEstimationId)
    {
        MannerEstimationResultResponse? mannerEstimationResultResponse = null;
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerEstimationResultByIdAsyncAPI, HttpUtility.UrlEncode(mannerEstimationId.ToString())));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                mannerEstimationResultResponse = responseWrapper?.Data?.ToObject<MannerEstimationResultResponse>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimationResultResponse, error);
    }

    public async Task<(int, Error?)> CopyMannerEstimation(int id, string estimationName)
    {
        int newEstimationId = 0;
        Error? error = null;
        string jsonData = JsonConvert.SerializeObject(new
        {
            ID = id,
            Name = estimationName
        });

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PostAsync(ApiurlHelper.CopyMannerEstimationAsyncAPI,
                new StringContent(jsonData, Encoding.UTF8, _contentType));

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var mannerEstimationId = responseWrapper?.Data?.mannerEstimationId;

            if (mannerEstimationId > 0)
            {
                newEstimationId = mannerEstimationId;
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (newEstimationId, error);

        
    }
    public async Task<(List<NutrientProductResponse>, Error?)> FetchNutrientProductByNutrientId(int nurteintId)
    {
        List<NutrientProductResponse> nutrientProducts = new List<NutrientProductResponse>();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientProductByNutrientIdAsyncAPI, nurteintId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var nutrientProductList = responseWrapper?.Data?.ToObject<List<NutrientProductResponse>>();
                nutrientProducts.AddRange(nutrientProductList);
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (nutrientProducts, error);
    }
    public async Task<(MannerEstimation?, Error?)> FetchMannerEstimateById(int mannerEstimateId)
    {
        MannerEstimation? mannerEstimation = null;
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerEstimateByIdAsyncAPI, mannerEstimateId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                 mannerEstimation = responseWrapper?.Data?.records.ToObject<MannerEstimation>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimation, error);
    }
    public async Task<(MannerEstimation?, Error?)> UpdateMannerEstimationServiceAsync(string MannerData)
    {
        MannerEstimation? mannerEstimation = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(
                ApiurlHelper.UpdateMannerEstimateAsyncAPI,
                new StringContent(MannerData, Encoding.UTF8, _contentType));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is not null)
                {
                    mannerEstimation = responseWrapper.Data?.MannerEstimation.ToObject<MannerEstimation>();
                }
            }
            else
            {
                error = new Error();
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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

        return (mannerEstimation, error);

    }
    public async Task<(decimal, Error)> FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(int mannerEstimationId, DateTime startDate, DateTime endDate, bool isGreenFoodCompost, int? mannerApplicationId)
    {
        decimal totalN = 0;
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
       
        string url = ApiurlHelper.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompostAsyncAPI;
        if (mannerApplicationId.HasValue)
        {
            url += $"&mannerApplicationID={mannerApplicationId.Value}";
        }
        url = string.Format(url, mannerEstimationId, startDate.ToString(_dateFormat), endDate.ToString(_dateFormat), isGreenFoodCompost);
        var response = await httpClient.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            totalN = responseWrapper?.Data?.TotalN?.ToObject<decimal>() ?? 0;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (totalN, error);
    }
    public async Task<(decimal, Error)> FetchTotalNByMannerEstimationIdAppDate(int mannerEstimationId, DateTime startDate, DateTime endDate, int? mannerApplicationId)
    {
        decimal totalN = 0;
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        string url = ApiurlHelper.FetchTotalNByMannerEstimationIdAppDateAsyncAPI;
        if (mannerApplicationId.HasValue)
        {
            url += $"&mannerApplicationID={mannerApplicationId.Value}";
        }
        url = string.Format(url, mannerEstimationId, startDate.ToString(_dateFormat), endDate.ToString(_dateFormat));
        var response = await httpClient.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            totalN = responseWrapper?.Data?.TotalN?.ToObject<decimal>() ?? 0;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (totalN, error);
    }
    public async Task<(bool, Error)> CheckMannerGreenCompostExistanceByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId)
    {
        bool isExist = false;
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        
        string url = ApiurlHelper.CheckMannerGreenCompostExistanceByDateRangeAsyncAPI;
        if (mannerApplicationId.HasValue)
        {
            url += $"&mannerApplicationID={mannerApplicationId.Value}";
        }
        url = string.Format(url, mannerEstimationId, dateFrom, dateTo);
        var response = await httpClient.GetAsync(url);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            isExist = responseWrapper?.Data?.IsExist?.ToObject<bool>() ?? false;
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (isExist, error);
    }
    public async Task<(MannerEstimationApplication?, Error?)> FetchMannerEstimateApplicationByIdAsync(int mannerEstimateApplicationId)
    {
        MannerEstimationApplication? mannerEstimationApplication = null;
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchMannerEstimateApplicationByIdAsyncAPI, mannerEstimateApplicationId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                mannerEstimationApplication = responseWrapper?.Data?.records.ToObject<MannerEstimationApplication>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }

        return (mannerEstimationApplication, error);
    }
    public async Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimationApplicationServiceAsync(string MannerApplicationData)
    {
        MannerEstimationApplication? mannerEstimationApplication = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(
              string.Format(ApiurlHelper.UpdateMannerEstimateApplicationAsyncAPI),
                new StringContent(MannerApplicationData, Encoding.UTF8, _contentType));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is not null)
                {
                    mannerEstimationApplication = responseWrapper.Data?.MannerEstimationApplication.ToObject<MannerEstimationApplication>();
                }
            }
            else
            {
                error = new Error();
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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

        return (mannerEstimationApplication, error);

    }
}

