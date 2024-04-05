﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        public async Task<IActionResult> LoginAsync(string returnUrl = "")
        {

            var user = new User();
            user.GivenName = "NMPT";
            user.Surname = "User";
            user.UserName = new Guid().ToString();
            user.Id = 1;
            user.Email = "Mark.Brown@rsk-bsl.com";

            IIdentity identity = new GenericIdentity($"{user.GivenName} {user.Surname}");
            
            var claims = new[] { new Claim(ClaimTypes.Sid, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.GivenName),
                 new Claim(ClaimTypes.Surname, user.Surname),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier,user.UserName),
            };

            var claimsIdentity = new ClaimsIdentity(identity,claims, CookieAuthenticationDefaults.AuthenticationScheme, null, null);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            return Redirect(returnUrl?? "/");
        }

        public async Task<IActionResult> LogOut()
        {
            base.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index","Home");
        }
    }
}