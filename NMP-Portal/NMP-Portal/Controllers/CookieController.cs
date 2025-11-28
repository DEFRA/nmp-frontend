using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class CookieController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
