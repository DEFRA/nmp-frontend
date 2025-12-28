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
public class FertiliserManureLogic(ILogger<FertiliserManureLogic> logger, IFertiliserManureService fertiliserManureService) : IFertiliserManureLogic
{
    private readonly ILogger<FertiliserManureLogic> _logger = logger;
    private readonly IFertiliserManureService _fertiliserManureService = fertiliserManureService;
    public async Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure)
    {
        _logger.LogTrace("Adding Fertiliser Manure");
        return await _fertiliserManureService.AddFertiliserManureAsync(fertiliserManure);
    }

    public async Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds)
    {
        _logger.LogTrace("Deleting Fertiliser by Id");
        return await _fertiliserManureService.DeleteFertiliserByIdAsync(fertiliserIds);
    }

    public async Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        _logger.LogTrace("Fetching Crop Type by FarmId:{FarmId} and HarvestYear:{HarvestYear}", farmId, harvestYear);
        return await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYear(farmId, harvestYear);
    }

    public async Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId)
    {
        _logger.LogTrace("Fetching Fertiliser by Id");
        return await _fertiliserManureService.FetchFertiliserByIdAsync(fertiliserId);
    }

    public async Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        _logger.LogTrace("Fetching Field by Farm Id, Harvest Year and Crop Group name");
        return await _fertiliserManureService.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(harvestYear, farmId, cropGroupName);
    }

    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId, int farmId, int harvestYear)
    {
        _logger.LogTrace("Fetching field with samedate and nutrient");
        return await _fertiliserManureService.FetchFieldWithSameDateAndNutrient(fertiliserId, farmId, harvestYear);
    }

    public async Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations()
    {
        _logger.LogTrace("Fetching inorganic manure durations");
        return await _fertiliserManureService.FetchInOrganicManureDurations();
    }

    public async Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id)
    {
        _logger.LogTrace("Fetching Inorganic manure duration by Id");
        return await _fertiliserManureService.FetchInOrganicManureDurationsById(id);
    }

    public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        _logger.LogTrace("Fetching ManagementId by Field Id and Harvest year and Crop group name");
        return await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(harvestYear, fieldIds, cropGroupName, cropOrder);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate, int? fertiliserId, bool confirm)
    {
        _logger.LogTrace("Fetch total N based on Field Id and application date");
        return await _fertiliserManureService.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endDate, fertiliserId, confirm);
    }

    public async Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID)
    {
        _logger.LogTrace("Fetching total N by management perios Id");
        return await _fertiliserManureService.FetchTotalNByManagementPeriodID(managementPeriodID);
    }

    public async Task<(List<FertiliserManure>, Error)> UpdateFertiliser(string fertliserData)
    {
        _logger.LogTrace("Updating fertiliser");
        return await _fertiliserManureService.UpdateFertiliser(fertliserData);
    }
}