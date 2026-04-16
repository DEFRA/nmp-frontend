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
    public class FarmAverageYieldLogic(ILogger<FarmAverageYieldLogic> logger,IFarmAverageYieldService farmAverageYieldService) : IFarmAverageYieldLogic
    {
        private readonly ILogger<FarmAverageYieldLogic> _logger = logger;
        private readonly IFarmAverageYieldService _farmAverageYieldService = farmAverageYieldService;

        public async Task<(List<FarmAverageYields>?, Error?)> FetchFarmAverageYieldByFarmIdAndHarvestYear(int farmId, int harvestYear)
        {
            _logger.LogTrace("Fetch farm average yield by FarmId: {FarmId},and harvestYear: {Year}", farmId, harvestYear);
            return await _farmAverageYieldService.FetchFarmAverageYieldByFarmIdAndHarvestYear(farmId, harvestYear);
        }
    }
}
