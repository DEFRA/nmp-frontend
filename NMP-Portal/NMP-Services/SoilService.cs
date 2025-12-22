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
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilNutrientIndexAsyncAPI, nutrientId, nutrientValue, methodologyId));
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
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new();
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return (nutrientIndex,error);
    }
    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        Error error = null;
        string soilType = string.Empty;
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilTypeByIdAsyncAPI, soilTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                soilType = responseWrapper.Data["soilType"];
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
        }
        return soilType;
    }

}
