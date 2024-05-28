using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Xml.Linq;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
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

        public CropController(ILogger<CropController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, IFieldService fieldService, ICropService cropService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
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
        public async Task<IActionResult> HarvestYearForPlan(string q, string? year, bool? isPlanRecord)
        {
            PlanViewModel model = new PlanViewModel();
            Error? error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                if (!string.IsNullOrEmpty(q))
                {
                    int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.EncryptedFarmId = q;

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmID);
                    model.IsEnglishRules = farm.EnglishRules;

                    if (!string.IsNullOrWhiteSpace(year))
                    {
                        int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));
                        model.Year = harvestYear;
                        if (isPlanRecord==false || isPlanRecord==null)
                        {
                            model.IsAddAnotherCrop = true;
                        }
                        if(isPlanRecord==true)
                        {
                            model.IsPlanRecord = true;
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("CropGroups");
                    }

                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
                }
                if (model.IsPlanRecord.Value)
                {
                    return RedirectToAction("PlansAndRecordsOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) });
                }
                if (model.IsAddAnotherCrop)
                {
                    return RedirectToAction("HarvestYearOverview", "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = string.Concat(error == null ? "" : error.Message, ex.Message);
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HarvestYearForPlan(PlanViewModel model)
        {
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
            PlanViewModel model = new PlanViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropGroupsList = cropGroups.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
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
                TempData["ErrorOnHarvestYear"] = ex.Message;
                return RedirectToAction("HarvestYearForPlan");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(PlanViewModel model)
        {
            try
            {
                if (model.CropGroupId == null)
                {
                    ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                    var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var cropGroupsList = cropGroups.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
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
                TempData["CropGroupError"] = ex.Message;
                return View(model);
            }

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    List<CropTypeResponse> cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                    var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
                    ViewBag.CropTypeList = cropTypeList.OrderBy(c => c.CropType); ;
                }
                model.IsCropTypeChange = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
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
        public async Task<IActionResult> CropTypes(PlanViewModel model)
        {
            try
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other)
                {
                    model.CropTypeID = await _cropService.FetchCropTypeByGroupId(model.CropGroupId ?? 0);
                }
                if (model.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Other && model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
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
                        var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                        ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList().OrderBy(c => c.CropType); ;
                    }
                    return View(model);
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
                        }
                    }
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
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                if (model != null && model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes)
                {
                    List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
                    potatoVarieties = await _cropService.FetchPotatoVarieties();
                    var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    ViewBag.PotatoVarietyList = potatoVarieties.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList().OrderBy(c => c.PotatoVariety); ;
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
        public async Task<IActionResult> VarietyName(PlanViewModel model)
        {
            try
            {
                if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes && model.Variety == null)
                {
                    ModelState.AddModelError("Variety", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblVarietyName.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    if (model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Potatoes)
                    {
                        List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
                        potatoVarieties = await _cropService.FetchPotatoVarieties();
                        var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                        ViewBag.PotatoVarietyList = potatoVarieties.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList().OrderBy(c => c.PotatoVariety); ;
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
                if (harvestYearPlanResponse.Count() > 0)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value)).ToList();
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
                                        EncryptedCounter = _fieldDataProtector.Protect(counter.ToString())
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
                                        return RedirectToAction("CheckAnswer");
                                    }
                                }
                            }
                        }

                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                        return RedirectToAction("SowingDateQuestion");
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
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("CropFields");
            }
            catch (Exception ex)
            {
                TempData["ErrorOnVariety"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CropFields()
        {
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);
                var SelectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);
                if (harvestYearPlanResponse.Count() > 0)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value)).ToList();
                    if (SelectListItem.Count == 1)
                    {
                        return RedirectToAction("VarietyName");
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
                TempData["ErrorOnVariety"] = ex.Message;
                return RedirectToAction("VarietyName");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropFields(PlanViewModel model)
        {
            try
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmID);
                var selectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();
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
                                EncryptedCounter = _fieldDataProtector.Protect(counter.ToString())
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
                                return RedirectToAction("CheckAnswer");
                            }
                        }
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);

                return RedirectToAction("SowingDateQuestion");
            }
            catch (Exception ex)
            {
                TempData["ErrorOnSelectField"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SowingDateQuestion()
        {
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                if (model.IsQuestionChange)
                {
                    model.IsQuestionChange = false;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropData", model);
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorOnSelectField"] = ex.Message;
                return RedirectToAction("CropFields");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SowingDateQuestion(PlanViewModel model)
        {
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
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
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

            if ((!ModelState.IsValid) && ModelState.ContainsKey("Crops[" + model.SowingDateCurrentCounter + "].SowingDate"))
            {
                var dateError = ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Count > 0 ?
                                ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblSowingDateForError)))
                {
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Clear();
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Add(Resource.MsgEnterTheDateInNumber);
                }
                else if (dateError != null && (dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, Resource.lblSowingDateForError)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, Resource.lblSowingDateForError)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, Resource.lblSowingDateForError)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, Resource.lblSowingDateForError)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, Resource.lblSowingDateForError)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, Resource.lblSowingDateForError))))
                {
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Clear();
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"].Errors.Add(Resource.ErrorMsgForDate);
                }
            }
            if (model.Crops[model.SowingDateCurrentCounter].SowingDate == null)
            {
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", Resource.MsgEnterADateBeforeContinuing);
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
                            //model.Crops[i + 1].FieldName = (await _fieldService.FetchFieldByFieldId(model.Crops[i + 1].FieldId.Value)).Name;
                            //model.Crops[i + 1].EncryptedCounter = _fieldDataProtector.Protect((model.SowingDateCurrentCounter + 1).ToString());
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
                return RedirectToAction("YieldQuestion");
            }

            if (model.SowingDateCurrentCounter == model.Crops.Count)
            {
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction("CheckAnswer");
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
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
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
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Yield(PlanViewModel model)
        {
            if (model.Crops[model.YieldCurrentCounter].Yield == null)
            {
                ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", Resource.MsgEnterFigureBeforeContinuing);
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
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }
                List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0);
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropInfoOneList = cropInfoOneResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
                ViewBag.CropInfoOneList = cropInfoOneList.OrderBy(c => c.CropInfo1Name); ;

            }
            catch (Exception ex)
            {
                TempData["ErrorOnYield"] = ex.Message;
                return RedirectToAction("Yield");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropInfoOne(PlanViewModel model)
        {
            try
            {
                List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0);
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropInfoOneList = cropInfoOneResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
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
            PlanViewModel model = new PlanViewModel();

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnotherCrop(PlanViewModel model)
        {
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CropInfoTwo()
        {
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
                }

                List<CropInfoTwoResponse> cropInfoTwoResponse = await _cropService.FetchCropInfoTwoByCropTypeId();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropInfoTwoList = cropInfoTwoResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
                ViewBag.CropInfoTwoList = cropInfoTwoList.OrderBy(c => c.CropInfo2);
            }
            catch (Exception ex)
            {
                TempData["CropInfoOneError"] = ex.Message;
                return RedirectToAction("CropInfoOne");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropInfoTwo(PlanViewModel model)
        {
            try
            {
                List<CropInfoTwoResponse> cropInfoTwoResponse = await _cropService.FetchCropInfoTwoByCropTypeId();
                var country = model.IsEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropInfoTwoList = cropInfoTwoResponse.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
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
                TempData["CropInfoTwoError"] = ex.Message;
                return RedirectToAction("CropInfoTwo");
            }

            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            PlanViewModel model = new PlanViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            model.IsCheckAnswer = true;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            return View(model);
        }

        public IActionResult BackCheckAnswer()
        {
            PlanViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("CropData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            string action = model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Other ? "Yield" : model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Cereals ?
                "CropInfoTwo" : "CropInfoOne";
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("CropData", model);
            return RedirectToAction(action, new { q = model.YieldEncryptedCounter });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(PlanViewModel model)
        {
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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;  // Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.Sid)?.Value);
            List<CropData> cropEntries = new List<CropData>();
            foreach (Crop crop in model.Crops)
            {
                crop.CreatedOn = DateTime.Now;
                crop.CreatedByID = userId;
                crop.FieldName = null;
                crop.EncryptedCounter = null;
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
                    q = _farmDataProtector.Protect(success.ToString())
                });
            }
            else
            {
                TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater; //error.Message; //
                return RedirectToAction("CheckAnswer");
            }
        }
        [HttpGet]
        public async Task<IActionResult> HarvestYearOverview(string id, string year, string? q)
        {
            PlanViewModel model = new PlanViewModel();
            try
            {
                if (!string.IsNullOrWhiteSpace(q))
                {
                    ViewBag.Success = true;
                }
                else
                {
                    ViewBag.Success = false;
                    _httpContextAccessor.HttpContext?.Session.Remove("CropData");
                }
                if (!string.IsNullOrWhiteSpace(id))
                {
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                    int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));

                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                    if (farm != null)
                    {
                        model.FarmName = farm.Name;
                    }
                    List<string> fields = new List<string>();

                    (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(harvestYear, farmId);

                    model.Year = harvestYear;
                    if (harvestYearPlanResponse != null && error.Message == null)
                    {
                        model.LastModifiedOn = harvestYearPlanResponse.Max(x => x.LastModifiedOn).ToString("dd MMM yyyy");

                        var groupedResult = harvestYearPlanResponse
                                            .GroupBy(h => new { h.CropTypeName, h.CropVariety })
                                            .Select(g => new
                                            {
                                                CropTypeName = g.Key.CropTypeName,
                                                CropVariety = g.Key.CropVariety,
                                                HarvestPlans = g.ToList()
                                            }).OrderBy(g => g.CropTypeName);
                        model.FieldCount = harvestYearPlanResponse.Select(h => h.FieldID).Distinct().Count();
                        List<Field> fieldList = await _fieldService.FetchFieldsByFarmId(farmId);
                        if (harvestYearPlanResponse.Count() > 0)
                        {
                            var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                            fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString())).ToList();
                            ViewBag.PendingField = fieldList;
                        }
                        foreach (var group in groupedResult)
                        {
                            var harvestYearPlans = new HarvestYearPlans
                            {
                                CropTypeName = group.CropTypeName,
                                CropVariety = group.CropVariety,
                            };

                            foreach (var plan in group.HarvestPlans)
                            {
                                harvestYearPlans.FieldData.Add(_cropDataProtector.Protect(plan.FieldID.ToString()), plan.FieldName);
                                //harvestYearPlans.FieldNames.Add(plan.FieldName);
                            }
                            model.HarvestYearPlans.Add(harvestYearPlans);
                        }
                        model.EncryptedFarmId = id;
                        model.EncryptedHarvestYear = year;
                    }
                    else
                    {
                        TempData["ErrorOnHarvestYearOverview"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;//error.Message; //
                        model = null;
                    }

                }
            }
            catch (Exception ex)
            {
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                model = null;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HarvestYearOverview(PlanViewModel model)
        {
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PlansAndRecordsOverview(string id, string? year)
        {
            PlanViewModel model = new PlanViewModel();
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
                if (planSummaryResponse != null)
                {
                    foreach (var item in planSummaryResponse)
                    {
                        yearList.Add(item.Year);
                    }

                    int minYear = System.DateTime.Now.Year - 1;
                    int maxYear = System.DateTime.Now.Year + 1;
                    for (int i = minYear; i <= maxYear; i++)
                    {
                        if (!yearList.Contains(i))
                        {
                            var harvestYear = new HarvestYear
                            {
                                Year = i,
                                EncryptedYear = _farmDataProtector.Protect(i.ToString()),
                            };
                            model.HarvestYear.Add(harvestYear);
                        }
                    }
                }
                model.EncryptedFarmId = id;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlansAndRecordsOverview(PlanViewModel model)
        {
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Recommendations(string q, string r, string? s)//q=fieldId,s=harvestYear
        {
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
                if (!string.IsNullOrWhiteSpace(q))
                {
                    decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.FarmName = (await _farmService.FetchFarmByIdAsync(decryptedFarmId)).Item1.Name;
                    model.EncryptedFarmId = q;
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    decryptedFieldId = Convert.ToInt32(_cropDataProtector.Unprotect(r));
                }
                if (!string.IsNullOrWhiteSpace(s))
                {
                    decryptedHarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
                    model.EncryptedHarvestYear = s;
                }
                if (decryptedFieldId > 0 && decryptedHarvestYear > 0)
                {
                    (recommendations, error) = await _cropService.FetchRecommendationByFieldIdAndYear(decryptedFieldId, decryptedHarvestYear);
                    if (error == null)
                    {
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
                        foreach (var recommendation in recommendations)
                        {
                            var crop = new CropViewModel
                            {
                                ID=recommendation.Crops.ID,                                
                                Year = recommendation.Crops.Year,
                                CropTypeID = recommendation.Crops.CropTypeID,
                                FieldID = recommendation.Crops.FieldID,
                                Variety = recommendation.Crops.Variety,
                                CropInfo1 = recommendation.Crops.CropInfo1,
                                CropInfo2 = recommendation.Crops.CropInfo2,
                                Yield = recommendation.Crops.Yield,
                                SowingDate = recommendation.Crops.SowingDate,
                                CropTypeName = await _fieldService.FetchCropTypeById(recommendation.Crops.CropTypeID.Value),
                                CropInfo1Name = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(recommendation.Crops.CropTypeID.Value, recommendation.Crops.CropInfo1.Value)
                            };
                            model.FieldName = (await _fieldService.FetchFieldByFieldId(recommendation.Crops.FieldID.Value)).Name;
                            List<CropTypeResponse> cropTypeResponseList = (await _fieldService.FetchAllCropTypes());
                            if (cropTypeResponseList != null)
                            {
                                CropTypeResponse cropTypeResponse = cropTypeResponseList.Where(x => x.CropTypeId == crop.CropTypeID).FirstOrDefault();
                                if (cropTypeResponse != null)
                                {
                                    model.CropGroupID = cropTypeResponse.CropGroupId;
                                }
                            }
                            if (model.CropGroupID == (int)NMP.Portal.Enums.CropGroup.Cereals)
                            {
                                crop.CropInfo2Name = await _cropService.FetchCropInfo2NameByCropInfo2Id(crop.CropInfo2.Value);
                            }

                            model.Crops.Add(crop);

                            
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
                                        ID=recData.Recommendation.ID,
                                        ManagementPeriodID=recData.Recommendation.ManagementPeriodID,
                                        CropN = recData.Recommendation.CropN.Value,
                                        CropP2O5 = recData.Recommendation.CropP2O5,
                                        CropK2O = recData.Recommendation.CropK2O,
                                        CropSO3 = recData.Recommendation.CropSO3,
                                        CropLime = recData.Recommendation.CropLime,
                                        SNSIndex = recData.Recommendation.SNSIndex,
                                        SIndex = recData.Recommendation.SIndex,
                                        KIndex = recData.Recommendation.KIndex,
                                        MgIndex = recData.Recommendation.MgIndex,
                                        PIndex = recData.Recommendation.PIndex,
                                        NaIndex = recData.Recommendation.NaIndex
                                    };
                                    model.Recommendations.Add(rec);


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

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorOnHarvestYearOverview"] = string.Concat(error != null ? error.Message : "", ex.Message);
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = q,
                    year = s
                });
            }
            return View(model);
        }
    }
}
