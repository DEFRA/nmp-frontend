using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Resources;
using System.Text;
using NMP.Portal.Security;

namespace NMP.Portal.Services;
public class UserExtensionService : Service, IUserExtensionService
{
    private readonly ILogger<UserExtensionService> _logger;
    public UserExtensionService(ILogger<UserExtensionService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        _logger = logger;
    }

    public async Task<(UserExtension, Error)> FetchUserExtensionAsync()
    {
        UserExtension userExtension = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchUserExtensionAPI));
            string result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (responseWrapper != null && responseWrapper.Data != null)
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
                if(response.StatusCode!= System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception(Resource.MsgServiceNotAvailable);
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

        return (userExtension, error);
    }


    public async Task<(UserExtension, Error)> UpdateTermsOfUseAsync(TermsOfUse termsOfUse)
    {
        string jsonData = JsonConvert.SerializeObject(termsOfUse);
        UserExtension userExtension = null;
        Error error = new Error();
        try
        {
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
        return (userExtension, error);
    }

    public async Task<(UserExtension, Error)> UpdateShowAboutServiceAsync(AboutService aboutService)
    {
        string jsonData = JsonConvert.SerializeObject(aboutService);
        UserExtension userExtension = null;
        Error error = new Error();
        try
        {
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
        return (userExtension, error);
    }
}
