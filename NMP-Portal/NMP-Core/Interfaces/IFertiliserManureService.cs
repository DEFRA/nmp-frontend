using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces
{
    public interface IFertiliserManureService
    {
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
        Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName);
        Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations();
        Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id);

        Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure);
        Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate,int? fertiliserId, bool confirm);
        Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds);
        Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId);
        Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId,int farmId,int harvestYear);
        Task<(List<FertiliserManure>, Error)> UpdateFertiliser(string fertliserData);
        Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID);
    }
}
