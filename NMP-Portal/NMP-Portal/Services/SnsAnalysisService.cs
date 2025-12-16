using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using System.Net;
using System.Text;

namespace NMP.Portal.Services
{
    public class SnsAnalysisService:Service,ISnsAnalysisService
    {
        private readonly ILogger<SnsAnalysisService> _logger;
        public SnsAnalysisService(ILogger<SnsAnalysisService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, Security.TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
        {
            _logger = logger;
        }

        public async Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId)
        {
            SnsAnalysis snsAnalysis = new SnsAnalysis();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSnsAnalysisByCropIdAsyncAPI, cropId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        if(responseWrapper.Data.snsAnalyses.records.Count>0)
                        {
                            snsAnalysis = responseWrapper.Data.snsAnalyses.records[0].ToObject<SnsAnalysis>();

                        }
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
            return snsAnalysis;
        }

        public async Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData)
        {
            //string jsonData = JsonConvert.SerializeObject(snsData);
            string jsonData = JsonConvert.SerializeObject(new { SnsAnalysis = snsData }, Formatting.Indented);

            SnsAnalysis snsAnalysis = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                
                    var response = await httpClient.PostAsync(APIURLHelper.AddSnsAnalysisAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                    {

                        JObject farmDataJObject = responseWrapper.Data["snsAnalysis"] as JObject;
                        if (snsData != null)
                        {
                           snsAnalysis = farmDataJObject.ToObject<SnsAnalysis>();
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
            return (snsAnalysis, error);
        }
        public async Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId)
        {
            string message = string.Empty;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteSNSAnalysisAPI, snsAnalysisId));
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
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }
            return (message, error);
        }
    }
}
