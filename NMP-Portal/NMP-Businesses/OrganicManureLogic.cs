using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Collections.Generic;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class OrganicManureLogic(ILogger<OrganicManureLogic> logger, IOrganicManureService organicManureService) : IOrganicManureLogic
{
    private readonly ILogger<OrganicManureLogic> _logger = logger;
    private readonly IOrganicManureService _organicManureService = organicManureService;
    public async Task<(bool, Error?)> AddOrganicManuresAsync(string organicManureData)
    {
        _logger.LogTrace("OrganicManureLogic : AddOrganicManuresAsync() called");
        return await _organicManureService.AddOrganicManuresAsync(organicManureData);
    }

    public async Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds)
    {
        _logger.LogTrace("OrganicManureLogic : DeleteOrganicManureByIdAsync() called");
        return await _organicManureService.DeleteOrganicManureByIdAsync(orgManureIds);
    }


    public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString)
    {
        _logger.LogTrace("OrganicManureLogic : FetchAutumnCropNitrogenUptake() called");
        return await _organicManureService.FetchAutumnCropNitrogenUptakeAsync(jsonString);
    }

    public async Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodID(int managementPeriodID)
    {
        _logger.LogTrace("OrganicManureLogic : FetchAvailableNByManagementPeriodID() called");
        return await _organicManureService.FetchAvailableNByManagementPeriodIDAsync(managementPeriodID);
    }

    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeByFarmIdAndHarvestYear() called");
        return await _organicManureService.FetchCropTypeByFarmIdAndHarvestYearAsync(farmId, harvestYear);
    }

    public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeByFieldIdAndHarvestYear() called");
        return await _organicManureService.FetchCropTypeByFieldIdAndHarvestYearAsync(fieldId, year, confirm);
    }

    public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeLinkingByCropTypeId() called");
        return await _organicManureService.FetchCropTypeLinkingByCropTypeIdAsync(cropTypeId);
    }

    public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmId(int farmId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeByFarmId() called");
        return await _organicManureService.FetchFarmManureTypeByFarmIdAsync(farmId);
    }

    public async Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeId(int farmId, int ManureTypeId, string ManureTypeName)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeCheckByFarmIdAndManureTypeId() called");
        return await _organicManureService.FetchFarmManureTypeCheckByFarmIdAndManureTypeIdAsync(farmId, ManureTypeId, ManureTypeName);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFieldByFarmIdAndHarvestYearAndCropGroupName() called");
        return await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsync(harvestYear, farmId, cropGroupName);
    }

    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureType(int fertiliserId, int farmId, int harvestYear)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFieldWithSameDateAndManureType() called");
        return await _organicManureService.FetchFieldWithSameDateAndManureTypeAsync(fertiliserId, farmId, harvestYear);
    }



    public async Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName() called");
        return await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsync(harvestYear, fieldIds, cropGroupName, cropOrder);
    }

    public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrient(string jsonData)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMannerCalculateNutrient() called");
        return await _organicManureService.FetchMannerCalculateNutrientAsync(jsonData);
    }



    public async Task<(List<int>, Error?)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure() called");
        return await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsync(fieldId, year, confirm);
    }
    public async Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManure(int managementId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypsIdsByManIdFromOrgManure() called");
        return await _organicManureService.FetchManureTypsIdsByManIdFromOrgManureAsync(managementId);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeById() called");
        return await _organicManureService.FetchMoisterTypeByIdAsync(moisterTypeId);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeDefaultByApplicationDate() called");
        return await _organicManureService.FetchMoisterTypeDefaultByApplicationDateAsync(applicationDate);
    }

    public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeList() called");
        return await _organicManureService.FetchMoisterTypeListAsync();
    }

    public async Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYear(int farmId, int year)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureByFarmIdAndYear() called");
        return await _organicManureService.FetchOrganicManureByFarmIdAndYearAsync(farmId, year);
    }

    public async Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureById(int id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureById() called");
        return await _organicManureService.FetchOrganicManureByIdAsync(id);
    }

    public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureExistanceByDateRange() called");
        return await _organicManureService.FetchOrganicManureExistanceByDateRangeAsync(managementId, dateFrom, dateTo, isConfirm, organicManureId, isSlurryOnly);
    }

    public async Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString)
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainfallByPostcodeAndDateRange() called");
        return await _organicManureService.FetchRainfallByPostcodeAndDateRangeAsync(jsonString);
    }

    public async Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeById() called");
        return await _organicManureService.FetchRainTypeByIdAsync(rainTypeId);
    }

    public async Task<(RainTypeResponse, Error)> FetchRainTypeDefault()
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeDefault() called");
        return await _organicManureService.FetchRainTypeDefaultAsync();
    }

    public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeList() called");
        return await _organicManureService.FetchRainTypeListAsync();
    }

    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeId(int soilTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchSoilTypeSoilTextureBySoilTypeId() called");
        return await _organicManureService.FetchSoilTypeSoilTextureBySoilTypeIdAsync(soilTypeId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(int fieldId, DateTime startDate, DateTime endDate, bool confirm, bool isGreenFoodCompost, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost() called");
        return await _organicManureService.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompostAsync(fieldId, startDate, endDate, confirm, isGreenFoodCompost, organicManureId);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDate(int fieldId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedByFieldIdAppDate() called");
        return await _organicManureService.FetchTotalNBasedByFieldIdAppDateAsync(fieldId, startDate, endDate, confirm, organicManureId);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnManIdAndAppDate() called");
        return await _organicManureService.FetchTotalNBasedOnManIdAndAppDateAsync(managementId, startDate, endDate, confirm, organicManureId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdAndAppDate(int cropId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnCropIdAndAppDate() called");
        return await _organicManureService.FetchTotalNBasedOnCropIdAndAppDateAsync(cropId, startDate, endDate, confirm, organicManureId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnManIdFromOrgManureAndFertiliser() called");
        return await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsync(managementId, confirm, fertiliserId, organicManureId);
    }
    public async Task<(decimal, Error)> FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(int cropId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser() called");
        return await _organicManureService.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserAsync(cropId, confirm, fertiliserId, organicManureId);
    }

    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedById(int windspeedId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedById() called");
        return await _organicManureService.FetchWindspeedByIdAsync(windspeedId);
    }

    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefault()
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedDataDefault() called");
        return await _organicManureService.FetchWindspeedDataDefaultAsync();
    }

    public async Task<(List<WindspeedResponse>, Error?)> FetchWindspeedList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedList() called");
        return await _organicManureService.FetchWindspeedListAsync();
    }

    public async Task<(List<OrganicManure>, Error)> UpdateOrganicManure(string organicManureData)
    {
        _logger.LogTrace("OrganicManureLogic : UpdateOrganicManure() called");
        return await _organicManureService.UpdateOrganicManureAsync(organicManureData);
    }

    public async Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeById(int id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeById() called");
        return await _organicManureService.FetchFarmManureTypeByIdAsync(id);
    }
    public async Task<(string?, Error?)> FetchOrganicManureClosedPeriod(OrganicClosedPeriodRequest organicClosedPeriodRequest)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureClosedPeriod() called");
        return await _organicManureService.FetchOrganicManureClosedPeriodAsync(organicClosedPeriodRequest);
    }
    public async Task<(bool, Error)> FetchLivestockManureExistanceByDateRange(int cropId, string dateFrom, string dateTo, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchLivestockManureExistanceByDateRange() called");
        return await _organicManureService.FetchLivestockManureExistanceByDateRangeAsync(cropId, dateFrom, dateTo, organicManureId);
    }

    public async Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRange(int cropId, string dateFrom, string dateTo, int? organicManureId, bool isPoultry)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalApplicationRateByDateRange() called");
        return await _organicManureService.FetchTotalApplicationRateByDateRangeAsync(cropId, dateFrom, dateTo, organicManureId, isPoultry);
    }

    public async Task<(bool, Error)> CheckGreenCompostExistanceByDateRange(int fieldId, string dateFrom, string dateTo, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : CheckGreenCompostExistanceByDateRange() called");
        return await _organicManureService.CheckGreenCompostExistanceByDateRangeAsync(fieldId, dateFrom, dateTo, organicManureId);
    }
    public async Task<(int?, Error?)> FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroup(int cropTypeId, int soilTypeId, int residueGroup)
    {
        _logger.LogTrace("OrganicManureLogic : FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroup() called");
        return await _organicManureService.FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroupAsync(cropTypeId, soilTypeId, residueGroup);
    }
    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeIdAsync(int soilTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchSoilTypeSoilTextureBySoilTypeIdAsync() called");
        return await _organicManureService.FetchSoilTypeSoilTextureBySoilTypeIdAsync(soilTypeId);
    }
    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchAllCropTypeLinking()
    {
        _logger.LogTrace("OrganicManureLogic : FetchAllCropTypeLinking() called");
        return await _organicManureService.FetchAllCropTypeLinkingAsync();
    }
}