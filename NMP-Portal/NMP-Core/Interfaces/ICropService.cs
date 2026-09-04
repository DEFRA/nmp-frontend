using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface ICropService
{
    
    Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmIdAsync(int farmId, int type);
    Task<(List<HarvestYearPlanResponse>, Error?)> FetchHarvestYearPlansByFarmIdAsync(int harvestYear, int farmId);

    Task<(bool, Error?)> AddCropNutrientManagementPlanAsync(CropDataWrapper cropData);
    Task<(List<RecommendationHeader>, Error?)> FetchRecommendationByFieldIdAndYearAsync(int fieldId, int harvestYear);
    

    Task<List<Crop>> FetchCropsByFieldIdAsync(int fieldId);

    Task<decimal> FetchCropTypeDefaultYieldByCropTypeIdAsync(int cropTypeId, bool isScotland);
    Task<List<int>> FetchSecondCropListByFirstCropIdAsync(int firstCropTypeId, int rb209CountryId);
    Task<(HarvestYearResponseHeader?, Error?)> FetchHarvestYearPlansDetailsByFarmIdAsync(int harvestYear, int farmId);
    Task<string?> FetchCropInfoOneQuestionByCropTypeIdAsync(int cropTypeId, int countryId);
    Task<(ManagementPeriod?, Error?)> FetchManagementperiodByIdAsync(int id);
    Task<(Crop?, Error?)> FetchCropByIdAsync(int id);
    Task<(string, Error?)> RemoveCropPlanAsync(List<int> cropIds);
    Task<(bool, Error?)> IsCropsGroupNameExistForUpdateAsync(string cropIds,string cropGroupName,int year, int farmId);
    Task<(List<Crop>, Error)> UpdateCropAsync(string cropData);
    Task<(List<GrassGrowthClassResponse>, Error?)> FetchGrassGrowthClassAsync(List<int> fieldIds);

    
    Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropIdAsync(int cropId,bool isShortSummary);
    
    Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinkingAsync();

    Task<(bool, Error)> CopyCropNutrientManagementPlanAsync(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser);
    Task<(bool, Error)> MergeCropAsync(string cropData);
    Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYearAsync(int fieldId,int year);

    Task<bool> FetchIsPerennialByCropTypeIdAsync(int cropTypeId);
}
