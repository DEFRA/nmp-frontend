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
    public FertiliserManureService(ILogger<FertiliserManureService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        _logger = logger;
    }
    public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        string url = string.Format(ApiurlHelper.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsyncAPI,
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
    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        string url = string.Format(ApiurlHelper.FetchCropTypeByFarmIdAndHarvestYearAsyncAPI, harvestYear, farmId);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<ManureCropTypeResponse>>(), url);
        return (data ?? new List<ManureCropTypeResponse>(), error);

    }
    public async Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        string url = string.Empty;
        if (!string.IsNullOrWhiteSpace(cropGroupName))
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsyncAPI, harvestYear, cropGroupName, farmId);
        }
        else
        {
            url = string.Format(ApiurlHelper.FetchFieldByFarmIdAndHarvestYearAsyncAPI, harvestYear, farmId);
        }
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<CommonResponse>>(), url);
        return (data ?? new List<CommonResponse>(), error);

    }

    public async Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations()
    {
        string url = ApiurlHelper.FetchInOrganicManureDurationsAsyncAPI;
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.InorganicManureDurations.ToObject<List<InOrganicManureDurationResponse>>(), url);
        return (data ?? new List<InOrganicManureDurationResponse>(), error);
    }
    public async Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id)
    {
        string url = string.Format(ApiurlHelper.FetchInOrganicManureDurationsByIdAsyncAPI, id);
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
            var response = await httpClient.PostAsync(ApiurlHelper.AddFertiliserManuresAsyncAPI, new StringContent(fertiliserManure, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                List<FertiliserManure> fertiliser = responseWrapper.Data.FertiliserManure.ToObject<List<FertiliserManure>>();
                if (fertiliser != null && fertiliser.Count > 0)
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
        return (fertilisers, error);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate, int? fertiliserId, bool confirm)
    {
        string fromdate = startDate.ToString("yyyy-MM-dd");
        string toDate = endDate.ToString("yyyy-MM-dd");
        string url = ApiurlHelper.FetchTotalNFromFertiliserBasedOnManIdAndAppDateAsyncAPI;
        if (fertiliserId.HasValue)
        {
            url += $"&fertiliserId={fertiliserId.Value}";
        }
        url = string.Format(url, fieldId, fromdate, toDate, confirm);
        var (data, error) = await HandleApiRequest(rw => (decimal?)rw?.Data?.TotalN == null
      ? 0m
      : rw.Data.TotalN.Value<decimal>(), url);
        return (data??0, error);
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
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error.Message = ex.Message;
            _logger.LogError(ex, ex.Message);
            throw new Exception(error.Message, ex);
        }

        return (message, error);
    }
    public async Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserByIdAPI, fertiliserId);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<FertiliserManureDataViewModel>(), url);
        return (data ?? new FertiliserManureDataViewModel(), error);
    }
    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId, int farmId, int harvestYear)
    {
        string url = string.Format(ApiurlHelper.FetchFieldWithSameDateAndNutrientAPI, fertiliserId, farmId, harvestYear);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ToObject<List<FertiliserAndOrganicManureUpdateResponse>>(), url);
        return (data ?? new List<FertiliserAndOrganicManureUpdateResponse>(), error);
    }
    public async Task<(List<FertiliserManure>, Error?)> UpdateFertiliser(string fertliserData)
    {
        Error? error = null;
        List<FertiliserManure> fertiliser = new List<FertiliserManure>();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.PutAsync(string.Format(ApiurlHelper.UpdateFertiliserAPI), new StringContent(fertliserData, Encoding.UTF8, "application/json"));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
            {
                List<FertiliserManure> fertilisers = responseWrapper.Data.FertiliserManure.ToObject<List<FertiliserManure>>();
                if (fertilisers != null && fertilisers.Count > 0)
                {
                    fertiliser.AddRange(fertilisers);
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
            error = new Error
            {
                Message = Resource.MsgServiceNotAvailable
            };
            _logger.LogError(hre, hre.Message);
            throw new Exception(error.Message, hre);
        }
        catch (Exception ex)
        {
            error = new Error
            {
                Message = ex.Message
            };
            _logger.LogError(ex, ex.Message);
            throw new Exception(error.Message, ex);
        }

        return (fertiliser, error);
    }
    public async Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserTotalNByManagementPeriodIDAPI, managementPeriodID);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.TotalN, url);
        return (data ?? 0, error);

    }

    public async Task<(string?, Error?)> FetchFertiliserManureClosedPeriod(
    int countryId, int cropTypeId, int? nvzProgramId)
    {
        string url = nvzProgramId == null
            ? string.Format(ApiurlHelper.FetchFertiliserManureClosedPeriodAsyncAPI, countryId, cropTypeId)
            : string.Format(ApiurlHelper.FetchFertiliserManureClosedPeriodByNvzIdAsyncAPI, countryId, cropTypeId, nvzProgramId);

        var (data, error) = await HandleApiRequest(rw => rw?.Data?.ClosedPeriod.ToObject<string>(), url);
        return (data ?? string.Empty, error);

    }

    public async Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumn(int managementPeriodID, bool isAutumn)
    {
        string url = string.Format(ApiurlHelper.FetchFertiliserTotalNByManagementPeriodIDIsAutumnAsyncAPI, managementPeriodID, isAutumn);
        var (data, error) = await HandleApiRequest(rw => rw?.Data?.TotalN, url);
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
                if (responseWrapper?.Error != null)
                {
                    error = responseWrapper.Error.ToObject<Error>();
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}",
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
            throw;
        }

        return (default, error);
    }
}
