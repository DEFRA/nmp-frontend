using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Core.Interfaces;
namespace NMP.Businesses;
public class HomeLogic : IHomeLogic
{
    private readonly ILogger _logger;
    private readonly IHomeService _homeService;
    public HomeLogic(ILogger<HomeLogic> logger, IHomeService homeService) 
    {
        _logger = logger;
        _homeService = homeService;
    }
    public async Task<bool> IsDefraCustomerIdentifyConfigurationWorkingAsync()
    {
        _logger.LogTrace("HomeLogic : IsDefraCustomerIdentifyConfigurationWorkingAsync() called");
        return await _homeService.IsDefraCustomerIdentifyConfigurationWorkingAsync();
    }

    public async Task<bool> IsNmptServiceWorkingAsync()
    {
        _logger.LogTrace("HomeLogic : IsNmptServiceWorkingAsync() called");
        return await _homeService.IsNmptServiceWorkingAsync();
    }
}
