using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Reflection;
using System.Threading.Tasks;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class PreviousCroppingController : Controller
    {
        private readonly ILogger<PreviousCroppingController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IFarmService _farmService;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IPreviousCroppingService _previousCroppingService;
        public PreviousCroppingController(ILogger<PreviousCroppingController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IFieldService fieldService, ICropService cropService, IPreviousCroppingService previousCroppingService, IFarmService farmService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _fieldService = fieldService;
            _cropService = cropService;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _previousCroppingService = previousCroppingService;
            _farmService = farmService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> HasGrassInLastThreeYear(string? q, string? r, string? s)
        {
            _logger.LogTrace($"Previous Croppping Controller: HasGrassInLastThreeYear() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                }
                else if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
                {
                    model.EncryptedFarmID = q;
                    model.EncryptedFieldID = r;
                    model.EncryptedYear = s;
                    model.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(r));
                    model.FieldName = (await _fieldService.FetchFieldByFieldId(model.FieldID)).Name;
                    int currentYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
                    //model.EncryptedCurrentYear = s;
                    model.HarvestYear = currentYear - 1;

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);
                }
                //ViewBag.PreviousYear = model.HarvestYear;
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Previous Croppping Controller : Exception in HasGrassInLastThreeYear() action : {ex.Message}, {ex.StackTrace}");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGrassInLastThreeYear(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGrassInLastThreeYear() post action called");
            if (model.HasGrassInLastThreeYear == null)
            {
                ModelState.AddModelError("HasGrassInLastThreeYear", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    PreviousCroppingViewModel previousCroppingData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                    if (model.HasGrassInLastThreeYear != previousCroppingData.HasGrassInLastThreeYear)
                    {
                        model.IsHasGrassInLastThreeYearChange = true;
                        if ((model.HasGrassInLastThreeYear != null && (!model.HasGrassInLastThreeYear.Value)))
                        {
                            model.CropGroupID = null;
                            //model.CropGroup = string.Empty;
                            model.CropTypeID = null;
                            model.CropTypeName = string.Empty;
                            //model.HarvestYear = null;
                            model.GrassManagementOptionID = null;
                            model.HasGreaterThan30PercentClover = null;
                            model.SoilNitrogenSupplyItemID = null;
                            model.PreviousGrassYears = null;
                            model.IsPreviousYearGrass = null;
                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);
                            return RedirectToAction("CropGroups");
                        }
                        else
                        {
                            if (model.HasGrassInLastThreeYear.Value)
                            {
                                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
                                return RedirectToAction("GrassLastThreeHarvestYear");
                            }
                        }
                    }
                    else
                    {
                        model.IsHasGrassInLastThreeYearChange = false;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
                        return RedirectToAction("CheckAnswer");
                    }
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.HasGrassInLastThreeYear.Value)
            {

                return RedirectToAction("GrassLastThreeHarvestYear");
            }
            else
            {
                model.CropGroupID = null;
                //model.CropGroup = string.Empty;
                model.CropTypeID = null;
                model.CropTypeName = string.Empty;
                // model.HarvestYear = null;
                model.GrassManagementOptionID = null;
                model.HasGreaterThan30PercentClover = null;
                model.SoilNitrogenSupplyItemID = null;
                model.PreviousGrassYears = null;
                model.IsPreviousYearGrass = null;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("CropGroups");
            }

        }


        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            _logger.LogTrace($"Previous Croppping Controller : CropGroups() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                cropGroups = await _fieldService.FetchCropGroups();
                List<CropGroupResponse> cropGroupArables = cropGroups.Where(x => x.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName).ToList();
                ViewBag.CropGroupList = cropGroupArables;
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Previous Croppping Controller : Exception in CropGroups() action : {ex.Message}, {ex.StackTrace}");
                TempData["HasGrassInLastThreeYearError"] = ex.Message;
                return RedirectToAction("HasGrassInLastThreeYear");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CropGroups() post action called");
            if (model.CropGroupID == null)
            {
                ModelState.AddModelError("CropGroupID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();
                cropGroups = await _fieldService.FetchCropGroups();
                if (cropGroups.Count > 0)
                {
                    ViewBag.CropGroupList = cropGroups.OrderBy(x => x.CropGroupName);
                }
                return View(model);
            }

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                PreviousCroppingViewModel previousCroppingData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                if (previousCroppingData.CropGroupID != model.CropGroupID)
                {
                    model.CropTypeName = string.Empty;
                    model.CropTypeID = null;
                }
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            //model.CropGroupID = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            _logger.LogTrace($"Previous Croppping Controller : CropTypes() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                cropTypes = await _fieldService.FetchCropTypes(model.CropGroupID ?? 0);
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmID));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    var country = (farm.CountryID.Value == (int)NMP.Portal.Enums.FarmCountry.England ||
                        farm.CountryID.Value == (int)NMP.Portal.Enums.FarmCountry.Wales) ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                    var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();

                    ViewBag.CropTypeList = cropTypeList;
                    if (cropTypeList.Count == 1)
                    {
                        if (cropTypeList[0].CropTypeId == (int)NMP.Portal.Enums.CropTypes.Other)
                        {
                            model.CropTypeID = cropTypeList[0].CropTypeId;
                            model.CropTypeName = cropTypeList[0].CropType;
                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);
                            if (model.IsCheckAnswer)
                            {
                                return RedirectToAction("CheckAnswer");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Previous Croppping Controller : Exception in CropTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CropTypes() post action called");
            if (model.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(model.CropGroupID ?? 0);
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmID));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    var country = (farm.CountryID.Value == (int)NMP.Portal.Enums.FarmCountry.England ||
                        farm.CountryID.Value == (int)NMP.Portal.Enums.FarmCountry.Wales) ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                    var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();
                    ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();
                }

                return View(model);
            }

            model.CropTypeName = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("CheckAnswer");
        }
        
        [HttpGet]
        public IActionResult GrassLastThreeHarvestYear()
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassLastThreeHarvestYear() action called");
            Error error = new Error();

            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            try
            {
                List<int> previousYears = new List<int>();
                int lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousGrassesYear = previousYears;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            }
            catch (Exception ex)
            {
                TempData["HasGrassInLastThreeYearError"] = ex.Message;
                return RedirectToAction("HasGrassInLastThreeYear");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrassLastThreeHarvestYear(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassLastThreeHarvestYear() post action called");
            int lastHarvestYear = 0;
            if (model.PreviousGrassYears == null)
            {
                ModelState.AddModelError("PreviousGrassYears", Resource.lblSelectAtLeastOneYearBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousGrassesYear = previousYears;
                return View(model);
            }
            //below condition is for select all
            if (model.PreviousGrassYears?.Count == 1 && model.PreviousGrassYears[0] == 0)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                model.PreviousGrassYears = previousYears;
            }
            lastHarvestYear = model.HarvestYear ?? 0;
            model.IsPreviousYearGrass = (model.PreviousGrassYears != null && model.PreviousGrassYears.Contains(lastHarvestYear)) ? true : false;

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);

            if (model.PreviousGrassYears?.Count == 3)
            {
                model.LayDuration = (int)NMP.Portal.Enums.LayDuration.ThreeYearsOrMore;
            }
            else if (model.PreviousGrassYears?.Count <= 2 && model.PreviousGrassYears[0] == model.HarvestYear)
            {
                model.LayDuration = (int)NMP.Portal.Enums.LayDuration.OneToTwoYears;
            }
            else
            {
                return RedirectToAction("LayDuration");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("GrassManagementOptions");
        }

        [HttpGet]
        public async Task<IActionResult> GrassManagementOptions()
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassManagementOptions() action called");
            Error error = new Error();

            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
            ViewBag.GrassManagementOptions = commonResponses;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassManagementOptions(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassManagementOptions() post action called");

            if (model.GrassManagementOptionID == null)
            {
                ModelState.AddModelError("GrassManagementOptionID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = commonResponses;
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.GrassManagementOptionID == (int)NMP.Portal.Enums.GrassManagementOption.GrazedOnly)
            {
                return RedirectToAction("HasGreaterThan30PercentClover");
            }
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("HasGreaterThan30PercentClover");
        }



        [HttpGet]
        public async Task<IActionResult> HasGreaterThan30PercentClover()
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGreaterThan30PercentClover() action called");
            Error error = new Error();

            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGreaterThan30PercentClover(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGreaterThan30PercentClover() post action called");
            if (model.HasGreaterThan30PercentClover == null)
            {
                ModelState.AddModelError("HasGreaterThan30PercentClover", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            if (model.HasGreaterThan30PercentClover.Value)
            {
                if (model.IsPreviousYearGrass == false)
                {
                    return RedirectToAction("CropGroups");
                }
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                return RedirectToAction("SoilNitrogenSupplyItems");
            }

        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyItems()
        {
            _logger.LogTrace($"Previous Croppping Controller : SoilNitrogenSupplyItems() action called");
            Error error = new Error();

            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
            ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNitrogenSupplyItems(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : SoilNitrogenSupplyItems() post action called");

            if (model.SoilNitrogenSupplyItemID == null)
            {
                ModelState.AddModelError("SoilNitrogenSupplyItemID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            if (model.IsPreviousYearGrass == false)
            {
                return RedirectToAction("CropGroups");
            }

            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public IActionResult LayDuration()
        {
            _logger.LogTrace($"Previous Croppping Controller : LayDuration() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LayDuration(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : LayDuration() post action called");

            if (model.LayDuration == null)
            {
                ModelState.AddModelError("LayDuration", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("GrassManagementOptions");
        }
        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"Previous Croppping Controller : CheckAnswer() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
                model.IsCheckAnswer = true;
                model.IsHasGrassInLastThreeYearChange = false;
                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldService.FetchCropGroupById(model.CropGroupID.Value);
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("PreviousCroppingData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Previous Croppping Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CheckAnswer() post action called");
            if (!model.HasGrassInLastThreeYear.HasValue)
            {
                ModelState.AddModelError("HasGrassInLastThreeYear", $"{string.Format(Resource.lblHasFieldNameBeenUsedForGrassInAnyOfTheLastThreeYear, model.FieldName)} {Resource.lblNotSet}");
            }
            else
            {
                if (model.HasGrassInLastThreeYear.Value)
                {
                    if (model.PreviousGrassYears == null)
                    {
                        ModelState.AddModelError("GrassLastThreeHarvestYear", $"{string.Format(Resource.lblInWhichYearsWasUsedForGrass, model.FieldName)} {Resource.lblNotSet}");
                    }

                    if (model.PreviousGrassYears != null && model.PreviousGrassYears.Count == 3 && model.LayDuration == null)
                    {
                        ModelState.AddModelError("LayDuration", $"{string.Format(Resource.lblWhatWasTheLengthOfTheLayInYear, model.PreviousGrassYears?[0])} {Resource.lblNotSet}");
                    }

                    if (model.GrassManagementOptionID == null)
                    {
                        ModelState.AddModelError("GrassManagementOptionID", $"{Resource.lblHowWasTheGrassTypicallyManagedEachYear} {Resource.lblNotSet}");
                    }

                    if (model.HasGreaterThan30PercentClover == null)
                    {
                        ModelState.AddModelError("HasGreaterThan30PercentClover", $"{string.Format(Resource.lblDoesFieldTypicallyHaveMoreThan30PercentClover, model.FieldName)} {Resource.lblNotSet}");
                    }
                    if (model.HasGreaterThan30PercentClover == false && model.SoilNitrogenSupplyItemID == null)
                    {
                        ModelState.AddModelError("SoilNitrogenSupplyItemID", $"{string.Format(Resource.lblHowMuchNitrogenHasBeenAppliedToFieldEachYear, model.FieldName)} {Resource.lblNotSet}");
                    }
                    if (model.CropTypeID == null)
                    {
                        if (model.HasGrassInLastThreeYear.HasValue && (!model.HasGrassInLastThreeYear.Value)
                            || (model.IsPreviousYearGrass.HasValue && !model.IsPreviousYearGrass.Value))
                        {
                            ModelState.AddModelError("CropTypeID", $"{string.Format(Resource.lblWhatWasTheCropType, model.HarvestYear)} {Resource.lblNotSet}");
                        }
                    }
                }
                else
                {

                    if (model.CropTypeID == null)
                    {
                        ModelState.AddModelError("CropTypeID", $"{string.Format(Resource.lblWhatWasTheCropType, model.HarvestYear)} {Resource.lblNotSet}");
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;
                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldService.FetchCropGroupById(model.CropGroupID.Value);
                }

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
                return View(model);
            }


            List<PreviousCropping> previousCropping = new List<PreviousCropping>();
            Error error = null; 
            int? id = null;
            (List<PreviousCroppingData> previousCropList, error) = await _previousCroppingService.FetchDataByFieldId(model.FieldID, null);
            if (model.IsPreviousYearGrass == true && model.PreviousGrassYears != null)
            {                
                model.CropGroupID = (int)NMP.Portal.Enums.CropGroup.Grass;
                model.CropTypeID = (int)NMP.Portal.Enums.CropTypes.Grass;
                foreach (var year in model.PreviousGrassYears)
                {
                    //model.HarvestYear = year;
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count>0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == year).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    var newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        FieldID = model.FieldID,
                        CropGroupID = model.CropGroupID,
                        CropTypeID = model.CropTypeID,
                        HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                        HarvestYear = year,
                        LayDuration = model.LayDuration,
                        GrassManagementOptionID = model.GrassManagementOptionID,
                        HasGreaterThan30PercentClover = model.HasGreaterThan30PercentClover,
                        SoilNitrogenSupplyItemID = model.SoilNitrogenSupplyItemID
                    };
                    previousCropping.Add(newPreviousCropping);
                }
                if (model.PreviousGrassYears.Count < 3)
                {
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 1))
                    {
                        var newPreviousCropping = new PreviousCropping
                        {
                            ID=id,
                            CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                            CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                            HarvestYear = model.HarvestYear - 1,
                            HasGrassInLastThreeYear = true,
                            FieldID = model.FieldID,
                        };
                        previousCropping.Add(newPreviousCropping);
                    }

                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 2))
                    {
                        var newPreviousCropping = new PreviousCropping
                        {
                            ID= id,
                            CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                            CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                            HarvestYear = model.HarvestYear - 2,
                            HasGrassInLastThreeYear = true,
                            FieldID = model.FieldID,
                        };
                        previousCropping.Add(newPreviousCropping);
                    }
                }
            }
            else
            {
                id = null;
                if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                {
                    id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear.Value).Select(x => x.ID).FirstOrDefault(); ;
                }
                var newPreviousCropping = new PreviousCropping
                {
                    ID = id,
                    FieldID = model.FieldID,
                    CropGroupID = model.CropGroupID,
                    CropTypeID = model.CropTypeID,
                    HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                    HarvestYear = model.HarvestYear,
                    LayDuration = null,
                    GrassManagementOptionID = null,
                    HasGreaterThan30PercentClover = null,
                    SoilNitrogenSupplyItemID = null
                };
                previousCropping.Add(newPreviousCropping);

                if (model.PreviousGrassYears != null)
                {
                    model.CropGroupID = (int)NMP.Portal.Enums.CropGroup.Grass;
                    model.CropTypeID = (int)NMP.Portal.Enums.CropTypes.Grass;
                    foreach (var year in model.PreviousGrassYears)
                    {
                        //model.HarvestYear = year;

                        id = null;
                        if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                        {
                            id = previousCropList.Where(x => x.HarvestYear == year).Select(x => x.ID).FirstOrDefault(); ;
                        }
                        var newPreviousGrass = new PreviousCropping
                        {
                            ID = id,
                            FieldID = model.FieldID,
                            CropGroupID = model.CropGroupID,
                            CropTypeID = model.CropTypeID,
                            HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                            HarvestYear = year,
                            LayDuration = model.LayDuration,
                            GrassManagementOptionID = model.GrassManagementOptionID,
                            HasGreaterThan30PercentClover = model.HasGreaterThan30PercentClover,
                            SoilNitrogenSupplyItemID = model.SoilNitrogenSupplyItemID
                        };
                        previousCropping.Add(newPreviousGrass);                       
                    }
                    if (model.PreviousGrassYears.Count < 3)
                    {
                        if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 1))
                        {
                            id = null;
                            if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                            {
                                id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault(); ;
                            }
                            newPreviousCropping = new PreviousCropping
                            {
                                ID = id,
                                CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                                FieldID = model.FieldID,
                                CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                                HarvestYear = model.HarvestYear - 1,
                                HasGrassInLastThreeYear = true
                            };
                            previousCropping.Add(newPreviousCropping);
                        }
                        if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 2))
                        {
                            id = null;
                            if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                            {
                                id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault(); ;
                            }
                            newPreviousCropping = new PreviousCropping
                            {
                                ID = id,
                                CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                                FieldID = model.FieldID,
                                CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                                HarvestYear = model.HarvestYear - 2,
                                HasGrassInLastThreeYear = true
                            };
                            previousCropping.Add(newPreviousCropping);
                        }
                    }
                }
                else
                {
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                        CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                        FieldID = model.FieldID,
                        HarvestYear = model.HarvestYear - 1,
                        HasGrassInLastThreeYear = false,
                    };
                    previousCropping.Add(newPreviousCropping);

                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        CropGroupID = (int)NMP.Portal.Enums.CropGroup.Other,
                        CropTypeID = (int)NMP.Portal.Enums.CropTypes.Other,
                        FieldID = model.FieldID,
                        HarvestYear = model.HarvestYear - 2,
                        HasGrassInLastThreeYear = false,
                    };
                    previousCropping.Add(newPreviousCropping);
                }

            }
            //var previousCropWrapper = new
            //{
            //    PreviousCropping = previousCropping
            //};

            var previousDataWrapper = new
            {
                PreviousCroppings = previousCropping
            };
            string jsonData = JsonConvert.SerializeObject(previousDataWrapper);
            (bool success, error) = await _previousCroppingService.MergePreviousCropping(jsonData);
            if (string.IsNullOrWhiteSpace(error.Message) && success)
            {
                HttpContext?.Session.Remove("PreviousCroppingData");
                return RedirectToAction("Recommendations", "Crop", new
                {
                    q = model.EncryptedFarmID,
                    r = model.EncryptedFieldID,
                    s = model.EncryptedYear,
                    t = _cropDataProtector.Protect(Resource.MsgRecommendationsUpdated)
                });
            }
            else
            {
                TempData["Error"] = error.Message;
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;
                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldService.FetchCropGroupById(model.CropGroupID.Value);
                }

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
            }



            return View(model);
        }

        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Previous cropping Controller : BackCheckAnswer() action called");
            PreviousCroppingViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            if (model.HasGrassInLastThreeYear != null && model.HasGrassInLastThreeYear.Value)
            {
                if (!model.PreviousGrassYears.Contains(model.HarvestYear ?? 0))
                {
                    return RedirectToAction("CropTypes");
                }
                else
                {
                    if (model.HasGreaterThan30PercentClover == false)
                    {
                        return RedirectToAction("SoilNitrogenSupplyItems");
                    }
                    else
                    {
                        return RedirectToAction("HasGreaterThan30PercentClover");
                    }
                }

            }
            return RedirectToAction("CropTypes");
        }


        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Previous cropping Controller : Cancel() action called");
            PreviousCroppingViewModel? model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("PreviousCroppingData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("PreviousCroppingData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Previous cropping  Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(PreviousCroppingViewModel model)
        {
            _logger.LogTrace("Previous cropping Controller : Cancel() post action called");
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
                return RedirectToAction("Recommendations", "Crop", new
                {
                    q = model.EncryptedFarmID,
                    r = model.EncryptedFieldID,
                    s = model.EncryptedYear,
                });
            }

        }
    }
}
