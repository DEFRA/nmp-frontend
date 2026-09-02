using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using System.Web;
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
        var requestUrl = string.Format(ApiurlHelper.FetchFieldCountByFarmIdAPI, farmId);
        var response = await httpClient.GetAsync(requestUrl);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
        {
            fieldCount = data["count"]?.Value<int>() ?? 0;
        }
        return fieldCount;
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

    private async static Task<HttpResponseMessage> PostFieldAsync(HttpClient httpClient, int farmId, FieldData fieldData)
    {
        string jsonData = JsonConvert.SerializeObject(fieldData);
        string url = string.Format(ApiurlHelper.AddFieldAPI, farmId);

        var response = await httpClient.PostAsync(
            url,
            new StringContent(jsonData, Encoding.UTF8, _applicationJson));

        
        return response;
    }

    private async Task<(Field?, Error?)> ParseAddFieldResponseAsync(HttpResponseMessage response)
    {
        string result = await response.Content.ReadAsStringAsync();
        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        Error? error = null;
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper?.Data != null && responseWrapper?.Data?.GetType().Name != "String")
            {
                var field = ExtractObject<Field>(responseWrapper, "Field");
                return (field, null);
            }
        }
        else
        {
            _logger.ExtractError(responseWrapper, error);
        }
        return (null, error);

    }

    private static T? ExtractObject<T>(ResponseWrapper? wrapper, string key)
    {
        return wrapper?.Data[key] is JObject jobject
            ? jobject.ToObject<T>()
            : default(T);
    }

    public async Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId = null)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        string url = fieldId == null ? string.Format(ApiurlHelper.IsFieldExistAPI, farmId, HttpUtility.UrlEncode(name)) : string.Format(ApiurlHelper.IsFieldExistByFieldIdAPI, farmId, HttpUtility.UrlEncode(name), fieldId);
        var response = await httpClient.GetAsync(url);
        
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        bool isFieldExist = responseWrapper?.Data?["exists"] ?? false;

        return isFieldExist;
    }

    public async Task<List<Field>> FetchFieldsByFarmIdAsync(int farmId)
    {
        List<Field> fields = new List<Field>();
        HttpClient httpClient = await GetNMPAPIClient();
        var url = string.Format(ApiurlHelper.FetchFieldsByFarmIdAPI, farmId);
        var response = await httpClient.GetAsync(url);
        
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
            Error? error = new Error();
            _logger.ExtractError(responseWrapper, error);
        }
        return fields;
    }

    public async Task<Field> FetchFieldByFieldIdAsync(int fieldId)
    {
        Field field = new Field();
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFieldByFieldIdAPI, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.Field is JToken fieldToken)
                {
                    field = fieldToken.ToObject<Field>() ?? new Field();
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return field;
    }

    

    public async Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldIdAsync(int fieldId, string shortSummary)
    {
        Error? error = null;
        List<SoilAnalysisResponse> soilAnalysis = new List<SoilAnalysisResponse>();
        try
        {

            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchSoilAnalysisByFieldIdAPI, fieldId, shortSummary));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data?.SoilAnalyses?.records is JToken records)
            {
                soilAnalysis = records.ToObject<List<SoilAnalysisResponse>>() ?? new List<SoilAnalysisResponse>();
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return soilAnalysis;
    }

    public async Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYearAsync(int fieldId, int year, bool confirm)
    {
        FieldDetailResponse fieldDetail = new FieldDetailResponse();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFieldDetailByFieldIdAndHarvestYearAPI, fieldId, year, "false"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.FieldDetails is JToken fieldDetails)
                {
                    fieldDetail = fieldDetails.ToObject<FieldDetailResponse>() ?? new FieldDetailResponse();
                    if (fieldDetail.SowingDate.HasValue)
                    {
                        fieldDetail.SowingDate = fieldDetail.SowingDate.Value.ToLocalTime();
                    }
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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

    public async Task<int> FetchSNSCategoryIdByCropTypeIdAsync(int cropTypeId)
    {
        int? snsCategoryID = null;
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropTypeLinkingsByCropTypeIdAPI, cropTypeId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    CropTypeLinkingResponse? cropTypeLinkingResponse = responseWrapper?.Data?.CropTypeLinking.ToObject<CropTypeLinkingResponse>();
                    if (cropTypeLinkingResponse != null)
                    {
                        snsCategoryID = cropTypeLinkingResponse.SNSCategoryID;
                    }
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
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

   

    public async Task<(Field, Error)> UpdateFieldAsync(FieldData fieldData, int fieldId)
    {
        string jsonData = JsonConvert.SerializeObject(fieldData);
        Field? field = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(string.Format(ApiurlHelper.UpdateFieldAPI, fieldId), new StringContent(jsonData, Encoding.UTF8, _applicationJson));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper?.Data?["Field"] is JObject fieldObject)
            {
                field = fieldObject.ToObject<Field>() ?? new Field();
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteFieldByIdAPI, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
            {
                message = data["message"]?.Value<string>() ?? string.Empty;
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error) ?? new Error();
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

    public async Task<List<CommonResponse>> GetGrassManagementOptionsAsync()
    {
        List<CommonResponse> grassManagementOptions = new List<CommonResponse>();
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchGrassManagementOptionsAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken records)
                {
                    var grassManagementOption = records.ToObject<List<CommonResponse>>()
                    ?? new List<CommonResponse>();
                    grassManagementOptions.AddRange(grassManagementOption);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return grassManagementOptions;
    }

    public async Task<List<CommonResponse>> GetGrassTypicalCutsAsync()
    {
        List<CommonResponse> grassTypicalCuts = new List<CommonResponse>();
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchGrassTypicalCutsAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    var grassTypicalCut = data.ToObject<List<CommonResponse>>() ?? new List<CommonResponse>();
                    grassTypicalCuts.AddRange(grassTypicalCut);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return grassTypicalCuts;
    }

    public async Task<List<CommonResponse>> GetSoilNitrogenSupplyItemsAsync()
    {
        List<CommonResponse> soilNitrogenSupplyItems = new List<CommonResponse>();
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchSoilNitrogenSupplyItemsAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    var soilNitrogenSupplyItem = data.ToObject<List<CommonResponse>>() ?? new List<CommonResponse>();
                    soilNitrogenSupplyItems.AddRange(soilNitrogenSupplyItem);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, error);
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
        return soilNitrogenSupplyItems;
    }
    public async Task<(Error, List<Field>)> FetchFieldByFarmIdAsync(int farmId, string shortSummary)
    {
        List<Field> fields = new List<Field>();
        Error? error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            string url = string.Format(ApiurlHelper.FetchFieldByFarmIdAPI, farmId, shortSummary);
            var response = await httpClient.GetAsync(url);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data?.Fields is JToken fieldsToken)
                {
                    var fieldslist = fieldsToken.ToObject<List<Field>>()
                    ?? new List<Field>();
                    fields.AddRange(fieldslist);
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
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
        return (error, fields);
    }
    public async Task<(FieldResponse?, Error?)> FetchFieldSoilAnalysisAndSnsByIdAsync(int fieldId)
    {
        FieldResponse? fieldResponse = new FieldResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchFieldSoilAnalysisAndSnsByIdAPI, fieldId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    fieldResponse = responseWrapper?.Data?.Records.ToObject<FieldResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
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
        return (fieldResponse, error);
    }
    public async Task<(CropAndFieldReportResponse?, Error?)> FetchCropAndFieldReportByIdAsync(string fieldId, int year)
    {
        CropAndFieldReportResponse? cropAndFieldReportResponse = new CropAndFieldReportResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchCropAndFieldReportByIdAPI, fieldId, year));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    cropAndFieldReportResponse = responseWrapper?.Data?.ToObject<CropAndFieldReportResponse>();
                }
            }
            else
            {
                error = _logger.ExtractError(responseWrapper, error);
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
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateOnlyFieldAPI, new StringContent(jsonData, Encoding.UTF8, _applicationJson));
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
                _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error.Code, error.Message, error.Stack, error.Path);

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
    public async Task<List<CommonResponse>> FetchPscIndexAsync()
    {
        List<CommonResponse> pscIndexList = [];
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchPscIndexesAPI);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var pscIndexes = responseWrapper?.Data?.records.ToObject<List<CommonResponse>>();
                    pscIndexList.AddRange(pscIndexes);
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, null);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return pscIndexList;
    }

    public async Task<CommonResponse?> FetchPscIndexByIdAsync(int id)
    {
        CommonResponse pscIndex = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchPscIndexeByIdAPI, id));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var pscIndexData = responseWrapper?.Data?.records.ToObject<CommonResponse>();
                    if (pscIndexData != null)
                    {
                        pscIndex = pscIndexData;
                    }
                }
            }
            else
            {
                _logger.ExtractError(responseWrapper, null);
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return pscIndex;
    }

}
