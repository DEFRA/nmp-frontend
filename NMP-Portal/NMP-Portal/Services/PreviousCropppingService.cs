using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Security;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class PreviousCropppingService : Service, IPreviousCropppingService
    {
        private readonly ILogger<PreviousCropppingService> _logger;
        public PreviousCropppingService(ILogger<PreviousCropppingService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }

        public async Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year)
        {
            PreviousCropping previousCroppping = new PreviousCropping();
            Error error = new Error();

            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchDataByFieldIdAndYearAsyncAPI, fieldId,year));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                         previousCroppping = responseWrapper.Data.ToObject<PreviousCropping>();                        
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

            return (previousCroppping, error);
        }

    }
}
