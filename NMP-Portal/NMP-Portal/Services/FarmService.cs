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
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using static System.Runtime.InteropServices.JavaScript.JSType;

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
                bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode,farmData.Farm.ID);
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
                var farmExist = await httpClient.GetAsync(string.Format(APIURLHelper.IsFarmExist, farmName, postcode.Trim(),Id));
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
                var rainfall = await httpClient.GetAsync(string.Format(APIURLHelper.FetchRainfallAverageAsyncAPI, firstHalfPostcode));
                string result = await rainfall.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(result);

                rainfallAverage = responseWrapperFarmExist.Data["rainfallAverage"];
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
                bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode, farmData.Farm.ID);
                if (!IsFarmExist)
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
    }
}
