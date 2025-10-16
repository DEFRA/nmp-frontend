using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Helpers;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    public class PreviousCropppingController : Controller
    {
        private readonly ILogger<PreviousCropppingController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public PreviousCropppingController(ILogger<PreviousCropppingController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> IsPreviousYearGrass()
        {
            _logger.LogTrace($"Field Controller : IsPreviousYearGrass() action called");
            PreviousCroppingViewModel model = new PreviousCroppingViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PreviousCroppingViewModel>("FieldData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

               
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CropGroups() action : {ex.Message}, {ex.StackTrace}");
                
               
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CropGroups() post action called");
            if (field.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();
                cropGroups = await _fieldService.FetchCropGroups();
                if (cropGroups.Count > 0)
                {
                    ViewBag.CropGroupList = cropGroups.OrderBy(x => x.CropGroupName);
                }
                return View(field);
            }

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                if (fieldData.CropGroupId != field.CropGroupId)
                {
                    field.CropType = string.Empty;
                    field.CropTypeID = null;
                }
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            field.CropGroup = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);

            return RedirectToAction("CropTypes");
        }

        //[HttpGet]
        //public async Task<IActionResult> CropGroups()
        //{
        //    _logger.LogTrace($"Field Controller : CropGroups() action called");
        //    FieldViewModel model = new FieldViewModel();
        //    List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

        //    try
        //    {
        //        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //        {
        //            model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //        }
        //        else
        //        {
        //            return RedirectToAction("FarmList", "Farm");
        //        }
        //        cropGroups = await _fieldService.FetchCropGroups();
        //        List<CropGroupResponse> cropGroupArables = cropGroups.Where(x => x.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName).ToList();
        //        //_httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropGroupList", cropGroups);
        //        ViewBag.CropGroupList = cropGroupArables;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogTrace($"Field Controller : Exception in CropGroups() action : {ex.Message}, {ex.StackTrace}");
        //        //TempData["Error"] = ex.Message;
        //        if (model.RecentSoilAnalysisQuestion != null && model.RecentSoilAnalysisQuestion.Value == true)
        //        {
        //            ViewBag.Error = ex.Message;
        //            return RedirectToAction("SoilNutrientValue");
        //        }
        //        else if (model.PreviousGrasses.HasGrassInLastThreeYear != null)
        //        {
        //            TempData["Error"] = ex.Message;
        //            return RedirectToAction("HasGrassInLastThreeYear");
        //        }
        //        else
        //        {
        //            TempData["Error"] = ex.Message;
        //            return RedirectToAction("RecentSoilAnalysisQuestion");
        //        }
        //        //return RedirectToAction("SNSCalculationMethod");
        //    }
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CropGroups(FieldViewModel field)
        //{
        //    _logger.LogTrace($"Field Controller : CropGroups() post action called");
        //    if (field.CropGroupId == null)
        //    {
        //        ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();
        //        cropGroups = await _fieldService.FetchCropGroups();
        //        if (cropGroups.Count > 0)
        //        {
        //            ViewBag.CropGroupList = cropGroups.OrderBy(x => x.CropGroupName);
        //        }
        //        return View(field);
        //    }

        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //        if (fieldData.CropGroupId != field.CropGroupId)
        //        {
        //            field.CropType = string.Empty;
        //            field.CropTypeID = null;
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    field.CropGroup = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
        //    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);

        //    return RedirectToAction("CropTypes");
        //}

        //[HttpGet]
        //public async Task<IActionResult> CropTypes()
        //{
        //    _logger.LogTrace($"Field Controller : CropTypes() action called");
        //    FieldViewModel model = new FieldViewModel();
        //    List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();

        //    try
        //    {
        //        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //        {
        //            model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //        }
        //        else
        //        {
        //            return RedirectToAction("FarmList", "Farm");
        //        }

        //        cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
        //        var country = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
        //        var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();

        //        ViewBag.CropTypeList = cropTypeList;
        //        if (cropTypeList.Count == 1)
        //        {
        //            if (cropTypeList[0].CropTypeId == (int)NMP.Portal.Enums.CropTypes.Other)
        //            {
        //                model.CropTypeID = cropTypeList[0].CropTypeId;
        //                model.CropType = cropTypeList[0].CropType;
        //                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
        //                if (model.IsCheckAnswer)
        //                {
        //                    return RedirectToAction("CheckAnswer");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogTrace($"Field Controller : Exception in CropTypes() action : {ex.Message}, {ex.StackTrace}");
        //        TempData["Error"] = ex.Message;
        //        return RedirectToAction("CropGroups");
        //    }

        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CropTypes(FieldViewModel field)
        //{
        //    _logger.LogTrace($"Field Controller : CropTypes() post action called");
        //    if (field.CropTypeID == null)
        //    {
        //        ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
        //        cropTypes = await _fieldService.FetchCropTypes(field.CropGroupId ?? 0);
        //        var country = field.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
        //        ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();
        //        return View(field);
        //    }
        //    field.CropType = await _fieldService.FetchCropTypeById(field.CropTypeID.Value);
        //    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);
        //    if (field.IsCheckAnswer)
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }
        //    return RedirectToAction("CheckAnswer");
        //}

        //[HttpGet]
        //public async Task<IActionResult> HasGrassInLastThreeYear()
        //{
        //    _logger.LogTrace($"Field Controller : HasGrassInLastThreeYear() action called");
        //    Error error = new Error();

        //    FieldViewModel model = new FieldViewModel();
        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult HasGrassInLastThreeYear(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : HasGrassInLastThreeYear() post action called");
        //    if (model.PreviousGrasses.HasGrassInLastThreeYear == null)
        //    {
        //        ModelState.AddModelError("PreviousGrasses.HasGrassInLastThreeYear", Resource.MsgSelectAnOptionBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    if (model.IsCheckAnswer)
        //    {
        //        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //        {
        //            FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //            if (fieldData.PreviousGrasses != null &&
        //                model.PreviousGrasses != null &&
        //                fieldData.PreviousGrasses.HasGrassInLastThreeYear != model.PreviousGrasses.HasGrassInLastThreeYear)
        //            {
        //                model.IsHasGrassInLastThreeYearChange = true;
        //                if ((model.PreviousGrasses.HasGrassInLastThreeYear != null && (!model.PreviousGrasses.HasGrassInLastThreeYear.Value)))
        //                {
        //                    model.CropGroupId = null;
        //                    model.CropGroup = string.Empty;
        //                    model.CropTypeID = null;
        //                    model.CropType = string.Empty;
        //                    model.PreviousGrasses.HarvestYear = null;
        //                    model.PreviousGrasses.GrassManagementOptionID = null;
        //                    model.PreviousGrasses.HasGreaterThan30PercentClover = null;
        //                    model.PreviousGrasses.SoilNitrogenSupplyItemID = null;
        //                    model.PreviousGrassYears = null;
        //                    model.IsPreviousYearGrass = null;
        //                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
        //                    return RedirectToAction("CropGroups");
        //                }
        //                else
        //                {
        //                    if (model.PreviousGrasses.HasGrassInLastThreeYear.Value)
        //                    {
        //                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //                        return RedirectToAction("GrassLastThreeHarvestYear");
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                model.IsHasGrassInLastThreeYearChange = false;
        //                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //                return RedirectToAction("CheckAnswer");
        //            }
        //        }
        //        //if ((model.PreviousGrasses.HasGrassInLastThreeYear != null && (!model.PreviousGrasses.HasGrassInLastThreeYear.Value)))
        //        //{
        //        //    model.CropGroupId = null;
        //        //    model.CropGroup = string.Empty;
        //        //    model.CropTypeID = null;
        //        //    model.CropType = string.Empty;
        //        //    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
        //        //    return RedirectToAction("CropGroups");
        //        //}
        //        //return RedirectToAction("CheckAnswer");
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.PreviousGrasses.HasGrassInLastThreeYear.Value)
        //    {

        //        return RedirectToAction("GrassLastThreeHarvestYear");
        //    }
        //    else
        //    {
        //        model.CropGroupId = null;
        //        model.CropGroup = string.Empty;
        //        model.CropTypeID = null;
        //        model.CropType = string.Empty;
        //        model.PreviousGrasses.HarvestYear = null;
        //        model.PreviousGrasses.GrassManagementOptionID = null;
        //        model.PreviousGrasses.HasGreaterThan30PercentClover = null;
        //        model.PreviousGrasses.SoilNitrogenSupplyItemID = null;
        //        model.PreviousGrassYears = null;
        //        model.IsPreviousYearGrass = null;
        //        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
        //        if (model.IsCheckAnswer)
        //        {
        //            return RedirectToAction("CheckAnswer");
        //        }
        //        return RedirectToAction("CropGroups");
        //    }

        //}

        //[HttpGet]
        //public async Task<IActionResult> GrassLastThreeHarvestYear()
        //{
        //    _logger.LogTrace($"Field Controller : GrassLastThreeHarvestYear() action called");
        //    Error error = new Error();

        //    FieldViewModel model = new FieldViewModel();
        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }

        //    List<int> previousYears = new List<int>();
        //    int lastHarvestYear = model.LastHarvestYear ?? 0;
        //    previousYears.Add(lastHarvestYear);
        //    previousYears.Add(lastHarvestYear - 1);
        //    previousYears.Add(lastHarvestYear - 2);
        //    ViewBag.PreviousGrassesYear = previousYears;
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult GrassLastThreeHarvestYear(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : GrassLastThreeHarvestYear() post action called");
        //    int lastHarvestYear = 0;
        //    if (model.PreviousGrassYears == null)
        //    {
        //        ModelState.AddModelError("PreviousGrassYears", Resource.lblSelectAtLeastOneYearBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        List<int> previousYears = new List<int>();
        //        lastHarvestYear = model.LastHarvestYear ?? 0;
        //        previousYears.Add(lastHarvestYear);
        //        previousYears.Add(lastHarvestYear - 1);
        //        previousYears.Add(lastHarvestYear - 2);
        //        ViewBag.PreviousGrassesYear = previousYears;
        //        return View(model);
        //    }
        //    //below condition is for select all
        //    if (model.PreviousGrassYears?.Count == 1 && model.PreviousGrassYears[0] == 0)
        //    {
        //        List<int> previousYears = new List<int>();
        //        lastHarvestYear = model.LastHarvestYear ?? 0;
        //        previousYears.Add(lastHarvestYear);
        //        previousYears.Add(lastHarvestYear - 1);
        //        previousYears.Add(lastHarvestYear - 2);
        //        model.PreviousGrassYears = previousYears;
        //    }
        //    lastHarvestYear = model.LastHarvestYear ?? 0;
        //    model.IsPreviousYearGrass = (model.PreviousGrassYears != null && model.PreviousGrassYears.Contains(lastHarvestYear)) ? true : false;

        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

        //    if (model.PreviousGrassYears?.Count == 3)
        //    {
        //        model.PreviousGrasses.LayDuration = (int)NMP.Portal.Enums.LayDuration.ThreeYearsOrMore;
        //    }
        //    else if (model.PreviousGrassYears?.Count <= 2 && model.PreviousGrassYears[0] == model.LastHarvestYear)
        //    {
        //        model.PreviousGrasses.LayDuration = (int)NMP.Portal.Enums.LayDuration.OneToTwoYears;
        //    }
        //    else
        //    {
        //        return RedirectToAction("LayDuration");
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }
        //    return RedirectToAction("GrassManagementOptions");
        //}

        //[HttpGet]
        //public async Task<IActionResult> GrassManagementOptions()
        //{
        //    _logger.LogTrace($"Field Controller : GrassManagementOptions() action called");
        //    Error error = new Error();

        //    FieldViewModel model = new FieldViewModel();
        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
        //    ViewBag.GrassManagementOptions = commonResponses;
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> GrassManagementOptions(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : GrassManagementOptions() post action called");

        //    if (model.PreviousGrasses.GrassManagementOptionID == null)
        //    {
        //        ModelState.AddModelError("PreviousGrasses.GrassManagementOptionID", Resource.MsgSelectAnOptionBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
        //        ViewBag.GrassManagementOptions = commonResponses;
        //        return View(model);
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.PreviousGrasses.GrassManagementOptionID == (int)NMP.Portal.Enums.GrassManagementOption.GrazedOnly)
        //    {
        //        return RedirectToAction("HasGreaterThan30PercentClover");
        //    }
        //    if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }

        //    return RedirectToAction("HasGreaterThan30PercentClover");
        //}



        //[HttpGet]
        //public async Task<IActionResult> HasGreaterThan30PercentClover()
        //{
        //    _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() action called");
        //    Error error = new Error();

        //    FieldViewModel model = new FieldViewModel();
        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult HasGreaterThan30PercentClover(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() post action called");
        //    if (model.PreviousGrasses.HasGreaterThan30PercentClover == null)
        //    {
        //        ModelState.AddModelError("PreviousGrasses.HasGreaterThan30PercentClover", Resource.MsgSelectAnOptionBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }
        //    if (model.PreviousGrasses.HasGreaterThan30PercentClover.Value)
        //    {
        //        if (model.IsPreviousYearGrass == false)
        //        {
        //            return RedirectToAction("CropGroups");
        //        }
        //        return RedirectToAction("CheckAnswer");
        //    }
        //    else
        //    {
        //        return RedirectToAction("SoilNitrogenSupplyItems");
        //    }

        //}

        //[HttpGet]
        //public async Task<IActionResult> SoilNitrogenSupplyItems()
        //{
        //    _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() action called");
        //    Error error = new Error();

        //    FieldViewModel model = new FieldViewModel();
        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
        //    ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SoilNitrogenSupplyItems(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() post action called");

        //    if (model.PreviousGrasses.SoilNitrogenSupplyItemID == null)
        //    {
        //        ModelState.AddModelError("PreviousGrasses.SoilNitrogenSupplyItemID", Resource.MsgSelectAnOptionBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
        //        ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);
        //        return View(model);
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }
        //    if (model.IsPreviousYearGrass == false)
        //    {
        //        return RedirectToAction("CropGroups");
        //    }

        //    return RedirectToAction("CheckAnswer");
        //}

        //[HttpGet]
        //public async Task<IActionResult> LayDuration()
        //{
        //    _logger.LogTrace($"Field Controller : LayDuration() action called");
        //    FieldViewModel model = new FieldViewModel();

        //    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
        //    {
        //        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
        //    }
        //    else
        //    {
        //        return RedirectToAction("FarmList", "Farm");
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> LayDuration(FieldViewModel model)
        //{
        //    _logger.LogTrace($"Field Controller : LayDuration() post action called");

        //    if (model.PreviousGrasses.LayDuration == null)
        //    {
        //        ModelState.AddModelError("PreviousGrasses.LayDuration", Resource.MsgSelectAnOptionBeforeContinuing);
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }
        //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
        //    if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
        //    {
        //        return RedirectToAction("CheckAnswer");
        //    }

        //    return RedirectToAction("GrassManagementOptions");
        //}

    }
}
