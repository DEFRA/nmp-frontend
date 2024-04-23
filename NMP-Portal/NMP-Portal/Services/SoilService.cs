using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class SoilService : Service, ISoilService
    {
        private readonly ILogger<SoilService> _logger;
        public SoilService(ILogger<SoilService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base
        (httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<(int, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId)
        {
            Error error = null;
            int nutrientIndex = 0;
            try
            {

                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilNutrientIndexAsyncAPI, nutrientId, nutrientValue, methodologyId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    nutrientIndex = responseWrapper.Data["index"];
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
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }
            return (nutrientIndex,error);
        }
    }
}
