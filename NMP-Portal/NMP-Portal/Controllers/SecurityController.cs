using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    public class SecurityController : Controller
    {
        

        public IActionResult AfterLogin(string code="")
        {
            return Redirect("/");
        }

        public async Task<IActionResult> LogOut()
        {
            base.SignOut();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext?.Session.Remove("JwtToken");
            HttpContext?.Session.Clear();
            return RedirectToAction("SignOut", "Account");
        }
    }
}
