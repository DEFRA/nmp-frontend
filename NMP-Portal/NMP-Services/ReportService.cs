using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Text;
using NMP.Commons.Helpers;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class ReportService(ILogger<FarmService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IReportService
{
    private readonly ILogger<FarmService> _logger = logger;

    public async Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetailsData);
        NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.AddNutrientsLoadingFarmDetailsAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject nutrientsLoadingFarmDetailsJObject)
                {
                    nutrientsLoadingFarmDetail = nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>()
                        ?? new NutrientsLoadingFarmDetail();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }

        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingFarmDetail, error);
    }
    public async Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year)
    {
        NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsLoadingFarmDetailsByfarmIdAndYearAPI, farmId, year));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                if (responseWrapper?.Data?["NutrientsLoadingFarmDetails"] is JObject nutrientsLoadingFarmDetailsJObject)
                {
                    nutrientsLoadingFarmDetail =
                        nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>()
                        ?? new NutrientsLoadingFarmDetail();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error= _logger.ExtractError(responseWrapper, error)?? new Error();
                }
            }

        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingFarmDetail, error);
    }
    public async Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetailsData);
        NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(ApiurlHelper.UpdateNutrientsLoadingFarmDetailsAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper?.Data?["NutrientsLoadingFarmDetails"] is JObject nutrientsLoadingFarmDetailsJObject)
            {
                nutrientsLoadingFarmDetail =
                    nutrientsLoadingFarmDetailsJObject.ToObject<NutrientsLoadingFarmDetail>()
                    ?? new NutrientsLoadingFarmDetail();
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }

        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingFarmDetail, error);
    }
    public async Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmId(int farmId)
    {
        Error error = new Error();
        List<NutrientsLoadingManures> NutrientsLoadingManuresList = new List<NutrientsLoadingManures>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsloadingFarmDetailsFarmIdAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper != null && responseWrapper.Data != null)
                {
                    var nutrientsLoadingManures = responseWrapper?.Data?.NutrientsLoadingFarmDetails.ToObject<List<NutrientsLoadingManures>>();
                    if (nutrientsLoadingManures != null)
                    {
                        NutrientsLoadingManuresList = nutrientsLoadingManures;
                    }
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (NutrientsLoadingManuresList, error);
    }
    public async Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        NutrientsLoadingManures nutrientsLoadingManureData = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.AddNutrientsLoadingManureAPI, new StringContent(nutrientsLoadingManure, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject nutrientsLoadingManureDataObj)
                {
                    nutrientsLoadingManureData = nutrientsLoadingManureDataObj.ToObject<NutrientsLoadingManures>()
                    ?? new NutrientsLoadingManures();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }

        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingManureData, error);
    }
    public async Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmId(int farmId)
    {
        Error error = new Error();
        List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetailList = new List<NutrientsLoadingFarmDetail>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsLoadingFarmDetailsByFarmIdAPI, farmId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    nutrientsLoadingFarmDetailList = data.ToObject<List<NutrientsLoadingFarmDetail>>() ?? new List<NutrientsLoadingFarmDetail>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingFarmDetailList, error);
    }

    public async Task<(List<CommonResponse>, Error)> FetchLivestockGroupList()
    {
        List<CommonResponse> livestockGroupList = new List<CommonResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchLivestockGroupListAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.LivestockGroups is JToken livestockGroupsToken)
                {
                    var livestockGroups =
                        livestockGroupsToken.ToObject<List<CommonResponse>>()
                        ?? new List<CommonResponse>();

                    livestockGroupList.AddRange(livestockGroups);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (livestockGroupList, error);
    }

    public async Task<(CommonResponse, Error)> FetchLivestockGroupById(int livestockGroupId)
    {
        CommonResponse livestockGroup = new CommonResponse();
        Error? error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchLivestockManureGroupByIdAsyncAPI, livestockGroupId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.livestockGroup is JToken livestockGroupToken)
                {
                    livestockGroup =
                        livestockGroupToken.ToObject<CommonResponse>()
                        ?? new CommonResponse();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (livestockGroup, error);
    }
    public async Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id)
    {
        Error error = new Error();
        NutrientsLoadingManures NutrientsLoadingManure = new NutrientsLoadingManures();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsLoadingManureByIdAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.NutrientsLoadingManure is JToken manureToken)
                {
                    NutrientsLoadingManure =
                        manureToken.ToObject<NutrientsLoadingManures>()
                        ?? new NutrientsLoadingManures();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (NutrientsLoadingManure, error);
    }
    public async Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        NutrientsLoadingManures NutrientsLoadingManureData = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PutAsync(ApiurlHelper.UpdateNutrientsLoadingManureAsyncAPI, new StringContent(nutrientsLoadingManure, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject nutrientsLoadingManureDataObj)
                {
                    NutrientsLoadingManureData = nutrientsLoadingManureDataObj.ToObject<NutrientsLoadingManures>()
                    ?? new NutrientsLoadingManures();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (NutrientsLoadingManureData, error);
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupId(int livestockGroupId)
    {
        List<LivestockTypeResponse> livestockTypeList = new List<LivestockTypeResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchLivestockTypesByGroupIdAsyncAPI, livestockGroupId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.livestockTypes is JToken livestockTypesToken)
                {
                    var livestockTypes = livestockTypesToken.ToObject<List<LivestockTypeResponse>>()
                        ?? new List<LivestockTypeResponse>();
                    livestockTypeList.AddRange(livestockTypes);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (livestockTypeList, error);
    }
    public async Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteNutrientsLoadingManuresByIdAPI, nutrientsLoadingManureId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JObject data)
                {
                    message = data["message"]?.Value<string>() ?? string.Empty;
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }

        return (message, error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingLiveStockData);
        NutrientsLoadingLiveStock nutrientsLoadingLiveStocks = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();

            var response = await httpClient.PostAsync(ApiurlHelper.AddNutrientsLoadingLivestockAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject nutrientsLoadingLiveStocksJObject)
                {
                    nutrientsLoadingLiveStocks = nutrientsLoadingLiveStocksJObject.ToObject<NutrientsLoadingLiveStock>()
                   ?? new NutrientsLoadingLiveStock();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingLiveStocks, error);
    }

    public async Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYear(int farmId, int year)
    {
        Error error = new Error();
        List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList = new List<NutrientsLoadingLiveStockViewModel>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsLoadingLivestockByFarmIdAndYearAsyncAPI, farmId,year));

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    nutrientsLoadingLiveStockList = data.ToObject<List<NutrientsLoadingLiveStockViewModel>>()
                        ?? new List<NutrientsLoadingLiveStockViewModel>();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingLiveStockList, error);
    }
    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypes()
    {
        List<LivestockTypeResponse> livestockTypeList = new List<LivestockTypeResponse>();
        Error error = null;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(ApiurlHelper.FetchLivestockTypesAsyncAPI);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.LivestockTypes is JToken livestockTypesToken)
                {
                    var livestockTypes = livestockTypesToken.ToObject<List<LivestockTypeResponse>>()
                        ?? new List<LivestockTypeResponse>();
                    livestockTypeList.AddRange(livestockTypes);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (livestockTypeList, error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id)
    {
        Error error = new Error();
        NutrientsLoadingLiveStock nutrientsLoadingLiveStock = new NutrientsLoadingLiveStock();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchNutrientsLoadingLiveStockByIdAsyncAPI, id));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data?.records is JToken recordsToken)
                {
                    nutrientsLoadingLiveStock = recordsToken.ToObject<NutrientsLoadingLiveStock>()
                        ?? new NutrientsLoadingLiveStock();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingLiveStock, error);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.DeleteAsync(string.Format(ApiurlHelper.DeleteNutrientsLoadingLivestockByIdAPI, nutrientsLoadingLivestockId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data is JObject data)
                {
                    message = data["message"]?.Value<string>() ?? string.Empty;
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }

        return (message, error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingLiveStockData);
        NutrientsLoadingLiveStock? nutrientsLoadingLiveStocks = null;
        Error error = new Error();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();            
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateNutrientsLoadingLivestockAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if(responseWrapper?.Data?["NutrientsLoadingLiveStock"] is JObject nutrientsLoadingLiveStocksJObject)
                {
                    nutrientsLoadingLiveStocks = nutrientsLoadingLiveStocksJObject.ToObject<NutrientsLoadingLiveStock>()
                    ?? new NutrientsLoadingLiveStock();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (nutrientsLoadingLiveStocks, error);
    }

    public async Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropId(int cropId)
    {
        Error? error = null;
        OrganicManureFertiliserResponse organicManureFertiliserResponse = new OrganicManureFertiliserResponse();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(ApiurlHelper.FetchOrganicManuresFertilisersByCropIdAsyncAPI, cropId));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data is JToken data)
                {
                    organicManureFertiliserResponse = data.ToObject<OrganicManureFertiliserResponse>()
                        ?? new OrganicManureFertiliserResponse();
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    _logger.ExtractError(responseWrapper, error);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, error);
        }
        return (organicManureFertiliserResponse, error);
    }
}
