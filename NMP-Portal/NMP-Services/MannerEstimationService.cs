using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class MannerEstimationService(ILogger<MannerEstimationService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IMannerEstimationService
{
    private readonly ILogger<MannerEstimationService> _logger = logger;
    public async Task<(List<MannerEstimation>, Error?)> FetchMannerEstimationsList(Guid orgId)
    {
        List<MannerEstimation> mannerEstimationsList = new List<MannerEstimation>();
        Error? error = null;

        HttpClient httpClient = await GetNMPAPIClient();
        string url = string.Format(ApiurlHelper.FetchAllMannerEstimationsAsyncAPI, orgId);
        var response = await httpClient.GetAsync(url);

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
        {
            var mannerEstimations = responseWrapper?.Data?.records.ToObject<List<MannerEstimation>>();
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
    public async Task<(bool, Error?)> AddMannerEstimationServiceAsync(string MannerData)
    {
        bool success = false;
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(
                ApiurlHelper.AddMannerEstimationAsyncAPI,
                new StringContent(MannerData, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is not null)
                {
                    success = responseWrapper?.Data?.savedMannerEstimation ?? false;
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

        return (success, error);

    }
}
