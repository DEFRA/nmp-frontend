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

        public IActionResult Name()
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;            
            return View();
        }

        public IActionResult Address(Farm farm)
        {
            FarmsViewModel farmsViewModel = new FarmsViewModel();
            //need to take the farms count according to userId
            //farm.count=
            ViewBag.IsUserHaveAnyFarms = farmsViewModel.Farms.Count > 0 ? true : false;
            FarmViewModel model = new FarmViewModel();
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Name.cshtml",model);
            }
            else
            {
                model.Name = farm.Name;
                model.PostCode = farm.PostCode;
                //bind address list
                ViewBag.Address = "";
            }
            
            return View(model);
        }

        public IActionResult Rainfall(Farm farm)
        {
            FarmsViewModel farmsViewModel = new FarmsViewModel();
            //need to take the farms count according to userId
            //farm.count=
            ViewBag.IsUserHaveAnyFarms = farmsViewModel.Farms.Count > 0 ? true : false;
            FarmViewModel model = new FarmViewModel();
            model.Name = farm.Name;
            model.PostCode = farm.PostCode;
            model.Address1 = farm.Address1;
            model.Address2 = farm.Address2;
            model.Address3 = farm.Address3;
            model.Address4 = farm.Address4;
            return View(model);
        }

        public IActionResult ManualAddress(Farm farm)
        {
            FarmViewModel model = new FarmViewModel();
            model.Name = farm.Name;
            model.PostCode = farm.PostCode;
            return View(model);
        }
    }
}
