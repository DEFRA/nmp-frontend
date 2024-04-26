using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Linq.Expressions;
using System.Text;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NMP.Portal.Services
{
    public class FieldService : Service, IFieldService
    {
        private readonly ILogger<FieldService> _logger;
        public FieldService(ILogger<FieldService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
        {
            int fieldCount = 0;
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldCountByFarmIdAPI, farmId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        fieldCount = responseWrapper.Data["count"];

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

            return fieldCount;
        }
        public async Task<List<SoilTypesResponse>> FetchSoilTypes()
        {
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(APIURLHelper.FetchSoilTypesAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var soiltypeslist = responseWrapper.Data.ToObject<List<SoilTypesResponse>>();
                        soilTypes.AddRange(soiltypeslist);
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
            return soilTypes;
        }
        public async Task<List<FieldResponseWapper>> FetchNutrientsAsync()
        {
            List<FieldResponseWapper> nutrients = new List<FieldResponseWapper>();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchNutrientsAsyncAPI));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        List<FieldResponseWapper> fieldResponseWapper = responseWrapper.Data.ToObject<List<FieldResponseWapper>>();
                        nutrients.AddRange(fieldResponseWapper);
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

            return nutrients;
        }
        public async Task<List<CropGroupResponse>> FetchCropGroups()
        {
            List<CropGroupResponse> soilTypes = new List<CropGroupResponse>();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(APIURLHelper.FetchCropGroupsAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var soiltypeslist = responseWrapper.Data.ToObject<List<CropGroupResponse>>();
                        soilTypes.AddRange(soiltypeslist);
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
            return soilTypes;
        }
        public async Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId)
        {
            List<CropTypeResponse> soilTypes = new List<CropTypeResponse>();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypesAsyncAPI, cropGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var soiltypeslist = responseWrapper.Data.ToObject<List<CropTypeResponse>>();
                        soilTypes.AddRange(soiltypeslist);
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

            return soilTypes;
        }
        public async Task<(Field,Error)> AddFieldAsync(FieldData fieldData, int farmId, string farmName)
        {
            string jsonData = JsonConvert.SerializeObject(fieldData);
            Field field = null;
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);

                //check if farm already exists or not
                bool IsFarmExist = await IsFieldExistAsync(farmId, fieldData.Field.Name);
                if (!IsFarmExist)
                {
                    // if new farm then save farm data
                    var response = await httpClient.PostAsync(string.Format(APIURLHelper.AddFieldAsyncAPI, farmId), new StringContent(jsonData, Encoding.UTF8, "application/json"));
                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                    {

                        JObject farmDataJObject = responseWrapper.Data["Field"] as JObject;
                        if (fieldData != null)
                        {
                            field = farmDataJObject.ToObject<Field>();
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
                        string.Format(Resource.MsgFarmAlreadyExist, farmName, fieldData.Field.Name);
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
            return (field,error);
        }
        public async Task<bool> IsFieldExistAsync(int fieldId, string name)
        {
            bool isFieldExist = false;
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var farmExist = await httpClient.GetAsync(string.Format(APIURLHelper.IsFieldExistAsyncAPI, fieldId, name));
                string resultFarmExist = await farmExist.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapperFarmExist = JsonConvert.DeserializeObject<ResponseWrapper>(resultFarmExist);
                if (responseWrapperFarmExist.Data["exists"] == true)
                {
                    isFieldExist = true;
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

            return isFieldExist;
        }
    }
}
