using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {            
            HttpContext.Session.Remove("current_farm_name");
            HttpContext.Session.Remove("current_farm_id");
            
            
            HttpContext.Session.Remove("current_manner_estimate_farm_name");
            HttpContext.Session.Remove("current_manner_estimate_manner_farm_id");
            return View();
        }
    }
}
