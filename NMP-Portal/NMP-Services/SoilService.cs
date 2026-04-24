using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class SoilService(ILogger<SoilService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), ISoilService
{
    private readonly ILogger<SoilService> _logger = logger;
    private const string _errorLog= "{Code} : {Message} : {Stack} : {Path}";
    public async Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId)
    {
        Error error = null;
        string nutrientIndex = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilNutrientIndexAsyncAPI, HttpUtility.UrlEncode(nutrientId.ToString()), HttpUtility.UrlEncode(nutrientValue.ToString()), HttpUtility.UrlEncode(methodologyId.ToString()));
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            nutrientIndex = responseWrapper.Data["index"];
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLog, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (nutrientIndex, error);
    }
    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        string soilType = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilTypeByIdAsyncAPI, HttpUtility.UrlEncode(soilTypeId.ToString()));
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            soilType = responseWrapper?.Data["soilType"];
        }

        return soilType;
    }

    public async Task<(List<SoilMethologiesResponse>?, Error?)> FetchSoilMethodologies(int nutrientId,int countryId)
    {
        List<SoilMethologiesResponse>? soilMethodologyList = null;
        Error? error = null;

        _logger.LogTrace("Soil Service: soil-analyses-methods called.");
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilMethodologiesByNutrientAndCountryIdAsyncAPI, nutrientId,countryId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            soilMethodologyList = responseWrapper?.Data?.ToObject<List<SoilMethologiesResponse>>();
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                _logger.LogError(_errorLog, error?.Code, error?.Message, error?.Stack, error?.Path);
            }
        }
        return (soilMethodologyList, error);
    }

    public async Task<(SoilMethologiesResponse?, Error?)> FetchSoilMethodologyNameByNutrientIdAndMethodologyId(int nutrientId,int methodologyId)
    {
        SoilMethologiesResponse? soilAnalysesMethod = null;
        Error? error = null;

        _logger.LogTrace("Soil Service: soil-analyses-methods called.");
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchAllSoilMethodologyNameAsyncAPI, nutrientId, methodologyId));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            soilAnalysesMethod = responseWrapper?.Data?.ToObject<SoilMethologiesResponse>();
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                _logger.LogError(_errorLog, error?.Code, error?.Message, error?.Stack, error?.Path);
            }
        }
        return (soilAnalysesMethod, error);
    }
    public async Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId)
    {
        Error? error = null;
        List<SoilNutrientStatusResponse>? statusList = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(ApiurlHelper.FetchSoilNutrientStatusListAsyncAPI, HttpUtility.UrlEncode(HttpUtility.UrlEncode(methodologyId.ToString())));
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper?.Data != null)
        {
            List<SoilNutrientStatusResponse>? soilNutrientIndiceResponse = responseWrapper?.Data?.ToObject<List<SoilNutrientStatusResponse>>();

            if (soilNutrientIndiceResponse != null)
            {
                statusList = soilNutrientIndiceResponse;
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError(_errorLog, error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (statusList, error);
    }
}
