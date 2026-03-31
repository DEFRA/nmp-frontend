using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses
{
    [Business(ServiceLifetime.Transient)]
    public class RecommendationLogic(ILogger<RecommendationLogic> logger, IRecommendationService recommendationService, ISnsAnalysisService snsAnalysisService) : IRecommendationLogic
    {
        private readonly ILogger<RecommendationLogic> _logger = logger;
        private readonly IRecommendationService _recommendationService = recommendationService;
        public async Task<(Recommendation?, Error?)> FetchRecommendationByManagementPeriodId(int managementPeriodID)
        {
            _logger.LogTrace("Fetch Recommendation By ManagementPeriodId :{0}", managementPeriodID);
            return await _recommendationService.FetchRecommendationByManagementPeriodId(managementPeriodID);
        }

    }
}
