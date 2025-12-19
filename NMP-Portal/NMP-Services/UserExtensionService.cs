using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using NMP.Securities;
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class UserExtensionService(ILogger<UserExtensionService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) 
    : Service(httpContextAccessor, clientFactory, tokenRefreshService), IUserExtensionService
{
    private readonly ILogger<UserExtensionService> _logger = logger;

    public async Task<UserExtension?> FetchUserExtensionAsync()
    {
        _logger.LogTrace("FetchUserExtensionAsync method in UserExtensionService");
        UserExtension? userExtension = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = APIURLHelper.FetchUserExtensionAPI;
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (responseWrapper?.Data["UserExtension"] is JObject UserExtensionJObject)
                {
                    userExtension = UserExtensionJObject.ToObject<UserExtension>();
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        return userExtension;
    }


    public async Task<UserExtension?> UpdateTermsOfUseAsync(TermsOfUse termsOfUse)
    {
        _logger.LogTrace("UpdateTermsOfUseAsync method in UserExtensionService");
        string jsonData = JsonConvert.SerializeObject(termsOfUse);
        UserExtension? userExtension = null;

        HttpClient httpClient = await GetNMPAPIClient();
        // if new farm then save farm data
        var response = await httpClient.PutAsync(APIURLHelper.UpdateUserExtensionTermsOfUseAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (responseWrapper?.Data["UserExtension"] is JObject UserExtensionJObject)
                {
                    userExtension = UserExtensionJObject.ToObject<UserExtension>();
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        return userExtension;
    }

    public async Task<UserExtension?> UpdateShowAboutServiceAsync(AboutService aboutService)
    {
        _logger.LogTrace("UpdateShowAboutServiceAsync method in UserExtensionService");
        string jsonData = JsonConvert.SerializeObject(aboutService);
        UserExtension? userExtension = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PutAsync(APIURLHelper.UpdateUserExtensionDoNotShowAboutServiceAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (responseWrapper?.Data["UserExtension"] is JObject UserExtensionJObject)
                {
                    userExtension = UserExtensionJObject.ToObject<UserExtension>();
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        return userExtension;
    }
}
