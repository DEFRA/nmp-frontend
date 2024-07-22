using Microsoft.AspNetCore.Authorization;
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
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Microsoft.IdentityModel.Abstractions;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.VisualBasic.FileIO;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class OrganicManureController : Controller
    {
        private readonly ILogger<OrganicManureController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IDataProtector _organicManureProtector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrganicManureService _organicManureService;
        private readonly IFarmService _farmService;
        private readonly ICropService _cropService;
        private readonly IFieldService _fieldService;
        private readonly IMannerService _mannerService;

        public OrganicManureController(ILogger<OrganicManureController> logger, IDataProtectionProvider dataProtectionProvider,
              IHttpContextAccessor httpContextAccessor, IOrganicManureService organicManureService, IFarmService farmService, ICropService cropService, IFieldService fieldService, IMannerService mannerService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _organicManureProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.OrganicManureController");
            _organicManureService = organicManureService;
            _farmService = farmService;
            _cropService = cropService;
            _fieldService = fieldService;
            _mannerService = mannerService;
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
        public async Task<IActionResult> FieldGroup(string q, string r, string? s)//q=FarmId,r=harvestYear,s=fieldId
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
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (error.Message == null)
                    {
                        model.FarmName = farm.Name;
                        model.isEnglishRules = farm.EnglishRules;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                    else
                    {
                        TempData["FieldGroupError"] = error.Message;
                    }
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        model.FieldList = new List<string>();
                        model.FieldGroup = Resource.lblSelectSpecificFields;
                        model.FieldGroupName = Resource.lblSelectSpecificFields;
                        model.IsComingFromRecommendation = true;
                        string fieldId = _cropDataProtector.Unprotect(s);
                        model.FieldList.Add(fieldId);
                        (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                            TempData["NutrientRecommendationsError"] = error.Message;
                            return RedirectToAction("Recommendations", "Crop", new { q = q, r = s, s = r });
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return RedirectToAction("ManureGroup");
                    }


                }
                (List<OrganicManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                if (error == null)
                {
                    //if (cropTypeList.Count > 1)
                    //{
                    var SelectListItem = cropTypeList.Select(f => new SelectListItem
                    {
                        Value = f.CropTypeId.ToString(),
                        Text = string.Format(Resource.lblTheCropTypeField, f.CropType.ToString())
                    }).ToList();
                    SelectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
                    SelectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });
                    ViewBag.FieldGroupList = SelectListItem;
                    //}
                    //if (cropTypeList.Count == 1)
                    //{
                    //    model.FieldGroup = "Select specific fields";

                    //    (List<OrganicManureFieldResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                    //    if (error == null)
                    //    {
                    //        if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                    //        {
                    //            if (fieldList.Count == 1)
                    //            {

                    //                var SelectListItem = fieldList.Select(f => new SelectListItem
                    //                {
                    //                    Value = f.FieldId.ToString(),
                    //                    Text = f.FieldName.ToString()
                    //                }).ToList();
                    //                //ViewBag.FieldList = SelectListItem;
                    //                //code sk

                    //                model.FieldList = SelectListItem.Select(item => item.Value).ToList();

                    //                string fieldIds = string.Join(",", model.FieldList);
                    //                (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                    //                if (error == null)
                    //                {
                    //                    if (managementIds.Count > 0)
                    //                    {
                    //                        if (model.OrganicManures == null)
                    //                        {
                    //                            model.OrganicManures = new List<OrganicManure>();
                    //                        }
                    //                        if (model.OrganicManures.Count > 0)
                    //                        {
                    //                            model.OrganicManures.Clear();
                    //                        }
                    //                        foreach (var manIds in managementIds)
                    //                        {
                    //                            var organicManure = new OrganicManure
                    //                            {
                    //                                ManagementPeriodID = manIds
                    //                            };
                    //                            model.OrganicManures.Add(organicManure);
                    //                        }
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    TempData["FieldError"] = error.Message;
                    //                    return View(model);
                    //                }
                    //                model.IsSingleField = true;
                    //                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

                    //                return RedirectToAction("ManureGroup");

                    //            }
                    //            return View(model);
                    //        }

                    //    }
                    //}
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
                (List<OrganicManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                if (!ModelState.IsValid)
                {
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

                if (int.TryParse(model.FieldGroup, out int value))
                {
                    model.CropTypeName = cropTypeList.FirstOrDefault(x => x.CropTypeId == value).CropType;
                    model.FieldGroupName = string.Format(Resource.lblTheCropTypeField, model.CropTypeName);
                }
                else
                {
                    if (model.FieldGroup.Equals(Resource.lblAll))
                    {
                        model.FieldGroupName = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear);
                    }
                    else
                    {
                        model.FieldGroupName = model.FieldGroup;
                    }
                }
                model.IsComingFromRecommendation = false;
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
                            ViewBag.FieldList = SelectListItem.OrderBy(x => x.Text).ToList();
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
                    ViewBag.FieldList = selectListItem.OrderBy(x => x.Text).ToList();

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
                        ViewBag.ManureGroupList = SelectListItem.OrderBy(x => x.Text).ToList();
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
            if (model.ManureGroupIdForFilter == null)
            {
                ModelState.AddModelError("ManureGroupIdForFilter", Resource.MsgSelectAnOptionBeforeContinuing);
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
                            ViewBag.ManureGroupList = SelectListItem.OrderBy(x => x.Text).ToList(); ;
                        }
                    }
                    else
                    {
                        TempData["ManureGroupError"] = error.Message;
                    }
                    return View(model);

                }

                if (model.IsCheckAnswer)
                {
                    model.IsManureTypeChange = true;
                }

                (CommonResponse manureGroup, error) = await _organicManureService.FetchManureGroupById(model.ManureGroupIdForFilter.Value);
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
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupIdForFilter.Value, countryId);
                if (error == null)
                {
                    if (manureTypeList.Count > 0)
                    {

                        var SelectListItem = manureTypeList.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).ToList();
                        ViewBag.ManureTypeList = SelectListItem.OrderBy(x => x.Text).ToList();
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
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupIdForFilter.Value, countryId);
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
                            ViewBag.ManureTypeList = SelectListItem.OrderBy(x => x.Text).ToList(); ;

                        }
                        return View(model);

                    }

                    ManureType manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    if (manureType != null)
                    {
                        model.ManureTypeName = manureType.Name;
                        model.IsManureTypeLiquid = manureType.IsLiquid.Value;
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

            if (model.ManureGroupIdForFilter.HasValue)
            {
                model.ManureGroupId = model.ManureGroupIdForFilter;
            }

            //if manure type change 
            if (model.IsCheckAnswer)
            {
                OrganicManureViewModel organicManure = new OrganicManureViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    organicManure = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (organicManure.ManureTypeId != model.ManureTypeId)
                {
                    if (model.ApplicationRateMethod == (int)NMP.Portal.Enums.ApplicationRate.UseDefaultApplicationRate)
                    {
                        model.ApplicationRateMethod = null;
                        model.ApplicationRate = null;
                    }
                    if (organicManure.IsManureTypeLiquid.Value != model.IsManureTypeLiquid.Value)
                    {
                        model.ApplicationMethod = null;
                        model.IncorporationMethod = null;
                        model.IncorporationDelay = null;
                        model.ApplicationMethodName = string.Empty;
                        model.IncorporationMethodName = string.Empty;
                        model.IncorporationDelayName = string.Empty;
                    }
                    if (!model.IsManureTypeLiquid.Value)
                    {
                        List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                        var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                        string applicableFor = model.IsManureTypeLiquid.Value ? Resource.lblL : Resource.lblB;
                        (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, applicableFor);
                        if (error == null && applicationMethodList.Count > 0)
                        {
                            model.ApplicationMethod = applicationMethodList[0].ID;
                            foreach (var orgManure in model.OrganicManures)
                            {
                                orgManure.ApplicationMethodID = model.ApplicationMethod.Value;
                            }
                            (model.ApplicationMethodName, error) = await _organicManureService.FetchApplicationMethodById(model.ApplicationMethod.Value);
                            if (error != null)
                            {
                                TempData["ManureTypeError"] = error.Message;
                                return View(model);
                            }
                        }
                        else if (error != null)
                        {
                            TempData["ManureTypeError"] = error.Message;
                            return View(model);
                        }

                    }
                }
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ManureApplyingDate");

        }

        [HttpGet]
        public async Task<IActionResult> ManureApplyingDate()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
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
                if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
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
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureApplyingDate(OrganicManureViewModel model)
        {
            try
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
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
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
                        orgManure.ApplicationDate = model.ApplicationDate.Value;
                    }
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

                if (model.IsCheckAnswer && (!model.IsManureTypeChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("ApplicationMethod");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> ApplicationMethod()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error error = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
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
            (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, applicableFor);
            if (error == null && applicationMethodList.Count > 0)
            {
                ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(x => x.Name).ToList();
            }

            model.ApplicationMethodCount = applicationMethodList.Count;
            if (applicationMethodList.Count == 1)
            {
                model.ApplicationMethod = applicationMethodList[0].ID;
                (model.ApplicationMethodName, error) = await _organicManureService.FetchApplicationMethodById(model.ApplicationMethod.Value);
                if (error != null)
                {
                    TempData["ManureApplyingDateError"] = error.Message;
                    return RedirectToAction("ManureApplyingDate", model);
                }
                if (model.OrganicManures.Count > 0)
                {
                    foreach (var orgManure in model.OrganicManures)
                    {
                        orgManure.ApplicationMethodID = model.ApplicationMethod.Value;
                    }
                }
                if (model.IsCheckAnswer)
                {
                    OrganicManureViewModel organicManureViewModel = new OrganicManureViewModel();
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                    {
                        organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if ((organicManureViewModel.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.DeepInjection2530cm) || (organicManureViewModel.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.ShallowInjection57cm))
                    {
                        model.IncorporationDelay = null;
                        model.IncorporationMethod = null;
                        model.IncorporationDelayName = string.Empty;
                        model.IncorporationMethodName = string.Empty;
                    }
                    if (!(model.IsFieldGroupChange && model.IsManureTypeChange))
                    {
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return RedirectToAction("CheckAnswer");
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                
                if (model.IsDefaultNutrient.Value)
                {
                    return RedirectToAction("ManureApplyingDate");
                }

                return RedirectToAction("DefaultNutrientValues");
            }

            if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange))
            {
                model.IsApplicationMethodChange = true;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationMethod(OrganicManureViewModel model)
        {
            Error error = null;
            if (model.ApplicationMethod == null)
            {
                ModelState.AddModelError("ApplicationMethod", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            bool isLiquid = false;
            if (!ModelState.IsValid)
            {
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
                (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, applicableFor);
                ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(x => x.Name).ToList(); ;
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
            (model.ApplicationMethodName, error) = await _organicManureService.FetchApplicationMethodById(model.ApplicationMethod.Value);

            if ((model.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.DeepInjection2530cm) || (model.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.ShallowInjection57cm))
            {
                if (manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isLiquid = manureType.IsLiquid.Value;
                    string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
                    List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                    var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
                    (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(fieldType ?? 0, applicableFor, model.ApplicationMethod ?? 0);
                    if (error == null && incorporationMethods.Count == 1)
                    {
                        model.IncorporationMethod = incorporationMethods.FirstOrDefault().ID;
                        applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                        if (manureType.Id == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                        {
                            applicableFor = Resource.lblP;
                        }
                        (model.IncorporationMethodName, error) = await _organicManureService.FetchIncorporationMethodById(model.IncorporationMethod.Value);
                        if (error == null)
                        {
                            (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                            if (error == null && incorporationDelaysList.Count == 1)
                            {

                                model.IncorporationDelay = incorporationDelaysList.FirstOrDefault().ID;
                                (model.IncorporationDelayName, error) = await _organicManureService.FetchIncorporationDelayById(model.IncorporationDelay.Value);
                                if (error == null)
                                {
                                    if (model.OrganicManures.Count > 0)
                                    {
                                        foreach (var orgManure in model.OrganicManures)
                                        {
                                            orgManure.IncorporationMethodID = model.IncorporationMethod.Value;
                                            orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                                        }
                                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                                        if (model.IsCheckAnswer && model.IsApplicationMethodChange)
                                        {
                                            return RedirectToAction("CheckAnswer");
                                        }
                                    }
                                }
                                else
                                {
                                    TempData["ApplicationMethodError"] = error.Message;
                                    return View(model);
                                }
                            }
                            else
                            {
                                TempData["ApplicationMethodError"] = error.Message;
                                return View(model);
                            }
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        }
                        else if (error != null)
                        {
                            TempData["ApplicationMethodError"] = error.Message;
                            return View(model);
                        }
                    }
                    else if (error != null)
                    {
                        TempData["ApplicationMethodError"] = error.Message;
                        return View(model);
                    }
                }
            }
            else
            {
                OrganicManureViewModel organicManureViewModel = new OrganicManureViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if ((organicManureViewModel.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.DeepInjection2530cm) || (organicManureViewModel.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.ShallowInjection57cm))
                {
                    model.IncorporationDelay = null;
                    model.IncorporationMethod = null;
                    model.IncorporationDelayName = string.Empty;
                    model.IncorporationMethodName = string.Empty;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IsCheckAnswer && model.IsApplicationMethodChange)
            {
                return RedirectToAction("IncorporationMethod");
            }
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
            try
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
                    ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblDryMatter.ToLower()));
                }
                if (model.N == null)
                {
                    ModelState.AddModelError("N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblTotalNitrogen.ToLower()));
                }
                if (model.NH4N == null)
                {
                    ModelState.AddModelError("NH4N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAmmoniumForError));
                }
                if (model.UricAcid == null)
                {
                    ModelState.AddModelError("UricAcid", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.MsgUricAcid));
                }
                if (model.NO3N == null)
                {
                    ModelState.AddModelError("NO3N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblNitrateForErrorMsg));
                }
                if (model.P2O5 == null)
                {
                    ModelState.AddModelError("P2O5", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPhosphate.ToLower()));
                }
                if (model.K2O == null)
                {
                    ModelState.AddModelError("K2O", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPotash.ToLower()));
                }
                if (model.SO3 == null)
                {
                    ModelState.AddModelError("SO3", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblSulphur));
                }
                if (model.MgO == null)
                {
                    ModelState.AddModelError("MgO", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblMagnesium.ToLower()));
                }
                if (model.N != null && model.NH4N != null && model.UricAcid != null && model.NO3N != null)
                {
                    decimal totalValue = model.NH4N.Value + model.UricAcid.Value + model.NO3N.Value;
                    if (model.N < totalValue)
                    {
                        ModelState.AddModelError("N", Resource.lblTotalNitrogenMustBeGreaterOrEqualToAmmoniumUricacidNitrate);
                    }
                }

                if (model.DryMatterPercent != null)
                {
                    if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PigSlurry ||
                        model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.CattleSlurry)
                    {
                        if (model.DryMatterPercent < 0 || model.DryMatterPercent > 25)
                        {
                            ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter.ToLower(), 25));
                        }
                    }
                    else
                    {
                        if (model.DryMatterPercent < 0 || model.DryMatterPercent > 99)
                        {
                            ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter, 99));
                        }
                    }
                }
                if (model.N != null)
                {
                    if (model.N < 0 || model.N > 297)
                    {
                        ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblTotalNitrogenN, 297));
                    }
                }
                if (model.NH4N != null)
                {
                    if (model.NH4N < 0 || model.NH4N > 99)
                    {
                        ModelState.AddModelError("NH4N", string.Format(Resource.MsgMinMaxValidation, Resource.lblAmmonium, 99));
                    }
                }
                if (model.UricAcid != null)
                {
                    if (model.UricAcid < 0 || model.UricAcid > 99)
                    {
                        ModelState.AddModelError("UricAcid", string.Format(Resource.MsgMinMaxValidation, Resource.lblUricAcid, 99));
                    }
                }
                if (model.NO3N != null)
                {
                    if (model.NO3N < 0 || model.NO3N > 99)
                    {
                        ModelState.AddModelError("NO3N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrate, 99));
                    }
                }
                if (model.P2O5 != null)
                {
                    if (model.P2O5 < 0 || model.P2O5 > 99)
                    {
                        ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5, 99));
                    }
                }
                if (model.K2O != null)
                {
                    if (model.K2O < 0 || model.K2O > 99)
                    {
                        ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2O, 99));
                    }
                }
                if (model.MgO != null)
                {
                    if (model.MgO < 0 || model.MgO > 99)
                    {
                        ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblTotalMagnesiumOxide, 99));
                    }
                }
                if (model.SO3 != null)
                {
                    if (model.SO3 < 0 || model.SO3 > 99)
                    {
                        ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3, 99));
                    }
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


                return RedirectToAction("NutrientValuesStoreForFuture");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }
        [HttpGet]
        public async Task<IActionResult> NutrientValuesStoreForFuture()
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
        public async Task<IActionResult> NutrientValuesStoreForFuture(OrganicManureViewModel model)
        {
            if (model.IsAnyNeedToStoreNutrientValueForFuture == null)
            {
                ModelState.AddModelError("IsAnyNeedToStoreNutrientValueForFuture", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
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
                ViewBag.Error = error.Message;
            }

            (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
            model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
            if (error1 != null && (!string.IsNullOrWhiteSpace(error1.Message)))
            {
                ViewBag.Error = error1.Message;
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(OrganicManureViewModel model)
        {
            try
            {
                if (model.ApplicationRateMethod == null)
                {
                    ModelState.AddModelError("ApplicationRate", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                if (!ModelState.IsValid)
                {
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

                if (model.ApplicationRateMethod.Value == (int)NMP.Portal.Enums.ApplicationRate.EnterAnApplicationRate)
                {
                    model.Area = null;
                    model.Quantity = null;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    return RedirectToAction("ManualApplicationRate");
                }
                else if (model.ApplicationRateMethod.Value == (int)NMP.Portal.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
                {
                    model.ApplicationRate = null;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    return RedirectToAction("AreaQuantity");
                }
                else if (model.ApplicationRateMethod.Value == (int)NMP.Portal.Enums.ApplicationRate.UseDefaultApplicationRate)
                {
                    model.ApplicationRate = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.ApplicationRateArable;
                    model.Area = null;
                    model.Quantity = null;
                    if (model.OrganicManures.Count > 0)
                    {
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.AreaSpread = null;
                            orgManure.ManureQuantity = null;
                            orgManure.ApplicationRate = model.ApplicationRate.Value;
                        }
                    }
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                }

                if (model.IsCheckAnswer && (!model.IsManureTypeChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("IncorporationMethod");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }


        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
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

                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

                (List<CommonResponse> manureGroupList, Error error1) = await _organicManureService.FetchManureGroupList();
                model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
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
            model.Area = null;
            model.Quantity = null;
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AreaSpread = null;
                    orgManure.ManureQuantity = null;
                    orgManure.ApplicationRate = model.ApplicationRate.Value;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange))
            {
                return RedirectToAction("CheckAnswer");
            }
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
            if (model.Area != null && model.Area == 0)
            {
                ModelState.AddModelError("Area", Resource.MsgAreaMustBeGreaterThanZero);
            }
            if (!ModelState.IsValid)
            {
                return View("AreaQuantity", model);
            }
            model.ApplicationRate = (int)Math.Round(model.Quantity.Value / model.Area.Value);
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AreaSpread = model.Area.Value;
                    orgManure.ManureQuantity = model.Quantity.Value;
                    orgManure.ApplicationRate = model.ApplicationRate.Value;
                }
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("IncorporationMethod");
        }
        [HttpGet]
        public async Task<IActionResult> IncorporationMethod()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error error = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if ((model.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.DeepInjection2530cm) || (model.ApplicationMethod == (int)NMP.Portal.Enums.ApplicationMethod.ShallowInjection57cm))
            {
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            bool isLiquid = false;
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                isLiquid = manureType.IsLiquid.Value;

            }

            string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;

            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

            (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(fieldType ?? 0, applicableFor, model.ApplicationMethod ?? 0);
            if (error == null && incorporationMethods.Count > 0)
            {
                ViewBag.IncorporationMethod = incorporationMethods;
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationMethod(OrganicManureViewModel model)
        {
            Error error = null;
            if (model.IncorporationMethod == null)
            {
                ModelState.AddModelError("IncorporationMethod", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isLiquid = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isLiquid = manureType.IsLiquid.Value;

                }

                string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(fieldType ?? 0, applicableFor, model.ApplicationMethod ?? 0);

                ViewBag.IncorporationMethod = incorporationMethods;
                return View(model);
            }

            (model.IncorporationMethodName, error) = await _organicManureService.FetchIncorporationMethodById(model.IncorporationMethod.Value);

            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.IncorporationMethodID = model.IncorporationMethod.Value;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IncorporationMethod == (int)NMP.Portal.Enums.IncorporationMethod.NotIncorporated)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    bool isLiquid = manureType.IsLiquid.Value;
                    string applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                    if (manureType.Id == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                    {
                        applicableFor = Resource.lblP;
                    }
                    (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                    if (error == null && incorporationDelaysList.Count == 1)
                    {
                        model.IncorporationDelay = incorporationDelaysList.FirstOrDefault().ID;
                        if (model.OrganicManures.Count > 0)
                        {
                            foreach (var orgManure in model.OrganicManures)
                            {
                                orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                    else if (error != null)
                    {
                        TempData["IncorporationMethodError"] = error.Message;
                        return View(model);
                    }
                }
                else if (error != null)
                {
                    TempData["IncorporationMethodError"] = error.Message;
                    return View(model);
                }
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            return RedirectToAction("IncorporationDelay");

        }

        [HttpGet]
        public async Task<IActionResult> IncorporationDelay()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error error = null;
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
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
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

            (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
            if (error == null && incorporationDelaysList.Count > 0)
            {
                ViewBag.IncorporationDelaysList = incorporationDelaysList;
            }
            return View(model);


        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationDelay(OrganicManureViewModel model)
        {
            Error error = null;
            try
            {
                if (model.IncorporationDelay == null)
                {
                    ModelState.AddModelError("IncorporationDelay", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    string applicableFor = string.Empty;
                    int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
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

                    (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _organicManureService.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                    ViewBag.IncorporationDelaysList = incorporationDelaysList;
                    return View(model);
                }
                (model.IncorporationDelayName, error) = await _organicManureService.FetchIncorporationDelayById(model.IncorporationDelay.Value);

                if (model.OrganicManures.Count > 0)
                {
                    foreach (var orgManure in model.OrganicManures)
                    {
                        orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                if ((!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && model.IsCheckAnswer && model.IsApplicationMethodChange)
                {
                    return RedirectToAction("CheckAnswer");
                }

                return RedirectToAction("ConditionsAffectingNutrients");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> ConditionsAffectingNutrients()
        {
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error error = new Error();
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
                //crop N uptake
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var crop = cropsResponse.Where(x => x.Year == model.HarvestYear);
                int cropTypeId = crop.Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                int cropCategoryId = await _mannerService.FetchCategoryIdByCropTypeIdAsync(cropTypeId);

                //check early and late for winter cereals and winter oilseed rape
                //if sowing date after 15 sept then late
                DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();
                if (model.AutumnCropNitrogenUptake == null)
                {
                    if (cropCategoryId == (int)NMP.Portal.Enums.CropCategory.EarlySownWinterCereal || cropCategoryId == (int)NMP.Portal.Enums.CropCategory.EarlyStablishedWinterOilseedRape)
                    {
                        if (sowingDate != null)
                        {
                            int day = sowingDate.Value.Day;
                            int month = sowingDate.Value.Month;
                            if (month == (int)NMP.Portal.Enums.Month.September && day > 15)
                            {
                                if (cropCategoryId == (int)NMP.Portal.Enums.CropCategory.EarlySownWinterCereal)
                                {
                                    cropCategoryId = (int)NMP.Portal.Enums.CropCategory.LateSownWinterCereal;
                                }
                                else
                                {
                                    cropCategoryId = (int)NMP.Portal.Enums.CropCategory.LateStablishedWinterOilseedRape;
                                }
                            }
                        }
                    }

                    if (model.ApplicationDate.Value.Month >= (int)NMP.Portal.Enums.Month.August && model.ApplicationDate.Value.Month <= (int)NMP.Portal.Enums.Month.October)
                    {

                        model.AutumnCropNitrogenUptake = await _mannerService.FetchCropNUptakeDefaultAsync(cropCategoryId);
                    }
                    else
                    {
                        model.AutumnCropNitrogenUptake = 0;
                    }
                }


                //Soil drainage end date
                if (model.SoilDrainageEndDate == null)
                {
                    model.SoilDrainageEndDate = new DateTime(model.ApplicationDate.Value.AddYears(1).Year, (int)NMP.Portal.Enums.Month.March, 31);
                }

                //Rainfall within 6 hours
                if (model.RainfallWithinSixHoursID == null)
                {
                    (RainTypeResponse rainType, error) = await _organicManureService.FetchRainTypeDefault();
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.RainfallWithinSixHours = rainType.Name;
                        model.RainfallWithinSixHoursID = rainType.ID;
                    }
                }
                else
                {
                    (List<RainTypeResponse> rainTypes, error) = await _organicManureService.FetchRainTypeList();
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.RainfallWithinSixHours = rainTypes.Where(x => x.ID == model.RainfallWithinSixHoursID).Select(x => x.Name).FirstOrDefault();
                    }
                }

                //Effective rainfall after application
                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                string halfPostCode = string.Empty;
                if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                {
                    ViewBag.Error = error.Message;
                    return View(model);
                }
                else
                {
                    string[] postCodeParts = farm.Postcode.Split(' ');

                    if (postCodeParts.Length == 2)
                    {
                        halfPostCode = postCodeParts[0];
                    }
                }

                if (model.ApplicationDate.HasValue && model.SoilDrainageEndDate.HasValue)
                {
                    if (model.TotalRainfall == null)
                    {
                        model.TotalRainfall = await _organicManureService.FetchRainfallByPostcodeAndDateRange(halfPostCode, model.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"), model.SoilDrainageEndDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                }

                //Windspeed during application 
                if (model.WindspeedID == null)
                {
                    (WindspeedResponse windspeed, error) = await _organicManureService.FetchWindspeedDataDefault();
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.WindspeedID = windspeed.ID;
                        model.Windspeed = windspeed.Name;
                    }
                }
                else
                {
                    (List<WindspeedResponse> windspeeds, error) = await _organicManureService.FetchWindspeedList();
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.Windspeed = windspeeds.Where(x => x.ID == model.WindspeedID).Select(x => x.Name).FirstOrDefault();
                    }
                }

                //Topsoil moisture
                if (model.MoistureTypeId == null)
                {
                    (MoistureTypeResponse moisterType, error) = await _organicManureService.FetchMoisterTypeDefaultByApplicationDate(model.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.MoistureType = moisterType.Name;
                        model.MoistureTypeId = moisterType.ID;
                    }
                }
                else
                {
                    (List<MoistureTypeResponse> moisterTypes, error) = await _organicManureService.FetchMoisterTypeList();
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                    else
                    {
                        model.MoistureType = moisterTypes.Where(x => x.ID == model.MoistureTypeId).Select(x => x.Name).FirstOrDefault();
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConditionsAffectingNutrients(OrganicManureViewModel model)
        {
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0;
                    orgManure.SoilDrainageEndDate = model.SoilDrainageEndDate.Value;
                    orgManure.RainfallWithinSixHoursID = model.RainfallWithinSixHoursID.Value;
                    orgManure.Rainfall = model.TotalRainfall.Value;
                    orgManure.WindspeedID = model.WindspeedID.Value;
                    orgManure.MoistureID = model.MoistureTypeId.Value;


                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            return RedirectToAction("CheckAnswer");

        }

        [HttpGet]
        public async Task<IActionResult> backActionForManureGroup()
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

            if (model.IsCheckAnswer)
            {
                model.ManureGroupIdForFilter = model.ManureGroupId;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                (CommonResponse manureGroup, Error error) = await _organicManureService.FetchManureGroupById(model.ManureGroupId.Value);
                if (error == null)
                {
                    if (manureGroup != null)
                    {
                        model.ManureGroupName = manureGroup.Name;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                }
                else
                {
                    TempData["ManureGroupError"] = error.Message;
                    return View(model);
                }
                return RedirectToAction("CheckAnswer");
            }


            if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
            {
                if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
                {
                    string fieldId = model.FieldList[0];
                    return RedirectToAction("Recommendations", "Crop", new
                    {
                        q = model.EncryptedFarmId,
                        r = _cropDataProtector.Protect(fieldId),
                        s = model.EncryptedHarvestYear

                    });
                }
            }
            else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
            {
                return RedirectToAction("Fields");
            }

            return RedirectToAction("FieldGroup", new
            {
                q = model.EncryptedFarmId,
                r = model.EncryptedHarvestYear
            });



        }
        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
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
                model.IsCheckAnswer = true;
                model.IsManureTypeChange = false;
                model.IsApplicationMethodChange = false;
                model.IsFieldGroupChange = false;
                if (model.OrganicManures.Count > 0)
                {

                    if (model.IsDefaultNutrientValues.Value && model.ManureType.K2O.HasValue && model.ApplicationRate.HasValue && model.ManureType.K2OAvailable.HasValue)
                    {
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.AvailableK2O = model.ManureType.K2O.Value * (model.ApplicationRate.Value * (model.ManureType.K2OAvailable.Value / 100));
                            orgManure.AvailableP2O5 = model.ManureType.P2O5.Value * (model.ApplicationRate.Value * (model.ManureType.P2O5Available.Value / 100));
                        }
                    }
                    else
                    {
                        if (model.K2O.HasValue && model.ApplicationRate.HasValue && model.ManureType.K2OAvailable.HasValue)
                        {
                            foreach (var orgManure in model.OrganicManures)
                            {
                                orgManure.AvailableK2O = model.K2O.Value * (model.ApplicationRate.Value * (model.ManureType.K2OAvailable.Value / 100));
                                orgManure.AvailableP2O5 = model.P2O5.Value * (model.ApplicationRate.Value * (model.ManureType.P2O5Available.Value / 100));
                            }
                        }
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            }
            catch (Exception ex)
            {
                TempData["ConditionsAffectingNutrientsError"] = ex.Message;
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> CheckAnswer(OrganicManureViewModel model)
        {
            try
            {
                if (model.ManureTypeId == null)
                {
                    ModelState.AddModelError("ManureTypeId", Resource.MsgManureTypeNotSet);
                }
                if (model.IncorporationMethod == null)
                {
                    ModelState.AddModelError("IncorporationMethod", string.Format(Resource.MsgIncorporationMethodNotSet, model.ManureTypeName));
                }
                if (model.IncorporationDelay == null)
                {
                    ModelState.AddModelError("IncorporationDelay", string.Format(Resource.MsgIncorporationDelayNotSet, model.ManureTypeName));
                }
                if (model.ApplicationMethod == null)
                {
                    ModelState.AddModelError("ApplicationMethod", string.Format(Resource.MsgApplicationMethodNotSet, model.ManureTypeName));
                }
                if (model.ApplicationDate == null)
                {
                    ModelState.AddModelError("ApplicationDate", string.Format(Resource.MsgApplyingDateNotSet, model.ManureTypeName));
                }
                if (model.IsDefaultNutrientValues == null)
                {
                    ModelState.AddModelError("IsDefaultNutrientValues", string.Format(Resource.MsgDefaultNutrientValuesNotSet, model.ManureTypeName));
                }
                if (model.ApplicationRateMethod == null)
                {
                    ModelState.AddModelError("ApplicationRateMethod", string.Format(Resource.MsgApplicationRateMethodNotSet, model.ManureTypeName));
                }
                if (model.ApplicationRate == null)
                {
                    ModelState.AddModelError("ApplicationRate", Resource.MsgApplicationRateNotSet);
                }
                if (model.ApplicationRateMethod == (int)NMP.Portal.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
                {
                    if (model.Area == null)
                    {
                        ModelState.AddModelError("Area", Resource.MsgAreaNotSet);
                    }
                    if (model.Quantity == null)
                    {
                        ModelState.AddModelError("Quantity", Resource.MsgQuantityNotSet);
                    }
                }
                if (model.AutumnCropNitrogenUptake == null)
                {
                    ModelState.AddModelError("AutumnCropNitrogenUptake", Resource.MsgAutumnCropNitrogenUptakeNotSet);
                }
                if (model.SoilDrainageEndDate == null)
                {
                    ModelState.AddModelError("SoilDrainageEndDate", Resource.MsgEndOfSoilDrainageNotSet);
                }
                if (model.RainfallWithinSixHoursID == null)
                {
                    ModelState.AddModelError("RainfallWithinSixHoursID", Resource.MsgRainfallWithinSixHoursOfApplicationNotSet);
                }
                if (model.TotalRainfall == null)
                {
                    ModelState.AddModelError("TotalRainfall", Resource.MsgTotalRainfallSinceApplicationNotSet);
                }
                if (model.WindspeedID == null)
                {
                    ModelState.AddModelError("WindspeedID", Resource.MsgWindspeedAtApplicationNotSet);
                }
                if (model.MoistureTypeId == null)
                {
                    ModelState.AddModelError("MoistureTypeId", Resource.MsgTopsoilMoistureNotSet);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (model.OrganicManures != null)
                {
                    model.OrganicManures.ForEach(x => x.EndOfDrain = x.SoilDrainageEndDate);
                }
                var jsonData = new
                {
                    OrganicManures = model.OrganicManures.Select(orgManure => new
                    {
                        OrganicManure = orgManure,
                        FarmID = model.FarmId,
                        FieldTypeID = (int)NMP.Portal.Enums.FieldType.Arable,
                        SaveDefaultForFarm = model.IsAnyNeedToStoreNutrientValueForFuture
                    }).ToList()
                };


                string jsonString = JsonConvert.SerializeObject(jsonData);
                (bool success, Error error) = await _organicManureService.AddOrganicManuresAsync(jsonString);
                if (!success || error != null)
                {
                    TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                    return View(model);
                }

                string successMsg = string.Empty;
                if (int.TryParse(model.FieldGroup, out int value))
                {
                    successMsg = string.Format(Resource.lblOrganicManureCreatedSuccessfullyForCropType, model.CropTypeName);
                }
                else
                {
                    (List<OrganicManureFieldResponse> organicManureField, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                    if (error == null)
                    {
                        if (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count < organicManureField.Count)
                        {

                            List<string> fieldNames = model.FieldList
                           .Select(id => organicManureField.FirstOrDefault(f => f.FieldId == Convert.ToInt64(id))?.FieldName).ToList();
                            string concatenatedFieldNames = string.Join(", ", fieldNames);
                            successMsg = string.Format(Resource.lblOrganicManureCreatedSuccessfullyForSpecificField, concatenatedFieldNames);

                        }
                        else
                        {
                            successMsg = Resource.lblOrganicManureCreatedSuccessfullyForAllField;
                        }
                    }
                    else
                    {
                        TempData["AddOrganicManureError"] = error.Message;
                        return View(model);
                    }

                }
                if (success)
                {
                    _httpContextAccessor.HttpContext?.Session.Remove("OrganicManure");
                    return RedirectToAction("HarvestYearOverview", "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear,
                        q = _farmDataProtector.Protect(success.ToString()),
                        r = _cropDataProtector.Protect(successMsg)
                    });
                }

            }
            catch (Exception ex)
            {
                TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                return View(model);
            }
            return View(model);

        }
        public IActionResult BackCheckAnswer()
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
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> AutumnCropNitrogenUptake()
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
        public async Task<IActionResult> AutumnCropNitrogenUptake(OrganicManureViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey("AutumnCropNitrogenUptake"))
            {
                var autumnCropNitrogenUptakeState = ModelState["AutumnCropNitrogenUptake"];

                if (autumnCropNitrogenUptakeState.Errors.Count > 0)
                {
                    var firstError = autumnCropNitrogenUptakeState.Errors[0];

                    if (firstError.ErrorMessage == string.Format(Resource.lblEnterNumericValue, autumnCropNitrogenUptakeState.RawValue, "AutumnCropNitrogenUptake"))
                    {
                        autumnCropNitrogenUptakeState.Errors.Clear();
                        autumnCropNitrogenUptakeState.Errors.Add(Resource.MsgEnterValidNumericValueBeforeContinuing);
                    }
                }
            }

            if (model.AutumnCropNitrogenUptake == null)
            {
                ModelState.AddModelError("AutumnCropNitrogenUptake", Resource.MsgEnterAValueBeforeContinue);
            }
            if (!ModelState.IsValid)
            {
                return View("AutumnCropNitrogenUptake", model);
            }


            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> SoilDrainageEndDate()
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
        public async Task<IActionResult> SoilDrainageEndDate(OrganicManureViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilDrainageEndDate"))
            {
                var dateError = ModelState["SoilDrainageEndDate"].Errors.Count > 0 ?
                                ModelState["SoilDrainageEndDate"].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, "SoilDrainageEndDate")))
                {
                    ModelState["SoilDrainageEndDate"].Errors.Clear();
                    ModelState["SoilDrainageEndDate"].Errors.Add(Resource.MsgEnterValidDate);
                }
            }

            if (model.SoilDrainageEndDate == null)
            {
                ModelState.AddModelError("SoilDrainageEndDate", Resource.MsgEnterADateBeforeContinuing);
            }
            if (model.SoilDrainageEndDate != null)
            {
                //if (model.SoilDrainageEndDate.Value.Date.Year > model.HarvestYear + 1)
                //{
                //    ModelState.AddModelError("SoilDrainageEndDate", Resource.MsgDateCannotBeLaterThanHarvestYear);
                //}
                if (DateTime.TryParseExact(model.SoilDrainageEndDate.Value.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    ModelState.AddModelError("SoilDrainageEndDate", Resource.MsgEnterValidDate);
                }

                if (!(model.SoilDrainageEndDate.Value.Month >= (int)NMP.Portal.Enums.Month.January && model.SoilDrainageEndDate.Value.Month <= (int)NMP.Portal.Enums.Month.April))
                {
                    ModelState.AddModelError("SoilDrainageEndDate", Resource.MsgSoilDrainageEndDate1stJan30Apr);
                }
            }
            if (!ModelState.IsValid)
            {
                return View("SoilDrainageEndDate", model);
            }


            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> RainfallWithinSixHour()
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
            (List<RainTypeResponse> rainType, Error error) = await _organicManureService.FetchRainTypeList();
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                ViewBag.Error = error.Message;
            }
            else
            {
                ViewBag.RainTypes = rainType;
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RainfallWithinSixHour(OrganicManureViewModel model)
        {
            if (model.RainfallWithinSixHoursID == null)
            {
                ModelState.AddModelError("RainfallWithinSixHoursID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("RainfallWithinSixHour", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfall()
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
        public async Task<IActionResult> EffectiveRainfall(OrganicManureViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EffectiveRainfall", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfallManual()
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
        public async Task<IActionResult> EffectiveRainfallManual(OrganicManureViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("TotalRainfall"))
            {
                var RainfallError = ModelState["TotalRainfall"].Errors.Count > 0 ?
                                ModelState["TotalRainfall"].Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["TotalRainfall"].RawValue, "TotalRainfall")))
                {
                    ModelState["TotalRainfall"].Errors.Clear();
                    decimal decimalValue;
                    if (decimal.TryParse(ModelState["TotalRainfall"].RawValue.ToString(), out decimalValue))
                    {
                        ModelState["TotalRainfall"].Errors.Add(Resource.MsgIfUserEnterDecimalValueInRainfall);
                    }
                    else
                    {
                        ModelState["TotalRainfall"].Errors.Add(Resource.MsgForEffectiveRainfallManual);
                    }
                }
            }

            if (model.TotalRainfall == null)
            {
                ModelState.AddModelError("TotalRainfall", Resource.MsgEnterRainfallAmountBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("EffectiveRainfallManual", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> Windspeed()
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
            (List<WindspeedResponse> windspeeds, Error error) = await _organicManureService.FetchWindspeedList();
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                ViewBag.Error = error.Message;
            }
            else
            {
                ViewBag.Windspeeds = windspeeds;
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Windspeed(OrganicManureViewModel model)
        {
            if (model.WindspeedID == null)
            {
                ModelState.AddModelError("WindspeedID", Resource.MsgSelectAWindConditionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("Windspeed", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }

        [HttpGet]
        public async Task<IActionResult> TopsoilMoisture()
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
            (List<MoistureTypeResponse> moisterTypes, Error error) = await _organicManureService.FetchMoisterTypeList();
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                ViewBag.Error = error.Message;
            }
            else
            {
                ViewBag.moisterTypes = moisterTypes;
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopsoilMoisture(OrganicManureViewModel model)
        {
            if (model.MoistureTypeId == null)
            {
                ModelState.AddModelError("MoistureTypeId", Resource.MsgSelectATopsoilWetnessConditionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("TopsoilMoisture", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }
    }
}