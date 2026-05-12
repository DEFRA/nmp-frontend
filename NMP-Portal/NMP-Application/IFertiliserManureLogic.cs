using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Application;

public interface IFertiliserManureLogic
{
    Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder);
    Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear);
    Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName);
    Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations();
    Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id);

    Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure);
    Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate, int? fertiliserId, bool confirm);
    Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds);
    Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId);
    Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId, int farmId, int harvestYear);
    Task<(List<FertiliserManure>, Error?)> UpdateFertiliser(string fertliserData);
    Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID);
    Task<(string?, Error?)> FetchFertiliserManureClosedPeriod(int countryId,int cropTypeId, int? nvzProgramId);
    Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumn(int managementPeriodID, bool isAutumn);
    Task<(Error? error, decimal nitrogenInFourWeek)> BindNitrogenInFourWeekForWarning(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, DateTime fourWeekDate, decimal nitrogenInFourWeek);
    (string, string, string) BindStartEndDateAndWarningPeriod(FertiliserManureViewModel model, DateTime endDate, string closedPeriod);
    (string, string) BindStartPeriodAndEndPeriod(string closedPeriod);
    Task<(Error? error, decimal PreviousApplicationsNitrogen)> BindPreviousYearNitrogen(FertiliserManureViewModel model, int managementId, DateTime startDate, int fieldId, Error? error, DateTime endOfOctober, decimal PreviousApplicationsNitrogen);
    
    FertiliserManureViewModel SetClosedPeriodWarning(FertiliserManureViewModel model, WarningResponse warningResponse, string para2 = null);
    Task<(Error? error, decimal nitrogenWithinWarningPeriod)> BindNitrogenWithInWarningPeriod(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, DateTime start, DateTime end, decimal nitrogenWithinWarningPeriod);
    Task<(Error? error, decimal previousApplicationsN)> BindPreviousApplicationN(FertiliserManureViewModel model, int managementId, Error? error, int cropId, decimal previousApplicationsN);
    Task<(Error? error, CropTypeLinkingResponse cropTypeLinking, int? scotlandNmax, int residueGroup, bool isWinterOilseedRapeAutumn)> BindDataForNmaxWarning(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, int farmCountryId, int scotland, Crop? crop);
    Task<(FertiliserManureViewModel, WarningResponse)> BindNmaxWarningInModelForAsparagusAndOnionCrops(FertiliserManureViewModel model, int cropTypeId, decimal totalNitrogen, bool isWithinClosedPeriod, string startPeriod, string endPeriod);
    Task<(FertiliserManureViewModel, WarningResponse)> BindOilseedRapeWarnings(FertiliserManureViewModel model, int managementId, decimal totalNitrogen, string startPeriod, decimal PreviousApplicationsNitrogen, bool isWithinWarningPeriod, int cropTypeId);
    Task<(bool flowControl, (FertiliserManureViewModel, Error?) value)> BindNmaxWarnings(FertiliserManureViewModel model, Error? error, decimal totalNitrogenApplied, int farmCountryId, Crop crop, int? scotlandNmax, int? nmaxLimitEnglandOrWales, decimal nMaxLimit);
}
