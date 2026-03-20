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

namespace NMP.Businesses;
[Business(ServiceLifetime.Transient)]
public class FarmsNvzLogic(ILogger<FarmsNvzLogic> logger, IFarmsNvzService farmsNvzService) : IFarmsNvzLogic
{
    private readonly ILogger<FarmsNvzLogic> _logger = logger;
    private readonly IFarmsNvzService _farmsNvzService = farmsNvzService;
    public async Task<(List<FarmsNvz>, Error?)> FetchFarmNVZByID(int farmId)
    {
        _logger.LogTrace("Fetch farmNVZ By FarmId: {FarmId}", farmId);
        return await _farmsNvzService.FetchFarmNVZByID(farmId);

    }
}
