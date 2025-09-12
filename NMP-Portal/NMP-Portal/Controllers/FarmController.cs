using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Web;
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
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IReportService _reportService;
        private readonly IStorageCapacityService _storageCapacityService;
        public FarmController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IAddressLookupService addressLookupService,
            IUserFarmService userFarmService, IFarmService farmService,
            IFieldService fieldService, ICropService cropService, IReportService reportService, IStorageCapacityService storageCapacityService)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _addressLookupService = addressLookupService;
            _userFarmService = userFarmService;
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
            _reportService = reportService;
            _storageCapacityService = storageCapacityService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace($"Farm Controller : Index() action called");
            return View();
        }

        public async Task<IActionResult> FarmList(string? q)
        {
            _logger.LogTrace($"Farm Controller : FarmList({q}) action called");
            HttpContext?.Session.Remove("FarmData");
            HttpContext?.Session.Remove("AddressList");
            HttpContext?.Session.Remove("StorageCapacityData");

            FarmsViewModel model = new FarmsViewModel();
            Error error = null;
            try
            {
                Guid organisationId = Guid.Parse(HttpContext.User.FindFirst("organisationId").Value);
                (List<Farm> farms, error) = await _farmService.FetchFarmByOrgIdAsync(organisationId);
                if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                {
                    ViewBag.Error = error.Message;
                    return View("~/Views/Home/Index.cshtml");
                }
                if (farms != null && farms.Count > 0)
                {
                    model.Farms.AddRange(farms);
                    model.Farms.ForEach(m => m.EncryptedFarmId = _dataProtector.Protect(m.ID.ToString()));
                }
                if (model.Farms.Count == 0)
                {
                    return RedirectToAction("Name", "Farm");
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    ViewBag.Success = "true";
                    ViewBag.FarmName = _dataProtector.Unprotect(q);
                }
                else
                {
                    ViewBag.Success = "false";
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in FarmList() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }
        public IActionResult CreateFarmCancel()
        {
            _logger.LogTrace($"Farm Controller : CreateFarmCancel() action called");
            HttpContext?.Session.Remove("FarmData");
            HttpContext?.Session.Remove("AddressList");
            return RedirectToAction("FarmList");
        }

        [HttpGet]
        public IActionResult Name()
        {
            _logger.LogTrace($"Farm Controller : Name() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains<string>("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Name(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : Name() post action called");
            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
            }
            if (!ModelState.IsValid)
            {
                return View(farm);
            }
            FarmViewModel farmView = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                farmView = JsonConvert.DeserializeObject<FarmViewModel>(HttpContext.Session.GetString("FarmData"));
            }

            HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Country");
        }

        [HttpGet]
        public async Task<IActionResult> Country()
        {
            _logger.LogTrace($"Farm Controller : Country() action called");
            FarmViewModel? model = null;
            (List<Country> countryList, Error error) = await _farmService.FetchCountryAsync();
            if (error != null && countryList.Count > 0)
            {
                ViewBag.CountryList = countryList.OrderBy(c => c.Name);
            }
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Country(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : Country() post action called");
            if (farm.CountryID == null)
            {
                ModelState.AddModelError("CountryID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCountry.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                (List<Country> countryList, Error error) = await _farmService.FetchCountryAsync();
                if (error != null && countryList.Count > 0)
                {
                    ViewBag.CountryList = countryList.OrderBy(c => c.Name);
                }
                return View("Country", farm);
            }
            if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England ||
                farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
            {
                farm.EnglishRules = true;
            }
            else
            {
                farm.EnglishRules = false;
            }
            if (Enum.IsDefined(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID))
            {
                farm.Country = Enum.GetName(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("FarmingRules");

        }

        [HttpGet]
        public IActionResult FarmingRules()
        {
            _logger.LogTrace($"Farm Controller : FarmingRules() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FarmingRules(FarmViewModel farm)
        {
            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("PostCode");

        }
        [HttpGet]
        public IActionResult PostCode()
        {
            _logger.LogTrace($"Farm Controller : PostCode() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostCode(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.Postcode))
            {
                ModelState.AddModelError("Postcode", Resource.MsgEnterTheFarmPostcode);
            }

            if (!string.IsNullOrWhiteSpace(farm.Postcode))
            {
                int id = 0;
                if (farm.EncryptedFarmId != null)
                {
                    id = Convert.ToInt32(_dataProtector.Unprotect(farm.EncryptedFarmId));
                }
                bool IsFarmExist = await _farmService.IsFarmExistAsync(farm.Name, farm.Postcode, id);
                if (IsFarmExist)
                {
                    ModelState.AddModelError("Postcode", Resource.MsgFarmAlreadyExist);
                }
            }
            if (!ModelState.IsValid)
            {
                return View(farm);
            }
            FarmViewModel farmView = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                farmView = JsonConvert.DeserializeObject<FarmViewModel>(HttpContext.Session.GetString("FarmData"));
            }
            if (farm.IsCheckAnswer)
            {
                var updatedFarm = JsonConvert.SerializeObject(farm);
                HttpContext?.Session.SetString("FarmData", updatedFarm);

                if (farmView.Postcode == farm.Postcode)
                {
                    farm.IsPostCodeChanged = false;
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    farm.IsPostCodeChanged = true;
                    farm.Rainfall = null;
                    //return RedirectToAction("Address");
                }
            }
            if (farmView != null)
            {
                if (farmView.Postcode != farm.Postcode)
                {
                    farm.Rainfall = null;
                }
            }
            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            //if (farm.IsCheckAnswer)
            //{
            //    return RedirectToAction("CheckAnswer");
            //}
            return RedirectToAction("Address");

        }
        [HttpGet]
        public async Task<IActionResult> Address()
        {
            _logger.LogTrace($"Farm Controller : Address() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            HttpContext.Session.Remove("AddressList");

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
                HttpContext.Session.SetObjectAsJson("AddressList", addresses);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Address(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : Address() post action called");
            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                ModelState.AddModelError("FullAddress", Resource.MsgSelectAddress);
            }

            List<AddressLookupResponse> addresses = new List<AddressLookupResponse>();
            if (HttpContext.Session.Keys.Contains("AddressList"))
            {
                addresses = HttpContext.Session.GetObjectFromJson<List<AddressLookupResponse>>("AddressList");

            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (!farm.IsPostCodeChanged && farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("ClimatePostCode");
        }


        public IActionResult AddressNotFound()
        {
            _logger.LogTrace($"Farm Controller : AddressNotFound() action called");
            FarmViewModel? model = null;
            if (HttpContext != null && HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ManualAddress()
        {
            _logger.LogTrace($"Farm Controller : ManualAddress() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }



            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualAddress(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : ManualAddress() post action called");
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
            if (!string.IsNullOrWhiteSpace(farm.Address1) && farm.Address1.Length > 50)
            {
                ModelState.AddModelError("Address1", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine1,50));
            }
            if (!string.IsNullOrWhiteSpace(farm.Address2) && farm.Address2.Length > 50)
            {
                ModelState.AddModelError("Address2", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine2ForErrorMsg, 50));
            }
            if (!string.IsNullOrWhiteSpace(farm.Address3) && farm.Address3.Length > 50)
            {
                ModelState.AddModelError("Address3", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblTownOrCity, 50));
            }
            if (!string.IsNullOrWhiteSpace(farm.Address4) && farm.Address4.Length > 50)
            {
                ModelState.AddModelError("Address4", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblCountry,50));
            }
            if (!string.IsNullOrWhiteSpace(farm.Postcode))
            {
                int id = 0;
                if (farm.EncryptedFarmId != null)
                {
                    id = Convert.ToInt32(_dataProtector.Unprotect(farm.EncryptedFarmId));
                }
                bool IsFarmExist = await _farmService.IsFarmExistAsync(farm.Name, farm.Postcode, id);
                if (IsFarmExist)
                {
                    ModelState.AddModelError("Postcode", Resource.MsgFarmAlreadyExist);
                }
            }
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/ManualAddress.cshtml", farm);
            }

            FarmViewModel farmView = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                farmView = JsonConvert.DeserializeObject<FarmViewModel>(HttpContext.Session.GetString("FarmData"));
            }
            if (farmView != null)
            {
                if (farmView.Postcode != farm.Postcode)
                {
                    farm.Rainfall = null;
                }
            }
            farm.FullAddress = string.Empty;
            farm.IsManualAddress = true;

            HttpContext.Session.SetObjectAsJson("FarmData", farm);

            return RedirectToAction("ClimatePostCode");
        }
        [HttpGet]
        public async Task<IActionResult> ClimatePostCode()
        {
            _logger.LogTrace($"Farm Controller : ClimatePostCode() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model.Rainfall == 0 || model.Rainfall == null)
            {
                string firstHalfPostcode = string.Empty;
                if (!model.Postcode.Contains(" "))
                {
                    firstHalfPostcode = model.Postcode.Substring(0, model.Postcode.Length - 3);
                }
                else
                {
                    string[] postcode = model.Postcode.Split(' ');
                    firstHalfPostcode = postcode[0];
                }
                var rainfall = await _farmService.FetchRainfallAverageAsync(firstHalfPostcode);
                if (rainfall != null)
                {
                    model.Rainfall = (int)Math.Round(rainfall);
                }
                if (model.Rainfall > 0)
                {
                    if (model.IsPostCodeChanged)
                    {
                        model.ClimateDataPostCode = null;
                    }
                    HttpContext.Session.SetObjectAsJson("FarmData", model);
                    return RedirectToAction("Rainfall");
                }
            }
            else if (string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
            {
                return RedirectToAction("Rainfall");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClimatePostCode(FarmViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
            {
                ModelState.AddModelError("ClimateDataPostCode", Resource.lblEnterTheClimatePostcode);
            }

            if (!string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
            {
                FarmViewModel? farmView = null;
                if (HttpContext.Session.Keys.Contains("FarmData"))
                {
                    farmView = JsonConvert.DeserializeObject<FarmViewModel>(HttpContext.Session.GetString("FarmData"));
                }
                bool ClimateDataPostCodeChange = false;
                if (farmView != null && model.ClimateDataPostCode != farmView.ClimateDataPostCode)
                {
                    ClimateDataPostCodeChange = true;
                }
                if ((ClimateDataPostCodeChange) || (model.Rainfall == 0 || model.Rainfall == null))
                {
                    string firstHalfPostcode = string.Empty;
                    if (!model.ClimateDataPostCode.Contains(" "))
                    {
                        firstHalfPostcode = model.ClimateDataPostCode.Substring(0, model.ClimateDataPostCode.Length - 3);
                    }
                    else
                    {
                        string[] climatePostCode = model.ClimateDataPostCode.Split(' ');
                        firstHalfPostcode = climatePostCode[0];
                    }
                    var rainfall = await _farmService.FetchRainfallAverageAsync(firstHalfPostcode);
                    if (rainfall != null)
                    {
                        model.Rainfall = (int)Math.Round(rainfall);
                    }
                    if (model.Rainfall == null || model.Rainfall == 0)
                    {
                        ModelState.AddModelError("ClimateDataPostCode", Resource.lblWeatherDataCannotBeFoundForTheCurrentPostcode);
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("FarmData", model);
            return RedirectToAction("Rainfall");

        }
        [HttpGet]
        public async Task<IActionResult> Rainfall()
        {
            _logger.LogTrace($"Farm Controller : Rainfall() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model == null)
            {
                model = new FarmViewModel();
            }
            if (model.Rainfall == 0 || model.Rainfall == null)
            {
                string firstHalfPostcode = string.Empty;
                if (!model.Postcode.Contains(" "))
                {
                    firstHalfPostcode = model.Postcode.Substring(0, model.Postcode.Length - 3);
                }
                else
                {
                    string[] postcode = model.Postcode.Split(' ');
                    firstHalfPostcode = postcode[0];
                }
                var rainfall = await _farmService.FetchRainfallAverageAsync(firstHalfPostcode);
                if (rainfall != null)
                {
                    model.Rainfall = (int)Math.Round(rainfall);
                }
                HttpContext.Session.SetObjectAsJson("FarmData", model);
            }

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rainfall(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : Rainfall() post action called");
            if (farm.Rainfall == null)
            {
                ModelState.AddModelError("Rainfall", Resource.MsgEnterAverageAnnualRainfall);
            }
            if (!ModelState.IsValid)
            {
                return View("Rainfall", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }
        [HttpGet]
        public IActionResult RainfallManual()
        {
            _logger.LogTrace($"Farm Controller : RainfallManual() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RainfallManual(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : RainfallManual() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Rainfall"))
            {
                var RainfallError = ModelState["Rainfall"].Errors.Count > 0 ?
                                ModelState["Rainfall"].Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Rainfall"].RawValue, Resource.lblRainfall)))
                {
                    ModelState["Rainfall"].Errors.Clear();
                    decimal decimalValue;
                    if (decimal.TryParse(ModelState["Rainfall"].RawValue.ToString(), out decimalValue))
                    {
                        ModelState["Rainfall"].Errors.Add(RainfallError);
                    }
                    else
                    {
                        ModelState["Rainfall"].Errors.Add(Resource.MsgForRainfallManual);
                    }
                }
            }
            //we need to call api for rainfall on the basis of postcode
            if (farm.Rainfall == null)
            {
                ModelState.AddModelError("Rainfall", Resource.MsgEnterTheAverageAnnualRainfall);
            }
            if (farm.Rainfall != null)
            {
                if (farm.Rainfall < 0)
                {
                    ModelState.AddModelError("Rainfall", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
            }
            if (!ModelState.IsValid)
            {
                return View("RainfallManual", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }
        [HttpGet]
        public async Task<IActionResult> NVZ()
        {
            _logger.LogTrace($"Farm Controller : NVZ() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model != null)
            {
                if (model.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                {
                    model.NVZFields = (int)NMP.Portal.Enums.NVZFields.AllFieldsInNVZ;
                    HttpContext.Session.SetObjectAsJson("FarmData", model);
                    return RedirectToAction("Elevation");
                }
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZ(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : NVZ() post action called");
            if (farm.NVZFields == null)
            {
                ModelState.AddModelError("NVZFields", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("NVZ", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Elevation");

        }
        [HttpGet]
        public IActionResult Elevation()
        {
            _logger.LogTrace($"Farm Controller : Elevation() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Elevation(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : Elevation() post action called");
            if (farm.FieldsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("FieldsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("Elevation", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Organic");


        }
        [HttpGet]
        public IActionResult Organic()
        {
            _logger.LogTrace($"Farm Controller : Organic() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
            _logger.LogTrace($"Farm Controller : Organic() post action called");
            if (farm.RegisteredOrganicProducer == null)
            {
                ModelState.AddModelError("RegisteredOrganicProducer", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("Organic", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("LastHarvestYear");
        }
        [HttpGet]
        public IActionResult CheckAnswer(string? q)
        {
            _logger.LogTrace($"Farm Controller : CheckAnswer({q}) action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
            //model.OldPostcode = model.Postcode;
            if (q != null)
            {
                model.EncryptedIsUpdate = q;
            }
            HttpContext.Session.SetObjectAsJson("FarmData", model);
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : CheckAnswer() post action called");
            try
            {
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                farm.AverageAltitude = farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.NoneAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.below :
                        farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.AllFieldsAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.above : 0;
                //var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "relationships").Value;
                //string[] relationshipData = claim.Split(":");
                Guid organisationId = Guid.Parse(HttpContext.User.FindFirst("organisationId")?.Value);
                if (string.IsNullOrWhiteSpace(farm.ClimateDataPostCode))
                {
                    farm.ClimateDataPostCode = farm.Postcode;
                }
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
                        OrganisationID = organisationId,
                        TotalFarmArea = farm.TotalFarmArea,
                        AverageAltitude = farm.AverageAltitude,
                        RegisteredOrganicProducer = farm.RegisteredOrganicProducer,
                        MetricUnits = farm.MetricUnits,
                        EnglishRules = farm.EnglishRules,
                        NVZFields = farm.NVZFields,
                        FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                        LastHarvestYear = farm.LastHarvestYear,
                        CountryID = farm.CountryID,
                        ClimateDataPostCode = farm.ClimateDataPostCode,
                        CreatedByID = userId,
                        CreatedOn = System.DateTime.Now,
                        ModifiedByID = farm.ModifiedByID,
                        ModifiedOn = farm.ModifiedOn
                    },
                    UserID = userId,
                    RoleID = 2
                };
                (Farm farmResponse, Error error) = await _farmService.AddFarmAsync(farmData);

                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["AddFarmError"] = error.Message;
                    return View(farm);
                }
                string success = _dataProtector.Protect("true");
                farmResponse.EncryptedFarmId = _dataProtector.Protect(farmResponse.ID.ToString());
                HttpContext.Session.Remove("FarmData");
                HttpContext.Session.Remove("AddressList");
                return RedirectToAction("FarmSummary", new { id = farmResponse.EncryptedFarmId, q = success });
            }
            catch(Exception ex)
            {
                TempData["AddFarmError"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

        }
        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Farm Controller : BackCheckAnswer() action called");
            FarmViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            bool isUpdate = false;
            if (model.EncryptedIsUpdate != null)
            {
                isUpdate = Convert.ToBoolean(_dataProtector.Unprotect(model.EncryptedIsUpdate));
            }
            if (isUpdate)
            {
                return RedirectToAction("FarmDetails", new { id = model.EncryptedFarmId });
            }
            else
            {
                model.IsCheckAnswer = false;
                HttpContext.Session.SetObjectAsJson("FarmData", model);
                return RedirectToAction("LastHarvestYear");
            }

        }

        [HttpGet]
        public async Task<IActionResult> FarmSummary(string id, string? q, string? u, string? r)
        {
            _logger.LogTrace($"Farm Controller : FarmSummary() action called");
            string farmId = string.Empty;
            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Success = _dataProtector.Unprotect(q);
                if (!string.IsNullOrWhiteSpace(r))
                {
                    TempData["successMsg"] = _dataProtector.Unprotect(r);
                }
            }
            else
            {
                ViewBag.Success = "false";
            }
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                HttpContext?.Session.Remove("ReportData");
            }
            if (HttpContext.Session.Keys.Contains("StorageCapacityData"))
            {
                HttpContext?.Session.Remove("StorageCapacityData");
            }
            ViewBag.FieldCount = 0;

            FarmViewModel? farmData = null;
            Error error = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    farmId = _dataProtector.Unprotect(id);

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmList");
                    }
                    (List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetailList, error) = await _reportService.FetchNutrientsLoadingFarmDetailsByFarmId(farm.ID);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmList");
                    }
                    else
                    {
                        if (nutrientsLoadingFarmDetailList.Count > 0)
                        {
                            ViewBag.LiveStockHaveImportExportData = true;
                        }
                    }

                    (List<StoreCapacity> storeCapacityList, error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(farm.ID,null);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmList");
                    }
                    else
                    {
                        if (storeCapacityList.Count > 0)
                        {
                            ViewBag.StoreCapacityList = true;
                        }
                    }

                    if (farm != null)
                    {
                        farmData = new FarmViewModel();
                        farmData.Name = farm.Name;
                        farmData.FullAddress = string.Format("{0}, {1} {2}, {3} {4}", farm.Address1, farm.Address2 != null ? farm.Address2 + "," : string.Empty, farm.Address3, farm.Address4, farm.Postcode);
                        farmData.EncryptedFarmId = _dataProtector.Protect(farm.ID.ToString());
                        farmData.ClimateDataPostCode = farm.ClimateDataPostCode;
                        ViewBag.FieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmId));
                    }
                    List<PlanSummaryResponse> planSummaryResponse = await _cropService.FetchPlanSummaryByFarmId(Convert.ToInt32(farmId), 0);
                    planSummaryResponse.RemoveAll(x => x.Year == 0);
                    if (planSummaryResponse.Count() > 0)
                    {
                        farmData.IsPlanExist = true;
                    }
                    if (u != null)
                    {
                        farmData.EncryptedIsUpdate = u;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in FarmSummary() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
            }
            return View(farmData);

        }
        [HttpGet]
        public async Task<IActionResult> FarmDetails(string id)
        {
            _logger.LogTrace($"Farm Controller : FarmDetails({id}) action called");
            string farmId = string.Empty;
            FarmViewModel? farmData = null;
            Error error = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    farmId = _dataProtector.Unprotect(id);
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmList");
                    }

                    if (farm != null)
                    {
                        farmData = new FarmViewModel();
                        farmData.FullAddress = string.Format("{0}, {1} {2}, {3} {4}", farm.Address1, farm.Address2 != null ? farm.Address2 + "," : string.Empty, farm.Address3, farm.Address4, farm.Postcode);
                        farmData.EncryptedFarmId = _dataProtector.Protect(farm.ID.ToString());
                        farmData.ID = farm.ID;
                        farmData.Name = farm.Name;
                        farmData.Address1 = farm.Address1;
                        farmData.Address2 = farm.Address2;
                        farmData.Address3 = farm.Address3;
                        farmData.Address4 = farm.Address4;
                        farmData.Postcode = farm.Postcode;
                        farmData.CPH = farm.CPH;
                        farmData.FarmerName = farm.FarmerName;
                        farmData.BusinessName = farm.BusinessName;
                        farmData.SBI = farm.SBI;
                        farmData.STD = farm.STD;
                        farmData.Telephone = farm.Telephone;
                        farmData.Mobile = farm.Mobile;
                        farmData.Email = farm.Email;
                        farmData.Rainfall = farm.Rainfall;
                        farmData.TotalFarmArea = farm.TotalFarmArea;
                        farmData.AverageAltitude = farm.AverageAltitude;
                        farmData.RegisteredOrganicProducer = farm.RegisteredOrganicProducer;
                        farmData.MetricUnits = farm.MetricUnits;
                        farmData.EnglishRules = farm.EnglishRules;
                        farmData.NVZFields = farm.NVZFields;
                        farmData.FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel;
                        farmData.ClimateDataPostCode = farm.ClimateDataPostCode;
                        farmData.LastHarvestYear = farm.LastHarvestYear;
                        farmData.CreatedByID = farm.CreatedByID;
                        farmData.CreatedOn = farm.CreatedOn;
                        farmData.CountryID = farm.CountryID;
                        if (Enum.IsDefined(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID))
                        {
                            farmData.Country = Enum.GetName(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID);
                        }

                        bool update = true;
                        farmData.EncryptedIsUpdate = _dataProtector.Protect(update.ToString());
                        HttpContext.Session.SetObjectAsJson("FarmData", farmData);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in FarmDetails() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
            }

            return View(farmData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmUpdate(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : FarmUpdate() action called");
            try
            {
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                farm.AverageAltitude = farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.NoneAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.below :
                        farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.AllFieldsAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.above : 0;

                Guid organisationId = Guid.Parse(HttpContext.User.FindFirst("organisationId")?.Value);
                int farmId = Convert.ToInt32(_dataProtector.Unprotect(farm.EncryptedFarmId));

                int createdByID = 0;
                DateTime createdOn = DateTime.Now;
                (Farm farmDetail, Error apiError) = await _farmService.FetchFarmByIdAsync(farmId);
                if (!string.IsNullOrWhiteSpace(apiError.Message))
                {
                    TempData["Error"] = apiError.Message;
                    return RedirectToAction("FarmList");
                }
                if (farmDetail != null)
                {
                    createdByID = farmDetail.CreatedByID ?? 0;
                    createdOn = farmDetail.CreatedOn;

                }
                if (string.IsNullOrWhiteSpace(farm.ClimateDataPostCode))
                {
                    farm.ClimateDataPostCode = farm.Postcode;
                }
                var farmData = new FarmData
                {
                    Farm = new Farm()
                    {
                        ID = farmId,
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
                        OrganisationID = organisationId,
                        TotalFarmArea = farm.TotalFarmArea,
                        AverageAltitude = farm.AverageAltitude,
                        RegisteredOrganicProducer = farm.RegisteredOrganicProducer,
                        MetricUnits = farm.MetricUnits,
                        EnglishRules = farm.EnglishRules,
                        NVZFields = farm.NVZFields,
                        FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                        LastHarvestYear = farm.LastHarvestYear,
                        CountryID = farm.CountryID,
                        ClimateDataPostCode = farm.ClimateDataPostCode,
                        CreatedByID = createdByID,
                        CreatedOn = createdOn,
                        ModifiedByID = userId,
                        ModifiedOn = farm.ModifiedOn
                    },
                    UserID = userId,
                    RoleID = 2
                };

                (Farm farmResponse, Error error) = await _farmService.UpdateFarmAsync(farmData);

                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["AddFarmError"] = error.Message;
                    string EncryptUpdateStatus = _dataProtector.Protect(Resource.lblTrue.ToString());
                    return RedirectToAction("CheckAnswer", new { q = EncryptUpdateStatus });
                    return View(farm);
                }
                string success = _dataProtector.Protect("true");
                farmResponse.EncryptedFarmId = _dataProtector.Protect(farmResponse.ID.ToString());
                HttpContext.Session.Remove("FarmData");
                HttpContext.Session.Remove("AddressList");

                string isUpdate = _dataProtector.Protect("true");
                return RedirectToAction("FarmSummary", new { id = farmResponse.EncryptedFarmId, q = success, u = isUpdate });
            }
            catch (Exception ex)
            {
                TempData["AddFarmError"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

        }

        [HttpGet]
        public IActionResult FarmRemove()
        {
            _logger.LogTrace($"Farm Controller : FarmRemove() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmRemove(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : FarmRemove() post action called");
            if (farm.FarmRemove == null)
            {
                ModelState.AddModelError("FarmRemove", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("FarmRemove", farm);
            }
            if (!farm.FarmRemove.Value)
            {
                return RedirectToAction("FarmList");
            }
            else
            {
                int id = Convert.ToInt32(_dataProtector.Unprotect(farm.EncryptedFarmId));
                (string message, Error error) = await _farmService.DeleteFarmByIdAsync(id);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["AddFarmError"] = error.Message;
                    return View(farm);
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    //string success = _dataProtector.Protect("true");
                    string name = _dataProtector.Protect(farm.Name);
                    HttpContext.Session.Remove("FarmData");

                    return RedirectToAction("FarmList", new { q = name });
                }
            }
            return View(farm);

        }

        [HttpGet]
        public IActionResult LastHarvestYear()
        {
            _logger.LogTrace($"Farm Controller : LastHarvestYear() action called");
            FarmViewModel? model = new FarmViewModel();
            if (HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LastHarvestYear(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : LastHarvestYear() post action called");
            if (farm.LastHarvestYear == null)
            {
                ModelState.AddModelError("LastHarvestYear", Resource.MsgSelectAHarvestYearBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("LastHarvestYear", farm);
            }

            HttpContext.Session.SetObjectAsJson("FarmData", farm);
            return RedirectToAction("CheckAnswer");
        }
        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Farm Controller : Cancel() action called");
            FarmViewModel model = new FarmViewModel();
            try
            {
                if (HttpContext.Session.Keys.Contains("FarmData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"farm Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["AddFarmError"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(FarmViewModel model)
        {
            _logger.LogTrace("Farm Controller : Cancel() post action called");
            if (model.IsCancel == null)
            {
                ModelState.AddModelError("IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("Cancel", model);
            }
            if (!model.IsCancel.Value)
            {
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                HttpContext?.Session.Remove("FarmData");
                return RedirectToAction("FarmList", "Farm");

            }

        }
    }
}
