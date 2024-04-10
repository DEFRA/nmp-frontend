using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FarmController : Controller
    {
        private readonly ILogger<FarmController> _logger;
        private readonly IDataProtector _dataProtector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public FarmController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult FarmList()
        {
            FarmsViewModel model = new FarmsViewModel();

            if (model.Farms.Count == 0)
            {
                return RedirectToAction("Name", "Farm");

            }
            return View(model);

        }
        public IActionResult CreateFarmCancel()
        {
            FarmsViewModel model = new FarmsViewModel();
            return View("~/Views/Farm/FarmList.cshtml", model);
        }

        [HttpGet]
        public IActionResult Name()
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;
            return View();
        }

        [HttpPost]
        public IActionResult Name(FarmViewModel farm)
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms 
            ViewBag.IsUserHaveAnyFarms = model.Farms.Count > 0 ? true : false;
            return View(farm);
        }



        public async Task<IActionResult> Address(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.Name) && string.IsNullOrWhiteSpace(farm.PostCode))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
                ModelState.AddModelError("PostCode", Resource.MsgEnterTheFarmPostcode);
            }
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
                return View("~/Views/Farm/Name.cshtml", farm);
            }

            JArray addressList = await FetchAddressesFromAPI(farm.PostCode);
            List<string> addressLines = new List<string>();
            foreach (JObject result in addressList)
            {
                string addressLine = result["addressLine"].ToString();
                addressLines.Add(addressLine);
            }

            var itemList = new List<string>();
            if (addressList.Count > 0)
            {
                ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addressList.Count.ToString());
            }
            else
            {
                //return RedirectToAction("PostcodeError", "Farm", farm);
                return View("~/Views/Farm/PostcodeError.cshtml", farm);
            }
            itemList.AddRange(addressLines);

            if (itemList != null)
            {
                // Convert each string item to SelectListItem
                var selectListItems = itemList.Select(item => new SelectListItem { Value = item, Text = item }).ToList();

                ViewBag.AddressList = selectListItems;
            }
            if (farm.IsManualAddress)
            {
                farm.IsManualAddress = false;
            }
            return View(farm);
        }


        public IActionResult PostcodeError(FarmViewModel farm)
        {
            return View(farm);
        }
        [HttpGet]
        public IActionResult ManualAddress()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManualAddress(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Address.cshtml", farm);
            }
            else
            {
                farm.FullAddress = "";
                farm.IsManualAddress = true;
                var farmModel = JsonConvert.SerializeObject(farm);

                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);

                return RedirectToAction("Rainfall");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Rainfall()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);

        }
        [HttpPost]
        public async Task<IActionResult> Rainfall(FarmViewModel farm)
        {
            FarmsViewModel farmsViewModel = new FarmsViewModel();
            FarmViewModel model = new FarmViewModel();

            if (string.IsNullOrWhiteSpace(farm.FullAddress) && (!farm.IsManualAddress))
            {
                ModelState.AddModelError("FullAddress", Resource.MsgSelectAddress);
                JArray addressList = await FetchAddressesFromAPI(farm.PostCode);
                List<string> addressLines = new List<string>();
                foreach (JObject result in addressList)
                {
                    string addressLine = result["addressLine"].ToString();
                    addressLines.Add(addressLine);
                }

                var itemList = new List<string>();
                if (addressList.Count > 0)
                {
                    ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addressList.Count.ToString());
                }
                itemList.AddRange(addressLines);

                if (itemList != null)
                {
                    // Convert each string item to SelectListItem
                    var selectListItems = itemList.Select(item => new SelectListItem { Value = item, Text = item }).ToList();

                    ViewBag.AddressList = selectListItems;
                }
                return View("~/Views/Farm/Address.cshtml", farm);
            }
            else if (!string.IsNullOrWhiteSpace(farm.FullAddress) && (!farm.IsManualAddress))
            {
                List<string> addressList = await GetHistoricCountyFromJson(farm.PostCode, farm.FullAddress);
                if (addressList != null && addressList.Count > 3)
                {
                    farm.Address1 = addressList[0];
                    farm.Address2 = addressList[1];
                    farm.Address3 = addressList[2];
                    farm.Address4 = addressList[3];
                }
                farm.IsManualAddress = false;
                if (farm.Rainfall == null)
                {
                    farm.Rainfall = 600;//get rainfall default value from Api
                }
            }
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
            else
            {
                if (farm.IsManualAddress && farm.Rainfall == null) // from Manual Address screen
                {
                    farm.Rainfall = 600;  //get rainfall default value from Api

                }

                ViewBag.IsUserHaveAnyFarms = farmsViewModel.Farms.Count > 0 ? true : false;

                var farmModel = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
                return RedirectToAction("NVZ");
            }

        }
        [HttpGet]
        public async Task<IActionResult> RainfallManual()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);

        }
        [HttpPost]
        public IActionResult RainfallManual(FarmViewModel farm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Rainfall.cshtml", farm);
            }
            else
            {
                var farmModel = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
                return View(farmModel);
            }
        }
        [HttpGet]
        public async Task<IActionResult> NVZ()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);

        }
        [HttpPost]
        public IActionResult NVZ(FarmViewModel farm)
        {
            //we need to call api for rainfall on the basis of postcode
            if (farm.Rainfall == null)
            {
                ModelState.AddModelError("Rainfall", Resource.MsgEnterTheAverageAnnualRainfall);
            }
            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/RainfallManual.cshtml", farm);
            }
            else
            {
                var farmModel = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
                return View(farmModel);
            }

        }
        [HttpGet]
        public async Task<IActionResult> Elevation()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);

        }
        [HttpPost]
        public IActionResult Elevation(FarmViewModel farm)
        {
            if (farm.NVZField == null)
            {
                ModelState.AddModelError("NVZField", Resource.MsgSelectAnOptionBeforeContinuing);
                //return View("~/Views/Farm/NVZ.cshtml", farm);
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/NVZ.cshtml", farm);
            }
            else
            {
                var farmModel = JsonConvert.SerializeObject(farm);
                _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
                return View(farmModel);
            }

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
                    string url = $"http://localhost:3000/apis/v1/vendors/address-lookup/addresses?postcode={encodedPostcode}";


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
