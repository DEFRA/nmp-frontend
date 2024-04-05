using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FarmController : Controller
    {
        private readonly ILogger<FarmController> _logger;
        private readonly IDataProtector _dataProtector;
        public FarmController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        }
        public IActionResult Index()
        {
            return View();
        }

        
        public IActionResult FarmList()
        {
            FarmsViewModel model = new FarmsViewModel();

            if (model.Farms.Count > 0)
            {
                ViewBag.IsUserHaveAnyFarms = true;
                
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = false;
                return RedirectToAction("Name", "Farm");
            }
            //if (model.Farms.Count > 0)
            //{
            //    ViewBag.IsUserHaveAnyFarms = true;
            //}
            //else
            //{
            //    ViewBag.IsUserHaveAnyFarms = false;


            //}
            return View(model);

        }

        [HttpGet]
        public IActionResult Name()
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
            model.Name = farmName ?? string.Empty;
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
