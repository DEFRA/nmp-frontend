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
    public class AccountController : Controller
    {
        private readonly ILogger _logger;
        public AccountController(ILogger<AccountController> logger) {

            _logger = logger;
        }

        //public IActionResult afterlogin(string returnUrl = "")
        //{
        //    return Redirect(returnUrl ?? "/");
        //}

        public async Task<IActionResult>logout()
        {
            _logger.LogTrace("Account Controller : Logout action called");
            base.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);            
            HttpContext?.Session.Clear();
            return RedirectToAction("SignOut","Account", new { Area = "MicrosoftIdentity" });            
        }
    }
}
