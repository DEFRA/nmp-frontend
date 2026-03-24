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

    public async Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id)
    {
        SoilAnalysis? soilAnalysis = null;
        Error? error = null;

        _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id} called.", id);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilAnalysisByIdAsyncAPI, HttpUtility.UrlEncode(id.ToString())));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            JObject? soilanalysisObject = responseWrapper?.Data["SoilAnalysis"] as JObject;
            if (soilanalysisObject != null)
            {
                soilAnalysis = soilanalysisObject?.ToObject<SoilAnalysis>();
            }
        }
        else
        {
            error = _logger.ExtractError(responseWrapper, error);
        }
        return (soilAnalysis, error);
    }
    public async Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        SoilAnalysis? soilAnalysis = null;
        Error? error = null;
        try
        {
            _logger.LogTrace("SoilAnalysisService: soil-analyses/{Id}/{SoilData} called.", id, soilData);
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(string.Format(ApiurlHelper.UpdateSoilAnalysisAsyncAPI, id), new StringContent(soilData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                JObject soilAnalysisJObject = responseWrapper?.Data["SoilAnalysis"] as JObject;
                if (soilAnalysisJObject != null)
                {
                    soilAnalysis = soilAnalysisJObject.ToObject<SoilAnalysis>();
                }

            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = error ?? new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre,hre.Message);
        }
        catch (Exception ex)
        {
            error = error ?? new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }

        return (soilAnalysis, error);
    }

    public async Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        SoilAnalysis? soilAnalysis = null;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddSoilAnalysisAsyncAPI, new StringContent(soilAnalysisData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {

                JObject soilAnalysisJObject = responseWrapper?.Data["soilAnalysis"] as JObject;
                if (soilAnalysisJObject != null)
                {
                    soilAnalysis = soilAnalysisJObject.ToObject<SoilAnalysis>();
                }

            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = error ?? new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = error ?? new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (soilAnalysis, error);
    }

    public async Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        Error? error = null;
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteSoilAnalysisByIdAPI, soilAnalysisId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                message = responseWrapper.Data["message"].Value;
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = error ?? new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = error ?? new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }

        return (message, error);
    }

}