﻿using Azure;
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
    public class FarmService : Service, IFarmService
    {
        private readonly ILogger<FarmService> _logger;
        public FarmService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base
        (httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<(List<Farm>, Error)> FetchFarmByOrgIdAsync(Guid orgId)
        {
            List<Farm> farmList = new List<Farm>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmByOrgIdAPI, orgId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<Farm> farms = responseWrapper.Data.Farms.ToObject<List<Farm>>();
                    if (farms != null && farms.Count > 0)
                    {
                        farmList.AddRange(farms);
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

            return (farmList, error);
        }
        public async Task<(Farm, Error)> AddFarmAsync(FarmData farmData)
        {
            string jsonData = JsonConvert.SerializeObject(farmData);
            Farm farm = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();

                // check if farm already exists or not
                bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID);
                if (!IsFarmExist)
                {
                    // if new farm then save farm data
                    var response = await httpClient.PostAsync(APIURLHelper.AddFarmAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                    {

                        JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                        if (farmData != null)
                        {
                            farm = farmDataJObject.ToObject<Farm>();
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
                else
                {
                    error.Message =
                        string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode);
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
            return (farm, error);
        }
        public async Task<(Farm, Error)> FetchFarmByIdAsync(int farmId)
        {
            Farm farm = new Farm();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmByIdAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                    if (farmDataJObject != null)
                    {
                        farm = farmDataJObject.ToObject<Farm>();
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

            return (farm, error);
        }

        public async Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id)
        {
            bool isFarmExist = false;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var farmExist = await httpClient.GetAsync(string.Format(APIURLHelper.IsFarmExist, farmName, postcode.Trim(), Id));
                string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
                if (responseWrapperFarmExist.Data["exists"] == true)
                {
                    isFarmExist = true;
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

            return isFarmExist;
        }

        public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
        {
            decimal rainfallAverage = 0;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchMannerRainfallAverageAsyncAPI, firstHalfPostcode));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    rainfallAverage = responseWrapper.Data.avarageAnnualRainfall != null ? responseWrapper.Data.avarageAnnualRainfall.value : 0;
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
            return rainfallAverage;
        }

        public async Task<(Farm, Error)> UpdateFarmAsync(FarmData farmData)
        {
            string jsonData = JsonConvert.SerializeObject(farmData);
            Farm farm = null;
            Error error = new Error();
            try
            {
                //check if Updated farm Name already exist or not in the Postcode...
                bool IsFarmNameWithInPostCodeAlreadyExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID);
                if (!IsFarmNameWithInPostCodeAlreadyExist)
                {
                    HttpClient httpClient = await GetNMPAPIClient();
                    var response = await httpClient.PutAsync(APIURLHelper.UpdateFarmAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                    {

                        JObject farmDataJObject = responseWrapper.Data["Farm"] as JObject;
                        if (farmData != null)
                        {
                            farm = farmDataJObject.ToObject<Farm>();
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
                else
                {
                    error.Message =
                        string.Format(Resource.MsgFarmAlreadyExist, farmData.Farm.Name, farmData.Farm.Postcode);
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
            return (farm, error);
        }

        public async Task<(string, Error)> DeleteFarmByIdAsync(int farmId)
        {
            Error error = new Error();
            string message = string.Empty;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteFarmByIdAPI, farmId));
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
        public async Task<(List<Country>, Error)> FetchCountryAsync()
        {
            List<Country> countryList = new List<Country>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCountryListAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<Country> countries = responseWrapper.Data.Countries.ToObject<List<Country>>();
                    if (countries != null && countries.Count > 0)
                    {
                        countryList.AddRange(countries);
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

            return (countryList, error);
        }
        public async Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year)
        {
            ExcessRainfalls excessRainfalls = new ExcessRainfalls();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchExcessRainfallByFarmIdAndYearAPI, farmId, year));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    excessRainfalls = responseWrapper.Data.ExcessRainfall.ToObject<ExcessRainfalls>();

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

            return (excessRainfalls, error);
        }
        public async Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync()
        {
            List<CommonResponse> excessWinterRainfallOption = new List<CommonResponse>();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchExcessWinterRainfallOptionAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    excessWinterRainfallOption = responseWrapper.Data.ExcessWinterRainFallOptions.ToObject<List<CommonResponse>>();

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

            return (excessWinterRainfallOption, error);
        }
        public async Task<(ExcessRainfalls, Error)> AddExcessWinterRainfallAsync(int farmId,int year,string excessWinterRainfallData,bool isUpdated)
        {
            ExcessRainfalls excessRainfalls = null;
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                string url = string.Empty;
                HttpResponseMessage response = null;
                if (isUpdated!=null&&isUpdated)
                {
                     response = await httpClient.PutAsync(string.Format(APIURLHelper.AddOrUpdateExcessWinterRainfallAPI, farmId, year), new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
                }
                else
                {
                     response = await httpClient.PostAsync(string.Format(APIURLHelper.AddOrUpdateExcessWinterRainfallAPI, farmId, year), new StringContent(excessWinterRainfallData, Encoding.UTF8, "application/json"));
                }
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {

                    JObject excessRainfallObj = responseWrapper.Data["ExcessRainfall"] as JObject;
                    if (excessRainfallObj != null)
                    {
                        excessRainfalls = excessRainfallObj.ToObject<ExcessRainfalls>();
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
            return (excessRainfalls, error);
        }
        public async Task<(CommonResponse, Error)> FetchExcessWinterRainfallOptionByIdAsync(int id)
        {
            CommonResponse excessWinterRainfallOption = new CommonResponse();
            Error error = new Error();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchExcessWinterRainfallOptionByIdAPI,id));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    excessWinterRainfallOption = responseWrapper.Data.records.ToObject<CommonResponse>();

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

            return (excessWinterRainfallOption, error);
        }
    }
}

