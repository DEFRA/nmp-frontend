using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Net.Mime.MediaTypeNames;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FertiliserManureController : Controller
    {
        private readonly ILogger<FertiliserManureController> _logger;
        private readonly IDataProtector _fertiliserManureProtector;
        private readonly IDataProtector _farmDataProtector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataProtector _cropDataProtector;
        private readonly IFarmService _farmService;
        private readonly IFertiliserManureService _fertiliserManureService;
        private readonly ICropService _cropService;
        private readonly IFieldService _fieldService;
        private readonly IOrganicManureService _organicManureService;


        public FertiliserManureController(ILogger<FertiliserManureController> logger, IDataProtectionProvider dataProtectionProvider,
            IHttpContextAccessor httpContextAccessor, IFarmService farmService, IFertiliserManureService fertiliserManureService, ICropService cropService, IFieldService fieldService, IOrganicManureService organicManureService)
        {
            _logger = logger;
            _fertiliserManureProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FertiliserManureController");
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _farmService = farmService;
            _fertiliserManureService = fertiliserManureService;
            _cropService = cropService;
            _fieldService = fieldService;
            _organicManureService = organicManureService;
        }

        public IActionResult Index()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : Index() action called");
            return View();
        }

        public IActionResult CreateFertiliserManureCancel(string q, string r)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : CreateFertiliserManureCancel({q}, {r}) action called");
            _httpContextAccessor.HttpContext?.Session.Remove("FertiliserManure");
            return RedirectToAction("HarvestYearOverview", "Crop", new { Id = q, year = r });
        }

        [HttpGet]
        public IActionResult backActionForInOrganicManure()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : backActionForInOrganicManure() action called");
            FertiliserManureViewModel? model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (model.IsCheckAnswer)
            {
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
        public async Task<IActionResult> FieldGroup(string q, string r, string? s)//q=FarmId,r=harvestYear,s=fieldId
        {
            _logger.LogTrace($"Fertiliser Manure Controller : FieldGroup({q}, {r}, {s}) action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
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
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                    }
                    else
                    {
                        TempData["ErrorOnHarvestYearOverview"] = error.Message;
                        if (TempData["FieldGroupError"] != null)
                        {
                            TempData["FieldGroupError"] = null;
                        }
                        if (TempData["FieldError"] != null)
                        {
                            TempData["FieldError"] = null;
                        }
                        return RedirectToAction("HarvestYearOverview", "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
                    }
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        model.FieldList = new List<string>();
                        model.FieldGroup = Resource.lblSelectSpecificFields;
                        model.FieldGroupName = Resource.lblSelectSpecificFields;
                        model.IsComingFromRecommendation = true;
                        string fieldId = _cropDataProtector.Unprotect(s);
                        model.FieldList.Add(fieldId);
                        (List<int> managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);// 1 id cropOrder
                        if (error == null)
                        {
                            if (managementIds.Count > 0)
                            {
                                if (model.FertiliserManures == null)
                                {
                                    model.FertiliserManures = new List<FertiliserManure>();
                                }
                                if (model.FertiliserManures.Count > 0)
                                {
                                    model.FertiliserManures.Clear();
                                }
                                foreach (var manIds in managementIds)
                                {
                                    var fertiliserManures = new FertiliserManure
                                    {
                                        ManagementPeriodID = manIds
                                    };
                                    model.FertiliserManures.Add(fertiliserManures);
                                }
                            }
                        }
                        else
                        {
                            TempData["NutrientRecommendationsError"] = error.Message;
                            if (TempData["FieldGroupError"] != null)
                            {
                                TempData["FieldGroupError"] = null;
                            }
                            if (TempData["FieldError"] != null)
                            {
                                TempData["FieldError"] = null;
                            }
                            return RedirectToAction("Recommendations", "Crop", new { q = q, r = s, s = r });
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        return RedirectToAction("InOrgnaicManureDuration");
                    }


                }
                (List<ManureCropTypeResponse> cropTypeList, error) = await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                cropTypeList = cropTypeList.DistinctBy(x => x.CropTypeId).ToList();
                if (error == null && cropTypeList.Count > 0)
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
                    TempData["ErrorOnHarvestYearOverview"] = error.Message;
                    if (TempData["FieldGroupError"] != null)
                    {
                        TempData["FieldGroupError"] = null;
                    }
                    if (TempData["FieldError"] != null)
                    {
                        TempData["FieldError"] = null;
                    }
                    return RedirectToAction("HarvestYearOverview", "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in FieldGroup() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                if (TempData["FieldGroupError"] != null)
                {
                    TempData["FieldGroupError"] = null;
                }
                if (TempData["FieldError"] != null)
                {
                    TempData["FieldError"] = null;
                }
                return RedirectToAction("HarvestYearOverview", "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

            //if (model.IsCheckAnswer && (string.IsNullOrWhiteSpace(s)))
            //{
            //    model.IsFieldGroupChange = true;
            //}
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            return View("Views/FertiliserManure/FieldGroup.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldGroup(FertiliserManureViewModel model)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : FieldGroup() post action called");
            Error error = null;
            if (model.FieldGroup == null)
            {
                ModelState.AddModelError("FieldGroup", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            try
            {
                (List<ManureCropTypeResponse> cropTypeList, error) = await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
                if (!ModelState.IsValid)
                {
                    if (error == null && cropTypeList.Count > 0)
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
                    return View("Views/FertiliserManure/FieldGroup.cshtml", model);
                }
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
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in FieldGroup() post action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldGroupError"] = ex.Message;
                return View("Views/FertiliserManure/FieldGroup.cshtml", model);
            }
            return RedirectToAction("Fields");


        }
        [HttpGet]
        public async Task<IActionResult> Fields()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : Fields() action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (List<CommonResponse> fieldList, error) = await _fertiliserManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                        int counter = 0;
                        if (fieldList.Count > 0)
                        {

                            model.FieldList = fieldList.Select(x => x.Id.ToString()).ToList();
                            string fieldIds = string.Join(",", model.FieldList);
                            List<int> managementIds = new List<int>();
                            if (int.TryParse(model.FieldGroup, out int value))
                            {
                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);//1 is CropOrder
                            }
                            else
                            {
                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);
                            }
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    if (model.FertiliserManures == null)
                                    {
                                        model.FertiliserManures = new List<FertiliserManure>();
                                    }
                                    if (model.FertiliserManures.Count > 0)
                                    {
                                        model.FertiliserManures.Clear();
                                    }
                                    foreach (var manIds in managementIds)
                                    {
                                        var fertiliserManure = new FertiliserManure
                                        {
                                            ManagementPeriodID = manIds
                                        };
                                        model.FertiliserManures.Add(fertiliserManure);
                                    }
                                }
                            }
                            else
                            {
                                TempData["FieldGroupError"] = error.Message;
                                if (TempData["FieldError"] != null)
                                {
                                    TempData["FieldError"] = null;
                                }
                                return RedirectToAction("FieldGroup", new { q = model.EncryptedFarmId, r = model.EncryptedHarvestYear });
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        if (model.IsCheckAnswer)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                        return RedirectToAction("InOrgnaicManureDuration");
                    }
                }
                else
                {
                    TempData["FieldGroupError"] = error.Message;
                    if (TempData["FieldError"] != null)
                    {
                        TempData["FieldError"] = null;
                    }
                    return RedirectToAction("FieldGroup", new { q = model.EncryptedFarmId, r = model.EncryptedHarvestYear });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in Fields() action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldGroupError"] = ex.Message;
                if (TempData["FieldError"] != null)
                {
                    TempData["FieldError"] = null;
                }
                return RedirectToAction("FieldGroup", new { q = model.EncryptedFarmId, r = model.EncryptedHarvestYear });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fields(FertiliserManureViewModel model)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : Fields() post action called");
            Error error = null;
            try
            {
                (List<CommonResponse> fieldList, error) = await _fertiliserManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                    (List<int> managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);//1 is CropOrder
                    if (error == null)
                    {
                        if (managementIds.Count > 0)
                        {
                            if (model.FertiliserManures == null)
                            {
                                model.FertiliserManures = new List<FertiliserManure>();
                            }
                            if (model.FertiliserManures.Count > 0)
                            {
                                model.FertiliserManures.Clear();
                            }
                            foreach (var manIds in managementIds)
                            {
                                var fertiliserManure = new FertiliserManure
                                {
                                    ManagementPeriodID = manIds
                                };
                                model.FertiliserManures.Add(fertiliserManure);
                            }
                        }
                    }
                    else
                    {
                        TempData["FieldError"] = error.Message;
                        return View(model);
                    }
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                }
                else
                {
                    TempData["FieldError"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in Fields() post action : {ex.Message}, {ex.StackTrace}");
                TempData["FieldError"] = ex.Message;
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("InOrgnaicManureDuration");

        }

        [HttpGet]
        public async Task<IActionResult> InOrgnaicManureDuration()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : InOrgnaicManureDuration() action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                (List<InOrganicManureDurationResponse> OrganicManureDurationList, error) = await _fertiliserManureService.FetchInOrganicManureDurations();
                if (error == null && OrganicManureDurationList.Count > 0)
                {
                    var SelectListItem = OrganicManureDurationList.Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Name.ToString()
                    }).ToList();
                    ViewBag.InOrganicManureDurationsList = SelectListItem;
                }
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    foreach (var fieldId in model.FieldList)
                    {
                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                        if (error == null)
                        {
                            WarningMessage warning = new WarningMessage();
                            ViewBag.closingPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId);

                        }
                    }
                }
                Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(model.FieldList[0]));
                if (field != null)
                {
                    model.IsWithinNVZ = field.IsWithinNVZ;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in InOrgnaicManureDuration() action : {ex.Message}, {ex.StackTrace}");
                if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                {
                    TempData["FieldError"] = ex.Message;
                    if (TempData["InOrgnaicManureDurationError"] != null)
                    {
                        TempData["InOrgnaicManureDurationError"] = null;
                    }
                    return RedirectToAction("Fields");
                }
                else
                {
                    TempData["FieldGroupError"] = ex.Message;
                    if (TempData["InOrgnaicManureDurationError"] != null)
                    {
                        TempData["InOrgnaicManureDurationError"] = null;
                    }
                    return RedirectToAction("FieldGroup", new { q = model.EncryptedFarmId, r = model.EncryptedHarvestYear });

                }
            }
            //int counter = 0;
            //if (model.ApplicationForFertiliserManures == null)
            //{
            //    model.ApplicationForFertiliserManures = new List<ApplicationForFertiliserManure>();
            //    var AppForFertManure = new ApplicationForFertiliserManure
            //    {
            //        EncryptedCounter = _fertiliserManureProtector.Protect(counter.ToString()),
            //        Counter = counter
            //    };
            //    model.Counter = counter;
            //    model.EncryptedCounter = _fertiliserManureProtector.Protect(counter.ToString());
            //    model.ApplicationForFertiliserManures.Add(AppForFertManure);
            //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            //}
            //if (!string.IsNullOrWhiteSpace(q))
            //{
            //    int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(q));
            //    model.Counter = currentCounter;
            //    model.EncryptedCounter = q;
            //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            //}
            model.IsClosedPeriodWarning = false;
            model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
            model.IsWarningMsgNeedToShow = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InOrgnaicManureDuration(FertiliserManureViewModel model)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : InOrgnaicManureDuration() post action called");
            Error? error = null;
            try
            {
                if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
                {
                    var dateError = ModelState["Date"]?.Errors.Count > 0 ?
                                    ModelState["Date"]?.Errors[0].ErrorMessage.ToString() : null;

                    //if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDate)))
                    //{
                    //    ModelState["Date"]?.Errors.Clear();
                    //    ModelState["Date"]?.Errors.Add(Resource.MsgEnterTheDateInNumber);
                    //}
                    if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                    {
                        ModelState["Date"].Errors.Clear();
                        ModelState["Date"].Errors.Add(Resource.MsgTheDateMustInclude);
                    }
                }

                if (model.Date == null)
                {
                    ModelState.AddModelError("Date", Resource.MsgEnterADateBeforeContinuing);
                }

                DateTime maxDate = new DateTime(model.HarvestYear.Value + 1, 12, 31);
                DateTime minDate = new DateTime(model.HarvestYear.Value - 1, 01, 01);

                if (model.Date > maxDate)
                {
                    ModelState.AddModelError("Date", string.Format(Resource.MsgManureApplicationMaxDate, model.HarvestYear.Value, maxDate.Date.ToString("dd MMMM yyyy")));
                }
                if (model.Date < minDate)
                {
                    ModelState.AddModelError("Date", string.Format(Resource.MsgManureApplicationMinDate, model.HarvestYear.Value, minDate.Date.ToString("dd MMMM yyyy")));
                }
                if (!ModelState.IsValid)
                {
                    if (int.TryParse(model.FieldGroup, out int fieldGroup) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                    {
                        foreach (var fieldId in model.FieldList)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                WarningMessage warning = new WarningMessage();
                                ViewBag.closingPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId);
                            }
                        }
                    }
                    return View(model);
                }

                model.IsClosedPeriodWarning = false;
                model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    FertiliserManureViewModel fertiliserManureViewModel = new FertiliserManureViewModel();
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                    {
                        fertiliserManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                    if (fertiliserManureViewModel != null)
                    {
                        if (model.Date != fertiliserManureViewModel.Date)
                        {
                            model.IsWarningMsgNeedToShow = false;
                        }
                    }
                    (model, error) = await IsClosedPeriodWarningMessageShow(model, false);

                }
                if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed || model.IsClosedPeriodWarning)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        foreach (var fieldId in model.FieldList)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                WarningMessage warning = new WarningMessage();
                                ViewBag.closingPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId);
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        return View("InOrgnaicManureDuration", model);
                    }
                }
                else
                {
                    model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
                    model.IsClosedPeriodWarning = false;
                    model.IsWarningMsgNeedToShow = false;
                }


                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Farm Controller : Exception in InOrgnaicManureDuration() post action : {ex.Message}, {ex.StackTrace}");
                TempData["InOrgnaicManureDurationError"] = ex.Message;
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("NutrientValues");
        }

        [HttpGet]
        public async Task<IActionResult> NutrientValues()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : NutrientValues() action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }


            if (model.FieldList.Count == 1)
            {
                RecommendationViewModel recommendationViewModel = new RecommendationViewModel();
                Error error = null;
                int fieldId;
                try
                {
                    if (int.TryParse(model.FieldList[0], out fieldId))
                    {
                        model.FieldName = (await _fieldService.FetchFieldByFieldId(fieldId)).Name;
                        List<RecommendationHeader> recommendationsHeader = null;

                        (recommendationsHeader, error) = await _cropService.FetchRecommendationByFieldIdAndYear(fieldId, model.HarvestYear.Value);
                        if (error == null)
                        {
                            if (recommendationViewModel.Crops == null)
                            {
                                recommendationViewModel.Crops = new List<CropViewModel>();
                            }
                            if (recommendationViewModel.ManagementPeriods == null)
                            {
                                recommendationViewModel.ManagementPeriods = new List<ManagementPeriod>();
                            }
                            if (recommendationViewModel.Recommendations == null)
                            {
                                recommendationViewModel.Recommendations = new List<Recommendation>();
                            }
                            foreach (var recommendation in recommendationsHeader)
                            {
                                var crop = new CropViewModel
                                {
                                    ID = recommendation.Crops.ID,
                                    Year = recommendation.Crops.Year,
                                    CropTypeID = recommendation.Crops.CropTypeID,
                                    FieldID = recommendation.Crops.FieldID,
                                    Variety = recommendation.Crops.Variety,
                                    CropInfo1 = recommendation.Crops.CropInfo1,
                                    CropInfo2 = recommendation.Crops.CropInfo2,
                                    Yield = recommendation.Crops.Yield,
                                    SowingDate = recommendation.Crops.SowingDate,
                                };
                                recommendationViewModel.Crops.Add(crop);
                                if (recommendation.Crops.CropOrder == 1)
                                {
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
                                            recommendationViewModel.ManagementPeriods.Add(ManagementPeriods);
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
                                                NIndex = recData.Recommendation.NIndex,
                                                SIndex = recData.Recommendation.SIndex,
                                                LimeIndex = recData.Recommendation.PH,
                                                KIndex = recData.Recommendation.KIndex != null ? (recData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (recData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : recData.Recommendation.KIndex)) : null,
                                                MgIndex = recData.Recommendation.MgIndex,
                                                PIndex = recData.Recommendation.PIndex,
                                                NaIndex = recData.Recommendation.NaIndex,
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
                                            recommendationViewModel.Recommendations.Add(rec);
                                        }
                                        model.RecommendationViewModel = recommendationViewModel;
                                    }
                                }

                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace($"Farm Controller : Exception in NutrientValues() action : {ex.Message}, {ex.StackTrace}");
                    TempData["InOrgnaicManureDurationError"] = ex.Message;
                    return RedirectToAction("InOrgnaicManureDuration", model);
                }

            }

            model.IsNitrogenExceedWarning = false;
            model.IsWarningMsgNeedToShow = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            //if (!string.IsNullOrWhiteSpace(q))
            //{
            //    int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(q));
            //    model.Counter = currentCounter;
            //    model.EncryptedCounter = q;
            //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            //}

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NutrientValues(FertiliserManureViewModel model)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : NutrientValues() post action called");
            int index = 0;
            Error error = null;
            if ((!ModelState.IsValid) && ModelState.ContainsKey("N"))
            {
                var totalNitrogenError = ModelState["N"].Errors.Count > 0 ?
                                ModelState["N"].Errors[0].ErrorMessage.ToString() : null;

                if (totalNitrogenError != null && totalNitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["N"].RawValue, Resource.lblN)))
                {
                    ModelState["N"].Errors.Clear();
                    ModelState["N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblNitrogen));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("P2O5"))
            {
                var totalPhosphateError = ModelState["P2O5"].Errors.Count > 0 ?
                                ModelState["P2O5"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPhosphateError != null && totalPhosphateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["P2O5"].RawValue, Resource.lblP2O5)))
                {
                    ModelState["P2O5"].Errors.Clear();
                    ModelState["P2O5"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPhosphateP2O5));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("K2O"))
            {
                var totalPotassiumError = ModelState["K2O"].Errors.Count > 0 ?
                                ModelState["K2O"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPotassiumError != null && totalPotassiumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["K2O"].RawValue, Resource.lblK2O)))
                {
                    ModelState["K2O"].Errors.Clear();
                    ModelState["K2O"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPotashK2O));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SO3"))
            {
                var sulphurSO3Error = ModelState["SO3"].Errors.Count > 0 ?
                                ModelState["SO3"].Errors[0].ErrorMessage.ToString() : null;

                if (sulphurSO3Error != null && sulphurSO3Error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SO3"].RawValue, Resource.lblSO3)))
                {
                    ModelState["SO3"].Errors.Clear();
                    ModelState["SO3"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblSulphurSO3));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Lime"))
            {
                var limeError = ModelState["Lime"].Errors.Count > 0 ?
                                ModelState["Lime"].Errors[0].ErrorMessage.ToString() : null;

                if (limeError != null && limeError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Lime"].RawValue, Resource.lblLime)))
                {
                    ModelState["Lime"].Errors.Clear();
                    ModelState["Lime"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblLime));
                }
            }

            if (model.N == null && model.P2O5 == null
                && model.K2O == null && model.SO3 == null
                && model.Lime == null)
            {
                ModelState.AddModelError("CropTypeName", Resource.MsgEnterAnAmountForAMinimumOfOneNutrientBeforeContinuing);
                //return View(model);
            }


            if (ModelState.IsValid)
            {
                decimal totalNutrientValue = (model.N ?? 0) + (model.P2O5 ?? 0) +
                     (model.K2O ?? 0) + (model.SO3 ?? 0) +
                     (model.Lime ?? 0);
                if (totalNutrientValue == 0)
                {
                    ModelState.AddModelError("CropTypeName", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
            }

            if (model.N != null)
            {
                if (model.N < 0 || model.N > 9999)
                {
                    ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrogenLowercase, 9999));
                }
            }
            if (model.P2O5 != null)
            {
                if (model.P2O5 < 0 || model.P2O5 > 9999)
                {
                    ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5Lowercase, 9999));
                }
            }
            if (model.K2O != null)
            {
                if (model.K2O < 0 || model.K2O > 9999)
                {
                    ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2OLowecase, 9999));
                }
            }
            if (model.SO3 != null)
            {
                if (model.SO3 < 0 || model.SO3 > 9999)
                {
                    ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3Lowercase, 9999));
                }
            }
            if (model.Lime != null)
            {
                if (model.Lime < 0 || model.Lime > 99.9m)
                {
                    ModelState.AddModelError("Lime", string.Format(Resource.MsgMinMaxValidation, Resource.lblLime.ToLower(), 99.9));
                }
            }

            if (!ModelState.IsValid)
            {
                if (model.FieldList.Count == 1)
                {
                    RecommendationViewModel recommendationViewModel = new RecommendationViewModel();

                    int fieldId;
                    try
                    {
                        if (int.TryParse(model.FieldList[0], out fieldId))
                        {
                            model.FieldName = (await _fieldService.FetchFieldByFieldId(fieldId)).Name;
                            List<RecommendationHeader> recommendationsHeader = null;

                            (recommendationsHeader, error) = await _cropService.FetchRecommendationByFieldIdAndYear(fieldId, model.HarvestYear.Value);
                            if (error == null)
                            {
                                if (recommendationViewModel.Crops == null)
                                {
                                    recommendationViewModel.Crops = new List<CropViewModel>();
                                }
                                if (recommendationViewModel.ManagementPeriods == null)
                                {
                                    recommendationViewModel.ManagementPeriods = new List<ManagementPeriod>();
                                }
                                if (recommendationViewModel.Recommendations == null)
                                {
                                    recommendationViewModel.Recommendations = new List<Recommendation>();
                                }
                                foreach (var recommendation in recommendationsHeader)
                                {
                                    var crop = new CropViewModel
                                    {
                                        ID = recommendation.Crops.ID,
                                        Year = recommendation.Crops.Year,
                                        CropTypeID = recommendation.Crops.CropTypeID,
                                        FieldID = recommendation.Crops.FieldID,
                                        Variety = recommendation.Crops.Variety,
                                        CropInfo1 = recommendation.Crops.CropInfo1,
                                        CropInfo2 = recommendation.Crops.CropInfo2,
                                        Yield = recommendation.Crops.Yield,
                                        SowingDate = recommendation.Crops.SowingDate,
                                    };
                                    recommendationViewModel.Crops.Add(crop);
                                    if (recommendation.Crops.CropOrder == 1)
                                    {
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
                                                recommendationViewModel.ManagementPeriods.Add(ManagementPeriods);
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
                                                    NIndex = recData.Recommendation.NIndex,
                                                    SIndex = recData.Recommendation.SIndex,
                                                    LimeIndex = recData.Recommendation.PH,
                                                    KIndex = recData.Recommendation.KIndex != null ? (recData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (recData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : recData.Recommendation.KIndex)) : null,
                                                    MgIndex = recData.Recommendation.MgIndex,
                                                    PIndex = recData.Recommendation.PIndex,
                                                    NaIndex = recData.Recommendation.NaIndex,
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
                                                recommendationViewModel.Recommendations.Add(rec);
                                            }
                                            model.RecommendationViewModel = recommendationViewModel;
                                        }
                                    }

                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace($"Farm Controller : Exception in NutrientValues() post action : {ex.Message}, {ex.StackTrace}");
                        TempData["NutrientValuesError"] = ex.Message;
                        return View(model);
                    }

                }
                return View(model);
            }

            if (model.Lime == null)
            {
                model.Lime = 0;
            }
            if (model.N == null)
            {
                model.N = 0;
            }
            if (model.P2O5 == null)
            {
                model.P2O5 = 0;
            }
            if (model.K2O == null)
            {
                model.K2O = 0;
            }
            if (model.SO3 == null)
            {
                model.SO3 = 0;
            }

            model.IsNitrogenExceedWarning = false;


            if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
            {
                FertiliserManureViewModel fertiliserManureViewModel = new FertiliserManureViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
                {
                    fertiliserManureViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (fertiliserManureViewModel != null)
                {
                    if (model.N != fertiliserManureViewModel.N)
                    {
                        model.IsWarningMsgNeedToShow = false;
                    }
                }
                if (model.FieldList.Count > 0)
                {
                    foreach (var fieldId in model.FieldList)
                    {
                        Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                        if (field != null)
                        {
                            bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                            if (isFieldIsInNVZ)
                            {
                                (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                if (error == null)
                                {
                                    int year = model.Date.Value.Year;
                                    WarningMessage warning = new WarningMessage();
                                    string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId) ?? string.Empty;

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

                                            DateTime startDate = new DateTime(year, startMonth, startDay);
                                            DateTime endDate = new DateTime(year + 1, endMonth, endDay);


                                            List<int> managementIds = new List<int>();
                                            if (int.TryParse(model.FieldGroup, out int fieldGroup))
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);//1 is CropOrder
                                            }
                                            else
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);//1 is CropOrder
                                            }
                                            if (error == null)
                                            {
                                                if (managementIds.Count > 0)
                                                {
                                                    //(model.IsNitrogenExceedWarning, string nitrogenExceedMessageTitle, string warningMsg, string nitrogenExceedFirstAdditionalMessage, string nitrogenExceedSecondAdditionalMessage, error) = await isNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, model.N.Value, startDate, endDate, cropTypeResponse.CropType);
                                                    (model, error) = await isNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, model.N.Value, startDate, endDate, cropTypeResponse.CropType, false, Convert.ToInt32(fieldId));

                                                }
                                            }
                                            else
                                            {
                                                TempData["NutrientValuesError"] = error.Message;
                                                return RedirectToAction("NutrientValues", model);
                                            }
                                        }
                                    }


                                }
                                else
                                {
                                    TempData["NutrientValuesError"] = error.Message;
                                    return RedirectToAction("NutrientValues", model);
                                }
                            }
                        }
                    }
                }
            }
            if (model.IsNitrogenExceedWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                    return View(model);
                }
            }
            else
            {
                model.IsNitrogenExceedWarning = false;
                model.IsWarningMsgNeedToShow = false;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);

            return RedirectToAction("CheckAnswer");
        }



        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : CheckAnswer() action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            Error? error = null;

            if (model != null && model.FieldList != null)
            {
                model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
                model.IsClosedPeriodWarning = false;
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    (model, error) = await IsClosedPeriodWarningMessageShow(model, true);
                    //if (error != null)
                    //{
                    //    TempData["NutrientValuesError"] = error.Message;
                    //    return RedirectToAction("NutrientValues", model);
                    //}
                }

                foreach (var fieldId in model.FieldList)
                {
                    Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                    if (field != null)
                    {
                        bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                        if (isFieldIsInNVZ)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape ||
                                    cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                                    cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese ||
                                    cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions ||
                                    cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage)
                                {
                                    DateTime applicationDate = DateTime.Now;
                                    int year = model.Date.Value.Year;

                                    DateTime startDate = new DateTime(year, 9, 1); // 1st Sep
                                    DateTime endDate = new DateTime(year + 1, 1, 15); // 15th Jan
                                    if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                                        cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese)
                                    {
                                        int daysInFebruary = DateTime.DaysInMonth(year + 1, 2);
                                        endDate = new DateTime(year + 1, 2, daysInFebruary);
                                    }
                                    if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        startDate = new DateTime(year, 9, 15); // 15th Sep
                                        endDate = new DateTime(year, 10, 31); // 31st Oct
                                    }
                                    else if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
                                    {
                                        startDate = new DateTime(year, 9, 1); // 1st Sep
                                        endDate = new DateTime(year, 10, 31); // 31st Oct
                                    }
                                    if (applicationDate >= startDate && applicationDate <= endDate)
                                    {
                                        decimal nitrogen = 0;
                                        if (((cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                                            cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese)
                                            && model.N > 50) || (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass && model.N > 40))
                                        {
                                            model.IsNitrogenExceedWarning = true;
                                            break;
                                        }
                                        else
                                        {
                                            List<int> managementIds = new List<int>();
                                            if (int.TryParse(model.FieldGroup, out int fieldGroup))
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);
                                            }
                                            else
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);//1 is CropOrder
                                            }
                                            //(List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null, null);
                                            if (error == null)
                                            {
                                                if (managementIds.Count > 0)
                                                {
                                                    (model, error) = await isNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, 0, startDate, endDate, cropTypeResponse.CropType, true, Convert.ToInt32(fieldId));

                                                }
                                            }
                                            else
                                            {
                                                TempData["NutrientValuesError"] = error.Message;
                                                return RedirectToAction("NutrientValues", model);
                                            }
                                        }
                                    }
                                    if (model.IsNitrogenExceedWarning)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                TempData["NutrientValuesError"] = error.Message;
                                return RedirectToAction("NutrientValues", model);
                            }
                            if (model.IsNitrogenExceedWarning)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            model.IsCheckAnswer = true;
            if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed || model.IsClosedPeriodWarning || model.IsNitrogenExceedWarning)
            {
                model.IsWarningMsgNeedToShow = true;
            }
            (List<CommonResponse> fieldList, error) = await _fertiliserManureService.FetchFieldByFarmIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FertiliserManureViewModel model)
        {
            _logger.LogTrace($"Fertiliser Manure Controller : CheckAnswer() post action called");

            if (model.FertiliserManures.Count > 0)
            {
                foreach (var fertiliserManure in model.FertiliserManures)
                {
                    fertiliserManure.ManagementPeriodID = fertiliserManure.ManagementPeriodID;
                    fertiliserManure.ApplicationDate = model.Date.Value;
                    fertiliserManure.N = model.N;
                    fertiliserManure.P2O5 = model.P2O5;
                    fertiliserManure.K2O = model.K2O;
                    fertiliserManure.SO3 = model.SO3;
                    fertiliserManure.Lime = model.Lime;
                    fertiliserManure.ApplicationRate = 1;
                }
            }
            if (model.FertiliserManures.Count > 0)
            {
                List<FertiliserManure> fertiliserList = new List<FertiliserManure>();
                foreach (FertiliserManure fertiliserManure in model.FertiliserManures)
                {
                    FertiliserManure FertiliserManure = new FertiliserManure
                    {
                        ManagementPeriodID = fertiliserManure.ManagementPeriodID,
                        ApplicationDate = fertiliserManure.ApplicationDate,
                        ApplicationRate = fertiliserManure.ApplicationRate ?? 0,
                        Confirm = fertiliserManure.Confirm,
                        N = fertiliserManure.N ?? 0,
                        P2O5 = fertiliserManure.P2O5 ?? 0,
                        K2O = fertiliserManure.K2O ?? 0,
                        MgO = fertiliserManure.MgO ?? 0,
                        SO3 = fertiliserManure.SO3 ?? 0,
                        Na2O = fertiliserManure.Na2O ?? 0,
                        NFertAnalysisPercent = fertiliserManure.NFertAnalysisPercent ?? 0,
                        P2O5FertAnalysisPercent = fertiliserManure.P2O5FertAnalysisPercent ?? 0,
                        K2OFertAnalysisPercent = fertiliserManure.K2OFertAnalysisPercent ?? 0,
                        MgOFertAnalysisPercent = fertiliserManure.MgOFertAnalysisPercent ?? 0,
                        SO3FertAnalysisPercent = fertiliserManure.SO3FertAnalysisPercent ?? 0,
                        Na2OFertAnalysisPercent = fertiliserManure.Na2OFertAnalysisPercent ?? 0,
                        Lime = fertiliserManure.Lime ?? 0,
                        NH4N = fertiliserManure.NH4N ?? 0,
                        NO3N = fertiliserManure.NO3N ?? 0,
                    };
                    fertiliserList.Add(FertiliserManure);
                }
                var result = new
                {
                    FertiliserManure = fertiliserList
                };
                string jsonString = JsonConvert.SerializeObject(result);
                (List<FertiliserManure> fertiliserResponse, Error error) = await _fertiliserManureService.AddFertiliserManureAsync(jsonString);
                if (error == null)
                {
                    string successMsg = Resource.lblFertilisersHavebeenSuccessfullyAdded;
                    string successMsgSecond = Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation;
                    bool success = true;
                    _httpContextAccessor.HttpContext?.Session.Remove("FertiliserManure");
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
            return View(model);
        }
        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Fertiliser Manure Controller : BackCheckAnswer() action called");
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            return RedirectToAction("NutrientValues");
        }

        private async Task<(FertiliserManureViewModel, Error?)> IsClosedPeriodWarningMessageShow(FertiliserManureViewModel model, bool isGetCheckAnswer)
        {
            Error? error = null;
            bool IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
            bool IsClosedPeriodWarningExceptGrassAndOilseed = false;
            string warningMsg = string.Empty;
            foreach (var fieldId in model.FieldList)
            {
                Field field = await _fieldService.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                if (field != null)
                {
                    bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                    if (isFieldIsInNVZ)
                    {
                        (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                        if (error == null)
                        {
                            (FieldDetailResponse fieldDetail, error) = await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                            if (error == null)
                            {

                                HashSet<int> filterCrops = new HashSet<int>
                                {
                                    (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape,
                                    (int)NMP.Portal.Enums.CropTypes.Asparagus,
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


                                WarningMessage warning = new WarningMessage();
                                string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId) ?? string.Empty;
                                bool isWithinClosedPeriod = warning.IsFertiliserApplicationWithinWarningPeriod(model.Date.Value, closedPeriod);

                                if (!filterCrops.Contains(cropTypeResponse.CropTypeId))
                                {
                                    if (isWithinClosedPeriod)
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            if (!isGetCheckAnswer)
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                                model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodFertiliserWarningPara2;
                                            }
                                            else
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                            }
                                        }
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                        {
                                            if (!isGetCheckAnswer)
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                                model.ClosedPeriodWarningPara2 = Resource.MsgClosedPeriodFertiliserWarningPara2Wales;
                                            }
                                            else
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                            }
                                        }
                                    }
                                }

                                if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                {
                                    //31 october and end of closed period
                                    string warningPeriod = string.Empty;
                                    string startPeriod = string.Empty;
                                    string endPeriod = string.Empty;
                                    string[] periods = closedPeriod.Split(" to ");

                                    if (periods.Length == 2)
                                    {
                                        startPeriod = Resource.lbl31October;
                                        endPeriod = periods[1];
                                        warningPeriod = $"{startPeriod} to {endPeriod}";
                                    }
                                    bool isWithinWarningPeriod = warning.IsFertiliserApplicationWithinWarningPeriod(model.Date.Value, warningPeriod);

                                    if (isWithinWarningPeriod)
                                    {
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                                        {
                                            if (!isGetCheckAnswer)
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                                model.ClosedPeriodWarningPara2 = Resource.Msg31OctoberToEndPeriodFertiliserWarningPara2;
                                            }
                                            else
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                            }
                                        }
                                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                        {
                                            if (!isGetCheckAnswer)
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                                model.ClosedPeriodWarningPara2 = Resource.Msg31OctoberToEndPeriodFertiliserWarningPara2Wales;
                                            }
                                            else
                                            {
                                                model.IsClosedPeriodWarning = true;
                                                model.ClosedPeriodWarningHeading = Resource.MsgClosedPeriodFertiliserWarningHeading;
                                            }
                                        }
                                    }

                                }

                            }
                            else
                            {
                                return (model, error);
                            }
                        }
                        else
                        {
                            return (model, error);

                        }
                    }
                }
            }
            return (model, error);
        }
        private async Task<(FertiliserManureViewModel, Error?)> isNitrogenExceedWarning(FertiliserManureViewModel model, int managementId, int cropTypeId, decimal appNitrogen, DateTime startDate, DateTime endDate, string cropType, bool isGetCheckAnswer, int fieldId)
        {
            Error? error = null;
            bool isNitrogenExceedWarning = false;
            string nitrogenExceedMessageTitle = string.Empty;
            string warningMsg = string.Empty;
            string nitrogenExceedFirstAdditionalMessage = string.Empty;
            string nitrogenExceedSecondAdditionalMessage = string.Empty;
            decimal totalNitrogen = 0;
            (totalNitrogen, error) = await _fertiliserManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endDate, false);
            if (error == null)
            {
                WarningMessage warningMessage = new WarningMessage();
                string message = string.Empty;
                totalNitrogen = totalNitrogen + Convert.ToDecimal(model.N);

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
                string closedPeriod = warningMessage.ClosedPeriodForFertiliser(cropTypeId) ?? string.Empty;
                bool isWithinClosedPeriod = warningMessage.IsFertiliserApplicationWithinWarningPeriod(model.Date.Value, closedPeriod);
                string startPeriod = string.Empty;
                string endPeriod = string.Empty;
                string[] periods = closedPeriod.Split(" to ");

                if (periods.Length == 2)
                {
                    startPeriod = periods[0];
                    endPeriod = periods[1];
                }
                if (brassicaCrops.Contains(cropTypeId) && isWithinClosedPeriod)
                {
                    DateTime fourWeekDate = model.Date.Value.AddDays(-28);
                    (decimal nitrogenInFourWeek, error) = await _fertiliserManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, model.Date.Value, fourWeekDate, false);
                    if (error == null)
                    {
                        nitrogenInFourWeek = nitrogenInFourWeek + Convert.ToDecimal(model.N);

                        if (totalNitrogen > 100 || model.N.Value > 50 || nitrogenInFourWeek > 50)
                        {
                            if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                            {
                                model.IsNitrogenExceedWarning = true;
                                if (!isGetCheckAnswer)
                                {

                                    model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                                    model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgClosedPeriodNitrogenExceedWarningPara1England, startPeriod, endPeriod);
                                    model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNitrogenExceedWarningPara2England;
                                }
                                else
                                {
                                    model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                                }
                            }
                            if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                            {
                                model.IsNitrogenExceedWarning = true;
                                if (!isGetCheckAnswer)
                                {
                                    model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingWales;
                                    model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgClosedPeriodNitrogenExceedWarningPara1Wales, startPeriod, endPeriod);
                                    model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNitrogenExceedWarningPara2Wales;
                                }
                                else
                                {
                                    model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingWales;
                                }
                            }
                        }

                    }
                    else
                    {
                        return (model, error);
                    }
                }
                if ((cropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions) && isWithinClosedPeriod)
                {
                    bool isNitrogenRateExceeded = false;
                    int maxNitrogenRate = 0;
                    if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus)
                    {
                        if (model.N.Value > 50)
                        {
                            isNitrogenRateExceeded = true;
                            maxNitrogenRate = 50;
                        }
                    }
                    if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions)
                    {
                        if (model.N.Value > 40)
                        {
                            isNitrogenRateExceeded = true;
                            maxNitrogenRate = 40;
                        }
                    }
                    if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions)
                    {
                        if (model.N.Value > 40)
                        {
                            isNitrogenRateExceeded = true;
                            maxNitrogenRate = 40;
                        }
                    }
                    if (isNitrogenRateExceeded)
                    {
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgClosedPeriodNRateExceedWarningPara1England, startPeriod, endPeriod, maxNitrogenRate);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNitrogenExceedWarningPara2England;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                            }
                        }
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNRateExceedWarningHeadingWales;
                                //model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgClosedPeriodNitrogenExceedWarningPara1Wales, startPeriod, endPeriod);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNRateExceedWarningPara2Wales;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNRateExceedWarningHeadingWales;
                            }
                        }
                    }
                }

                string warningPeriod = string.Empty;
                periods = closedPeriod.Split(" to ");

                if (periods.Length == 2)
                {
                    startPeriod = periods[0];
                    endPeriod = Resource.lbl31October;
                    warningPeriod = $"{startPeriod} to {endPeriod}";
                }
                bool isWithinWarningPeriod = warningMessage.IsFertiliserApplicationWithinWarningPeriod(model.Date.Value, warningPeriod);

                DateTime endOfOctober = new DateTime(model.Date.Value.Year, 10, 31);
               (decimal PreviousApplicationsNitrogen, error) = await _fertiliserManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, startDate, endOfOctober, false);

                if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape && isWithinWarningPeriod)
                {
                    bool isNitrogenRateExceeded = false;
                    int maxNitrogenRate = 0;

                    if ((PreviousApplicationsNitrogen + model.N.Value) > 30)
                    {
                        isNitrogenRateExceeded = true;
                        maxNitrogenRate = 30;
                    }

                    if (isNitrogenRateExceeded)
                    {
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgWinterOilseedRapeNRateExceedWarningPara1England, startPeriod);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNitrogenExceedWarningPara2England;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                            }
                        }
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgWinterOilseedRapeNRateExceedWarningHeadingWales;
                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgWinterOilseedRapeNRateExceedWarningPara1Wales, startPeriod);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNRateExceedWarningPara2Wales;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgWinterOilseedRapeNRateExceedWarningHeadingWales;
                            }
                        }
                    }
                }

                if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass && isWithinWarningPeriod)
                {
                    bool isNitrogenRateExceeded = false;
                    int maxNitrogenRate = 0;
                    string startString = $"{startPeriod} {model.HarvestYear}";
                    DateTime start = DateTime.ParseExact(startString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                    string endString = $"{endPeriod} {model.HarvestYear}";
                    DateTime end = DateTime.ParseExact(endString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                    (decimal nitrogenWithinWarningPeriod, error) = await _fertiliserManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, start, end, false);
                    if (model.N.Value > 40 || nitrogenWithinWarningPeriod > 80)
                    {
                        isNitrogenRateExceeded = true;
                    }

                    if (isNitrogenRateExceeded)
                    {
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.England)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgWinterGrassNRateExceedWarningPara1England, startPeriod);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNitrogenExceedWarningPara2England;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                            }
                        }
                        if (model.FarmCountryId == (int)NMP.Portal.Enums.FarmCountry.Wales)
                        {
                            model.IsNitrogenExceedWarning = true;
                            if (!isGetCheckAnswer)
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgWinterOilseedRapeNRateExceedWarningHeadingWales;
                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgWinterGrassNRateExceedWarningPara1Wales, startPeriod);
                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgClosedPeriodNRateExceedWarningPara2Wales;
                            }
                            else
                            {
                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgWinterOilseedRapeNRateExceedWarningHeadingWales;
                            }
                        }
                    }
                }

                //NMax limit for crop logic
                decimal previousApplicationsN = 0;
                decimal currentApplicationNitrogen = Convert.ToDecimal(model.N);
                (previousApplicationsN, error) = await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false);
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
                                //(totalN, error) = await _organicManureService.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false);
                                if (error == null)
                                {
                                    decimal nMaxLimit = 0;
                                    //totalN = totalN + (totalNitrogen * model.ApplicationRate.Value);
                                    (List<int> currentYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                    (List<int> previousYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
                                    if (error == null)
                                    {
                                        nMaxLimit = nmaxLimitEnglandOrWales ?? 0;
                                        //string cropInfo1 = await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(crop[0].CropTypeID.Value, crop[0].CropInfo1.Value);
                                        OrganicManureNMaxLimitLogic organicManureNMaxLimitLogic = new OrganicManureNMaxLimitLogic();
                                        nMaxLimit = organicManureNMaxLimitLogic.NMaxLimit(nmaxLimitEnglandOrWales ?? 0, crop[0].Yield == null ? null : crop[0].Yield.Value, fieldDetail.SoilTypeName, crop[0].CropInfo1 == null ? null : crop[0].CropInfo1.Value, crop[0].CropTypeID.Value, currentYearManureTypeIds, previousYearManureTypeIds, null);
                                        if ((previousApplicationsN + currentApplicationNitrogen) > nMaxLimit)
                                        {
                                            model.IsNitrogenExceedWarning = true;
                                            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);

                                            if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                            {
                                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingEngland;
                                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1England, nMaxLimit);
                                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2England;
                                            }

                                            if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.Wales)
                                            {
                                                model.ClosedPeriodNitrogenExceedWarningHeading = Resource.MsgCropNmaxLimitWarningHeadingWales;
                                                model.ClosedPeriodNitrogenExceedWarningPara1 = string.Format(Resource.MsgCropNmaxLimitWarningPara1Wales, nMaxLimit);
                                                model.ClosedPeriodNitrogenExceedWarningPara2 = Resource.MsgCropNmaxLimitWarningPara2Wales;
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
            else
            {
                return (model, error);
            }

            return (model, error);
        }
    }
}
