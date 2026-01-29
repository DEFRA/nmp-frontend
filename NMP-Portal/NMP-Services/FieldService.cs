using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class FieldService(ILogger<FieldService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IFieldService
{
    private readonly ILogger<FieldService> _logger = logger;
    private const string _applicationJson = "application/json"; 

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        int fieldCount = 0;
        HttpClient httpClient = await GetNMPAPIClient();
        var requestUrl = string.Format(APIURLHelper.FetchFieldCountByFarmIdAPI, farmId);
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                fieldCount = responseWrapper.Data["count"];
            }
        }

        return fieldCount;
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchSoilTypesAsyncAPI);
        response.EnsureSuccessStatusCode();
        response.EnsureSuccessStatusCode();
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
        return soilTypes;
    }

    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
        List<NutrientResponseWrapper> nutrients = new List<NutrientResponseWrapper>();
        Error error = null;
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchNutrientsAsyncAPI);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                List<NutrientResponseWrapper> nutrientResponseWapper = responseWrapper.Data.ToObject<List<NutrientResponseWrapper>>();
                nutrients.AddRange(nutrientResponseWapper);
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }
        return (nutrients, error);
    }

    public async Task<List<CropGroupResponse>> FetchCropGroups()
    {
        List<CropGroupResponse> soilTypes = new List<CropGroupResponse>();
        Error error = new Error();
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchCropGroupsAsyncAPI);
        response.EnsureSuccessStatusCode();
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
                if (error != null)
                {
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                }

            }
        }

        return soilTypes;
    }

    public async Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId)
    {
        List<CropTypeResponse> soilTypes = new List<CropTypeResponse>();
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
                    var soiltypeslist = responseWrapper.Data.ToObject<List<CropTypeResponse>>();
                    soilTypes.AddRange(soiltypeslist);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }

        return soilTypes;
    }

    public async Task<string> FetchCropGroupById(int cropGroupId)
    {
        Error error = null;
        string cropGroup = string.Empty;
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropGroupByIdAsyncAPI, cropGroupId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                cropGroup = responseWrapper.Data["cropGroupName"];
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return cropGroup;
    }

    public async Task<string> FetchCropTypeById(int cropTypeId)
    {
        Error error = null;
        string cropType = string.Empty;
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypeByIdAsyncAPI, cropTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                cropType = responseWrapper.Data["cropTypeName"];
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);           
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);           
        }
        return cropType;
    }

    public async Task<(Field?, Error?)> AddFieldAsync(FieldData fieldData, int farmId, string farmName)
    {
        if (fieldData != null && fieldData.Field != null && !string.IsNullOrWhiteSpace(fieldData.Field.Name) && await IsFieldExistAsync(farmId, fieldData.Field.Name))
        {
            return (null, CreateFieldAlreadyExistsError());
        }

        var httpClient = await GetNMPAPIClient();
        var response = await PostFieldAsync(httpClient, farmId, fieldData);

        return await ParseAddFieldResponseAsync(response);
    }

    private static Error CreateFieldAlreadyExistsError()
    {
        return new Error
        {
            Message = Resource.MsgFieldAlreadyExist
        };
    }

    private async static Task<HttpResponseMessage> PostFieldAsync( HttpClient httpClient, int farmId, FieldData fieldData)
    {
        string jsonData = JsonConvert.SerializeObject(fieldData);
        string url = string.Format(APIURLHelper.AddFieldAsyncAPI, farmId);

        var response = await httpClient.PostAsync(
            url,
            new StringContent(jsonData, Encoding.UTF8, _applicationJson));

        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<(Field?, Error?)> ParseAddFieldResponseAsync(HttpResponseMessage response)
    {
        string result = await response.Content.ReadAsStringAsync();
        var wrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (wrapper?.Data != null && wrapper?.Data?.GetType().Name != "String")
        {
            var field = ExtractObject<Field>(wrapper, "Field");
            return (field, null);
        }

        var error = ExtractError(wrapper, _logger);
        return (null, error);
    }

    private static T? ExtractObject<T>(ResponseWrapper? wrapper, string key)
    {
        return wrapper?.Data[key] is JObject jobject
            ? jobject.ToObject<T>()
            : default(T);
    }

    private static Error? ExtractError(ResponseWrapper? wrapper,ILogger<FieldService> logger)
    {
        var error = wrapper?.Error?.ToObject<Error>();

        if (error != null)
        {            
            logger.LogError("Error Response Wrapper: {Wrapper}", JsonConvert.SerializeObject(wrapper));
        }

        return error;
    }

    public async Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId = null)
    {
        bool isFieldExist = false;
        HttpClient httpClient = await GetNMPAPIClient();
        string url = fieldId == null ? string.Format(APIURLHelper.IsFieldExistAsyncAPI, farmId, name) : string.Format(APIURLHelper.IsFieldExistByFieldIdAsyncAPI, farmId, name, fieldId);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        isFieldExist = responseWrapper?.Data?["exists"] ?? false;

        return isFieldExist;
    }

    public async Task<List<Field>> FetchFieldsByFarmId(int farmId)
    {
        List<Field> fields = new List<Field>();
        HttpClient httpClient = await GetNMPAPIClient();
        var url = string.Format(APIURLHelper.FetchFieldsByFarmIdAsyncAPI, farmId);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var fieldslist = responseWrapper?.Data?.Fields.ToObject<List<Field>>();
                fields.AddRange(fieldslist);
            }
        }
        else
        {
            if (responseWrapper != null && responseWrapper.Error != null)
            {
                Error? error = responseWrapper?.Error?.ToObject<Error>();
                if (error != null)
                {
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                }
            }
        }

        return fields;
    }

    public async Task<Field> FetchFieldByFieldId(int fieldId)
    {
        Field field = new Field();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldByFieldIdAsyncAPI, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    field = responseWrapper?.Data?.Field.ToObject<Field>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex,ex.Message);            
        }
        return field;
    }

    public async Task<List<CropTypeResponse>> FetchAllCropTypes()
    {
        List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();            
            var response = await httpClient.GetAsync(APIURLHelper.FetchAllCropTypeAsyncAPI);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var cropTypesList = responseWrapper?.Data?.ToObject<List<CropTypeResponse>>();
                    cropTypes.AddRange(cropTypesList);
                }
            }
            else
            {
                ExtractError(responseWrapper, _logger);
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre,hre.Message);           
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }

        return cropTypes;
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        Error error = null;
        string soilType = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilTypeBySoilTypeIdAsyncAPI, soilTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                soilType = responseWrapper.Data["soilType"];
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);           
        }
        return soilType;
    }

    public async Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary)
    {
        Error error = null;
        List<SoilAnalysisResponse> soilAnalysis = new List<SoilAnalysisResponse>();
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchSoilAnalysisByFieldIdAsyncAPI, fieldId, shortSummary));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                soilAnalysis = responseWrapper.Data.SoilAnalyses.records.ToObject<List<SoilAnalysisResponse>>();
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);           
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);           
        }
        return soilAnalysis;
    }

    public async Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        FieldDetailResponse fieldDetail = new FieldDetailResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldDetailByFieldIdAndHarvestYearAsyncAPI, fieldId, year, "false"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    fieldDetail = responseWrapper.Data.FieldDetails.ToObject<FieldDetailResponse>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error = new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);           
        }
        return (fieldDetail, error);
    }

    public async Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId)
    {
        int? snsCategoryID = null;
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
                    if (cropTypeLinkingResponse != null)
                    {
                        snsCategoryID = cropTypeLinkingResponse.SNSCategoryID;
                    }
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return snsCategoryID ?? 0;
    }

    public async Task<List<SeasonResponse>> FetchSeasons()
    {
        List<SeasonResponse> seasons = new List<SeasonResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchSeasonsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var seasonlist = responseWrapper.Data.ToObject<List<SeasonResponse>>();
                    seasons.AddRange(seasonlist);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);           
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return seasons;
    }

    public async Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData)
    {
        string jsonData = JsonConvert.SerializeObject(measurementData);
        SnsResponse snsResponse = new SnsResponse();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(APIURLHelper.FetchSNSIndexByMeasurementMethodAsyncAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
            {

                JObject farmDataJObject = responseWrapper?.Data as JObject;
                snsResponse = farmDataJObject.ToObject<SnsResponse>();
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper?.Error?.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }

        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre,hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex,ex.Message);
        }
        return (snsResponse, error);
    }

    public async Task<(Field, Error)> UpdateFieldAsync(FieldData fieldData, int fieldId)
    {
        string jsonData = JsonConvert.SerializeObject(fieldData);
        Field field = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(string.Format(APIURLHelper.UpdateFieldAsyncAPI, fieldId), new StringContent(jsonData, Encoding.UTF8, _applicationJson));
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
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (field, error);
    }
    public async Task<(string, Error)> DeleteFieldByIdAsync(int fieldId)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(APIURLHelper.DeleteFieldByIdAPI, fieldId));
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
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }

        return (message, error);
    }

    public async Task<List<CommonResponse>> GetGrassManagementOptions()
    {
        List<CommonResponse> grassManagementOptions = new List<CommonResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchGrassManagementOptionsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var grassManagementOption = responseWrapper.Data.records.ToObject<List<CommonResponse>>();
                    grassManagementOptions.AddRange(grassManagementOption);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(   hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return grassManagementOptions;
    }

    public async Task<List<CommonResponse>> GetGrassTypicalCuts()
    {
        List<CommonResponse> grassTypicalCuts = new List<CommonResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchGrassTypicalCutsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var grassTypicalCut = responseWrapper.Data.ToObject<List<CommonResponse>>();
                    grassTypicalCuts.AddRange(grassTypicalCut);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return grassTypicalCuts;
    }

    public async Task<List<CommonResponse>> GetSoilNitrogenSupplyItems()
    {
        List<CommonResponse> soilNitrogenSupplyItems = new List<CommonResponse>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(APIURLHelper.FetchSoilNitrogenSupplyItemsAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var soilNitrogenSupplyItem = responseWrapper.Data.ToObject<List<CommonResponse>>();
                    soilNitrogenSupplyItems.AddRange(soilNitrogenSupplyItem);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return soilNitrogenSupplyItems;
    }
    public async Task<(Error, List<Field>)> FetchFieldByFarmId(int farmId, string shortSummary)
    {
        List<Field> fields = new List<Field>();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url= string.Format(APIURLHelper.FetchFieldByFarmIdAsyncAPI, farmId, shortSummary);
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var fieldslist = responseWrapper.Data.Fields.ToObject<List<Field>>();
                    fields.AddRange(fieldslist);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return (error, fields);
    }
    public async Task<(FieldResponse, Error)> FetchFieldSoilAnalysisAndSnsById(int fieldId)
    {
        FieldResponse fieldResponse = new FieldResponse();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchFieldSoilAnalysisAndSnsByIdAsyncAPI, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    fieldResponse = responseWrapper.Data.Records.ToObject<FieldResponse>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return (fieldResponse, error);
    }
    public async Task<(CropAndFieldReportResponse, Error)> FetchCropAndFieldReportById(string fieldId, int year)
    {
        CropAndFieldReportResponse cropAndFieldReportResponse = new CropAndFieldReportResponse();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropAndFieldReportByIdAsyncAPI, fieldId, year));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    cropAndFieldReportResponse = responseWrapper.Data.ToObject<CropAndFieldReportResponse>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper?.Error?.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);            
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);            
        }
        return (cropAndFieldReportResponse, error);
    }
    public async Task<(Field?, Error)> UpdateFieldDataAsync(Field field)
    {
        string jsonData = JsonConvert.SerializeObject(field);
        Field? fieldData = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(APIURLHelper.UpdateOnlyFieldAsyncAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode &&
                responseWrapper?.Data?["Field"] is JObject farmDataJObject)
            {
                fieldData = farmDataJObject.ToObject<Field>();
            }
            else if (responseWrapper is { Error: not null })
            {
                error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
              
            }

        }
        catch (HttpRequestException hre)
        {
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (fieldData, error);
    }
}
