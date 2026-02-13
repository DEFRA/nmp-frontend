using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class FarmContextService(ILogger<FarmContextService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IFarmContextService
{
    private readonly ILogger<FarmContextService> _logger = logger;

    public async Task<Farm?> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching farm details for FarmId: {FarmId}", farmId);
        Farm? farm = null;        
        string url = string.Format(ApiurlHelper.FetchFarmByIdAPI, farmId);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();        
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if(responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                if (responseWrapper?.Data["Farm"] is JObject farmDataJObject)
                {
                    farm = farmDataJObject.ToObject<Farm>();
                }
            }            
        }        

        return farm;
    }
}