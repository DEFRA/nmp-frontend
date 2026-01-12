using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchPKBalanceByYearAndFieldIdAsyncAPI, year, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null&&responseWrapper.Data.PkBalances!=null)
                {
                    pKBalance = new PKBalance();
                    pKBalance = responseWrapper.Data.ToObject<PKBalance>();
                }
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
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex.Message);
            throw new Exception(error.Message, ex);
        }
        return pKBalance;
    }        
}
