using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFertiliserManureService
    {
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId);
        Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId);
        Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations();
        Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id);

        Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure);
    }
}
