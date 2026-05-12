using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces
{
    public interface IFertiliserManureService
    {
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameServiceAsync(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
        Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYearServiceAsync(int farmId, int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupNameServiceAsync(int harvestYear, int farmId, string? cropGroupName);
        Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurationsServiceAsync();
        Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsByIdServiceAsync(int id);

        Task<(List<FertiliserManure>, Error)> AddFertiliserManureServiceAsync(string fertiliserManure);
        Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDateServiceAsync(int fieldId, DateTime startDate, DateTime endDate,int? fertiliserId, bool confirm);
        Task<(string, Error)> DeleteFertiliserByIdServiceAsync(string fertiliserIds);
        Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdServiceAsync(int fertiliserId);
        Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrientServiceAsync(int fertiliserId,int farmId,int harvestYear);
        Task<(List<FertiliserManure>, Error?)> UpdateFertiliserServiceAsync(string fertliserData);
        Task<(decimal?, Error)> FetchTotalNByManagementPeriodIDServiceAsync(int managementPeriodID);
        Task<(string?, Error?)> FetchFertiliserManureClosedPeriodServiceAsync(int countryId, int cropTypeId, int? nvzProgramId);
        Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumnServiceAsync(int managementPeriodID, bool isAutumn);
    }
}
