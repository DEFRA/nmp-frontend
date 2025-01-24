using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Security.Cryptography;
using System.Text;

namespace NMP.Portal.Services
{
    public class OrganicManureService : Service, IOrganicManureService
    {
        private readonly ILogger<OrganicManureService> _logger;
        public OrganicManureService(ILogger<OrganicManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
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

        public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId, int? cropOrder)
        {
            List<int> managementIds = new List<int>();
            Error error = null;
            if(cropOrder==null)
            {
                cropOrder = 1;
            }
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = string.Empty;
                if (cropTypeId != null)
                {
                    url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeIdAsyncAPI, harvestYear, cropTypeId, fieldIds,cropOrder);
                }
                else
                {
                    url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAsyncAPI, harvestYear, fieldIds);
                }
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

        public async Task<(List<CommonResponse>, Error)> FetchManureGroupList()
        {
            List<CommonResponse> manureGroupList = new List<CommonResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchMannerManureGroupListAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var manureGroups = responseWrapper.Data.ToObject<List<CommonResponse>>();
                        manureGroupList.AddRange(manureGroups);
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
            return (manureGroupList, error);
        }
        public async Task<(List<ManureType>, Error)> FetchManureTypeList(int manureGroupId, int countryId)
        {
            List<ManureType> manureTypeList = new List<ManureType>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureTypeListByGroupIdAndCountryAsyncAPI, manureGroupId, countryId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var manureTypes = responseWrapper.Data.ToObject<List<ManureType>>();
                        manureTypeList.AddRange(manureTypes);
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
            return (manureTypeList, error);
        }
        public async Task<(CommonResponse, Error)> FetchManureGroupById(int manureGroupId)
        {
            CommonResponse manureGroup = new CommonResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureGroupByIdAsyncAPI, manureGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        manureGroup = responseWrapper.Data.ToObject<CommonResponse>();
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
            return (manureGroup, error);
        }

        public async Task<(ManureType, Error)> FetchManureTypeByManureTypeId(int manureTypeId)
        {
            ManureType manureType = new ManureType();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerManureTypeByManureTypeIdAsyncAPI, manureTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        manureType = responseWrapper.Data.ToObject<ManureType>();
                        //manureTypeList.AddRange(manureTypes);
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
            return (manureType, error);
        }

