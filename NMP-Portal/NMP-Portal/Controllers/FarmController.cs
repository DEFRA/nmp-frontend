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

        [HttpGet]
        public IActionResult Name()
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;            
            return View();
        }

    }
}
