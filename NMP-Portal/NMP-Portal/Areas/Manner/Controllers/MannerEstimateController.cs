using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Areas.Manner.Controllers
{
    [Area("Manner")]
    [Authorize]    
    public class MannerEstimateController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
