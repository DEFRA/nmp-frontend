using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ICropService
    {
        Task<List<PotatoVarietyResponse>> FetchPotatoVarieties();
        Task<int> FetchCropTypeByGroupId(int cropGroupId);
        Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId);
        Task<CropTypeResponse> FetchCropTypeByGroupId(int cropGroupId);
        Task<(Crop, Error)> AddCropNutrientManagementPlan(CropData cropData, int fieldID);
    }
}
