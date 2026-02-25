using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using Polly;
using System.Net;
using System.Net.Http;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController(ILogger<AccountController> logger, IOptionsMonitor<OpenIdConnectOptions> openIdConnectOptions) : Controller
    {
        private readonly ILogger _logger = logger;
        private readonly IOptionsMonitor<OpenIdConnectOptions> _openIdConnectOptions = openIdConnectOptions;
        public async Task<IActionResult> Logout()
        {
            _logger.LogTrace("Account Controller : Logout action called");            
            base.SignOut();
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("NMP-Portal");            
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = GetConfigurationManager();
            string url = Url.Action("index", "home", new { area = "" }) ?? "/";
            if (configurationManager != null)
            {
                var metadata = await configurationManager.GetConfigurationAsync(CancellationToken.None);
                url = metadata.EndSessionEndpoint;
            }            
            return Redirect(url);
        }
        public IActionResult ChangeOrganisation()
        {            
            return RedirectToAction("SignIn", "Account", new { Area = "MicrosoftIdentity", redirectUri = "/Farm/FarmList" });
        }

        private IConfigurationManager<OpenIdConnectConfiguration>? GetConfigurationManager()
        {
            // Use the SAME scheme name you registered
            var options = _openIdConnectOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);

            return options.ConfigurationManager;
        }

    }
}
