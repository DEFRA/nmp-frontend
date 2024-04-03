using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    public class FarmController : Controller
    {
        public IActionResult Index()
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;            
            return View();
        }
        public IActionResult Address()
        {
            return View();
        }
        public IActionResult ManualAddress(string? farmName)
        {
            FarmViewModel model = new FarmViewModel();
            model.Name = farmName??string.Empty;
            return View(model);
        }
        [HttpPost]
        public IActionResult ManualAddress(FarmViewModel farm)
        {
            FarmViewModel model = new FarmViewModel();
            //need to fetch user farms 
            //ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;            
            return View();
        }

    }
}
