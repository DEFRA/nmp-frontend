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
        Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId,int type);
        Task<(List<HarvestYearPlanResponse>, Error)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId);

        Task<(bool, Error)> AddCropNutrientManagementPlan(CropDataWrapper cropData);
        Task<(List<Recommendation>, Error)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear);
        Task<(List<Crop>, Error)> FetchCropByFieldId(int fieldId);
    }
}
