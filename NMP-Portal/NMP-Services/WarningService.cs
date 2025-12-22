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

    public async Task<(List<WarningHeaderResponse>, Error)> FetchWarningHeaderByFieldIdAndYear(string fieldIds, int harvestYear)
    {
        var warningHeaders = new List<WarningHeaderResponse>();
        var error = new Error();

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(
                string.Format(APIURLHelper.FetchWarningCodesByFieldIdAndYearAsyncAPI, fieldIds, harvestYear));

            string result = await response.Content.ReadAsStringAsync();

            ResponseWrapper? responseWrapper = null;

            try
            {
                responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            }
            catch
            {
                // invalid JSON
                responseWrapper = null;
            }

            if (response.IsSuccessStatusCode)
            {
                var warningList = responseWrapper?.Data?.ToObject<List<WarningHeaderResponse>>();

                if (warningList != null)
                {
                    warningHeaders.AddRange(warningList);
                }
            }
            else
            {
                var apiError = responseWrapper?.Error?.ToObject<Error>();

                if (apiError != null)
                {
                    error = apiError;
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre.Message);
            throw new HttpRequestException(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new InvalidOperationException(error.Message, ex);
        }

        return (warningHeaders, error);
    }

}
