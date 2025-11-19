using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Security;
using NMP.Portal.ServiceResponses;
using System.Text;

namespace NMP.Portal.Services
{
    public class PreviousCroppingService : Service, IPreviousCroppingService
    {
        private readonly ILogger<PreviousCroppingService> _logger;
        public PreviousCroppingService(ILogger<PreviousCroppingService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }

        public async Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year)
        {
            PreviousCropping? previousCropping = null;
            Error error = new Error();

            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchDataByFieldIdAndYearAsyncAPI, fieldId, year));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    previousCropping = responseWrapper.Data.PreviousCropping.ToObject<PreviousCropping>();
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = responseWrapper.Error.ToObject<ErrorViewModel>();
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

            return (previousCropping, error);
        }

        public async Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year)
        {
            List<PreviousCroppingData> previousCroppings = null;
            Error error = new Error();

            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = "";
                if(year==null)
                {
                    url = string.Format(APIURLHelper.FetchFieldDataByFieldIdAsyncAPI, fieldId);
                }
                else
                {
                    url = string.Format(APIURLHelper.FetchFieldDataByFieldIdOldestHarvestYearAsyncAPI, fieldId, year);
                }
                var response = await httpClient.GetAsync(url);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    previousCroppings = responseWrapper.Data.PreviousCropping.ToObject<List<PreviousCroppingData>>();
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = responseWrapper.Error.ToObject<ErrorViewModel>();
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

            return (previousCroppings, error);
        }
        public async Task<(bool, Error)> MergePreviousCropping(string jsonData)
        {
            bool success = false;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PutAsync(string.Format(APIURLHelper.MergePreviousCropAPI), new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    success = responseWrapper.Data.PreviousCropping;
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
            return (success, error);
        }
        public async Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId)
        {
            int? oldestYear = null;
            Error error = new Error();

            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchPreviousCroppingYearByFarmIdAsyncAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    oldestYear = responseWrapper.Data.OldestPreviousCropping.ToObject<int>();
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = responseWrapper.Error.ToObject<ErrorViewModel>();
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

            return (oldestYear, error);
        }
    }
}
