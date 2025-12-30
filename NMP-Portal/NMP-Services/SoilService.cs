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
public class SoilService(ILogger<SoilService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), ISoilService
{
    private readonly ILogger<SoilService> _logger = logger;

    public async Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId)
    {
        Error error = null;
        string nutrientIndex = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(APIURLHelper.FetchSoilNutrientIndexAsyncAPI, nutrientId, nutrientValue, methodologyId);
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
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return (nutrientIndex, error);
    }
    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        string soilType = string.Empty;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(APIURLHelper.FetchSoilTypeByIdAsyncAPI, soilTypeId);
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

}
