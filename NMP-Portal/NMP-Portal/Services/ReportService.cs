using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.ViewModels;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace NMP.Portal.Services
{
    public class ReportService : Service, IReportService
    {
        private readonly ILogger<FarmService> _logger;
        public ReportService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, Security.TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails)
        {
            string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetails);
            NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PostAsync(APIURLHelper.AddNutrientsLoadingFarmDetailsAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject nutrientsLoadingFarmDetailsJObject = responseWrapper.Data["NutrientsLoadingFarmDetails"] as JObject;
                    if (nutrientsLoadingFarmDetailsJObject != null)
                    {
                        nutrientsLoadingFarmDetail = nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>();
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
            return (nutrientsLoadingFarmDetail, error);
        }
        public async Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year)
        {
            NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsLoadingFarmDetailsByfarmIdAndYearAPI, farmId, year));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    JObject nutrientsLoadingFarmDetailsJObject = responseWrapper.Data["NutrientsLoadingFarmDetails"] as JObject;
                    if (nutrientsLoadingFarmDetailsJObject != null)
                    {
                        nutrientsLoadingFarmDetail = nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>();
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
            return (nutrientsLoadingFarmDetail, error);
        }
        public async Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails)
        {
            string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetails);
            NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PutAsync(APIURLHelper.UpdateNutrientsLoadingFarmDetailsAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject nutrientsLoadingFarmDetailsJObject = responseWrapper.Data["NutrientsLoadingFarmDetails"] as JObject;
                    if (nutrientsLoadingFarmDetailsJObject != null)
                    {
                        nutrientsLoadingFarmDetail = nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>();
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
            return (nutrientsLoadingFarmDetail, error);
        }
        public async Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmId(int farmId)
        {
            Error error = new Error();
            List<NutrientsLoadingManures> NutrientsLoadingManuresList = new List<NutrientsLoadingManures>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsloadingFarmDetailsFarmIdAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var nutrientsLoadingManures = responseWrapper.Data.NutrientsLoadingFarmDetails.ToObject<List<NutrientsLoadingManures>>();
                        if (nutrientsLoadingManures != null)
                        {
                            NutrientsLoadingManuresList = nutrientsLoadingManures;
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
            return (NutrientsLoadingManuresList, error);
        }
        public async Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
        {
            NutrientsLoadingManures nutrientsLoadingManureData = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PostAsync(APIURLHelper.AddNutrientsLoadingManureAPI, new StringContent(nutrientsLoadingManure, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject nutrientsLoadingManureDataObj = responseWrapper.Data as JObject;
                    if (nutrientsLoadingManureDataObj != null)
                    {
                        nutrientsLoadingManureData = nutrientsLoadingManureDataObj.ToObject<NutrientsLoadingManures>();
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
            return (nutrientsLoadingManureData, error);
        }
        public async Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmId(int farmId)
        {
            Error error = new Error();
            List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetailList = new List<NutrientsLoadingFarmDetail>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsLoadingFarmDetailsByFarmIdAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var nutrientsLoadingFarmDetail = responseWrapper.Data.ToObject<List<NutrientsLoadingFarmDetail>>();
                        if (nutrientsLoadingFarmDetail != null)
                        {
                            nutrientsLoadingFarmDetailList = nutrientsLoadingFarmDetail;
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
            return (nutrientsLoadingFarmDetailList, error);
        }

        public async Task<(List<CommonResponse>, Error)> FetchLivestockGroupList()
        {
            List<CommonResponse> livestockGroupList = new List<CommonResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchLivestockGroupListAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var livestockGroups = responseWrapper.Data.LivestockGroups.ToObject<List<CommonResponse>>();
                        livestockGroupList.AddRange(livestockGroups);
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
            return (livestockGroupList, error);
        }

        public async Task<(CommonResponse, Error)> FetchLivestockGroupById(int livestockGroupId)
        {
            CommonResponse livestockGroup = new CommonResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchLivestockManureGroupByIdAsyncAPI, livestockGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        livestockGroup = responseWrapper.Data.livestockGroup.ToObject<CommonResponse>();
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
            return (livestockGroup, error);
        }
        public async Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id)
        {
            Error error = new Error();
            NutrientsLoadingManures NutrientsLoadingManure = new NutrientsLoadingManures();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsLoadingManureByIdAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        NutrientsLoadingManure = responseWrapper.Data.NutrientsLoadingManure.ToObject<NutrientsLoadingManures>();
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
            return (NutrientsLoadingManure, error);
        }
        public async Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
        {
            NutrientsLoadingManures NutrientsLoadingManureData = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PutAsync(APIURLHelper.UpdateNutrientsLoadingManureAsyncAPI, new StringContent(nutrientsLoadingManure, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    JObject nutrientsLoadingManureDataObj = responseWrapper.Data as JObject;
                    if (nutrientsLoadingManureDataObj != null)
                    {
                        NutrientsLoadingManureData = nutrientsLoadingManureDataObj.ToObject<NutrientsLoadingManures>();
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
            return (NutrientsLoadingManureData, error);
        }

        public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupId(int livestockGroupId)
        {
            List<LivestockTypeResponse> livestockTypeList = new List<LivestockTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchLivestockTypesByGroupIdAsyncAPI, livestockGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var livestockTypes = responseWrapper.Data.livestockTypes.ToObject<List<LivestockTypeResponse>>();
                        livestockTypeList.AddRange(livestockTypes);
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
            return (livestockTypeList, error);
        }
        public async Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId)
        {
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteNutrientsLoadingManuresByIdAPI, nutrientsLoadingManureId));
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

        public async Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStock)
        {
            string jsonData = JsonConvert.SerializeObject(nutrientsLoadingLiveStock);
            NutrientsLoadingLiveStock nutrientsLoadingLiveStocks = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PostAsync(APIURLHelper.AddNutrientsLoadingLivestockAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject nutrientsLoadingLiveStocksJObject = responseWrapper.Data["NutrientsLoadingLiveStock"] as JObject;
                    if (nutrientsLoadingLiveStocksJObject != null)
                    {
                        nutrientsLoadingLiveStocks = nutrientsLoadingLiveStocksJObject.ToObject<NutrientsLoadingLiveStock>();
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
            return (nutrientsLoadingLiveStocks, error);
        }

        public async Task<(List<NutrientsLoadingLiveStock>, Error)> FetchLivestockByFarmIdAndYear(int farmId, int year)
        {
            Error error = new Error();
            List<NutrientsLoadingLiveStock> nutrientsLoadingLiveStockList = new List<NutrientsLoadingLiveStock>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsLoadingLivestockByFarmIdAndYearAsyncAPI, farmId,year));

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var nutrientsLoadingLiveStock = responseWrapper.Data.ToObject<List<NutrientsLoadingLiveStock>>();
                        if (nutrientsLoadingLiveStock != null)
                        {
                            nutrientsLoadingLiveStockList = nutrientsLoadingLiveStock;
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
            return (nutrientsLoadingLiveStockList, error);
        }
        public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypes()
        {
            List<LivestockTypeResponse> livestockTypeList = new List<LivestockTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchLivestockTypesAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var livestockTypes = responseWrapper.Data.LivestockTypes.ToObject<List<LivestockTypeResponse>>();
                        livestockTypeList.AddRange(livestockTypes);
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
            return (livestockTypeList, error);
        }

        public async Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id)
        {
            Error error = new Error();
            NutrientsLoadingLiveStock nutrientsLoadingLiveStock = new NutrientsLoadingLiveStock();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsLoadingLiveStockByIdAsyncAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        nutrientsLoadingLiveStock = responseWrapper.Data.records.ToObject<NutrientsLoadingLiveStock>();
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
            return (nutrientsLoadingLiveStock, error);
        }

        public async Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId)
        {
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteNutrientsLoadingLivestockByIdAPI, nutrientsLoadingLivestockId));
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
