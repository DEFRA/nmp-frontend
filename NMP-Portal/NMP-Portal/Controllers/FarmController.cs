using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;
using System.Reflection;

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

            if (string.IsNullOrEmpty(farm.Address1))
            {
                ModelState.AddModelError("Address1", Resource.MsgEnterAnAddress);
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }
            if (string.IsNullOrEmpty(farm.Address3))
            {
                ModelState.AddModelError("Address3", Resource.MsgEnterATownOrCity);
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }
            if (string.IsNullOrEmpty(farm.Address4))
            {
                ModelState.AddModelError("Address4", Resource.MsgEnterACounty);
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }
            if (string.IsNullOrEmpty(farm.PostCode))
            {
                ModelState.AddModelError("PostCode", Resource.MsgEnterAPostcode);
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }
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
        public IActionResult NVZ(FarmViewModel farm, bool isRainfalManual)
        {
            ViewBag.IsManualRainfall=isRainfalManual;
            //we need to call api for rainfall on the basis of postcode
            if (farm.Rainfall==null)
            {
                //ModelState.AddModelError("Rainfall", Resource.MsgEnterAverageAnnualRainfall);
               // return View("~/Views/Farm/RainfallManual.cshtml", farm);
            }
            if (farm.Rainfall != null)
            {   //check valid rainfall value
                //ModelState.AddModelError("Rainfall", Resource.MsgEnterValidAnnualRainfall);
                //return View("~/Views/Farm/RainfallManual.cshtml", farm);
            }
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
            if (farm.NVZField == null)
            {
                ModelState.AddModelError("NVZField", Resource.MsgSelectAnOptionBeforeContinuing);
                return View("~/Views/Farm/NVZ.cshtml", farm);
            }

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
