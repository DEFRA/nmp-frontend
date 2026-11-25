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
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class SnsAnalysisService(ILogger<SnsAnalysisService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : CacheableService(httpContextAccessor, clientFactory, tokenRefreshService,redisCacheService, memoryCacheService), ISnsAnalysisService
{
    private readonly ILogger<SnsAnalysisService> _logger = logger;

    public async Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId)
    {
        SnsAnalysis snsAnalysis = new SnsAnalysis();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSnsAnalysisByCropIdAPI, cropId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.snsAnalyses?.records is JArray records && records.Count > 0)
                {
                    snsAnalysis = records[0].ToObject<SnsAnalysis>() ?? new SnsAnalysis();
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
        return snsAnalysis;
    }

    public async Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData)
    {
        string jsonData = JsonConvert.SerializeObject(new { SnsAnalysis = snsData }, Formatting.Indented);

        SnsAnalysis? snsAnalysis = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.AddSnsAnalysisAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?["snsAnalysis"] is JObject farmDataJObject)
                {
                    snsAnalysis = farmDataJObject.ToObject<SnsAnalysis>() ?? new SnsAnalysis();
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return (snsAnalysis, error);
    }
    public async Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId)
    {
        string message = string.Empty;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteSNSAnalysisAPI, snsAnalysisId));
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
                _logger.ExtractError(responseWrapper, error);
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
    public async Task<SnsAnalysisViewModel?> GetSnsFromSessionAsync(string cacheKey)
    {
        var cached = await GetFromCacheAsync<SnsAnalysisViewModel>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
        return null;
    }

    public async Task SetSnsToSessionAsync(SnsAnalysisViewModel snsAnalysisViewModel, string cacheKey)
    {
        await SetCacheAsync(cacheKey, snsAnalysisViewModel);
    }
    public async Task RemoveSnsSessionAsync(string cacheKey)
    {
        await RemoveCacheAsync(cacheKey);
    }
}
