using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Net;
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
        public readonly IHttpContextAccessor _httpContextAccessor;
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
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            _logger.LogTrace("Farm Controller : Index() action called");
            HttpContext.Session.Clear();
            return RedirectToAction("FarmList");
        }

        public async Task<IActionResult> FarmList(string? q)
        {
            _logger.LogTrace("Farm Controller : FarmList({0}) action called", q);
            HttpContext.Session.Clear();



            FarmsViewModel model = new FarmsViewModel();
            Error error = null;
            try
            {
                string orgId = HttpContext.User.FindFirst("organisationId").Value;
                Guid.TryParse(orgId, out Guid organisationId);
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
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in FarmList() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in FarmList() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

            return View(model);
        }
        public IActionResult CreateFarmCancel()
        {
            _logger.LogTrace("Farm Controller : CreateFarmCancel() action called");
            RemoveFarmSession();
            RemoveAddressesSession();
            return RedirectToAction("FarmList");
        }

        [HttpGet]
        public IActionResult Name()
        {
            _logger.LogTrace("Farm Controller : Name() action called");
            FarmViewModel? model = GetFarmFromSession();
            if (model == null)
            {
                model = new FarmViewModel();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Name(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Name() post action called");
            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
            }
            if (!ModelState.IsValid)
            {
                return View(farm);
            }

            SetFarmToSession(farm);

            return farm.IsCheckAnswer ? RedirectToAction("CheckAnswer") : RedirectToAction("Country");
        }

        [HttpGet]
        public async Task<IActionResult> Country()
        {
            _logger.LogTrace("Farm Controller : Country() action called");
            FarmViewModel? model = GetFarmFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogError("Farm Controller : Session not found in Country() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                (List<Country> countryList, Error error) = await _farmService.FetchCountryAsync();
                if (error != null && countryList.Count > 0)
                {
                    countryList.RemoveAll(x => x.ID == (int)NMP.Portal.Enums.FarmCountry.Scotland);
                    ViewBag.CountryList = countryList.OrderBy(c => c.Name);
                }


                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in Country() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Country(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Country() post action called");
            try
            {
                if (farm.CountryID == null)
                {
                    ModelState.AddModelError("CountryID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCountry.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    (List<Country> countryList, Error error) = await _farmService.FetchCountryAsync();
                    if (error != null && countryList.Count > 0)
                    {
                        countryList.RemoveAll(x => x.ID == (int)NMP.Portal.Enums.FarmCountry.Scotland);
                        ViewBag.CountryList = countryList.OrderBy(c => c.Name);
                    }
                    return View("Country", farm);
                }

                farm.EnglishRules = farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Scotland ? false : true;

                if (farm.CountryID.HasValue && Enum.IsDefined(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID.Value))
                {
                    farm.Country = Enum.GetName(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID);
                }

                SetFarmToSession(farm);

                if (farm.IsCheckAnswer)
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in Country() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

            return RedirectToAction("FarmingRules");
        }

        [HttpGet]
        public IActionResult FarmingRules()
        {
            _logger.LogTrace("Farm Controller : FarmingRules() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in FarmingRules() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FarmingRules(FarmViewModel farm)
        {
            SetFarmToSession(farm);

            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("PostCode");
        }

        [HttpGet]
        public IActionResult PostCode()
        {
            _logger.LogTrace("Farm Controller : PostCode() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in PostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PostCode(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : PostCode() post action called");
            try
            {
                ValidatePostcode(farm);

                if (!ModelState.IsValid)
                {
                    return View(farm);
                }

                var farmView = GetFarmFromSession();
                bool isPostcodeChanged = farmView?.Postcode != farm.Postcode;

                if (farm.IsCheckAnswer)
                {
                    SetFarmToSession(farm);
                    if (!isPostcodeChanged)
                    {
                        farm.IsPostCodeChanged = false;
                        return RedirectToAction("CheckAnswer");
                    }

                    farm.IsPostCodeChanged = true;
                    farm.Rainfall = null;
                }
                else if (isPostcodeChanged)
                {

                    farm.Rainfall = null;
                }

                SetFarmToSession(farm);

                return RedirectToAction("Address"); ;
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in PostCode()");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in PostCode()");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        private void ValidatePostcode(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.Postcode))
            {
                ModelState.AddModelError("Postcode", Resource.MsgEnterTheFarmPostcode);
                return;
            }

            int farmId = farm.EncryptedFarmId != null
                ? Convert.ToInt32(_dataProtector.Unprotect(farm.EncryptedFarmId))
                : 0;

            bool exists = _farmService.IsFarmExistAsync(farm.Name, farm.Postcode, farmId).Result;
            if (exists)
            {
                ModelState.AddModelError("Postcode", Resource.MsgFarmAlreadyExist);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Address()
        {
            _logger.LogTrace("Farm Controller : Address() action called");
            try
            {
                FarmViewModel? model = GetFarmFromSession();
                if (model == null)
                {
                    _logger.LogError("Farm Controller : Session not found in Address() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                RemoveAddressesSession();
                List<AddressLookupResponse> addresses = await _addressLookupService.AddressesAsync(model.Postcode, 0);
                var addressesList = addresses.Select(a => new SelectListItem { Value = a.AddressLine, Text = a.AddressLine }).ToList();

                if (addressesList.Count == 0)
                {
                    return RedirectToAction("AddressNotFound");
                }

                ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, addresses.Count.ToString());
                ViewBag.AddressList = addressesList;
                SetAddressesToSession(addresses);
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in Address() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in Address() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Address(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Address() post action called");

            try
            {
                ValidateAddress(farm);
                var addresses = await LoadAddressesAsync(farm);

                if (!ModelState.IsValid)
                {
                    PopulateAddressViewBags(addresses);
                    return View(farm);
                }

                ApplySelectedAddress(farm, addresses);
                farm.IsManualAddress = false;

                SetFarmToSession(farm);

                return (farm.IsPostCodeChanged || !farm.IsCheckAnswer)
                    ? RedirectToAction("ClimatePostCode")
                    : RedirectToAction("CheckAnswer");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in Address() action");
                return Functions.RedirectToErrorHandler(
                    (int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in Address() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        private void ValidateAddress(FarmViewModel farm)
        {
            if (string.IsNullOrWhiteSpace(farm.FullAddress))
            {
                ModelState.AddModelError("FullAddress", Resource.MsgSelectAddress);
            }
        }

        private async Task<List<AddressLookupResponse>> LoadAddressesAsync(FarmViewModel farm)
        {
            var addresses = GetAddressesFromSession() ?? new List<AddressLookupResponse>();
            return addresses.Count > 0
                ? addresses
                : await _addressLookupService.AddressesAsync(farm.Postcode, 0);
        }

        private void PopulateAddressViewBags(List<AddressLookupResponse> addresses)
        {
            if (addresses == null || addresses.Count == 0) return;

            var items = addresses.Select(a => new SelectListItem
            {
                Value = a.AddressLine,
                Text = a.AddressLine
            }).ToList();

            ViewBag.AddressList = items;
            ViewBag.AddressCount = string.Format(Resource.lblAdddressFound, items.Count.ToString());
        }

        private void ApplySelectedAddress(FarmViewModel farm, List<AddressLookupResponse> addresses)
        {
            var address = addresses.FirstOrDefault(a => a.AddressLine == farm.FullAddress);
            if (address == null) return;

            farm.Address1 = $"{Functions.FormatPart(address.SubBuildingName)}{Functions.FormatPart(address.BuildingNumber)}{Functions.FormatPart(address.BuildingName)}{address.Street}";
            farm.Address2 = address.Locality;
            farm.Address3 = address.Town;
            farm.Address4 = address.HistoricCounty;
        }

        public IActionResult AddressNotFound()
        {
            _logger.LogTrace("Farm Controller : AddressNotFound() action called");
            FarmViewModel? model = GetFarmFromSession();
            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in AddressNotFound() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ManualAddress()
        {
            _logger.LogTrace("Farm Controller : ManualAddress() action called");
            FarmViewModel? model = null;
            model = GetFarmFromSession();
            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in ManualAddress() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualAddress(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : ManualAddress() post action called");
            try
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
                if (!string.IsNullOrWhiteSpace(farm.Address1) && farm.Address1.Length > 50)
                {
                    ModelState.AddModelError("Address1", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine1, 50));
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
                    ModelState.AddModelError("Address4", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblCountry, 50));
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

                FarmViewModel? farmView = GetFarmFromSession();

                if (farmView != null)
                {
                    if (farmView.Postcode != farm.Postcode)
                    {
                        farm.Rainfall = null;
                    }
                }
                farm.FullAddress = string.Empty;
                farm.IsManualAddress = true;

                SetFarmToSession(farm);

                return RedirectToAction("ClimatePostCode");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in ManualAddress() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in ManualAddress() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClimatePostCode()
        {
            _logger.LogTrace("Farm Controller : ClimatePostCode() action called");

            try
            {
                var model = GetFarmFromSession();
                if (model == null)
                {
                    _logger.LogError("Farm Controller : Session not found in ClimatePostCode() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                bool rainfallNotAvailable = !model.Rainfall.HasValue || model.Rainfall <= 0;

                if (rainfallNotAvailable)
                {
                    string firstHalfPostcode = Functions.ExtractFirstHalfPostcode(model.Postcode);
                    decimal rainfall = await _farmService.FetchRainfallAverageAsync(firstHalfPostcode);
                    model.Rainfall = (int)Math.Round(rainfall);

                    if (model.Rainfall.HasValue && model.Rainfall > 0)
                    {
                        if (model.IsPostCodeChanged)
                        {
                            model.ClimateDataPostCode = null;
                        }

                        SetFarmToSession(model);
                        return RedirectToAction("Rainfall");
                    }
                }
                else if (string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
                {
                    return RedirectToAction("Rainfall");
                }

                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in ClimatePostCode() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in ClimatePostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClimatePostCode(FarmViewModel model)
        {
            _logger.LogTrace("Farm Controller : ClimatePostCode() action called");
            try
            {
                if (string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
                {
                    ModelState.AddModelError("ClimateDataPostCode", Resource.lblEnterTheClimatePostcode);
                }

                if (!string.IsNullOrWhiteSpace(model.ClimateDataPostCode))
                {
                    FarmViewModel? farmView = null;
                    farmView = GetFarmFromSession();
                    bool ClimateDataPostCodeChange = false;
                    if (farmView != null && model.ClimateDataPostCode != farmView.ClimateDataPostCode)
                    {
                        ClimateDataPostCodeChange = true;
                    }
                    if ((ClimateDataPostCodeChange) || (model.Rainfall == 0 || model.Rainfall == null))
                    {
                        string firstHalfPostcode = Functions.ExtractFirstHalfPostcode(model.ClimateDataPostCode);

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
                SetFarmToSession(model);
                return RedirectToAction("Rainfall");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in ClimatePostCode() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in ClimatePostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Rainfall()
        {
            _logger.LogTrace("Farm Controller : Rainfall() action called");
            try
            {
                FarmViewModel? model = GetFarmFromSession();

                if (model == null)
                {
                    _logger.LogError("Farm Controller : Session not found in Rainfall() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                if (model.Rainfall == 0 || model.Rainfall == null)
                {
                    string firstHalfPostcode = Functions.ExtractFirstHalfPostcode(model.Postcode);

                    decimal? rainfall = await _farmService.FetchRainfallAverageAsync(firstHalfPostcode);
                    if (rainfall != null)
                    {
                        model.Rainfall = (int)Math.Round(rainfall.Value);
                    }

                    SetFarmToSession(model);
                }

                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in Rainfall() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in Rainfall() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rainfall(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Rainfall() post action called");
            if (farm.Rainfall == null)
            {
                ModelState.AddModelError("Rainfall", Resource.MsgEnterAverageAnnualRainfall);
            }
            if (!ModelState.IsValid)
            {
                return View("Rainfall", farm);
            }

            SetFarmToSession(farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }
        [HttpGet]
        public IActionResult RainfallManual()
        {
            _logger.LogTrace("Farm Controller : RainfallManual() action called");
            FarmViewModel? model = GetFarmFromSession();
            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in RainfallManual() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RainfallManual(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : RainfallManual() post action called");
            ValidateRainfall(farm);
            if (!ModelState.IsValid)
            {
                return View("RainfallManual", farm);
            }

            SetFarmToSession(farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("NVZ");
        }

        private void ValidateRainfall(FarmViewModel farm)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Rainfall"))
            {

                var RainfallError = ModelState["Rainfall"]?.Errors.Count > 0 ?
                                ModelState["Rainfall"]?.Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Rainfall"].RawValue, Resource.lblRainfall)))
                {
                    ModelState["Rainfall"]?.Errors.Clear();
                    decimal decimalValue;
                    if (decimal.TryParse(ModelState["Rainfall"]?.RawValue?.ToString(), out decimalValue))
                    {
                        ModelState["Rainfall"]?.Errors.Add(RainfallError);
                    }
                    else
                    {
                        ModelState["Rainfall"]?.Errors.Add(Resource.MsgForRainfallManual);
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
        }

        [HttpGet]
        public async Task<IActionResult> NVZ()
        {
            _logger.LogTrace("Farm Controller : NVZ() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in NVZ() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (model.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
            {
                model.NVZFields = (int)NMP.Portal.Enums.NVZFields.AllFieldsInNVZ;
                SetFarmToSession(model);
                return RedirectToAction("Elevation");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZ(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : NVZ() post action called");
            if (farm.NVZFields == null)
            {
                ModelState.AddModelError("NVZFields", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("NVZ", farm);
            }

            SetFarmToSession(farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Elevation");
        }

        [HttpGet]
        public IActionResult Elevation()
        {
            _logger.LogTrace("Farm Controller : Elevation() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in Elevation() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Elevation(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Elevation() post action called");
            if (farm.FieldsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("FieldsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("Elevation", farm);
            }

            SetFarmToSession(farm);
            if (farm.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("Organic");


        }
        [HttpGet]
        public IActionResult Organic()
        {
            _logger.LogTrace("Farm Controller : Organic() action called");
            FarmViewModel? model = GetFarmFromSession();
            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in Organic() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Organic(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : Organic() post action called");
            if (farm.RegisteredOrganicProducer == null)
            {
                ModelState.AddModelError("RegisteredOrganicProducer", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("Organic", farm);
            }

            SetFarmToSession(farm);

            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public IActionResult CheckAnswer(string id, string? q)
        {
            _logger.LogTrace("Farm Controller : CheckAnswer({0}) action called", q);
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in CheckAnswer({0}) action", q);
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (string.IsNullOrWhiteSpace(model.FullAddress))
            {
                model.FullAddress = string.Format("{0}, {1} {2}, {3}, {4}", model.Address1, model.Address2 != null ? model.Address2 + "," : string.Empty, model.Address3, model.Address4, model.Postcode);
            }

            model.IsCheckAnswer = true;
            if (q != null)
            {
                model.EncryptedIsUpdate = q;
            }

            SetFarmToSession(model);

            if (!string.IsNullOrWhiteSpace(q))
            {
                HttpContext.Session.SetObjectAsJson("FarmDataBeforeUpdate", model);
            }
            var previousModel = HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmDataBeforeUpdate");

            bool isDataChanged = false;
            if (previousModel != null)
            {
                string oldJson = JsonConvert.SerializeObject(previousModel);
                string newJson = JsonConvert.SerializeObject(model);
                isDataChanged = !string.Equals(oldJson, newJson, StringComparison.Ordinal);
            }

            ViewBag.IsDataChange = isDataChanged;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : CheckAnswer() post action called");
            try
            {
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                farm.AverageAltitude = farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.NoneAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.below :
                farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.FieldsAbove300SeaLevel.AllFieldsAbove300m ? (int)NMP.Portal.Enums.AverageAltitude.above : 0;

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
                HttpContext.Session.Remove("FarmDataBeforeUpdate");
                return RedirectToAction("FarmSummary", new { id = farmResponse.EncryptedFarmId, q = success });
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in CheckAnswer() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in CheckAnswer() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace("Farm Controller : BackCheckAnswer() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in BackCheckAnswer() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
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
                SetFarmToSession(model);
                return RedirectToAction("Organic");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FarmSummary(string id, string? q, string? u, string? r)
        {
            _logger.LogTrace("Farm Controller : FarmSummary() action called");
            HttpContext.Session.Clear();
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

            ViewBag.FieldCount = 0;
            FarmViewModel? farmData = null;
            Error? error = null;
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

                    HttpContext.Session.SetString("current_farm_name", farm.Name ?? "");
                    HttpContext.Session.SetString("current_farm_id", id);

                    if (farm != null)
                    {
                        farmData = new FarmViewModel();
                        farmData.Name = farm.Name;
                        farmData.FullAddress = string.Format("{0}, {1} {2}, {3} {4}", farm.Address1, farm.Address2 != null ? farm.Address2 + "," : string.Empty, farm.Address3, farm.Address4, farm.Postcode);
                        farmData.EncryptedFarmId = _dataProtector.Protect(farm.ID.ToString());
                        farmData.ClimateDataPostCode = farm.ClimateDataPostCode;
                        ViewBag.FieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmId));
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller: HttpRequestException in FarmSummary() action. Message: {0}, StackTrace: {1}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError("Farm Controller : Exception in FarmSummary() action : {1} {2}", ex.Message, ex.StackTrace);
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
            return View(farmData);

        }

        [HttpGet]
        public async Task<IActionResult> FarmDetails(string id)
        {
            _logger.LogTrace("Farm Controller : FarmDetails({id}) action called", id);
            string farmId = string.Empty;
            FarmViewModel? farmData = null;
            Error? error = null;
            try
            {
                if (HttpContext.Session.Exists("FarmDataBeforeUpdate"))
                {
                    HttpContext.Session.Remove("FarmDataBeforeUpdate");
                }
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogError("Farm Controller : FarmDetails({id}) action called", id);
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.BadRequest);
                }

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
                    farmData.CreatedByID = farm.CreatedByID;
                    farmData.CreatedOn = farm.CreatedOn;
                    farmData.CountryID = farm.CountryID;
                    if (farm.CountryID.HasValue && Enum.IsDefined(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID))
                    {
                        farmData.Country = Enum.GetName(typeof(NMP.Portal.Enums.FarmCountry), farm.CountryID);
                    }

                    bool update = true;
                    farmData.EncryptedIsUpdate = _dataProtector.Protect(update.ToString());
                    SetFarmToSession(farmData);
                }

            }
            catch (HttpRequestException hre)
            {
                _logger.LogError("Farm Controller : HttpRequestException in FarmDetails() action : {0}, {1}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError("Farm Controller : Exception in FarmDetails() action : {1}, {1}", ex.Message, ex.StackTrace);
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
            return View(farmData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmUpdate(FarmViewModel farm)
        {
            _logger.LogTrace("Farm Controller : FarmUpdate() action called");
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
                }
                string success = _dataProtector.Protect("true");
                farmResponse.EncryptedFarmId = _dataProtector.Protect(farmResponse.ID.ToString());
                HttpContext.Session.Remove("FarmData");
                HttpContext.Session.Remove("AddressList");

                string isUpdate = _dataProtector.Protect("true");
                return RedirectToAction("FarmSummary", new { id = farmResponse.EncryptedFarmId, q = success, u = isUpdate });
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in FarmUpdate() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in FarmUpdate() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public IActionResult FarmRemove()
        {
            _logger.LogTrace("Farm Controller : FarmRemove() action called");
            FarmViewModel? model = GetFarmFromSession();

            if (model == null)
            {
                _logger.LogError("Farm Controller : Session not found in FarmRemove() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmRemove(FarmViewModel farm)
        {
            _logger.LogTrace($"Farm Controller : FarmRemove() post action called");
            try
            {
                if (farm.FarmRemove == null)
                {
                    ModelState.AddModelError("FarmRemove", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View("FarmRemove", farm);
                }
                if (farm.FarmRemove.HasValue && !farm.FarmRemove.Value)
                {
                    return RedirectToAction("FarmDetails", new { id = farm.EncryptedFarmId });
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
                        string name = _dataProtector.Protect(farm.Name);
                        RemoveFarmSession();
                        return RedirectToAction("FarmList", new { q = name });
                    }
                }
                return View(farm);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "Farm Controller : HttpRequestException in FarmRemove() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Farm Controller : Exception in FarmRemove() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Farm Controller : Cancel() action called");
            FarmViewModel? model = GetFarmFromSession();
            try
            {
                if (model == null)
                {
                    _logger.LogError("Farm Controller : Session not found in Cancel() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"farm Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
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
            if (model.IsCancel.HasValue && !model.IsCancel.Value)
            {
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                RemoveFarmSession();
                if (string.IsNullOrWhiteSpace(model.EncryptedFarmId))
                {
                    return RedirectToAction("FarmList");
                }
                return RedirectToAction("FarmDetails", new { id = model.EncryptedFarmId });
            }
        }

        private FarmViewModel? GetFarmFromSession()
        {
            if (HttpContext.Session.Exists("FarmData"))
            {
                return HttpContext.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            return null;
        }

        private void SetFarmToSession(FarmViewModel farm)
        {
            HttpContext.Session.SetObjectAsJson("FarmData", farm);
        }

        private void RemoveFarmSession()
        {
            if (HttpContext.Session.Exists("FarmData"))
            {
                HttpContext.Session.Remove("FarmData");
            }
        }

        private List<AddressLookupResponse>? GetAddressesFromSession()
        {
            if (HttpContext.Session.Exists("AddressList"))
            {
                return HttpContext.Session.GetObjectFromJson<List<AddressLookupResponse>>("AddressList");
            }
            return null;
        }

        private void SetAddressesToSession(List<AddressLookupResponse> addresses)
        {
            HttpContext.Session.SetObjectAsJson("AddressList", addresses);
        }

        private void RemoveAddressesSession()
        {
            if (HttpContext.Session.Exists("AddressList"))
            {
                HttpContext.Session.Remove("AddressList");
            }
        }
    }
}
