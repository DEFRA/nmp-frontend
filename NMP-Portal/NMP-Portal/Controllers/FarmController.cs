using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;

namespace NMP.Portal.Controllers
{
    public class FarmController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ManualAddress()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ManualAddress(FarmAddress farm)
        {
            return View();
        }
    }
}
