using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class WarningService(ILogger<WarningService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IWarningService
{
    private readonly ILogger<WarningService> _logger = logger;

    public async Task<List<WarningHeaderResponse>> FetchWarningHeaderByFieldIdAndYear(string fieldIds, int harvestYear)
    {
        var warningHeaders = new List<WarningHeaderResponse>();
        string requestUrl = string.Format(APIURLHelper.FetchWarningCodesByFieldIdAndYearAsyncAPI, fieldIds, harvestYear);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (responseWrapper != null && responseWrapper.Data != null)
        {
            warningHeaders.AddRange(responseWrapper?.Data?.ToObject<List<WarningHeaderResponse>>());
        }

        return warningHeaders;
    }
    public async Task<WarningResponse> FetchWarningByCountryIdAndWarningKeyAsync(int countryId, string warningKey)
    {
        string requestUrl = string.Format(APIURLHelper.FetchWarningByCountryIdAndWarningKeyAsyncAPI, countryId, warningKey);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (responseWrapper?.Data is null)
        {
            return new WarningResponse();
        }
        return responseWrapper.Data.ToObject<WarningResponse>();
    }


}
