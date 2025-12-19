using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class AcceptTermsLogic(ILogger<AcceptTermsLogic> logger, IUserExtensionService userExtensionService) : IAcceptTermsLogic
{
    private readonly ILogger<AcceptTermsLogic> _logger = logger;
    private readonly IUserExtensionService _userExtensionService = userExtensionService;
    
    public async Task<bool> IsUserTermsOfUseAccepted()
    {
        UserExtension? userExtension = await _userExtensionService.FetchUserExtensionAsync();
        if (userExtension != null)
        {
            return userExtension.IsTermsOfUseAccepted;
        }
        return false;
    }

    public async Task<UserExtension?> UpdateTermsOfUseAsync(TermsOfUseViewModel model)
    {
        _logger.LogTrace("AcceptTermsLogic : UpdateTermsOfUseAsync() called");
        UserExtension? userExtension = await _userExtensionService.UpdateTermsOfUseAsync(model);
        return userExtension;
    }        
}
