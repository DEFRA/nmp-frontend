using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Text;

namespace NMP.Portal.Services
{
    public class SoilAnalysisService : Service, ISoilAnalysisService
    {
        private readonly ILogger<SoilAnalysisService> _logger;
        public SoilAnalysisService(ILogger<SoilAnalysisService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base
        (httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id)
        {
            SoilAnalysis soilAnalysis = new SoilAnalysis();
            Error error = null;
            try
            {
                _logger.LogTrace($"SoilAnalysisService: soil-analyses/{id} called.");
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilAnalysisByIdAsyncAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    JObject soilanalysis = responseWrapper.Data["SoilAnalysis"] as JObject;
                    if (soilanalysis != null)
                    {
                        soilAnalysis = soilanalysis.ToObject<SoilAnalysis>();
                    }
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = new Error();
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
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                error = new Error();
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }

            return (soilAnalysis, error);
        }
        public async Task<(SoilAnalysis, Error)> UpdateSoilAnalysisAsync(int id, string soilData)
        {
            SoilAnalysis soilAnalysis = null;
            Error error = new Error();
            try
            {
                _logger.LogTrace($"SoilAnalysisService: soil-analyses/{id}/{soilData} called.");
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PutAsync(string.Format(APIURLHelper.UpdateSoilAnalysisAsyncAPI, id), new StringContent(soilData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    JObject soilAnalysisJObject = responseWrapper.Data["SoilAnalysis"] as JObject;
                    if (soilAnalysisJObject != null)
                    {
                        soilAnalysis = soilAnalysisJObject.ToObject<SoilAnalysis>();
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
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }
            return (soilAnalysis, error);
        }

        public async Task<(SoilAnalysis, Error)> AddSoilAnalysisAsync(string soilAnalysisData)
        {
            SoilAnalysis? soilAnalysis = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();


                var response = await httpClient.PostAsync(APIURLHelper.AddSoilAnalysisAsyncAPI, new StringContent(soilAnalysisData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject soilAnalysisJObject = responseWrapper.Data["soilAnalysis"] as JObject;
                    if (soilAnalysisJObject != null)
                    {
                        soilAnalysis = soilAnalysisJObject.ToObject<SoilAnalysis>();
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
            return (soilAnalysis, error);
        }

        public async Task<(string, Error)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
        {
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteSoilAnalysisByIdAPI, soilAnalysisId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    message = responseWrapper.Data["message"].Value;
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

            return (message, error);
        }
    }
}