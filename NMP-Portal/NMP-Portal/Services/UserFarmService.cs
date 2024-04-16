using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class UserFarmService : Service, IUserFarmService
    {
        private readonly ILogger<UserFarmService> _logger;
        public UserFarmService(ILogger<UserFarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<List<Farm>> UserFarmAsync(int userId)
        {
            List<Farm> userFarmList = new List<Farm>();
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmByUserIdAPI, userId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    UserFarmResponse userFarmResponse = responseWrapper.Data.ToObject<UserFarmResponse>();
                    userFarmList.AddRange(userFarmResponse.Farms);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    Error error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                }
            }

            return userFarmList;
        }
    }
}
