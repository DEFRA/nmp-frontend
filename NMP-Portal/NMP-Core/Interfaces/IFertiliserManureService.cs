using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces
{
    public interface IFertiliserManureService
    {
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
        Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYearAsync(int farmId, int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameAsync(int harvestYear, int farmId, string? cropGroupName);
        Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurationsAsync();
        Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsByIdAsync(int id);

        Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure);
        Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDateAsync(int fieldId, DateTime startDate, DateTime endDate,int? fertiliserId, bool confirm);
        Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds);
        Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId);
        Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrientAsync(int fertiliserId,int farmId,int harvestYear);
        Task<(List<FertiliserManure>, Error?)> UpdateFertiliserAsync(string fertliserData);
        Task<(decimal?, Error)> FetchTotalNByManagementPeriodIDAsync(int managementPeriodID);
        Task<(string?, Error?)> FetchFertiliserManureClosedPeriodAsync(int countryId, int cropTypeId, int? nvzProgramId);
        Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumnAsync(int managementPeriodID, bool isAutumn);
    }
}
