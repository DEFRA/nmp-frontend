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
                        model.FarmCountryId = farm.CountryID;
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
                        string fieldId = _fieldDataProtector.Unprotect(s);
                        model.FieldList.Add(fieldId);
                        (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);
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
                cropTypeList = cropTypeList.DistinctBy(x => x.CropTypeId).ToList();
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
                        cropTypeList = cropTypeList.DistinctBy(x => x.CropTypeId).ToList();
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

                int cropTypeId = 0;
                if (cropTypeList.Count > 0)
                {
                    if (int.TryParse(model.FieldGroup, out int fieldGroup))
                    {
                        List<string> cropOrderList = cropTypeList.Where(x => x.CropTypeId == fieldGroup).Select(x => x.CropOrder).ToList();
                        if (cropOrderList.Count == 1)
                        {
                            model.CropOrder = Convert.ToInt32(cropOrderList.FirstOrDefault());
                        }
                        else
                        {
                            model.CropOrder = 1;
                        }
                        //string[] parts = model.FieldGroup.Split('-');
                        //model.FieldGroup = parts[0];
                        //model.CropOrder = int.Parse(parts[1]);
                    }
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
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);
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
                                        int i = 0;
                                        model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                                        foreach (var field in model.FieldList)
                                        {
                                            int fieldId = Convert.ToInt32(field);
                                            Field fieldData = await _fieldService.FetchFieldByFieldId(fieldId);
                                            if (fieldData != null)
                                            {
                                                //if (model.AutumnCropNitrogenUptakes.Any(f => f.FieldName == fieldData.Name.ToString()))
                                                //{
                                                //    break;
                                                //}
                                                (CropTypeResponse cropsResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                                                if (cropsResponse != null)
                                                {
                                                    (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(cropsResponse.CropTypeId);

                                                    if (error == null && cropTypeLinkingResponse != null)
                                                    {
                                                        int mannerCropTypeId = cropTypeLinkingResponse.MannerCropTypeID;

                                                        var uptakeData = new
                                                        {
                                                            cropTypeId = mannerCropTypeId,
                                                            applicationMonth = model.ApplicationDate.Value.Month
                                                        };

                                                        string jsonString = JsonConvert.SerializeObject(uptakeData);
                                                        (NitrogenUptakeResponse nitrogenUptakeResponse, error) = await _organicManureService.FetchAutumnCropNitrogenUptake(jsonString);
                                                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                                        {
                                                            TempData["FieldGroupError"] = error.Message;
                                                            return View("FieldGroup", model);
                                                        }
                                                        if (nitrogenUptakeResponse != null && error == null)
                                                        {
                                                            if (model.AutumnCropNitrogenUptakes == null)
                                                            {
                                                                model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                                                            }

                                                            model.AutumnCropNitrogenUptakes.Add(new AutumnCropNitrogenUptakeDetail
                                                            {
                                                                EncryptedFieldId = _organicManureProtector.Protect(fieldId.ToString()),
                                                                FieldName = fieldData.Name ?? string.Empty,
                                                                CropTypeId = cropsResponse.CropTypeId,
                                                                CropTypeName = cropsResponse.CropType,
                                                                AutumnCropNitrogenUptake = nitrogenUptakeResponse.value
                                                            });
                                                        }
                                                    }
                                                    else if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                                    {
                                                        TempData["FieldGroupError"] = error.Message;
                                                        return View("FieldGroup", model);
                                                    }
                                                }
                                            }
                                        }
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
                                            organicManure.ManureTypeName = model.OtherMaterialName;
                                            if (model.TotalRainfall.HasValue)
                                            {
                                                organicManure.Rainfall = model.TotalRainfall.Value;
                                            }
                                            //if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (!model.DefaultNutrientValue.Value))
                                            //{
                                            if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
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
                                            //if (model.AutumnCropNitrogenUptake.HasValue)
                                            //{
                                            //    organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake.Value;
                                            //}
                                            if (model.AutumnCropNitrogenUptakes != null && model.AutumnCropNitrogenUptakes.Count > 0 && i < model.AutumnCropNitrogenUptakes.Count)
                                            {
                                                organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes[i].AutumnCropNitrogenUptake;
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
                                            i++;
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
                    (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);
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
                                int i = 0;
                                model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                                foreach (var field in model.FieldList)
                                {
                                    int fieldId = Convert.ToInt32(field);
                                    Field fieldData = await _fieldService.FetchFieldByFieldId(fieldId);
                                    if (fieldData != null)
                                    {
                                        //if (model.AutumnCropNitrogenUptakes.Any(f => f.FieldName == fieldData.Name.ToString()))
                                        //{
                                        //    break;
                                        //}
                                        (CropTypeResponse cropsResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);

                                        (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(cropsResponse.CropTypeId);

                                        if (error == null && cropTypeLinkingResponse != null)
                                        {
                                            int mannerCropTypeId = cropTypeLinkingResponse.MannerCropTypeID;

                                            var uptakeData = new
                                            {
                                                cropTypeId = mannerCropTypeId,
                                                applicationMonth = model.ApplicationDate.Value.Month
                                            };

                                            string jsonString = JsonConvert.SerializeObject(uptakeData);
                                            (NitrogenUptakeResponse nitrogenUptakeResponse, error) = await _organicManureService.FetchAutumnCropNitrogenUptake(jsonString);
                                            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                            {
                                                TempData["FieldError"] = error.Message;
                                                return View(model);
                                            }
                                            if (nitrogenUptakeResponse != null && error == null)
                                            {
                                                if (model.AutumnCropNitrogenUptakes == null)
                                                {
                                                    model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                                                }

                                                model.AutumnCropNitrogenUptakes.Add(new AutumnCropNitrogenUptakeDetail
                                                {
                                                    EncryptedFieldId = _organicManureProtector.Protect(fieldId.ToString()),
                                                    FieldName = fieldData.Name ?? string.Empty,
                                                    CropTypeId = cropsResponse.CropTypeId,
                                                    CropTypeName = cropsResponse.CropType,
                                                    AutumnCropNitrogenUptake = nitrogenUptakeResponse.value
                                                });
                                            }
                                        }
                                        else if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                        {
                                            TempData["FieldError"] = error.Message;
                                            return View(model);
                                        }
                                    }
                                }
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
                                    organicManure.ManureTypeName = model.OtherMaterialName;
                                    if (model.TotalRainfall.HasValue)
                                    {
                                        organicManure.Rainfall = model.TotalRainfall.Value;
                                    }
                                    //if (model.DefaultNutrientValue.HasValue && (!model.DefaultNutrientValue.Value))
                                    //{
                                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
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
                                    //if (model.AutumnCropNitrogenUptake.HasValue)
                                    //{
                                    //    organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake.Value;
                                    //}
                                    if (model.AutumnCropNitrogenUptakes != null && model.AutumnCropNitrogenUptakes.Count > 0 && i < model.AutumnCropNitrogenUptakes.Count)
                                    {
                                        organicManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes[i].AutumnCropNitrogenUptake;
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
                                    i++;
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
                (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
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

                if (error1 == null)
                {
                    if (farmManureTypeList.Count > 0)
                    {
                        //foreach (var farmManureType in farmManureTypeList)
                        //{
                        var filteredFarmManureTypes = farmManureTypeList
                        .Where(farmManureType => farmManureType.ManureTypeID == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials ||
                        farmManureType.ManureTypeID == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                        .ToList();
                        if (filteredFarmManureTypes != null && filteredFarmManureTypes.Count > 0)
                        {
                            var selectListItems = filteredFarmManureTypes.Select(f => new SelectListItem
                            {
                                Value = f.ManureTypeID.ToString(),
                                Text = f.ManureTypeName
                            }).OrderBy(x => x.Text).ToList();
                            ViewBag.FarmManureTypeList = selectListItems;
                        }
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
                    (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
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
                    if (error1 == null)
                    {
                        if (farmManureTypeList.Count > 0)
                        {
                            var filteredFarmManureTypes = farmManureTypeList
                            .Where(farmManureType => farmManureType.ManureTypeID == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials ||
                            farmManureType.ManureTypeID == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                            .ToList();
                            if (filteredFarmManureTypes != null && filteredFarmManureTypes.Count > 0)
                            {
                                var selectListItems = filteredFarmManureTypes.Select(f => new SelectListItem
                                {
                                    Value = f.ManureTypeID.ToString(),
                                    Text = f.ManureTypeName
                                }).OrderBy(x => x.Text).ToList();
                                ViewBag.FarmManureTypeList = selectListItems;
                            }
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
                if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {

                    (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                    if (error == null)
                    {
                        if (farmManureTypeList.Count > 0)
                        {
                            (List<CommonResponse> manureGroupList, error) = await _organicManureService.FetchManureGroupList();
                            if (error == null)
                            {
                                model.OtherMaterialName = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter)?.ManureTypeName;
                                model.ManureGroupId = manureGroupList.FirstOrDefault(x => x.Name.Equals(Resource.lblOtherOrganicMaterials, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                                model.ManureTypeId = model.ManureGroupIdForFilter;
                                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

                                return RedirectToAction("ManureApplyingDate");
                            }

                        }
                    }

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
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupIdForFilter.Value, countryId);
                if (error == null)
                {
                    if (manureTypeList.Count > 0)
                    {
                        var manures = manureTypeList.OrderBy(m => m.SortOrder).ToList();
                        var SelectListItem = manures.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name
                        }).ToList();
                        ViewBag.ManureTypeList = SelectListItem.ToList();
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

                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupIdForFilter.Value, countryId);
                if (error == null)
                {
                    if (!ModelState.IsValid)
                    {
                        if (manureTypeList.Count > 0)
                        {
                            var manures = manureTypeList.OrderBy(m => m.SortOrder).ToList();
                            var SelectListItem = manures.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name
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
                    model.DefaultNutrientValue = null;
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
                        //if (model.DefaultNutrientValue.HasValue && (!model.DefaultNutrientValue.Value))
                        //{
                        if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
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
            if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials)
            {
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                return RedirectToAction("OtherMaterialName");
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
                if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {
                    return View(model);
                }
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;
                bool isHighReadilyAvailableNitrogen = false;
                if (error == null && manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    model.ManureTypeName = manureType.Name;
                    isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                    model.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
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
                    model.ClosedPeriod = closedPeriod;
                    if (!string.IsNullOrWhiteSpace(closedPeriod))
                    {
                        int harvestYear = model.HarvestYear ?? 0;
                        int startYear = harvestYear;
                        int endYear = harvestYear + 1;
                        string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                        Regex regex = new Regex(pattern);
                        if (closedPeriod != null)
                        {
                            Match match = regex.Match(closedPeriod);
                            if (match.Success)
                            {
                                int startDay = int.Parse(match.Groups[1].Value);
                                string startMonthStr = match.Groups[2].Value;
                                int endDay = int.Parse(match.Groups[3].Value);
                                string endMonthStr = match.Groups[4].Value;

                                Dictionary<int, string> dtfi = new Dictionary<int, string>();
                                dtfi.Add(0, Resource.lblJanuary);
                                dtfi.Add(1, Resource.lblFebruary);
                                dtfi.Add(2, Resource.lblMarch);
                                dtfi.Add(3, Resource.lblApril);
                                dtfi.Add(4, Resource.lblMay);
                                dtfi.Add(5, Resource.lblJune);
                                dtfi.Add(6, Resource.lblJuly);
                                dtfi.Add(7, Resource.lblAugust);
                                dtfi.Add(8, Resource.lblSeptember);
                                dtfi.Add(9, Resource.lblOctober);
                                dtfi.Add(10, Resource.lblNovember);
                                dtfi.Add(11, Resource.lblDecember);
                                int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1; // Array.IndexOf(dtfi.Values, startMonthStr) + 1;
                                int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;//Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;
                                if (startMonth <= endMonth)
                                {
                                    model.ClosedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                    model.ClosedPeriodEndDate = new DateTime(harvestYear - 1, endMonth, endDay);
                                }
                                else if (startMonth >= endMonth)
                                {
                                    model.ClosedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                    model.ClosedPeriodEndDate = new DateTime(harvestYear, endMonth, endDay);
                                }
                                string formattedStartDate = model.ClosedPeriodStartDate?.ToString("d MMMM yyyy");
                                string formattedEndDate = model.ClosedPeriodEndDate?.ToString("d MMMM yyyy");
                                model.ClosedPeriodForUI = $"{formattedStartDate} to {formattedEndDate}";
                            }
                        }


                    }
                    foreach (var fieldId in model.FieldList)
                    {
                        Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                        if (field != null && field.IsWithinNVZ==true)
                        {
                            model.IsWithinNVZ = true;
                        }
                    }
                        
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

                DateTime minDate = new DateTime(model.HarvestYear.Value - 1, 8, 01);
                DateTime maxDate = new DateTime(model.HarvestYear.Value, 7, 31);

                if (model.ApplicationDate > maxDate)
                {
                    ModelState.AddModelError("ApplicationDate", string.Format(Resource.MsgManureApplicationMaxDate, model.HarvestYear.Value, maxDate.Date.ToString("dd MMMM yyyy")));
                }
                if (model.ApplicationDate < minDate)
                {
                    ModelState.AddModelError("ApplicationDate", string.Format(Resource.MsgManureApplicationMinDate, model.HarvestYear.Value, minDate.Date.ToString("dd MMMM yyyy")));
                }

                if (!ModelState.IsValid)
                {
                    string formattedStartDate = model.ClosedPeriodStartDate?.ToString("d MMMM yyyy");
                    string formattedEndDate = model.ClosedPeriodEndDate?.ToString("d MMMM yyyy");
                    model.ClosedPeriodForUI = $"{formattedStartDate} to {formattedEndDate}";
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
                                            if (!(model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                            {
                                                (model, error) = await IsClosedPeriodWarningMessage(model, field.IsWithinNVZ.Value, farm.RegisteredOrganicProducer.Value, false);

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

        private async Task PopulateManureApplyingDateModel(OrganicManureViewModel model)
        {
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
            (List<ManureType> manureTypeList, Error error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (Farm farm, Error farmError) = await _farmService.FetchFarmByIdAsync(farmId);
            if (farmError != null && !string.IsNullOrWhiteSpace(farmError.Message))
            {
                TempData["Error"] = farmError.Message;
            }

            bool isHighReadilyAvailableNitrogen = false;
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                model.ManureTypeName = manureType?.Name;
                isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
                ViewBag.HighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen;
            }

            if (farm != null)
            {
                (FieldDetailResponse fieldDetail, Error fieldError) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(
                    Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                WarningMessage warningMessage = new WarningMessage();
                string closedPeriod = string.Empty;
                bool isPerennial = false;

                if (farm.RegisteredOrganicProducer == false && isHighReadilyAvailableNitrogen)
                {
                    (CropTypeResponse cropTypeResponse, Error cropTypeError) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(
                        Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);

                    if (cropTypeError == null)
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

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(a=>a.SortOrder).ToList();
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
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(a => a.SortOrder).ToList(); 
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
                    string applicableFor = Resource.lblNull;
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

            try
            {
                if (model.IsCheckAnswer && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
                    && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange))
                {
                    model.IsDefaultNutrientOptionChange = true;
                }
                Error? error = null;
                FarmManureTypeResponse? farmManure = null;

                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {
                    if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                    {
                        (ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;
                        // (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error == null)
                        {
                            if (farmManureTypeList.Count > 0)
                            {
                                farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter);
                                if (farmManure != null)
                                {
                                    model.ManureType.DryMatter = farmManure.DryMatter;
                                    model.ManureType.TotalN = farmManure.TotalN;
                                    model.ManureType.NH4N = farmManure.NH4N;
                                    model.ManureType.Uric = farmManure.Uric;
                                    model.ManureType.NO3N = farmManure.NO3N;
                                    model.ManureType.P2O5 = farmManure.P2O5;
                                    model.ManureType.K2O = farmManure.K2O;
                                    model.ManureType.SO3 = farmManure.SO3;
                                    model.ManureType.MgO = farmManure.MgO;
                                    model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                                }
                                else
                                {
                                    model.DefaultFarmManureValueDate = null;
                                }
                            }
                        }
                        if (manureTypeError == null)
                        {
                            model.ManureType = manureType;
                        }
                        model.IsDefaultNutrient = true;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                    }
                    else
                    {
                        model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                        return RedirectToAction("ManualNutrientValues");
                    }
                }
                else
                {
                    (ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

                    if (error == null && farmManureTypeList.Count > 0)//&&(string.IsNullOrWhiteSpace(model.DefaultNutrientValue) || model.DefaultNutrientValue== Resource.lblYesUseTheseValues))
                    {
                        farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                        if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
                        {
                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                            }
                            else
                            {
                                if (manureTypeError == null)
                                {
                                    model.ManureType = manureType;
                                }
                            }
                        }
                        else
                        {
                            if (farmManure != null)
                            {
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                                if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseValues) || (model.IsThisDefaultValueOfRB209 != null && (!model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                                }
                                else if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues) || (model.IsThisDefaultValueOfRB209 != null && (model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.FarmManureApiOption = null;
                                    ViewBag.RB209ApiOption = Resource.lblTrue;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (manureTypeError == null)
                        {
                            model.ManureType = manureType;
                        }
                    }
                }

                model.IsDefaultNutrient = true;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);

            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefaultNutrientValues(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : DefaultNutrientValues() post action called");
            if (model.DefaultNutrientValue == null)
            {
                ModelState.AddModelError("DefaultNutrientValue", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                Error? error = null;
                FarmManureTypeResponse? farmManure = null;

                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {
                    if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                    {
                        (ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;
                        // (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error == null)
                        {
                            if (farmManureTypeList.Count > 0)
                            {
                                farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter);
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                            }
                        }
                        if (manureTypeError == null)
                        {
                            model.ManureType = manureType;
                        }
                        model.IsDefaultNutrient = true;

                    }
                    else
                    {
                        model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;

                    }
                }
                else
                {
                    (ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

                    if (error == null && farmManureTypeList.Count > 0)//&&(string.IsNullOrWhiteSpace(model.DefaultNutrientValue) || model.DefaultNutrientValue== Resource.lblYesUseTheseValues))
                    {
                        farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                        if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
                        {
                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                            }
                            else
                            {
                                if (manureTypeError == null)
                                {
                                    model.ManureType = manureType;
                                }
                            }
                        }
                        else
                        {
                            if (farmManure != null)
                            {
                                if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseValues) || (model.IsThisDefaultValueOfRB209 != null && (!model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                                }
                                else if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues) || (model.IsThisDefaultValueOfRB209 != null && (model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.RB209ApiOption = Resource.lblTrue;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (manureTypeError == null)
                        {
                            model.ManureType = manureType;
                        }
                    }
                }



                //(ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                //FarmManureTypeResponse? farmManure = null;

                //(List<FarmManureTypeResponse> farmManureTypeList, Error error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                //if (error == null && farmManureTypeList.Count > 0)
                //{
                //    farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                //    if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
                //    {
                //        if (farmManure != null)
                //        {
                //            model.ManureType.DryMatter = farmManure.DryMatter;
                //            model.ManureType.TotalN = farmManure.TotalN;
                //            model.ManureType.NH4N = farmManure.NH4N;
                //            model.ManureType.Uric = farmManure.Uric;
                //            model.ManureType.NO3N = farmManure.NO3N;
                //            model.ManureType.P2O5 = farmManure.P2O5;
                //            model.ManureType.K2O = farmManure.K2O;
                //            model.ManureType.SO3 = farmManure.SO3;
                //            model.ManureType.MgO = farmManure.MgO;
                //            if (model.ManureTypeId != (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials && model.ManureTypeId != (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                //            {
                //                ViewBag.FarmManureApiOption = Resource.lblTrue;
                //            }
                //        }
                //        else
                //        {
                //            if (manureTypeError == null)
                //            {
                //                model.ManureType = manureType;
                //            }
                //            model.DefaultNutrientValue = Resource.lblYes;
                //        }
                //    }
                //}
                //else
                //{
                //    if (manureTypeError == null)
                //    {
                //        model.ManureType = manureType;
                //    }

                //}
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
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
                OrganicManureViewModel organicManureViewModel = new OrganicManureViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
                {
                    organicManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
                }

                if (organicManureViewModel != null && (!string.IsNullOrWhiteSpace(organicManureViewModel.DefaultNutrientValue)))
                {
                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                    {
                        (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error1 == null && farmManureTypeList.Count > 0)
                        {
                            FarmManureTypeResponse farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);

                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                            }

                            model.IsThisDefaultValueOfRB209 = false;
                            if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                            {
                                if (farmManure != null)
                                {
                                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                                }
                                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                                if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (organicManureViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || organicManureViewModel.DefaultNutrientValue != Resource.lblYesUseTheseStandardNutrientValues)
                                    && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                                {
                                    return View(model);
                                }
                            }
                        }
                    }
                    else
                    {
                        (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;

                        model.IsThisDefaultValueOfRB209 = true;
                        if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                            if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (organicManureViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || organicManureViewModel.DefaultNutrientValue != Resource.lblYesUseTheseValues)
                                  && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                            {
                                return View(model);
                            }

                        }
                        if (organicManureViewModel.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                    {

                        (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error1 == null && farmManureTypeList.Count > 0)
                        {
                            FarmManureTypeResponse farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);

                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;

                            }
                            if (model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                            {
                                model.IsThisDefaultValueOfRB209 = false;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                            }
                        }
                    }
                    else
                    {
                        (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;
                        if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            model.IsThisDefaultValueOfRB209 = true;
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                            return View(model);
                        }

                    }
                }
                if (model.OrganicManures != null && model.OrganicManures.Count > 0)
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
                //model.IsAnyNeedToStoreNutrientValueForFuture = true;
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
                        ModelState["MgO"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblMagnesiumMgO));
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
                    ModelState.AddModelError("SO3", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblSulphur.ToLower()));
                }
                if (model.MgO == null)
                {
                    ModelState.AddModelError("MgO", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblMagnesiumMgO.ToLower()));
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
                        ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblMagnesiumMgO, 99));
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

                if (model.ManureType.DryMatter != model.DryMatterPercent || model.ManureType.TotalN != model.N
               || model.ManureType.NH4N != model.NH4N || model.ManureType.Uric != model.UricAcid
                || model.ManureType.NO3N != model.NO3N || model.ManureType.P2O5 != model.P2O5 ||
                model.ManureType.K2O != model.K2O || model.ManureType.MgO != model.MgO
                || model.ManureType.SO3 != model.SO3)
                {
                    model.IsAnyNeedToStoreNutrientValueForFuture = true;
                }
                else
                {
                    model.IsAnyNeedToStoreNutrientValueForFuture = false;
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
                if (model.IsCheckAnswer && model.IsDefaultNutrientOptionChange && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
                && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange))
                {
                    return RedirectToAction("CheckAnswer");
                }

                return RedirectToAction("ApplicationRateMethod");
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
            if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials)
            {
                model.IsAnyNeedToStoreNutrientValueForFuture = true;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                return RedirectToAction("ApplicationRateMethod");
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

            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                                    (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null, null);
                                    if (error == null)
                                    {
                                        if (managementIds.Count > 0)
                                        {
                                            (model, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0], false);
                                            if (error == null)
                                            {
                                                (model, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0], false);
                                                if (error == null)
                                                {
                                                    (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId), false);

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

                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
            if (model.ApplicationRate != null && model.ApplicationRate > 250)
            {
                ModelState.AddModelError("ApplicationRate", Resource.MsgForApplicationRate);
            }
            //if (model.ApplicationRate != null)
            //{
            //    string input = model.ApplicationRate.ToString();
            //    if (input.Split('.').Length > 2)
            //    {
            //        //error msg
            //    }
            //}

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
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null, null);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    (model, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0], false);
                                    if (error == null)
                                    {
                                        (model, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0], false);
                                        if (error == null)
                                        {
                                            if (!(model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                            {
                                                (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId), false);

                                            }

                                        }
                                        else
                                        {
                                            TempData["ManualApplicationRateError"] = error.Message;
                                            return View(model);
                                        }

                                        //Closed period and maximum application rate for high N organic manure on a registered organic farm message - Max Application Rate - Warning Message
                                        if (!(model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                        {
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
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    ViewBag.IsWales = farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales ? true : false;
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
            if (model.Quantity != null&& model.Area != null && model.Area > 0 && model.Quantity > 0)
            {
                model.ApplicationRate = model.Quantity.Value / model.Area.Value;

                if (model.ApplicationRate != null && model.ApplicationRate > 250)
                {
                    ModelState.AddModelError("Quantity", Resource.MsgForApplicationRate);
                }
            }
            if (!ModelState.IsValid)
            {
                return View("AreaQuantity", model);
            }
            model.ApplicationRate =Math.Round((model.Quantity.Value / model.Area.Value),1);
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
                            (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null, null);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    (model, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0], false);
                                    if (error == null)
                                    {
                                        (model, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0], false);
                                        if (error == null)
                                        {
                                            (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId), false);

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
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                ViewBag.IncorporationMethod = incorporationMethods.OrderBy(i=>i.SortOrder).ToList();
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
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {
                    if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials)
                    {
                        applicableFor = Resource.lblL;
                    }
                    else
                    {
                        applicableFor = Resource.lblS;
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
                    int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                //Autumn crop Nitrogen uptake
                if (model.AutumnCropNitrogenUptake == null)
                {
                    model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                    foreach (var field in model.FieldList)
                    {
                        int fieldId = Convert.ToInt32(field);
                        (CropTypeResponse cropsResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);

                        (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(cropsResponse.CropTypeId);

                        if (error == null && cropTypeLinkingResponse != null)
                        {
                            int mannerCropTypeId = cropTypeLinkingResponse.MannerCropTypeID;

                            //check early and late for winter cereals and winter oilseed rape
                            //if sowing date after 15 sept then late
                            //DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();

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
                                if (model.AutumnCropNitrogenUptakes == null)
                                {
                                    model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                                }
                                var fieldData = await _fieldService.FetchFieldByFieldId(fieldId);
                                model.AutumnCropNitrogenUptakes.Add(new AutumnCropNitrogenUptakeDetail
                                {
                                    EncryptedFieldId = _organicManureProtector.Protect(fieldId.ToString()),
                                    FieldName = fieldData.Name ?? string.Empty,
                                    CropTypeId = cropsResponse.CropTypeId,
                                    CropTypeName = cropsResponse.CropType,
                                    AutumnCropNitrogenUptake = nitrogenUptakeResponse.value
                                });
                            }

                        }
                        else if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            TempData["IncorporationDelayError"] = error.Message;
                            return RedirectToAction("IncorporationDelay");
                        }
                    }
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
                    halfPostCode = farm.ClimateDataPostCode.Substring(0, 4).Trim();
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
            if (!ModelState.IsValid)
            {
                return View("ConditionsAffectingNutrients", model);
            }
            if (model.OrganicManures.Count > 0)
            {
                int i = 0;
                foreach (var orgManure in model.OrganicManures)
                {
                    //orgManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0;
                    if (model.AutumnCropNitrogenUptakes != null && model.AutumnCropNitrogenUptakes.Count > 0)
                    {
                        orgManure.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes[i].AutumnCropNitrogenUptake;
                    }
                    orgManure.SoilDrainageEndDate = model.SoilDrainageEndDate.Value;
                    orgManure.RainfallWithinSixHoursID = model.RainfallWithinSixHoursID.Value;
                    orgManure.Rainfall = model.TotalRainfall.Value;
                    orgManure.WindspeedID = model.WindspeedID.Value;
                    orgManure.MoistureID = model.MoistureTypeId.Value;

                    i++;
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
                        r = _fieldDataProtector.Protect(fieldId),
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
                                (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null, null);
                                if (error == null)
                                {
                                    if (managementIds.Count > 0)
                                    {
                                        (model, error) = await IsNFieldLimitWarningMessage(model, isFieldIsInNVZ, managementIds[0], true);
                                        if (error == null)
                                        {
                                            (model, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), managementIds[0], true);
                                            if (error == null)
                                            {
                                                if (!(model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                                {
                                                    (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, Convert.ToInt32(fieldId), true);
                                                    if (error != null)
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
                                        else
                                        {
                                            TempData["ConditionsAffectingNutrientsError"] = error.Message;
                                            return RedirectToAction("ConditionsAffectingNutrients");
                                        }

                                        if (!(model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                        {
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
                                            if (!(model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials))
                                            {
                                                (model, error) = await IsClosedPeriodWarningMessage(model, field.IsWithinNVZ.Value, farm.RegisteredOrganicProducer.Value, true);
                                                if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : CheckAnswer() post action called");
            Error error = null;
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
                if (model.DefaultNutrientValue == null)
                {
                    ModelState.AddModelError("DefaultNutrientValue", string.Format(Resource.MsgDefaultNutrientValuesNotSet, model.ManureTypeName));
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
                //if (model.AutumnCropNitrogenUptake == null)
                //{
                //    ModelState.AddModelError("AutumnCropNitrogenUptake", Resource.MsgAutumnCropNitrogenUptakeNotSet);
                //}
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
                    if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                    {
                        model.OrganicManures.ForEach(x => x.ManureTypeName = model.OtherMaterialName);
                        if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                        {
                            model.OrganicManures.ForEach(x => x.ManureTypeID = model.ManureGroupIdForFilter ?? 0);
                        }
                    }
                    else
                    {
                        model.OrganicManures.ForEach(x => x.ManureTypeName = model.ManureTypeName);
                    }

                    //logic for AvailableNForNMax column that will be used to get sum of previous manure applications
                    int? percentOfTotalNForUseInNmaxCalculation = null;
                    decimal? currentApplicationNitrogen = null;
                    (ManureType manure, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                    if (manure != null)
                    {
                        percentOfTotalNForUseInNmaxCalculation = manure.PercentOfTotalNForUseInNmaxCalculation;
                    }
                    decimal totalNitrogen = 0;
                    if (percentOfTotalNForUseInNmaxCalculation != null)
                    {
                        if (model.OrganicManures != null && model.OrganicManures.Any())
                        {
                            foreach (var organicManure in model.OrganicManures)
                            {
                                totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                                break;
                            }

                            decimal decimalOfTotalNForUseInNmaxCalculation = Convert.ToDecimal(percentOfTotalNForUseInNmaxCalculation / 100.0);
                            currentApplicationNitrogen = (totalNitrogen * model.ApplicationRate.Value * decimalOfTotalNForUseInNmaxCalculation);
                        }
                    }


                    //foreach (string field in model.FieldList)
                    //{
                    (Farm farmData, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (farmData != null && (string.IsNullOrWhiteSpace(error.Message)))
                    {
                        //Field fieldData = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(field));
                        //if (fieldData != null)
                        //{
                        foreach (var organic in model.OrganicManures)
                        {
                            (ManagementPeriod managementPeriod, error) = await _cropService.FetchManagementperiodById(organic.ManagementPeriodID);
                            if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                            {
                                (Crop crop, error) = await _cropService.FetchCropById(managementPeriod.CropID.Value);
                                if (crop != null && string.IsNullOrWhiteSpace(error.Message))
                                {
                                    Field fieldData = await _fieldService.FetchFieldByFieldId(crop.FieldID.Value);
                                    if (fieldData != null)
                                    {
                                        (SoilTypeSoilTextureResponse soilTexture, error) = await _organicManureService.FetchSoilTypeSoilTextureBySoilTypeId(fieldData.SoilTypeID ?? 0);
                                        int topSoilID = 0;
                                        int subSoilID = 0;
                                        if (error == null && soilTexture != null)
                                        {
                                            topSoilID = soilTexture.TopSoilID;
                                            subSoilID = soilTexture.SubSoilID;
                                        }
                                        (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
                                        if (error == null && cropTypeLinkingResponse != null)
                                        {
                                            (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(organic.ManureTypeID);
                                            if (error == null && manureType != null)
                                            {
                                                var mannerOutput = new
                                                {
                                                    runType = farmData.EnglishRules ? 3 : 4,
                                                    postcode = farmData.ClimateDataPostCode.Split(" ")[0],
                                                    countryID = farmData.CountryID,
                                                    field = new
                                                    {
                                                        fieldID = fieldData.ID,
                                                        fieldName = fieldData.Name,
                                                        MannerCropTypeID = cropTypeLinkingResponse.MannerCropTypeID,
                                                        topsoilID = topSoilID,
                                                        subsoilID = subSoilID,
                                                        isInNVZ = Convert.ToBoolean(fieldData.IsWithinNVZ)
                                                    },
                                                    manureApplications = new[]
                                                 {
                                                        new
                                                        {
                                                            manureDetails = new
                                                            {
                                                                manureID = organic.ManureTypeID,
                                                                name = organic.ManureTypeName,
                                                                isLiquid = manureType.IsLiquid,
                                                                dryMatter = organic.DryMatterPercent,
                                                                totalN = organic.N,
                                                                nH4N = organic.NH4N,
                                                                uric = organic.UricAcid,
                                                                nO3N = organic.NO3N,
                                                                p2O5 = organic.P2O5,
                                                                k2O = organic.K2O,
                                                                sO3 = organic.SO3,
                                                                mgO = organic.MgO
                                                            },
                                                            applicationDate = organic.ApplicationDate.ToString("yyyy-MM-dd"),
                                                            applicationRate = new
                                                            {
                                                                value = organic.ApplicationRate,
                                                                unit = model.IsManureTypeLiquid.Value ? Resource.lblMeterCubePerHectare : Resource.lblTonnesPerHectare
                                                            },
                                                            applicationMethodID = organic.ApplicationMethodID,
                                                            incorporationMethodID = organic.IncorporationMethodID,
                                                            incorporationDelayID = organic.IncorporationDelayID,
                                                            autumnCropNitrogenUptake = new
                                                            {
                                                                value = organic.AutumnCropNitrogenUptake,
                                                                unit = Resource.lblKgPerHectare
                                                            },
                                                            endOfDrainageDate = organic.SoilDrainageEndDate.ToString("yyyy-MM-dd"),
                                                            rainfallPostApplication = organic.Rainfall,
                                                            windspeedID = organic.WindspeedID,
                                                            rainTypeID = organic.RainfallWithinSixHoursID,
                                                            topsoilMoistureID = organic.MoistureID
                                                        }
                                                    }
                                                };
                                                //var mannerJsonData = new
                                                //{
                                                //    mannerOutput
                                                //};
                                                string mannerJsonString = JsonConvert.SerializeObject(mannerOutput);
                                                (MannerCalculateNutrientResponse mannerCalculateNutrientResponse, error) = await _organicManureService.FetchMannerCalculateNutrient(mannerJsonString);
                                                if (error == null && mannerCalculateNutrientResponse != null)
                                                {
                                                    organic.AvailableN = mannerCalculateNutrientResponse.CurrentCropAvailableN;
                                                    organic.AvailableSO3 = mannerCalculateNutrientResponse.CropAvailableSO3;
                                                    organic.AvailableP2O5 = mannerCalculateNutrientResponse.CropAvailableP2O5;
                                                    organic.AvailableK2O = mannerCalculateNutrientResponse.CropAvailableK2O;
                                                    organic.TotalN = mannerCalculateNutrientResponse.TotalN;
                                                    organic.TotalP2O5 = mannerCalculateNutrientResponse.TotalP2O5;
                                                    organic.TotalSO3 = mannerCalculateNutrientResponse.TotalSO3;
                                                    organic.TotalK2O = mannerCalculateNutrientResponse.TotalK2O;
                                                    organic.TotalMgO = mannerCalculateNutrientResponse.TotalMgO;
                                                    organic.AvailableNForNMax = currentApplicationNitrogen != null ? currentApplicationNitrogen : mannerCalculateNutrientResponse.CurrentCropAvailableN;
                                                }
                                                else
                                                {
                                                    TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                                    return View(model);
                                                }
                                            }
                                            else
                                            {
                                                TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                                return View(model);
                                            }
                                        }
                                        else
                                        {
                                            TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                            return View(model);
                                        }
                                    }
                                    else
                                    {
                                        TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                        return View(model);
                                    }
                                }
                                else
                                {
                                    TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                    return View(model);
                                }
                            }
                            else
                            {
                                TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                                return View(model);
                            }
                        }
                        //}
                    }
                    else
                    {
                        TempData["AddOrganicManureError"] = Resource.MsgWeCounldNotAddOrganicManure;
                        return View(model);
                    }
                    //}
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
                (bool success, error) = await _organicManureService.AddOrganicManuresAsync(jsonString);
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
                    successMsg = Resource.lblOrganicManureCreatedSuccessfullyForAllField;
                    string successMsgSecond = Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation;
                    _httpContextAccessor.HttpContext?.Session.Remove("OrganicManure");
                    return RedirectToAction("HarvestYearOverview", "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear,
                        q = _farmDataProtector.Protect(success.ToString()),
                        r = _cropDataProtector.Protect(successMsg),
                        v = _cropDataProtector.Protect(successMsgSecond)
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
        public async Task<IActionResult> AutumnCropNitrogenUptake(string? f)
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
            if (f != null)
            {
                int fieldId = Convert.ToInt32(_organicManureProtector.Unprotect(f));
                Field field = await _fieldService.FetchFieldByFieldId(fieldId);
                model.EncryptedFieldId = f;
                ViewBag.FieldName = field.Name;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes?.FirstOrDefault(x => x.EncryptedFieldId == f)?.AutumnCropNitrogenUptake;
            }
            if (model.FieldList.Count == 1)
            {
                Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(model.FieldList[0]));
                ViewBag.FieldName = field.Name;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes[0].AutumnCropNitrogenUptake;
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
                Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(_organicManureProtector.Unprotect(model.EncryptedFieldId)));
                ViewBag.FieldName = field.Name;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes?.FirstOrDefault(x => x.EncryptedFieldId == model.EncryptedFieldId)?.AutumnCropNitrogenUptake;
                return View("AutumnCropNitrogenUptake", model);
            }

            if (model.FieldList.Count == 1)
            {
                model.AutumnCropNitrogenUptakes[0].AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0;

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            else
            {
                model.AutumnCropNitrogenUptakes?
                     .Where(detail => detail.EncryptedFieldId == model.EncryptedFieldId)
                     .ToList()
                     .ForEach(detail => detail.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0);

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
                return RedirectToAction("AutumnCropNitrogenUptakeDetail");
            }

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

                //if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, "SoilDrainageEndDate")))
                //{
                //    ModelState["SoilDrainageEndDate"].Errors.Clear();
                //    ModelState["SoilDrainageEndDate"].Errors.Add(Resource.MsgEnterValidDate);
                //}
                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["SoilDrainageEndDate"].Errors.Clear();
                    ModelState["SoilDrainageEndDate"].Errors.Add(Resource.MsgTheDateMustInclude);
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
        private async Task<(OrganicManureViewModel, Error?)> IsNFieldLimitWarningMessage(OrganicManureViewModel model, bool isFieldIsInNVZ, int managementId, bool isGetCheckAnswer)
        {
            Error? error = null;
            decimal defaultNitrogen = 0;
            if (model.OrganicManures != null && model.OrganicManures.Any())
            {
                foreach (var organicManure in model.OrganicManures)
                {
                    defaultNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                    break;
                }
            }

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                decimal previousAppliedTotalN = 0;
                decimal totalN = 0;
                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);

                //DateTime startDate = model.ApplicationDate.Value.AddDays(-364);
                //DateTime endDate = model.ApplicationDate.Value;

                //The planned application would result in more than 250 kg/ha of total N from all applications of any Manure type apart from ‘Green compost’ or ‘Green/food compost’, applied or planned to the field in the last 365 days up to and including the application date of the manure
                if (model.ManureTypeId != (int)NMP.Portal.Enums.ManureTypes.GreenCompost && model.ManureTypeId != (int)NMP.Portal.Enums.ManureTypes.GreenFoodCompost)
                {
                    (previousAppliedTotalN, error) = await _organicManureService.FetchTotalNBasedByManIdAppDateAndIsGreenCompost(managementId, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, false);
                    if (error == null)
                    {
                        decimal currentApplicationNitrogen = 0;
                        currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                        totalN = previousAppliedTotalN + currentApplicationNitrogen;
                        if (totalN > 250)
                        {
                            model.IsOrgManureNfieldLimitWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                {
                                    model.NmaxWarningHeading = Resource.lblThisApplicationWillTakeYouOverTheOrganicManureNFieldLimit;
                                    model.NmaxWarningPara1 = Resource.MsgIfOrganicManureNMaxLimitExceed;
                                    model.NmaxWarningPara2 = Resource.MsgIfOrganicManureNMaxLimitExceedAdditional;
                                }
                                if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                {
                                    model.NmaxWarningHeading = Resource.lblThisApplicationWillTakeYouOverTheOrganicManureNFieldLimitWales;
                                    model.NmaxWarningPara1 = Resource.MsgIfOrganicManureNMaxLimitExceedWales;
                                    model.NmaxWarningPara2 = Resource.MsgIfOrganicManureNMaxLimitExceedAdditionalWales;
                                }

                            }
                            else
                            {
                                if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                {
                                    model.NmaxWarningHeading = Resource.lblThisApplicationWillTakeYouOverTheOrganicManureNFieldLimit;
                                }
                                if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                {
                                    model.NmaxWarningHeading = Resource.lblThisApplicationWillTakeYouOverTheOrganicManureNFieldLimitWales;
                                }

                            }

                        }
                    }
                }

                if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.GreenCompost || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.GreenFoodCompost)
                {
                    var cropTypeIdsForTrigger = new HashSet<int> {
                        (int)NMP.Portal.Enums.CropTypes.CiderApples,
                        (int)NMP.Portal.Enums.CropTypes.CulinaryApples,
                        (int)NMP.Portal.Enums.CropTypes.DessertApples,
                        (int)NMP.Portal.Enums.CropTypes.Cherries,
                        (int)NMP.Portal.Enums.CropTypes.Pears,
                        (int)NMP.Portal.Enums.CropTypes.Plums
                    };

                    //The planned application would result in more than 500 of total N from all applications of Green compost & Green/food compost applied or planned to the field in the last 730 days up to and including the application date of the manure.

                    (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                    if (!cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId))
                    {
                        (previousAppliedTotalN, error) = await _organicManureService.FetchTotalNBasedByManIdAppDateAndIsGreenCompost(managementId, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, false, true);
                        if (error == null)
                        {

                            decimal currentApplicationNitrogen = 0;
                            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                            totalN = previousAppliedTotalN + currentApplicationNitrogen;
                            if (totalN > 500)
                            {
                                model.IsOrgManureNfieldLimitWarning = true;
                                if (!isGetCheckAnswer)
                                {
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingEngland;
                                        model.NmaxWarningPara1 = Resource.MsgNmaxWarningPara1England;
                                        model.NmaxWarningPara2 = Resource.MsgNmaxWarningPara2England;
                                    }
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingWales;
                                        model.NmaxWarningPara1 = Resource.MsgNmaxWarningPara1Wales;
                                        model.NmaxWarningPara2 = Resource.MsgIfOrganicManureNMaxLimitExceedAdditionalWales;
                                    }

                                }
                                else
                                {
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingEngland;
                                    }
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingWales;
                                    }

                                }

                            }

                        }
                    }

                    if (cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId))
                    {
                        (previousAppliedTotalN, error) = await _organicManureService.FetchTotalNBasedByManIdAppDateAndIsGreenCompost(managementId, model.ApplicationDate.Value.AddDays(-1459), model.ApplicationDate.Value, false, true);
                        if (error == null)
                        {
                            decimal currentApplicationNitrogen = 0;
                            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                            totalN = previousAppliedTotalN + currentApplicationNitrogen;
                            if (totalN > 1000)
                            {
                                model.IsOrgManureNfieldLimitWarning = true;
                                if (!isGetCheckAnswer)
                                {
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingGreaterThan1000England;
                                        model.NmaxWarningPara1 = Resource.MsgNmaxWarningPara1GreaterThan1000England;
                                        model.NmaxWarningPara2 = Resource.MsgNmaxWarningPara2England;
                                    }
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingGreaterThan1000Wales;
                                        model.NmaxWarningPara1 = Resource.MsgNmaxWarningPara1GreaterThan1000Wales;
                                        model.NmaxWarningPara2 = Resource.MsgIfOrganicManureNMaxLimitExceedAdditionalWales;
                                    }

                                }
                                else
                                {
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingGreaterThan1000England;
                                    }
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.NmaxWarningHeading = Resource.MsgNmaxWarningHeadingGreaterThan1000Wales;
                                    }


                                }
                            }

                        }
                    }

                }

            }
            return (model, error);

        }
        private async Task<(OrganicManureViewModel, Error?)> IsNMaxWarningMessage(OrganicManureViewModel model, int fieldId, int managementId, bool isGetCheckAnswer)
        {
            Error? error = null;
            //bool IsNMaxLimitWarning = false;
            decimal defaultNitrogen = 0;
            if (model.OrganicManures != null && model.OrganicManures.Any())
            {
                foreach (var organicManure in model.OrganicManures)
                {
                    defaultNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                    break;
                }
            }

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                decimal totalN = 0;
                decimal previousApplicationsN = 0;
                //DateTime startDate = model.ApplicationDate.Value.AddDays(-364);
                //DateTime endDate = model.ApplicationDate.Value;
                List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(fieldId));
                var crop = cropsResponse.Where(x => x.Year == model.HarvestYear && x.Confirm == false).ToList();
                if (crop != null)
                {
                    //(totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endDate, false);

                    (CropTypeLinkingResponse cropTypeLinking, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(crop[0].CropTypeID.Value);
                    if (error == null)
                    {
                        int? nmaxLimitEnglandOrWales = (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales ? cropTypeLinking.NMaxLimitWales : cropTypeLinking.NMaxLimitEngland) ?? 0;
                        if (nmaxLimitEnglandOrWales > 0)
                        {
                            (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                (previousApplicationsN, error) = await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false);
                                if (error == null)
                                {
                                    decimal nMaxLimit = 0;
                                    int? percentOfTotalNForUseInNmaxCalculation = null;
                                    (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                                    if (manureType != null)
                                    {
                                        percentOfTotalNForUseInNmaxCalculation = manureType.PercentOfTotalNForUseInNmaxCalculation;
                                    }

                                    decimal currentApplicationNitrogen = 0;
                                    if (percentOfTotalNForUseInNmaxCalculation != null)
                                    {
                                        decimal decimalOfTotalNForUseInNmaxCalculation = Convert.ToDecimal(percentOfTotalNForUseInNmaxCalculation / 100.0);
                                        currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value * decimalOfTotalNForUseInNmaxCalculation);
                                        totalN = previousApplicationsN + currentApplicationNitrogen;

                                        (List<int> currentYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                        (List<int> previousYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
                                        if (error == null)
                                        {
                                            nMaxLimit = nmaxLimitEnglandOrWales ?? 0;
                                            OrganicManureNMaxLimitLogic organicManureNMaxLimitLogic = new OrganicManureNMaxLimitLogic();
                                            nMaxLimit = organicManureNMaxLimitLogic.NMaxLimit(nmaxLimitEnglandOrWales ?? 0, crop[0].Yield == null ? null : crop[0].Yield.Value, fieldDetail.SoilTypeName, crop[0].CropInfo1 == null ? null : crop[0].CropInfo1.Value, crop[0].CropTypeID.Value, currentYearManureTypeIds, previousYearManureTypeIds, model.ManureTypeId.Value);

                                            if (totalN > nMaxLimit)
                                            {
                                                model.IsNMaxLimitWarning = true;
                                                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                                                if (!isGetCheckAnswer)
                                                {
                                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                                    {
                                                        model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingEngland;
                                                        model.CropNmaxLimitWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1England, nMaxLimit);
                                                        model.CropNmaxLimitWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2England;
                                                    }

                                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                                    {
                                                        model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingWales;
                                                        model.CropNmaxLimitWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1Wales, nMaxLimit);
                                                        model.CropNmaxLimitWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2Wales;
                                                    }

                                                }
                                                else
                                                {
                                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                                    {
                                                        model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingWales;
                                                    }
                                                    else
                                                    {
                                                        model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingEngland;


                                                    }
                                                }

                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isGetCheckAnswer)
                                        {
                                            (decimal? availableNFromMannerOutput, error) = await GetAvailableNFromMannerOutput(model);
                                            //decimal? currentApplicationN = totalNitrogen * model.ApplicationRate.Value;
                                            if (error == null)
                                            {
                                                (List<int> currentYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                                (List<int> previousYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
                                                if (error == null)
                                                {
                                                    nMaxLimit = nmaxLimitEnglandOrWales ?? 0;
                                                    //string cropInfo1 = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(crop[0].CropTypeID.Value, crop[0].CropInfo1.Value);
                                                    OrganicManureNMaxLimitLogic organicManureNMaxLimitLogic = new OrganicManureNMaxLimitLogic();
                                                    nMaxLimit = organicManureNMaxLimitLogic.NMaxLimit(nmaxLimitEnglandOrWales ?? 0, crop[0].Yield == null ? null : crop[0].Yield.Value, fieldDetail.SoilTypeName, crop[0].CropInfo1 == null ? null : crop[0].CropInfo1.Value, crop[0].CropTypeID.Value, currentYearManureTypeIds, previousYearManureTypeIds, model.ManureTypeId.Value);
                                                    if ((previousApplicationsN + availableNFromMannerOutput) > nMaxLimit)
                                                    {
                                                        model.IsNMaxLimitWarning = true;
                                                        (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);

                                                        if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                                        {
                                                            model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingEngland;
                                                            model.CropNmaxLimitWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1England, nMaxLimit);
                                                            model.CropNmaxLimitWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2England;
                                                        }

                                                        if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                                        {
                                                            model.CropNmaxLimitWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingWales;
                                                            model.CropNmaxLimitWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1Wales, nMaxLimit);
                                                            model.CropNmaxLimitWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2Wales;
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
                                                }

                                            }
                                            else
                                            {
                                                return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
                                }
                            }
                            else
                            {
                                return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
                            }
                        }
                    }
                    else
                    {
                        return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
                    }
                }
            }

            return (model, string.IsNullOrWhiteSpace(error?.Message) ? null : error);
        }
        private async Task<(OrganicManureViewModel, Error?)> IsEndClosedPeriodFebruaryWarningMessage(OrganicManureViewModel model, int fieldId, bool isGetCheckAnswer)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            //bool IsEndClosedPeriodFebruaryWarning = false;
            //end of closed period and end of february warning message
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                return (model, error);
            }
            if (farm != null)
            {
                bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
                bool isHighReadilyAvailableNitrogen = false;
                if (error != null)
                {
                    return (model, error);
                }
                else
                {
                    if (manureTypeList.Count > 0)
                    {
                        var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                        isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                        model.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
                    }
                    (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                    if (error != null)
                    {
                        return (model, error);
                    }
                    else
                    {
                        WarningMessage warningMessage = new WarningMessage();
                        string closedPeriod = string.Empty;
                        bool isPerennial = false;

                        //Non Organic farm closed period
                        if (!farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear ?? 0, false);
                            if (error == null)
                            {
                                isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);
                            }
                            else
                            {
                                return (model, error);
                            }
                            closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);
                        }

                        //Organic farm closed period
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

                        if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PigSlurry || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.CattleSlurry || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryStrainerBox || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryWeepingWall || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryMechanicalSeparator || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedPigSlurryLiquidPortion)
                        {
                            isSlurry = true;
                        }
                        if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                        {
                            isPoultryManure = true;
                        }
                        string message = warningMessage.EndClosedPeriodAndFebruaryWarningMessage(model.ApplicationDate.Value, closedPeriod, model.ApplicationRate, isSlurry, isPoultryManure);
                        bool? isWithinClosedPeriodAndFebruary = warningMessage.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, closedPeriod);
                        //(bool? isSlurryOrPoultryExistWithing20Days, error) = await _organicManureService.FetchOrganicManureExistanceByDateRange(model.OrganicManures[0].ManagementPeriodID,model.ApplicationDate.Value.AddDays(-20).ToString("yyyy-MM-dd"), model.ApplicationDate.Value.ToString("yyyy-MM-dd"), false);

                        if (isWithinClosedPeriodAndFebruary == true)
                        {
                            if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                            {
                                if (isSlurry)
                                {
                                    if (model.ApplicationRate.Value > 30)
                                    {
                                        model.IsEndClosedPeriodFebruaryWarning = true;
                                        if (!isGetCheckAnswer)
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading30SlurryEngland;
                                            model.EndClosedPeriodEndFebWarningPara1 = Resource.MsgEndPeriodEndFebWarningPara1st30SlurryEngland;
                                            model.EndClosedPeriodEndFebWarningPara2 = Resource.MsgEndPeriodEndFebWarningPara2nd30SlurryEngland;
                                        }
                                        else
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading30SlurryEngland;
                                        }

                                    }
                                }
                                if (isPoultryManure)
                                {
                                    if (model.ApplicationRate.Value > 8)
                                    {
                                        model.IsEndClosedPeriodFebruaryWarning = true;
                                        if (!isGetCheckAnswer)
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading8PoultryEngland;
                                            model.EndClosedPeriodEndFebWarningPara1 = Resource.MsgEndPeriodEndFebWarningPara1st8PoultryEngland;
                                            model.EndClosedPeriodEndFebWarningPara2 = Resource.MsgEndPeriodEndFebWarningPara2nd8PoultryEngland;
                                        }
                                        else
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading8PoultryEngland;
                                        }

                                    }
                                }

                            }
                            if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                            {
                                if (isSlurry)
                                {
                                    if (model.ApplicationRate.Value > 30)
                                    {
                                        model.IsEndClosedPeriodFebruaryWarning = true;
                                        if (!isGetCheckAnswer)
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading30SlurryWales;
                                            model.EndClosedPeriodEndFebWarningPara1 = Resource.MsgEndPeriodEndFebWarningPara30SlurryWales;
                                            model.EndClosedPeriodEndFebWarningPara2 = Resource.MsgEndPeriodEndFebWarningPara2nd30SlurryWales;
                                        }
                                        else
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading30SlurryWales;
                                        }

                                    }
                                }
                                if (isPoultryManure)
                                {
                                    if (model.ApplicationRate.Value > 8)
                                    {
                                        model.IsEndClosedPeriodFebruaryWarning = true;
                                        if (!isGetCheckAnswer)
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading8PoultryWales;
                                            model.EndClosedPeriodEndFebWarningPara1 = Resource.MsgEndPeriodEndFebWarningPara8PoultryWales;
                                            model.EndClosedPeriodEndFebWarningPara2 = Resource.MsgEndPeriodEndFebWarningPara2nd8PoultryWales;
                                        }
                                        else
                                        {
                                            model.EndClosedPeriodEndFebWarningHeading = Resource.MsgEndPeriodEndFebWarningHeading8PoultryWales;
                                        }
                                    }
                                }

                            }

                        }

                    }
                }


            }
            return (model, error);
        }
        private async Task<(OrganicManureViewModel, Error?)> IsClosedPeriodWarningMessage(OrganicManureViewModel model, bool isWithinNVZ, bool registeredOrganicProducer, bool isGetCheckAnswer)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            string? closedPeriod = string.Empty;
            string SlurryOrPoultryManureExistWithinLast20Days = string.Empty;
            bool isWithinClosedPeriod = false;
            string message = string.Empty;
            int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
            (List<ManureType> manureTypeList, error) = await _organicManureService.FetchManureTypeList(model.ManureGroupId.Value, countryId);
            if (error != null)
            {
                return (model, error);
            }
            else
            {
                bool isHighReadilyAvailableNitrogen = false;
                if (manureTypeList.Count > 0)
                {
                    var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                    isHighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen ?? false;
                    model.HighReadilyAvailableNitrogen = manureType.HighReadilyAvailableNitrogen;
                }
                (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                if (error != null)
                {
                    return (model, error);
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
                            return (model, error);
                        }
                        else
                        {
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeResponse.CropTypeId);

                            closedPeriod = warningMessage.ClosedPeriodNonOrganicFarm(fieldDetail, model.HarvestYear ?? 0, isPerennial);

                            isWithinClosedPeriod = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, model.ClosedPeriodEndDate);
                            if (isWithinClosedPeriod)
                            {
                                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                                if (!isGetCheckAnswer)
                                {
                                    if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                        model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodWarningPara2England;
                                    }
                                    if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriodWales;
                                        model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodWarningPara2Wales;
                                    }
                                }
                                else
                                {
                                    if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                    }
                                    if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                    {
                                        model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriodWales;
                                    }
                                }

                                model.IsClosedPeriodWarning = true;
                            }
                        }
                    }

                    //Organic farm

                    HashSet<int> cropTypeIdsForTrigger = new HashSet<int>
                    {
                    (int)NMP.Portal.Enums.CropTypes.Asparagus,
                    (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape,
                    (int)NMP.Portal.Enums.CropTypes.ForageRape,
                    (int)NMP.Portal.Enums.CropTypes.ForageSwedesRootsLifted,
                    (int)NMP.Portal.Enums.CropTypes.KaleGrazed,
                    (int)NMP.Portal.Enums.CropTypes.StubbleTurnipsGrazed,
                    (int)NMP.Portal.Enums.CropTypes.SwedesGrazed,
                    (int)NMP.Portal.Enums.CropTypes.TurnipsRootLifted,
                    (int)NMP.Portal.Enums.CropTypes.BrusselSprouts,
                    (int)NMP.Portal.Enums.CropTypes.Cabbage,
                    (int)NMP.Portal.Enums.CropTypes.Calabrese,
                    (int)NMP.Portal.Enums.CropTypes.Cauliflower,
                    (int)NMP.Portal.Enums.CropTypes.Radish,
                    (int)NMP.Portal.Enums.CropTypes.WildRocket,
                    (int)NMP.Portal.Enums.CropTypes.Swedes,
                    (int)NMP.Portal.Enums.CropTypes.Turnips,
                    (int)NMP.Portal.Enums.CropTypes.BulbOnions,
                    (int)NMP.Portal.Enums.CropTypes.SaladOnions,
                    (int)NMP.Portal.Enums.CropTypes.Grass
                    };

                    decimal totalNitrogen = 0;
                    if (model.OrganicManures != null && model.OrganicManures.Any())
                    {
                        foreach (var organicManure in model.OrganicManures)
                        {
                            totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                            break;
                        }
                    }
                    //decimal totalManureNitrogen = totalNitrogen * model.ApplicationRate.Value;

                    if (registeredOrganicProducer && isHighReadilyAvailableNitrogen && isWithinNVZ)
                    {
                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
                        if (error != null)
                        {
                            return (model, error);
                        }
                        else
                        {
                            List<Crop> cropsResponse = await _cropService.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                            int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
                            isPerennial = await _organicManureService.FetchIsPerennialByCropTypeId(cropTypeId);
                            int? cropInfo1 = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropInfo1).FirstOrDefault();
                            closedPeriod = warningMessage.ClosedPeriodOrganicFarm(fieldDetail, model.HarvestYear ?? 0, cropTypeId, cropInfo1, isPerennial);

                            isWithinClosedPeriod = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, model.ClosedPeriodEndDate);
                            if (isWithinClosedPeriod)
                            {

                                if (!cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId))
                                {
                                    model.IsClosedPeriodWarning = true;

                                    if (!isGetCheckAnswer)
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                            model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodWarningPara2England;
                                        }
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                        {
                                            model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriodWales;
                                            model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodWarningPara2Wales;
                                        }

                                    }
                                    else
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                        }
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                        {
                                            model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriodWales;
                                        }

                                    }

                                }

                            }

                            DateTime endOfOctober = new DateTime((model.HarvestYear ?? 0) - 1, 10, 31);
                            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                            {
                                bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, endOfOctober, model.ClosedPeriodEndDate);
                                if (isWithinDateRange)
                                {

                                    if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        model.IsClosedPeriodWarning = true;
                                        model.ClosedPeriodWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                        model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodWarningPara2England;
                                    }
                                }
                            }
                        }
                    }
                    //if application date is between end of closed period and end of february.
                    //check 20 days or less since the last application of slurry or poultry manure.
                    bool isOrganicManureExist = false;
                    bool? isWithinClosedPeriodAndFebruary = warningMessage.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, closedPeriod);

                    if (isWithinClosedPeriodAndFebruary != null && isWithinClosedPeriodAndFebruary == true)
                    {

                        (isOrganicManureExist, error) = await _organicManureService.FetchOrganicManureExistanceByDateRange(model.OrganicManures[0].ManagementPeriodID, model.ApplicationDate.Value.AddDays(-20).ToString("yyyy-MM-dd"), model.ApplicationDate.Value.ToString("yyyy-MM-dd"), false);
                        if (error != null)
                        {
                            return (model, error);
                        }
                    }
                    if (isOrganicManureExist)
                    {
                        bool isSlurry = false;
                        bool isPoultryManure = false;

                        if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PigSlurry || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.CattleSlurry || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryStrainerBox || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryWeepingWall || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedCattleSlurryMechanicalSeparator || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.SeparatedPigSlurryLiquidPortion)
                        {
                            isSlurry = true;
                        }
                        if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PoultryManure)
                        {
                            isPoultryManure = true;
                        }
                        if (isSlurry || isPoultryManure)
                        {
                            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;
                            if (!isGetCheckAnswer)
                            {
                                if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                {
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeading = Resource.MsgEndPeriodEndFebWarningHeadingWithin20DaysEngland;
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = Resource.MsgEndPeriodEndFebWarningPara1Within20DaysEngland;
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = Resource.MsgEndPeriodEndFebWarningPara2Within20DaysEngland;
                                }
                                if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                {
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeading = Resource.MsgEndPeriodEndFebWarningHeadingWithin20DaysWales;
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = Resource.MsgEndPeriodEndFebWarningPara1Within20DaysWales;
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = Resource.MsgEndPeriodEndFebWarningPara2ndWithin20DaysWales;

                                }
                            }
                            else
                            {

                                if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                {
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeading = Resource.MsgEndPeriodEndFebWarningHeadingWithin20DaysEngland;

                                }
                                if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                {
                                    model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeading = Resource.MsgEndPeriodEndFebWarningHeadingWithin20DaysWales;

                                }
                            }
                        }
                    }

                }
            }

            model.ClosedPeriod = closedPeriod;
            model.IsWithinClosedPeriod = isWithinClosedPeriod;
            return (model, error);
        }

        private async Task<(bool, string, Error?)> IsClosedPeriodStartAndEndFebExceedNRateException(OrganicManureViewModel model, int fieldId)
        {
            Error? error = null;
            string warningMsg = string.Empty;
            HashSet<int> cropTypeIdsForTrigger = new HashSet<int>
            {
                    (int)NMP.Portal.Enums.CropTypes.Asparagus,
                    (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape,
                    (int)NMP.Portal.Enums.CropTypes.ForageRape,
                    (int)NMP.Portal.Enums.CropTypes.ForageSwedesRootsLifted,
                    (int)NMP.Portal.Enums.CropTypes.KaleGrazed,
                    (int)NMP.Portal.Enums.CropTypes.StubbleTurnipsGrazed,
                    (int)NMP.Portal.Enums.CropTypes.SwedesGrazed,
                    (int)NMP.Portal.Enums.CropTypes.TurnipsRootLifted,
                    (int)NMP.Portal.Enums.CropTypes.BrusselSprouts,
                    (int)NMP.Portal.Enums.CropTypes.Cabbage,
                    (int)NMP.Portal.Enums.CropTypes.Calabrese,
                    (int)NMP.Portal.Enums.CropTypes.Cauliflower,
                    (int)NMP.Portal.Enums.CropTypes.Radish,
                    (int)NMP.Portal.Enums.CropTypes.WildRocket,
                    (int)NMP.Portal.Enums.CropTypes.Swedes,
                    (int)NMP.Portal.Enums.CropTypes.Turnips,
                    (int)NMP.Portal.Enums.CropTypes.BulbOnions,
                    (int)NMP.Portal.Enums.CropTypes.SaladOnions,
                    (int)NMP.Portal.Enums.CropTypes.Grass
            };

            HashSet<int> brassicaCrops = new HashSet<int>
            {
                (int)NMP.Portal.Enums.CropTypes.ForageRape,
                    (int)NMP.Portal.Enums.CropTypes.ForageSwedesRootsLifted,
                    (int)NMP.Portal.Enums.CropTypes.KaleGrazed,
                    (int)NMP.Portal.Enums.CropTypes.StubbleTurnipsGrazed,
                    (int)NMP.Portal.Enums.CropTypes.SwedesGrazed,
                    (int)NMP.Portal.Enums.CropTypes.TurnipsRootLifted,
                    (int)NMP.Portal.Enums.CropTypes.BrusselSprouts,
                    (int)NMP.Portal.Enums.CropTypes.Cabbage,
                    (int)NMP.Portal.Enums.CropTypes.Calabrese,
                    (int)NMP.Portal.Enums.CropTypes.Cauliflower,
                    (int)NMP.Portal.Enums.CropTypes.Radish,
                    (int)NMP.Portal.Enums.CropTypes.WildRocket,
                    (int)NMP.Portal.Enums.CropTypes.Swedes,
                    (int)NMP.Portal.Enums.CropTypes.Turnips

            };

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(farmId);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }
            if (farm != null)
            {
                bool nonRegisteredOrganicProducer = farm.RegisteredOrganicProducer.Value;
                int countryId = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                        //bool isWithinClosedPeriod = false;

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

                                    DateTime endDateFebruary = new DateTime((model.HarvestYear ?? 0), 3, 1).AddDays(-1);
                                    DateTime endOfOctober = new DateTime((model.HarvestYear ?? 0) - 1, 10, 31);
                                    decimal totalN = 0;

                                    (List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId.ToString(), null, null);

                                    if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endOfOctober);
                                            if (isWithinDateRange)
                                            {
                                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], model.ClosedPeriodStartDate.Value, endOfOctober, false);

                                                decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                if (currentNitrogen != null)
                                                {
                                                    if (currentNitrogen > 40 || currentNitrogen + totalN > 150)
                                                    {
                                                        model.StartClosedPeriodEndFebWarningHeading = Resource.MsgStartClosedPeriodEndFebWarningHeading;
                                                        model.StartClosedPeriodEndFebWarningPara1 = Resource.MsgStartClosedPeriodEndFebWarningPara1Grass;
                                                        model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgStartClosedPeriodEndFebWarningPara2;
                                                        model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                    }

                                                }
                                            }
                                        }
                                    }
                                    if ((cropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions) || (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions))
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endDateFebruary);
                                            if (isWithinDateRange)
                                            {
                                                decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], model.ClosedPeriodStartDate.Value, endDateFebruary, false);
                                                if (currentNitrogen + totalN > 150)
                                                {
                                                    model.StartClosedPeriodEndFebWarningHeading = Resource.MsgStartClosedPeriodEndFebWarningHeading;
                                                    model.StartClosedPeriodEndFebWarningPara1 = Resource.MsgStartClosedPeriodEndFebWarningPara1;
                                                    model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgStartClosedPeriodEndFebWarningPara2;
                                                    model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                }
                                            }
                                        }
                                    }
                                    if (cropTypeIdsForTrigger.Contains(cropTypeId))
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                        {
                                            bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endDateFebruary);
                                            if (isWithinDateRange)
                                            {

                                                decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], model.ClosedPeriodStartDate.Value, endDateFebruary, false);
                                                if (currentNitrogen + totalN > 150)
                                                {
                                                    model.StartClosedPeriodEndFebWarningHeading = Resource.MsgStartClosedPeriodEndFebWarningHeadingWales;
                                                    model.StartClosedPeriodEndFebWarningPara1 = Resource.MsgStartClosedPeriodEndFebWarningPara1Wales;
                                                    model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgStartClosedPeriodEndFebWarningPara2Wales;
                                                    model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                }
                                            }
                                        }

                                    }
                                    if (brassicaCrops.Contains(cropTypeId))
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endDateFebruary);
                                            if (isWithinDateRange)
                                            {
                                                totalN = 0;
                                                if (managementIds.Count > 0)
                                                {
                                                    (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], model.ClosedPeriodStartDate.Value, endDateFebruary, false);

                                                    (bool isOrganicManureExistWithin4Weeks, error) = await _organicManureService.FetchOrganicManureExistanceByDateRange(model.OrganicManures[0].ManagementPeriodID, model.ApplicationDate.Value.AddDays(-28).ToString("yyyy-MM-dd"), model.ApplicationDate.Value.ToString("yyyy-MM-dd"), false);

                                                    decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                    if (currentNitrogen != null)
                                                    {
                                                        if (currentNitrogen > 50 || currentNitrogen + totalN > 150 || isOrganicManureExistWithin4Weeks)
                                                        {
                                                            model.StartClosedPeriodEndFebWarningHeading = Resource.MsgStartClosedPeriodEndFebWarningHeading;
                                                            model.StartClosedPeriodEndFebWarningPara1 = Resource.MsgStartClosedPeriodEndFebWarningPara1Brassica;
                                                            model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgStartClosedPeriodEndFebWarningPara2;
                                                            model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                        }

                                                    }
                                                }
                                            }
                                        }

                                    }
                                    if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endOfOctober);
                                            if (isWithinDateRange)
                                            {
                                                decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
                                                (totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdAndAppDate(managementIds[0], model.ClosedPeriodStartDate.Value, endOfOctober, false);
                                                if (currentNitrogen + totalN > 150)
                                                {
                                                    model.StartClosedPeriodEndFebWarningHeading = Resource.MsgStartClosedPeriodEndFebWarningHeading;
                                                    model.StartClosedPeriodEndFebWarningPara1 = Resource.MsgStartClosedPeriodEndFebWarningPara1WinterOilseed;
                                                    model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgStartClosedPeriodEndFebWarningPara2;
                                                    model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                                }
                                            }
                                        }

                                    }
                                    //if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    //{
                                    //    bool isWithinDateRange = warningMessage.IsApplicationDateWithinDateRange(model.ApplicationDate, endOfOctober, model.ClosedPeriodEndDate);
                                    //    if (isWithinDateRange)
                                    //    {
                                    //        model.StartClosedPeriodEndFebWarningHeading = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                                    //        model.StartClosedPeriodEndFebWarningPara2 = Resource.MsgClosedPeriodWarningPara2England;
                                    //        model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
                                    //    }

                                    //}

                                }
                            }
                        }
                        else
                        {
                            return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
                        }

                    }
                }


            }
            return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
        }

        private async Task<(decimal?, Error?)> GetAvailableNFromMannerOutput(OrganicManureViewModel model)
        {
            Error error = new Error();
            decimal? availableNfromManner = null;

            if (model.OrganicManures != null)
            {
                model.OrganicManures.ForEach(x => x.EndOfDrain = x.SoilDrainageEndDate);
                if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                {
                    model.OrganicManures.ForEach(x => x.ManureTypeName = model.OtherMaterialName);
                    if (model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Portal.Enums.ManureTypes.OtherSolidMaterials)
                    {
                        model.OrganicManures.ForEach(x => x.ManureTypeID = model.ManureGroupIdForFilter ?? 0);
                    }
                }
                else
                {
                    model.OrganicManures.ForEach(x => x.ManureTypeName = model.ManureTypeName);
                }

                //logic for AvailableNForNMax column that will be used to get sum of previous manure applications
                int? percentOfTotalNForUseInNmaxCalculation = null;
                decimal? currentApplicationNitrogen = null;
                (ManureType manure, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                if (manure != null)
                {
                    percentOfTotalNForUseInNmaxCalculation = manure.PercentOfTotalNForUseInNmaxCalculation;
                }
                decimal totalNitrogen = 0;
                if (percentOfTotalNForUseInNmaxCalculation != null)
                {
                    if (model.OrganicManures != null && model.OrganicManures.Any())
                    {
                        foreach (var organicManure in model.OrganicManures)
                        {
                            totalNitrogen = organicManure.N != null ? organicManure.N.Value : 0;
                            break;
                        }

                        decimal decimalOfTotalNForUseInNmaxCalculation = Convert.ToDecimal(percentOfTotalNForUseInNmaxCalculation / 100.0);
                        currentApplicationNitrogen = (totalNitrogen * model.ApplicationRate.Value * decimalOfTotalNForUseInNmaxCalculation);
                    }
                }


                //foreach (string field in model.FieldList)
                //{
                (Farm farmData, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                if (farmData != null && (string.IsNullOrWhiteSpace(error.Message)))
                {
                    //Field fieldData = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(field));
                    //if (fieldData != null)
                    //{
                    foreach (var organic in model.OrganicManures)
                    {
                        (ManagementPeriod managementPeriod, error) = await _cropService.FetchManagementperiodById(organic.ManagementPeriodID);
                        if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                        {
                            (Crop crop, error) = await _cropService.FetchCropById(managementPeriod.CropID.Value);
                            if (crop != null && string.IsNullOrWhiteSpace(error.Message))
                            {
                                Field fieldData = await _fieldService.FetchFieldByFieldId(crop.FieldID.Value);
                                if (fieldData != null)
                                {
                                    (SoilTypeSoilTextureResponse soilTexture, error) = await _organicManureService.FetchSoilTypeSoilTextureBySoilTypeId(fieldData.SoilTypeID ?? 0);
                                    int topSoilID = 0;
                                    int subSoilID = 0;
                                    if (error == null && soilTexture != null)
                                    {
                                        topSoilID = soilTexture.TopSoilID;
                                        subSoilID = soilTexture.SubSoilID;
                                    }
                                    (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
                                    if (error == null && cropTypeLinkingResponse != null)
                                    {
                                        (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(organic.ManureTypeID);
                                        if (error == null && manureType != null)
                                        {
                                            var mannerOutput = new
                                            {
                                                runType = farmData.EnglishRules ? 3 : 4,
                                                postcode = farmData.ClimateDataPostCode.Split(" ")[0],
                                                countryID = farmData.CountryID,
                                                field = new
                                                {
                                                    fieldID = fieldData.ID,
                                                    fieldName = fieldData.Name,
                                                    MannerCropTypeID = cropTypeLinkingResponse.MannerCropTypeID,
                                                    topsoilID = topSoilID,
                                                    subsoilID = subSoilID,
                                                    isInNVZ = Convert.ToBoolean(fieldData.IsWithinNVZ)
                                                },
                                                manureApplications = new[]
                                             {
                                                new
                                                {
                                                    manureDetails = new
                                                    {
                                                        manureID = organic.ManureTypeID,
                                                        name = organic.ManureTypeName,
                                                        isLiquid = manureType.IsLiquid,
                                                        dryMatter = organic.DryMatterPercent,
                                                        totalN = organic.N,
                                                        nH4N = organic.NH4N,
                                                        uric = organic.UricAcid,
                                                        nO3N = organic.NO3N,
                                                        p2O5 = organic.P2O5,
                                                        k2O = organic.K2O,
                                                        sO3 = organic.SO3,
                                                        mgO = organic.MgO
                                                    },
                                                    applicationDate = organic.ApplicationDate.ToString("yyyy-MM-dd"),
                                                    applicationRate = new
                                                    {
                                                        value = organic.ApplicationRate,
                                                        unit = model.IsManureTypeLiquid.Value ? Resource.lblMeterCubePerHectare : Resource.lblTonnesPerHectare
                                                    },
                                                    applicationMethodID = organic.ApplicationMethodID,
                                                    incorporationMethodID = organic.IncorporationMethodID,
                                                    incorporationDelayID = organic.IncorporationDelayID,
                                                    autumnCropNitrogenUptake = new
                                                    {
                                                        value = organic.AutumnCropNitrogenUptake,
                                                        unit = Resource.lblKgPerHectare
                                                    },
                                                    endOfDrainageDate = organic.SoilDrainageEndDate.ToString("yyyy-MM-dd"),
                                                    rainfallPostApplication = organic.Rainfall,
                                                    windspeedID = organic.WindspeedID,
                                                    rainTypeID = organic.RainfallWithinSixHoursID,
                                                    topsoilMoistureID = organic.MoistureID
                                                }
                                            }
                                            };
                                            //var mannerJsonData = new
                                            //{
                                            //    mannerOutput
                                            //};
                                            string mannerJsonString = JsonConvert.SerializeObject(mannerOutput);
                                            (MannerCalculateNutrientResponse mannerCalculateNutrientResponse, error) = await _organicManureService.FetchMannerCalculateNutrient(mannerJsonString);
                                            if (error == null && mannerCalculateNutrientResponse != null)
                                            {
                                                availableNfromManner = mannerCalculateNutrientResponse.CurrentCropAvailableN;
                                                return (availableNfromManner, error);

                                            }
                                            else
                                            {
                                                return (availableNfromManner, error);
                                            }
                                        }
                                        else
                                        {
                                            return (availableNfromManner, error);
                                        }
                                    }
                                    else
                                    {
                                        return (availableNfromManner, error);
                                    }
                                }
                                else
                                {
                                    return (availableNfromManner, error);
                                }
                            }
                            else
                            {
                                return (availableNfromManner, error);
                            }
                        }
                        else
                        {
                            return (availableNfromManner, error);
                        }
                    }
                    //}
                }
                else
                {
                    return (availableNfromManner, error);
                }
                //}
            }
            return (availableNfromManner, error);
        }

        [HttpGet]
        public async Task<IActionResult> AutumnCropNitrogenUptakeDetail()
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptakeDetail() action called");
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
        public async Task<IActionResult> AutumnCropNitrogenUptakeDetail(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptakeDetail() post action called");

            if (!ModelState.IsValid)
            {
                return View("AutumnCropNitrogenUptakeDetail", model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            return RedirectToAction("ConditionsAffectingNutrients");
        }


        [HttpGet]
        public async Task<IActionResult> OtherMaterialName()
        {
            _logger.LogTrace("Organic Manure Controller : OtherMaterialName() action called");
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

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in OtherMaterialName() get action : {ex.Message}, {ex.StackTrace}");
                TempData["CropTypeError"] = ex.Message;
                return RedirectToAction("ManureTypes");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtherMaterialName(OrganicManureViewModel model)
        {
            _logger.LogTrace("Organic Manure Controller : OtherMaterialName() post action called");
            try
            {
                if (model.OtherMaterialName == null)
                {
                    ModelState.AddModelError("OtherMaterialName", Resource.MsgEnterNameOfTheMaterial);
                }


                (bool farmManureExist, Error error) = await _organicManureService.FetchFarmManureTypeCheckByFarmIdAndManureTypeId(model.FarmId.Value, model.ManureTypeId.Value, model.OtherMaterialName);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (farmManureExist)
                    {
                        ModelState.AddModelError("OtherMaterialName", Resource.MsgThisManureTypeNameAreadyExist);
                    }
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                foreach (var manure in model.OrganicManures)
                {
                    manure.ManureTypeName = model.OtherMaterialName;
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Organic Manure Controller : Exception in OtherMaterialName() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnVariety"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("ManureApplyingDate");
        }
    }
}