using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Commons.Resources;
using NMP.Portal.Security;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public class WarningService : Service, IWarningService
    {
        private readonly ILogger<WarningService> _logger;
        public WarningService(ILogger<WarningService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
        {
            _logger = logger;
        }
        public async Task<(List<WarningCodeResponse>, Error)> FetchWarningCodeByFieldIdAndYear(
    string fieldIds, int harvestYear)
        {
            var warningCodes = new List<WarningCodeResponse>();
            var error = new Error();

            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(
                    string.Format(APIURLHelper.FetchWarningCodesByFieldIdAndYearAsyncAPI, fieldIds, harvestYear));

                string result = await response.Content.ReadAsStringAsync();

                ResponseWrapper? responseWrapper = null;

                try
                {
                    responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                }
                catch
                {
                    // invalid JSON
                    responseWrapper = null;
                }

                if (response.IsSuccessStatusCode)
                {
                    var warningList = responseWrapper?.Data?.ToObject<List<WarningCodeResponse>>();

                    if (warningList != null)
                    {
                        warningCodes.AddRange(warningList);
                    }
                }
                else
                {
                    var apiError = responseWrapper?.Error?.ToObject<Error>();

                    if (apiError != null)
                    {
                        error = apiError;
                        _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
                throw new HttpRequestException(error.Message, hre);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new InvalidOperationException(error.Message, ex);
            }

            return (warningCodes, error);
        }

    }
}
