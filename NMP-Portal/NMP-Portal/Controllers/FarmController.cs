using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
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
            _httpContextAccessor.HttpContext?.Session.Clear();
            FarmsViewModel model = new FarmsViewModel();

            if (model.Farms.Count == 0)
            {
                return RedirectToAction("Name", "Farm");
            }

            return View(model);

        }
        public IActionResult CreateFarmCancel()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
            FarmsViewModel model = new FarmsViewModel();
            return View("~/Views/Farm/FarmList.cshtml", model);
        }

        [HttpGet]
        public IActionResult Name()
        {
            FarmViewModel model = null;
            if (_httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData"));
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

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);

            return RedirectToAction("Address");
        }
        [HttpGet]
        public async Task<IActionResult> Address()
        {
            FarmViewModel model = null;
            if (_httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData"));
            }

            JArray addressList = await FetchAddressesFromAPI(model.PostCode);
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
                return RedirectToAction("AddressNotFound");
            }
            itemList.AddRange(addressLines);

            if (itemList != null)
            {
                // Convert each string item to SelectListItem
                var selectListItems = itemList.Select(item => new SelectListItem { Value = item, Text = item }).ToList();

                ViewBag.AddressList = selectListItems;
                // Serialize ViewBag.AddressList to JSON string
                var jsonString = JsonConvert.SerializeObject(selectListItems);
                _httpContextAccessor.HttpContext?.Session.SetString("AddressList", jsonString);


            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Address(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                ModelState.AddModelError("FullAddress", Resource.MsgSelectAddress);
            }

            if (!ModelState.IsValid)
            {
                if (_httpContextAccessor.HttpContext.Session.Keys.Contains("AddressList"))
                {
                    var addressList = JsonConvert.DeserializeObject<List<SelectListItem>>(_httpContextAccessor.HttpContext?.Session.GetString("AddressList"));
                    if (addressList != null && addressList.Count > 0)
                    {
                        ViewBag.AddressList = addressList;
                        ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addressList.Count.ToString());
                    }
                }

                return View(farm);
            }

            farm.IsManualAddress = false;
            farm.Rainfall = farm.Rainfall ?? 600;

            var farmData = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmData);


            return RedirectToAction("Rainfall");
        }


        public IActionResult AddressNotFound()
        {
            FarmViewModel model = null;
            if (_httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData"));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ManualAddress()
        {
            FarmViewModel model = null;
            if (_httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                model = JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData"));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualAddress(FarmViewModel farm)
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

            farm.FullAddress = "";
            farm.IsManualAddress = true;
            farm.Rainfall = farm.Rainfall ?? 600;
            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);

            return RedirectToAction("Rainfall");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rainfall(FarmViewModel farm)
        {
            FarmsViewModel farmsViewModel = new FarmsViewModel();
            FarmViewModel model = new FarmViewModel();

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
            return RedirectToAction("NVZ");
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

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);

            return RedirectToAction("NVZ");
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
        [ValidateAntiForgeryToken]
        public IActionResult NVZ(FarmViewModel farm)
        {
            if (farm.NVZField == null)
            {
                ModelState.AddModelError("NVZField", Resource.MsgSelectAnOptionBeforeContinuing);
                //return View("~/Views/Farm/NVZ.cshtml", farm);
            }
            if (!ModelState.IsValid)
            {
                return View("NVZ", farm);
            }

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
            return RedirectToAction("Elevation");

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

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
            return RedirectToAction("Organic");


        }
        [HttpGet]
        public async Task<IActionResult> Organic()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
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

            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                farm.FullAddress = string.Format("{0},{1},{2},{3},{4}", farm.Address1, farm.Address2, farm.Address3, farm.Address4, farm.PostCode);
            }
            if (!string.IsNullOrWhiteSpace(farm.OldPostcode))
            {
                if (farm.OldPostcode != farm.PostCode)
                {
                    return RedirectToAction("Address", farm);
                }
            }
            farm.OldPostcode = farm.PostCode;
            farm.IsCheckAnswer = true;

            var farmModel = JsonConvert.SerializeObject(farm);
            _httpContextAccessor.HttpContext?.Session.SetString("FarmData", farmModel);
            return RedirectToAction("CheckAnswer");
        }
        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            FarmViewModel model = new FarmViewModel();
            if (_httpContextAccessor.HttpContext.Session.GetString("FarmData") != null)
            {
                model = (JsonConvert.DeserializeObject<FarmViewModel>(_httpContextAccessor.HttpContext?.Session.GetString("FarmData")));
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FarmViewModel farm)
        {
            List<string> addressList2 = await GetHistoricCountyFromJson(farm.PostCode, farm.FullAddress);
            if (addressList2 != null && addressList2.Count > 3)
            {
                farm.Address1 = addressList2[0];
                farm.Address2 = addressList2[1];
                farm.Address3 = addressList2[2];
                farm.Address4 = addressList2[3];
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
                    PostCode = farm.PostCode,
                    CPH = farm.CPH,
                    FarmerName = farm.Name,
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
           
            string jsonData = JsonConvert.SerializeObject(farmData);
            using (var client = new HttpClient())
            {
                string apiUrl = "http://localhost:3000/farm";

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                try
                {
                    var response = await client.PostAsync(apiUrl, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return Ok("Farm data posted successfully.");
                        //return RedirectToAction("CheckAnswer", farm);
                    }
                    else
                    {
                        return StatusCode((int)response.StatusCode, $"Failed to post farm data. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return RedirectToAction("CheckAnswer", farm);
                }
                
                
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
