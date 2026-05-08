using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
public class SoilAnalysisService(
    ILogger<SoilAnalysisService> logger,
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory clientFactory,
    TokenRefreshService tokenRefreshService)
    : Service(httpContextAccessor, clientFactory, tokenRefreshService), ISoilAnalysisService
{
    private readonly ILogger<SoilAnalysisService> _logger = logger;

    private const string _applicationJson = "application/json";
    private const string _soilAnalysisKey = "SoilAnalysis";
    private const string _soilAnalysisLowerKey = "soilAnalysis";
    private const string _messageKey = "message";

    public async Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id)
    {
        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id} called.", id);

        return await SendRequestAsync(
            client => client.GetAsync(
                string.Format(
                    ApiurlHelper.FetchSoilAnalysisByIdAsyncAPI,
                    HttpUtility.UrlEncode(id.ToString()))),
            wrapper => MapSoilAnalysis(wrapper, _soilAnalysisKey));
    }

    public async Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        _logger.LogTrace(
            "SoilAnalysisService: soil-analyses/{Id}/{SoilData} called.",
            id,
            soilData);

        return await SendRequestAsync(
            client => client.PutAsync(
                string.Format(ApiurlHelper.UpdateSoilAnalysisAsyncAPI, id),
                CreateJsonContent(soilData)),
            wrapper => MapSoilAnalysis(wrapper, _soilAnalysisKey));
    }

    public async Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        return await SendRequestAsync(
            client => client.PostAsync(
                ApiurlHelper.AddSoilAnalysisAsyncAPI,
                CreateJsonContent(soilAnalysisData)),
            wrapper => MapSoilAnalysis(wrapper, _soilAnalysisLowerKey));
    }

    public async Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        var (data, error) = await SendRequestAsync(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteSoilAnalysisByIdAPI, soilAnalysisId)),
            MapMessage);

        return (data ?? string.Empty, error);
    }

    private static StringContent CreateJsonContent(string data)
    {
        return new StringContent(data, Encoding.UTF8, _applicationJson);
    }

    private static SoilAnalysis MapSoilAnalysis(
        ResponseWrapper? responseWrapper,
        string key)
    {
        if (responseWrapper?.Data?[key] is JObject obj)
        {
            return obj.ToObject<SoilAnalysis>() ?? new SoilAnalysis();
        }

        return new SoilAnalysis();
    }

    private static string MapMessage(ResponseWrapper? responseWrapper)
    {
        if (responseWrapper?.Data is JObject obj)
        {
            return obj[_messageKey]?.Value<string>() ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task<(T?, Error)> SendRequestAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> httpCall,
        Func<ResponseWrapper?, T?> mapData)
    {
        Error error = new();
        T? resultData = default;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            HttpResponseMessage response = await httpCall(httpClient);

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper =
                JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                resultData = mapData(responseWrapper);
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

        return (resultData, error);
    }
}
