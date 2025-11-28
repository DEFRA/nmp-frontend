using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Security;
using NMP.Portal.ServiceResponses;
using System.Text;

namespace NMP.Portal.Services
{
    public class AuthService: Service,IAuthService
    {
        private readonly ILogger<AuthService> _logger;        
        public AuthService(ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
        {
            _logger = logger;
        }

        public async Task<(int,Error)> AddOrUpdateUser(UserData userData)
        {
            string jsonData = JsonConvert.SerializeObject(userData);
            int userId=0;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(APIURLHelper.AddOrUpdateUserAsyncAPI,new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    userId = responseWrapper.Data["UserID"];
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
                error = new Error();
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
            }
            catch (Exception ex)
            {
                error = new Error();
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }

            return (userId,error);
        }

    }
}
