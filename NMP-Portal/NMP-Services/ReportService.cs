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
public class ReportService(ILogger<ReportService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IReportService
{
    private readonly ILogger<ReportService> _logger = logger;
    private const string _applicationJson = "application/json";

    public async Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetailsData);

        var (data, error) = await SendRequestAsync(
                client => client.PostAsync(
                    ApiurlHelper.AddNutrientsLoadingFarmDetailsAPI,
                    new StringContent(jsonData, Encoding.UTF8, _applicationJson)),
                wrapper =>
                {
                    if (wrapper?.Data is JObject obj)
                        return obj.ToObject<NutrientsLoadingFarmDetail>()
                               ?? new NutrientsLoadingFarmDetail();

                    return new NutrientsLoadingFarmDetail();
                }, _logger);

        return (data ?? new NutrientsLoadingFarmDetail(), error);
    }

    public async Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year)
    {
        var (data, error) = await SendRequestAsync<NutrientsLoadingFarmDetail>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsLoadingFarmDetailsByfarmIdAndYearAPI, farmId, year)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["NutrientsLoadingFarmDetails"] is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingFarmDetail>()
                           ?? null;
                }

                return null;
            }, _logger);

        return (data ?? null, error);
    }
    public async Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(
    NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingFarmDetailsData);

        var (data, error) = await SendRequestAsync<NutrientsLoadingFarmDetail>(
            client => client.PutAsync(
                ApiurlHelper.UpdateNutrientsLoadingFarmDetailsAPI,
                new StringContent(jsonData, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["NutrientsLoadingFarmDetails"] is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingFarmDetail>()
                           ?? new NutrientsLoadingFarmDetail();
                }

                return new NutrientsLoadingFarmDetail();
            }, _logger);

        return (data ?? new NutrientsLoadingFarmDetail(), error);
    }
    public async Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmIdAsync(int farmId)
    {
        var (data, error) = await SendRequestAsync<List<NutrientsLoadingManures>>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsloadingFarmDetailsFarmIdAPI, farmId)
            ),
            wrapper =>
            {
                if (wrapper?.Data?.NutrientsLoadingFarmDetails is JArray array)
                {
                    return array.ToObject<List<NutrientsLoadingManures>>()
                           ?? new List<NutrientsLoadingManures>();
                }

                return new List<NutrientsLoadingManures>();
            }, _logger);

        return (data ?? new List<NutrientsLoadingManures>(), error);
    }

    public async Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        var (data, error) = await SendRequestAsync<NutrientsLoadingManures>(
            client => client.PostAsync(
                ApiurlHelper.AddNutrientsLoadingManureAPI,
                new StringContent(nutrientsLoadingManure, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingManures>()
                           ?? new NutrientsLoadingManures();
                }

                return new NutrientsLoadingManures();
            }, _logger);

        return (data ?? new NutrientsLoadingManures(), error);
    }

    public async Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAsync(int farmId)
    {
        var (data, error) = await SendRequestAsync<List<NutrientsLoadingFarmDetail>>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsLoadingFarmDetailsByFarmIdAPI, farmId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                {
                    return token.ToObject<List<NutrientsLoadingFarmDetail>>()
                           ?? new List<NutrientsLoadingFarmDetail>();
                }

                return new List<NutrientsLoadingFarmDetail>();
            }, _logger);

        return (data ?? new List<NutrientsLoadingFarmDetail>(), error);
    }

    public async Task<(List<CommonResponse>, Error)> FetchLivestockGroupListAsync()
    {
        var (data, error) = await SendRequestAsync<List<CommonResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchLivestockGroupListAPI),
            wrapper =>
            {
                if (wrapper?.Data?.LivestockGroups is JToken token)
                {
                    return token.ToObject<List<CommonResponse>>()
                           ?? new List<CommonResponse>();
                }

                return new List<CommonResponse>();
            }, _logger);

        return (data ?? new List<CommonResponse>(), error);
    }

    public async Task<(CommonResponse, Error)> FetchLivestockGroupByIdAsync(int livestockGroupId)
    {
        var (data, error) = await SendRequestAsync<CommonResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchLivestockManureGroupByIdAPI, livestockGroupId)
            ),
            wrapper =>
            {
                if (wrapper?.Data?.livestockGroup is JToken token)
                {
                    return token.ToObject<CommonResponse>()
                           ?? new CommonResponse();
                }

                return new CommonResponse();
            }, _logger);

        return (data ?? new CommonResponse(), error);
    }

    public async Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id)
    {
        var (data, error) = await SendRequestAsync<NutrientsLoadingManures>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsLoadingManureByIdAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?.NutrientsLoadingManure is JToken token)
                {
                    return token.ToObject<NutrientsLoadingManures>()
                           ?? new NutrientsLoadingManures();
                }

                return new NutrientsLoadingManures();
            }, _logger);

        return (data ?? new NutrientsLoadingManures(), error);
    }
    public async Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        var (data, error) = await SendRequestAsync<NutrientsLoadingManures>(
            client => client.PutAsync(
                ApiurlHelper.UpdateNutrientsLoadingManureAPI,
                new StringContent(nutrientsLoadingManure, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingManures>()
                           ?? new NutrientsLoadingManures();
                }

                return new NutrientsLoadingManures();
            }, _logger);

        return (data ?? new NutrientsLoadingManures(), error);
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupIdAsync(int livestockGroupId)
    {
        var (data, error) = await SendRequestAsync<List<LivestockTypeResponse>>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchLivestockTypesByGroupIdAPI, livestockGroupId)
            ),
            wrapper =>
            {
                if (wrapper?.Data?.livestockTypes is JToken token)
                {
                    return token.ToObject<List<LivestockTypeResponse>>()
                           ?? new List<LivestockTypeResponse>();
                }

                return new List<LivestockTypeResponse>();
            }, _logger);

        return (data ?? new List<LivestockTypeResponse>(), error);
    }
    public async Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId)
    {
        var (data, error) = await SendRequestAsync<string>(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteNutrientsLoadingManuresByIdAPI, nutrientsLoadingManureId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj["message"]?.Value<string>() ?? string.Empty;
                }

                return string.Empty;
            }, _logger);

        return (data ?? string.Empty, error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(
    NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingLiveStockData);

        var (data, error) = await SendRequestAsync<NutrientsLoadingLiveStock>(
            client => client.PostAsync(
                ApiurlHelper.AddNutrientsLoadingLivestockAPI,
                new StringContent(jsonData, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingLiveStock>()
                           ?? new NutrientsLoadingLiveStock();
                }

                return new NutrientsLoadingLiveStock();
            }, _logger);

        return (data ?? new NutrientsLoadingLiveStock(), error);
    }

    public async Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYearAsync(int farmId, int year)
    {
        var (data, error) = await SendRequestAsync<List<NutrientsLoadingLiveStockViewModel>>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsLoadingLivestockByFarmIdAndYearAPI, farmId, year)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                {
                    return token.ToObject<List<NutrientsLoadingLiveStockViewModel>>()
                           ?? new List<NutrientsLoadingLiveStockViewModel>();
                }

                return new List<NutrientsLoadingLiveStockViewModel>();
            }, _logger);

        return (data ?? new List<NutrientsLoadingLiveStockViewModel>(), error);
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesAsync()
    {
        var (data, error) = await SendRequestAsync<List<LivestockTypeResponse>>(
            client => client.GetAsync(ApiurlHelper.FetchLivestockTypesAPI),
            wrapper =>
            {
                if (wrapper?.Data?.LivestockTypes is JToken token)
                {
                    return token.ToObject<List<LivestockTypeResponse>>()
                           ?? new List<LivestockTypeResponse>();
                }

                return new List<LivestockTypeResponse>();
            }, _logger);

        return (data ?? new List<LivestockTypeResponse>(), error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id)
    {
        var (data, error) = await SendRequestAsync<NutrientsLoadingLiveStock>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchNutrientsLoadingLiveStockByIdAPI, id)
            ),
            wrapper =>
            {
                if (wrapper?.Data?.records is JToken token)
                {
                    return token.ToObject<NutrientsLoadingLiveStock>()
                           ?? new NutrientsLoadingLiveStock();
                }

                return new NutrientsLoadingLiveStock();
            }, _logger);

        return (data ?? new NutrientsLoadingLiveStock(), error);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId)
    {
        var (data, error) = await SendRequestAsync<string>(
            client => client.DeleteAsync(
                string.Format(ApiurlHelper.DeleteNutrientsLoadingLivestockByIdAPI, nutrientsLoadingLivestockId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JObject obj)
                {
                    return obj["message"]?.Value<string>() ?? string.Empty;
                }

                return string.Empty;
            }, _logger);

        return (data ?? string.Empty, error);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(
    NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        string jsonData = JsonConvert.SerializeObject(nutrientsLoadingLiveStockData);

        var (data, error) = await SendRequestAsync<NutrientsLoadingLiveStock>(
            client => client.PutAsync(
                ApiurlHelper.UpdateNutrientsLoadingLivestockAPI,
                new StringContent(jsonData, Encoding.UTF8, _applicationJson)
            ),
            wrapper =>
            {
                if (wrapper?.Data?["NutrientsLoadingLiveStock"] is JObject obj)
                {
                    return obj.ToObject<NutrientsLoadingLiveStock>()
                           ?? new NutrientsLoadingLiveStock();
                }

                return new NutrientsLoadingLiveStock();
            }, _logger);

        return (data ?? new NutrientsLoadingLiveStock(), error);
    }

    public async Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropIdAsync(int cropId)
    {
        var (data, error) = await SendRequestAsync<OrganicManureFertiliserResponse>(
            client => client.GetAsync(
                string.Format(ApiurlHelper.FetchOrganicManuresFertilisersByCropIdAPI, cropId)
            ),
            wrapper =>
            {
                if (wrapper?.Data is JToken token)
                {
                    return token.ToObject<OrganicManureFertiliserResponse>()
                           ?? new OrganicManureFertiliserResponse();
                }

                return new OrganicManureFertiliserResponse();
            }, _logger);

        return (data ?? new OrganicManureFertiliserResponse(), error);
    }
    public async Task<(List<ReportYearLastUpdatedDateResponse>, Error?)> FetchLastUpdatedDateByFarmIdAndYearAsync(int farmId, string years)
    {
        var (data, error) = await SendRequestAsync<List<ReportYearLastUpdatedDateResponse>>(
           client => client.GetAsync(
               string.Format(ApiurlHelper.FetchYearsAndLastUpdatedDateByFarmIDAndYearAPI, farmId, years)
           ),
           wrapper =>
           {
               if (wrapper?.Data is JArray array)
               {
                   return array.ToObject<List<ReportYearLastUpdatedDateResponse>>();
               }
           
               return null;
           }, _logger
        );

        return (data ?? null, error);
    }
}
