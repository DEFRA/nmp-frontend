using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Security;
using NMP.Portal.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace NMP.Portal.Services
{
    public class CropService : Service, ICropService
    {
        private readonly ILogger<CropService> _logger;
        public CropService(ILogger<CropService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<List<PotatoVarietyResponse>> FetchPotatoVarieties()
        {
            List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchPotatoVarietiesAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var potatoVarietyList = responseWrapper.Data.ToObject<List<PotatoVarietyResponse>>();
                        potatoVarieties.AddRange(potatoVarietyList);
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
            return potatoVarieties;
        }
        public async Task<int> FetchCropTypeByGroupId(int cropGroupId)
        {
            int cropTypeId = 0;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypesAsyncAPI, cropGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var cropTypeResponse = responseWrapper.Data.ToObject<List<CropTypeResponse>>();
                        if (cropTypeResponse != null)
                        {
                            cropTypeId = cropTypeResponse[0].CropTypeId;
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
            return cropTypeId;
        }
        public async Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId)
        {
            List<CropInfoOneResponse> cropInfoOneList = new List<CropInfoOneResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropInfoOneByCropTypeIdAsyncAPI, cropTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var cropInfoOneResponses = responseWrapper.Data.ToObject<List<CropInfoOneResponse>>();
                        cropInfoOneList.AddRange(cropInfoOneResponses);
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
            return cropInfoOneList;
        }
        public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeId()
        {
            List<CropInfoTwoResponse> cropInfoTwoList = new List<CropInfoTwoResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropInfoTwoByCropTypeIdAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var cropInfoTwoResponses = responseWrapper.Data.ToObject<List<CropInfoTwoResponse>>();
                        cropInfoTwoList.AddRange(cropInfoTwoResponses);
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
            return cropInfoTwoList;
        }
        public async Task<(bool, Error)> AddCropNutrientManagementPlan(CropDataWrapper cropData)
        {
            string jsonData = JsonConvert.SerializeObject(cropData);
            bool success = false;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddCropNutrientManagementPlanAsyncAPI), new StringContent(jsonData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    var cropResponsss = responseWrapper.Data.Recommendations;
                    if(cropResponsss!=null)
                    {
                        success = true;
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
            return (success, error);
        }

        public async Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId, int type)
        {
            List<PlanSummaryResponse> planSummaryList = new List<PlanSummaryResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchPlanSummaryByFarmIdAsyncAPI, farmId,type));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var planSummaryResponses = responseWrapper.Data.ToObject<List<PlanSummaryResponse>>();
                        planSummaryList.AddRange(planSummaryResponses);
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
            return planSummaryList;
        }

        public async Task<(List<HarvestYearPlanResponse>, Error)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId)
        {
            List<HarvestYearPlanResponse> harvestYearPlanList = new List<HarvestYearPlanResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchHarvestYearPlansByFarmIdAsyncAPI, harvestYear,farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var harvestYearPlanResponses = responseWrapper.Data.ToObject<List<HarvestYearPlanResponse>>();
                        harvestYearPlanList.AddRange(harvestYearPlanResponses);
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
            return (harvestYearPlanList, error);
        }
        public async Task<(List<RecommendationHeader>, Error)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear)
        {
            List<RecommendationHeader> recommendationList = new List<RecommendationHeader>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchRecommendationByFieldIdAndYearAsyncAPI, fieldId, harvestYear));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var recommendationsList = responseWrapper.Data.Recommendations.ToObject<List<RecommendationHeader>>();
                        recommendationList.AddRange(recommendationsList);
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
                error= new Error();
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
            return (recommendationList, error);
        }

        public async Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(int cropTypeId,int cropInfo1Id)
        {
            Error error = null;
            string cropInfo1Name = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAsyncAPI, cropTypeId, cropInfo1Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    cropInfo1Name = responseWrapper.Data["cropInfo1Name"];
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
            return cropInfo1Name;
        }
        public async Task<string> FetchCropInfo2NameByCropInfo2Id(int cropInfo2Id)
        {
            Error error = null;
            string cropInfo2Name = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropInfo2NameByCropInfo2IdAsyncAPI,cropInfo2Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    cropInfo2Name = responseWrapper.Data["cropInfo2Name"];
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
            return cropInfo2Name;
        }
        public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
        {
            List<Crop> cropList = new List<Crop>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropsByFieldIdAsyncAPI, fieldId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var crops = responseWrapper.Data.Crops.records.ToObject<List<Crop>>();
                        cropList.AddRange(crops);
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
            return cropList;
        }

        public async Task<decimal> FetchCropTypeDefaultYieldByCropTypeId(int cropTypeId)
        {
            decimal? defaultYield = 0;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeLinkingsByCropTypeIdAsyncAPI, cropTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        CropTypeLinkingResponse cropTypeLinkingResponse = responseWrapper.Data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
                        defaultYield = cropTypeLinkingResponse.DefaultYield;
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
            return defaultYield??0;
        }

        public async Task<List<int>> FetchSecondCropListByFirstCropId(int firstCropTypeId)
        {
            List<int> secondCropList=new List<int>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSecondCropListByFirstCropIdAsyncAPI, firstCropTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var secondCrops = responseWrapper.Data.SecondCropID.ToObject<List<int>>();
                        secondCropList.AddRange(secondCrops);
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
            return secondCropList;
        }
        public async Task<(HarvestYearResponseHeader, Error)> FetchHarvestYearPlansDetailsByFarmId(int harvestYear, int farmId)
        {
            HarvestYearResponseHeader harvestYearPlan = new HarvestYearResponseHeader();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropsOrganicinorganicdetailsByYearFarmIdAsyncAPI, harvestYear, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        harvestYearPlan = responseWrapper.Data.ToObject<HarvestYearResponseHeader>();
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
            return (harvestYearPlan, error);
        }

        public async Task<string?> FetchCropInfoOneQuestionByCropTypeId(int cropTypeId)
        {
            string? cropInfoOneQuestion = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropInfoOneQuestionByCropTypeIdAsyncAPI, cropTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        cropInfoOneQuestion = responseWrapper.Data.CropTypeQuestion.ToObject<string>();
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
            return cropInfoOneQuestion;
        }
        public async Task<(ManagementPeriod, Error)> FetchManagementperiodById(int id)
        {
            ManagementPeriod managementPeriod = new ManagementPeriod();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManagementperiodByIdAsyncAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        managementPeriod = responseWrapper.Data.ManagementPeriods.ToObject<ManagementPeriod>();
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
            return (managementPeriod, error);
        }
        public async Task<(Crop, Error)> FetchCropById(int id)
        {
            Crop crop = new Crop();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropByIdAsyncAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        crop = responseWrapper.Data.ToObject<Crop>();
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
            return (crop, error);
        }
        public async Task<(string, Error)> RemoveCropPlan(List<int> cropIds)
        {
            var cropIdsRequest = new { cropIds };
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var jsonContent = JsonConvert.SerializeObject(cropIdsRequest);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var url = string.Format(APIURLHelper.DeleteCropPlanByIdsAPI, ""); 

                
                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
                {
                    Content = content
                };
                
                var response = await httpClient.SendAsync(requestMessage);
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
        public async Task<(bool,Error)> IsCropsGroupNameExistForUpdate(string cropIds, string cropGroupName, int year, int farmId)
        {
            bool isCropsGroupNameExist = false;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropGroupNameByCropIdGroupNameAndYearAPI, cropIds, cropGroupName, year, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null&&responseWrapper.Data == true)
                {
                    isCropsGroupNameExist = true;
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

            return (isCropsGroupNameExist,error);
        }
        public async Task<(List<Crop>, Error)> UpdateCrop(string cropData)
        {
            List<Crop> crops = new List<Crop>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PutAsync(string.Format(APIURLHelper.UpdateCropAPI) , new StringContent(cropData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    var cropResponse = responseWrapper.Data.updatedCrops.ToObject<List<Crop>>();
                    if (cropResponse != null)
                    {
                        crops = cropResponse;
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
            return (crops, error);
        }

        public async Task<List<GrassSeasonResponse>> FetchGrassSeasons()
        {
            List<GrassSeasonResponse> grassSeasons = new List<GrassSeasonResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchGrassSeasonsAsyncAPI,3));  //3 is country id
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var grassSeasonsList = responseWrapper.Data.ToObject<List<GrassSeasonResponse>>();
                        grassSeasons.AddRange(grassSeasonsList);
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
            return grassSeasons;
        }

        public async Task<(List<GrassGrowthClassResponse>, Error)> FetchGrassGrowthClass(List<int> fieldIds)
        {
            var fieldIdsRequest = new { fieldIds };
            Error error = new Error();
            string message = string.Empty;
            List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var jsonContent = JsonConvert.SerializeObject(fieldIdsRequest);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var url = APIURLHelper.FetchGrassGrowthClassesAsyncAPI;


                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                var response = await httpClient.SendAsync(requestMessage);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var grassGrowthClassList = responseWrapper.Data.ToObject<List<GrassGrowthClassResponse>>();
                        grassGrowthClasses.AddRange(grassGrowthClassList);
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
            return (grassGrowthClasses, error);
        }

        public async Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropId(int cropId, bool isShortSummary)
        {
            List<ManagementPeriod> managementPeriodList = new List<ManagementPeriod>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManagementPeriodByCropIdAsyncAPI, cropId, isShortSummary));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        managementPeriodList = responseWrapper.Data.ManagementPeriods.ToObject<List<ManagementPeriod>>();
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
            return (managementPeriodList, error);
        }
        //grass
        public async Task<(List<DefoliationSequenceResponse>,Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(int swardTypeId, int swardManagementId, int numberOfCut,bool isNewSward)
        {
            Error error = null;
            List<DefoliationSequenceResponse> defoliationSequenceResponses = new List<DefoliationSequenceResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchDefoliationSequencesBySwardTypeIdAndNumberOfCutAsyncAPI, swardTypeId, swardManagementId, numberOfCut, isNewSward));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    var defoliationSequenceList = responseWrapper.Data.ToObject<List<DefoliationSequenceResponse>>();
                    defoliationSequenceResponses.AddRange(defoliationSequenceList);
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
            return (defoliationSequenceResponses,error);
        }

        public async Task<(List<PotentialCutResponse>,Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementId(int swardTypeId, int swardManagementId)
        {
            Error error = null;
            List<PotentialCutResponse> potentialCuts = new List<PotentialCutResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchPotentialCutsBySwardTypeIdAndSwardManagementIdAsyncAPI, swardTypeId, swardManagementId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    var potentialCutList = responseWrapper.Data.ToObject<List<PotentialCutResponse>>();
                    potentialCuts.AddRange(potentialCutList);
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
            return (potentialCuts,error);
        }

        public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagements()
        {
            List<SwardManagementResponse> swardManagementResponses = new List<SwardManagementResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSwardManagementsAsyncAPI)); 
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var swardManagementList = responseWrapper.Data.ToObject<List<SwardManagementResponse>>();
                        swardManagementResponses.AddRange(swardManagementList);
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
            return (swardManagementResponses, error);
        }

        public async Task<(List<SwardTypeResponse>, Error)> FetchSwardTypes()
        {
            List<SwardTypeResponse> swardTypeResponses = new List<SwardTypeResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSwardTypesAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var swardTypeResponseList = responseWrapper.Data.ToObject<List<SwardTypeResponse>>();
                        swardTypeResponses.AddRange(swardTypeResponseList);
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
            return (swardTypeResponses, error);
        }

        public async Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(int sequenceId, int grassGrowthClassId)
        {
            Error error = null;
            List<YieldRangesEnglandAndWalesResponse> yieldRanges = new List<YieldRangesEnglandAndWalesResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdAsyncAPI, sequenceId, grassGrowthClassId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    var yieldRangesList = responseWrapper.Data.ToObject<List<YieldRangesEnglandAndWalesResponse>>();
                    yieldRanges.AddRange(yieldRangesList);
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
            return (yieldRanges,error);
        }
        public async Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesById(int defoliationId)
        {
            Error error = null;
            DefoliationSequenceResponse defoliationSequenceResponse = new DefoliationSequenceResponse();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchDefoliationSequencesByIdAsyncAPI, defoliationId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    defoliationSequenceResponse = responseWrapper.Data.ToObject<DefoliationSequenceResponse>();                    
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
            return (defoliationSequenceResponse, error);
        }

        public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementId(int swardManagementId)
        {
            Error error = null;
            SwardManagementResponse swardManagementResponse = new SwardManagementResponse();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSwardManagementBySwardManagementIdAsyncAPI, swardManagementId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    swardManagementResponse = responseWrapper.Data.ToObject<SwardManagementResponse>();
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
            return (swardManagementResponse, error);
        }
        public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeId(int swardTypeId)
        {
            Error error = null;
            List<SwardManagementResponse> swardManagementResponse = new List<SwardManagementResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSwardManagementBySwardTypeIdAsyncAPI, swardTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    swardManagementResponse = responseWrapper.Data.ToObject<List<SwardManagementResponse>>();
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
            return (swardManagementResponse, error);
        }

        public async Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeId(int swardTypeId)
        {
            Error error = null;
            SwardTypeResponse swardTypeResponse = new SwardTypeResponse();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSwardTypeBySwardTypeIdAsyncAPI, swardTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    swardTypeResponse = responseWrapper.Data.ToObject<SwardTypeResponse>();
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
            return (swardTypeResponse, error);
        }
        public async Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinking()
        {
            Error error = null;
            List<CropTypeLinkingResponse> cropTypeLinkingResponse = new List<CropTypeLinkingResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchCropTypeLinkingsAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if ((response.IsSuccessStatusCode && responseWrapper != null) || responseWrapper.Data != null)
                {
                    cropTypeLinkingResponse = responseWrapper.Data.CropTypeLinking.records.ToObject<List<CropTypeLinkingResponse>>();
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
            return (cropTypeLinkingResponse, error);
        }

        public async Task<(bool, Error)> CopyCropNutrientManagementPlan(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser)
        {
            bool success = false;
            Error error = new Error();
            try
            {
                var requestData = new
                {
                    farmID = farmID,
                    harvestYear = harvestYear,
                    copyYear = copyYear,
                    isOrganic = isOrganic,
                    isFertiliser = isFertiliser
                };

                string jsonData = JsonConvert.SerializeObject(requestData);
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(string.Format(APIURLHelper.CopyCropNutrientManagementPlanAsyncAPI), new StringContent(jsonData, Encoding.UTF8, "application/json"));

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    var cropResponses = responseWrapper.Data.Recommendations;
                    if (cropResponses != null)
                    {
                        success = true;
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
            return (success, error);
        }

        public async Task<(bool, Error)> MergeCrop(string cropData)
        {
            bool success = false;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PutAsync(string.Format(APIURLHelper.MergeCropAPI), new StringContent(cropData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    success = responseWrapper.Data;
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

    }

}
