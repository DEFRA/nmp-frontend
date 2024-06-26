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
using Newtonsoft.Json;

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
        private readonly ICropService _cropService;
        private readonly IFieldService _fieldService;

        public OrganicManureController(ILogger<OrganicManureController> logger, IDataProtectionProvider dataProtectionProvider,
              IHttpContextAccessor httpContextAccessor, IOrganicManureService organicManureService, IFarmService farmService, ICropService cropService, IFieldService fieldService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _organicManureService = organicManureService;
            _farmService = farmService;
            _cropService = cropService;
            _fieldService = fieldService;
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
                else if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if ((!string.IsNullOrWhiteSpace(q)) && (!string.IsNullOrWhiteSpace(r)))
                {
                    model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                    model.EncryptedFarmId = q;
                    model.EncryptedHarvestYear = r;
                    model.IsSingleField = false;
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
                    if (cropTypeList.Count > 1)
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
                    if (cropTypeList.Count == 1)
                    {
                        model.FieldGroup = "Select specific fields";

                        (List<OrganicManureFieldResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                        if (error == null)
                        {
                            if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                            {
                                if (fieldList.Count == 1)
                                {

                                    var SelectListItem = fieldList.Select(f => new SelectListItem
                                    {
                                        Value = f.FieldId.ToString(),
                                        Text = f.FieldName.ToString()
                                    }).ToList();
                                    //ViewBag.FieldList = SelectListItem;
                                    //code sk

                                    model.FieldList = SelectListItem.Select(item => item.Value).ToList();

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
                                    model.IsSingleField = true;
                                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

                                    return RedirectToAction("ManureGroup");

                                }
                                return View(model);
                            }

                        }
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
                else
                {
                    return RedirectToAction("FarmList", "Farm");
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
                        ModelState.AddModelError("FieldList", Resource.MsgSelectAtLeastOneField);
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            try
            {
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
                ModelState.AddModelError("ManureGroupId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            Error error = null;
            try
            {
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


                (CommonResponse manureGroup, error) = await _organicManureService.FetchManureGroupById(model.ManureGroupId.Value);
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
                ModelState.AddModelError("ManureTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
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
                            if (manureType.MgO != null)
                            {
                                orgManure.MgO = manureType.MgO.Value;
                            }
                            orgManure.P2O5 = manureType.P2O5.Value;
                            if (manureType.SO3 != null)
                            {
                                orgManure.SO3 = manureType.SO3.Value;
                            }
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

            OrganicManureViewModel organicManureViewModel = JsonConvert.DeserializeObject<OrganicManureViewModel>(HttpContext.Session.GetString("OrganicManure"));
            if (organicManureViewModel != null)
            {
                if (organicManureViewModel.ManureTypeId != model.ManureTypeId)
                {
                    model.DryMatterPercent = null;
                    model.N = null;
                    model.P2O5 = null;
                    model.NH4N = null;
                    model.UricAcid = null;
                    model.SO3 = null;
                    model.K2O = null;
                    model.MgO = null;
                    model.NO3N = null;
                }
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                model.ManureTypeName = manureType.Name;
                ViewBag.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
            }
            else
            {
                model.ManureTypeName = string.Empty;
            }

            (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
            model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["Error"] = error.Message;
            }
            if (farm != null)
            {
                //Closed Period for non organic farm
                if (!farm.RegisteredOrganicProducer.Value)
                {
                    (FieldDetailResponse fieldDetail, Error error2) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                    DateTime september16 = new DateTime(model.HarvestYear ?? 0, 9, 16);

                    if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass)
                    {
                        ViewBag.ClosedPeriod = Resource.lbl1Septo31Dec;
                    }
                    else if (fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass)
                    {
                        ViewBag.ClosedPeriod = Resource.lbl15Octto15Jan;
                    }
                    else if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable && fieldDetail.SowingDate >= september16)
                    {
                        ViewBag.ClosedPeriod = Resource.lbl1Augto31Dec;
                    }
                    else if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable && fieldDetail.SowingDate < september16)
                    {
                        ViewBag.ClosedPeriod = Resource.lbl16Septo31Dec;
                    }
                    else if (fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable)
                    {
                        ViewBag.ClosedPeriod = Resource.lbl1Octto15Jan;
                    }
                }

                //Closed period for organic farm need to work
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureApplyingDate(OrganicManureViewModel model)
        {
            if (model.ApplicationDate == null)
            {
                ModelState.AddModelError("ApplicationDate", Resource.MsgEnterADateBeforeContinuing);
            }
            if (model.ApplicationDate != null)
            {
                if (model.ApplicationDate.Value.Date.Year > model.HarvestYear)
                {
                    ModelState.AddModelError("ApplicationDate", Resource.MsgDateCannotBeLaterThanHarvestYear);
                }
            }

            if (!ModelState.IsValid)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["Error"] = error.Message;
                }
                if (farm != null)
                {
                    //Closed Period for non organic farm
                    if (!farm.RegisteredOrganicProducer.Value)
                    {
                        (FieldDetailResponse fieldDetail, Error error2) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                        DateTime september16 = new DateTime(model.HarvestYear ?? 0, 9, 16);

                        if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass)
                        {
                            ViewBag.ClosedPeriod = Resource.lbl1Septo31Dec;
                        }
                        else if (fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass)
                        {
                            ViewBag.ClosedPeriod = Resource.lbl15Octto15Jan;
                        }
                        else if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable && fieldDetail.SowingDate >= september16)
                        {
                            ViewBag.ClosedPeriod = Resource.lbl1Augto31Dec;
                        }
                        else if ((fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow) && fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable && fieldDetail.SowingDate < september16)
                        {
                            ViewBag.ClosedPeriod = Resource.lbl16Septo31Dec;
                        }
                        else if (fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable)
                        {
                            ViewBag.ClosedPeriod = Resource.lbl1Octto15Jan;
                        }
                    }

                    //Closed period for organic farm need to work
                }
                return View(model);
            }
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AppDate = model.ApplicationDate.Value;
                }
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);


            return RedirectToAction("ApplicationMethod");
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationMethod()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            bool isLiquid = false;
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                model.ManureTypeName = manureType?.Name;
                isLiquid = manureType.IsLiquid.Value;

            }
            else
            {
                model.ManureTypeName = string.Empty;
            }
            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

            string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
            (List<ApplicationMethodResponse> applicationMethodList, Error error1) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, applicableFor);
            ViewBag.ApplicationMethodList = applicationMethodList;
            model.ApplicationMethodCount = applicationMethodList.Count;
            if (applicationMethodList.Count == 1)
            {
                model.ApplicationMethod = applicationMethodList[0].ID;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                if (model.IsDefaultNutrient.Value)
                {
                    return RedirectToAction("ManureApplyingDate");
                }
                return RedirectToAction("DefaultNutrientValues");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationMethod(OrganicManureViewModel model)
        {
            if (model.ApplicationMethod == null)
            {
                ModelState.AddModelError("ApplicationMethod", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isLiquid = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    model.ManureTypeName = manureType?.Name;
                    isLiquid = manureType.IsLiquid.Value;

                }
                else
                {
                    model.ManureTypeName = string.Empty;
                }
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
                (List<ApplicationMethodResponse> applicationMethodList, Error error1) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, applicableFor);
                ViewBag.ApplicationMethodList = applicationMethodList;
                model.ApplicationMethodCount = applicationMethodList.Count;
                return View(model);
            }

            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.ApplicationMethodID = model.ApplicationMethod.Value;
                }
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            model.ManureType = manureType;
            model.IsDefaultNutrient = true;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefaultNutrientValues(OrganicManureViewModel model)
        {
            if (model.IsDefaultNutrientValues == null)
            {
                ModelState.AddModelError("IsDefaultNutrientValues", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                model.ManureType = manureType;

                return View(model);
            }
            if (!model.IsDefaultNutrientValues.Value)
            {
                if (model.DryMatterPercent == null)
                {
                    model.DryMatterPercent = model.ManureType.DryMatter;
                    model.N = model.ManureType.TotalN;
                    model.P2O5 = model.ManureType.P2O5;
                    model.NH4N = model.ManureType.NH4N;
                    model.UricAcid = model.ManureType.Uric;
                    model.SO3 = model.ManureType.SO3;
                    model.K2O = model.ManureType.K2O;
                    model.MgO = model.ManureType.MgO;
                    model.NO3N = model.ManureType.NO3N;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                }
                return RedirectToAction("ManualNutrientValues");
            }
            else
            {
                model.DryMatterPercent = null;
                model.N = null;
                model.P2O5 = null;
                model.NH4N = null;
                model.UricAcid = null;
                model.SO3 = null;
                model.K2O = null;
                model.MgO = null;
                model.NO3N = null;
                if (model.OrganicManures.Count > 0)
                {
                    foreach (var orgManure in model.OrganicManures)
                    {
                        orgManure.DryMatterPercent = model.ManureType.DryMatter;
                        orgManure.N = model.ManureType.TotalN;
                        orgManure.NH4N = model.ManureType.NH4N;
                        orgManure.UricAcid = model.ManureType.Uric;
                        orgManure.NO3N = model.ManureType.NO3N;
                        orgManure.P2O5 = model.ManureType.P2O5;
                        orgManure.K2O = model.ManureType.K2O;
                        orgManure.SO3 = model.ManureType.SO3;
                        orgManure.MgO = model.ManureType.MgO;
                    }
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            return RedirectToAction("ApplicationRateMethod");
        }

        [HttpGet]
        public async Task<IActionResult> ManualNutrientValues()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualNutrientValues(OrganicManureViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("DryMatterPercent"))
            {
                var dryMatterPercentError = ModelState["DryMatterPercent"].Errors.Count > 0 ?
                                ModelState["DryMatterPercent"].Errors[0].ErrorMessage.ToString() : null;

                if (dryMatterPercentError != null && dryMatterPercentError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["DryMatterPercent"].RawValue, Resource.lblDryMatterPercent)))
                {
                    ModelState["DryMatterPercent"].Errors.Clear();
                    ModelState["DryMatterPercent"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblDryMatter));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("N"))
            {
                var totalNitrogenError = ModelState["N"].Errors.Count > 0 ?
                                ModelState["N"].Errors[0].ErrorMessage.ToString() : null;

                if (totalNitrogenError != null && totalNitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["N"].RawValue, Resource.lblN)))
                {
                    ModelState["N"].Errors.Clear();
                    ModelState["N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalNitrogen));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("NH4N"))
            {
                var ammoniumError = ModelState["NH4N"].Errors.Count > 0 ?
                                ModelState["NH4N"].Errors[0].ErrorMessage.ToString() : null;

                if (ammoniumError != null && ammoniumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["NH4N"].RawValue, Resource.lblNH4N)))
                {
                    ModelState["NH4N"].Errors.Clear();
                    ModelState["NH4N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmmonium));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("UricAcid"))
            {
                var uricAcidError = ModelState["UricAcid"].Errors.Count > 0 ?
                                ModelState["UricAcid"].Errors[0].ErrorMessage.ToString() : null;

                if (uricAcidError != null && uricAcidError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["UricAcid"].RawValue, Resource.lblUricAcidForError)))
                {
                    ModelState["UricAcid"].Errors.Clear();
                    ModelState["UricAcid"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblUricAcid));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("NO3N"))
            {
                var nitrogenError = ModelState["NO3N"].Errors.Count > 0 ?
                                ModelState["NO3N"].Errors[0].ErrorMessage.ToString() : null;

                if (nitrogenError != null && nitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["NO3N"].RawValue, Resource.lblNO3N)))
                {
                    ModelState["NO3N"].Errors.Clear();
                    ModelState["NO3N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblNitrogen));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("P2O5"))
            {
                var totalPhosphateError = ModelState["P2O5"].Errors.Count > 0 ?
                                ModelState["P2O5"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPhosphateError != null && totalPhosphateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["P2O5"].RawValue, Resource.lblP2O5)))
                {
                    ModelState["P2O5"].Errors.Clear();
                    ModelState["P2O5"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalPhosphate));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("K2O"))
            {
                var totalPotassiumError = ModelState["K2O"].Errors.Count > 0 ?
                                ModelState["K2O"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPotassiumError != null && totalPotassiumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["K2O"].RawValue, Resource.lblK2O)))
                {
                    ModelState["K2O"].Errors.Clear();
                    ModelState["K2O"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalPotassium));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SO3"))
            {
                var sulphurSO3Error = ModelState["SO3"].Errors.Count > 0 ?
                                ModelState["SO3"].Errors[0].ErrorMessage.ToString() : null;

                if (sulphurSO3Error != null && sulphurSO3Error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SO3"].RawValue, Resource.lblSO3)))
                {
                    ModelState["SO3"].Errors.Clear();
                    ModelState["SO3"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalSulphur));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("MgO"))
            {
                var totalMagnesiumOxideError = ModelState["MgO"].Errors.Count > 0 ?
                                ModelState["MgO"].Errors[0].ErrorMessage.ToString() : null;

                if (totalMagnesiumOxideError != null && totalMagnesiumOxideError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["MgO"].RawValue, Resource.lblMgO)))
                {
                    ModelState["MgO"].Errors.Clear();
                    ModelState["MgO"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalMagnesiumOxide));
                }
            }
            if (model.DryMatterPercent == null)
            {
                ModelState.AddModelError("DryMatterPercent", string.Format(Resource.lblEnterValidValue, Resource.lblDryMatter));
            }
            if (model.N == null)
            {
                ModelState.AddModelError("N", string.Format(Resource.lblEnterValidValue, Resource.lblTotalNitrogen));
            }
            if (model.NH4N == null)
            {
                ModelState.AddModelError("NH4N", string.Format(Resource.lblEnterValidValue, Resource.lblAmmonium));
            }
            if (model.UricAcid == null)
            {
                ModelState.AddModelError("UricAcid", string.Format(Resource.lblEnterValidValue, Resource.lblUricAcid));
            }
            if (model.NO3N == null)
            {
                ModelState.AddModelError("NO3N", string.Format(Resource.lblEnterValidValue, Resource.lblNitrogen));
            }
            if (model.P2O5 == null)
            {
                ModelState.AddModelError("P2O5", string.Format(Resource.lblEnterValidValue, Resource.lblTotalPhosphate));
            }
            if (model.K2O == null)
            {
                ModelState.AddModelError("K2O", string.Format(Resource.lblEnterValidValue, Resource.lblTotalPotassium));
            }
            if (model.SO3 == null)
            {
                ModelState.AddModelError("SO3", string.Format(Resource.lblEnterValidValue, Resource.lblSulphurSO3));
            }
            if (model.MgO == null)
            {
                ModelState.AddModelError("MgO", string.Format(Resource.lblEnterValidValue, Resource.lblTotalMagnesiumOxide));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.DryMatterPercent = model.DryMatterPercent;
                    orgManure.N = model.N;
                    orgManure.NH4N = model.NH4N;
                    orgManure.UricAcid = model.UricAcid;
                    orgManure.NO3N = model.NO3N;
                    orgManure.P2O5 = model.P2O5;
                    orgManure.K2O = model.K2O;
                    orgManure.SO3 = model.SO3;
                    orgManure.MgO = model.MgO;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);


            return RedirectToAction("ApplicationRateMethod");
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationRateMethod()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            if (error == null && manureTypeList.Count > 0)
            {
                model.ManureTypeName = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name;
                model.ApplicationRateArable = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.ApplicationRateArable;
            }
            else
            {
                model.ManureTypeName = string.Empty;
            }

            (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
            model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(OrganicManureViewModel model)
        {
            if (model.ApplicationRateMethod == null)
            {
                ModelState.AddModelError("ApplicationRate", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);

                if (error == null && manureTypeList.Count > 0)
                {
                    model.ManureTypeName = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name;
                    model.ApplicationRateArable = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.ApplicationRateArable;
                }
                else
                {
                    model.ManureTypeName = string.Empty;
                }

                (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
                model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
                return View("ApplicationRateMethod", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            if (model.ApplicationRateMethod.Value == 0)
            {
                return RedirectToAction("ManualApplicationRate");
            }
            else if (model.ApplicationRateMethod.Value == 1)
            {
                return RedirectToAction("AreaQuantity");
            }

            return RedirectToAction("IncorporationMethod");
        }



        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

            (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
            model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualApplicationRate(OrganicManureViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationRate"))
            {
                var applicationRateError = ModelState["ApplicationRate"].Errors.Count > 0 ?
                                ModelState["ApplicationRate"].Errors[0].ErrorMessage.ToString() : null;

                if (applicationRateError != null && applicationRateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationRate"].RawValue, Resource.lblApplicationRate)))
                {
                    ModelState["ApplicationRate"].Errors.Clear();
                    ModelState["ApplicationRate"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.MsgApplicationRate));
                }
            }

            if (model.ApplicationRate == null)
            {
                ModelState.AddModelError("ApplicationRate", Resource.MsgEnterAnapplicationRateBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("ManualApplicationRate", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            return RedirectToAction("IncorporationMethod");
        }

        [HttpGet]
        public async Task<IActionResult> AreaQuantity()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AreaQuantity(OrganicManureViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Area"))
            {
                var areaError = ModelState["Area"].Errors.Count > 0 ?
                                ModelState["Area"].Errors[0].ErrorMessage.ToString() : null;

                if (areaError != null && areaError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Area"].RawValue, Resource.lblAreas)))
                {
                    ModelState["Area"].Errors.Clear();
                    ModelState["Area"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Quantity"))
            {
                var quantityError = ModelState["Quantity"].Errors.Count > 0 ?
                                ModelState["Quantity"].Errors[0].ErrorMessage.ToString() : null;

                if (quantityError != null && quantityError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Quantity"].RawValue, Resource.lblQuantity)))
                {
                    ModelState["Quantity"].Errors.Clear();
                    ModelState["Quantity"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.MsgQuantity));
                }
            }

            if (model.Area == null)
            {
                ModelState.AddModelError("Area", Resource.MsgEnterAValidArea);
            }
            if (model.Quantity == null)
            {
                ModelState.AddModelError("Quantity", Resource.MsgEnterAValidQuantity);
            }
            if (!ModelState.IsValid)
            {
                return View("AreaQuantity", model);
            }
            model.ApplicationRate = model.Quantity / model.Area;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            return RedirectToAction("IncorporationMethod");
        }
        [HttpGet]
        public async Task<IActionResult> IncorporationMethod()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if ((model.ApplicationMethod== (int)NMP.Portal.Enums.ApplicationMethod.DeepInjection2530cm) || (model.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.ShallowInjection57cm))
            {
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            bool isLiquid = false;
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                isLiquid = manureType.IsLiquid.Value;

            }

            string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;

            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

            (List<IncorporationMethodResponse> incorporationMethods, Error error1) = await _organicManureService.FetchIncorporationMethodsByApplicationId(fieldType ?? 0, applicableFor, model.ApplicationMethod ?? 0);

            ViewBag.IncorporationMethod = incorporationMethods;
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationMethod(OrganicManureViewModel model)
        {
            if (model.IncorporationMethod == null)
            {
                ModelState.AddModelError("IncorporationMethod", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isLiquid = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isLiquid = manureType.IsLiquid.Value;

                }

                string applicableFor = isLiquid ? Resource.lblL :Resource.lblB;
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                (List<IncorporationMethodResponse> incorporationMethods, Error error1) = await _organicManureService.FetchIncorporationMethodsByApplicationId(fieldType ?? 0, applicableFor, model.ApplicationMethod ?? 0);

                ViewBag.IncorporationMethod = incorporationMethods;
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IncorporationMethod == (int)NMP.Portal.Enums.IncorporationMethod.NotIncorporated)
            {
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            return RedirectToAction("IncorporationDelay");

        }

        [HttpGet]
        public async Task<IActionResult> IncorporationDelay()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            string applicableFor = string.Empty;
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            bool isLiquid = false;
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                isLiquid = manureType.IsLiquid.Value;
                applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                if (manureType.Id == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                {
                    applicableFor = Resource.lblP;
                }
            }

            (List<IncorprationDelaysResponse> incorporationDelaysList, Error error1) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
            ViewBag.IncorporationDelaysList = incorporationDelaysList;
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationDelay(OrganicManureViewModel model)
        {
            if (model.IncorporationDelay == null)
            {
                ModelState.AddModelError("IncorporationDelay", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                string applicableFor = string.Empty;
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isLiquid = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isLiquid = manureType.IsLiquid.Value;
                    applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                    if (manureType.Id == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                    {
                        applicableFor = Resource.lblP;
                    }
                }

                (List<IncorprationDelaysResponse> incorporationDelaysList, Error error1) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                ViewBag.IncorporationDelaysList = incorporationDelaysList;
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            return RedirectToAction("ConditionsAffectingNutrients");

        }

        [HttpGet]
        public IActionResult ConditionsAffectingNutrients()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }
    }
}
