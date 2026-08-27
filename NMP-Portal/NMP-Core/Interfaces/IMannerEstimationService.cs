using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IMannerEstimationService
    {
        Task<(List<MannerEstimationDetailsViewModel>, Error?)> FetchMannerEstimationsList(Guid orgId);
        Task<bool> FetchIsExistMannerEstimationsByMannerFarmIdAndNameAsyncAPI(int mannerFarmId, string name);
        Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationServiceAsync(string MannerData);
        Task<(MannerFarmEstimationApplicationResponse?, Error?)> AddMannerFarmEstimationServiceAsync(string MannerData);

        Task<(int?, Error?)> FetchSoilTypeSoilTextureByTopSoilSubSoilId(int topSoilId, int subSoilId);

        Task<(List<MannerEstimationApplication>, Error?)> FetchMannerApplicationsByMannerEstimationId(int mannerEstimationId);
        Task<(MannerEstimationApplication, Error?)> FetchMannerApplicationById(int mannerApplicationId);
        Task<(MannerEstimationResultResponse?, Error?)> FetchMannerApplicationResultById(int mannerEstimationId);
        Task<(int, Error?)> CopyMannerEstimation(int id, string estimationName);
        Task<(List<NutrientProductResponse>, Error?)> FetchNutrientProductByNutrientId(int nurteintId);
        Task<(MannerEstimation?, Error?)> FetchMannerEstimateById(int mannerEstimateId);
        Task<(MannerEstimation?, Error?)> UpdateMannerEstimationServiceAsync(string MannerData);
        Task<(decimal, Error)> FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(int mannerEstimationId, DateTime startDate, DateTime endDate, bool isGreenFoodCompost, int? mannerApplicationId);

        Task<(decimal, Error)> FetchTotalNByMannerEstimationIdAppDate(int mannerEstimationId, DateTime startDate, DateTime endDate, int? mannerApplicationId);

        Task<(bool, Error)> CheckMannerGreenCompostExistanceByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId);
        Task<(MannerEstimationApplication?, Error?)> FetchMannerEstimateApplicationByIdAsync(int mannerEstimateApplicationId);
        Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimationApplicationServiceAsync(string MannerApplicationData);
        Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationApplicationServiceAsync(string applicationData);
        Task<Error?> RemoveMannerEstimationsServiceAsync(string mannerEstimationIds);
        Task<(string, Error?)> DeleteMannerEstimateApplicationByIdServiceAsync(int mannerEstimationId);
         Task<(List<MannerFarmViewModel>, Error?)> FetchMannerFarmListByOrgId(Guid orgId);
        Task<(MannerFarmViewModel?, Error?)> FetchMannerFarmById(int mannerFarmId);
        Task<(List<MannerEstimationSummaryViewModel>?, Error?)> FetchMannerEstimateByFarmIdAsync(int mannerFarmId);
        Task<Error?> RemoveMannerFarmsServiceAsync(string mannerFarmIds);
        Task<bool> FetchIsExistMannerFarmByOrgIdAndNameAsyncAPI(Guid organisationId, string name);
        Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRangeServiceAsync(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId, bool isPoultry);

    }
}
