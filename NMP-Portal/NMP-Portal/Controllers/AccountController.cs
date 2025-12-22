using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;
using NMP.Portal.Helpers;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController(ILogger<AccountController> logger) : Controller
    {
        private readonly ILogger _logger = logger;

        public async Task<IActionResult> Logout()
        {
            _logger.LogTrace("Account Controller : Logout action called");
            base.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext?.Session.Clear();
            return RedirectToAction("SignOut", "Account", new { Area = "MicrosoftIdentity" });
        }

        public IActionResult ChangeOrganisation()
        {
            return RedirectToAction("SignIn", "Account", new { Area = "MicrosoftIdentity", redirectUri = "/Farm/FarmList" });
        }
    }
}
