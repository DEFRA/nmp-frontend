﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using System.Reflection;
using NMP.Portal.Resources;
using NMP.Portal.Helpers;
using NMP.Portal.ViewModels;
using System.Diagnostics.Metrics;
using NMP.Portal.Enums;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class OrganicManureController : Controller
    {
        private readonly ILogger<OrganicManureController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrganicManureService _organicManureService;
        private readonly IFarmService _farmService;

        public OrganicManureController(ILogger<OrganicManureController> logger, IDataProtectionProvider dataProtectionProvider,
              IHttpContextAccessor httpContextAccessor, IOrganicManureService organicManureService, IFarmService farmService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _organicManureService = organicManureService;
            _farmService = farmService;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult CreateManureCancel(string q, string r)
        {
            _httpContextAccessor.HttpContext?.Session.Remove("OrganicManure");
            return RedirectToAction("HarvestYearOverview", "Crop", new { Id = q, year = r });
        }

        [HttpGet]
        public async Task<IActionResult> FieldGroup(string q, string r)
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }
                if ((!string.IsNullOrWhiteSpace(q)) && (!string.IsNullOrWhiteSpace(r)))
                {
                    model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                    model.EncryptedFarmId = q;
                    model.EncryptedHarvestYear = r;
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (error == null)
                    {
                        model.isEnglishRules = farm.EnglishRules;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                    else
                    {
                        TempData["FieldGroupError"] = error.Message;
                    }

                }
                (List<OrganicManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                if (error == null)
                {
                    if (cropTypeList.Count > 0)
                    {
                        var SelectListItem = cropTypeList.Select(f => new SelectListItem
                        {
                            Value = f.CropTypeId.ToString(),
                            Text = string.Format(Resource.lblTheCropTypeField, f.CropType.ToString())
                        }).ToList();
                        SelectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
                        SelectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });
                        ViewBag.FieldGroupList = SelectListItem;
                    }
                }
                else
                {
                    TempData["FieldGroupError"] = error.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["FieldGroupError"] = ex.Message;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldGroup(OrganicManureViewModel model)
        {
            Error error = null;
            if (model.FieldGroup == null)
            {
                ModelState.AddModelError("FieldGroup", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            try
            {
                if (!ModelState.IsValid)
                {
                    (List<OrganicManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                    if (error == null)
                    {
                        if (cropTypeList.Count > 0)
                        {

                            var SelectListItem = cropTypeList.Select(f => new SelectListItem
                            {
                                Value = f.CropTypeId.ToString(),
                                Text = string.Format(Resource.lblTheCropTypeField, f.CropType.ToString())
                            }).ToList();
                            SelectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
                            SelectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });
                            ViewBag.FieldGroupList = SelectListItem;
                        }
                    }
                    else
                    {
                        TempData["FieldGroupError"] = error.Message;
                    }
                    return View(model);
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            }
            catch (Exception ex)
            {
                TempData["FieldGroupError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("Fields");

        }

        [HttpGet]
        public async Task<IActionResult> Fields()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }
                (List<OrganicManureFieldResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                    {
                        if (fieldList.Count > 0)
                        {

                            var SelectListItem = fieldList.Select(f => new SelectListItem
                            {
                                Value = f.FieldId.ToString(),
                                Text = f.FieldName.ToString()
                            }).ToList();
                            ViewBag.FieldList = SelectListItem;
                        }
                        return View(model);
                    }
                    else
                    {
                        if (fieldList.Count > 0)
                        {
                            model.FieldList = fieldList.Select(x => x.FieldId.ToString()).ToList();
                            string fieldIds = string.Join(",", model.FieldList);
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    if (model.OrganicManures == null)
                                    {
                                        model.OrganicManures = new List<OrganicManure>();
                                    }
                                    if (model.OrganicManures.Count > 0)
                                    {
                                        model.OrganicManures.Clear();
                                    }
                                    foreach (var manIds in managementIds)
                                    {
                                        var organicManure = new OrganicManure
                                        {
                                            ManagementPeriodID = manIds
                                        };
                                        model.OrganicManures.Add(organicManure);
                                    }
                                }
                            }
                            else
                            {
                                TempData["FieldGroupError"] = error.Message;
                                return View("FieldGroup", model);
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return RedirectToAction("ManureGroup");
                    }
                }
                else
                {
                    TempData["FieldGroupError"] = error.Message;
                    return View("FieldGroup", model);
                }
            }
            catch (Exception ex)
            {
                TempData["FieldGroupError"] = ex.Message;
                return RedirectToAction("FieldGroup", model);
            }
            //return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fields(OrganicManureViewModel model)
        {
            Error error = null;
            try
            {
                (List<OrganicManureFieldResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    var selectListItem = fieldList.Select(f => new SelectListItem
                    {
                        Value = f.FieldId.ToString(),
                        Text = f.FieldName.ToString()
                    }).ToList();
                    ViewBag.FieldList = selectListItem;

                    if (model.FieldList == null || model.FieldList.Count == 0)
                    {
                        ModelState.AddModelError("FieldList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
                    }
                    if (!ModelState.IsValid)
                    {

                        return View(model);

                    }
                    if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblSelectAll)
                    {
                        model.FieldList = selectListItem.Select(item => item.Value).ToList();
                    }
                    string fieldIds = string.Join(",", model.FieldList);
                    (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                    if (error == null)
                    {
                        if (managementIds.Count > 0)
                        {
                            if (model.OrganicManures == null)
                            {
                                model.OrganicManures = new List<OrganicManure>();
                            }
                            if (model.OrganicManures.Count > 0)
                            {
                                model.OrganicManures.Clear();
                            }
                            foreach (var manIds in managementIds)
                            {
                                var organicManure = new OrganicManure
                                {
                                    ManagementPeriodID = manIds
                                };
                                model.OrganicManures.Add(organicManure);
                            }
                        }
                    }
                    else
                    {
                        TempData["FieldError"] = error.Message;
                        return View(model);
                    }
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                }
                else
                {
                    TempData["FieldError"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["FieldError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("ManureGroup");

        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            try { 
            (List<CommonResponse> manureGroupList, Error error) = await _organicManureService.FetchManureGroupList();
            if (error == null)
            {
                if (manureGroupList.Count > 0)
                {

                    var SelectListItem = manureGroupList.Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Name.ToString()
                    }).ToList();
                    ViewBag.ManureGroupList = SelectListItem;
                }
            }
            else
            {
                TempData["FieldError"] = error.Message;
                return RedirectToAction("Fields", model);
            }
            }
            catch (Exception ex)
            {
                TempData["FieldError"] = ex.Message;
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureGroup(OrganicManureViewModel model)
        {
            if (model.ManureGroupId == null)
            {
                ModelState.AddModelError("ManureGroup", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            Error error = null;
            try { 
            if (!ModelState.IsValid)
            {
                (List<CommonResponse> manureGroupList, error) = await _organicManureService.FetchManureGroupList();
                if (error == null)
                {

                    if (manureGroupList.Count > 0)
                    {

                        var SelectListItem = manureGroupList.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).ToList();
                        ViewBag.ManureGroupList = SelectListItem;
                    }
                }
                else
                {
                    TempData["ManureGroupError"] = error.Message;
                }
                return View(model);

            }


            (CommonResponse manureGroup,error) = await _organicManureService.FetchManureGroupById(model.ManureGroupId.Value);
            if (error == null)
            {
                if (manureGroup != null)
                {
                    model.ManureGroupName = manureGroup.Name;
                }
            }
            else
            {
                TempData["ManureGroupError"] = error.Message;
                return View(model);
            }
            }
            catch (Exception ex)
            {
                TempData["ManureGroupError"] = ex.Message;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ManureType");

        }

        [HttpGet]
        public async Task<IActionResult> ManureType()
        {
            Error error = null;
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            try
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                if (error == null)
                {
                    if (manureTypeList.Count > 0)
                    {

                        var SelectListItem = manureTypeList.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).ToList();
                        ViewBag.ManureTypeList = SelectListItem;
                    }
                    return View(model);
                }
                else
                {
                    TempData["ManureGroupError"] = error.Message;
                    return RedirectToAction("ManureGroup", model);
                }
            }
            catch (Exception ex)
            {
                TempData["ManureGroupError"] = ex.Message;
                return RedirectToAction("ManureGroup", model);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureType(OrganicManureViewModel model)
        {
            Error error = null;
            if (model.ManureTypeId == null)
            {
                ModelState.AddModelError("ManureType", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            try
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                if (error == null)
                {
                    if (!ModelState.IsValid)
                    {
                        if (manureTypeList.Count > 0)
                        {

                            var SelectListItem = manureTypeList.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name.ToString()
                            }).ToList();
                            ViewBag.ManureTypeList = SelectListItem;

                        }
                        return View(model);

                    }

                    ManureType manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    if (manureType != null)
                    {
                        model.ManureTypeName = manureType.Name;
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.ManureTypeID = model.ManureTypeId.Value;
                            orgManure.K2O = manureType.K2O.Value;
                            orgManure.MgO = manureType.MgO==null?0:manureType.MgO.Value;
                            orgManure.P2O5 = manureType.P2O5.Value;
                            orgManure.SO3 = manureType.SO3 == null ? 0 : manureType.SO3.Value;
                            orgManure.NH4N = manureType.NH4N.Value;
                            orgManure.NO3N = manureType.NO3N.Value;
                            orgManure.UricAcid = manureType.Uric.Value;
                            orgManure.DryMatterPercent = manureType.DryMatter.Value;
                            orgManure.N = manureType.TotalN.Value;
                        }
                    }
                }
                else
                {
                    TempData["ManureTypeError"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["ManureTypeError"] = ex.Message;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ManureApplyingDate");

        }
        [HttpGet]
        public async Task<IActionResult> ManureApplyingDate()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            model.ManureTypeName= (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id==model.ManureTypeId)?.Name:string.Empty;

            (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
            model.ManureGroupName=(error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
            
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManureApplyingDate(OrganicManureViewModel model)
        {
            if (model.ApplicationDate == null)
            {
                ModelState.AddModelError("ApplicationDate", Resource.MsgEnterADateBeforeContinuing);
            }
            if (model.ApplicationDate != null)
            {
                if (model.ApplicationDate.Value.Date.Year < 1601 || model.ApplicationDate.Value.Date.Year > DateTime.Now.AddYears(1).Year)
                {
                    ModelState.AddModelError("ApplicationDate", Resource.MsgEnterTheDateInNumber);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);


            return RedirectToAction("DefaultNutrientValues");
        }

        [HttpGet]
        public async Task<IActionResult> DefaultNutrientValues()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(2);
            model.ManureType = manureType;
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DefaultNutrientValues(OrganicManureViewModel model)
        {
            if(model.IsDefaultNutrientValues==null)
            {
                ModelState.AddModelError("IsDefaultNutrientValues", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);


            return RedirectToAction("DefaultNutrientValues");
        }
    }
}
