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
using System.Linq;
using System.Reflection;
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

            if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
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
                        string fieldId = _fertiliserManureProtector.Unprotect(s);
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
                            if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                            {
                                ViewBag.closingPeriod = Resource.lbl31OctoberTo15January;

                            }
                            else if (cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Asparagus && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cabbage &&
                                cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cauliflower && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Calabrese &&
                                cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BulbOnions && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
                            {
                                ViewBag.closingPeriod = Resource.lbl1SeptemberTo15January;

                            }
                        }
                    }
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
            model.IsClosedPeriodWarningExceptGrassAndOilseed = false;
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
            Error error = null;
            try
            {
                if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
                {
                    var dateError = ModelState["Date"].Errors.Count > 0 ?
                                    ModelState["Date"].Errors[0].ErrorMessage.ToString() : null;

                    if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDate)))
                    {
                        ModelState["Date"].Errors.Clear();
                        ModelState["Date"].Errors.Add(Resource.MsgEnterTheDateInNumber);
                    }
                }

                if (model.Date == null)
                {
                    ModelState.AddModelError("Date", Resource.MsgEnterADateBeforeContinuing);
                }
                DateTime maxDate = new DateTime(model.HarvestYear.Value + 1, 7, 31);
                DateTime minDate = new DateTime(model.HarvestYear.Value, 8, 01);
                if (model.Date > maxDate)
                {
                    ModelState.AddModelError("Date", string.Format(Resource.MsgDateShouldNotBeExceed, maxDate.Date.ToString("dd MMMM yyyy")));
                }
                if (model.Date < minDate)
                {
                    ModelState.AddModelError("Date", string.Format(Resource.MsgDateShouldBeExceedFrom, minDate.Date.ToString("dd MMMM yyyy")));
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
                                if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                {
                                    ViewBag.closingPeriod = Resource.lbl31OctoberTo15January;

                                }
                                else if (cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Asparagus && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cabbage &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cauliflower && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Calabrese &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BulbOnions && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
                                {
                                    ViewBag.closingPeriod = Resource.lbl1SeptemberTo15January;

                                }
                            }
                        }
                    }
                    return View(model);
                }

                model.IsClosedPeriodWarningExceptGrassAndOilseed = false;
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
                    (model.IsClosedPeriodWarningOnlyForGrassAndOilseed, model.IsClosedPeriodWarningExceptGrassAndOilseed, string warningMsg, error) = await IsClosedPeriodWarningMessageShow(model);
                    if (error == null)
                    {
                        if (!string.IsNullOrWhiteSpace(warningMsg))
                        {
                            TempData["ClosedPeriodWarningMessage"] = warningMsg;
                        }
                    }
                    else
                    {
                        TempData["InOrgnaicManureDurationError"] = error.Message;
                        return RedirectToAction("InOrgnaicManureDuration", new { q = model.EncryptedCounter });
                    }
                }
                if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed || model.IsClosedPeriodWarningExceptGrassAndOilseed)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        foreach (var fieldId in model.FieldList)
                        {
                            (CropTypeResponse cropTypeResponse, error) = await _organicManureService.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                            if (error == null)
                            {
                                if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                {
                                    ViewBag.closingPeriod = Resource.lbl31OctoberTo15January;

                                }
                                else if (cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Asparagus && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cabbage &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cauliflower && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Calabrese &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BulbOnions && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
                                {
                                    ViewBag.closingPeriod = Resource.lbl1SeptemberTo15January;

                                }
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        return View("InOrgnaicManureDuration", model);
                    }
                }
                else
                {
                    model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
                    model.IsClosedPeriodWarningExceptGrassAndOilseed = false;
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
                                            CropLime = recData.Recommendation.CropLime,
                                            ManureN = recData.Recommendation.ManureN,
                                            ManureP2O5 = recData.Recommendation.ManureP2O5,
                                            ManureK2O = recData.Recommendation.ManureK2O,
                                            ManureSO3 = recData.Recommendation.ManureSO3,
                                            ManureLime = recData.Recommendation.ManureLime,
                                            FertilizerN = recData.Recommendation.CropN ?? 0 - recData.Recommendation.ManureN ?? 0,//recData.Recommendation.FertilizerN,
                                            FertilizerP2O5 = recData.Recommendation.CropP2O5 ?? 0 - recData.Recommendation.ManureP2O5 ?? 0, //recData.Recommendation.FertilizerP2O5,
                                            FertilizerK2O = recData.Recommendation.CropK2O ?? 0 - recData.Recommendation.ManureK2O ?? 0,// recData.Recommendation.FertilizerK2O,
                                            FertilizerSO3 = recData.Recommendation.CropSO3 ?? 0 - recData.Recommendation.ManureSO3 ?? 0,// recData.Recommendation.FertilizerSO3,
                                            FertilizerLime = recData.Recommendation.CropLime ?? 0 - recData.Recommendation.ManureLime ?? 0,// recData.Recommendation.FertilizerLime,
                                            SNSIndex = recData.Recommendation.SNSIndex,
                                            SIndex = recData.Recommendation.SIndex,
                                            KIndex = recData.Recommendation.KIndex,
                                            MgIndex = recData.Recommendation.MgIndex,
                                            PIndex = recData.Recommendation.PIndex,
                                            NaIndex = recData.Recommendation.NaIndex
                                        };
                                        recommendationViewModel.Recommendations.Add(rec);
                                    }
                                    model.RecommendationViewModel = recommendationViewModel;
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
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrogen, 9999));
                }
            }
            if (model.P2O5 != null)
            {
                if (model.P2O5 < 0 || model.P2O5 > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5, 9999));
                }
            }
            if (model.K2O != null)
            {
                if (model.K2O < 0 || model.K2O > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2O, 9999));
                }
            }
            if (model.SO3 != null)
            {
                if (model.SO3 < 0 || model.SO3 > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3, 9999));
                }
            }
            if (model.Lime != null)
            {
                if (model.Lime < 0 || model.Lime > 99.9m)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].Lime", string.Format(Resource.MsgMinMaxValidation, Resource.lblLime, 99.9));
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
                                                CropLime = recData.Recommendation.CropLime,
                                                ManureN = recData.Recommendation.ManureN,
                                                ManureP2O5 = recData.Recommendation.ManureP2O5,
                                                ManureK2O = recData.Recommendation.ManureK2O,
                                                ManureSO3 = recData.Recommendation.ManureSO3,
                                                ManureLime = recData.Recommendation.ManureLime,
                                                FertilizerN = recData.Recommendation.CropN ?? 0 - recData.Recommendation.ManureN ?? 0,//recData.Recommendation.FertilizerN,
                                                FertilizerP2O5 = recData.Recommendation.CropP2O5 ?? 0 - recData.Recommendation.ManureP2O5 ?? 0, //recData.Recommendation.FertilizerP2O5,
                                                FertilizerK2O = recData.Recommendation.CropK2O ?? 0 - recData.Recommendation.ManureK2O ?? 0,// recData.Recommendation.FertilizerK2O,
                                                FertilizerSO3 = recData.Recommendation.CropSO3 ?? 0 - recData.Recommendation.ManureSO3 ?? 0,// recData.Recommendation.FertilizerSO3,
                                                FertilizerLime = recData.Recommendation.CropLime ?? 0 - recData.Recommendation.ManureLime ?? 0,// recData.Recommendation.FertilizerLime,
                                                SNSIndex = recData.Recommendation.SNSIndex,
                                                SIndex = recData.Recommendation.SIndex,
                                                KIndex = recData.Recommendation.KIndex,
                                                MgIndex = recData.Recommendation.MgIndex,
                                                PIndex = recData.Recommendation.PIndex,
                                                NaIndex = recData.Recommendation.NaIndex
                                            };
                                            recommendationViewModel.Recommendations.Add(rec);
                                        }
                                        model.RecommendationViewModel = recommendationViewModel;
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
                                    if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape ||
                                        cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                                        cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese ||
                                        cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions ||
                                        cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage)
                                    {
                                        //DateTime applicationDate = model.Date.Value;
                                        int year = model.Date.Value.Year;
                                        DateTime startDate = new DateTime(year, 9, 1); // 1st Sep
                                        DateTime endDate = new DateTime(year, 1, 15); // 15th Jan
                                        if (model.Date.Value.Month >= 8)
                                        {
                                            endDate = new DateTime(year + 1, 1, 15); // 15th Jan
                                        }
                                        //DateTime startDate = new DateTime(year, 9, 1); // 1st Sep
                                        //DateTime endDate = new DateTime(year, 1, 15); // 15th Jan
                                        if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                                            cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese)
                                        {
                                            if (model.Date.Value.Month >= 8)
                                            {
                                                year = year + 1;
                                            }
                                            int daysInFebruary = DateTime.DaysInMonth(year, 2);
                                            endDate = new DateTime(year, 2, daysInFebruary);
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
                                        if (model.Date >= startDate && model.Date <= endDate)
                                        {
                                            List<int> managementIds = new List<int>();
                                            if (int.TryParse(model.FieldGroup, out int fieldGroup))
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, model.CropOrder);//1 is CropOrder
                                            }
                                            else
                                            {
                                                (managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);//1 is CropOrder
                                            }
                                            //(List<int> managementIds, error) = await _organicManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, null,null);
                                            if (error == null)
                                            {
                                                if (managementIds.Count > 0)
                                                {
                                                    (model.IsNitrogenExceedWarning, string nitrogenExceedMessageTitle, string warningMsg, string nitrogenExceedFirstAdditionalMessage, string nitrogenExceedSecondAdditionalMessage, error) = await isNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, model.N.Value, startDate, endDate, cropTypeResponse.CropType);
                                                    if (error == null)
                                                    {
                                                        if (!string.IsNullOrWhiteSpace(warningMsg))
                                                        {
                                                            TempData["NitrogenExceedMessageTitle"] = nitrogenExceedMessageTitle;
                                                            TempData["NitrogenExceedForFertiliser"] = warningMsg;
                                                            TempData["NitrogenExceedFirstAdditionalMessage"] = nitrogenExceedFirstAdditionalMessage;
                                                            if (!string.IsNullOrWhiteSpace(nitrogenExceedSecondAdditionalMessage))
                                                            {
                                                                TempData["NitrogenExceedSecondAdditionalMessage"] = nitrogenExceedSecondAdditionalMessage;
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
                model.IsClosedPeriodWarningExceptGrassAndOilseed = false;
                if (int.TryParse(model.FieldGroup, out int value) || (model.FieldGroup == Resource.lblSelectSpecificFields && model.FieldList.Count == 1))
                {
                    (model.IsClosedPeriodWarningOnlyForGrassAndOilseed, model.IsClosedPeriodWarningExceptGrassAndOilseed, string warningMsg, error) = await IsClosedPeriodWarningMessageShow(model);
                    if (error != null)
                    {
                        TempData["NutrientValuesError"] = error.Message;
                        return RedirectToAction("NutrientValues", model);
                    }
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
                                                    (model.IsNitrogenExceedWarning, string nitrogenExceedMessageTitle, string warningMsg, string nitrogenExceedFirstAdditionalMessage, string nitrogenExceedSecondAdditionalMessage, error) = await isNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, 0, startDate, endDate, cropTypeResponse.CropType);
                                                    if (error == null)
                                                    {
                                                        if (!string.IsNullOrWhiteSpace(warningMsg))
                                                        {
                                                            model.IsNitrogenExceedWarning = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        TempData["NutrientValuesError"] = error.Message;
                                                        return RedirectToAction("NutrientValues", model);
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
            if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed || model.IsClosedPeriodWarningExceptGrassAndOilseed || model.IsNitrogenExceedWarning)
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
                    bool success = true;
                    _httpContextAccessor.HttpContext?.Session.Remove("FertiliserManure");
                    return RedirectToAction("HarvestYearOverview", "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear,
                        q = _farmDataProtector.Protect(success.ToString()),
                        r = _cropDataProtector.Protect(successMsg)
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

        private async Task<(bool, bool, string, Error?)> IsClosedPeriodWarningMessageShow(FertiliserManureViewModel model)
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
                                WarningMessage warningMessage = new WarningMessage();
                                string message = warningMessage.ClosedPeriodForFertiliserWarningMessage(model.Date.Value, cropTypeResponse.CropTypeId, fieldDetail.SoilTypeName, cropTypeResponse.CropType);
                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    if (cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        IsClosedPeriodWarningOnlyForGrassAndOilseed = true;
                                    }
                                    else if (cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Asparagus && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cabbage &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Cauliflower && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.Calabrese &&
                                    cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.BulbOnions && cropTypeResponse.CropTypeId != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
                                    {
                                        IsClosedPeriodWarningExceptGrassAndOilseed = true;
                                    }
                                    // TempData["ClosedPeriodWarningMessage"] = message;
                                    warningMsg = message;

                                }
                            }
                            else
                            {
                                return (IsClosedPeriodWarningOnlyForGrassAndOilseed, IsClosedPeriodWarningExceptGrassAndOilseed, warningMsg, error);
                            }
                        }
                        else
                        {
                            return (IsClosedPeriodWarningOnlyForGrassAndOilseed, IsClosedPeriodWarningExceptGrassAndOilseed, warningMsg, error);

                        }
                    }
                }
            }
            return (IsClosedPeriodWarningOnlyForGrassAndOilseed, IsClosedPeriodWarningExceptGrassAndOilseed, warningMsg, error);
        }
        private async Task<(bool, string, string, string, string, Error?)> isNitrogenExceedWarning(FertiliserManureViewModel model, int managementId, int cropTypeId, decimal appNitrogen, DateTime startDate, DateTime endDate, string cropType)
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
                if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage || cropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts ||
                    cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese)
                {
                    DateTime fourWeekDate = model.Date.Value.AddDays(28);
                    (decimal nitrogenInFourWeek, error) = await _fertiliserManureService.FetchTotalNBasedOnManIdAndAppDate(managementId, model.Date.Value, fourWeekDate, false);
                    if (error == null)
                    {
                        nitrogenInFourWeek = nitrogenInFourWeek + Convert.ToDecimal(model.N);

                        message = warningMessage.NitrogenLimitForFertiliserForBrassicasWarningMessage(totalNitrogen, nitrogenInFourWeek, model.N.Value);
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            nitrogenExceedMessageTitle = Resource.MsgForMaxNitrogenForFertiliserForBrassicasTitle;
                            warningMsg = message;
                            nitrogenExceedFirstAdditionalMessage = Resource.MsgForMaxNitrogenForFertiliserForBrassicasFirstAdditionalWarningMsg;
                            nitrogenExceedSecondAdditionalMessage = Resource.MsgForMaxNitrogenForFertiliserForBrassicasSecondAdditionalWarningMsg;
                            isNitrogenExceedWarning = true;
                        }
                    }
                    else
                    {
                        return (isNitrogenExceedWarning, nitrogenExceedMessageTitle, warningMsg, nitrogenExceedFirstAdditionalMessage, nitrogenExceedSecondAdditionalMessage, error);
                    }
                }
                else
                {
                    message = warningMessage.NitrogenLimitForFertiliserExceptBrassicasWarningMessage(cropTypeId, cropType, totalNitrogen, appNitrogen);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass && appNitrogen > 40)
                        {
                            nitrogenExceedMessageTitle = Resource.MsgForMaxNitrogenForFertiliserForGrassTitle;
                        }
                        else
                        {
                            nitrogenExceedMessageTitle = Resource.MsgForMaxNitrogenForFertiliserForExceptBrassicasTitle;
                        }
                        nitrogenExceedFirstAdditionalMessage = Resource.MsgForMaxNitrogenForFertiliserForExceptBrassicasFirstAdditionalWarningMsg;
                        warningMsg = message;
                        isNitrogenExceedWarning = true;
                    }
                }

            }
            else
            {
                return (isNitrogenExceedWarning, nitrogenExceedMessageTitle, warningMsg, nitrogenExceedFirstAdditionalMessage, nitrogenExceedSecondAdditionalMessage, error);
            }

            return (isNitrogenExceedWarning, nitrogenExceedMessageTitle, warningMsg, nitrogenExceedFirstAdditionalMessage, nitrogenExceedSecondAdditionalMessage, error);
        }
    }
}
