using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFertiliserManureService
    {
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId, int? cropOrder);
        Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId);
        Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations();
        Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id);

        Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure);
        Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate,int? fertiliserId, bool confirm);
        Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds);
        Task<(FertiliserManure, Error)> FetchFertiliserByIdAsync(int fertiliserId);
        Task<(List<FertiliserAndManureSameDateAndNutrientValueResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId,int farmId,int harvestYear);
        Task<(List<FertiliserManure>, Error)> UpdateFertiliser(string fertliserData);
    }
}
