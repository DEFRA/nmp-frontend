using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using NMP.Commons.Helpers;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class PreviousCroppingService(ILogger<PreviousCroppingService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IPreviousCroppingService
{
    private readonly ILogger<PreviousCroppingService> _logger = logger;

    public async Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year)
    {
        PreviousCropping? previousCropping = null;
        Error error = new Error();

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchDataByFieldIdAndYearAsyncAPI, fieldId, year));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.PreviousCropping is JToken previousCroppingToken)
                {
                    previousCropping = previousCroppingToken.ToObject<PreviousCropping>() ?? new PreviousCropping();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);

                }
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

        return (previousCropping, error);
    }

    public async Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year)
    {
        List<PreviousCroppingData> previousCroppings = null;
        Error error = new Error();

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = "";
            if (year == null)
            {
                url = string.Format(ApiurlHelper.FetchFieldDataByFieldIdAsyncAPI, fieldId);
            }
            else
            {
                url = string.Format(ApiurlHelper.FetchFieldDataByFieldIdOldestHarvestYearAsyncAPI, fieldId, year);
            }
            var response = await httpClient.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.PreviousCropping is JToken previousCroppingToken)
                {
                    previousCroppings = previousCroppingToken.ToObject<List<PreviousCroppingData>>() ?? new List<PreviousCroppingData>();
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

        return (previousCroppings, error);
    }
    public async Task<(bool, Error)> MergePreviousCropping(string jsonData)
    {
        bool success = false;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(
                ApiurlHelper.MergePreviousCropAPI,
                new StringContent(jsonData, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is not null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    success = responseWrapper?.Data?.PreviousCropping;
                }
            }
            else
            {
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

        return (success, error);
    }
    public async Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId)
    {
        int? oldestYear = null;
        Error error = new Error();

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchPreviousCroppingYearByFarmIdAsyncAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                oldestYear = responseWrapper?.Data?.OldestPreviousCropping.ToObject<int>();
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = _logger.ExtractError(responseWrapper, error) ?? new Error();
                }
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

        return (oldestYear, error);
    }
}
