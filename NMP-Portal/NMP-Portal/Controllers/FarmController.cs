using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;
using System.Reflection;

namespace NMP.Portal.Controllers
{
    public class FarmController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult FarmList(FarmsViewModel model)
        {
            if (model.Farms.Count > 0)
            {
                ViewBag.IsUserHaveAnyFarms = true;
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = false;
            }
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
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;
            return View();
        }
        public IActionResult Address(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Name.cshtml", farm);
            }
            else
            {
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode
                };
                return View(model);
            }


        }

        [HttpPost]
        public IActionResult ManualAddress(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Address.cshtml", farm);
            }
            else
            {
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode
                };
                return View(model);
            }

        }
        public IActionResult Rainfall(FarmViewModel farm)
        {
            FarmsViewModel farmsViewModel = new FarmsViewModel();
            FarmViewModel model = new FarmViewModel();
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = farmsViewModel.Farms.Count > 0 ? true : false;

                model.Name = farm.Name;
                model.PostCode = farm.PostCode;
                model.Address1 = farm.Address1;
                model.Address2 = farm.Address2;
                model.Address3 = farm.Address3;
                model.Address4 = farm.Address4;
                return View(model);
            }

        }
        public IActionResult RainfallManual(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Rainfall.cshtml", farm);
            }
            else
            {
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    PostCode = farm.PostCode,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4
                };
                return View(model);
            }

        }
        public IActionResult NVZ(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/RainfallManual.cshtml", farm);
            }
            else
            {
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode,
                    Rainfall= farm.Rainfall,
                    RegistredOrganicProducer=farm.RegistredOrganicProducer,
                    NVZField= farm.NVZField,
                    FieldsAbove300SeaLevel= farm.FieldsAbove300SeaLevel


                };
                return View(model);
            }
            
        }
        public IActionResult Elevation(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/NVZ.cshtml", farm);
            }
            else
            {
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode,
                    Rainfall = farm.Rainfall,
                    RegistredOrganicProducer = farm.RegistredOrganicProducer,
                    NVZField = farm.NVZField,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel


                };
                return View(model);
            }

        }
    }
}
