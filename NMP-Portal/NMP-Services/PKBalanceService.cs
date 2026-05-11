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
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class PKBalanceService(ILogger<PKBalanceService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IPKBalanceService
{
    private readonly ILogger<PKBalanceService> _logger = logger;

    public async Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year, int fieldId)
    {
        PKBalance? pKBalance = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchPKBalanceByYearAndFieldIdAsyncAPI, year, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.PkBalances is JToken pkBalancesToken)
                {
                    pKBalance = pkBalancesToken.ToObject<PKBalance>()
                        ?? new PKBalance();
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
        return pKBalance;
    }
}
