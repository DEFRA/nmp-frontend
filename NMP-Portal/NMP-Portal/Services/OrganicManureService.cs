using Newtonsoft.Json;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Security.Cryptography;

namespace NMP.Portal.Services
{
    public class OrganicManureService : Service, IOrganicManureService
    {
        private readonly ILogger<OrganicManureService> _logger;
        public OrganicManureService(ILogger<OrganicManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<(List<OrganicManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
        {
            List<OrganicManureCropTypeResponse> cropTypeList = new List<OrganicManureCropTypeResponse>();
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
                        var cropTypeResponseList = responseWrapper.Data.ToObject<List<OrganicManureCropTypeResponse>>();
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
        public async Task<(List<OrganicManureFieldResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId)
        {
            List<OrganicManureFieldResponse> organicManureFieldResponses = new List<OrganicManureFieldResponse>();
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
                        var organicManureFieldResponseList = responseWrapper.Data.ToObject<List<OrganicManureFieldResponse>>();
                        organicManureFieldResponses.AddRange(organicManureFieldResponseList);
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
            return (organicManureFieldResponses, error);
        }

        public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId)
        {
            List<int> managementIds = new List<int>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = string.Empty;
                if (cropTypeId != null)
                {
                    url = string.Format(APIURLHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeIdAsyncAPI, harvestYear, cropTypeId, fieldIds);
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
                var response = await httpClient.GetAsync(APIURLHelper.FetchManureGroupListAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var manureGroups = responseWrapper.Data.ManureGroups.ToObject<List<CommonResponse>>();
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
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureTypeListByGroupIdAsyncAPI, manureGroupId, countryId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var manureTypes = responseWrapper.Data.ManureTypes.ToObject<List<ManureType>>();                       
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
            CommonResponse manureGroup =new CommonResponse();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureGroupByIdAsyncAPI, manureGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        manureGroup = responseWrapper.Data.ManureGroup.ToObject<CommonResponse>();
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

        public async Task<(ManureType,Error)> FetchManureTypeByManureTypeId(int manureTypeId)
        {
            ManureType manureType = new ManureType();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchManureTypeByManureTypeIdAsyncAPI, manureTypeId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        manureType = responseWrapper.Data.ManureType.ToObject<ManureType>();
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

        public async Task<(List<ApplicationMethodResponse>, Error)> FetchApplicationMethodList(int fieldType,string applicableFor)
        {
            List<ApplicationMethodResponse> applicationMethodList = new List<ApplicationMethodResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchApplicationMethodsByApplicableForAsyncAPI, fieldType, applicableFor));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var applicationMethods = responseWrapper.Data.ApplicationMethods.ToObject<List<ApplicationMethodResponse>>();
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

        public async Task<(List<IncorporationMethodResponse>, Error)> FetchIncorporationMethodsByApplicationId(int fieldType, string applicableFor, int appId)
        {
            List<IncorporationMethodResponse> incorporationMethods = new List<IncorporationMethodResponse>();
            Error error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchIncorporationMethodsByApplicationIdAsyncAPI, appId,fieldType,applicableFor));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var methods = responseWrapper.Data.IncorporationMethods.ToObject<List<IncorporationMethodResponse>>();
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
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchIncorporationDelaysByMethodIdAndApplicableForAsyncAPI, methodId, applicableFor));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var delays = responseWrapper.Data.IncorporationDelays.ToObject<List<IncorprationDelaysResponse>>();
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
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchApplicationMethodByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null&& responseWrapper.Data.ApplicationMethod!=null)
                    {
                        applicationMethod = responseWrapper.Data.ApplicationMethod.Name;
                        
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
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchIncorporationMethodByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null&& responseWrapper.Data.IncorporationMethod!=null)
                    {
                        incorporationMethod = responseWrapper.Data.IncorporationMethod.Name;

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
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchIncorporationDelayByIdAsyncAPI, Id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null&& responseWrapper.Data.IncorporationDelay!=null)
                    {
                        incorporationDelay = responseWrapper.Data.IncorporationDelay.Name;

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
    }
}
