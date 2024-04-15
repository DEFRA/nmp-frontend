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

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FarmController : Controller
    {
        private readonly ILogger<FarmController> _logger;
        private readonly IDataProtector _dataProtector;
        private readonly IAddressLookupService _addressLookupService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFarmService _farmService;
        public FarmController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IAddressLookupService addressLookupService, IFarmService farmService)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _httpContextAccessor = httpContextAccessor;
            _addressLookupService = addressLookupService;
            _farmService = farmService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FarmList()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("FarmData");
            _httpContextAccessor.HttpContext?.Session.Remove("AddressList");
            FarmsViewModel model = new FarmsViewModel();

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
            FarmsViewModel model = new FarmsViewModel();
            return View("~/Views/Farm/FarmList.cshtml", model);
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
        public IActionResult Name(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
            }
            if (string.IsNullOrWhiteSpace(farm.PostCode))
            {
                ModelState.AddModelError("PostCode", Resource.MsgEnterTheFarmPostcode);
            }

            if (!ModelState.IsValid)
            {
                return View(farm);
            }
            if (farm.IsCheckAnswer)
            {
                FarmViewModel dataObject = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext.Session.GetString("FarmData"));

                var updatedFarm = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", updatedFarm);

                if (dataObject.PostCode == farm.PostCode)
                {
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    return RedirectToAction("Address");
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

            List<AddressLookupResponse> addresses = await _addressLookupService.AddressesAsync(model.PostCode, 0);
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
                farm.Address1 = string.Format("{0}{1}{2}{3}", address.SubBuildingName != null ? address.SubBuildingName + ", " : string.Empty, address.BuildingNumber !=null?address.BuildingNumber + ", ": string.Empty, address.BuildingName !=null ?address.BuildingName + ", " : string.Empty , address.Street);
                farm.Address2 = address.Locality;
                farm.Address3 = address.Town;
                farm.Address4 = address.HistoricCounty;
            }


            farm.IsManualAddress = false;
            //farm.Rainfall = farm.Rainfall ?? 600;

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", farm);
            if(farm.IsCheckAnswer)
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
            if (string.IsNullOrEmpty(farm.PostCode))
            {
                ModelState.AddModelError("PostCode", Resource.MsgEnterAPostcode);
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
            if(model == null)
            {
                model= new FarmViewModel();
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
            if(farm.IsCheckAnswer)
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
            if (farm.NVZField == null)
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
            if(model== null)
            {
                model=  new FarmViewModel();
            }

            if (string.IsNullOrWhiteSpace(model.FullAddress))
            {
                model.FullAddress = string.Format("{0}, {1} {2}, {3}, {4}", model.Address1, model.Address2 != null ? model.Address2 + "," :  string.Empty, model.Address3, model.Address4, model.PostCode);
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
                    PostCode = farm.PostCode,
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
                    NVZField = farm.NVZField,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel
                },
                UserID = 1,
                RoleID = 2
            };
            (Farm farmResponse ,Error error)= await _farmService.AddFarmAsync(farmData);
            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.AddFarmError = error.Message;
                return View(farm);
            }
            return Ok("Farm data posted successfully.");
        }
        public IActionResult BackCheckAnswer()
        {
            FarmViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            model.IsCheckAnswer =false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FarmData", model);
            return RedirectToAction("Organic");
        }
        private async Task<List<string>> GetHistoricCountyFromJson(string postcode, string addressLine)
        {
            JArray addressList = await FetchAddressesFromAPI(postcode);
            JObject matchingEntry = addressList.Children<JObject>().FirstOrDefault(x => (string)x["addressLine"] == addressLine);

            // If a matching entry is found, return its historicCounty
            List<string> strings = new List<string>();
            if (matchingEntry != null)
            {
                strings.Add(string.Concat((string)matchingEntry["buildingNumber"], " ", (string)matchingEntry["street"]));
                strings.Add((string)matchingEntry["locality"]);
                strings.Add((string)matchingEntry["town"]);
                strings.Add((string)matchingEntry["historicCounty"]);
                return strings;
            }
            else
            {
                return null; // Return null if no matching entry is found
            }
        }


        private async Task<JArray> FetchAddressesFromAPI(string postcode)
        {
            JArray addressList = new JArray();
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Encode the postcode for safe inclusion in the URL
                    string encodedPostcode = Uri.EscapeDataString(postcode);

                    // Construct the URL with the postcode parameter
                    string url = $"http://localhost:3000/vendors/address-lookup/addresses?postcode={encodedPostcode}&offset=0";


                    // Send a GET request to the URL and get the response
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the JSON response as a string
                        string jsonString = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            JObject jsonObj = JObject.Parse(jsonString);
                            string message = (string)jsonObj["message"];
                            if (!string.IsNullOrWhiteSpace(message) && message == Resource.lblMessage)
                            {
                                // Extract the "results" array
                                addressList = (JArray)jsonObj["data"]["results"];
                            }
                        }
                    }
                    else
                    {
                        //if IsSuccessStatusCode is false
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                return addressList;
            }
        }
    }
}
