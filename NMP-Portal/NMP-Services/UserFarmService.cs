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
public class UserFarmService(ILogger<UserFarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IUserFarmService
{
    private readonly ILogger<UserFarmService> _logger = logger;

    public async Task<(UserFarmResponse, Error)> UserFarmAsync(int userId)
    {
        UserFarmResponse userFarmList = new UserFarmResponse();
        Error? error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(APIURLHelper.FetchFarmByUserIdAPI, userId);
        var response = await httpClient.GetAsync(requestUrl);
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                UserFarmResponse userFarmResponse = responseWrapper.Data.ToObject<UserFarmResponse>();
                userFarmList.Farms = userFarmResponse.Farms;
            }
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

        return (userFarmList, error);
    }
}