        public async Task<(List<ApplicationMethodResponse>, Error)> FetchApplicationMethodList(int fieldType, bool isLiquid)
        {
            List<ApplicationMethodResponse> applicationMethodList = new List<ApplicationMethodResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerApplicationMethodsByApplicableForAsyncAPI, isLiquid, fieldType));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var applicationMethods = responseWrapper.Data.ToObject<List<ApplicationMethodResponse>>();
                        applicationMethodList.AddRange(applicationMethods);
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
            return (applicationMethodList, error);
        }

        public async Task<(List<IncorporationMethodResponse>, Error)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor)
        {
            List<IncorporationMethodResponse> incorporationMethods = new List<IncorporationMethodResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationMethodsByApplicationIdAsyncAPI, appId, applicableFor));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var methods = responseWrapper.Data.ToObject<List<IncorporationMethodResponse>>();
                        incorporationMethods.AddRange(methods);
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
            return (incorporationMethods, error);
        }
        public async Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
        {
            List<IncorprationDelaysResponse> incorporationDelays = new List<IncorprationDelaysResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationDelaysByMethodIdAndApplicableForAsyncAPI, methodId,applicableFor));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var delays = responseWrapper.Data.ToObject<List<IncorprationDelaysResponse>>();
                        incorporationDelays.AddRange(delays);
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
            return (incorporationDelays, error);
        }
        public async Task<(string, Error)> FetchApplicationMethodById(int Id)
        {
            string applicationMethod = string.Empty;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerApplicationMethodByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        applicationMethod = responseWrapper.Data.name;

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
            return (applicationMethod, error);
        }
        public async Task<(string, Error)> FetchIncorporationMethodById(int Id)
        {
            string incorporationMethod = string.Empty;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationMethodByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)// && responseWrapper.Data.IncorporationMethod != null
                    {
                        incorporationMethod = responseWrapper.Data.name;//.IncorporationMethod.Name;

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
            return (incorporationMethod, error);
        }
        public async Task<(string, Error)> FetchIncorporationDelayById(int Id)
        {
            string incorporationDelay = string.Empty;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerIncorporationDelaysByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)// && responseWrapper.Data.IncorporationDelay != null
                    {
                        incorporationDelay = responseWrapper.Data.name;//.IncorporationDelay.Name;

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
            return (incorporationDelay, error);
        }
        public async Task<(bool, Error)> AddOrganicManuresAsync(string organicManureData)
        {
            bool success = false;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddOrganicManuresAsyncAPI), new StringContent(organicManureData, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    var organicManures = responseWrapper.Data.OrganicManures;

                    if (organicManures != null)
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
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }
            return (success, error);
        }
        public async Task<(RainTypeResponse, Error)> FetchRainTypeDefault()
        {
            RainTypeResponse rainType = new RainTypeResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchRainTypeDefault);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        rainType = responseWrapper.Data.ToObject<RainTypeResponse>();
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
            return (rainType, error);
        }
        public async Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString)
        {
            int totalRainfall = 0;
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(string.Format(APIURLHelper.FetchMannerRainfallByPostcodeAndDateRangeAsyncAPI), new StringContent(jsonString, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        totalRainfall = responseWrapper.Data.rainfallPostApplication!=null? responseWrapper.Data.rainfallPostApplication.value.ToObject<int>():0;
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
            return totalRainfall;
        }

        public async Task<(WindspeedResponse, Error)> FetchWindspeedDataDefault()
        {
            WindspeedResponse windSpeed = new WindspeedResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchWindspeedDataDefault);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        windSpeed = responseWrapper.Data.ToObject<WindspeedResponse>();
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
            return (windSpeed, error);
        }

        public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate)
        {
            MoistureTypeResponse moisterType = new MoistureTypeResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMoisterTypeDefaultByApplicationDate, applicationDate));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        moisterType = responseWrapper.Data.MoistureType.ToObject<MoistureTypeResponse>();
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
            return (moisterType, error);
        }

        public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeList()
        {
            List<RainTypeResponse> rainType = new List<RainTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchMannerRainTypesAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var rainTypes = responseWrapper.Data.ToObject<List<RainTypeResponse>>();
                        rainType.AddRange(rainTypes);
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
            return (rainType, error);
        }
        public async Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId)
        {
            RainTypeResponse rainType = new RainTypeResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerRainTypeByIdAsyncAPI,rainTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        rainType = responseWrapper.Data.ToObject<RainTypeResponse>();
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
            return (rainType, error);
        }

        public async Task<(List<WindspeedResponse>, Error)> FetchWindspeedList()
        {
            List<WindspeedResponse> windspeeds = new List<WindspeedResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchMannerWindspeedsAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var windspeed = responseWrapper.Data.ToObject<List<WindspeedResponse>>();
                        windspeeds.AddRange(windspeed);
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
            return (windspeeds, error);
        }
        public async Task<(WindspeedResponse, Error)> FetchWindspeedById(int windspeedId)
        {
            WindspeedResponse windspeedResponse = new WindspeedResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerWindspeedByIdAsyncAPI, windspeedId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        windspeedResponse = responseWrapper.Data.ToObject<WindspeedResponse>();
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
            return (windspeedResponse, error);
        }
        public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList()
        {
            List<MoistureTypeResponse> moisterTypes = new List<MoistureTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(APIURLHelper.FetchMannerMoistureTypesAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var moisterType = responseWrapper.Data.ToObject<List<MoistureTypeResponse>>();
                        moisterTypes.AddRange(moisterType);
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
            return (moisterTypes, error);
        }
        public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId)
        {
            MoistureTypeResponse moistureTypeResponse = new MoistureTypeResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerMoistureTypeByIdAsyncAPI, moisterTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        moistureTypeResponse = responseWrapper.Data.ToObject<MoistureTypeResponse>();
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
            return (moistureTypeResponse, error);
        }
        public async Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId)
        {
            Error error = new Error();
            bool isPerennial = false;
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
                        isPerennial = cropTypeLinkingResponse.IsPerennial ?? false;
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
            return isPerennial;
        }

        public async Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate, bool confirm)
        {
            Error error = null;
            decimal totalN = 0;
            string fromdate = startDate.ToString("yyyy-MM-dd");
            string toDate = endDate.ToString("yyyy-MM-dd");
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchTotalNBasedOnManIdAndAppDateAsyncAPI, managementId, fromdate, toDate, confirm));
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

        public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
        {
            CropTypeResponse cropType = new CropTypeResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeByFieldIdAndHarvestYearAsyncAPI, fieldId, year, confirm));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        cropType = responseWrapper.Data.ToObject<CropTypeResponse>();
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
            return (cropType, error);
        }
        public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId)
        {
            CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeLinkingByCropTypeIdAsyncAPI, cropTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        cropTypeLinking = responseWrapper.Data.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
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
            return (cropTypeLinking, error);
        }
        public async Task<(List<int>, Error)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year, bool confirm)
        {
            List<int> manureTypeIds = new List<int>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsyncAPI, fieldId, year, confirm));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        //manureTypeIds = responseWrapper.Data.manureTypeIds.ToObject<List<int>>();
                        manureTypeIds = responseWrapper.Data.ManureTypeIds.ToObject<List<int>>();
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
            return (manureTypeIds, error);
        }

        public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm)
        {
            Error error = null;
            decimal totalN = 0;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsyncAPI, managementId, confirm));
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

        public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(string dateFrom, string dateTo, bool isConfirm)
        {
            Error error = null;
            bool isOrganicManureExist = false;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchOrganicManureExistanceByDateRangeAsyncAPI, dateFrom, dateTo, isConfirm));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        isOrganicManureExist = responseWrapper.Data.exists.ToObject<bool>();
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
            return (isOrganicManureExist, error);
        }

        public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString)
        {
            Error error = null;
            NitrogenUptakeResponse nitrogenUptakeResponse = new NitrogenUptakeResponse();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(string.Format(APIURLHelper.FetchMannerAutumnCropNitrogenUptakeAsyncAPI), new StringContent(jsonString, Encoding.UTF8, "application/json"));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        nitrogenUptakeResponse = responseWrapper.Data.nitrogenUptake != null ? responseWrapper.Data.nitrogenUptake.ToObject<NitrogenUptakeResponse>() : 0;
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
            return (nitrogenUptakeResponse, error);
        }
        public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmId(int farmId)
        {
            List<FarmManureTypeResponse> farmManureTypes = new List<FarmManureTypeResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmManureTypesByFarmIdAsyncAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var farmManureType = responseWrapper.Data.FarmManureTypes.ToObject<List<FarmManureTypeResponse>>();
                        farmManureTypes.AddRange(farmManureType);
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
            return (farmManureTypes, error);
        }
        public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrient(string mannerJsonData)
        {
            MannerCalculateNutrientResponse mannerCalculateNutrientResponse = new MannerCalculateNutrientResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.PostAsync(string.Format(APIURLHelper.FetchMannerCalculateNutrientAsyncAPI), new StringContent(mannerJsonData, Encoding.UTF8, "application/json"));
                //var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerCalculateNutrientAsyncAPI, mannerJsonData));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        mannerCalculateNutrientResponse = responseWrapper.Data.ToObject<MannerCalculateNutrientResponse>();
                        
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
            return (mannerCalculateNutrientResponse, error);
        }

        public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeId(int soilTypeId)
        {
            SoilTypeSoilTextureResponse soilTypeSoilTexture = new SoilTypeSoilTextureResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilTypeSoilTextureBySoilTypeIdAsyncAPI, soilTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        soilTypeSoilTexture = responseWrapper.Data.SoilTypeSoilTexture.ToObject<SoilTypeSoilTextureResponse>();
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
            return (soilTypeSoilTexture, error);
        }
    }
}
