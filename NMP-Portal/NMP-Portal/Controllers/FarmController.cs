using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FarmController : Controller
    {
        private readonly ILogger<FarmController> _logger;
        private readonly IDataProtector _dataProtector;
        private readonly IAddressLookupService _addressLookupService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        public FarmController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IAddressLookupService addressLookupService,
            IUserFarmService userFarmService, IFarmService farmService,
            IFieldService fieldService)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _httpContextAccessor = httpContextAccessor;
            _addressLookupService = addressLookupService;
            _userFarmService = userFarmService;
            _farmService = farmService;
            _fieldService = fieldService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> FarmList()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("FarmData");
            _httpContextAccessor.HttpContext?.Session.Remove("AddressList");
            FarmsViewModel model = new FarmsViewModel();
            Error error = null;
            UserFarmResponse userFarmList = null;
            (userFarmList, error) = await _userFarmService.UserFarmAsync(1);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                ViewBag.Error = error.Message;
                return View("~/Views/Home/Index.cshtml");
            }
            if (userFarmList != null && userFarmList.Farms.Count > 0)
            {
                model.Farms.AddRange(userFarmList.Farms);
                model.Farms.ForEach(m => m.EncryptedFarmId = _dataProtector.Protect(m.ID.ToString()));
            }
            if (model.Farms.Count == 0)
            {
                return RedirectToAction("Name", "Farm");
            }

            return View(model);
        }
        public IActionResult CreateFarmCancel()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("FarmData");
            _httpContextAccessor.HttpContext?.Session.Remove("AddressList");
            return RedirectToAction("FarmList");
        }

        [HttpGet]
        public IActionResult Name()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains<string>("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Name(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
            }
            if (string.IsNullOrWhiteSpace(farm.Postcode))
            {
                ModelState.AddModelError("Postcode", Resource.MsgEnterTheFarmPostcode);
            }
            if (!ModelState.IsValid)
            {
                return View(farm);
            }
            bool IsFarmExist = await _farmService.IsFarmExistAsync(farm.Name, farm.Postcode);
            if (IsFarmExist)
            {
                ModelState.AddModelError("Name", string.Format(Resource.MsgFarmAlreadyExist, farm.Name, farm.Postcode));
            }
            if (farm.IsCheckAnswer)
            {
                FarmViewModel dataObject = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext.Session.GetString("FarmData"));

                var updatedFarm = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", updatedFarm);

                if (dataObject.Postcode == farm.Postcode)
                {
                    farm.IsPostCodeChanged = false;
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    farm.IsPostCodeChanged= true;
                    //return RedirectToAction("Address");
                }
            }
            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);

            return RedirectToAction("Address");
        }
        [HttpGet]
        public async Task<IActionResult> Address()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }

            _httpContextAccessor.HttpContext?.Session.Remove("AddressList");

            List<AddressLookupResponse> addresses = await _addressLookupService.AddressesAsync(model.Postcode, 0);
            var addressesList = addresses.Select(a => new SelectListItem { Value = a.AddressLine, Text = a.AddressLine }).ToList();

            if (addressesList.Count > 0 && addressesList.Any())
            {
                ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addresses.Count.ToString());
            }
            else
            {
                return RedirectToAction("AddressNotFound");
            }

            if (addressesList != null && addressesList.Any())
            {
                ViewBag.AddressList = addressesList;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("AddressList", addresses);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Address(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                ModelState.AddModelError("FullAddress", Resource.MsgSelectAddress);
            }

            List<AddressLookupResponse> addresses = new List<AddressLookupResponse>();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("AddressList"))
            {
                addresses = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<AddressLookupResponse>>("AddressList");

            }

            if (!ModelState.IsValid)
            {
                if (addresses != null && addresses.Count > 0)
                {
                    var addressList = addresses.Select(a => new SelectListItem { Value = a.AddressLine, Text = a.AddressLine }).ToList();
                    ViewBag.AddressList = addressList;
                    ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addressList.Count.ToString());
                }
                return View(farm);
            }

            AddressLookupResponse? address = addresses.FirstOrDefault(a => a.AddressLine == farm.FullAddress);
            if (address != null)
            {
                farm.Address1 = string.Format("{0}{1}{2}{3}", address.SubBuildingName != null ? address.SubBuildingName + ", " : string.Empty, address.BuildingNumber != null ? address.BuildingNumber + ", " : string.Empty, address.BuildingName != null ? address.BuildingName + ", " : string.Empty, address.Street);
                farm.Address2 = address.Locality;
                farm.Address3 = address.Town;
                farm.Address4 = address.HistoricCounty;
            }


            farm.IsManualAddress = false;
            //farm.Rainfall = farm.Rainfall ?? 600;

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (!farm.IsPostCodeChanged && farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("Rainfall");
        }


        public IActionResult AddressNotFound()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ManualAddress()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }



            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManualAddress(FarmViewModel farm)
        {
            if (string.IsNullOrEmpty(farm.Address1))
            {
                ModelState.AddModelError("Address1", Resource.MsgEnterAddressLine1TypicallyTheBuildingAndSreet);
            }
            if (string.IsNullOrEmpty(farm.Address3))
            {
                ModelState.AddModelError("Address3", Resource.MsgEnterATownOrCity);
            }
            if (string.IsNullOrEmpty(farm.Address4))
            {
                ModelState.AddModelError("Address4", Resource.MsgEnterACounty);
            }
            if (string.IsNullOrEmpty(farm.Postcode))
            {
                ModelState.AddModelError("Postcode", Resource.MsgEnterAPostcode);
            }
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }

            farm.FullAddress = string.Empty;
            farm.IsManualAddress = true;

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);

            return RedirectToAction("Rainfall");
        }
        [HttpGet]
        public IActionResult Rainfall()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            if (model == null)
            {
                model = new FarmViewModel();
            }
            model.Rainfall = model.Rainfall ?? 600;

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", model);

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rainfall(FarmViewModel farm)
        {
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }
        [HttpGet]
        public IActionResult RainfallManual()
        {
            FarmViewModel? model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RainfallManual(FarmViewModel farm)
        {

            //we need to call api for rainfall on the basis of postcode
            if (farm.Rainfall == null)
            {
                ModelState.AddModelError("Rainfall", Resource.MsgEnterTheAverageAnnualRainfall);
            }
            if (!ModelState.IsValid)
            {
                return View("RainfallManual", farm);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }
        [HttpGet]
        public IActionResult NVZ()
        {
            FarmViewModel? model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZ(FarmViewModel farm)
        {
            if (farm.NVZFields == null)
            {
                ModelState.AddModelError("NVZField", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("NVZ", farm);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Elevation");

        }
        [HttpGet]
        public IActionResult Elevation()
        {
            FarmViewModel? model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Elevation(FarmViewModel farm)
        {
            if (farm.FieldsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("FieldsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("Elevation", farm);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Organic");


        }
        [HttpGet]
        public IActionResult Organic()
        {
            FarmViewModel? model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            //model.IsCheckAnswer = false;
            //string updatedSessionData = JsonConvert.SerializeObject(model);
            //_httpContextAccessor.HttpContext.Session.SetString("FarmData", updatedSessionData);
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Organic(FarmViewModel farm)
        {
            if (farm.RegisteredOrganicProducer == null)
            {
                ModelState.AddModelError("RegisteredOrganicProducer", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("Organic", farm);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            return RedirectToAction("CheckAnswer");
        }
        [HttpGet]
        public IActionResult CheckAnswer()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            if (model == null)
            {
                model = new FarmViewModel();
            }

            if (string.IsNullOrWhiteSpace(model.FullAddress))
            {
                model.FullAddress = string.Format("{0}, {1} {2}, {3}, {4}", model.Address1, model.Address2 != null ? model.Address2 + "," : string.Empty, model.Address3, model.Address4, model.Postcode);
            }

            model.IsCheckAnswer = true;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", model);
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FarmViewModel farm)
        {
            var farmData = new FarmData
            {
                Farm = new Farm()
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    Postcode = farm.Postcode,
                    CPH = farm.CPH,
                    FarmerName = farm.FarmerName,
                    BusinessName = farm.BusinessName,
                    SBI = farm.SBI,
                    STD = farm.STD,
                    Telephone = farm.Telephone,
                    Mobile = farm.Mobile,
                    Email = farm.Email,
                    Rainfall = farm.Rainfall,
                    TotalFarmArea = farm.TotalFarmArea,
                    AverageAltitude = farm.AverageAltitude,
                    RegisteredOrganicProducer = farm.RegisteredOrganicProducer,
                    MetricUnits = farm.MetricUnits,
                    EnglishRules = farm.EnglishRules,
                    NVZFields = farm.NVZFields,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                    CreatedByID=1,
                    CreatedOn=System.DateTime.Now,
                    ModifiedByID=farm.ModifiedByID,
                    ModifiedOn=farm.ModifiedOn
                },
                UserID = 1,
                RoleID = 2
            };
            (Farm farmResponse, Error error) = await _farmService.AddFarmAsync(farmData);

            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.AddFarmError = error.Message;
                return View(farm);
            }
            farmResponse.EncryptedFarmId = _dataProtector.Protect(farmResponse.ID.ToString());
            return RedirectToAction("FarmSummary", new { EncryptedFarmId = farmResponse.EncryptedFarmId });

        }
        public IActionResult BackCheckAnswer()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", model);
            return RedirectToAction("Organic");
        }

        [HttpGet]
        public async Task<IActionResult> FarmSummary(string EncryptedFarmId)
        {
            string farmId = string.Empty;

            ViewBag.FieldCount = 0;
            ViewBag.Success = false;
            FarmViewModel? farmData = null;
            Error error = null;
            FarmViewModel model = new FarmViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
                }
                if (model.IsCheckAnswer)
                {
                    ViewBag.Success = true;
                }

            if (!string.IsNullOrWhiteSpace(EncryptedFarmId))
            {
                farmId = _dataProtector.Unprotect(EncryptedFarmId);

                 (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
 
                if (farm != null)
                {
                    farmData = new FarmViewModel();
                    farmData.Name=farm.Name;
                    farmData.FullAddress = string.Format("{0}, {1} {2}, {3} {4}", farm.Address1, farm.Address2 != null ? farm.Address2 + "," : string.Empty, farm.Address3, farm.Address4, farm.Postcode);
                    farmData.EncryptedFarmId = _dataProtector.Protect(farm.ID.ToString());
                    ViewBag.FieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmId));
                }
            }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }
            return View(farmData);

        }

    }
}
