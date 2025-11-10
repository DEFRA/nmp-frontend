using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Text;

namespace NMP.Portal.Services
{
    public class PKBalanceService : Service, IPKBalanceService
    {
        private readonly ILogger<PKBalanceService> _logger;
        public PKBalanceService(ILogger<PKBalanceService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, Security.TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year, int fieldId)
        {
            PKBalance? pKBalance = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchPKBalanceByYearAndFieldIdAsyncAPI, year, fieldId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null&&responseWrapper.Data.PkBalances!=null)
                    {
                        pKBalance = new PKBalance();
                        pKBalance = responseWrapper.Data.ToObject<PKBalance>();
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
            return pKBalance;
        }
        //public async Task<(PKBalance, Error Error)> AddPKBalance(string pkBalanceData)
        //{
        //    string jsonData = JsonConvert.SerializeObject(pkBalanceData);
        //    PKBalance pKBalance = null;
        //    Error error = new Error();
        //    try
        //    {
        //        HttpClient httpClient = await GetNMPAPIClient();
        //        var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddFieldAsyncAPI,string.Empty), new StringContent(pkBalanceData, Encoding.UTF8, "application/json"));
        //        string result = await response.Content.ReadAsStringAsync();
        //        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        //        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
        //        {

        //            JObject pKBalanceJObject = responseWrapper.Data["PKBalance"] as JObject;
        //            if (pKBalanceJObject != null)
        //            {
        //                pKBalance = pKBalanceJObject.ToObject<PKBalance>();
        //            }

        //        }
        //        else
        //        {
        //            if (responseWrapper != null && responseWrapper.Error != null)
        //            {
        //                error = responseWrapper.Error.ToObject<Error>();
        //                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
        //            }
        //        }
        //    }
        //    catch (HttpRequestException hre)
        //    {
        //        error.Message = Resource.MsgServiceNotAvailable;
        //        _logger.LogError(hre.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        error.Message = ex.Message;
        //        _logger.LogError(ex.Message);
        //    }
        //    return (pKBalance, error);
        //}
        //public async Task<(PKBalance, Error Error)> UpdatePKBalance(string pkBalanceData)
        //{
        //    PKBalance pKBalance = null;
        //    Error error = new Error();
        //    try
        //    {
        //        HttpClient httpClient = await GetNMPAPIClient();
        //        var response = await httpClient.PutAsync(string.Format(APIURLHelper.UpdateFieldAsyncAPI, string.Empty), new StringContent(pkBalanceData, Encoding.UTF8, "application/json"));
        //        string result = await response.Content.ReadAsStringAsync();
        //        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        //        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
        //        {

        //            JObject pKBalanceJObject = responseWrapper.Data["PKBalance"] as JObject;
        //            if (pKBalanceJObject != null)
        //            {
        //                pKBalance = pKBalanceJObject.ToObject<PKBalance>();
        //            }

        //        }
        //        else
        //        {
        //            if (responseWrapper != null && responseWrapper.Error != null)
        //            {
        //                error = responseWrapper.Error.ToObject<Error>();
        //                _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
        //            }
        //        }
        //    }
        //    catch (HttpRequestException hre)
        //    {
        //        error.Message = Resource.MsgServiceNotAvailable;
        //        _logger.LogError(hre.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        error.Message = ex.Message;
        //        _logger.LogError(ex.Message);
        //    }
        //    return (pKBalance, error);
        //}
    }
}
