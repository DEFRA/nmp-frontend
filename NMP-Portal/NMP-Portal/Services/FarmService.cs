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
        public async Task<(Farm, Error)> AddFarmAsync(FarmData farmData)
        {
            string jsonData = JsonConvert.SerializeObject(farmData);
            Farm farm = null;
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);

                // check if farm already exists or not
                bool IsFarmExist = await IsFarmExistAsync(farmData.Farm.Name, farmData.Farm.Postcode);
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
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }
            return (farm,error);
        }
        public async Task<(Farm, Error)> FetchFarmByIdAsync(int farmId)
        {
            Farm farm = new Farm();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFarmByIdAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null )
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
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
            }

            return (farm, error);
        }

        public async Task<bool> IsFarmExistAsync(string farmName, string postcode)
        {
            bool isFarmExist = false;
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var farmExist = await httpClient.GetAsync(string.Format(APIURLHelper.IsFarmExist, farmName, postcode.Trim()));
            string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
            if (responseWrapperFarmExist.Data["exists"] == true)
            {
                isFarmExist = true;
            }

            return isFarmExist;
        }

        public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
        {
            decimal rainfallAverage=0;
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var rainfall = await httpClient.GetAsync(string.Format(APIURLHelper.FetchRainfallAverageAsyncAPI, firstHalfPostcode));
            string result = await rainfall.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            
            rainfallAverage = responseWrapperFarmExist.Data["averageRainfall"];
            return rainfallAverage;
        }
    }
}
