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
        //public IActionResult afterlogin(string returnUrl = "")
        //{
        //    return Redirect(returnUrl ?? "/");
        //}

        public async Task<IActionResult>logout()
        {            
            base.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);            
            HttpContext?.Session.Clear();
            return RedirectToAction("SignOut","Account", new { Area = "MicrosoftIdentity" });            
        }
    }
}
