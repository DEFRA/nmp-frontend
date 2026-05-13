using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IOrganicManureService
{
    Task<(List<ManureCropTypeResponse>,Error?)> FetchCropTypeByFarmIdAndHarvestYearServiceAsync(int farmId,int harvestYear);
    Task<(List<CommonResponse>, Error?)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameServiceAsync(int harvestYear, int farmId, string? cropGroupName);
    Task<(List<int>, Error?)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameServiceAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
    
    Task<(bool, Error?)> AddOrganicManuresServiceAsync(string organicManureData);

    Task<(RainTypeResponse, Error)> FetchRainTypeDefaultServiceAsync();
    Task<int> FetchRainfallByPostcodeAndDateRangeServiceAsync(string jsonString);

    Task<(WindspeedResponse?, Error?)> FetchWindspeedDataDefaultServiceAsync();
    Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDateServiceAsync(string applicationDate);
    Task<(List<RainTypeResponse>, Error)> FetchRainTypeListServiceAsync();
    Task<(List<WindspeedResponse>, Error?)> FetchWindspeedListServiceAsync();
    Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeListServiceAsync();
    Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDateServiceAsync(int managementId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedOnCropIdAndAppDateServiceAsync(int cropId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYearServiceAsync(int fieldId, int year,bool confirm);
    Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeIdServiceAsync(int cropTypeId);
    Task<(List<int>, Error?)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureServiceAsync(int fieldId, int year,bool confirm);
    Task<(List<int>, Error)> FetchManureTypsIdsByManIdFromOrgManureServiceAsync(int managementId);
    Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliserServiceAsync(int managementId, bool confirm, int? fertiliserId, int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedOnCropIdFromOrgManureAndFertiliserServiceAsync(int cropId, bool confirm, int? fertiliserId, int? organicManureId);
    Task<(bool, Error)> FetchOrganicManureExistanceByDateRangeServiceAsync(int managementId, string dateFrom, string dateTo, bool isConfirm, int? organicManureId, bool isSlurryOnly);
    Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptakeServiceAsync(string jsonString);
    Task<(RainTypeResponse, Error)> FetchRainTypeByIdServiceAsync(int rainTypeId);
    Task<(WindspeedResponse?, Error?)> FetchWindspeedByIdServiceAsync(int windspeedId);
    Task<(MoistureTypeResponse, Error)> FetchMoisterTypeByIdServiceAsync(int moisterTypeId);

    Task<(List<FarmManureTypeResponse>, Error)> FetchFarmManureTypeByFarmIdServiceAsync(int farmId);
    Task<(MannerCalculateNutrientResponse, Error)> FetchMannerCalculateNutrientServiceAsync(string jsonData);
    Task<(SoilTypeSoilTextureResponse, Error)> FetchSoilTypeSoilTextureBySoilTypeIdServiceAsync(int soilTypeId);
    Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateAndIsGreenCompostServiceAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm,bool isGreenFoodCompost,int? organicManureId);
    Task<(decimal, Error)> FetchTotalNBasedByFieldIdAppDateServiceAsync(int fieldId, DateTime startDate, DateTime endDate, bool confirm, int? organicManureId);
    Task<(OrganicManureDataViewModel, Error)> FetchOrganicManureByIdServiceAsync(int id);
    Task<(List<OrganicManure>, Error)> FetchOrganicManureByFarmIdAndYearServiceAsync(int farmId, int year);
    Task<(string, Error)> DeleteOrganicManureByIdServiceAsync(string orgManureIds);
    Task<(bool, Error)> FetchFarmManureTypeCheckByFarmIdAndManureTypeIdServiceAsync(int farmId, int ManureTypeId, string ManureTypeName);
    Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndManureTypeServiceAsync(int fertiliserId, int farmId, int harvestYear);
    Task<(List<OrganicManure>, Error)> UpdateOrganicManureServiceAsync(string organicManureData);
    Task<(decimal?, Error?)> FetchAvailableNByManagementPeriodIDServiceAsync(int managementPeriodID);
    Task<(FarmManureTypeResponse, Error?)> FetchFarmManureTypeByIdServiceAsync(int id);
    Task<(string?, Error?)> FetchOrganicManureClosedPeriodServiceAsync(OrganicClosedPeriodRequest organicClosedPeriodRequest);
    Task<(bool, Error)> FetchLivestockManureExistanceByDateRangeServiceAsync(int cropId, string dateFrom, string dateTo, int? organicManureId);
    Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRangeServiceAsync(int cropId, string dateFrom, string dateTo, int? organicManureId, bool isPoultry);
    Task<(bool, Error)> CheckGreenCompostExistanceByDateRangeServiceAsync(int fieldId, string dateFrom, string dateTo, int? organicManureId);
    Task<(int?, Error?)> FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroupServiceAsync(int cropTypeId, int soilTypeId, int residueGroup);
}
