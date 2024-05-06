using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class CropController : Controller
    {
        private readonly ILogger<CropController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;

        public CropController(ILogger<CropController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, IFieldService fieldService, ICropService cropService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateCropPlanCancel(string q)
        {
            _httpContextAccessor.HttpContext?.Session.Remove("CropData");
            return RedirectToAction("FarmSummary", "Farm", new { Id = q });
        }

        [HttpGet]
        public async Task<IActionResult> HarvestYearForPlan(string q)
        {
            CropViewModel model = new CropViewModel();
            Error? error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<CropViewModel>("CropData");
                }
                if (!string.IsNullOrEmpty(q))
                {
                    int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.EncryptedFarmId = q;

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmID);
                    model.isEnglishRules = farm.EnglishRules;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = string.Concat(error.Message == null ? "" : error.Message, ex.Message);
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HarvestYearForPlan(CropViewModel model)
        {
            if (model.Year == null)
            {
                ModelState.AddModelError("Year", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblHarvestYear.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

            return RedirectToAction("CropGroups");
        }

        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            CropViewModel model = new CropViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<CropViewModel>("CropData");
                }
                ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
            }
            catch (Exception ex)
            {
                TempData["ErrorOnHarvestYear"] = ex.Message;
                return RedirectToAction("HarvestYearForPlan");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(CropViewModel model)
        {
            try
            {
                if (model.CropGroupId == null)
                {
                    ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
                    return View(model);
                }

                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    CropViewModel CropData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<CropViewModel>("CropData");
                    if (CropData.CropGroupId != model.CropGroupId)
                    {
                        model.CropType = string.Empty;
                        model.CropTypeID = null;
                    }
                }
                if (model.CropGroupId != null)
                {
                    model.CropGroup = await _fieldService.FetchCropGroupById(model.CropGroupId.Value);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                TempData["CropGroupError"] = ex.Message;
                return View(model);
            }

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            CropViewModel model = new CropViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<CropViewModel>("CropData");
                }
                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    List<CropTypeResponse> cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                    var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.Both).ToList();
                    ViewBag.CropTypeList = cropTypeList.OrderBy(c => c.CropType); ;
                }
            }
            catch (Exception ex)
            {
                TempData["CropGroupError"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(CropViewModel model)
        {
            try
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    model.CropTypeID = (await _cropService.FetchCropTypeByGroupId(model.CropGroupId ?? 0)).CropTypeId;
                }
                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other && model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
                }

                //Other crop validation
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    if (model.OtherCropName == null)
                    {
                        ModelState.AddModelError("OtherCropName", string.Format(Resource.lblEnterTheCropName, Resource.lblCropType.ToLower()));
                    }
                }
                if (!ModelState.IsValid)
                {
                    if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other)
                    {
                        List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                        cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                        var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                        ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.Both).ToList().OrderBy(c => c.CropType); ;
                    }
                    return View(model);
                }
                if (model.CropTypeID != null)
                {
                    model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                TempData["CropTypeError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("VarietyName");
        }

        [HttpGet]
        public async Task<IActionResult> VarietyName()
        {
            CropViewModel model = new CropViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<CropViewModel>("CropData");
                }
                if (model != null && model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes)
                {
                    List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
                    potatoVarieties = await _cropService.FetchPotatoVarieties();
                    var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    ViewBag.PotatoVarietyList = potatoVarieties.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.Both).ToList().OrderBy(c => c.PotatoVariety); ;
                }
            }
            catch (Exception ex)
            {
                TempData["CropTypeError"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VarietyName(CropViewModel model)
        {
            try
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes)
                {
                    ModelState.AddModelError("Variety", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblVarietyName.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes)
                    {
                        List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
                        potatoVarieties = await _cropService.FetchPotatoVarieties();
                        var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                        ViewBag.PotatoVarietyList = potatoVarieties.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.Both).ToList().OrderBy(c => c.PotatoVariety); ;
                    }
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

                return RedirectToAction("VarietyName");
            }
            catch (Exception ex)
            {
                TempData["ErrorOnVariety"] = ex.Message;
                return View(model);
            }
        }
    }
}
