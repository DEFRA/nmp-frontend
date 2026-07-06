using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface ICropService
{
    Task<List<PotatoVarietyResponse>> FetchPotatoVarietiesServiceAsync();
    Task<int> FetchCropTypeByGroupIdServiceAsync(int cropGroupId);
    Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeIdServiceAsync(int cropTypeId);
    Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeIdServiceAsync();
    Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmIdServiceAsync(int farmId, int type);
    Task<(List<HarvestYearPlanResponse>, Error?)> FetchHarvestYearPlansByFarmIdServiceAsync(int harvestYear, int farmId);

    Task<(bool, Error?)> AddCropNutrientManagementPlanServiceAsync(CropDataWrapper cropData);
    Task<(List<RecommendationHeader>, Error?)> FetchRecommendationByFieldIdAndYearServiceAsync(int fieldId, int harvestYear);
    Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1IdServiceAsync(int cropTypeId, int cropInfo1Id);
    Task<string> FetchCropInfo2NameByCropInfo2IdServiceAsync(int cropInfo2Id);

    Task<List<Crop>> FetchCropsByFieldIdServiceAsync(int fieldId);

    Task<decimal> FetchCropTypeDefaultYieldByCropTypeIdServiceAsync(int cropTypeId, bool isScotland);
    Task<List<int>> FetchSecondCropListByFirstCropIdServiceAsync(int firstCropTypeId, int rb209CountryId);
    Task<(HarvestYearResponseHeader?, Error?)> FetchHarvestYearPlansDetailsByFarmIdServiceAsync(int harvestYear, int farmId);
    Task<string?> FetchCropInfoOneQuestionByCropTypeIdServiceAsync(int cropTypeId, int countryId);
    Task<(ManagementPeriod?, Error?)> FetchManagementperiodByIdServiceAsync(int id);
    Task<(Crop?, Error?)> FetchCropByIdServiceAsync(int id);
    Task<(string, Error?)> RemoveCropPlanServiceAsync(List<int> cropIds);
    Task<(bool, Error?)> IsCropsGroupNameExistForUpdateServiceAsync(string cropIds,string cropGroupName,int year, int farmId);
    Task<(List<Crop>, Error)> UpdateCropServiceAsync(string cropData);
    Task<List<GrassSeasonResponse>> FetchGrassSeasonsServiceAsync();
    Task<(List<GrassGrowthClassResponse>, Error?)> FetchGrassGrowthClassServiceAsync(List<int> fieldIds);

    Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCutServiceAsync(int swardTypeId,int swardManagementId, int numberOfCut,bool isNewSward);
    Task<(List<PotentialCutResponse>,Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementIdServiceAsync(int swardTypeId, int swardManagementId);
    Task<(List<SwardManagementResponse>,Error)> FetchSwardManagementsServiceAsync();
    Task<(List<SwardTypeResponse>, Error)> FetchSwardTypesServiceAsync();
    Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdServiceAsync(int sequenceId, int grassGrowthClassId);

    Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropIdServiceAsync(int cropId,bool isShortSummary);
    Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesByIdServiceAsync(int defoliationId);
    Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementIdServiceAsync(int swardManagementId);
    Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeIdServiceAsync(int swardTypeId);

    Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeIdServiceAsync(int swardTypeId);
    Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinkingServiceAsync();

    Task<(bool, Error)> CopyCropNutrientManagementPlanServiceAsync(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser);
    Task<(bool, Error)> MergeCropServiceAsync(string cropData);
    Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYearServiceAsync(int fieldId,int year);

    Task<bool> FetchIsPerennialByCropTypeIdServiceAsync(int cropTypeId);
}
