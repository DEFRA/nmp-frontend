using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IMannerEstimationService
    {
        Task<(List<MannerEstimation>, Error?)> FetchMannerEstimationsList(Guid orgId);
        Task<bool> FetchIsExistMannerEstimationsByOrgIdAndNameAsyncAPI(Guid organisationId, string name);
        Task<(bool, Error?)> AddMannerEstimationServiceAsync(string MannerData);

        Task<bool> FetchIsPerennialByCropTypeIdServiceAsync(int cropTypeId);
        Task<(int?, Error?)> FetchSoilTypeSoilTextureByTopSoilSubSoilId(int topSoilId, int subSoilId);

        Task<(List<MannerEstimationApplication>, Error?)> FetchMannerApplicationsByMannerEstimationId(int mannerEstimationId);
        Task<(MannerEstimationApplication, Error?)> FetchMannerApplicationById(int mannerApplicationId);

    }
}
