using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IRecommendationLogic
    {
        Task<(Recommendation?, Error?)> FetchRecommendationByManagementPeriodId(int managementPeriodID);
    }
}
