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
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class FertiliserManureService : Service, IFertiliserManureService
{
    private readonly ILogger<FertiliserManureService> _logger;
    private const string _logError = "{Code} : {Message} : {Stack} : {Path}";
    public FertiliserManureService(ILogger<FertiliserManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        _logger = logger;
    }
    public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        string url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAPI,
        harvestYear, cropGroupName, fieldIds, cropOrder);

        if (cropOrder == null)
            url = url.Replace("&cropOrder=", "");

        if (string.IsNullOrWhiteSpace(cropGroupName))
            url = url.Replace("cropGroupName=&", "");

        var (data, error) = await HandleApiRequest<List<int>>(
    rw => ((JToken)rw.Data["ManagementPeriods"])
            .ToObject<List<CommonResponse>>()
            .Select(x => x.Id)
            .ToList(),
    url
);
        return (data ?? new List<int>(), error);

    }
    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYearAsync(int farmId, int harvestYear)
    {
        string url = string.Format(ApiurlHelper.FetchCropTypeByFarmIdAndHarvestYearAPI, harvestYear, farmId);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<ManureCropTypeResponse>>(), url);
        return (data ?? new List<ManureCropTypeResponse>(), error);

    }
    public async Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, int farmId, string? cropGroupName)
    {
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAPI, harvestYear, cropGroupName, farmId);
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAPI, harvestYear, farmId);
        }
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<CommonResponse>>(), url);
        return (data ?? new List<CommonResponse>(), error);

    }

    public async Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurationsAsync()
    {
        string url = ApiurlHelper.FetchInOrganicManureDurationsAPI;
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.InorganicManureDurations.ToObject<List<InOrganicManureDurationResponse>>(), url);
        return (data ?? new List<InOrganicManureDurationResponse>(), error);
    }
    public async Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsByIdAsync(int id)
    {
        string url = string.Format(ApiurlHelper.FetchInOrganicManureDurationsByIdAPI, id);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.InorganicManureDuration.ToObject<InOrganicManureDurationResponse>(), url);
        return (data ?? new InOrganicManureDurationResponse(), error);
    }

    public async Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure)
    {
        Error? error = null;
        List<FertiliserManure> fertilisers = new List<FertiliserManure>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PostAsync(ApiurlHelper.AddFertiliserManuresAPI, new StringContent(fertiliserManure, Encoding.UTF8, "application/json"));
            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data?.FertiliserManure is JToken fertiliserManures)
            {
                List<FertiliserManure> fertiliser = fertiliserManures.ToObject<List<FertiliserManure>>() ?? new List<FertiliserManure>();

                if (fertiliser.Count > 0)
                {
                    fertilisers.AddRange(fertiliser);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    error = responseWrapper?.Error?.ToObject<Error>();
                    if (error != null)
                    {
                        _logger.LogError(_logError, error.Code, error.Message, error.Stack, error.Path);
                    }
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error ??= new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error ??= new Error();
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
        }
        return (fertilisers, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDateAsync(int fieldId, DateTime startDate, DateTime endDate, int? fertiliserId, bool confirm)
    {
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");
        string url = ApiurlHelper.FetchTotalNFromFertiliserBasedOnManIdAndAppDateAPI;
        if (fertiliserId.HasValue)
        {
            url += $"&fertiliserId={fertiliserId.Value}";
        }
        url = string.Format(url, fieldId, fromdate, toDate, confirm);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.TotalN?.ToObject<decimal?>() ?? 0m, url);
        return (data ?? 0, error);
    }
    public async Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds)
    {
        Error error = new Error();
        string message = string.Empty;
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var content = new StringContent(fertiliserIds, Encoding.UTF8, "application/json");
            var url = ApiurlHelper.DeleteFertiliserByIdsAPI;
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };
            var response = await httpClient.SendAsync(requestMessage);
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper?.Data is JObject data)
            {
                message = data["message"]?.Value<string>() ?? string.Empty;
            }
            else
            {
                if (responseWrapper?.Error is JToken errorToken)
                {
                    error = errorToken.ToObject<Error>() ?? new Error();                    
                        _logger.LogError(_logError, error.Code, error.Message, error.Stack, error.Path);
                    
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
    public async Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserByIdAPI, fertiliserId);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<FertiliserManureDataViewModel>(), url);
        return (data ?? new FertiliserManureDataViewModel(), error);
    }
    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrientAsync(int fertiliserId, int farmId, int harvestYear)
    {
        string url = string.Format(ApiurlHelper.FetchFieldWithSameDateAndNutrientAPI, fertiliserId, farmId, harvestYear);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<FertiliserAndOrganicManureUpdateResponse>>(), url);
        return (data ?? new List<FertiliserAndOrganicManureUpdateResponse>(), error);
    }
    public async Task<(List<FertiliserManure>, Error?)> UpdateFertiliserAsync(string fertliserData)
    {
        Error? error = null;
        List<FertiliserManure> fertiliser = new List<FertiliserManure>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(ApiurlHelper.UpdateFertiliserAPI, new StringContent(fertliserData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode && responseWrapper?.Data?.FertiliserManure is JToken fertiliserManureToken)
            {
                List<FertiliserManure> fertilisers = fertiliserManureToken.ToObject<List<FertiliserManure>>()
                    ?? new List<FertiliserManure>();
                if (fertilisers.Count > 0)
                {
                    fertiliser.AddRange(fertilisers);
                }
            }
            else if (responseWrapper?.Error is JToken errorToken)
            {
                error = errorToken.ToObject<Error>();

                if (error != null)
                {
                    _logger.LogError(_logError, error.Code, error.Message, error.Stack, error.Path);

                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error
            {
                Message = Resource.MsgServiceNotAvailable
            };
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            error = new Error
            {
                Message = ex.Message
            };
            _logger.LogError(ex, ex.Message);
        }

        return (fertiliser, error);
    }
    public async Task<(decimal?, Error)> FetchTotalNByManagementPeriodIDAsync(int managementPeriodID)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserTotalNByManagementPeriodIDAPI, managementPeriodID);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.TotalN?.ToObject<decimal?>() ?? 0m, url);
        return (data ?? 0, error);

    }

    public async Task<(string?, Error?)> FetchFertiliserManureClosedPeriodAsync(
    int countryId, int cropTypeId, int? nvzProgramId)
    {
        string url = nvzProgramId == null
            ? string.Format(ApiurlHelper.FetchFertiliserManureClosedPeriodAPI, countryId, cropTypeId)
            : string.Format(ApiurlHelper.FetchFertiliserManureClosedPeriodByNvzIdAPI, countryId, cropTypeId, nvzProgramId);

        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ClosedPeriod.ToObject<string>(), url);
        return (data ?? string.Empty, error);

    }

    public async Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumnAsync(int managementPeriodID, bool isAutumn)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserTotalNByManagementPeriodIDIsAutumnAPI, managementPeriodID, isAutumn);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.TotalN?.ToObject<decimal?>() ?? 0m, url);
        return (data ?? 0, error);
    }

    private async Task<(T? data, Error? error)> HandleApiRequest<T>(Func<ResponseWrapper, T> mapData, string url)
    {
        Error? error = null;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(url);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                if (responseWrapper?.Data != null)
                {
                    return (mapData(responseWrapper), null);
                }
            }
            else
            {
                if (responseWrapper?.Error is JToken errorToken)
                {
                    error = errorToken.ToObject<Error>();
                    _logger.LogError(_logError,
                        error?.Code, error?.Message, error?.Stack, error?.Path);
                }
            }
        }
        catch (HttpRequestException hre)
        {
            error = new Error { Message = Resource.MsgServiceNotAvailable };
            _logger.LogError(hre, hre.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return (default, error);
    }
}
