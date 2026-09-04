using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class AboutServiceLogic(ILogger<AboutServiceLogic> logger, IUserExtensionService userExtensionService) : IAboutServiceLogic
{
    private readonly ILogger<AboutServiceLogic> _logger = logger;
    private readonly IUserExtensionService _userExtensionService = userExtensionService;

    public async Task<bool> CheckDoNotShowAboutThisService()
    {
        _logger.LogTrace("AboutServiceLogic : HasDoNotShowAboutThisService() called");
        UserExtension? userExtension = await _userExtensionService.FetchUserExtensionAsync();
        return userExtension != null && userExtension.DoNotShowAboutThisService;
    }

    public async Task<bool> UpdateShowAboutAsync(bool doNotShowAboutThisService)
    {
        _logger.LogTrace("AboutServiceLogic : UpdateShowAboutAsync() called");
        AboutService aboutService = new() { DoNotShowAboutThisService = doNotShowAboutThisService };
        UserExtension? userExtension = await _userExtensionService.UpdateShowAboutAsync(aboutService);
        return userExtension != null && userExtension.DoNotShowAboutThisService;
    }
}
