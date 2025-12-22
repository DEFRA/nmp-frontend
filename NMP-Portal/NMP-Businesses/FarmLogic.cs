using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FarmLogic(ILogger<FarmLogic> logger, IFarmService farmService) : IFarmLogic
{
    private readonly ILogger<FarmLogic> _logger = logger;
    private readonly IFarmService _farmService = farmService;
    public async Task<Farm?> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("FarmLogic : FetchFarmByIdAsync() called");
        (var farm, _) = await _farmService.FetchFarmByIdAsync(farmId);
        return farm;
    }
}
