using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NMP.Commons.Resources;
using System.Text;

using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using NMP.Securities;

namespace NMP.Services;
public class UserExtensionService : Service, IUserExtensionService
{
    private readonly ILogger<UserExtensionService> _logger;
    public UserExtensionService(ILogger<UserExtensionService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        _logger = logger;
    }

    public async Task<UserExtension?> FetchUserExtensionAsync()
    {
        UserExtension? userExtension = null;

        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = APIURLHelper.FetchUserExtensionAPI;
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                JObject UserExtensionJObject = responseWrapper.Data["UserExtension"] as JObject;
                if (UserExtensionJObject != null)
                {
                    userExtension = UserExtensionJObject.ToObject<UserExtension>();
                }
            }
        }
        
        return userExtension;
    }


    public async Task<UserExtension?> UpdateTermsOfUseAsync(TermsOfUse termsOfUse)
    {
        string jsonData = JsonConvert.SerializeObject(termsOfUse);
        UserExtension? userExtension = null;

        HttpClient httpClient = await GetNMPAPIClient();
        // if new farm then save farm data
        var response = await httpClient.PutAsync(APIURLHelper.UpdateUserExtensionTermsOfUseAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
            {
                JObject UserExtensionJObject = responseWrapper.Data["UserExtension"] as JObject;
                if (UserExtensionJObject != null)
                {
                    userExtension = UserExtensionJObject.ToObject<UserExtension>();
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
        else
        {
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception(Resource.MsgServiceNotAvailable);
            }
        }

        return userExtension;
    }

    public async Task<(UserExtension?)> UpdateShowAboutServiceAsync(AboutService aboutService)
    {
        string jsonData = JsonConvert.SerializeObject(aboutService);
        UserExtension userExtension = null;
        
            HttpClient httpClient = await GetNMPAPIClient();
            // if new farm then save farm data
            var response = await httpClient.PutAsync(APIURLHelper.UpdateUserExtensionDoNotShowAboutServiceAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    JObject UserExtensionJObject = responseWrapper.Data["UserExtension"] as JObject;
                    if (UserExtensionJObject != null)
                    {
                        userExtension = UserExtensionJObject.ToObject<UserExtension>();
                    }
                }                
            }            
       
        return userExtension;
    }
}
