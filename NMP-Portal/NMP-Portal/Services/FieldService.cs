using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class FieldService : Service, IFieldService
    {
        private readonly ILogger<FieldService> _logger;
        public FieldService(ILogger<FieldService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
        {
            int fieldCount = 0;
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldCountByFarmIdAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    fieldCount = responseWrapper.Data["count"];

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

            return fieldCount;
        }
        public async Task<List<FieldResponseWapper>> FetchNutrientsAsync()
        {
            List<FieldResponseWapper> nutrients = new List<FieldResponseWapper>();
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsAsyncAPI));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    List<FieldResponseWapper> fieldResponseWapper = responseWrapper.Data.ToObject<List<FieldResponseWapper>>();
                    nutrients.AddRange(fieldResponseWapper);
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

            return nutrients;
        }
    }
}
