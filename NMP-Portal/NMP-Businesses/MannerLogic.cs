using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class MannerLogic(ILogger<MannerLogic> logger, IMannerService mannerService) : IMannerLogic
{
    private readonly ILogger<MannerLogic> _logger = logger;
    private readonly IMannerService _mannerService = mannerService;
    public async Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId)
    {
        _logger.LogTrace("Fetching category Id by crop type Id");
        return await _mannerService.FetchCategoryIdByCropTypeIdAsync(cropTypeId); 
    }

    public async Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId)
    {
        _logger.LogTrace("Fetching crop N uptake default");
        return await _mannerService.FetchCropNUptakeDefaultAsync(cropCategoryId);
    }
}
