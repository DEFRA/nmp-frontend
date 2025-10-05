using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Security;
using NMP.Portal.ServiceResponses;
using System.Text;

namespace NMP.Portal.Services
{
    public class FertiliserManureService : Service, IFertiliserManureService
    {
        private readonly ILogger<FertiliserManureService> _logger;
        public FertiliserManureService(ILogger<FertiliserManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenAcquisitionService tokenAcquisitionService) : base(httpContextAccessor, clientFactory, tokenAcquisitionService)
        {
            _logger = logger;
        }
        public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId, int? cropOrder)
        {
            List<int> managementIds = new List<int>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = string.Empty;
                //if (cropTypeId != null)
                //{
                url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeIdAsyncAPI, harvestYear, cropTypeId, fieldIds, cropOrder);
                if (cropOrder == null)
                {
                    url = url.Replace("&cropOrder=", "");
                }
                if (cropTypeId == null)
                {
                    url = url.Replace("cropTypeId=&", "");
                }
                //}
                //else
                //{
                //    url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAsyncAPI, harvestYear, fieldIds, cropOrder);
                //    if (cropTypeId == null)
                //    {
                //        url = url.Replace("cropTypeId=&", "");
                //    }
                //}
                var response = await httpClient.GetAsync(url);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        List<CommonResponse> managementIdsList = responseWrapper.Data.ManagementPeriods.ToObject<List<CommonResponse>>();
                        managementIds.AddRange(managementIdsList.Select(x => x.Id));
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
            return (managementIds, error);
        }
        public async Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
        {
            List<ManureCropTypeResponse> cropTypeList = new List<ManureCropTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeByFarmIdAndHarvestYearAsyncAPI, harvestYear, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var cropTypeResponseList = responseWrapper.Data.ToObject<List<ManureCropTypeResponse>>();
                        cropTypeList.AddRange(cropTypeResponseList);
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
            return (cropTypeList, error);
        }
        public async Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId)
        {
            List<CommonResponse> fieldResponses = new List<CommonResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = string.Empty;
                if (cropTypeId != null)
                {
                    url = string.Format(APIURLHelper.FetchFieldByFarmIdAndHarvestYearAndCropTypeIdAsyncAPI, harvestYear, cropTypeId, farmId);
                }
                else
                {
                    url = string.Format(APIURLHelper.FetchFieldByFarmIdAndHarvestYearAsyncAPI, harvestYear, farmId);
                }
                var response = await httpClient.GetAsync(url);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var fieldResponseList = responseWrapper.Data.ToObject<List<CommonResponse>>();
                        fieldResponses.AddRange(fieldResponseList);
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
            return (fieldResponses, error);
        }

        public async Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations()
        {
            List<InOrganicManureDurationResponse> inOrganicManureDurationList = new List<InOrganicManureDurationResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchInOrganicManureDurationsAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var inOrganicManureDurationResponseList = responseWrapper.Data.InorganicManureDurations.ToObject<List<InOrganicManureDurationResponse>>();
                        if (inOrganicManureDurationResponseList != null)
                        {
                            inOrganicManureDurationList.AddRange(inOrganicManureDurationResponseList);
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
            return (inOrganicManureDurationList, error);
        }
        public async Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id)
        {
            InOrganicManureDurationResponse inOrganicManureDuration = new InOrganicManureDurationResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchInOrganicManureDurationsByIdAsyncAPI, id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var inOrganicManureDurationResponse = responseWrapper.Data.InorganicManureDuration.ToObject<InOrganicManureDurationResponse>();
                        if (inOrganicManureDurationResponse != null)
                        {
                            inOrganicManureDuration = inOrganicManureDurationResponse;
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
            return (inOrganicManureDuration, error);
        }

        public async Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure)
        {
            bool success = false;
            Error error = null;
            List<FertiliserManure> fertilisers = new List<FertiliserManure>();
            try
            {
                //string jsonString = JsonConvert.SerializeObject(fertiliserManure);
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddFertiliserManuresAsyncAPI), new StringContent(fertiliserManure, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<FertiliserManure> fertiliser = responseWrapper.Data.FertiliserManure.ToObject<List<FertiliserManure>>();
                    if (fertiliser != null && fertiliser.Count > 0)
                    {
                        fertilisers.AddRange(fertiliser);
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
            return (fertilisers, error);
        }
        public async Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate,int? fertiliserId, bool confirm)
        {
            Error error = null;
            decimal totalN = 0;
            string fromdate = startDate.ToString("yyyy-MM-dd");
            string toDate = endDate.ToString("yyyy-MM-dd");
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = APIURLHelper.FetchTotalNFromFertiliserBasedOnManIdAndAppDateAsyncAPI;

                if (fertiliserId.HasValue)
                {
                    url += $"&fertiliserId={fertiliserId.Value}";
                }

                url = string.Format(url, fieldId, fromdate, toDate, confirm);
                var response = await httpClient.GetAsync(url);
                //var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchTotalNFromFertiliserBasedOnManIdAndAppDateAsyncAPI, managementId, fromdate, toDate, fertiliserId, confirm));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        totalN = responseWrapper.Data.TotalN != null ? responseWrapper.Data.TotalN.ToObject<decimal>() : 0;
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
                if (error == null)
                {
                    error = new Error();
                }
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                if (error == null)
                {
                    error = new Error();
                }
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }
            return (totalN, error);
        }
        public async Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds)
        {
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var content = new StringContent(fertiliserIds, Encoding.UTF8, "application/json");
                var url = APIURLHelper.DeleteFertiliserByIdsAPI;


                var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
                {
                    Content = content
                };
                var response = await httpClient.SendAsync(requestMessage);
                //var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteFertiliserByIdsAPI, fertiliserIds));
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
        public async Task<(FertiliserManure, Error)> FetchFertiliserByIdAsync(int fertiliserId)
        {
            Error error = new Error();
            FertiliserManure fertiliserManure = new FertiliserManure();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFertiliserByIdAPI, fertiliserId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    var fertiliser = responseWrapper.Data.ToObject<FertiliserManure>();
                    if (fertiliser != null)
                    {
                        fertiliserManure = fertiliser;
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

            return (fertiliserManure, error);
        }
        public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId, int farmId, int harvestYear)
        {
            Error error = new Error();
            List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse = new List<FertiliserAndOrganicManureUpdateResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldWithSameDateAndNutrientAPI, fertiliserId,farmId,harvestYear));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    fertiliserResponse = responseWrapper.Data.ToObject<List<FertiliserAndOrganicManureUpdateResponse>>();                    
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

            return (fertiliserResponse, error);
        }
        public async Task<(List<FertiliserManure>, Error)> UpdateFertiliser(string fertliserData)
        {
            Error error = new Error();
            List<FertiliserManure> fertiliser = new List<FertiliserManure>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PutAsync(string.Format(APIURLHelper.UpdateFertiliserAPI), new StringContent(fertliserData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<FertiliserManure> fertilisers = responseWrapper.Data.FertiliserManure.ToObject<List<FertiliserManure>>();
                    if (fertilisers != null && fertilisers.Count > 0)
                    {
                        fertiliser.AddRange(fertilisers);
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

            return (fertiliser, error);
        }
        public async Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID)
        {
            Error error = null;
            decimal? totalN = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFertiliserTotalNByManagementPeriodIDAPI, managementPeriodID));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        totalN = responseWrapper.Data.TotalN;//!= null ? responseWrapper.Data.TotalN.ToObject<decimal>() : 0
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
                if (error == null)
                {
                    error = new Error();
                }
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                if (error == null)
                {
                    error = new Error();
                }
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }
            return (totalN, error);
        }
    }
}
