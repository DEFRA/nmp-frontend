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
public class SoilAnalysisService(ILogger<SoilAnalysisService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), ISoilAnalysisService
{
    private readonly ILogger<SoilAnalysisService> _logger = logger;
    private const string _applicationJson = "application/json";

    public async Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id)
    {
        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id} called.", id);

        var (data, error) = await SendRequestAsync<SoilAnalysis>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchSoilAnalysisByIdAsyncAPI, HttpUtility.UrlEncode(id.ToString()))
            ),
            wrapper =>
            {
                if (wrapper?.Data?["SoilAnalysis"] is JObject obj)
                {
                    return obj.ToObject<SoilAnalysis>()
                           ?? new SoilAnalysis();
                }

                return new SoilAnalysis();
            });

        return (data, error);
    }
    public async Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id}/{SoilData} called.", id, soilData);

        var (data, error) = await SendRequestAsync<SoilAnalysis>(
            client => client.PutAsync(
                string.Format(ApiurlHelper.UpdateSoilAnalysisAsyncAPI, id),
                new StringContent(soilData, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["SoilAnalysis"] is JObject obj)
                {
                    return obj.ToObject<SoilAnalysis>()
                           ?? new SoilAnalysis();
                }

                return new SoilAnalysis();
            });

        return (data, error);
    }

    public async Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        var (data, error) = await SendRequestAsync<SoilAnalysis>(
            client => client.PostAsync(
                ApiurlHelper.AddSoilAnalysisAsyncAPI,
                new StringContent(soilAnalysisData, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["soilAnalysis"] is JObject obj)
                {
                    return obj.ToObject<SoilAnalysis>()
                           ?? new SoilAnalysis();
                }

                return new SoilAnalysis();
            });

        return (data, error);
    }

    public async Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        var (data, error) = await SendRequestAsync<string>(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteSoilAnalysisByIdAPI, soilAnalysisId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj["message"]?.Value<string>() ?? string.Empty;
                }

                return string.Empty;
            });

        return (data ?? string.Empty, error);
    }
    private async Task<(T?, Error)> SendRequestAsync<T>(Func<HttpClient, Task<HttpResponseMessage>> httpCall,
   Func<ResponseWrapper?, T?> mapData)
    {
        Error error = new Error();
        T? resultData = default;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpCall(httpClient);

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