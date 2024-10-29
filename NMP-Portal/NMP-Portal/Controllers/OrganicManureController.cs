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
using System.Collections.Immutable;
using System;
using Microsoft.Identity.Client;
using System.Text.RegularExpressions;

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
            _logger.LogTrace($"Organic Manure Controller : Index() action called");
            return View();
        }
        public IActionResult CreateManureCancel(string q, string r)
        {
            _logger.LogTrace($"Organic Manure Controller : CreateManureCancel({q}, {r}) action called");
            _httpContextAccessor.HttpContext?.Session.Remove("OrganicManure");
            return RedirectToAction("HarvestYearOverview", "Crop", new { Id = q, year = r });
        }

        [HttpGet]
        public async Task<IActionResult> FieldGroup(string q, string r, string? s)//q=FarmId,r=harvestYear,s=fieldId
        {
            _logger.LogTrace($"Organic Manure Controller : FieldGroup({q}, {r}, {s}) action called");
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
                (List<ManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                if (error == null)
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
                else
                {
                    TempData["FieldGroupError"] = error.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in FieldGroup() action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldGroupError"] = ex.Message;
            }

            if (model.IsCheckAnswer && (string.IsNullOrWhiteSpace(s)))
            {
                model.IsFieldGroupChange = true;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View("Views/OrganicManure/FieldGroup.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldGroup(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : FieldGroup() post action called");
            Error error = null;
            if (model.FieldGroup == null)
            {
                ModelState.AddModelError("FieldGroup", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            try
            {
                (List<ManureCropTypeResponse> cropTypeList, error) = await _organicManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
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
                    return View("Views/OrganicManure/FieldGroup.cshtml", model);
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
                _logger.LogTrace($"Organic Manure Controller : Exception in FieldGroup() post action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldGroupError"] = ex.Message;
                return View("Views/OrganicManure/FieldGroup.cshtml", model);
            }
            return RedirectToAction("Fields");

        }

        [HttpGet]
        public async Task<IActionResult> Fields()
        {
            _logger.LogTrace($"Organic Manure Controller : Fields() action called");
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
                (List<CommonResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                    {
                        if (fieldList.Count > 0)
                        {

                            var SelectListItem = fieldList.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name.ToString()
                            }).ToList();
                            ViewBag.FieldList = SelectListItem.OrderBy(x => x.Text).ToList();
                        }
                        return View(model);
                    }
                    else
                    {
                        if (fieldList.Count > 0)
                        {
                            model.FieldList = fieldList.Select(x => x.Id.ToString()).ToList();
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
                                    if (model.IsCheckAnswer && model.OrganicManures.Count > 0)
                                    {
                                        foreach (var organicManure in model.OrganicManures)
                                        {
                                            if (model.ApplicationDate.HasValue)
                                            {
                                                organicManure.ApplicationDate = model.ApplicationDate.Value;
                                            }
                                            if (model.ApplicationMethod.HasValue)
                                            {
                                                organicManure.ApplicationMethodID = model.ApplicationMethod.Value;
                                            }
                                            if (model.ApplicationRate.HasValue)
                                            {
                                                organicManure.ApplicationRate = model.ApplicationRate.Value;
                                            }
                                            if (model.Area.HasValue)
                                            {
                                                organicManure.AreaSpread = model.Area.Value;
                                            }
                                            if (model.Quantity.HasValue)
                                            {
                                                organicManure.ManureQuantity = model.Quantity.Value;
                                            }
                                            organicManure.ManureTypeID = model.ManureTypeId.Value;
                                            if (model.TotalRainfall.HasValue)
                                            {
                                                organicManure.Rainfall = model.TotalRainfall.Value;
                                            }
                                            if (model.IsDefaultNutrientValues.HasValue && (!model.IsDefaultNutrientValues.Value))
                                            {
                                                organicManure.DryMatterPercent = model.DryMatterPercent.Value;
                                                organicManure.K2O = model.K2O.Value;
                                                organicManure.MgO = model.MgO.Value;
                                                organicManure.N = model.N.Value;
                                                organicManure.NH4N = model.NH4N.Value;
                                                organicManure.NO3N = model.NO3N.Value;
                                                organicManure.P2O5 = model.P2O5.Value;
                                                organicManure.SO3 = model.SO3.Value;
                                                organicManure.UricAcid = model.UricAcid.Value;
                                            }
                                            else
                                            {
                                                if (model.ManureType != null)
                                                {
                                                    organicManure.DryMatterPercent = model.ManureType.DryMatter.Value;
                                                    organicManure.K2O = model.ManureType.K2O.Value;
                                                    organicManure.MgO = model.ManureType.MgO.Value;
                                                    organicManure.N = model.ManureType.TotalN.Value;
                                                    organicManure.NH4N = model.ManureType.NH4N.Value;
                                                    organicManure.NO3N = model.ManureType.NO3N.Value;
                                                    organicManure.P2O5 = model.ManureType.P2O5.Value;
                                                    organicManure.SO3 = model.ManureType.SO3.Value;
                                                    organicManure.UricAcid = model.ManureType.Uric.Value;
                                                }
                                            }
                                            if (model.IncorporationDelay.HasValue)
                                            {
                                                organicManure.IncorporationDelayID = model.IncorporationDelay.Value;
                                            }
                                            if (model.IncorporationMethod.HasValue)
                                            {
                                                organicManure.IncorporationMethodID = model.IncorporationMethod.Value;
                                            }
                                            if (model.SoilDrainageEndDate.HasValue)
                                            {
                                                organicManure.EndOfDrain = model.SoilDrainageEndDate.Value;
                                            }
                                            if (model.AutumnCropNitrogenUptake.HasValue)
                                            {
                                                organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake.Value;
                                            }
                                            if (model.WindspeedID.HasValue)
                                            {
                                                organicManure.WindspeedID = model.WindspeedID.Value;
                                            }
                                            if (model.MoistureTypeId.HasValue)
                                            {
                                                organicManure.MoistureID = model.MoistureTypeId.Value;
                                            }
                                            if (model.RainfallWithinSixHoursID.HasValue)
                                            {
                                                organicManure.RainfallWithinSixHoursID = model.RainfallWithinSixHoursID.Value;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                TempData["FieldGroupError"] = error.Message;
                                return View("FieldGroup", model);
                            }



                            if (model.IsCheckAnswer && model.IsFieldGroupChange)
                            {
                                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                                {
                                    OrganicManureViewModel organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                                    if (organicManureViewModel != null && organicManureViewModel.FieldList.Count > 0)
                                    {
                                        var fieldListChange = organicManureViewModel.FieldList.Where(item1 => !model.FieldList.Any(item2 => item2 == item1)).ToList();

                                        // Perform the required action for these items
                                        if (fieldListChange != null && fieldListChange.Count > 0)
                                        {
                                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                                            var crop = cropsResponse.Where(x => x.Year == model.HarvestYear);
                                            int cropTypeId = crop.Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                                            int cropCategoryId = await _mannerService.FetchCategoryIdByCropTypeIdAsync(cropTypeId);

                                            //check early and late for winter cereals and winter oilseed rape
                                            //if sowing date after 15 sept then late
                                            DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();
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
                                    }
                                }
                                else
                                {
                                    return RedirectToAction("FarmList", "Farm");
                                }

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
                _logger.LogTrace($"Organic Manure Controller : Exception in Fields() action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldGroupError"] = ex.Message;
                return RedirectToAction("FieldGroup", model);
            }
            //return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fields(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : Fields() post action called");
            Error error = null;
            try
            {
                (List<CommonResponse> fieldList, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    var selectListItem = fieldList.Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Name.ToString()
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

                            if (model.IsCheckAnswer && model.OrganicManures.Count > 0)
                            {
                                foreach (var organicManure in model.OrganicManures)
                                {
                                    if (model.ApplicationDate.HasValue)
                                    {
                                        organicManure.ApplicationDate = model.ApplicationDate.Value;
                                    }
                                    if (model.ApplicationMethod.HasValue)
                                    {
                                        organicManure.ApplicationMethodID = model.ApplicationMethod.Value;
                                    }
                                    if (model.ApplicationRate.HasValue)
                                    {
                                        organicManure.ApplicationRate = model.ApplicationRate.Value;
                                    }
                                    if (model.Area.HasValue)
                                    {
                                        organicManure.AreaSpread = model.Area.Value;
                                    }
                                    if (model.Quantity.HasValue)
                                    {
                                        organicManure.ManureQuantity = model.Quantity.Value;
                                    }
                                    organicManure.ManureTypeID = model.ManureTypeId.Value;
                                    if (model.TotalRainfall.HasValue)
                                    {
                                        organicManure.Rainfall = model.TotalRainfall.Value;
                                    }
                                    if (model.IsDefaultNutrientValues.HasValue && (!model.IsDefaultNutrientValues.Value))
                                    {
                                        organicManure.DryMatterPercent = model.DryMatterPercent.Value;
                                        organicManure.K2O = model.K2O.Value;
                                        organicManure.MgO = model.MgO.Value;
                                        organicManure.N = model.N.Value;
                                        organicManure.NH4N = model.NH4N.Value;
                                        organicManure.NO3N = model.NO3N.Value;
                                        organicManure.P2O5 = model.P2O5.Value;
                                        organicManure.SO3 = model.SO3.Value;
                                        organicManure.UricAcid = model.UricAcid.Value;
                                    }
                                    else
                                    {
                                        if (model.ManureType != null)
                                        {
                                            organicManure.DryMatterPercent = model.ManureType.DryMatter.Value;
                                            organicManure.K2O = model.ManureType.K2O.Value;
                                            organicManure.MgO = model.ManureType.MgO.Value;
                                            organicManure.N = model.ManureType.TotalN.Value;
                                            organicManure.NH4N = model.ManureType.NH4N.Value;
                                            organicManure.NO3N = model.ManureType.NO3N.Value;
                                            organicManure.P2O5 = model.ManureType.P2O5.Value;
                                            organicManure.SO3 = model.ManureType.SO3.Value;
                                            organicManure.UricAcid = model.ManureType.Uric.Value;
                                        }
                                    }
                                    if (model.IncorporationDelay.HasValue)
                                    {
                                        organicManure.IncorporationDelayID = model.IncorporationDelay.Value;
                                    }
                                    if (model.IncorporationMethod.HasValue)
                                    {
                                        organicManure.IncorporationMethodID = model.IncorporationMethod.Value;
                                    }
                                    if (model.SoilDrainageEndDate.HasValue)
                                    {
                                        organicManure.EndOfDrain = model.SoilDrainageEndDate.Value;
                                    }
                                    if (model.AutumnCropNitrogenUptake.HasValue)
                                    {
                                        organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake.Value;
                                    }
                                    if (model.WindspeedID.HasValue)
                                    {
                                        organicManure.WindspeedID = model.WindspeedID.Value;
                                    }
                                    if (model.MoistureTypeId.HasValue)
                                    {
                                        organicManure.MoistureID = model.MoistureTypeId.Value;
                                    }
                                    if (model.RainfallWithinSixHoursID.HasValue)
                                    {
                                        organicManure.RainfallWithinSixHoursID = model.RainfallWithinSixHoursID.Value;
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        TempData["FieldError"] = error.Message;
                        return View(model);
                    }
                    if (model.IsCheckAnswer && model.IsFieldGroupChange)
                    {
                        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                        {
                            OrganicManureViewModel organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                            if (organicManureViewModel != null && organicManureViewModel.FieldList.Count > 0)
                            {
                                var fieldListChange = organicManureViewModel.FieldList.Where(item1 => !model.FieldList.Any(item2 => item2 == item1)).ToList();

                                // Perform the required action for these items
                                if (fieldListChange != null && fieldListChange.Count > 0)
                                {
                                    List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                                    var crop = cropsResponse.Where(x => x.Year == model.HarvestYear);
                                    int cropTypeId = crop.Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                                    int cropCategoryId = await _mannerService.FetchCategoryIdByCropTypeIdAsync(cropTypeId);

                                    //check early and late for winter cereals and winter oilseed rape
                                    //if sowing date after 15 sept then late
                                    DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();
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
                            }
                        }
                        else
                        {
                            return RedirectToAction("FarmList", "Farm");
                        }

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
                _logger.LogTrace($"Organic Manure Controller : Exception in Fields() post action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("ManureGroup");

        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup()
        {
            _logger.LogTrace($"Organic Manure Controller : ManureGroup() action called");
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
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureGroup() action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldError"] = ex.Message;
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureGroup(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManureGroup() post action called");
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
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureGroup() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ManureGroupError"] = ex.Message;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ManureType");

        }

        [HttpGet]
        public async Task<IActionResult> ManureType()
        {
            _logger.LogTrace($"Organic Manure Controller : ManureType() action called");
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
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureType() action : {ex.Message}, {ex.StackTrace}");
                TempData["ManureGroupError"] = ex.Message;
                return RedirectToAction("ManureGroup", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureType(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManureType() post action called");
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
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureType() post action : {ex.Message}, {ex.StackTrace}");
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
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.ApplicationRate = null;
                        }
                    }

                    //if manure type is changed liquid to soild or solid to liquid then ApplicationMethod,IncorporationMethod,IncorporationDelay need to set null
                    if (organicManure.IsManureTypeLiquid.Value != model.IsManureTypeLiquid.Value)
                    {
                        model.ApplicationMethod = null;
                        model.IncorporationMethod = null;
                        model.IncorporationDelay = null;
                        model.ApplicationMethodName = string.Empty;
                        model.IncorporationMethodName = string.Empty;
                        model.IncorporationDelayName = string.Empty;
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.ApplicationMethodID = null;
                            orgManure.IncorporationDelayID = null;
                            orgManure.IncorporationMethodID = null;
                        }
                    }

                    //if manure type is changed then we need to bind default values
                    (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    if (error == null)
                    {
                        model.ManureType = manureType;
                        if (model.IsDefaultNutrientValues.HasValue && (!model.IsDefaultNutrientValues.Value))
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
                        }
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.DryMatterPercent = manureType.DryMatter;
                            orgManure.N = manureType.DryMatter;
                            orgManure.NH4N = manureType.NH4N;
                            orgManure.NO3N = manureType.NO3N;
                            orgManure.K2O = manureType.K2O;
                            orgManure.SO3 = manureType.SO3;
                            orgManure.MgO = manureType.MgO;
                            orgManure.P2O5 = manureType.P2O5;
                            orgManure.UricAcid = manureType.Uric;
                        }
                    }
                    else
                    {
                        TempData["ManureTypeError"] = error.Message;
                        return View(model);
                    }


                    //if manure type is solid then need to set application method value.
                    if (!model.IsManureTypeLiquid.Value)
                    {
                        List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                        var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();


                        (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, model.IsManureTypeLiquid.Value);
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
            _logger.LogTrace($"Organic Manure Controller : ManureApplyingDate() action called");
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
                bool isHighReadilyAvailableNitrogen = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    model.ManureTypeName = manureType.Name;
                    isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
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

                    (FieldDetailResponse fieldDetail, Error error2) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                    WarningMessage warningMessage = new WarningMessage();
                    string closedPeriod = string.Empty;
                    bool isPerennial = false;
                    if (!farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen)
                    {
                        (CropTypeResponse cropTypeResponse, Error error3) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                        if (error3 == null)
                        {
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);
                        }
                        closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);
                    }
                    else
                    {
                        List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                        int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                        isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                        int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();
                        closedPeriod = warningMessage.ClosedPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);
                    }
                    TempData["ClosedPeriod"] = closedPeriod;
                }
                model.IsWarningMsgNeedToShow = false;
                model.IsClosedPeriodWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureApplyingDate() action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureApplyingDate(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManureApplyingDate() post action called");
            try
            {
                int farmId = 0;
                Farm farm = new Farm();
                Error error = new Error();
                if (model.ApplicationDate == null)
                {
                    ModelState.AddModelError("ApplicationDate", Resource.MsgEnterADateBeforeContinuing);
                }
                if (model.ApplicationDate != null)
                {
                    if (model.ApplicationDate.Value.Date.Year > model.HarvestYear + 2 || model.ApplicationDate.Value.Date.Year < model.HarvestYear - 2)
                    {
                        ModelState.AddModelError("ApplicationDate", Resource.MsgEnterADateWithin2YearsOfTheHarvestYear);
                    }
                }

                if (!ModelState.IsValid)
                {
                    int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                    model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

                    farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

                    (farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        TempData["Error"] = error.Message;
                    }

                    bool isHighReadilyAvailableNitrogen = false;
                    if (error == null && manureTypeList.Count > 0)
                    {
                        var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                        model.ManureTypeName = manureType.Name;
                        isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                        ViewBag.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
                    }
                    if (farm != null)
                    {

                        (FieldDetailResponse fieldDetail, Error error2) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                        WarningMessage warningMessage = new WarningMessage();
                        string closedPeriod = string.Empty;
                        bool isPerennial = false;
                        if (farm.RegisteredOrganicProducer == false && isHighReadilyAvailableNitrogen)
                        {
                            (CropTypeResponse cropTypeResponse, Error error3) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                            if (error3 == null)
                            {
                                isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);
                            }
                            closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);
                        }
                        else
                        {
                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                            int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                            int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();

                            closedPeriod = warningMessage.ClosedPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);
                        }
                        ViewBag.ClosedPeriod = closedPeriod;

                    }
                    return View(model);
                }

                //check for closed period warning.
                if (model != null)
                {
                    OrganicManureViewModel organicManureViewModel = new OrganicManureViewModel();
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                    {
                        organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                    }

                    if (model.ApplicationDate != organicManureViewModel.ApplicationDate)
                    {
                        model.IsWarningMsgNeedToShow = false;
                    }
                }
                model.IsClosedPeriodWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                    (farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        TempData["ManureApplyingDateError"] = error.Message;
                        return View(model);
                    }
                    else
                    {
                        if (farm != null)
                        {
                            bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                            if (model.FieldList != null && model.FieldList.Count > 0)
                            {
                                foreach (var fieldId in model.FieldList)
                                {
                                    Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                                    if (field != null)
                                    {

                                        if (field.IsWithinNVZ.Value)
                                        {
                                            (string closedPeriod, string warningMsg, string SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error) = await IsClosedPeriodWarningMessage(model, field.IsWithinNVZ.Value, farm.RegisteredOrganicProducer.Value);
                                            if (error == null)
                                            {
                                                if (!string.IsNullOrWhiteSpace(closedPeriod))
                                                {
                                                    TempData["ClosedPeriod"] = closedPeriod;
                                                }
                                                if (!string.IsNullOrWhiteSpace(warningMsg))
                                                {
                                                    TempData["ClosedPeriodWarningDetail"] = warningMsg;
                                                }
                                                if (!string.IsNullOrWhiteSpace(SlurryOrPoultryManureExistWithinLast20Days))
                                                {
                                                    TempData["SlurryOrPoultryManureExistWithinLast20Days"] = SlurryOrPoultryManureExistWithinLast20Days;
                                                }
                                            }
                                            else
                                            {
                                                TempData["ManureApplyingDateError"] = error.Message;
                                                return View(model);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }

                if (model.IsClosedPeriodWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return View(model);
                    }
                }
                else
                {
                    model.IsWarningMsgNeedToShow = false;
                    model.IsClosedPeriodWarning = false;
                    model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                }

                if (model.OrganicManures.Count > 0)
                {
                    foreach (var orgManure in model.OrganicManures)
                    {
                        orgManure.ApplicationDate = model.ApplicationDate.Value;
                    }
                }
                //model.IsWarningMsgNeedToShow = false;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

                if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("ApplicationMethod");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in ManureApplyingDate() post action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> ApplicationMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationMethod() action called");
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

            (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, isLiquid);
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
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.IncorporationDelayID = null;
                            orgManure.IncorporationMethodID = null;
                        }
                    }
                    if (!(model.IsFieldGroupChange) && !(model.IsManureTypeChange))
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
            _logger.LogTrace($"Organic Manure Controller : ApplicationMethod() post action called");
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


                (List<ApplicationMethodResponse> applicationMethodList, error) = await _organicManureService.FetchApplicationMethodList(fieldType ?? 0, isLiquid);
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
                    string applicableFor =Resource.lblNull;
                    List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                    var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                    (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableFor);
                    if (error == null && incorporationMethods.Count == 1)
                    {
                        model.IncorporationMethod = incorporationMethods.FirstOrDefault().ID;
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
            _logger.LogTrace($"Organic Manure Controller : DefaultNutrientValues() action called");
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }


            if (model.IsCheckAnswer && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
                && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange))
            {
                model.IsDefaultNutrientOptionChange = true;
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
            _logger.LogTrace($"Organic Manure Controller : DefaultNutrientValues() post action called");
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
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
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

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                if (model.IsCheckAnswer && model.IsDefaultNutrientOptionChange && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
                && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            return RedirectToAction("ApplicationRateMethod");
        }

        [HttpGet]
        public async Task<IActionResult> ManualNutrientValues()
        {
            _logger.LogTrace($"Organic Manure Controller : ManualNutrientValues() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : ManualNutrientValues() post action called");
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
                _logger.LogTrace($"Organic Manure Controller : Exception in ManualNutrientValues() post action : {ex.Message}, {ex.StackTrace}");
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
            _logger.LogTrace($"Organic Manure Controller : NutrientValuesStoreForFuture() post action called");
            if (model.IsAnyNeedToStoreNutrientValueForFuture == null)
            {
                ModelState.AddModelError("IsAnyNeedToStoreNutrientValueForFuture", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer && model.IsDefaultNutrientOptionChange && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
               && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange))
            {
                return RedirectToAction("CheckAnswer");
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ApplicationRateMethod");
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationRateMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationRateMethod() action called");
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
            model.IsWarningMsgNeedToShow = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View(model);

        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationRateMethod() post action called");
            try
            {
                Error error = null;
                if (model.ApplicationRateMethod == null)
                {
                    ModelState.AddModelError("ApplicationRate", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
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
                    model.IsNMaxLimitWarning = false;
                    model.IsOrgManureNfieldLimitWarning = false;
                    model.IsEndClosedPeriodFebruaryWarning = false;
                    OrganicManureViewModel? organicManureViewModel = new OrganicManureViewModel();
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                    {
                        organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if (organicManureViewModel != null)
                    {
                        if (model.ApplicationRate != organicManureViewModel.ApplicationRate)
                        {
                            model.IsWarningMsgNeedToShow = false;
                        }
                    }

                    if (model.FieldList != null && model.FieldList.Count > 0)
                    {
                        foreach (var fieldId in model.FieldList)
                        {
                            Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                            if (field != null)
                            {
                                bool isFieldIsInNVZ = field.IsWithinNVZ != null ? field.IsWithinNVZ.Value : false;
                                if (isFieldIsInNVZ)
                                {
                                    (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null);
                                    if (error == null)
                                    {
                                        if (managementIds.Count > 0)
                                        {
                                            (model.IsOrgManureNfieldLimitWarning, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0]);
                                            if (error == null)
                                            {
                                                (model.IsNMaxLimitWarning, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0]);
                                                if (error == null)
                                                {
                                                    (model.IsEndClosedPeriodFebruaryWarning, string message, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId));
                                                    if (error == null)
                                                    {
                                                        if (!string.IsNullOrWhiteSpace(message))
                                                        {
                                                            TempData["EndClosedPeriodAndFebruaryWarningMessage"] = message;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ViewBag.Error = error.Message;
                                                        return View(model);
                                                    }
                                                }
                                                else
                                                {
                                                    ViewBag.Error = error.Message;
                                                    return View(model);
                                                }
                                            }
                                            else
                                            {
                                                ViewBag.Error = error.Message;
                                                return View(model);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ViewBag.Error = error.Message;
                                        return View(model);
                                    }
                                }
                            }
                        }
                    }
                }
                if (model.IsOrgManureNfieldLimitWarning || model.IsNMaxLimitWarning || model.IsEndClosedPeriodFebruaryWarning)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return View(model);
                    }
                }
                else
                {
                    model.IsWarningMsgNeedToShow = false;
                    if (model.IsOrgManureNfieldLimitWarning)
                    {
                        model.IsOrgManureNfieldLimitWarning = false;
                    }
                    if (model.IsNMaxLimitWarning)
                    {
                        model.IsNMaxLimitWarning = false;
                    }
                    if (model.IsEndClosedPeriodFebruaryWarning)
                    {
                        model.IsEndClosedPeriodFebruaryWarning = false;
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("IncorporationMethod");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in ApplicationRateMethod() post action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate()
        {
            _logger.LogTrace($"Organic Manure Controller : ManualApplicationRate() action called");
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
                model.IsWarningMsgNeedToShow = false;
                model.IsOrgManureNfieldLimitWarning = false;
                model.IsNMaxLimitWarning = false;
                model.IsEndClosedPeriodFebruaryWarning = false;
                model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = false;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in ManualApplicationRate() action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualApplicationRate(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManualApplicationRate() post action called");
            Error? error = null;
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
            if (model.ApplicationRate != null && model.ApplicationRate < 0)
            {
                ModelState.AddModelError("ApplicationRate", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }
            if (!ModelState.IsValid)
            {
                return View("ManualApplicationRate", model);
            }
            model.IsNMaxLimitWarning = false;
            model.IsOrgManureNfieldLimitWarning = false;
            model.IsEndClosedPeriodFebruaryWarning = false;
            model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = false;

            string message = string.Empty;
            //Error error = null;
            OrganicManureViewModel? organicManureViewModel = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (organicManureViewModel != null)
            {
                if (model.ApplicationRate != organicManureViewModel.ApplicationRate)
                {
                    model.IsWarningMsgNeedToShow = false;
                }
            }

            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                foreach (var fieldId in model.FieldList)
                {
                    Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                    if (field != null)
                    {
                        bool isFieldIsInNVZ = field.IsWithinNVZ != null ? field.IsWithinNVZ.Value : false;
                        if (isFieldIsInNVZ)
                        {
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    (model.IsOrgManureNfieldLimitWarning, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0]);
                                    if (error == null)
                                    {
                                        (model.IsNMaxLimitWarning, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0]);
                                        if (error == null)
                                        {
                                            (model.IsEndClosedPeriodFebruaryWarning, message, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId));
                                            if (error == null)
                                            {
                                                if (!string.IsNullOrWhiteSpace(message))
                                                {
                                                    TempData["EndClosedPeriodAndFebruaryWarningMessage"] = message;
                                                }
                                            }
                                            else
                                            {
                                                TempData["ManualApplicationRateError"] = error.Message;
                                                return View(model);
                                            }

                                        }
                                        else
                                        {
                                            TempData["ManualApplicationRateError"] = error.Message;
                                            return View(model);
                                        }

                                        //Closed period and maximum application rate for high N organic manure on a registered organic farm message - Max Application Rate - Warning Message

                                        (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, message, error) = await IsClosedPeriodStartAndEndFebExceedNRateException(model, Convert.ToInt32(fieldId));
                                        if (error == null)
                                        {
                                            if (!string.IsNullOrWhiteSpace(message))
                                            {
                                                TempData["AppRateExceeds150WithinClosedPeriodOrganic"] = message;
                                            }
                                        }
                                        else
                                        {
                                            TempData["ManualApplicationRateError"] = error.Message;
                                            return View(model);
                                        }
                                    }
                                    else
                                    {
                                        TempData["ManualApplicationRateError"] = error.Message;
                                        return View(model);
                                    }
                                }
                            }
                            else
                            {
                                TempData["ManualApplicationRateError"] = error.Message;
                                return View(model);
                            }
                        }
                    }
                }
            }

            if (model.IsOrgManureNfieldLimitWarning || model.IsNMaxLimitWarning || model.IsEndClosedPeriodFebruaryWarning || model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    return View(model);
                }
            }
            else
            {
                model.IsWarningMsgNeedToShow = false;
                if (model.IsOrgManureNfieldLimitWarning)
                {
                    model.IsOrgManureNfieldLimitWarning = false;
                }
                if (model.IsNMaxLimitWarning)
                {
                    model.IsNMaxLimitWarning = false;
                }
                if (model.IsEndClosedPeriodFebruaryWarning)
                {
                    model.IsEndClosedPeriodFebruaryWarning = false;
                }
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
            model.IsWarningMsgNeedToShow = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("IncorporationMethod");
        }

        [HttpGet]
        public async Task<IActionResult> AreaQuantity()
        {
            _logger.LogTrace($"Organic Manure Controller : AreaQuantity() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsWarningMsgNeedToShow = false;
            model.IsOrgManureNfieldLimitWarning = false;
            model.IsNMaxLimitWarning = false;
            model.IsEndClosedPeriodFebruaryWarning = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AreaQuantity(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : AreaQuantity() post action called");
            int farmId = 0;
            Farm farm = new Farm();
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
            if (model.Area != null && model.Area < 0)
            {
                ModelState.AddModelError("Area", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }
            if (model.Quantity != null && model.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }
            if (!ModelState.IsValid)
            {
                return View("AreaQuantity", model);
            }
            model.ApplicationRate = (int)Math.Round(model.Quantity.Value / model.Area.Value);
            Error error = new Error();
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AreaSpread = model.Area.Value;
                    orgManure.ManureQuantity = model.Quantity.Value;
                    orgManure.ApplicationRate = model.ApplicationRate.Value;
                }
            }
            model.IsNMaxLimitWarning = false;
            model.IsOrgManureNfieldLimitWarning = false;
            model.IsEndClosedPeriodFebruaryWarning = false;
            OrganicManureViewModel? organicManureViewModel = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (organicManureViewModel != null)
            {
                if (model.ApplicationRate != organicManureViewModel.ApplicationRate)
                {
                    model.IsWarningMsgNeedToShow = false;
                }
            }
            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                foreach (var fieldId in model.FieldList)
                {
                    Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                    if (field != null)
                    {
                        bool isFieldIsInNVZ = field.IsWithinNVZ != null ? field.IsWithinNVZ.Value : false;
                        if (isFieldIsInNVZ)
                        {
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    (model.IsOrgManureNfieldLimitWarning, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0]);
                                    if (error == null)
                                    {
                                        (model.IsNMaxLimitWarning, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0]);
                                        if (error == null)
                                        {
                                            (model.IsEndClosedPeriodFebruaryWarning, string message, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId));
                                            if (error == null)
                                            {
                                                if (!string.IsNullOrWhiteSpace(message))
                                                {
                                                    TempData["EndClosedPeriodAndFebruaryWarningMessage"] = message;
                                                }
                                            }
                                            else
                                            {
                                                TempData["AreaAndQuantityError"] = error.Message;
                                                return View(model);
                                            }
                                        }
                                        else
                                        {
                                            TempData["AreaAndQuantityError"] = error.Message;
                                            return View(model);
                                        }
                                    }
                                    else
                                    {
                                        TempData["AreaAndQuantityError"] = error.Message;
                                        return View(model);
                                    }
                                }
                            }
                            else
                            {
                                TempData["AreaAndQuantityError"] = error.Message;
                                return View(model);
                            }
                        }
                    }
                }
            }
            if (model.IsOrgManureNfieldLimitWarning || model.IsNMaxLimitWarning || model.IsEndClosedPeriodFebruaryWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    return View(model);
                }
            }
            else
            {
                model.IsWarningMsgNeedToShow = false;
                if (model.IsOrgManureNfieldLimitWarning)
                {
                    model.IsOrgManureNfieldLimitWarning = false;
                }
                if (model.IsNMaxLimitWarning)
                {
                    model.IsNMaxLimitWarning = false;
                }
                if (model.IsEndClosedPeriodFebruaryWarning)
                {
                    model.IsEndClosedPeriodFebruaryWarning = false;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("IncorporationMethod");
        }

        [HttpGet]
        public async Task<IActionResult> IncorporationMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationMethod() action called");
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
            string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
            (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
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
            _logger.LogTrace($"Organic Manure Controller : IncorporationMethod() post action called");
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
                string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
                (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);

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
            if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && (!model.IsApplicationMethodChange))
            {
                model.IsIncorporationMethodChange = true;
            }

            if (model.IncorporationMethod == (int)NMP.Portal.Enums.IncorporationMethod.NotIncorporated)
            {
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    bool isLiquid = manureType.IsLiquid.Value;
                    string applicableFor = Resource.lblNull;// isLiquid ? Resource.lblL : Resource.lblS;
                    //if (manureType.Id == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                    //{
                    //    applicableFor = Resource.lblP;
                    //}
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
                                    orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                                }
                            }
                        }
                        else
                        {
                            TempData["IncorporationMethodError"] = error.Message;
                            applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
                            string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
                            (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
                            if (error == null && incorporationMethods.Count > 0)
                            {
                                ViewBag.IncorporationMethod = incorporationMethods;
                            }
                            return View(model);
                        }

                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                    else if (error != null)
                    {
                        TempData["IncorporationMethodError"] = error.Message;
                        applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
                        List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                        var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
                        string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
                        (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
                        if (error == null && incorporationMethods.Count > 0)
                        {
                            ViewBag.IncorporationMethod = incorporationMethods;
                        }
                        return View(model);
                    }
                }
                else if (error != null)
                {
                    TempData["IncorporationMethodError"] = error.Message;
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    bool isLiquid = manureType.IsLiquid.Value;
                    string applicableFor = isLiquid ? Resource.lblL : Resource.lblB;
                    List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                    var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
                    string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
                    (List<IncorporationMethodResponse> incorporationMethods, error) = await _organicManureService.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
                    if (error == null && incorporationMethods.Count > 0)
                    {
                        ViewBag.IncorporationMethod = incorporationMethods;
                    }

                    return View(model);
                }
                if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange))
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            else
            {
                OrganicManureViewModel? organicManure = new OrganicManureViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    organicManure = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (organicManure.IncorporationMethod != null && organicManure.IncorporationMethod == (int)NMP.Portal.Enums.IncorporationMethod.NotIncorporated)
                {
                    model.IncorporationDelay = null;
                    model.IncorporationDelayName = string.Empty;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("IncorporationDelay");

        }

        [HttpGet]
        public async Task<IActionResult> IncorporationDelay()
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationDelay() action called");
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
            try
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
                if (error == null && incorporationDelaysList.Count > 0)
                {
                    ViewBag.IncorporationDelaysList = incorporationDelaysList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in IncorporationDelay() action : {ex.Message}, {ex.StackTrace}");
                TempData["IncorporationMethodError"] = ex.Message;
                return View(model);
            }
            return View(model);


        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationDelay(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationDelay() post action called");
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
                if (error == null)
                {
                    if (model.OrganicManures.Count > 0)
                    {
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                        }
                    }
                }
                else
                {
                    TempData["IncorporationDelayError"] = error.Message;
                    return View(model);
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                if ((!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && model.IsCheckAnswer)// && model.IsApplicationMethodChange)
                {
                    return RedirectToAction("CheckAnswer");
                }

                return RedirectToAction("ConditionsAffectingNutrients");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in IncorporationDelay() post action : {ex.Message}, {ex.StackTrace}");
                TempData["IncorporationDelayError"] = ex.Message;
                return View(model);
            }

        }

        [HttpGet]
        public async Task<IActionResult> ConditionsAffectingNutrients()
        {
            _logger.LogTrace($"Organic Manure Controller : ConditionsAffectingNutrients() action called");
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
                (CropTypeResponse cropsResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear.Value, false);
                //List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                //var crop = cropsResponse.Where(x => x.Year == model.HarvestYear);
                //int cropTypeId = crop.Select(x => x.CropTypeID).FirstOrDefault() ?? 0;

                if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                {
                    TempData["IncorporationDelayError"] = error.Message;
                    return RedirectToAction("IncorporationDelay");
                }
                else
                {
                    (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(cropsResponse.CropTypeId);
                    if (error == null && cropTypeLinkingResponse != null)
                    {
                        int mannerCropTypeId = cropTypeLinkingResponse.MannerCropTypeID;

                        //check early and late for winter cereals and winter oilseed rape
                        //if sowing date after 15 sept then late
                        //DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();
                        if (model.AutumnCropNitrogenUptake == null)
                        {
                            var uptakeData = new
                            {
                                cropTypeId = mannerCropTypeId,
                                applicationMonth = model.ApplicationDate.Value.Month
                            };


                            string jsonString = JsonConvert.SerializeObject(uptakeData);
                            (NitrogenUptakeResponse nitrogenUptakeResponse, error) = await _organicManureService.FetchAutumnCropNitrogenUptake(jsonString);
                            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                            {
                                TempData["IncorporationDelayError"] = error.Message;
                                return RedirectToAction("IncorporationDelay");
                            }
                            if (nitrogenUptakeResponse != null && error == null)
                            {
                                model.AutumnCropNitrogenUptake = nitrogenUptakeResponse.value;
                            }
                        }

                    }
                    else if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        TempData["IncorporationDelayError"] = error.Message;
                        return RedirectToAction("IncorporationDelay");
                    }
                    //Soil drainage end date
                    if (model.SoilDrainageEndDate == null)
                    {
                        if (model.ApplicationDate.Value.Month >= 8)
                        {
                            model.SoilDrainageEndDate = new DateTime(model.ApplicationDate.Value.AddYears(1).Year, (int)NMP.Portal.Enums.Month.March, 31);
                        }
                        else
                        {
                            model.SoilDrainageEndDate = new DateTime(model.ApplicationDate.Value.Year, (int)NMP.Portal.Enums.Month.March, 31);
                        }
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
                        (RainTypeResponse rainType, error) = await _organicManureService.FetchRainTypeById(model.RainfallWithinSixHoursID.Value);
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            ViewBag.Error = error.Message;
                            return View(model);
                        }
                        else
                        {
                            model.RainfallWithinSixHours = rainType.Name;
                        }
                    }

                    //Effective rainfall after application
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    string halfPostCode = string.Empty;
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        TempData["IncorporationDelayError"] = error.Message;
                        return RedirectToAction("IncorporationDelay");
                    }
                    else
                    {
                        halfPostCode = farm.Postcode.Substring(0, 4).Trim();
                    }

                    if (model.ApplicationDate.HasValue && model.SoilDrainageEndDate.HasValue)
                    {
                        if (model.TotalRainfall == null)
                        {
                            var rainfallPostCodeApplication = new
                            {
                                applicationDate = model.ApplicationDate.Value.ToString("yyyy-MM-dd"),
                                endOfSoilDrainageDate = model.SoilDrainageEndDate.Value.ToString("yyyy-MM-dd"),
                                climateDataPostcode = halfPostCode
                            };

                            string jsonString = JsonConvert.SerializeObject(rainfallPostCodeApplication);
                            model.TotalRainfall = await _organicManureService.FetchRainfallByPostcodeAndDateRange(jsonString);
                        }
                    }

                    //Windspeed during application 
                    if (model.WindspeedID == null)
                    {
                        (WindspeedResponse windspeed, error) = await _organicManureService.FetchWindspeedDataDefault();
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            TempData["IncorporationDelayError"] = error.Message;
                            return RedirectToAction("IncorporationDelay");
                        }
                        else
                        {
                            model.WindspeedID = windspeed.ID;
                            model.Windspeed = windspeed.Name;
                        }
                    }
                    else
                    {
                        (WindspeedResponse windspeed, error) = await _organicManureService.FetchWindspeedById(model.WindspeedID.Value);
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            TempData["IncorporationDelayError"] = error.Message;
                            return RedirectToAction("IncorporationDelay");
                        }
                        else
                        {
                            model.Windspeed = windspeed.Name;
                        }
                    }

                    //Topsoil moisture
                    if (model.MoistureTypeId == null)
                    {
                        (MoistureTypeResponse moisterType, error) = await _organicManureService.FetchMoisterTypeDefaultByApplicationDate(model.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            TempData["IncorporationDelayError"] = error.Message;
                            return RedirectToAction("IncorporationDelay");
                        }
                        else
                        {
                            model.MoistureType = moisterType.Name;
                            model.MoistureTypeId = moisterType.ID;
                        }
                    }
                    else
                    {
                        (MoistureTypeResponse moisterType, error) = await _organicManureService.FetchMoisterTypeById(model.MoistureTypeId.Value);
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            TempData["IncorporationDelayError"] = error.Message;
                            return RedirectToAction("IncorporationDelay");
                        }
                        else
                        {
                            model.MoistureType = moisterType.Name;
                        }
                    }
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in ConditionsAffectingNutrients() action : {ex.Message}, {ex.StackTrace}");
                TempData["IncorporationDelayError"] = ex.Message;
                return RedirectToAction("IncorporationDelay");
            }


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConditionsAffectingNutrients(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ConditionsAffectingNutrients() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : BackActionForManureGroup() action called");
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

                if (!model.IsFieldGroupChange)
                {
                    return RedirectToAction("CheckAnswer");
                }
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
            _logger.LogTrace($"Organic Manure Controller : CheckAnswer() action called");
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

                (List<CommonResponse> fieldList, Error error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll))
                    {
                        if (fieldList.Count > 0)
                        {
                            var fieldNames = fieldList
                                             .Where(field => model.FieldList.Contains(field.Id.ToString())).OrderBy(field => field.Name)
                                             .Select(field => field.Name)
                                             .ToList();
                            ViewBag.SelectedFields = fieldNames.OrderBy(name => name).ToList();
                            ViewBag.Fields = fieldList;
                        }
                    }
                }
                else
                {
                    TempData["ConditionsAffectingNutrientsError"] = error.Message;
                    return RedirectToAction("ConditionsAffectingNutrients");
                }
                string message = string.Empty;
                model.IsOrgManureNfieldLimitWarning = false;
                model.IsNMaxLimitWarning = false;
                model.IsEndClosedPeriodFebruaryWarning = false;
                model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = false;
                if (model.FieldList != null && model.FieldList.Count > 0)
                {
                    foreach (var fieldId in model.FieldList)
                    {
                        Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                        if (field != null)
                        {
                            bool isFieldIsInNVZ = field.IsWithinNVZ != null ? field.IsWithinNVZ.Value : false;
                            if (isFieldIsInNVZ)
                            {
                                (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null);
                                if (error == null)
                                {
                                    if (managementIds.Count > 0)
                                    {
                                        (model.IsOrgManureNfieldLimitWarning, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0]);
                                        if (error == null)
                                        {
                                            (model.IsNMaxLimitWarning, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0]);
                                            if (error == null)
                                            {
                                                (model.IsEndClosedPeriodFebruaryWarning, message, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId));
                                                if (error != null)
                                                {
                                                    TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                                    return RedirectToAction("ConditionsAffectingNutrients");
                                                }
                                            }
                                            else
                                            {
                                                TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                                return RedirectToAction("ConditionsAffectingNutrients");
                                            }
                                        }
                                        else
                                        {
                                            TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                            return RedirectToAction("ConditionsAffectingNutrients");
                                        }


                                        (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, message, error) = await IsClosedPeriodStartAndEndFebExceedNRateException(model, Convert.ToInt32(fieldId));
                                        if (error == null)
                                        {
                                            if (!string.IsNullOrWhiteSpace(message))
                                            {
                                                TempData["AppRateExceeds150WithinClosedPeriodOrganic"] = message;
                                            }
                                        }
                                        else
                                        {
                                            TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                            return RedirectToAction("ConditionsAffectingNutrients");
                                        }
                                    }
                                }
                                else
                                {
                                    TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                    return RedirectToAction("ConditionsAffectingNutrients");
                                }
                            }
                        }
                    }
                }


                model.IsClosedPeriodWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
                    if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                    {
                        TempData["ConditionsAffectingNutrientsError"] = error.Message;
                        return RedirectToAction("ConditionsAffectingNutrients");
                    }
                    else
                    {
                        if (farm != null)
                        {
                            bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                            if (model.FieldList != null && model.FieldList.Count > 0)
                            {
                                foreach (var fieldId in model.FieldList)
                                {
                                    Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                                    if (field != null)
                                    {

                                        if (field.IsWithinNVZ.Value)
                                        {
                                            (string closedPeriod, string warningMsg, string SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error) = await IsClosedPeriodWarningMessage(model, field.IsWithinNVZ.Value, farm.RegisteredOrganicProducer.Value);
                                            if (error != null)
                                            {
                                                TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                                return RedirectToAction("ConditionsAffectingNutrients");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (model.IsNMaxLimitWarning || model.IsOrgManureNfieldLimitWarning || model.IsClosedPeriodWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks)
                {
                    model.IsWarningMsgNeedToShow = true;
                }
                model.IsCheckAnswer = true;
                model.IsManureTypeChange = false;
                model.IsApplicationMethodChange = false;
                model.IsFieldGroupChange = false;
                model.IsIncorporationMethodChange = false;
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
                _logger.LogTrace($"Organic Manure Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ConditionsAffectingNutrientsError"] = ex.Message;
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> CheckAnswer(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : CheckAnswer() post action called");
            try
            {
                if (model.ManureTypeId == null)
                {
                    ModelState.AddModelError("ManureTypeId", Resource.MsgManureTypeNotSet);
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
                if (model.IncorporationMethod == null)
                {
                    ModelState.AddModelError("IncorporationMethod", string.Format(Resource.MsgIncorporationMethodNotSet, model.ManureTypeName));
                }
                if (model.IncorporationDelay == null)
                {
                    ModelState.AddModelError("IncorporationDelay", string.Format(Resource.MsgIncorporationDelayNotSet, model.ManureTypeName));
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
                    model.OrganicManures.ForEach(x => x.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake.Value);
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
                    (List<CommonResponse> organicManureField, error) = await _organicManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                    if (error == null)
                    {
                        if (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count < organicManureField.Count)
                        {

                            List<string> fieldNames = model.FieldList
                           .Select(id => organicManureField.FirstOrDefault(f => f.Id == Convert.ToInt64(id))?.Name).ToList();
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
                _logger.LogTrace($"Organic Manure Controller : Exception in CheckAnswer() post action : {ex.Message}, {ex.StackTrace}");
                TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                return View(model);
            }
            return View(model);

        }
        
        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Organic Manure Controller : BackCheckAnswer() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptake() action called");
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
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptake() post action called");
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
            if (model.AutumnCropNitrogenUptake != null && model.AutumnCropNitrogenUptake < 0)
            {
                ModelState.AddModelError("AutumnCropNitrogenUptake", Resource.MsgEnterANumberWhichIsGreaterThanZero);
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
            _logger.LogTrace($"Organic Manure Controller : SoilDrainageEndDate() action called");
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
            _logger.LogTrace($"Organic Manure Controller : SoilDrainageEndDate() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : RainfallWithinSixHour() action called");
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
            _logger.LogTrace($"Organic Manure Controller : RainfallWithinSixHour() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfall() action called");
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
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfall() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfallManual() action called");
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
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfallManual() post action called");
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
            if (model.TotalRainfall != null && model.TotalRainfall < 0)
            {
                ModelState.AddModelError("TotalRainfall", Resource.MsgEnterANumberWhichIsGreaterThanZero);
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
            _logger.LogTrace($"Organic Manure Controller : Windspeed() action called");
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
            _logger.LogTrace($"Organic Manure Controller : Windspeed() post action called");
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
            _logger.LogTrace($"Organic Manure Controller : TopsoilMoisture() action called");
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
            _logger.LogTrace($"Organic Manure Controller : TopsoilMoisture() post action called");
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
        private async Task<(bool, Error?)> IsNFieldLimitWarningMessage(OrganicManureViewModel model, bool isFieldIsInNVZ, int managementId)
        {
            Error? error = null;
            //bool IsOrgManureNfieldLimitWarning = false;
            decimal totalNitrogen = 0;
            if (model.OrganicManures != null && model.OrganicManures.Any())
            {
                foreach (var organicManure in model.OrganicManures)
                {
                    totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                    break;
                }
            }

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                decimal totalN = 0;
                DateTime startDate = model.ApplicationDate.Value.AddDays(-364);
                DateTime endDate = model.ApplicationDate.Value;

                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endDate, false);

                if (error == null)
                {
                    totalN = totalN + (totalNitrogen * model.ApplicationRate.Value);
                    if (totalN > 250)
                    {
                        model.IsOrgManureNfieldLimitWarning = true;
                    }
                }
                else
                {
                    return (model.IsOrgManureNfieldLimitWarning, error);
                }
            }

            return (model.IsOrgManureNfieldLimitWarning, error);
        }
        private async Task<(bool, Error?)> IsNMaxWarningMessage(OrganicManureViewModel model, int fieldId, int managementId)
        {
            Error? error = null;
            //bool IsNMaxLimitWarning = false;
            decimal totalNitrogen = 0;
            if (model.OrganicManures != null && model.OrganicManures.Any())
            {
                foreach (var organicManure in model.OrganicManures)
                {
                    totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                    break;
                }
            }

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                decimal totalN = 0;
                DateTime startDate = model.ApplicationDate.Value.AddDays(-364);
                DateTime endDate = model.ApplicationDate.Value;
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(fieldId));
                var crop = cropsResponse.Where(x => x.Year == model.HarvestYear && x.Confirm == false).ToList();
                if (crop != null)
                {
                    (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endDate, false);

                    (CropTypeLinkingResponse cropTypeLinking, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(crop[0].CropTypeID.Value);
                    if (error == null)
                    {
                        if (cropTypeLinking.NMaxLimit > 0)
                        {
                            (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false);
                                if (error == null)
                                {
                                    decimal nMaxLimit = 0;
                                    totalN = totalN + (totalNitrogen * model.ApplicationRate.Value);
                                    (List<int> currentYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                    (List<int> previousYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
                                    if (error == null)
                                    {
                                        nMaxLimit = cropTypeLinking.NMaxLimit ?? 0;
                                        string cropInfo1 = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(crop[0].CropTypeID.Value, crop[0].CropInfo1.Value);
                                        OrganicManureNMaxLimitLogic organicManureNMaxLimitLogic = new OrganicManureNMaxLimitLogic();
                                        nMaxLimit = organicManureNMaxLimitLogic.NMaxLimit(cropTypeLinking.NMaxLimit ?? 0, crop[0].Yield.Value, fieldDetail.SoilTypeName, cropInfo1, crop[0].CropTypeID.Value, currentYearManureTypeIds, previousYearManureTypeIds, model.ManureTypeId.Value);
                                        if (totalN > nMaxLimit)
                                        {
                                            model.IsNMaxLimitWarning = true;
                                        }
                                    }
                                    else
                                    {
                                        return (model.IsNMaxLimitWarning, error);
                                    }
                                }
                                else
                                {
                                    return (model.IsNMaxLimitWarning, error);
                                }
                            }
                            else
                            {
                                return (model.IsNMaxLimitWarning, error);
                            }
                        }
                    }
                    else
                    {
                        return (model.IsNMaxLimitWarning, error);
                    }
                }
            }
            return (model.IsNMaxLimitWarning, error);
        }
        private async Task<(bool, string, Error?)> IsEndClosedPeriodFebruaryWarningMessage(OrganicManureViewModel model, int fieldId)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            //bool IsEndClosedPeriodFebruaryWarning = false;
            //end of closed period and end of february warning message
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                return (model.IsEndClosedPeriodFebruaryWarning, warningMsg, error);
            }
            if (farm != null)
            {
                bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isHighReadilyAvailableNitrogen = false;
                if (error != null)
                {
                    return (model.IsEndClosedPeriodFebruaryWarning, warningMsg, error);
                }
                else
                {
                    if (manureTypeList.Count > 0)
                    {
                        var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                        isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                    }
                    (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                    if (error != null)
                    {
                        return (model.IsEndClosedPeriodFebruaryWarning, warningMsg, error);
                    }
                    else
                    {
                        WarningMessage warningMessage = new WarningMessage();
                        string closedPeriod = string.Empty;
                        bool isPerennial = false;
                        if (!farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear ?? 0, false);
                            if (error == null)
                            {
                                isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);
                            }
                            else
                            {
                                return (model.IsEndClosedPeriodFebruaryWarning, warningMsg, error);
                            }
                            closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);
                        }

                        if (farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen)
                        {
                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(fieldId));
                            if (cropsResponse.Count > 0)
                            {
                                //List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                                int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                                isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                                int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();
                                closedPeriod = warningMessage.ClosedPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);
                            }
                        }
                        bool isSlurry = false;
                        bool isPoultryManure = false;
                        if (model.IsManureTypeLiquid == true)
                        {
                            isSlurry = true;
                        }
                        if (model.ManureTypeId == 8)
                        {
                            isPoultryManure = true;
                        }
                        string message = warningMessage.EndClosedPeriodAndFebruaryWarningMessage(model.ApplicationDate.Value, closedPeriod, model.ApplicationRate, isSlurry, isPoultryManure);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            if (!model.IsEndClosedPeriodFebruaryWarning)
                            {
                                warningMsg = message;
                                model.IsEndClosedPeriodFebruaryWarning = true;
                            }
                        }
                    }
                }


            }
            return (model.IsEndClosedPeriodFebruaryWarning, warningMsg, error);
        }
        private async Task<(string, string, string, bool, bool, Error?)> IsClosedPeriodWarningMessage(OrganicManureViewModel model, bool isWithinNVZ, bool registeredOrganicProducer)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            string? closedPeriod = string.Empty;
            string SlurryOrPoultryManureExistWithinLast20Days = string.Empty;
            bool isWithinClosedPeriod = false;
            string message = string.Empty;
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            if (error != null)
            {
                return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
            }
            else
            {
                bool isHighReadilyAvailableNitrogen = false;
                if (manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                    ViewBag.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
                }
                (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                if (error != null)
                {
                    return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
                }
                else
                {
                    WarningMessage warningMessage = new WarningMessage();

                    bool isPerennial = false;
                    if (!registeredOrganicProducer && isHighReadilyAvailableNitrogen && isWithinNVZ)
                    {
                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                        if (error != null)
                        {
                            return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
                        }
                        else
                        {
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);

                            closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);

                            isWithinClosedPeriod = warningMessage.ClosedPeriodWarningMessage(model.ApplicationDate.Value, closedPeriod, cropTypeResponse.CropType, fieldDetail, false);
                            if (isWithinClosedPeriod)
                            {
                                warningMsg = string.Format(Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetail, cropTypeResponse.CropType, fieldDetail.SowingDate == null ? "" : fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);
                                model.IsClosedPeriodWarning = true;
                            }
                        }
                        //TempData["ClosedPeriod"] = closedPeriod;
                    }

                    //Organic farm
                    if (registeredOrganicProducer && isHighReadilyAvailableNitrogen && isWithinNVZ)
                    {
                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                        if (error != null)
                        {
                            return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
                        }
                        else
                        {
                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                            int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                            int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();
                            closedPeriod = warningMessage.ClosedPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);

                            isWithinClosedPeriod = warningMessage.ClosedPeriodWarningMessage(model.ApplicationDate.Value, closedPeriod, cropTypeResponse.CropType, fieldDetail, true);
                            if (isWithinClosedPeriod)
                            {
                                warningMsg = string.Format(Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetailOrganic, cropTypeResponse.CropType, fieldDetail.SowingDate == null ? "" : fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);
                                model.IsClosedPeriodWarning = true;
                            }
                        }
                        //TempData["ClosedPeriod"] = closedPeriod;
                    }
                    //if application date is between end of closed period and end of february.
                    //check 20 days or less since the last application of slurry or poultry manure.
                    bool isOrganicManureExist = false;
                    bool? isWithinClosedPeriodAndFebruary = warningMessage.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, closedPeriod);

                    if (isWithinClosedPeriodAndFebruary != null && isWithinClosedPeriodAndFebruary == true)
                    {

                        (isOrganicManureExist, error) = await _organicManureService.FetchOrganicManureExistanceByDateRange(model.ApplicationDate.Value.AddDays(-20).ToString("yyyy-MM-dd"), model.ApplicationDate.Value.ToString("yyyy-MM-dd"), false);
                        if (error != null)
                        {
                            return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(warningMsg) || isOrganicManureExist)
                    {
                        //if (!model.IsWarningMsgNeedToShow)
                        //{
                        if (isOrganicManureExist)
                        {
                            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                            Regex regex = new Regex(pattern);
                            Match match = regex.Match(closedPeriod);
                            if (match.Success)
                            {
                                int startDay = int.Parse(match.Groups[1].Value);
                                string startMonthStr = match.Groups[2].Value;
                                int endDay = int.Parse(match.Groups[3].Value);
                                string endMonthStr = match.Groups[4].Value;

                                DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
                                int startMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, startMonthStr) + 1;
                                int endMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;
                                string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                                SlurryOrPoultryManureExistWithinLast20Days = string.Format(Resource.lblEndClosederiodAndEndFebYouMustAllow3WeeksGap, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;
                            }

                            //}
                            //model.IsWarningMsgNeedToShow = true;
                        }

                    }
                }
            }
            return (closedPeriod, warningMsg, SlurryOrPoultryManureExistWithinLast20Days, model.IsClosedPeriodWarning, model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks, error);
        }

        private async Task<(bool, string, Error?)> IsClosedPeriodStartAndEndFebExceedNRateException(OrganicManureViewModel model, int fieldId)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }
            if (farm != null)
            {
                bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isHighReadilyAvailableNitrogen = false;

                decimal totalNitrogen = 0;
                foreach (var organicManure in model.OrganicManures)
                {
                    totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                    break;
                }

                if (error != null)
                {
                    return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
                }
                else
                {
                    if (manureTypeList.Count > 0)
                    {
                        var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                        isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
                    }
                    FieldDetailResponse fieldDetail = new FieldDetailResponse();
                    if (model.HarvestYear != null)
                    {
                        (fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                    }

                    if (error != null)
                    {
                        return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
                    }
                    else
                    {
                        WarningMessage warningMessage = new WarningMessage();
                        string? warningPeriod = string.Empty;
                        bool isWithinClosedPeriod = false;

                        Field field = await _fieldService.FetchFieldByFieldId(fieldId);
                        bool isFieldIsInNVZ = false;
                        bool isPerennial = false;
                        if (field.IsWithinNVZ != null)
                        {
                            isFieldIsInNVZ = field.IsWithinNVZ.Value;
                        }

                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear ?? 0, false);
                        if (error == null)
                        {
                            if (farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen && isFieldIsInNVZ)
                            {
                                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(fieldId));
                                if (cropsResponse.Count > 0)
                                {
                                    int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                                    isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                                    int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();
                                    warningPeriod = warningMessage.WarningPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);

                                    isWithinClosedPeriod = warningMessage.ClosedPeriodWarningMessage(model.ApplicationDate.Value, warningPeriod, cropTypeResponse.CropType, fieldDetail, true);
                                    if (isWithinClosedPeriod)
                                    {
                                        DateTime september16 = new DateTime(model.HarvestYear ?? 0, 9, 16);

                                        var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.LightSand ||
                                                                 fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow;
                                        var isFieldTypeGrass = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass;
                                        var isFieldTypeArable = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable;
                                        DateTime? sowingDate = fieldDetail.SowingDate?.ToLocalTime();

                                        DateTime endDateFebruary = new DateTime(model.HarvestYear ?? 0, 3, 1).AddDays(-1);
                                        int lastDayOfFeb = endDateFebruary.Day;

                                        decimal totalN = 0;
                                        DateTime? fromDate = null;
                                        DateTime? toDate = null;

                                        string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                                        Regex regex = new Regex(pattern);
                                        if (warningPeriod != null)
                                        {
                                            Match match = regex.Match(warningPeriod);
                                            if (match.Success)
                                            {
                                                int startDay = int.Parse(match.Groups[1].Value);
                                                string startMonthStr = match.Groups[2].Value;
                                                int endDay = int.Parse(match.Groups[3].Value);
                                                string endMonthStr = match.Groups[4].Value;

                                                DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
                                                int startMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, startMonthStr) + 1;
                                                int endMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;
                                                string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                                                (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId.ToString(), null);

                                                if (isFieldTypeGrass)
                                                {
                                                    //(totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                    decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                    if (currentNitrogen != null)
                                                    {
                                                        if (currentNitrogen > 40)
                                                        {
                                                            warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceedsHighNWithinWarningPeriodOrganic, cropTypeResponse.CropType, warningPeriod, 40);
                                                            model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                        }
                                                        else
                                                        {
                                                            fromDate = new DateTime(model.ApplicationDate.Value.Year, startMonth, startDay);
                                                            toDate = new DateTime(model.ApplicationDate.Value.Year, endMonth, endDay);

                                                            //total N within warning period
                                                            (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                            if (currentNitrogen + totalN > 150)
                                                            {
                                                                warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceedsHighNWithinWarningPeriodOrganic, cropTypeResponse.CropType, warningPeriod, 40);
                                                                model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                            }


                                                        }
                                                    }
                                                }
                                                else if ((cropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions))
                                                {
                                                    decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                    fromDate = new DateTime(model.ApplicationDate.Value.Year, startMonth, startDay);
                                                    toDate = new DateTime(model.ApplicationDate.Value.Year + 1, endMonth, endDay);

                                                    //total N within warning period
                                                    (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                    if (currentNitrogen + totalN > 150)
                                                    {
                                                        warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceeds150WithinClosedPeriodOrganic, cropTypeResponse.CropType, warningPeriod);
                                                        model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                    }

                                                }
                                                else if ((cropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese))
                                                {
                                                    totalN = 0;
                                                    fromDate = model.ApplicationDate.Value.AddDays(-28);  //check last 4 weeks application
                                                    toDate = model.ApplicationDate.Value;

                                                    if (managementIds.Count > 0)
                                                    {
                                                        (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                        decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                        if (currentNitrogen != null)
                                                        {
                                                            if (currentNitrogen + totalN > 50)
                                                            {
                                                                warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceedsHighNWithinWarningPeriodOrganic, cropTypeResponse.CropType, warningPeriod, 50);
                                                                model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                            }
                                                            else
                                                            {

                                                                fromDate = new DateTime(model.ApplicationDate.Value.Year, startMonth, startDay);
                                                                toDate = new DateTime(model.ApplicationDate.Value.Year + 1, endMonth, endDay);

                                                                //total N within warning period
                                                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                                if (currentNitrogen + totalN > 150)
                                                                {
                                                                    warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceedsHighNWithinWarningPeriodOrganic, cropTypeResponse.CropType, warningPeriod, 50);
                                                                    model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                                }

                                                            }
                                                        }
                                                    }

                                                }
                                                else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
                                                {
                                                    decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                    fromDate = new DateTime(model.ApplicationDate.Value.Year, startMonth, startDay);
                                                    toDate = new DateTime(model.ApplicationDate.Value.Year, endMonth, endDay);

                                                    //total N within warning period
                                                    (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], fromDate.Value, toDate.Value, false);
                                                    if (currentNitrogen + totalN > 150)
                                                    {
                                                        warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceeds150WithinClosedPeriodOrganic, cropTypeResponse.CropType, warningPeriod);
                                                        model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
                        }

                        //if (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150)
                        //{
                        //    warningMsg = string.Format(Resource.lblTheNVZActionProgrammeAppRateExceeds150WithinClosedPeriodOrganic, cropTypeResponse.CropType, warningPeriod);
                        //    model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                        //}

                    }
                }


            }
            return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
        }
    }
}