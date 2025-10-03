using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Linq.Expressions;

namespace NMP.Portal.Services
{
    public class UserFarmService : Service, IUserFarmService
    {
        private readonly ILogger<UserFarmService> _logger;
        public UserFarmService(ILogger<UserFarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, Security.TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<(UserFarmResponse, Error)> UserFarmAsync(int userId)
        {
            UserFarmResponse userFarmList = new UserFarmResponse();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmByUserIdAPI, userId));
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
                        error = responseWrapper.Error.ToObject<Error>();
                        _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                    }
                }
            }
            catch(HttpRequestException hre)
            {
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }

            return (userFarmList, error);
        }
    }
}
