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
        Task<List<HarvestYearPlanResponse>> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId);

        Task<(bool, Error)> AddCropNutrientManagementPlan(CropDataWrapper cropData);
    }
}
