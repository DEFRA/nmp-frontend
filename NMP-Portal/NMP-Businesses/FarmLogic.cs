using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
namespace NMP.Businesses;

public class FarmLogic(ILogger<FarmLogic> logger) : IFarmLogic
{
    private readonly ILogger<FarmLogic> _logger = logger;
    public Task<Farm?> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("FarmLogic : FetchFarmByIdAsync() called");
        throw new NotImplementedException();
    }
}
