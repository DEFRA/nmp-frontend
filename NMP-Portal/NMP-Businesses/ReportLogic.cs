using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class ReportLogic(ILogger<ReportLogic> logger, IReportService reportService) : IReportLogic
{
    private readonly ILogger<ReportLogic> _logger = logger;
    private readonly IReportService _reportService = reportService;

    public async Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        _logger.LogTrace("Adding N Loading Farm details");
        return await _reportService.AddNutrientsLoadingFarmDetailsAsync(nutrientsLoadingFarmDetailsData);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        _logger.LogTrace("Adding N Loading live stock");
        return await _reportService.AddNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStockData);
    }

    public async Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        _logger.LogTrace("Adding N Loading manures");
        return await _reportService.AddNutrientsLoadingManuresAsync(nutrientsLoadingManure);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId)
    {
        _logger.LogTrace("Deleting N Loading live stock by Id");
        return await _reportService.DeleteNutrientsLoadingLivestockByIdAsync(nutrientsLoadingLivestockId);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId)
    {
        _logger.LogTrace("Deleting N loading manure by Id");
        return await _reportService.DeleteNutrientsLoadingManureByIdAsync(nutrientsLoadingManureId);
    }

    public async Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYear(int farmId, int year)
    {
        _logger.LogTrace("Fetching live stock by Farm Id and Year");
        return await _reportService.FetchLivestockByFarmIdAndYear(farmId, year);
    }

    public async Task<(CommonResponse, Error)> FetchLivestockGroupById(int livestockGroupId)
    {
        _logger.LogTrace("Fetching livestock group by Id");
        return await _reportService.FetchLivestockGroupById(livestockGroupId);
    }

    public async Task<(List<CommonResponse>, Error)> FetchLivestockGroupList()
    {
        _logger.LogTrace("Fetch livestock group list");
        return await _reportService.FetchLivestockGroupList();
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypes()
    {
        _logger.LogTrace("Fetching livestock types");
        return await _reportService.FetchLivestockTypes(); 
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupId(int livestockGroupId)
    {
        _logger.LogTrace("Fetching livestock types by group Id");
        return await _reportService.FetchLivestockTypesByGroupId(livestockGroupId);
    }

    public async Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching N Loading farm details by FarmId");
        return await _reportService.FetchNutrientsLoadingFarmDetailsByFarmId(farmId);
    }

    public async Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year)
    {
        _logger.LogTrace("Fetching N Loading farm details by FarmId and Year");
        return await _reportService.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(farmId, year);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id)
    {
        _logger.LogTrace("Fetching N Loading livestock by Id");
        return await _reportService.FetchNutrientsLoadingLiveStockByIdAsync(id);
    }

    public async Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching N Loading Manures by FarmId");
        return await _reportService.FetchNutrientsLoadingManuresByFarmId(farmId);
    }

    public async Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id)
    {
        _logger.LogTrace("Fetching N Loading Manures by Id");
        return await _reportService.FetchNutrientsLoadingManuresByIdAsync(id);
    }

    public async Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        _logger.LogTrace("Update N Loading Farm Details");
        return await _reportService.UpdateNutrientsLoadingFarmDetailsAsync(nutrientsLoadingFarmDetailsData);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        _logger.LogTrace("Update N Loading livestack");
        return await _reportService.UpdateNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStockData);
    }

    public async Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        _logger.LogTrace("Update Nutrients Loading Manures");
        return await _reportService.UpdateNutrientsLoadingManuresAsync(nutrientsLoadingManure);
    }
}
