using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    public class BaseController : Controller
    {
        protected IActionResult HandleExpiredSession()
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Dashboard")
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
