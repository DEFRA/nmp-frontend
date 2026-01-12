using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FarmContextLogic(ILogger<FarmContextLogic> logger, IFarmContextService farmContextService) : IFarmContextLogic
{
    private readonly ILogger<FarmContextLogic> _logger = logger;
    private readonly IFarmContextService _farmContextService = farmContextService;
    public async Task<Farm?> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("FarmContextLogic : FetchFarmByIdAsync() called");        
        return await _farmContextService.FetchFarmByIdAsync(farmId);
    }
}
