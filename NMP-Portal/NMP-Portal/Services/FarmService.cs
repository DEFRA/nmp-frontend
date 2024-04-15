using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using System.Text;
using System.Text.Json.Nodes;

namespace NMP.Portal.Services
{
    public class FarmService : Service, IFarmService
    {
        private readonly ILogger<FarmService> _logger;
        public FarmService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base
        (httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<Farm> AddFarmAsync(FarmData farmData)
        {
            string jsonData = JsonConvert.SerializeObject(farmData);
            Farm farm = new Farm();
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);

            var response = await httpClient.PostAsync(APIURLHelper.AddFarmAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
            {

                JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                if (farmData != null)
                {
                    farm = farmDataJObject.ToObject<Farm>();
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

            return farm;
        }
    }
}
