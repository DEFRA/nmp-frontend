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
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class SoilAnalysisService(ILogger<SoilAnalysisService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : CacheableService(httpContextAccessor, clientFactory, tokenRefreshService, redisCacheService, memoryCacheService), ISoilAnalysisService
{
    private readonly ILogger<SoilAnalysisService> _logger = logger;
    private const string _applicationJson = "application/json";

    public async Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id)
    {
        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id} called.", id);

        return await SendSoilAnalysisRequest(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchSoilAnalysisByIdAPI, HttpUtility.UrlEncode(id.ToString()))
            ));
    }

    public async Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id}/{SoilData} called.", id, soilData);

        return await SendSoilAnalysisRequest(
            client => client.PutAsync(
                string.Format(ApiurlHelper.UpdateSoilAnalysisAPI, id),
                new StringContent(soilData, Encoding.UTF8, _applicationJson)
            ));
    }

    public async Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        return await SendSoilAnalysisRequest(
            client => client.PostAsync(
                ApiurlHelper.AddSoilAnalysisAPI,
                new StringContent(soilAnalysisData, Encoding.UTF8, _applicationJson)
            ),
            key: "soilAnalysis"
        );
    }

    public async Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        var (data, error) = await SendRequestAsync<string>(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteSoilAnalysisByIdAPI, soilAnalysisId)
            ),
            wrapper => ExtractMessage(wrapper), _logger);

        return (data ?? string.Empty, error);
    }

    private Task<(SoilAnalysis?, Error)> SendSoilAnalysisRequest(
    Func<HttpClient, Task<HttpResponseMessage>> httpCall,
    string key = "SoilAnalysis")
    {
        return SendRequestAsync(
            httpCall,
            wrapper => ExtractObject<SoilAnalysis>(wrapper, key) ?? new SoilAnalysis()
        ,_logger);
    }

    private static T? ExtractObject<T>(ResponseWrapper? wrapper, string key)
    {
        if (wrapper?.Data?[key] is JObject obj)
        {
            return obj.ToObject<T>();
        }
        return default;
    }

    private static string ExtractMessage(ResponseWrapper? wrapper)
    {
        if (wrapper?.Data is JObject obj)
        {
            return obj["message"]?.Value<string>() ?? string.Empty;
        }
        return string.Empty;
    }

    public async Task<SoilAnalysisViewModel?> LoadFieldDataFromSession(string? cacheKey)
    {
        var cached = await GetFromCacheAsync<List<SoilAnalysisViewModel>>(cacheKey);
        if (cached != null)
        {
            return cached.FirstOrDefault();
        }
        return null;
    }

    public async Task SetFieldDataToSession(SoilAnalysisViewModel field, string? cacheKey)
    {
        await SetCacheAsync(cacheKey, new List<SoilAnalysisViewModel> { field });
    }
    public async Task RemoveFieldDataFromSession(string? cacheKey)
    {
        await RemoveCacheAsync(cacheKey);

    }

}