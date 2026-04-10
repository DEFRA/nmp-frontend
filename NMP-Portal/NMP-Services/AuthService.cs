using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Models;
using NMP.Commons.Helpers;
using NMP.Commons.ServiceResponses;
using NMP.Core.Interfaces;
using NMP.Core.Attributes;
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class AuthService(ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService),IAuthService
{
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<(int,Error)> AddOrUpdateUser(UserData userData)
    {
        string jsonData = JsonConvert.SerializeObject(userData);
        int userId=0;
        Error? error = new Error(); 
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddOrUpdateUserAsyncAPI,new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper?.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                userId = responseWrapper?.Data["UserID"];
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            error = _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            error = _logger.HandleException(ex, error);
        }

        return (userId,error);
    }

}
