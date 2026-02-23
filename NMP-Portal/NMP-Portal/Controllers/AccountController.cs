using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Helpers;
using System.Security.Claims;
using System.Security.Principal;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController(ILogger<AccountController> logger) : Controller
    {
        private readonly ILogger _logger = logger;

        public async Task<IActionResult> Logout()
        {
            _logger.LogTrace("Account Controller : Logout action called");            
            HttpContext.Session.Clear();
            return await Task.FromResult( SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("Index", "Home")
                },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            ));
        }

        public IActionResult ChangeOrganisation()
        {
            return RedirectToAction("SignIn", "Account", new { Area = "MicrosoftIdentity", redirectUri = "/Farm/FarmList" });
        }           
    }
}
