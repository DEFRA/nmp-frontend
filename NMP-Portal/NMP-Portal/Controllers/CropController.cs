using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Xml.Linq;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class CropController : Controller
    {
        private readonly ILogger<CropController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IOrganicManureService _organicManureService;
        private readonly IFertiliserManureService _fertiliserManureService;
        private readonly ISnsAnalysisService _snsAnalysisService;

        public CropController(ILogger<CropController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, IFieldService fieldService, ICropService cropService, IOrganicManureService organicManureService,
             IFertiliserManureService fertiliserManureService, ISnsAnalysisService snsAnalysisService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
            _organicManureService = organicManureService;
            _fertiliserManureService = fertiliserManureService;
            _snsAnalysisService = snsAnalysisService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace("Crop Controller : Index() action called");
            return View();
        }

        public async Task<IActionResult> CreateCropPlanCancel(string q)
        {
            _logger.LogTrace($"Crop Controller : CreateCropPlanCancel({q}) action called");
            _httpContextAccessor.HttpContext?.Session.Remove("CropData");
            if (!string.IsNullOrWhiteSpace(q))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                List<PlanSummaryResponse> planSummaryResponse = await _cropService.FetchPlanSummaryByFarmId(farmId, 0);
                if (planSummaryResponse.Count > 0)
                {
                    return RedirectToAction("PlansAndRecordsOverview", "Crop", new { id = q });
                }
            }
            return RedirectToAction("FarmSummary", "Farm", new { Id = q });
        }

        [HttpGet]
        public async Task<IActionResult> HarvestYearForPlan(string q, string? year, bool? isPlanRecord)
        {
            _logger.LogTrace($"Crop Controller : HarvestYearForPlan({q}, {year}, {isPlanRecord}) action called");
            PlanViewModel? model = new PlanViewModel();
            Error? error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(year))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrEmpty(q) && model != null)
                {
                    int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.EncryptedFarmId = q;

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmID);
                    model.IsEnglishRules = farm.EnglishRules;

                    if (!string.IsNullOrWhiteSpace(year))
                    {
                        int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));
                        model.Year = harvestYear;
                        if (isPlanRecord == false || isPlanRecord == null)
                        {
                            model.IsAddAnotherCrop = true;
                        }
                        if (isPlanRecord == true)
                        {
                            model.IsPlanRecord = true;
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("CropGroups");
                    }

                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);


                }
                List<PlanSummaryResponse> planSummaryResponse = await _cropService.FetchPlanSummaryByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), 0);
                if (planSummaryResponse.Count == 0)
                {
                    if (model != null && (model.IsPlanRecord == null || (model.IsPlanRecord != null && !model.IsPlanRecord.Value)))
                    {
                        return RedirectToAction("FarmSummary", "Farm", new { id = model.EncryptedFarmId });
                    }
                }
                if (model != null && model.IsPlanRecord.Value)
                {
                    return RedirectToAction("PlansAndRecordsOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) });
                }
                if (model != null && model.IsAddAnotherCrop)
                {
                    return RedirectToAction("HarvestYearOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Crop Controller: Exception in HarvestYearForPlan() action : {ex.Message}", ex.StackTrace);
                TempData["Error"] = string.Concat(error == null ? "" : error.Message, ex.Message);
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HarvestYearForPlan(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : HarvestYearForPlan() action posted");
            if (model.Year == null)
            {
                ModelState.AddModelError("Year", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblYear.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            if (model.IsCheckAnswer)
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Year = model.Year.Value;
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("CropGroups");
        }

        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            _logger.LogTrace("Crop Controller : CropGroups() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropGroupsList = cropGroups.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                ViewBag.CropGroupList = cropGroupsList.OrderBy(c => c.CropGroupName); ;
                //ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
                if (model.IsCropGroupChange)
                {
                    model.IsCropGroupChange = false;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Crop Controller: Exception in CropGroups() action : {ex.Message}", ex.StackTrace);
                TempData["ErrorOnHarvestYear"] = ex.Message;
                return RedirectToAction("HarvestYearForPlan");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropGroups() action posted");
            try
            {
                if (model.CropGroupId == null)
                {
                    ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                    var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                    var cropGroupsList = cropGroups.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                    ViewBag.CropGroupList = cropGroupsList.OrderBy(c => c.CropGroupName); ;
                    //ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
                    return View(model);
                }

                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    PlanViewModel CropData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                    if (CropData.CropGroupId != model.CropGroupId)
                    {
                        model.CropType = string.Empty;
                        model.CropTypeID = null;
                        model.CropInfo1 = null;
                        model.CropInfo2 = null;
                        model.CropInfo1Name = null;
                        model.CropInfo2Name = null;
                        model.IsCropGroupChange = true;
                    }
                    else if (CropData.CropGroupId == model.CropGroupId && model.IsCheckAnswer && (!model.IsCropGroupChange))
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    model.CropInfo1 = null;
                    model.CropInfo2 = null;
                    model.CropInfo1Name = null;
                    model.CropInfo2Name = null;
                }
                else
                {
                    model.OtherCropName = null;
                }

                if (model.CropGroupId != null)
                {
                    model.CropGroup = await _fieldService.FetchCropGroupById(model.CropGroupId.Value);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Crop Controller: Exception in CropGroups() post action : {ex.Message} : {ex.StackTrace}");
                TempData["CropGroupError"] = ex.Message;
                return View(model);
            }
            if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
            {
                model.CropType = Resource.lblGrass;
                model.CropTypeID = await _cropService.FetchCropTypeByGroupId(model.CropGroupId ?? 0);
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                return RedirectToAction("CropFields");
            }

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            _logger.LogTrace("Crop Controller : CropTypes() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    List<CropTypeResponse> cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                    var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                    var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                    ViewBag.CropTypeList = cropTypeList.OrderBy(c => c.CropType); ;
                }
                model.IsCropTypeChange = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Crop Controller: Exception in CropTypes() action : {ex.Message} : {ex.StackTrace}");
                TempData["CropGroupError"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropTypes() post action called");
            try
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    model.CropTypeID = await _cropService.FetchCropTypeByGroupId(model.CropGroupId ?? 0);
                }
                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Potatoes && model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
                }
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes && model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", Resource.MsgSelectAPotatoVarietyGroup);
                }
                //Other crop validation
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other && model.OtherCropName == null)
                {
                    ModelState.AddModelError("OtherCropName", string.Format(Resource.lblEnterTheCropName, Resource.lblCropType.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other)
                    {
                        List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                        cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                        var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                        ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList().OrderBy(c => c.CropType); ;
                    }
                    return View(model);
                }
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);
                var SelectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();

                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                //Fetch fields allowed for second crop based on first crop
                List<int> fieldsAllowedForSecondCrop = await FetchAllowedFieldsForSecondCrop(harvestYearPlanResponse, model.Year ?? 0, model.CropTypeID ?? 0);

                if (harvestYearPlanResponse.Count() > 0 || SelectListItem.Count == 1)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                    if (SelectListItem.Count == 1)
                    {
                        model.FieldList = new List<string>();
                        model.FieldList.Add(SelectListItem[0].Value);

                        if (model.FieldList.Count > 0)
                        {
                            if (model.Crops == null)
                            {
                                model.Crops = new List<Crop>();
                            }
                            if (model.Crops.Count > 0)
                            {
                                model.Crops.Clear();
                            }
                            int counter = 1;
                            foreach (var field in model.FieldList)
                            {
                                if (int.TryParse(field, out int fieldId))
                                {
                                    var crop = new Crop
                                    {
                                        Year = model.Year.Value,
                                        CropTypeID = model.CropTypeID,
                                        OtherCropName = model.OtherCropName,
                                        FieldID = fieldId,
                                        Variety = model.Variety,
                                        EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                                        CropOrder = fieldsAllowedForSecondCrop.Contains(fieldId) ? 2 : 1
                                    };
                                    counter++;
                                    crop.FieldName = (await _fieldService.FetchFieldByFieldId(fieldId)).Name;

                                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                                    {
                                        PlanViewModel planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");

                                        if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
                                        {
                                            for (int i = 0; i < planViewModel.Crops.Count; i++)
                                            {
                                                if (planViewModel.Crops[i].FieldID == fieldId)
                                                {
                                                    crop.SowingDate = planViewModel.Crops[i].SowingDate;
                                                    crop.Yield = planViewModel.Crops[i].Yield; break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return RedirectToAction("FarmList", "Farm");
                                    }

                                    model.Crops.Add(crop);
                                }
                            }
                        }
                        if (model.IsCheckAnswer && (!model.IsCropGroupChange))
                        {
                            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                            {
                                bool matchFound = false;
                                PlanViewModel planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                                if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
                                {
                                    foreach (var cropList1 in model.Crops)
                                    {
                                        matchFound = planViewModel.Crops.Any(cropList2 => cropList2.FieldID == cropList1.FieldID);
                                        if (!matchFound || model.Crops.Count != planViewModel.Crops.Count)
                                        {
                                            //model.IsCheckAnswer = false;
                                            model.IsAnyChangeInField = true;
                                            break;
                                        }
                                    }
                                    if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ||
                                       model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                                    {
                                        if (model.Crops.Count == 1)
                                        {
                                            model.SowingDateQuestion = model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ? null : model.SowingDateQuestion;
                                            model.YieldQuestion = (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                        }
                                    }
                                    if (matchFound && model.Crops.Count == planViewModel.Crops.Count && (!model.IsAnyChangeInField))
                                    {
                                        model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                        return RedirectToAction("CheckAnswer");
                                    }
                                }
                            }
                            else
                            {
                                return RedirectToAction("FarmList", "Farm");
                            }
                        }

                        model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("CropGroupName");
                    }
                    else
                    {
                        if (model.Crops != null && model.Crops.Count > 0)
                        {
                            foreach (var crop in model.Crops)
                            {
                                if (crop.FieldID != null)
                                {
                                    crop.CropOrder = fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) ? 2 : 1;
                                }
                            }
                        }

                    }
                }
                if (model.IsCheckAnswer)
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                    {
                        PlanViewModel CropData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                        if (CropData.CropTypeID == model.CropTypeID)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                        else
                        {
                            model.CropInfo1 = null;
                            model.CropInfo2 = null;
                            model.CropInfo1Name = null;
                            model.CropInfo2Name = null;
                            model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                            model.IsCropTypeChange = true;
                            for (int i = 0; i < model.Crops.Count; i++)
                            {
                                model.Crops[i].CropTypeID = model.CropTypeID.Value;
                                model.Crops[i].CropInfo1 = null;
                                model.Crops[i].CropInfo2 = null;
                            }
                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                            if (model.IsCropTypeChange)
                            {
                                model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                return RedirectToAction("CropFields");

                            }

                        }
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }

                }

                if (model.CropTypeID != null)
                {
                    model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    if (harvestYearPlanResponse.Count() > 0)
                    {
                        var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                        if (SelectListItem.Count == 0)
                        {
                            TempData["CropTypeError"] = Resource.lblNoFieldsAreAvailable;
                            return RedirectToAction("CropTypes");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Crop Controller: Exception in CropTypes() post action : {ex.Message} : {ex.StackTrace}");
                TempData["CropTypeError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("CropFields");
        }

        [HttpGet]
        public async Task<IActionResult> VarietyName(string? q)
        {
            _logger.LogTrace("Crop Controller : VarietyName() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                else
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                    {
                        ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
                    }
                    if (!string.IsNullOrWhiteSpace(model.CropType))
                    {
                        ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in VarietyName() post action : {ex.Message}, {ex.StackTrace}");

                if (string.IsNullOrWhiteSpace(q))
                {
                    TempData["CropGroupNameError"] = ex.Message;
                    return RedirectToAction("CropGroupName");
                }
                else
                {
                    TempData["ErrorUpdateCropGroupNameCheckAnswer"] = ex.Message;
                    return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = _cropDataProtector.Protect(model.CropGroupName) });
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VarietyName(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : VarietyName() post action called");
            try
            {
                if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && string.IsNullOrWhiteSpace(model.Variety))
                {
                    if (model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup1 ||
                        model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup2 ||
                        model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup3 ||
                        model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup4)
                    {
                        ModelState.AddModelError("Variety", Resource.MsgEnterAPotatoVarietyNameBeforeContinuing);
                    }
                }
                else
                {
                    if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes && model.Variety == null)
                    {
                        ModelState.AddModelError("Variety", Resource.MsgEnterAPotatoVarietyNameBeforeContinuing);
                    }
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    if (!string.IsNullOrWhiteSpace(model.Variety))
                    {
                        for (int i = 0; i < model.Crops.Count; i++)
                        {
                            model.Crops[i].Variety = model.Variety;
                        }
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

                    if (model.IsCheckAnswer)
                    {
                        for (int i = 0; i < model.Crops.Count; i++)
                        {
                            model.Crops[i].Variety = model.Variety;
                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        }
                        if (model.IsCropTypeChange)
                        {
                            return RedirectToAction("CropInfoOne");

                        }
                        if (model.IsAnyChangeInField)
                        {
                            return RedirectToAction("SowingDateQuestion");
                        }
                        return RedirectToAction("CheckAnswer");
                    }
                    return RedirectToAction("SowingDateQuestion");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                    {
                        ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
                    }
                    if (!string.IsNullOrWhiteSpace(model.CropType))
                    {
                        ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
                    }

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                    return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = (!string.IsNullOrWhiteSpace(model.CropGroupName) ? _cropDataProtector.Protect(model.CropGroupName) : string.Empty), s = _cropDataProtector.Protect(Resource.lblTrue) });
                    //return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = _cropDataProtector.Protect(model.CropGroupName), s = _cropDataProtector.Protect(Resource.lblTrue) });
                }



            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in VarietyName() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnVariety"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CropFields()
        {
            _logger.LogTrace("Crop Controller : CropFields() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);
                var SelectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                //Fetch fields allowed for second crop based on first crop
                List<int> fieldsAllowedForSecondCrop = await FetchAllowedFieldsForSecondCrop(harvestYearPlanResponse, model.Year ?? 0, model.CropTypeID ?? 0);

                if (harvestYearPlanResponse.Count() > 0)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                    if (SelectListItem.Count == 1)
                    {
                        return RedirectToAction("CropTypes");
                    }
                }
                ViewBag.fieldList = SelectListItem;
                if (model.IsAnyChangeInField)
                {
                    model.IsAnyChangeInField = false;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropFields() action : {ex.Message}, {ex.StackTrace}");
                TempData["CropTypeError"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropFields(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropFields() post action called");
            try
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);
                var selectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                //Fetch fields allowed for second crop based on first crop
                List<int> fieldsAllowedForSecondCrop = await FetchAllowedFieldsForSecondCrop(harvestYearPlanResponse, model.Year ?? 0, model.CropTypeID ?? 0);

                if (harvestYearPlanResponse.Count() > 0)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();

                }
                if (model.FieldList == null || model.FieldList.Count == 0)
                {
                    ModelState.AddModelError("FieldList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    ViewBag.fieldList = selectListItem;
                    return View(model);
                }
                if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblSelectAll)
                {
                    model.FieldList = selectListItem.Select(item => item.Value).ToList();
                }
                if (model.FieldList.Count > 0)
                {
                    if (model.Crops == null)
                    {
                        model.Crops = new List<Crop>();
                    }
                    if (model.Crops.Count > 0)
                    {
                        model.Crops.Clear();
                    }
                    int counter = 1;
                    foreach (var field in model.FieldList)
                    {
                        if (int.TryParse(field, out int fieldId))
                        {
                            var crop = new Crop
                            {
                                Year = model.Year.Value,
                                CropTypeID = model.CropTypeID,
                                OtherCropName = model.OtherCropName,
                                FieldID = fieldId,
                                Variety = model.Variety,
                                EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                                CropOrder = fieldsAllowedForSecondCrop.Contains(fieldId) ? 2 : 1
                            };
                            counter++;
                            crop.FieldName = (await _fieldService.FetchFieldByFieldId(fieldId)).Name;
                            if (model.CropInfo1.HasValue)
                            {
                                crop.CropInfo1 = model.CropInfo1.Value;
                            }
                            if (model.CropInfo2.HasValue)
                            {
                                crop.CropInfo2 = model.CropInfo2.Value;
                            }
                            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                            {
                                PlanViewModel planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");

                                if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
                                {
                                    for (int i = 0; i < planViewModel.Crops.Count; i++)
                                    {
                                        if (planViewModel.Crops[i].FieldID == fieldId)
                                        {
                                            crop.SowingDate = planViewModel.Crops[i].SowingDate;
                                            crop.Yield = planViewModel.Crops[i].Yield; break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                return RedirectToAction("FarmList", "Farm");
                            }

                            model.Crops.Add(crop);
                        }
                    }
                }
                if (model.IsCheckAnswer && (!model.IsCropGroupChange))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                    {
                        bool matchFound = false;
                        PlanViewModel planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                        if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
                        {
                            foreach (var cropList1 in model.Crops)
                            {
                                matchFound = planViewModel.Crops.Any(cropList2 => cropList2.FieldID == cropList1.FieldID);
                                if (matchFound && model.Crops.Count == 1)
                                {
                                    if (model.SowingDateQuestion != (int)NMP.Portal.Enums.SowingDateQuestion.NoIWillEnterTheDateLater)
                                    {
                                        model.SowingDateQuestion = (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields;
                                    }
                                    model.YieldQuestion = (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                    return RedirectToAction("CheckAnswer");
                                }
                                if (!matchFound || model.Crops.Count != planViewModel.Crops.Count)
                                {
                                    //model.IsCheckAnswer = false;
                                    model.IsAnyChangeInField = true;
                                    break;
                                }

                            }
                            if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ||
                               model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                            {
                                if (model.Crops.Count == 1)
                                {
                                    model.SowingDateQuestion = model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ? null : model.SowingDateQuestion;
                                    model.YieldQuestion = (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                }
                            }
                            if (matchFound && (!model.IsAnyChangeInField))
                            {
                                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                                return RedirectToAction("CheckAnswer");
                            }
                        }
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                return RedirectToAction("CropGroupName");

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropFields() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSelectField"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SowingDateQuestion()
        {
            _logger.LogTrace("Crop Controller : SowingDateQuestion() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model.IsQuestionChange)
                {
                    model.IsQuestionChange = false;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in SowingDateQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSelectField"] = ex.Message;
                return RedirectToAction("CropFields");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SowingDateQuestion(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : SowingDateQuestion() action called");
            if (model.SowingDateQuestion == null)
            {
                ModelState.AddModelError("SowingDateQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                PlanViewModel planViewModel = new PlanViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (planViewModel.SowingDateQuestion == model.SowingDateQuestion && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    model.IsQuestionChange = true;
                    model.SowingDateCurrentCounter = 0;
                }
            }

            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.NoIWillEnterTheDateLater)
            {
                if (model.Crops != null)
                {
                    for (int i = 0; i < model.Crops.Count; i++)
                    {
                        if (model.Crops[i].SowingDate != null)
                        {
                            model.Crops[i].SowingDate = null;
                        }
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
                {
                    return RedirectToAction("SwardType");
                }
                return RedirectToAction("YieldQuestion");
            }
            else
            {
                return RedirectToAction("SowingDate");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SowingDate(string q)
        {
            _logger.LogTrace($"Crop Controller : SowingDate({q}) action called");
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                if (model.SowingDateCurrentCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                    //model.FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[0].FieldId.Value)).Name;
                    //model.Crops[0].EncryptedCounter = _fieldDataProtector.Protect((model.SowingDateCurrentCounter + 1).ToString());
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;//index of list
                if (itemCount == 0)
                {
                    model.SowingDateCurrentCounter = 0;
                    model.SowingDateEncryptedCounter = string.Empty;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("SowingDateQuestion");
                }
                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.SowingDateCurrentCounter = index;
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SowingDate(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : SowingDate() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Crops[" + model.SowingDateCurrentCounter + "].SowingDate"))
            {
                var dateError = ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Count > 0 ?
                                ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors[0].ErrorMessage.ToString() : null;

                //if (dateError != null && dateError.Equals(Resource.MsgDateMustBeARealDate))
                //{
                //    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Clear();
                //    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Add(Resource.MsgTheDateMustInclude);
                //}
                //else 
                if (dateError != null && (dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, "SowingDate")) ||
                    dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, "SowingDate"))))
                {
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Clear();
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }
            if (model.Crops[model.SowingDateCurrentCounter].SowingDate == null)
            {
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", Resource.MsgEnterADateBeforeContinuing);
            }

            //if (model.Crops[model.SowingDateCurrentCounter].SowingDate != null)
            //{
            //    if (model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Year < 1601 || model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Date.Year >= model.Year + 1)
            //    {
            //        ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", Resource.MsgEnterADateAfter);
            //    }
            //}
            bool isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(model.CropTypeID.Value);

            //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
            DateTime maxDate = new DateTime(model.Year.Value, 12, 31);

            if (model.Crops[model.SowingDateCurrentCounter].SowingDate > maxDate)
            {
                //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgPlantingDateAfterHarvestYear, model.Year.Value, maxDate.Date.ToString("dd MMMM yyyy")));
            }

            if (!isPerennial)
            {
                DateTime minDate = new DateTime(model.Year.Value - 1, 01, 01);
                if (model.Crops[model.SowingDateCurrentCounter].SowingDate < minDate)
                {
                    //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
                    ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgPlantingDateBeforeHarvestYear, model.Year.Value, minDate.Date.ToString("dd MMMM yyyy")));
                }
            }
            // Removed by Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​ : If perennial flag is true =  no minimum date validation. Max date = end of calendar
            //else
            //{
            //    if (model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Year < 1601)
            //    {
            //        ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", Resource.MsgEnterADateAfter);
            //    }
            //}


            if (model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.WinterWheat ||
                model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.WinterTriticale ||
                model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.ForageWinterTriticale ||
                model.CropTypeID == (int)NMP.Portal.Enums.CropTypes.WholecropWinterWheat)
            {
                if (model.Crops[model.SowingDateCurrentCounter].SowingDate != null &&
                    model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Month >= 2 && model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Month <= 6)
                {
                    ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgForSowingDate, model.CropType));
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    if (model.FieldID == model.Crops[i].FieldID.Value)
                    {
                        model.SowingDateCurrentCounter++;
                        if (i + 1 < model.Crops.Count)
                        {
                            model.FieldID = model.Crops[i + 1].FieldID.Value;
                        }

                        break;
                    }
                }
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsQuestionChange) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
            }

            else if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
            {
                model.SowingDateCurrentCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].SowingDate = model.Crops[0].SowingDate;
                    //model.Crops[i].EncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                }
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange))
                {
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("CheckAnswer");
                }
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
                {
                    return RedirectToAction("SwardType");
                }
                return RedirectToAction("YieldQuestion");
            }

            if (model.SowingDateCurrentCounter == model.Crops.Count)
            {
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction("CheckAnswer");
                }
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
                {
                    return RedirectToAction("SwardType");
                }
                return RedirectToAction("YieldQuestion");
            }
            else
            {
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> YieldQuestion()
        {
            _logger.LogTrace("Crop Controller : YieldQuestion() action called");
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            ViewBag.DefaultYield = await _cropService.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0);
            if (model.IsQuestionChange)
            {
                model.IsQuestionChange = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            if (model.Crops.Count == 1)
            {
                model.YieldQuestion = (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                return RedirectToAction("Yield");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YieldQuestion(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : YieldQuestion() post action called");
            if (model.YieldQuestion == null)
            {
                ModelState.AddModelError("YieldQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                PlanViewModel planViewModel = new PlanViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    planViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (planViewModel.YieldQuestion == model.YieldQuestion && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    model.IsQuestionChange = true;
                    model.YieldCurrentCounter = 0;
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            return RedirectToAction("Yield");
        }

        [HttpGet]
        public async Task<IActionResult> Yield(string q)
        {
            _logger.LogTrace($"Crop Controller : Yield({q}) action called");
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                if (model.YieldCurrentCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                    //model.Crops[0].EncryptedCounter = _fieldDataProtector.Protect((model.YieldCurrentCounter + 1).ToString());
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;//index of list
                if (itemCount == 0)
                {
                    model.YieldCurrentCounter = 0;
                    model.YieldEncryptedCounter = string.Empty;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("YieldQuestion");

                }
                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.YieldCurrentCounter = index;
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
            }
            if (model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields)
            {
                decimal defaultYield = await _cropService.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0);
                model.YieldCurrentCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Yield = defaultYield;
                }
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yield(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : Yield() post action called");
            if (model.Crops[model.YieldCurrentCounter].Yield == null)
            {
                ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", Resource.MsgEnterFigureBeforeContinuing);
            }
            if (model.Crops[model.YieldCurrentCounter].Yield > Convert.ToInt32(Resource.lblFiveDigit))
            {
                ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", Resource.MsgEnterAValueOfNoMoreThan5Digits);
            }
            if (model.Crops[model.YieldCurrentCounter].Yield < 0)
            {
                ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblYield));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    if (model.FieldID == model.Crops[i].FieldID.Value)
                    {
                        model.YieldCurrentCounter++;
                        if (i + 1 < model.Crops.Count)
                        {
                            model.FieldID = model.Crops[i + 1].FieldID.Value;
                            //model.Crops[i + 1].EncryptedCounter = _fieldDataProtector.Protect((model.YieldCurrentCounter + 1).ToString());
                        }

                        break;
                    }
                }
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsQuestionChange) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            else if (model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
            {
                model.YieldCurrentCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Yield = model.Crops[0].Yield;
                }
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }

            if (model.YieldCurrentCounter == model.Crops.Count)
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }
            else
            {
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CropInfoOne()
        {
            _logger.LogTrace("Crop Controller : CropInfoOne() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0);
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropInfoOneList = cropInfoOneResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                ViewBag.CropInfoOneList = cropInfoOneList.OrderBy(c => c.CropInfo1Name);

                string? cropInfoOneQuestion = await _cropService.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
                ViewBag.CropInfoOneQuestion = cropInfoOneQuestion;
                if (cropInfoOneQuestion == null)
                {
                    model.CropInfo1Name = cropInfoOneList.FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone).CropInfo1Name;
                    model.CropInfo1 = cropInfoOneList.FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone).CropInfo1Id;

                    for (int i = 0; i < model.Crops.Count; i++)
                    {
                        model.Crops[i].CropInfo1 = model.CropInfo1;
                    }

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals)
                    {
                        return RedirectToAction("CropInfoTwo");
                    }
                    else
                    {
                        model.IsCropTypeChange = false;
                        model.IsCropGroupChange = false;
                        model.CropInfo2 = null;
                        model.CropInfo2Name = null;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("CheckAnswer");
                    }
                }


            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropInfoOne() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnYield"] = ex.Message;
                return RedirectToAction("Yield");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropInfoOne(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropInfoOne() post action called");
            try
            {
                List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0);
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropInfoOneList = cropInfoOneResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                if (model.CropInfo1 == null)
                {
                    ModelState.AddModelError("CropInfo1", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {

                    ViewBag.CropInfoOneList = cropInfoOneList.OrderBy(c => c.CropInfo1Name);
                    return View(model);
                }
                model.CropInfo1Name = cropInfoOneList.FirstOrDefault(x => x.CropInfo1Id == model.CropInfo1).CropInfo1Name;

                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].CropInfo1 = model.CropInfo1;
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropInfoOne() post action : {ex.Message}, {ex.StackTrace}");
                TempData["CropInfoOneError"] = ex.Message;
                return RedirectToAction("CropInfoOne");
            }

            if (model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals)
            {
                return RedirectToAction("CropInfoTwo");
            }
            else
            {
                model.IsCropTypeChange = false;
                model.IsCropGroupChange = false;
                model.CropInfo2 = null;
                model.CropInfo2Name = null;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                return RedirectToAction("CheckAnswer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AnotherCrop()
        {
            _logger.LogTrace("Crop Controller : AnotherCrop() action called");
            PlanViewModel model = new PlanViewModel();

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnotherCrop(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : AnotherCrop() post action called");
            //need to revisit for this functionality
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CropInfoTwo()
        {
            _logger.LogTrace("Crop Controller : CropInfoTwo() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                List<CropInfoTwoResponse> cropInfoTwoResponse = await _cropService.FetchCropInfoTwoByCropTypeId();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropInfoTwoList = cropInfoTwoResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                ViewBag.CropInfoTwoList = cropInfoTwoList.OrderBy(c => c.CropInfo2);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropInfoTwo() action : {ex.Message}, {ex.StackTrace}");
                TempData["CropInfoOneError"] = ex.Message;
                return RedirectToAction("CropInfoOne");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropInfoTwo(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropInfoTwo() post action called");
            try
            {
                List<CropInfoTwoResponse> cropInfoTwoResponse = await _cropService.FetchCropInfoTwoByCropTypeId();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropInfoTwoList = cropInfoTwoResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
                if (model.CropInfo2 == null)
                {
                    ModelState.AddModelError("CropInfo2", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.CropInfoTwoList = cropInfoTwoList.OrderBy(c => c.CropInfo2);
                    return View(model);
                }
                model.CropInfo2Name = cropInfoTwoList.FirstOrDefault(x => x.CropInfo2Id == model.CropInfo2).CropInfo2;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].CropInfo2 = model.CropInfo2;
                }
                model.IsCropTypeChange = false;
                model.IsCropGroupChange = false;

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropInfoTwo() post action : {ex.Message}, {ex.StackTrace}");
                TempData["CropInfoTwoError"] = ex.Message;
                return RedirectToAction("CropInfoTwo");
            }

            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace("Crop Controller : CheckAnswer() action called");
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");

                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);

                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                //Fetch fields allowed for second crop based on first crop
                List<int> fieldsAllowedForSecondCrop = await FetchAllowedFieldsForSecondCrop(harvestYearPlanResponse, model.Year ?? 0, model.CropTypeID ?? 0);

                if (harvestYearPlanResponse.Count() > 0 || fieldsAllowedForSecondCrop.Count() > 0)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString()) || fieldsAllowedForSecondCrop.Contains(x.ID ?? 0)).ToList();
                }
                ViewBag.FieldOptions = fieldList;
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = true;
            ViewBag.DefaultYield = await _cropService.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0);
            string? cropInfoOneQuestion = await _cropService.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
            ViewBag.CropInfoOneQuestion = cropInfoOneQuestion;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            return View(model);
        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace("Crop Controller : BackCheckAnswer() action called");
            PlanViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            //string action = model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other ? "Yield" : model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals ?
            //    "CropInfoTwo" : "CropInfoOne";
            string action = "YieldQuestion";
            try
            {
                List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0);
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                List<CropInfoOneResponse> cropInfoOneList = cropInfoOneResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();

                action = model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals ?
                   "CropInfoTwo" : (((model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                   || cropInfoOneList.Count == 1) ?
                   ((model.YieldQuestion != (int)NMP.Portal.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields) ?
               "Yield" : "YieldQuestion") : "CropInfoOne");


                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals)
                {
                    action = "CropInfoTwo";
                }
                else if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
                {
                    if (model.GrassGrowthClassDistinctCount == 1)
                    {
                        action = "DryMatterYield";
                    }
                    else
                    {
                        action = "GrassGrowthClass";
                    }
                }
                else if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other || cropInfoOneList.Count == 1)
                {
                    action = model.YieldQuestion != (int)NMP.Portal.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields
                        ? "Yield"
                        : "YieldQuestion";
                }
                else
                {
                    action = "CropInfoOne";
                }
                model.IsCheckAnswer = false;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in BackCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorCreatePlan"] = ex.Message;
                return View("CheckAnswer", model);
            }
            string encryptedCounter = string.Empty;
            if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
            {
                if(model.GrassGrowthClassQuestion != null)
                {
                    encryptedCounter = model.DryMatterYieldEncryptedCounter;
                }
                else
                {
                    encryptedCounter = model.GrassGrowthClassEncryptedCounter;
                }
            }
            else
            {
                encryptedCounter = model.YieldEncryptedCounter;
            }
            return RedirectToAction(action, new { q = encryptedCounter });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CheckAnswer() post action called");
            if (model != null)
            {
                int i = 0;
                int otherGroupId = (int)NMP.Portal.Enums.CropGroup.Other;
                int cerealsGroupId = (int)NMP.Portal.Enums.CropGroup.Cereals;
                int potatoesGroupId = (int)NMP.Portal.Enums.CropGroup.Potatoes;
                foreach (var crop in model.Crops)
                {
                    if (crop.SowingDate == null)
                    {
                        if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
                        {
                            ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingSingleDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                            break;
                        }
                        else if (model.SowingDateQuestion == (int)NMP.Portal.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
                        {
                            ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingDiffrentDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                        }
                    }
                    i++;
                }
                i = 0;
                foreach (var crop in model.Crops)
                {
                    if (crop.Yield == null)
                    {
                        if (model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                        {
                            ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheExpectedYieldForSingleNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                            break;
                        }
                        else if (model.YieldQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                        {
                            ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheDifferentExpectedYieldNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                        }
                    }
                    i++;
                }
                if (string.IsNullOrWhiteSpace(model.Variety) && model.CropGroupId == potatoesGroupId)
                {
                    ModelState.AddModelError("Variety", Resource.MsgVarietyNameNotSet);
                }
                if (model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", Resource.MsgMainCropTypeNotSet);
                }
                if (model.CropInfo1 == null && model.CropGroupId != otherGroupId)
                {
                    ModelState.AddModelError("CropInfo1", string.Format(Resource.MsgCropInfo1NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                }
                if (model.CropInfo2 == null && model.CropGroupId == cerealsGroupId)
                {
                    ModelState.AddModelError("CropInfo2", string.Format(Resource.MsgCropInfo2NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                }

            }
            if (!ModelState.IsValid)
            {
                return View("CheckAnswer", model);
            }


            Error error = null;
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);

            //var lastGroup = (await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFarmId))))
            //                    .OrderByDescending(cg => cg.CropGroupName)
            //                    .FirstOrDefault();
            int? lastGroupNumber = null;
            if (string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (harvestYearPlanResponse != null && error.Message == null)
                {
                    var lastGroup = harvestYearPlanResponse.Where(cg => !string.IsNullOrEmpty(cg.CropGroupName) && cg.CropGroupName.StartsWith("Crop group") &&
                                     int.TryParse(cg.CropGroupName.Split(' ')[2], out _))
                                    .OrderByDescending(cg => int.Parse(cg.CropGroupName.Split(' ')[2]))
                                    .FirstOrDefault();
                    if (lastGroup != null)
                    {
                        lastGroupNumber = int.Parse(lastGroup.CropGroupName.Split(' ')[2]);
                    }
                }
            }
            List<CropData> cropEntries = new List<CropData>();
            foreach (Crop crop in model.Crops)
            {
                crop.CreatedOn = DateTime.Now;
                crop.CreatedByID = userId;
                crop.FieldName = null;
                crop.EncryptedCounter = null;
                crop.FieldType = model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass ? (int)NMP.Portal.Enums.FieldType.Grass : (int)NMP.Portal.Enums.FieldType.Arable;
                //crop.CropOrder = 1;
                if (string.IsNullOrWhiteSpace(model.CropGroupName))
                {
                    if (lastGroupNumber != null)
                    {
                        crop.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, (lastGroupNumber + 1));
                    }
                    else
                    {
                        crop.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, 1);
                    }
                }
                else
                {
                    crop.CropGroupName = model.CropGroupName;
                }
                CropData cropEntry = new CropData
                {
                    Crop = crop,
                    ManagementPeriods = new List<ManagementPeriod>
                    {
                        new ManagementPeriod
                        {
                            DefoliationID=1,
                            Utilisation1ID=2,
                            CreatedOn=DateTime.Now,
                            CreatedByID=userId
                        }
                    }
                };
                cropEntries.Add(cropEntry);

            }
            CropDataWrapper cropDataWrapper = new CropDataWrapper
            {
                Crops = cropEntries
            };
            (bool success, error) = await _cropService.AddCropNutrientManagementPlan(cropDataWrapper);
            if (error.Message == null && success)
            {
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                _httpContextAccessor.HttpContext?.Session.Remove("CropData");
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear,
                    q = _farmDataProtector.Protect(success.ToString()),
                    r = _cropDataProtector.Protect(Resource.lblPlanCreated)
                });
            }
            else
            {
                TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater; //error.Message; //
                return RedirectToAction("CheckAnswer");
            }
        }
        [HttpGet]
        public async Task<IActionResult> HarvestYearOverview(string id, string year, string? q, string? r, string? s, string? t, string? u, string? v, string? w)//w is a link
        {
            _logger.LogTrace($"Crop Controller : HarvestYearOverview({id}, {year}, {q}, {r}) action called");
            PlanViewModel? model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                {
                    _httpContextAccessor.HttpContext?.Session.Remove("FertiliserManure");
                }
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    _httpContextAccessor.HttpContext?.Session.Remove("OrganicManure");
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        TempData["successMsg"] = _cropDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(v))
                        {
                            TempData["successMsgSecond"] = _cropDataProtector.Unprotect(v);
                        }
                        if (!string.IsNullOrWhiteSpace(w))
                        {
                            int decryptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(w));
                            if (decryptedFieldId > 0)
                            {
                                Field field = await _fieldService.FetchFieldByFieldId(decryptedFieldId);
                                if (field != null)
                                {
                                    TempData["fieldName"] = field.Name;
                                }
                            }
                            TempData["successMsgLink"] = w;
                        }
                    }
                    ViewBag.Success = true;
                }
                else
                {
                    ViewBag.Success = false;
                    _httpContextAccessor.HttpContext?.Session.Remove("CropData");

                }

                if (string.IsNullOrWhiteSpace(s) && string.IsNullOrWhiteSpace(u))
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        model = new PlanViewModel();
                        int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                        int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));

                        (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                        if (farm != null)
                        {
                            model.FarmName = farm.Name;
                        }
                        (ExcessRainfalls excessRainfalls, error) = await _farmService.FetchExcessRainfallsAsync(farmId, harvestYear);
                        if (!string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["ErrorOnHarvestYearOverview"] = error.Message;
                            return View("HarvestYearOverview", model);
                        }
                        else
                        {
                            if (excessRainfalls != null && excessRainfalls.WinterRainfall != null)
                            {
                                model.ExcessWinterRainfallValue = excessRainfalls.WinterRainfall.Value;
                                model.AnnualRainfall = excessRainfalls.WinterRainfall.Value;
                                model.IsExcessWinterRainfallUpdated = true;
                                (List<CommonResponse> excessWinterRainfallOption, error) = await _farmService.FetchExcessWinterRainfallOptionAsync();
                                if (string.IsNullOrWhiteSpace(error.Message) && excessWinterRainfallOption != null && excessWinterRainfallOption.Count > 0)
                                {
                                    string excessRainfallName = (excessWinterRainfallOption.FirstOrDefault(x => x.Value == model.ExcessWinterRainfallValue)).Name;
                                    string[] parts = excessRainfallName.Split(new string[] { " - " }, StringSplitOptions.None);
                                    model.ExcessWinterRainfallName = $"{parts[0]} ({parts[1]})";
                                    model.ExcessWinterRainfallId = (excessWinterRainfallOption.FirstOrDefault(x => x.Value == model.ExcessWinterRainfallValue)).Id;
                                }

                                ViewBag.ExcessRainfallContentFirst = string.Format(Resource.lblExcessWinterRainfallWithValue, model.ExcessWinterRainfallName);
                                ViewBag.ExcessRainfallContentSecond = Resource.lblUpdateExcessWinterRainfall;
                            }
                            else
                            {
                                model.AnnualRainfall = farm.Rainfall.Value;
                                model.IsExcessWinterRainfallUpdated = false;
                                ViewBag.ExcessRainfallContentFirst = Resource.lblYouHaveNotEnteredAnyExcessWinterRainfall;
                                ViewBag.ExcessRainfallContentSecond = string.Format(Resource.lblAddExcessWinterRainfallForHarvestYear, harvestYear);
                            }
                        }

                        List<string> fields = new List<string>();

                        (HarvestYearResponseHeader harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansDetailsByFarmId(harvestYear, farmId);
                        model.Year = harvestYear;
                        if (harvestYearPlanResponse != null && error.Message == null)
                        {
                            List<CropDetailResponse> allCropDetails = harvestYearPlanResponse.CropDetails ?? new List<CropDetailResponse>().ToList();
                            if (allCropDetails != null)
                            {
                                model.LastModifiedOn = allCropDetails.Max(x => x.LastModifiedOn.Value.ToString("dd MMM yyyy"));
                                var groupedResult = allCropDetails
                                .GroupBy(crop => new { crop.CropTypeName, crop.CropGroupName })
                                .Select(g => new
                                {
                                    CropTypeName = g.Key.CropTypeName,
                                    CropGroupName = g.Key.CropGroupName,
                                    HarvestPlans = g.ToList()
                                })
                                .OrderBy(g => g.CropTypeName);
                                model.FieldCount = allCropDetails.Select(h => h.FieldID).Distinct().Count();
                                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmId);
                                bool isSecondCropAllowed = await IsSecondCropAllowed(allCropDetails);
                                if (harvestYearPlanResponse.CropDetails.Count() > 0)
                                {
                                    var harvestFieldIds = allCropDetails.Select(x => x.FieldID.ToString()).ToList();
                                    fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString())).ToList();
                                    if (fieldList.Count > 0)
                                    {
                                        ViewBag.PendingField = true;
                                    }
                                    else
                                    {
                                        ViewBag.PendingField = isSecondCropAllowed;
                                    }
                                }
                                model.AnnualRainfall = harvestYearPlanResponse.farmDetails.Rainfall;
                                var harvestYearPlans = new HarvestYearPlans
                                {

                                    FieldData = new List<HarvestYearPlanFields>(),
                                    OrganicManureList = new List<OrganicManureResponse>(),
                                    InorganicFertiliserList = new List<InorganicFertiliserResponse>(),
                                };
                                foreach (var group in groupedResult)
                                {
                                    var newField = new HarvestYearPlanFields
                                    {
                                        CropTypeName = group.CropTypeName,
                                        CropGroupName = group.CropGroupName,
                                        EncryptedCropTypeName = _cropDataProtector.Protect((group.CropTypeName)),
                                        EncryptedCropGroupName = string.IsNullOrWhiteSpace(group.CropGroupName) ? null : _cropDataProtector.Protect((group.CropGroupName)),
                                        FieldData = new List<FieldDetails>()
                                    };
                                    foreach (var plan in group.HarvestPlans)
                                    {

                                        var fieldDetail = new FieldDetails
                                        {
                                            EncryptedFieldId = _fieldDataProtector.Protect(plan.FieldID.ToString()), // Encrypt field ID
                                            FieldName = plan.FieldName,
                                            PlantingDate = plan.PlantingDate,
                                            Yield = plan.Yield,
                                            Variety = plan.CropVariety
                                        };

                                        newField.FieldData.Add(fieldDetail);

                                    }
                                    harvestYearPlans.FieldData.Add(newField);
                                }

                                if (harvestYearPlanResponse.OrganicMaterial.Count > 0)
                                {
                                    harvestYearPlans.OrganicManureList = harvestYearPlanResponse.OrganicMaterial.OrderByDescending(x => x.ApplicationDate).ToList();
                                    harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedId = _cropDataProtector.Protect(m.ID.ToString()));
                                    ViewBag.Organic = _cropDataProtector.Protect(Resource.lblOrganic);
                                    harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));
                                }


                                if (harvestYearPlanResponse.InorganicFertiliserApplication.Count > 0)
                                {
                                    harvestYearPlans.InorganicFertiliserList = harvestYearPlanResponse.InorganicFertiliserApplication.OrderByDescending(x => x.ApplicationDate).ToList();
                                    harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFertId = _cropDataProtector.Protect(m.ID.ToString()));
                                    ViewBag.Fertliser = _cropDataProtector.Protect(Resource.lblFertiliser);
                                    harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));

                                }



                                model.encryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                model.encryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                model.sortInOrganicListOrderByDate = Resource.lblDesc;
                                model.sortOrganicListOrderByDate = Resource.lblDesc;
                                model.encryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                model.encryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                model.SortInOrganicListOrderByFieldName = null;
                                model.SortOrganicListOrderByFieldName = null;
                                ViewBag.InOrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                                ViewBag.InOrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                                ViewBag.OrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                                ViewBag.OrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                                model.HarvestYearPlans = harvestYearPlans;
                                //model.HarvestYearPlans
                                model.EncryptedFarmId = id;
                                model.EncryptedHarvestYear = year;
                                model.Year = harvestYear;

                            }
                            else
                            {
                                TempData["ErrorOnHarvestYearOverview"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;//error.Message; //
                                model = null;
                            }
                        }
                        //}
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                    }
                }
                else
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if (model != null)
                    {
                        if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(u))
                        {
                            string decrypSortBy = _cropDataProtector.Unprotect(s);
                            string decrypOrder = _cropDataProtector.Unprotect(u);
                            if (!string.IsNullOrWhiteSpace(decrypSortBy) && !string.IsNullOrWhiteSpace(decrypOrder))
                            {
                                if (!string.IsNullOrWhiteSpace(t))
                                {
                                    string decryptTabName = _cropDataProtector.Unprotect(t);
                                    if (!string.IsNullOrWhiteSpace(decryptTabName))
                                    {
                                        if (decryptTabName == Resource.lblOrganicMaterialApplicationsForSorting && model.HarvestYearPlans.OrganicManureList != null)
                                        {
                                            if (decrypOrder == Resource.lblDesc)
                                            {
                                                model.encryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblField);
                                                model.encryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDate);
                                                if (decrypSortBy == Resource.lblField)
                                                {
                                                    model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderByDescending(x => x.Field).ToList();
                                                    model.encryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                                    model.SortOrganicListOrderByFieldName = Resource.lblDesc;
                                                }
                                                else if (decrypSortBy == Resource.lblDate)
                                                {
                                                    model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderByDescending(x => x.ApplicationDate).ToList();
                                                    model.encryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                                    model.sortOrganicListOrderByDate = Resource.lblDesc;

                                                }
                                            }
                                            else
                                            {
                                                if (decrypSortBy == Resource.lblField)
                                                {
                                                    model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderBy(x => x.Field).ToList();
                                                    model.encryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblAsc);
                                                    model.SortOrganicListOrderByFieldName = Resource.lblAsc;
                                                }
                                                else if (decrypSortBy == Resource.lblDate)
                                                {
                                                    model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderBy(x => x.ApplicationDate).ToList();
                                                    model.encryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblAsc);
                                                    model.sortOrganicListOrderByDate = Resource.lblAsc;

                                                }
                                            }
                                        }
                                        else if (decryptTabName == Resource.lblInorganicFertiliserApplicationsForSorting && model.HarvestYearPlans.InorganicFertiliserList != null)
                                        {
                                            if (decrypOrder == Resource.lblDesc)
                                            {
                                                if (decrypSortBy == Resource.lblField)
                                                {
                                                    model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderByDescending(x => x.Field).ToList();
                                                    model.encryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                                    model.SortInOrganicListOrderByFieldName = Resource.lblDesc;
                                                }
                                                else if (decrypSortBy == Resource.lblDate)
                                                {
                                                    model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderByDescending(x => x.ApplicationDate).ToList();
                                                    model.encryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                                    model.sortInOrganicListOrderByDate = Resource.lblDesc;

                                                }
                                            }
                                            else
                                            {
                                                if (decrypSortBy == Resource.lblField)
                                                {
                                                    model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderBy(x => x.Field).ToList();
                                                    model.encryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblAsc);
                                                    model.SortInOrganicListOrderByFieldName = Resource.lblAsc;
                                                }
                                                else if (decrypSortBy == Resource.lblDate)
                                                {
                                                    model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderBy(x => x.ApplicationDate).ToList();
                                                    model.encryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblAsc);
                                                    model.sortInOrganicListOrderByDate = Resource.lblAsc;

                                                }
                                            }
                                        }
                                    }
                                }

                            }
                        }
                        else
                        {
                            model.encryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                            model.sortOrganicListOrderByDate = Resource.lblDesc;
                            model.sortInOrganicListOrderByDate = Resource.lblDesc;
                            model.encryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                            model.encryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                            model.encryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                            model.SortOrganicListOrderByFieldName = Resource.lblDesc;
                            model.SortInOrganicListOrderByFieldName = Resource.lblDesc;
                        }
                        ViewBag.InOrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                        ViewBag.InOrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                        ViewBag.OrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                        ViewBag.OrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);

                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                    }
                }
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    _httpContextAccessor.HttpContext?.Session.Remove("ReportData");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in HarvestYearOverview() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                model = null;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HarvestYearOverview(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : HarvestYearOverview() post action called");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PlansAndRecordsOverview(string id, string? year, string? q)
        {
            _logger.LogTrace($"Crop Controller : PlansAndRecordsOverview({id}, {year}) action called");
            PlanViewModel model = new PlanViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                TempData["successMsg"] = _cropDataProtector.Unprotect(q);
                ViewBag.Success = true;
            }
            if (!string.IsNullOrWhiteSpace(id))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                model.FarmName = farm.Name;
                List<PlanSummaryResponse> planSummaryResponse = await _cropService.FetchPlanSummaryByFarmId(farmId, 0);
                planSummaryResponse.RemoveAll(x => x.Year == 0);
                planSummaryResponse = planSummaryResponse.OrderByDescending(x => x.Year).ToList();
                model.EncryptedHarvestYearList = new List<string>();
                foreach (var planSummary in planSummaryResponse)
                {
                    model.EncryptedHarvestYearList.Add(_farmDataProtector.Protect(planSummary.Year.ToString()));
                }
                if (!string.IsNullOrWhiteSpace(year))
                {
                    model.EncryptedHarvestYear = year;

                }
                ViewBag.PlanSummaryList = planSummaryResponse;

                //To show the list Create Plan for year (2023,2024,..) 
                List<int> yearList = new List<int>();
                if (planSummaryResponse != null && planSummaryResponse.Count > 0)
                {
                    foreach (var item in planSummaryResponse)
                    {
                        yearList.Add(item.Year);
                    }
                    for (int j = 0; j < planSummaryResponse.Count; j++)
                    {
                        var harvestNewYear = new HarvestYear
                        {
                            Year = planSummaryResponse[j].Year,
                            EncryptedYear = _farmDataProtector.Protect(planSummaryResponse[j].Year.ToString()),
                            LastModifiedOn = planSummaryResponse[j].LastModifiedOn,
                            IsAnyPlan = true
                        };
                        model.HarvestYear.Add(harvestNewYear);
                    }
                    int minYear = planSummaryResponse.Min(x => x.Year) - 1;
                    int maxYear = planSummaryResponse.Max(x => x.Year) + 1;
                    for (int i = minYear; i <= maxYear; i++)
                    {
                        if (!yearList.Contains(i))
                        {
                            var harvestYear = new HarvestYear
                            {
                                Year = i,
                                EncryptedYear = _farmDataProtector.Protect(i.ToString()),
                                IsAnyPlan = false
                            };
                            model.HarvestYear.Add(harvestYear);
                        }
                    }
                }
                if (model.HarvestYear.Count > 0)
                {
                    model.HarvestYear = model.HarvestYear.OrderByDescending(x => x.Year).ToList();
                }
                else
                {
                    return RedirectToAction("HarvestYearForPlan", new { q = id, year = _farmDataProtector.Protect(farm.LastHarvestYear.ToString()), isPlanRecord = false });
                }
                model.EncryptedFarmId = id;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlansAndRecordsOverview(PlanViewModel model)
        {
            _logger.LogTrace($"Crop Controller : PlansAndRecordsOverview() post action called");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Recommendations(string q, string r, string? s, string? t, string? u, string? sns)//q=farmId,r=fieldId,s=harvestYear
        {
            _logger.LogTrace($"Crop Controller : Recommendations({q}, {r}, {s}) action called");
            RecommendationViewModel model = new RecommendationViewModel();
            Error error = null;
            int decryptedFarmId = 0;
            int decryptedFieldId = 0;
            int decryptedHarvestYear = 0;
            List<RecommendationHeader> recommendations = null;
            List<Crop> crops = null;
            try
            {
                //string q, 
                if (!string.IsNullOrWhiteSpace(t))
                {
                    ViewBag.Success = true;
                    TempData["successMsg"] = _cropDataProtector.Unprotect(t);
                    if (!string.IsNullOrWhiteSpace(u))
                    {
                        TempData["successMsgSecond"] = _cropDataProtector.Unprotect(u);
                    }
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.FarmName = (await _farmService.FetchFarmByIdAsync(decryptedFarmId)).Item1.Name;
                    model.EncryptedFarmId = q;
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    decryptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(r));
                    model.EncryptedFieldId = r;
                }
                if (!string.IsNullOrWhiteSpace(s))
                {
                    decryptedHarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
                    model.EncryptedHarvestYear = s;
                }

                if (!string.IsNullOrWhiteSpace(sns))
                {
                    TempData["successSnsAnalysis"] = _cropDataProtector.Unprotect(sns);
                }

                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(decryptedHarvestYear, decryptedFarmId);
                if (harvestYearPlanResponse != null && error.Message == null)
                {
                    bool isAllCropInfo1NonNull = harvestYearPlanResponse.All(h => h.CropInfo1 != null);
                    if (!isAllCropInfo1NonNull)
                    {
                        ViewBag.AddMannerDisabled = true;
                    }
                    if (decryptedFieldId > 0 && decryptedHarvestYear > 0)
                    {
                        (recommendations, error) = await _cropService.FetchRecommendationByFieldIdAndYear(decryptedFieldId, decryptedHarvestYear);
                        if (error == null)
                        {
                            ViewBag.isComingFromRecommendation = _cropDataProtector.Protect(Resource.lblFalse.ToString());
                            if (model.Crops == null)
                            {
                                model.Crops = new List<CropViewModel>();
                            }
                            if (model.ManagementPeriods == null)
                            {
                                model.ManagementPeriods = new List<ManagementPeriod>();
                            }
                            if (model.Recommendations == null)
                            {
                                model.Recommendations = new List<Recommendation>();
                            }
                            if (model.RecommendationComments == null)
                            {
                                model.RecommendationComments = new List<RecommendationComment>();
                            }
                            if (model.OrganicManures == null)
                            {
                                model.OrganicManures = new List<OrganicManureData>();
                            }
                            if (model.FertiliserManures == null)
                            {
                                model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                            }
                            foreach (var recommendation in recommendations)
                            {
                                //check sns already exist or not in SnsAnalyses table by cropID
                                SnsAnalysis snsData = await _snsAnalysisService.FetchSnsAnalysisByCropIdAsync(recommendation.Crops.ID ?? 0);


                                var crop = new CropViewModel
                                {
                                    ID = recommendation.Crops.ID,
                                    EncryptedCropId = _cropDataProtector.Protect(recommendation.Crops.ID.ToString()),
                                    Year = recommendation.Crops.Year,
                                    CropTypeID = recommendation.Crops.CropTypeID,
                                    FieldID = recommendation.Crops.FieldID,
                                    EncryptedFieldId = _fieldDataProtector.Protect(recommendation.Crops.FieldID.ToString()),
                                    Variety = recommendation.Crops.Variety,
                                    CropInfo1 = recommendation.Crops.CropInfo1,
                                    CropInfo2 = recommendation.Crops.CropInfo2,
                                    Yield = recommendation.Crops.Yield,
                                    SowingDate = recommendation.Crops.SowingDate,
                                    OtherCropName = recommendation.Crops.OtherCropName,
                                    CropTypeName = await _fieldService.FetchCropTypeById(recommendation.Crops.CropTypeID.Value),
                                    IsSnsExist = (snsData.CropID != null && snsData.CropID > 0) ? true : false,
                                    SnsAnalysisData = snsData

                                };
                                if (!string.IsNullOrWhiteSpace(crop.CropTypeName))
                                {
                                    crop.EncryptedCropTypeName = _cropDataProtector.Protect(crop.CropTypeName);
                                }
                                if (!string.IsNullOrWhiteSpace(recommendation.Crops.CropOrder.ToString()))
                                {
                                    crop.EncryptedCropOrder = _cropDataProtector.Protect(recommendation.Crops.CropOrder.ToString());
                                }

                                if (recommendation.Crops.CropInfo1 != null)
                                {
                                    crop.CropInfo1Name = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(recommendation.Crops.CropTypeID.Value, recommendation.Crops.CropInfo1.Value);
                                }
                                model.FieldName = (await _fieldService.FetchFieldByFieldId(recommendation.Crops.FieldID.Value)).Name;
                                if (!string.IsNullOrWhiteSpace(model.FieldName))
                                {
                                    crop.EncryptedFieldName = _cropDataProtector.Protect(model.FieldName);
                                }
                                List<CropTypeResponse> cropTypeResponseList = (await _fieldService.FetchAllCropTypes());
                                if (cropTypeResponseList != null)
                                {
                                    CropTypeResponse cropTypeResponse = cropTypeResponseList.Where(x => x.CropTypeId == crop.CropTypeID).FirstOrDefault();
                                    if (cropTypeResponse != null)
                                    {
                                        model.CropGroupID = cropTypeResponse.CropGroupId;
                                    }
                                }
                                if (recommendation.Crops.CropInfo2 != null && model.CropGroupID == (int)NMP.Portal.Enums.CropGroup.Cereals)
                                {
                                    crop.CropInfo2Name = await _cropService.FetchCropInfo2NameByCropInfo2Id(crop.CropInfo2.Value);
                                }

                                model.Crops.Add(crop);

                                if (recommendation.PKBalance != null)
                                {
                                    model.PKBalance = new PKBalance();
                                    model.PKBalance.PBalance = recommendation.PKBalance.PBalance;
                                    model.PKBalance.KBalance = recommendation.PKBalance.KBalance;

                                }
                                if (recommendation.RecommendationData.Count > 0)
                                {
                                    foreach (var recData in recommendation.RecommendationData)
                                    {
                                        var ManagementPeriods = new ManagementPeriod
                                        {
                                            ID = recData.ManagementPeriod.ID,
                                            CropID = recData.ManagementPeriod.CropID,
                                            DefoliationID = recData.ManagementPeriod.DefoliationID,
                                            Utilisation1ID = recData.ManagementPeriod.Utilisation1ID,
                                            Utilisation2ID = recData.ManagementPeriod.Utilisation2ID,
                                            PloughedDown = recData.ManagementPeriod.PloughedDown
                                        };
                                        model.ManagementPeriods.Add(ManagementPeriods);
                                        var rec = new Recommendation
                                        {
                                            ID = recData.Recommendation.ID,
                                            ManagementPeriodID = recData.Recommendation.ManagementPeriodID,
                                            CropN = recData.Recommendation.CropN,
                                            CropP2O5 = recData.Recommendation.CropP2O5,
                                            CropK2O = recData.Recommendation.CropK2O,
                                            CropSO3 = recData.Recommendation.CropSO3,
                                            CropMgO = recData.Recommendation.CropMgO,
                                            CropLime = (recData.Recommendation.PreviousAppliedLime != null && recData.Recommendation.PreviousAppliedLime > 0) ? recData.Recommendation.PreviousAppliedLime : recData.Recommendation.CropLime,
                                            ManureN = recData.Recommendation.ManureN,
                                            ManureP2O5 = recData.Recommendation.ManureP2O5,
                                            ManureK2O = recData.Recommendation.ManureK2O,
                                            ManureSO3 = recData.Recommendation.ManureSO3,
                                            ManureMgO = recData.Recommendation.ManureMgO,
                                            ManureLime = recData.Recommendation.ManureLime,
                                            FertilizerN = recData.Recommendation.FertilizerN,
                                            FertilizerP2O5 = recData.Recommendation.FertilizerP2O5,
                                            FertilizerK2O = recData.Recommendation.FertilizerK2O,
                                            FertilizerSO3 = recData.Recommendation.FertilizerSO3,
                                            FertilizerMgO = recData.Recommendation.FertilizerMgO,
                                            FertilizerLime = recData.Recommendation.FertilizerLime,
                                            SNSIndex = recData.Recommendation.SNSIndex,
                                            SIndex = recData.Recommendation.SIndex,
                                            LimeIndex = recData.Recommendation.PH,
                                            KIndex = recData.Recommendation.KIndex != null ? (recData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (recData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : recData.Recommendation.KIndex)) : null,
                                            MgIndex = recData.Recommendation.MgIndex,
                                            PIndex = recData.Recommendation.PIndex,
                                            NaIndex = recData.Recommendation.NaIndex,
                                            NIndex = recData.Recommendation.NIndex,
                                            CreatedOn = recData.Recommendation.CreatedOn,
                                            ModifiedOn = recData.Recommendation.ModifiedOn,
                                            FertiliserAppliedN = recData.Recommendation.FertiliserAppliedN,
                                            FertiliserAppliedP2O5 = recData.Recommendation.FertiliserAppliedP2O5,
                                            FertiliserAppliedK2O = recData.Recommendation.FertiliserAppliedK2O,
                                            FertiliserAppliedMgO = recData.Recommendation.FertiliserAppliedMgO,
                                            FertiliserAppliedSO3 = recData.Recommendation.FertiliserAppliedSO3,
                                            FertiliserAppliedNa2O = recData.Recommendation.FertiliserAppliedNa2O,
                                            FertiliserAppliedLime = recData.Recommendation.FertiliserAppliedLime,
                                            FertiliserAppliedNH4N = recData.Recommendation.FertiliserAppliedNH4N,
                                            FertiliserAppliedNO3N = recData.Recommendation.FertiliserAppliedNO3N,

                                        };
                                        model.Recommendations.Add(rec);

                                        if (recData.RecommendationComments.Count > 0)
                                        {
                                            foreach (var item in recData.RecommendationComments)
                                            {
                                                var recCom = new RecommendationComment
                                                {
                                                    ID = item.ID,
                                                    RecommendationID = item.RecommendationID,
                                                    Nutrient = item.Nutrient,
                                                    Comment = item.Comment
                                                };
                                                model.RecommendationComments.Add(recCom);
                                            }
                                        }

                                        if (recData.OrganicManures.Count > 0)
                                        {
                                            foreach (var item in recData.OrganicManures)
                                            {
                                                var orgManure = new OrganicManureData
                                                {
                                                    ID = item.ID,
                                                    ManureTypeName = item.ManureTypeName,
                                                    ApplicationMethodName = item.ApplicationMethodName,
                                                    ApplicationDate = item.ApplicationDate,
                                                    ApplicationRate = item.ApplicationRate,
                                                    EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
                                                    EncryptedFieldName = _cropDataProtector.Protect(model.FieldName),
                                                    EncryptedManureTypeName = _cropDataProtector.Protect(item.ManureTypeName)
                                                };
                                                model.OrganicManures.Add(orgManure);
                                            }
                                            ViewBag.OrganicManure = _cropDataProtector.Protect(Resource.lblOrganic);
                                            model.OrganicManures = model.OrganicManures.OrderByDescending(x => x.ApplicationDate).ToList();
                                        }
                                        if (recData.FertiliserManures.Count > 0)
                                        {
                                            foreach (var item in recData.FertiliserManures)
                                            {
                                                var fertiliserManure = new FertiliserManureDataViewModel
                                                {
                                                    ID = item.ID,
                                                    ManagementPeriodID = item.ManagementPeriodID,
                                                    ApplicationDate = item.ApplicationDate,
                                                    ApplicationRate = item.ApplicationRate,
                                                    Confirm = item.Confirm,
                                                    N = item.N,
                                                    P2O5 = item.P2O5,
                                                    K2O = item.K2O,
                                                    MgO = item.MgO,
                                                    SO3 = item.SO3,
                                                    Na2O = item.Na2O,
                                                    Lime = item.Lime,
                                                    NH4N = item.NH4N,
                                                    NO3N = item.NO3N,
                                                    EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
                                                    EncryptedFieldName = _cropDataProtector.Protect(model.FieldName)
                                                };
                                                ViewBag.Fertiliser = _cropDataProtector.Protect(Resource.lblFertiliser);
                                                model.FertiliserManures.Add(fertiliserManure);
                                            }

                                            model.FertiliserManures = model.FertiliserManures.OrderByDescending(x => x.ApplicationDate).ToList();
                                        }

                                    }

                                }

                            }
                            (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
                            if (error == null && nutrients.Count > 0)
                            {
                                model.Nutrients = new List<NutrientResponseWrapper>();
                                model.Nutrients = nutrients;
                            }

                        }
                    }
                }
                else
                {
                    TempData["ErrorOnHarvestYearOverview"] = error.Message;
                    return RedirectToAction("HarvestYearOverview", new
                    {
                        id = q,
                        year = s
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in Recommendations() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = string.Concat(error != null ? error.Message : "", ex.Message);
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = q,
                    year = s
                });
            }
            return View(model);
        }

        private async Task<List<int>> FetchAllowedFieldsForSecondCrop(List<HarvestYearPlanResponse> harvestYearPlanResponse, int harvestYear, int cropTypeId)
        {
            List<int> secondCropList = new List<int>();
            List<int> fieldsAllowedForSecondCrop = new List<int>();
            foreach (var firstCropPlans in harvestYearPlanResponse)
            {
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(firstCropPlans.FieldID);
                int cropPlanCount = cropsResponse.Where(x => x.Year == harvestYear && x.Confirm == false).Count();

                if (cropPlanCount == 1)
                {
                    secondCropList = await _cropService.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID);
                    if (secondCropList.Count > 0)
                    {
                        foreach (int secondCropTypeId in secondCropList)
                        {
                            if (secondCropTypeId == cropTypeId)
                            {
                                fieldsAllowedForSecondCrop.Add(firstCropPlans.FieldID);
                            }
                        }
                    }
                }

            }
            return fieldsAllowedForSecondCrop;
        }

        private async Task<bool> IsSecondCropAllowed(List<CropDetailResponse> CropDetailResponse)
        {
            List<int> secondCropList = new List<int>();
            bool isSecondCropAllowed = false;
            foreach (var firstCropPlans in CropDetailResponse)
            {
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(firstCropPlans.FieldID);


                secondCropList = await _cropService.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID);
                if (secondCropList.Count > 0)
                {
                    isSecondCropAllowed = true;
                }

            }
            return isSecondCropAllowed;
        }

        [HttpGet]
        public async Task<IActionResult> SortOrganicList(string year, string id, string q, string r)
        {
            _logger.LogTrace("Crop Controller : SortOrganicList() action called");
            PlanViewModel model = new PlanViewModel();

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (!string.IsNullOrWhiteSpace(q) && model != null)
            {
                string decrypt = _cropDataProtector.Unprotect(q);
                if (decrypt != null && decrypt == Resource.lblField)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        string decryptOrderBy = _cropDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblDesc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblAsc);
                        }
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblAsc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblDesc);
                        }
                        model.sortOrganicListOrderByDate = null;
                    }
                }
                else if (decrypt != null && decrypt == Resource.lblDate)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        string decryptOrderBy = _cropDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblDesc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblAsc);
                        }
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblAsc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblDesc);
                        }
                        model.SortOrganicListOrderByFieldName = null;
                    }
                }
            }
            //if (model == null)
            //{
            //    return RedirectToAction("HarvestYearOverview");
            //}
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            return Redirect(Url.Action("HarvestYearOverview", new { year = year, id = id, s = q, t = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationsForSorting), u = r }) + Resource.lblOrganicMaterialApplicationsForSorting);
            // return View("HarvestYearOverview", model);
        }

        [HttpGet]
        public async Task<IActionResult> SortInOrganicList(string year, string id, string q, string r)
        {
            _logger.LogTrace("Crop Controller : SortInOrganicList() action called");
            PlanViewModel model = null;

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
            {
                model = new PlanViewModel();
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (!string.IsNullOrWhiteSpace(q) && model != null)
            {
                string decrypt = _cropDataProtector.Unprotect(q);
                if (decrypt != null && decrypt == Resource.lblField)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        string decryptOrderBy = _cropDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblDesc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblAsc);
                        }
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblAsc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblDesc);
                        }
                        model.sortInOrganicListOrderByDate = null;
                    }
                }
                else if (decrypt != null && decrypt == Resource.lblDate)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        string decryptOrderBy = _cropDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblDesc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblAsc);
                        }
                        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblAsc)
                        {
                            r = _cropDataProtector.Protect(Resource.lblDesc);
                        }
                        model.SortInOrganicListOrderByFieldName = null;
                    }
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            return Redirect(Url.Action("HarvestYearOverview", new { year = year, id = id, s = q, t = _cropDataProtector.Protect(Resource.lblInorganicFertiliserApplicationsForSorting), u = r }) + Resource.lblInorganicFertiliserApplicationsForSorting);

        }

        [HttpGet]
        public IActionResult CropGroupName(string? q)
        {
            _logger.LogTrace("Crop Controller : CropGroupName() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                else
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                    {
                        ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
                    }
                    if (!string.IsNullOrWhiteSpace(model.CropType))
                    {
                        ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
                    }



                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropGroupName() action : {ex.Message}, {ex.StackTrace}");
                if (string.IsNullOrWhiteSpace(q))
                {
                    TempData["ErrorOnSelectField"] = ex.Message;
                    return RedirectToAction("CropFields");
                }
                else
                {
                    TempData["ErrorUpdateCropGroupNameCheckAnswer"] = ex.Message;
                    return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = _cropDataProtector.Protect(model.CropGroupName) });
                }

            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroupName(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropGroupName() post action called");
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                {
                    if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                    {
                        (List<HarvestYearPlanResponse> harvestYearPlanResponses, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                        if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponses.Count > 0)
                        {
                            bool cropGroupNameExists = harvestYearPlanResponses
                               .Any(harvest =>
                               !string.IsNullOrEmpty(harvest.CropGroupName) && harvest.CropGroupName.Equals(model.CropGroupName)
                               && harvest.Year == model.Year);

                            if (cropGroupNameExists)
                            {

                                ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                                return View(model);
                            }
                        }
                    }
                    else
                    {
                        if (model.Crops != null && model.Crops.Count > 0)
                        {
                            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                            {
                                ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
                            }
                            if (!string.IsNullOrWhiteSpace(model.CropType))
                            {
                                ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
                            }


                            string cropIds = string.Join(",", model.Crops.Select(x => x.ID));
                            (bool groupNameExist, Error error) = await _cropService.IsCropsGroupNameExistForUpdate(cropIds, model.CropGroupName, model.Year.Value);
                            if (string.IsNullOrWhiteSpace(error.Message) && groupNameExist)
                            {
                                ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                                return View(model);
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    for (int i = 0; i < model.Crops.Count; i++)
                    {
                        model.Crops[i].CropGroupName = model.CropGroupName;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
                else
                {
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CropGroupName() post action : {ex.Message}, {ex.StackTrace}");
                TempData["CropGroupNameError"] = ex.Message;
                return RedirectToAction("CropGroupName");
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                if (model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange) && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction("CheckAnswer");
                }

                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass)
                {
                    return RedirectToAction("CurrentSward");
                }
                return RedirectToAction("VarietyName");
            }
            else
            {
                return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = (!string.IsNullOrWhiteSpace(model.CropGroupName) ? _cropDataProtector.Protect(model.CropGroupName) : string.Empty), s = _cropDataProtector.Protect(Resource.lblTrue) });
            }
        }
        [HttpGet]
        public IActionResult RemoveCrop(string? q, string? r = null, string? s = null, string? t = null, string? u = null)
        {
            _logger.LogTrace("Crop Controller : RemoveCrop() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in RemoveCrop() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                model.CropType = _cropDataProtector.Unprotect(q);

            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                model.CropGroupName = _cropDataProtector.Unprotect(r);
            }
            if (!string.IsNullOrWhiteSpace(s))
            {
                model.FieldName = _cropDataProtector.Unprotect(s);
            }
            if (!string.IsNullOrWhiteSpace(t))
            {
                model.EncryptedFieldId = t;
                model.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(t));
            }
            if (!string.IsNullOrWhiteSpace(u))
            {
                model.CropOrder = Convert.ToInt32(_cropDataProtector.Unprotect(u));
            }
            ViewBag.EncryptedCropType = q;
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.EncryptedCropGroupName = r;
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCrop(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : RemoveCrop() post action called");
            try
            {
                if (model.RemoveCrop == null)
                {
                    ModelState.AddModelError("RemoveCrop", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View("RemoveCrop", model);
                }
                if (!model.RemoveCrop.Value)
                {
                    if (!string.IsNullOrWhiteSpace(model.FieldName))
                    {
                        return RedirectToAction("Recommendations", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
                    }
                    else
                    {
                        return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
                    }
                }
                else
                {
                    Error error = new Error();
                    (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear)), Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                    if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(model.FieldName))
                        {
                            harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropTypeName == model.CropType && x.CropGroupName == model.CropGroupName).ToList();
                        }
                        else
                        {
                            harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.FieldID == model.FieldID &&
                            x.Year == model.Year && x.CropOrder == model.CropOrder.Value).ToList();
                        }
                        if (harvestYearPlanResponse.Count > 0)
                        {
                            List<int> cropIds = harvestYearPlanResponse.Select(x => x.CropID).ToList();
                            (string message, error) = await _cropService.RemoveCropPlan(cropIds);
                            if (!string.IsNullOrWhiteSpace(error.Message))
                            {
                                TempData["RemoveGroupError"] = error.Message;
                                return View(model);
                            }
                            else
                            {
                                (harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                                {
                                    if (string.IsNullOrWhiteSpace(model.FieldName))
                                    {
                                        return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) });
                                    }
                                    else
                                    {
                                        return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                                    }
                                }
                                else
                                {
                                    List<PlanSummaryResponse> planSummaryResponse = await _cropService.FetchPlanSummaryByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), 0);
                                    if (planSummaryResponse != null && planSummaryResponse.Count > 0)
                                    {
                                        if (string.IsNullOrWhiteSpace(model.FieldName))
                                        {
                                            return RedirectToAction("PlansAndRecordsOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()), q = _cropDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) });
                                        }
                                        else
                                        {
                                            return RedirectToAction("PlansAndRecordsOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()), q = _cropDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                                        }
                                    }
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(model.FieldName))
                                        {
                                            return RedirectToAction("FarmSummary", "Farm", new { id = model.EncryptedFarmId, q = _farmDataProtector.Protect(Resource.lblTrue), r = _farmDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) });
                                        }
                                        else
                                        {
                                            return RedirectToAction("FarmSummary", "Farm", new { id = model.EncryptedFarmId, q = _farmDataProtector.Protect(Resource.lblTrue), r = _farmDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                                        }
                                    }
                                }
                            }
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in RemoveCrop() post action : {ex.Message}, {ex.StackTrace}");
                TempData["RemoveGroupError"] = ex.Message;

            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeletePlanOrganicAndFertiliser(string q, string r, string s, string? u, string? t, string? v)
        {
            _logger.LogTrace("Crop Controller : DeletePlanOrganicAndFertiliser() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r))
                {
                    model.EncryptedId = q;
                    model.DeletedAction = r;

                    string decryptedAction = _cropDataProtector.Unprotect(r);
                    if (!string.IsNullOrWhiteSpace(decryptedAction) && decryptedAction == Resource.lblOrganic)
                    {

                        ViewBag.RemoveContent = !string.IsNullOrWhiteSpace(model.ManureType) ? model.ManureType : (!string.IsNullOrWhiteSpace(t) ? _cropDataProtector.Unprotect(t) : string.Empty);
                        ViewBag.RemoveContent2 = Resource.MsgDeletePlanOrganicContent1;
                    }
                    else if (!string.IsNullOrWhiteSpace(decryptedAction) && decryptedAction == Resource.lblFertiliser)
                    {
                        ViewBag.RemoveContent = Resource.lblInorganicFertiliser;
                        ViewBag.RemoveContent2 = Resource.MsgDeletePlanFertiliserContent1;
                    }
                }
                if (!string.IsNullOrWhiteSpace(u))
                {
                    model.isComingFromRecommendation = Convert.ToBoolean(_cropDataProtector.Unprotect(u));
                    model.organicManureIds = new List<int>();
                    model.organicManureIds.Add(Convert.ToInt32(_cropDataProtector.Unprotect(q)));

                }
                if (!string.IsNullOrWhiteSpace(v))
                {
                    model.EncryptedFieldId = v;
                }

                if (!string.IsNullOrWhiteSpace(s))
                {
                    model.SelectedField = new List<string>();
                    model.FieldName = _cropDataProtector.Unprotect(s);
                    model.SelectedField.Add(model.FieldName);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in DeletePlanOrganicAndFertiliser() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlanOrganicAndFertiliser(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : DeletePlanOrganicAndFertiliser() post action called");
            try
            {
                Error error = new Error();
                string success = string.Empty;
                string decryptedAction = _cropDataProtector.Unprotect(model.DeletedAction);
                int decryptedId = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedId));

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                if (!string.IsNullOrWhiteSpace(decryptedAction) && decryptedAction == Resource.lblFertiliser)
                {
                    //(success, error) = await _fertiliserManureService.DeleteFertiliserByIdAsync(decryptedId);
                    //if (!string.IsNullOrWhiteSpace(error.Message))
                    //{
                    //    TempData["DeletePlanOrganicAndFertiliserError"] = error.Message;
                    //    return View(model);

                    //}
                    //else
                    //{
                    //    if (model.isComingFromRecommendation != null && model.isComingFromRecommendation == false)
                    //    {
                    //        return RedirectToAction("Recommendations", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved), u = _cropDataProtector.Protect(Resource.MsgNutrientRecommendationsMayBeUpdated) });
                    //    }
                    //    else
                    //    {
                    //        return Redirect(Url.Action("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved), v = _cropDataProtector.Protect(Resource.MsgNutrientRecommendationsMayBeUpdated) }) + Resource.lblInorganicFertiliserApplicationsForSorting); ;
                    //    }
                    //}
                }
                else if (!string.IsNullOrWhiteSpace(decryptedAction) && decryptedAction == Resource.lblOrganic)
                {
                    (success, error) = await _organicManureService.DeleteOrganicManureByIdAsync(model.organicManureIds);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["DeletePlanOrganicAndFertiliserError"] = error.Message;
                        return View(model);
                    }
                    else
                    {
                        if (model.isComingFromRecommendation != null && model.isComingFromRecommendation == false)
                        {
                            return RedirectToAction("Recommendations", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationRemoved), u = _cropDataProtector.Protect(Resource.lblSelectFieldToSeeItsUpdatedNutrientRecommendations) });
                        }
                        else
                        {
                            return Redirect(Url.Action("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationRemoved), v = _cropDataProtector.Protect(Resource.lblSelectFieldToSeeItsUpdatedNutrientRecommendations) }) + Resource.lblOrganicMaterialApplicationsForSorting);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in DeletePlanOrganicAndFertiliser() post action : {ex.Message}, {ex.StackTrace}");
                TempData["DeletePlanOrganicAndFertiliserError"] = ex.Message;
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult OrganicManureFieldRemove(string q)
        {
            _logger.LogTrace("Crop Controller : OrganicManureField() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.EncryptedId = q;
                    model.organicManureIds = new List<int>();
                    int id = Convert.ToInt32(_cropDataProtector.Unprotect(q));
                    if (id != null && id > 0)
                    {
                        OrganicManureResponse organicManureResponse = model.HarvestYearPlans.OrganicManureList.Where(x => x.ID == id).FirstOrDefault();
                        if (organicManureResponse != null)
                        {
                            model.SelectedField = new List<string>();
                            List<OrganicManureResponse> organicManureResponses = model.HarvestYearPlans.OrganicManureList.Where(x => x.TypeOfManure == organicManureResponse.TypeOfManure && x.ApplicationDate == organicManureResponse.ApplicationDate).DistinctBy(x => x.FieldId).ToList();
                            if (organicManureResponses != null && organicManureResponses.Count > 0)
                            {
                                //ViewBag.ManureType = organicManureResponse.TypeOfManure;
                                model.ManureType = organicManureResponse.TypeOfManure;
                                ViewBag.ApplicationDate = organicManureResponse.ApplicationDate.Value.ToString("dd MMMM yyyy");
                                if (organicManureResponses.Count == 1)
                                {
                                    model.FieldName = organicManureResponse.Field;
                                    //model.SelectedField.Add(organicManureResponse.Field);
                                    model.organicManureIds.Add(organicManureResponse.ID);
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                                    return RedirectToAction("DeletePlanOrganicAndFertiliser", new
                                    {
                                        q = model.EncryptedId,
                                        r = _cropDataProtector.Protect(Resource.lblOrganic),
                                        s = _cropDataProtector.Protect(model.FieldName),
                                        t = _cropDataProtector.Protect(Resource.lblTrue)
                                    });

                                }
                                else
                                {
                                    var SelectListItem = organicManureResponses.Select(f => new SelectListItem
                                    {
                                        Value = f.FieldId.ToString(),
                                        Text = f.Field
                                    }).DistinctBy(x => x.Text).ToList();
                                    ViewBag.fieldList = SelectListItem;
                                }
                            }
                        }
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in OrganicManureField() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrganicManureFieldRemove(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : OrganicManureFieldRemove() post action called");
            try
            {
                if (model.FieldList == null || model.FieldList.Count == 0)
                {
                    ModelState.AddModelError("FieldList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
                }
                int id = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedId));
                List<OrganicManureResponse> organicManureResponses = new List<OrganicManureResponse>();
                OrganicManureResponse organicManureResponse = null;
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
                if (id != null && id > 0)
                {
                    organicManureResponse = model.HarvestYearPlans.OrganicManureList.Where(x => x.ID == id).FirstOrDefault();
                    if (organicManureResponse != null)
                    {
                        organicManureResponses = model.HarvestYearPlans.OrganicManureList.Where(x => x.TypeOfManure == organicManureResponse.TypeOfManure && x.ApplicationDate == organicManureResponse.ApplicationDate).ToList();
                        if (organicManureResponses != null && organicManureResponses.Count > 0)
                        {
                            ViewBag.ManureType = organicManureResponse.TypeOfManure;
                            ViewBag.ApplicationDate = organicManureResponse.ApplicationDate.Value.ToString("dd MMMM yyyy");


                        }
                    }
                }
                if (!ModelState.IsValid)
                {
                    if (organicManureResponses != null && organicManureResponses.Count > 0)
                    {
                        var selectListItem = organicManureResponses.Select(f => new SelectListItem
                        {
                            Value = f.FieldId.ToString(),
                            Text = f.Field
                        }).DistinctBy(x => x.Text).ToList();
                        ViewBag.fieldList = selectListItem;
                    }
                    return View(model);
                }
                Error error = new Error();
                string success = string.Empty;
                //string decryptedAction = _cropDataProtector.Unprotect(model.DeletedAction);
                //int decryptedFertId = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedId));
                model.organicManureIds = new List<int>();
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblUpdateAllTheseFields)
                {
                    if (organicManureResponses != null && organicManureResponses.Count > 0)
                    {
                        //var selectListItem = organicManureResponses.Select(f => new SelectListItem
                        //{
                        //    Value = f.FieldId.ToString(),
                        //    Text = f.Field
                        //}).DistinctBy(x => x.Text).ToList();
                        model.organicManureIds = organicManureResponses.Select(item => item.ID).ToList();
                        model.SelectedField = organicManureResponses.Select(item => item.Field).Distinct().ToList();
                    }
                }
                else
                {
                    if (organicManureResponse != null)
                    {
                        //model.organicManureIds = organicManureResponses
                        //.Where(item => item.FieldId == organicManureResponse.FieldId &&
                        //item.TypeOfManure == organicManureResponse.TypeOfManure &&
                        //item.ApplicationDate == organicManureResponse.ApplicationDate)
                        //.Select(item => item.ID).ToList();

                        if (model.FieldList != null && model.FieldList.Count > 0)
                        {
                            List<string> fieldNames = new List<string>();
                            List<int> orgId = new List<int>();

                            foreach (var fieldId in model.FieldList)
                            {
                                fieldNames.Add((await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name);
                                model.organicManureIds.Add(organicManureResponses
                               .Where(item => item.FieldId == fieldId &&
                               item.TypeOfManure == organicManureResponse.TypeOfManure &&
                               item.ApplicationDate == organicManureResponse.ApplicationDate)
                               .Select(item => item.ID).FirstOrDefault());
                            }
                            model.SelectedField = fieldNames;
                        }

                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in OrganicManureFieldRemove() post action : {ex.Message}, {ex.StackTrace}");
                TempData["OrganicManureFieldRemoveError"] = ex.Message;

            }
            return RedirectToAction("DeletePlanOrganicAndFertiliser", new { q = model.EncryptedId, r = _cropDataProtector.Protect(Resource.lblOrganic), t = _cropDataProtector.Protect(Resource.lblTrue) });
        }
        [HttpGet]
        public IActionResult UpdateExcessWinterRainfall()
        {
            _logger.LogTrace("Crop Controller : UpdateExcessWinterRainfall() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in UpdateExcessWinterRainfall() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateExcessWinterRainfall(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : UpdateExcessWinterRainfall() post action called");
            return View(model);

        }
        [HttpGet]
        public async Task<IActionResult> ExcessWinterRainfall()
        {
            _logger.LogTrace("Crop Controller : ExcessWinterRainfall() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (List<CommonResponse> excessWinterRainfallOption, Error error) = await _farmService.FetchExcessWinterRainfallOptionAsync();
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (excessWinterRainfallOption.Count > 0)
                    {
                        var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name
                        }).ToList();

                        ViewBag.ExcessRainFallOptions = SelectListItem;
                    }
                }


            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in ExcessWinterRainfall() action : {ex.Message}, {ex.StackTrace}");
                TempData["UpdateExcessWinterRainfallError"] = ex.Message;
                return RedirectToAction("UpdateExcessWinterRainfall");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcessWinterRainfall(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : ExcessWinterRainfall() post action called");
            try
            {
                if (model.ExcessWinterRainfallId == null)
                {
                    ModelState.AddModelError("ExcessWinterRainfallId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    (List<CommonResponse> excessWinterRainfallOption, Error error) = await _farmService.FetchExcessWinterRainfallOptionAsync();
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (excessWinterRainfallOption.Count > 0)
                        {
                            var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name
                            }).ToList();

                            ViewBag.ExcessRainFallOptions = SelectListItem;
                        }
                    }
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                return RedirectToAction("ExcessWinterRainfallCheckAnswer", model);

            }



            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in ExcessWinterRainfall() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ExcessWinterRainfallError"] = ex.Message;
                return View(model);
            }

        }
        [HttpGet]
        public async Task<IActionResult> ExcessWinterRainfallCheckAnswer()
        {
            _logger.LogTrace("Crop Controller : ExcessWinterRainfallCheckAnswer() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (CommonResponse commonResponse, Error error) = await _farmService.FetchExcessWinterRainfallOptionByIdAsync(model.ExcessWinterRainfallId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && commonResponse != null)
                {
                    model.ExcessWinterRainfallName = commonResponse.Name;
                    model.ExcessWinterRainfallValue = commonResponse.Value;
                }
                model.IsExcessWinterRainfallCheckAnswer = true;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
            }

            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in ExcessWinterRainfallCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ExcessWinterRainfallError"] = ex.Message;
                return RedirectToAction("ExcessWinterRainfall", model);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcessWinterRainfallCheckAnswer(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : ExcessWinterRainfallCheckAnswer() action called");

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                var excessRainfalls = new ExcessRainfalls
                {
                    FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)),
                    Year = model.Year,
                    ExcessRainfall = 0,
                    WinterRainfall = model.ExcessWinterRainfallValue,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId
                };
                string jsonData = JsonConvert.SerializeObject(excessRainfalls);
                (ExcessRainfalls excessRainfall, Error error) = await _farmService.AddExcessWinterRainfallAsync(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), model.Year.Value, jsonData, model.IsExcessWinterRainfallUpdated.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && excessRainfall != null)
                {
                    return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(string.Format(Resource.MsgAddExcessWinterRainfallContentOne, model.Year.Value)), v = _cropDataProtector.Protect(string.Format(Resource.MsgAddExcessWinterRainfallContentSecond, model.Year.Value)) });
                }
                else
                {
                    TempData["ExcessWinterRainfallCheckAnswerError"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in ExcessWinterRainfallCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ExcessWinterRainfallCheckAnswerError"] = ex.Message;
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCropGroupNameCheckAnswer(string? q, string? r, string? s)
        {
            _logger.LogTrace("Crop Controller : UpdateCropGroupNameCheckAnswer() action called");
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.CropType = _cropDataProtector.Unprotect(q);

                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    model.CropGroupName = _cropDataProtector.Unprotect(r);
                }
                Error error = new Error();
                bool allYieldsAreSame = true;
                bool allSowingAreSame = true;
                string? yieldQuestion = null;
                string? sowingQuestion = null;
                bool isBasePlan = false;
                decimal? firstYield = null;
                DateTime? firstSowingDate = null;
                if (string.IsNullOrWhiteSpace(s))
                {
                    //if (model.Crops == null)
                    //{
                    //    model.PreviousCropGroupName = model.CropGroupName;
                    //}
                    (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear)), Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                    if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                    {
                        harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropTypeName == model.CropType && x.CropGroupName == model.CropGroupName).ToList();
                        if (harvestYearPlanResponse != null)
                        {
                            model.Crops = new List<Crop>();

                            decimal defaultYield = await _cropService.FetchCropTypeDefaultYieldByCropTypeId(harvestYearPlanResponse.FirstOrDefault().CropTypeID);
                            for (int i = 0; i < harvestYearPlanResponse.Count; i++)
                            {
                                var crop = new Crop();
                                crop.FieldName = harvestYearPlanResponse[i].FieldName;
                                crop.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
                                if (decimal.TryParse(harvestYearPlanResponse[i].Yield, out decimal yield))
                                {
                                    crop.Yield = yield;
                                    model.Yield = yield;
                                    yieldQuestion = yield == defaultYield ? string.Format(Resource.lblUseTheStandardFigure, defaultYield) : null;
                                    if (string.IsNullOrWhiteSpace(yieldQuestion))
                                    {
                                        if (firstYield == null)
                                        {
                                            firstYield = yield;
                                        }
                                        else if (firstYield != yield)
                                        {
                                            allYieldsAreSame = false;
                                        }
                                    }
                                }
                                else
                                {
                                    crop.Yield = null;
                                }
                                if (harvestYearPlanResponse[i].CropInfo1 == null && harvestYearPlanResponse[i].Yield == null)
                                {
                                    isBasePlan = true;
                                }

                                if (harvestYearPlanResponse[i].SowingDate == null)
                                {
                                    sowingQuestion = Resource.lblNoIWillEnterTheDateLater;
                                    crop.SowingDate = null;
                                }
                                else
                                {
                                    if (firstSowingDate == null)
                                    {
                                        firstSowingDate = harvestYearPlanResponse[i].SowingDate;
                                        model.SowingDate = firstSowingDate.Value;
                                    }
                                    else if (firstSowingDate != harvestYearPlanResponse[i].SowingDate)
                                    {
                                        allSowingAreSame = false;
                                    }
                                    crop.SowingDate = harvestYearPlanResponse[i].SowingDate;

                                }
                                crop.ID = harvestYearPlanResponse[i].CropID;
                                crop.CropInfo1 = harvestYearPlanResponse[i].CropInfo1;
                                model.Crops.Add(crop);
                            }

                            if (allYieldsAreSame && harvestYearPlanResponse.Count > 1)
                            {
                                ViewBag.allYieldsAreSame = allYieldsAreSame;
                            }
                            if (allSowingAreSame && harvestYearPlanResponse.Count > 1)
                            {
                                ViewBag.allSowingAreSame = allSowingAreSame;
                            }
                            model.CropInfo1 = harvestYearPlanResponse.FirstOrDefault().CropInfo1;
                            model.CropInfo2 = harvestYearPlanResponse.FirstOrDefault().CropInfo2;
                            ViewBag.SowingQuestion = sowingQuestion;
                            ViewBag.YieldQuestion = yieldQuestion;
                            model.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
                            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
                            model.CropType = harvestYearPlanResponse.FirstOrDefault().CropTypeName;
                            model.Variety = harvestYearPlanResponse.FirstOrDefault().CropVariety;
                            model.CropGroupName = harvestYearPlanResponse.FirstOrDefault().CropGroupName;
                            model.PreviousCropGroupName = model.CropGroupName;
                            if (model.CropTypeID != null && model.CropInfo1 != null)
                            {
                                model.CropInfo1Name = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(model.CropTypeID.Value, model.CropInfo1.Value);
                            }

                            if (model.CropInfo2 != null)
                            {
                                model.CropInfo2Name = await _cropService.FetchCropInfo2NameByCropInfo2Id(model.CropInfo2.Value);
                            }
                            ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType.ToString());


                            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
                            {
                                ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
                            }
                        }

                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(model.CropGroupName))
                    {
                        model.CropGroupName = model.PreviousCropGroupName;
                    }

                    for (int i = 0; i < model.Crops.Count; i++)
                    {
                        if (model.Crops[i].CropInfo1 == null && model.Crops[i].Yield == null)
                        {
                            isBasePlan = true;
                        }

                        decimal defaultYield = await _cropService.FetchCropTypeDefaultYieldByCropTypeId(model.Crops.FirstOrDefault().CropTypeID.Value);

                        yieldQuestion = model.Crops[i].Yield == defaultYield ? string.Format(Resource.lblUseTheStandardFigure, defaultYield) : null;
                        if (string.IsNullOrWhiteSpace(yieldQuestion))
                        {
                            if (firstYield == null)
                            {
                                firstYield = model.Crops[i].Yield;
                            }
                            else if (firstYield != model.Crops[i].Yield)
                            {
                                allYieldsAreSame = false;
                            }
                        }

                        if (model.Crops[i].SowingDate == null)
                        {
                            sowingQuestion = Resource.lblNoIWillEnterTheDateLater;
                        }
                        else
                        {
                            if (firstSowingDate == null)
                            {
                                firstSowingDate = model.Crops[i].SowingDate;
                            }
                            else if (firstSowingDate != model.Crops[i].SowingDate)
                            {
                                allSowingAreSame = false;
                            }
                        }
                    }

                    if (allYieldsAreSame && model.Crops.Count > 1)
                    {
                        ViewBag.allYieldsAreSame = allYieldsAreSame;
                    }
                    if (allSowingAreSame && model.Crops.Count > 1)
                    {
                        ViewBag.allSowingAreSame = allSowingAreSame;
                    }
                    ViewBag.SowingQuestion = sowingQuestion;
                    ViewBag.YieldQuestion = yieldQuestion;

                }
                ViewBag.isBasePlan = isBasePlan;
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                model.EncryptedIsCropUpdate = _cropDataProtector.Protect(Resource.lblTrue);
                string? cropInfoOneQuestion = await _cropService.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
                ViewBag.CropInfoOneQuestion = cropInfoOneQuestion;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in UpdateCropGroupNameCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCropGroupNameCheckAnswer(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : UpdateCropGroupNameCheckAnswer() post action called");
            try
            {
                string success = string.Empty;
                if (model.Crops != null && model.Crops.Count > 0)
                {
                    string cropIds = string.Join(",", model.Crops.Select(x => x.ID));
                    if (string.IsNullOrWhiteSpace(model.Variety))
                    {
                        model.Variety = null;
                    }
                    (List<Crop> crops, Error error) = await _cropService.UpdateCropGroupName(cropIds, model.CropGroupName, model.Variety, model.Year.Value);

                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["ErrorUpdateCropGroupNameCheckAnswer"] = error.Message;
                        return RedirectToAction("UpdateCropGroupNameCheckAnswer", new { q = _cropDataProtector.Protect(model.CropType), r = (!string.IsNullOrWhiteSpace(model.CropGroupName) ? _cropDataProtector.Protect(model.CropGroupName) : string.Empty), s = _cropDataProtector.Protect(Resource.lblTrue) });
                    }
                    else
                    {
                        return Redirect(Url.Action("HarvestYearOverview", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.lblCropPlanUpdated), v = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations) }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in UpdateCropGroupNameCheckAnswer() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorUpdateCropGroupNameCheckAnswer"] = ex.Message;
            }
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> CurrentSward()
        {
            _logger.LogTrace("Crop Controller : CurrentSward() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CurrentSward() action : {ex.Message}, {ex.StackTrace}");
                TempData["CurrentSwardError"] = ex.Message;
                return RedirectToAction("CropGroupName");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CurrentSward(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : CropInfoTwo() post action called");
            try
            {

                if (model.CurrentSward == null)
                {
                    ModelState.AddModelError("CurrentSward", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in CurrentSward() post action : {ex.Message}, {ex.StackTrace}");
                TempData["CurrentSwardError"] = ex.Message;
                return RedirectToAction("CurrentSward");
            }
            if (model.CurrentSward == (int)NMP.Portal.Enums.CurrentSward.NewSward)
            {
                return RedirectToAction("GrassSeason");
            }
            else
            {
                return RedirectToAction("SowingDateQuestion");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GrassSeason()
        {
            _logger.LogTrace("Crop Controller : GrassSeason() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                List<GrassSeasonResponse> grassSeasons = await _cropService.FetchGrassSeasons();
                grassSeasons.RemoveAll(g => g.SeasonId == 0);
                ViewBag.GrassSeason = grassSeasons.OrderByDescending(x => x.SeasonId);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in GrassSeason() action : {ex.Message}, {ex.StackTrace}");
                TempData["GrassSeasonError"] = ex.Message;
                return RedirectToAction("CurrentSward");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassSeason(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : GrassSeason() post action called");
            try
            {
                if (model.GrassSeason == null)
                {
                    ModelState.AddModelError("GrassSeason", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    List<GrassSeasonResponse> grassSeasons = await _cropService.FetchGrassSeasons();
                    grassSeasons.RemoveAll(g => g.SeasonId == 0);
                    ViewBag.GrassSeason = grassSeasons.OrderByDescending(x => x.SeasonId);
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in GrassSeason() post action : {ex.Message}, {ex.StackTrace}");
                TempData["GrassSeasonError"] = ex.Message;
                return RedirectToAction("GrassSeason");
            }

            return RedirectToAction("SowingDateQuestion");
        }

        public async Task<IActionResult> SwardType()
        {
            _logger.LogTrace("Crop Controller : SwardType() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in SwardType() action : {ex.Message}, {ex.StackTrace}");
                TempData["SwardTypeError"] = ex.Message;
                return RedirectToAction("SowingDate");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwardType(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : SwardType() post action called");

            return RedirectToAction("GrassManagement");

        }

        [HttpGet]
        public async Task<IActionResult> GrassManagement()
        {
            _logger.LogTrace("Crop Controller : GrassManagement() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in GrassManagement() action : {ex.Message}, {ex.StackTrace}");
                TempData["GrassManagementError"] = ex.Message;
                return RedirectToAction("SwardType");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassManagement(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : GrassManagement() post action called");

            return RedirectToAction("Defoliation");
        }

        [HttpGet]
        public async Task<IActionResult> Defoliation()
        {
            _logger.LogTrace("Crop Controller : Defoliation() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in Defoliation() action : {ex.Message}, {ex.StackTrace}");
                TempData["DefoliationError"] = ex.Message;
                return RedirectToAction("GrassManagement");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Defoliation(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : Defoliation() post action called");

            return RedirectToAction("DefoliationSequence");
        }

        [HttpGet]
        public async Task<IActionResult> DefoliationSequence()
        {
            _logger.LogTrace("Crop Controller : DefoliationSequence() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in DefoliationSequence() action : {ex.Message}, {ex.StackTrace}");
                TempData["DefoliationSequenceError"] = ex.Message;
                return RedirectToAction("Defoliation");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefoliationSequence(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : DefoliationSequence() post action called");
            model.GrassGrowthClassCounter = 0;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            return RedirectToAction("GrassGrowthClass");
        }

        [HttpGet]
        public async Task<IActionResult> GrassGrowthClass(string? q)
        {
            _logger.LogTrace("Crop Controller : GrassGrowthClass() action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                List<int> fieldIds = new List<int>();
                List<int> grassGrowthClassIds = new List<int>();

                foreach (var crop in model.Crops)
                {
                    fieldIds.Add(crop.FieldID ?? 0);
                }
                (List<GrassGrowthClassResponse> grassGrowthClasses, Error error) = await _cropService.FetchGrassGrowthClass(fieldIds);
                if (error.Message == null)
                {
                    foreach (var grassGrowthClass in grassGrowthClasses)
                    {
                        grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
                    }
                }
                //List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>()
                //{
                //    new GrassGrowthClassResponse{GrassGrowthClassId=1,GrassGrowthClassName="Good" },
                //    new GrassGrowthClassResponse{GrassGrowthClassId=1,GrassGrowthClassName="Good"}
                //};
                //foreach (var grassGrowthClass in grassGrowthClasses)
                //{
                //    grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
                //}

                model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Distinct().Count();
                if (model.GrassGrowthClassDistinctCount > 1)
                {
                    model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Count;
                }

                if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
                {
                    model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
                    if (model.GrassGrowthClassCounter == 0)
                    {
                        model.FieldID = model.Crops[0].FieldID.Value;
                        model.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                        ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                        ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
                else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
                {
                    int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                    int index = itemCount - 1;//index of list
                    if (itemCount == 0)
                    {
                        model.GrassGrowthClassCounter = 0;
                        model.GrassGrowthClassEncryptedCounter = string.Empty;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("DefoliationSequence");
                    }
                    model.FieldID = model.Crops[index].FieldID.Value;
                    model.FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                    model.GrassGrowthClassCounter = index;

                    model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
                    model.FieldID = model.Crops[model.GrassGrowthClassCounter].FieldID;
                    ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                    ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;
                    if (model.GrassGrowthClassQuestion != null)
                    {
                        return RedirectToAction("DefoliationSequence");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in GrassGrowthClass() action : {ex.Message}, {ex.StackTrace}");
                TempData["GrassGrowthClassError"] = ex.Message;
                return RedirectToAction("DefoliationSequence");
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassGrowthClass(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : GrassGrowthClass() post action called");

            List<int> fieldIds = new List<int>();
            List<int> grassGrowthClassIds = new List<int>();

            foreach (var crop in model.Crops)
            {
                fieldIds.Add(crop.FieldID ?? 0);
            }
            (List<GrassGrowthClassResponse> grassGrowthClasses, Error error) = await _cropService.FetchGrassGrowthClass(fieldIds);
            if (error.Message == null)
            {
                foreach (var grassGrowthClass in grassGrowthClasses)
                {
                    grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
                }
            }
            //List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>()
            //    {
            //        new GrassGrowthClassResponse{GrassGrowthClassId=1,GrassGrowthClassName="Good" },
            //        new GrassGrowthClassResponse{GrassGrowthClassId=1,GrassGrowthClassName="Good"}
            //    };
            //foreach (var grassGrowthClass in grassGrowthClasses)
            //{
            //    grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
            //}

            model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Distinct().Count();
            if (model.GrassGrowthClassDistinctCount > 1)
            {
                model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Count;
            }

            //if (model.GrassGrowthClassDistinctCount>1)
            //{
            for (int i = 0; i < model.Crops.Count; i++)
            {
                if (model.FieldID == model.Crops[i].FieldID.Value)
                {
                    model.GrassGrowthClassCounter++;
                    if (i + 1 < model.Crops.Count)
                    {
                        model.FieldID = model.Crops[i + 1].FieldID.Value;
                        model.FieldName = model.Crops[i + 1].FieldName;
                        ViewBag.FieldName = model.Crops[i + 1].FieldName;
                        ViewBag.GrassGrowthClass = grassGrowthClasses[i + 1].GrassGrowthClassName;
                    }

                    break;
                }
            }
            model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
            
            if (model.GrassGrowthClassDistinctCount == 1 && model.Crops.Count > 1)
            {
                return RedirectToAction("DryMatterYield");
            }
            if (model.GrassGrowthClassCounter == model.Crops.Count)
            {
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> DryMatterYield(string q)
        {
            _logger.LogTrace($"Crop Controller : DryMatterYield({q}) action called");
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
                {
                    model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                    if (model.DryMatterYieldCounter == 0)
                    {
                        model.FieldID = model.Crops[0].FieldID.Value;
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }
                else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
                {
                    int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                    int index = itemCount - 1;//index of list
                    if (itemCount == 0)
                    {
                        model.DryMatterYieldCounter = 0;
                        model.DryMatterYieldEncryptedCounter = string.Empty;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("GrassGrowthClass");
                    }
                    model.FieldID = model.Crops[index].FieldID.Value;
                    model.FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                    model.DryMatterYieldCounter = index;
                    model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                }

                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Crop Controller : Exception in DryMatterYield() action : {ex.Message}, {ex.StackTrace}");
                TempData["DryMatterYieldError"] = ex.Message;
                return RedirectToAction("GrassGrowthClass");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DryMatterYield(PlanViewModel model)
        {
            _logger.LogTrace("Crop Controller : DryMatterYield() post action called");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.GrassGrowthClassQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    if (model.FieldID == model.Crops[i].FieldID.Value)
                    {
                        model.DryMatterYieldCounter++;
                        if (i + 1 < model.Crops.Count)
                        {
                            model.FieldID = model.Crops[i + 1].FieldID.Value;
                        }

                        break;
                    }
                }
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsQuestionChange) && (!model.IsCropGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
            }

            else if (model.GrassGrowthClassQuestion == (int)NMP.Portal.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
            {
                model.DryMatterYieldCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].SowingDate = model.Crops[0].SowingDate;
                    //model.Crops[i].EncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                }
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange))
                {
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                    return RedirectToAction("CheckAnswer");
                }
                
                return RedirectToAction("CheckAnswer");
            }

            if (model.DryMatterYieldCounter == model.Crops.Count)
            {
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction("CheckAnswer");
                }
                
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                return View(model);
            }

        }
    }
}
