using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ICropService
    {
        Task<List<PotatoVarietyResponse>> FetchPotatoVarieties();
        Task<int> FetchCropTypeByGroupId(int cropGroupId);
        Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId);
        Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeId();
        Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId, int type);
        Task<(List<HarvestYearPlanResponse>, Error)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId);

        Task<(bool, Error)> AddCropNutrientManagementPlan(CropDataWrapper cropData);
        Task<(List<RecommendationHeader>, Error)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear);
        Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(int cropTypeId, int cropInfo1Id);
        Task<string> FetchCropInfo2NameByCropInfo2Id(int cropInfo2Id);

        Task<List<Crop>> FetchCropsByFieldId(int fieldId);

        Task<decimal> FetchCropTypeDefaultYieldByCropTypeId(int cropTypeId);
        Task<List<int>> FetchSecondCropListByFirstCropId(int firstCropTypeId);
        Task<(HarvestYearResponseHeader, Error)> FetchHarvestYearPlansDetailsByFarmId(int harvestYear, int farmId);
        Task<string?> FetchCropInfoOneQuestionByCropTypeId(int cropTypeId);
        Task<(ManagementPeriod, Error)> FetchManagementperiodById(int id);
        Task<(Crop, Error)> FetchCropById(int id);
        Task<(string, Error)> RemoveCropPlan(string cropIds);
    }
}
