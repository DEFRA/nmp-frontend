using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ICropService
    {
        Task<List<PotatoVarietyResponse>> FetchPotatoVarieties();
        Task<CropTypeResponse> FetchCropTypeByGroupId(int cropGroupId);
        Task<(Crop, Error)> AddCropNutrientManagementPlan(CropData cropData, int fieldID);
    }
}
