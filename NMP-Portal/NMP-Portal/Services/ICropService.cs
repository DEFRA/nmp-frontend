using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ICropService
    {
        Task<List<PotatoVarietyResponse>> FetchPotatoVarieties();
        Task<CropTypeResponse> FetchCropTypeByGroupId(int cropGroupId);
    }
}
