using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Commons.ViewModels;

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
        public IActionResult MannerHubPage()
        {
            return View();
        }
    }
}
