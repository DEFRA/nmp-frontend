using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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
            return View(new FarmViewModel());
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

        public IActionResult ManualAddress(string? farmName)
        {
            FarmViewModel model = new FarmViewModel();
            model.Name = farmName ?? string.Empty;
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
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode,
                    FullAddress = "",
                    IsManualAddress = true,
                    NVZField = farm.NVZField,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                    RegistredOrganicProducer = farm.RegistredOrganicProducer,
                    Rainfall = farm.Rainfall,
                    IsCheckAnswer = farm.IsCheckAnswer

                };
                return View(model);
            }
        }

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
                ModelState.AddModelError("Address1", Resource.MsgEnterAnAddress);
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
                model.Name = farm.Name;
                model.PostCode = farm.PostCode;
                model.Address1 = farm.Address1;
                model.Address2 = farm.Address2;
                model.Address3 = farm.Address3;
                model.Address4 = farm.Address4;
                model.FullAddress = farm.FullAddress;
                model.IsManualAddress = farm.IsManualAddress;
                model.Rainfall = farm.Rainfall;
                model.NVZField = farm.NVZField;
                model.RegistredOrganicProducer = farm.RegistredOrganicProducer;
                model.FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel;
                model.IsCheckAnswer= farm.IsCheckAnswer;
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
                    Address4 = farm.Address4,
                    FullAddress = farm.FullAddress,
                    IsManualAddress = farm.IsManualAddress,
                    Rainfall = farm.Rainfall,
                    NVZField = farm.NVZField,
                    RegistredOrganicProducer = farm.RegistredOrganicProducer,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel
                };
                return View(model);
            }
        }
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
                if (farm.IsCheckAnswer)
                {
                    farm.OldPostcode = string.Empty;
                   return RedirectToAction("CheckAnswer", farm);
                }
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode,
                    Rainfall = farm.Rainfall,
                    FullAddress = farm.FullAddress,
                    RegistredOrganicProducer = farm.RegistredOrganicProducer,
                    NVZField = farm.NVZField,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                    IsManualAddress = farm.IsManualAddress


                };
                return View(model);
            }

        }
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
                FarmViewModel model = new FarmViewModel
                {
                    Name = farm.Name,
                    Address1 = farm.Address1,
                    Address2 = farm.Address2,
                    Address3 = farm.Address3,
                    Address4 = farm.Address4,
                    PostCode = farm.PostCode,
                    FullAddress = farm.FullAddress,
                    Rainfall = farm.Rainfall,
                    RegistredOrganicProducer = farm.RegistredOrganicProducer,
                    NVZField = farm.NVZField,
                    FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
                    IsManualAddress = farm.IsManualAddress


                };
                return View(model);
            }

        }
        public IActionResult Organic(FarmViewModel farm)
        {
            if (farm.FieldsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("FieldsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Elevation.cshtml", farm);
            }
            FarmViewModel model = new FarmViewModel
            {
                Name = farm.Name,
                Address1 = farm.Address1,
                Address2 = farm.Address2,
                Address3 = farm.Address3,
                Address4 = farm.Address4,
                PostCode = farm.PostCode,
                FullAddress = farm.FullAddress,
                Rainfall = farm.Rainfall,
                RegistredOrganicProducer = farm.RegistredOrganicProducer,
                NVZField = farm.NVZField,
                FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel


            };
            return View(model);
        }
        public IActionResult CheckAnswer(FarmViewModel farm)
        {
            if (farm.RegistredOrganicProducer == null)
            {
                ModelState.AddModelError("RegistredOrganicProducer", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Farm/Elevation.cshtml", farm);
            }
            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                farm.FullAddress = string.Format("{0},{1},{2},{3},{4}", farm.Address1, farm.Address2, farm.Address3, farm.Address4, farm.PostCode);
            }
            if (!string.IsNullOrWhiteSpace(farm.OldPostcode))
            {
                if(farm.OldPostcode!= farm.PostCode)
                {
                    return RedirectToAction("Address",farm);
                }
            }
            //    FarmViewModel model = new FarmViewModel
            //    {
            //        Name = farm.Name,
            //        Address1 = farm.Address1,
            //        Address2 = farm.Address2,
            //        Address3 = farm.Address3,
            //        Address4 = farm.Address4,
            //        PostCode = farm.PostCode,
            //        Rainfall = farm.Rainfall,
            //        RegistredOrganicProducer = farm.RegistredOrganicProducer,
            //        NVZField = farm.NVZField,
            //        FieldsAbove300SeaLevel = farm.FieldsAbove300SeaLevel,
            //        FullAddress= farm.FullAddress,
            //        OldPostcode=farm.PostCode,
            //        IsCheckAnswer = true

            //};

            farm.OldPostcode = farm.PostCode;
            farm.IsCheckAnswer= true;
            return View(farm);
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
