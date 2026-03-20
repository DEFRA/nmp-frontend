using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NMP.Businesses;


[Business(ServiceLifetime.Transient)]
public class AboutMannerLogic(ILogger<AboutMannerLogic> logger, IUserExtensionService userExtensionService) : IAboutMannerLogic
{
    private readonly ILogger<AboutMannerLogic> _logger = logger;
    private readonly IUserExtensionService _userExtensionService = userExtensionService;

    public async Task<bool> CheckDoNotShowAboutManner()
    {
        _logger.LogTrace("AboutMannerLogic : HasDoNotShowAboutManner() called");
        UserExtension? userExtension = await _userExtensionService.FetchUserExtensionAsync();
        return userExtension != null && userExtension.DoNotShowAboutManner;
    }

    public async Task<bool> UpdateShowAboutMannerAsync(bool doNotShowAboutManner)
    {
        _logger.LogTrace("AboutMannerLogic : UpdateShowAboutMannerAsync() called");
        AboutManner aboutManner = new() { DoNotShowAboutManner = doNotShowAboutManner };
        UserExtension? userExtension = await _userExtensionService.UpdateShowAboutMannerAsync(aboutManner);
        return userExtension != null && userExtension.DoNotShowAboutManner;
    }

}
