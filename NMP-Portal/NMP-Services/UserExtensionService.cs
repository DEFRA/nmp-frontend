using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class UserExtensionService(ILogger<UserExtensionService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService)
    : Service(httpContextAccessor, clientFactory, tokenRefreshService), IUserExtensionService
{
    private readonly ILogger<UserExtensionService> _logger = logger;
    private readonly string _userExtension = "UserExtension";

    public async Task<UserExtension?> FetchUserExtensionAsync()
    {
        _logger.LogTrace("FetchUserExtensionAsync method in UserExtensionService");
        UserExtension? userExtension = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = ApiurlHelper.FetchUserExtensionAPI;
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper?.Data is JObject data &&
            data[_userExtension] is JObject userExtensionObject)
            {
                userExtension = userExtensionObject.ToObject<UserExtension>();
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
        var response = await httpClient.PutAsync(ApiurlHelper.UpdateUserExtensionTermsOfUseAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper?.Data is JObject data &&
            data[_userExtension] is JObject userExtensionObject)
            {
                userExtension = userExtensionObject.ToObject<UserExtension>();
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
        var response = await httpClient.PutAsync(ApiurlHelper.UpdateUserExtensionDoNotShowAboutServiceAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper?.Data is JObject data &&
             data[_userExtension] is JObject userExtensionObject)
            {
                userExtension = userExtensionObject.ToObject<UserExtension>();
            }
        }

        return userExtension;
    }
    public async Task<UserExtension?> UpdateShowAboutMannerAsync(AboutManner aboutManner)
    {
        _logger.LogTrace("UpdateShowAboutMannerAsync method in UserExtensionService");
        string jsonData = JsonConvert.SerializeObject(aboutManner);
        UserExtension? userExtension = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PutAsync(ApiurlHelper.UpdateUserExtensionDoNotShowAboutMannerAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper?.Data is JObject data &&
             data[_userExtension] is JObject userExtensionObject)
            {
                userExtension = userExtensionObject.ToObject<UserExtension>();
            }
        }

        return userExtension;
    }
}
