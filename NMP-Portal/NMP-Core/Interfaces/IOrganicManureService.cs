using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IOrganicManureService
{
    Task<(List<ManureCropTypeResponse>,Error?)> FetchCropTypeByFarmIdAndHarvestYearAsync(int farmId,int harvestYear);
    Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, int farmId, string? cropGroupName);
    Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
    
    Task<(bool, Error?)> AddOrganicManuresAsync(string organicManureData);

    Task<(RainTypeResponse, Error)> FetchRainTypeDefaultAsync();
    Task<int> FetchRainfallByPostcodeAndDateRangeAsync(string jsonString);

    Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefaultAsync();
    Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDateAsync(string applicationDate);
    Task<(List<RainTypeResponse>, Error)> FetchRainTypeListAsync();
    Task<(List<WindspeedResponse>, Error?)> FetchWindspeedListAsync();
    Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeListAsync();
    Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDateAsync(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedOnCropIdAndAppDateAsync(int cropId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYearAsync(int fieldId, int year,bool confirm);
    Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeIdAsync(int cropTypeId);
    Task<(List<int>, Error?)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsync(int fieldId, int year,bool confirm);
    Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManureAsync(int managementId);
    Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsync(int managementId, bool confirm, int? fertiliserId, int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserAsync(int cropId, bool confirm, int? fertiliserId, int? organicManureId);
    Task<(bool, Error)> FetchOrganicManureExistanceByDateRangeAsync(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly);
    Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptakeAsync(string jsonString);
    Task<(RainTypeResponse, Error)> FetchRainTypeByIdAsync(int rainTypeId);
    Task<(WindspeedResponse?, Error?)> FetchWindspeedByIdAsync(int windspeedId);
    Task<(MoistureTypeResponse, Error)> FetchMoisterTypeByIdAsync(int moisterTypeId);

    Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmIdAsync(int farmId);
    Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrientAsync(string jsonData);
    Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeIdAsync(int soilTypeId);
    Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompostAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm,bool isGreenFoodCompost,int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureByIdAsync(int id);
    Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYearAsync(int farmId, int year);
    Task<(string, Error)> DeleteOrganicManureByIdAsync(string orgManureIds);
    Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeIdAsync(int farmId, int ManureTypeId, string ManureTypeName);
    Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureTypeAsync(int fertiliserId, int farmId, int harvestYear);
    Task<(List<OrganicManure>, Error)> UpdateOrganicManureAsync(string organicManureData);
    Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodIDAsync(int managementPeriodID);
    Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeByIdAsync(int id);
    Task<(string?, Error?)> FetchOrganicManureClosedPeriodAsync(OrganicClosedPeriodRequest organicClosedPeriodRequest);
    Task<(bool, Error)> FetchLivestockManureExistanceByDateRangeAsync(int cropId, string dateFrom, string dateTo, int? organicManureId);
    Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRangeAsync(int cropId, string dateFrom, string dateTo, int? organicManureId, bool isPoultry);
    Task<(bool, Error)> CheckGreenCompostExistanceByDateRangeAsync(int fieldId, string dateFrom, string dateTo, int? organicManureId);
    Task<(int?, Error?)> FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroupAsync(int cropTypeId, int soilTypeId, int residueGroup);
    Task<(List<CropTypeLinkingResponse>, Error)> FetchAllCropTypeLinkingAsync();
}
