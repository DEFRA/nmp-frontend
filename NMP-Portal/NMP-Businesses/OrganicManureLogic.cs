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
    private readonly ILogger<OrganicManureLogic> _logger= logger;
    private readonly IOrganicManureService _organicManureService= organicManureService;
    public async Task<(bool, Error)> AddOrganicManuresAsync(string organicManureData)
    {
        _logger.LogTrace("OrganicManureLogic : AddOrganicManuresAsync() called");
        return await _organicManureService.AddOrganicManuresAsync(organicManureData);
    }

    public async Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds)
    {
        _logger.LogTrace("OrganicManureLogic : DeleteOrganicManureByIdAsync() called");
        return await _organicManureService.DeleteOrganicManureByIdAsync(orgManureIds);
    }

    public async Task<(string, Error)> FetchApplicationMethodById(int Id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchApplicationMethodById() called");
        return await _organicManureService.FetchApplicationMethodById(Id);
    }

    public async Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid)
    {
        _logger.LogTrace("OrganicManureLogic : FetchApplicationMethodList() called");
        return await _organicManureService.FetchApplicationMethodList(fieldType, isLiquid);
    }

    public async Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString)
    {
        _logger.LogTrace("OrganicManureLogic : FetchAutumnCropNitrogenUptake() called");
        return await _organicManureService.FetchAutumnCropNitrogenUptake(jsonString);
    }

    public async Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodID(int managementPeriodID)
    {
        _logger.LogTrace("OrganicManureLogic : FetchAvailableNByManagementPeriodID() called");
        return await _organicManureService.FetchAvailableNByManagementPeriodID(managementPeriodID);
    }

    public async Task<(List<ManureCropTypeResponse>, Error?)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeByFarmIdAndHarvestYear() called");
        return await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(farmId, harvestYear);
    }

    public async Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeByFieldIdAndHarvestYear() called");
        return await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(fieldId, year, confirm);
    }

    public async Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchCropTypeLinkingByCropTypeId() called");
        return await _organicManureService.FetchCropTypeLinkingByCropTypeId(cropTypeId);
    }

    public async Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmId(int farmId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeByFarmId() called");
        return await _organicManureService.FetchFarmManureTypeByFarmId(farmId);
    }

    public async Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeId(int farmId, int ManureTypeId, string ManureTypeName)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeCheckByFarmIdAndManureTypeId() called");
        return await _organicManureService.FetchFarmManureTypeCheckByFarmIdAndManureTypeId(farmId, ManureTypeId, ManureTypeName);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFieldByFarmIdAndHarvestYearAndCropGroupName() called");
        return await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(harvestYear, farmId, cropGroupName);
    }

    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureType(int fertiliserId, int farmId, int harvestYear)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFieldWithSameDateAndManureType() called");
        return await _organicManureService.FetchFieldWithSameDateAndManureType(fertiliserId, farmId, harvestYear);
    }

    public async Task<(string, Error)> FetchIncorporationDelayById(int Id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchIncorporationDelayById() called");
        return await _organicManureService.FetchIncorporationDelayById(Id);
    }

    public async Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
    {
        _logger.LogTrace("OrganicManureLogic : FetchIncorporationDelaysByMethodIdAndApplicableFor() called");
        return await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(methodId, applicableFor);
    }

    public async Task<(string, Error)> FetchIncorporationMethodById(int Id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchIncorporationMethodById() called");
        return await _organicManureService.FetchIncorporationMethodById(Id);
    }

    public async Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor)
    {
        _logger.LogTrace("OrganicManureLogic : FetchIncorporationMethodsByApplicationId() called");
        return await _organicManureService.FetchIncorporationMethodsByApplicationId(appId, applicableFor);
    }

    public async Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchIsPerennialByCropTypeId() called");
        return await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
    }

    public async Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName() called");
        return await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(harvestYear, fieldIds, cropGroupName, cropOrder);
    }

    public async Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrient(string jsonData)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMannerCalculateNutrient() called");
        return await _organicManureService.FetchMannerCalculateNutrient(jsonData);
    }

    public async Task<(CommonResponse, Error?)> FetchManureGroupById(int manureGroupId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureGroupById() called");
        return await _organicManureService.FetchManureGroupById(manureGroupId);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchManureGroupList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureGroupList() called");
        return await _organicManureService.FetchManureGroupList();
    }

    public async Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypeByManureTypeId() called");
        return await _organicManureService.FetchManureTypeByManureTypeId(manureTypeId);
    }

    public async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypeList() called");
        (List < ManureType > manures, Error? error) = await _organicManureService.FetchManureTypeList(manureGroupId, countryId);
        return (manures.OrderBy(m => m.SortOrder).ToList(), error);
    }

    public async Task<(List<int>, Error)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure() called");
        return await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(fieldId, year, confirm);
    }
    public async Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManure(int managementId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchManureTypsIdsByManIdFromOrgManure() called");
        return await _organicManureService.FetchManureTypsIdsByManIdFromOrgManure(managementId);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeById() called");
        return await _organicManureService.FetchMoisterTypeById(moisterTypeId);
    }

    public async Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate)
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeDefaultByApplicationDate() called");
        return await _organicManureService.FetchMoisterTypeDefaultByApplicationDate(applicationDate);
    }

    public async Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchMoisterTypeList() called");
        return await _organicManureService.FetchMoisterTypeList();
    }

    public async Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYear(int farmId, int year)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureByFarmIdAndYear() called");
        return await _organicManureService.FetchOrganicManureByFarmIdAndYear(farmId, year);
    }

    public async Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureById(int id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureById() called");
        return await _organicManureService.FetchOrganicManureById(id);
    }

    public async Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly)
    {
        _logger.LogTrace("OrganicManureLogic : FetchOrganicManureExistanceByDateRange() called");
        return await _organicManureService.FetchOrganicManureExistanceByDateRange(managementId, dateFrom, dateTo, isConfirm, organicManureId, isSlurryOnly);
    }

    public async Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString)
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainfallByPostcodeAndDateRange() called");
        return await _organicManureService.FetchRainfallByPostcodeAndDateRange(jsonString);
    }

    public async Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeById() called");
        return await _organicManureService.FetchRainTypeById(rainTypeId);
    }

    public async Task<(RainTypeResponse, Error)> FetchRainTypeDefault()
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeDefault() called");
        return await _organicManureService.FetchRainTypeDefault();
    }

    public async Task<(List<RainTypeResponse>, Error)> FetchRainTypeList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchRainTypeList() called");
        return await _organicManureService.FetchRainTypeList();
    }

    public async Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeId(int soilTypeId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchSoilTypeSoilTextureBySoilTypeId() called");
        return await _organicManureService.FetchSoilTypeSoilTextureBySoilTypeId(soilTypeId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(int fieldId, DateTime startDate, DateTime endDate, bool confirm, bool isGreenFoodCompost, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost() called");
        return await _organicManureService.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(fieldId, startDate, endDate, confirm, isGreenFoodCompost, organicManureId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnManIdAndAppDate() called");
        return await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endDate, confirm, organicManureId);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm, int? fertiliserId, int? organicManureId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchTotalNBasedOnManIdFromOrgManureAndFertiliser() called");
        return await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, confirm, fertiliserId, organicManureId);
    }

    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedById(int windspeedId)
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedById() called");
        return await _organicManureService.FetchWindspeedById(windspeedId);
    }

    public async Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefault()
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedDataDefault() called");
        return await _organicManureService.FetchWindspeedDataDefault();
    }

    public async Task<(List<WindspeedResponse>, Error?)> FetchWindspeedList()
    {
        _logger.LogTrace("OrganicManureLogic : FetchWindspeedList() called");
        return await _organicManureService.FetchWindspeedList();
    }

    public async Task<(List<OrganicManure>, Error)> UpdateOrganicManure(string organicManureData)
    {
        _logger.LogTrace("OrganicManureLogic : UpdateOrganicManure() called");
        return await _organicManureService.UpdateOrganicManure(organicManureData);
    }

    public async Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeById(int id)
    {
        _logger.LogTrace("OrganicManureLogic : FetchFarmManureTypeById() called");
        return await _organicManureService.FetchFarmManureTypeById(id);
    }

}