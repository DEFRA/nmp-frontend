using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class WarningService : Service, IWarningService
    {
        private readonly ILogger<FieldService> _logger;
        public WarningService(ILogger<FieldService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, Security.TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<(List<WarningCodeResponse>,Error)> FetchWarningCodeByFieldIdAndYear(string fieldIds, int harvestYear)
        {
            List<WarningCodeResponse> warningCodes = new List<WarningCodeResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchWarningCodesByFieldIdAndYearAsyncAPI, fieldIds, harvestYear));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var warningCodelist = responseWrapper.Data.ToObject<List<WarningCodeResponse>>();
                        warningCodes.AddRange(warningCodelist);
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

            return (warningCodes,error);
        }
    }
}
