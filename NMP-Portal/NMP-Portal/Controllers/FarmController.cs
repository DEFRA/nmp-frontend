using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                return View("~/Views/Farm/Name.cshtml", farm);
            }
            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
                return View("~/Views/Farm/Name.cshtml", farm);
            }
            if (string.IsNullOrWhiteSpace(farm.PostCode))
            {
                ModelState.AddModelError("PostCode", Resource.MsgEnterTheFarmPostcode);
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
                string AddressListFirstOption = string.Format(Resource.lblAdddressFound, addressList.Count.ToString());
                itemList.Add(AddressListFirstOption);
            }
            itemList.AddRange(addressLines);

            if (itemList != null)
            {
                // Convert each string item to SelectListItem
                var selectListItems = itemList.Select(item => new SelectListItem { Value = item, Text = item }).ToList();

                ViewBag.AddressList = selectListItems;
            }
            return View(farm);
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
       

        private async Task<string> GetHistoricCountyFromJson(string addressLine, string postcode)
        {
            JArray addressList = await FetchAddressesFromAPI(postcode);
            JObject matchingEntry = addressList.Children<JObject>().FirstOrDefault(x => (string)x["addressLine"] == addressLine);

            // If a matching entry is found, return its historicCounty
            if (matchingEntry != null)
            {
                return (string)matchingEntry["historicCounty"];
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

                            // Extract the "results" array
                            addressList = (JArray)jsonObj["data"]["results"];

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
