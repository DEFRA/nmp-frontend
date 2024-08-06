using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Diagnostics.Metrics;
using System.Reflection;

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


        public FertiliserManureController(ILogger<FertiliserManureController> logger, IDataProtectionProvider dataProtectionProvider,
            IHttpContextAccessor httpContextAccessor, IFarmService farmService, IFertiliserManureService fertiliserManureService, ICropService cropService, IFieldService fieldService)
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
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateFertiliserManureCancel(string q, string r)
        {
            _httpContextAccessor.HttpContext?.Session.Remove("FertiliserManure");
            return RedirectToAction("HarvestYearOverview", "Crop", new { Id = q, year = r });
        }

        [HttpGet]
        public async Task<IActionResult> backActionForInOrganicManure()
        {
            FertiliserManureViewModel? model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            //if (model.IsCheckAnswer)
            //{
            //    return RedirectToAction("CheckAnswer");
            //}


            //if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
            //{
            //    if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
            //    {
            //        string fieldId = model.FieldList[0];
            //        return RedirectToAction("Recommendations", "Crop", new
            //        {
            //            q = model.EncryptedFarmId,
            //            r = _cropDataProtector.Protect(fieldId),
            //            s = model.EncryptedHarvestYear

            //        });
            //    }
            //}
            //else

            if (model.Counter > 0)
            {
                int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(model.EncryptedCounter));
                model.EncryptedCounter = _fertiliserManureProtector.Protect((currentCounter - 1).ToString());
                model.Counter = currentCounter - 1;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                return RedirectToAction("QuestionForSpreadInorganicFertiliser", new { q = model.EncryptedCounter });
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
                        (List<int> managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                                        ManagementPeriodId = manIds
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
                        return RedirectToAction("ManureGroup");
                    }


                }
                (List<ManureCropTypeResponse> cropTypeList, error) = await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
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
                TempData["FieldGroupError"] = ex.Message;
                return View("Views/FertiliserManure/FieldGroup.cshtml", model);
            }
            return RedirectToAction("Fields");


        }
        [HttpGet]
        public async Task<IActionResult> Fields()
        {
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
                            (List<int> managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                                            ManagementPeriodId = manIds
                                        };
                                        model.FertiliserManures.Add(fertiliserManure);
                                    }
                                    model.Counter = counter;
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
                    (List<int> managementIds, error) = await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(model.HarvestYear.Value, fieldIds, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                                    ManagementPeriodId = manIds
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
                TempData["FieldError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction("InOrgnaicManureDuration");

        }

        [HttpGet]
        public async Task<IActionResult> InOrgnaicManureDuration(string q)//counter
        {
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
            }
            catch (Exception ex)
            {
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
            int counter = 0;
            if (model.ApplicationForFertiliserManures == null)
            {
                model.ApplicationForFertiliserManures = new List<ApplicationForFertiliserManure>();
                var AppForFertManure = new ApplicationForFertiliserManure
                {
                    EncryptedCounter = _fertiliserManureProtector.Protect(counter.ToString()),
                    Counter = counter
                };
                model.Counter = counter;
                model.EncryptedCounter = _fertiliserManureProtector.Protect(counter.ToString());
                model.ApplicationForFertiliserManures.Add(AppForFertManure);
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(q));
                model.Counter = currentCounter;
                model.EncryptedCounter = q;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InOrgnaicManureDuration(FertiliserManureViewModel model)
        {
            Error error = null;
            try
            {
                int index = 0;
                if (model.ApplicationForFertiliserManures != null && model.ApplicationForFertiliserManures.Count > 0)
                {
                    index = model.ApplicationForFertiliserManures.FindIndex(x => x.Counter == model.Counter);
                }

                if (model.ApplicationForFertiliserManures[index].InOrgnaicManureDurationId == null)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].InOrgnaicManureDurationId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
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

                    return View(model);
                }
                model.InOrgnaicManureDurationId = model.ApplicationForFertiliserManures[index].InOrgnaicManureDurationId.Value;
                (InOrganicManureDurationResponse OrganicManureDuration, error) = await _fertiliserManureService.FetchInOrganicManureDurationsById(model.InOrgnaicManureDurationId.Value);
                if (error == null)
                {
                    model.InOrgnaicManureDuration = OrganicManureDuration.Name;
                    DateTime applicationDate;

                    if (OrganicManureDuration.ApplicationMonth >= 9)
                    {
                        applicationDate = new DateTime(model.HarvestYear.Value - 1, OrganicManureDuration.ApplicationMonth, OrganicManureDuration.ApplicationDate);
                    }
                    else
                    {
                        applicationDate = new DateTime(model.HarvestYear.Value, OrganicManureDuration.ApplicationMonth, OrganicManureDuration.ApplicationDate);
                    }


                    if (applicationDate != null)
                    {
                        for (int i = 0; i < model.ApplicationForFertiliserManures.Count; i++)
                        {
                            if (model.ApplicationForFertiliserManures[i].Counter == model.Counter && model.ApplicationForFertiliserManures[i].ApplicationDate == null)
                            {
                                model.ApplicationForFertiliserManures[i].ApplicationDate = applicationDate;
                            }
                        }
                    }

                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);

                }
                else
                {
                    TempData["InOrgnaicManureDurationError"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["InOrgnaicManureDurationError"] = ex.Message;
                return View(model);
            }

            return RedirectToAction("NutrientValues");
        }

        [HttpGet]
        public async Task<IActionResult> NutrientValues(string q)//counter
        {
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
                                            FertilizerN = recData.Recommendation.FertilizerN,
                                            FertilizerP2O5 = recData.Recommendation.FertilizerP2O5,
                                            FertilizerK2O = recData.Recommendation.FertilizerK2O,
                                            FertilizerSO3 = recData.Recommendation.FertilizerSO3,
                                            FertilizerLime = recData.Recommendation.FertilizerLime,
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
                    TempData["InOrgnaicManureDurationError"] = ex.Message;
                    return RedirectToAction("InOrgnaicManureDuration", model);
                }

            }


            if (!string.IsNullOrWhiteSpace(q))
            {
                int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(q));
                model.Counter = currentCounter;
                model.EncryptedCounter = q;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NutrientValues(FertiliserManureViewModel model)
        {
            int index = 0;
            if (model.ApplicationForFertiliserManures != null && model.ApplicationForFertiliserManures.Count > 0)
            {
                index = model.ApplicationForFertiliserManures.FindIndex(x => x.Counter == model.Counter);
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationForFertiliserManures[" + index + "].N"))
            {
                var totalNitrogenError = ModelState["ApplicationForFertiliserManures[" + index + "].N"].Errors.Count > 0 ?
                                ModelState["ApplicationForFertiliserManures[" + index + "].N"].Errors[0].ErrorMessage.ToString() : null;

                if (totalNitrogenError != null && totalNitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationForFertiliserManures[" + index + "].N"].RawValue, Resource.lblN)))
                {
                    ModelState["ApplicationForFertiliserManures[" + index + "].N"].Errors.Clear();
                    ModelState["ApplicationForFertiliserManures[" + index + "].N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblNitrogen));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationForFertiliserManures[" + index + "].P2O5"))
            {
                var totalPhosphateError = ModelState["ApplicationForFertiliserManures[" + index + "].P2O5"].Errors.Count > 0 ?
                                ModelState["ApplicationForFertiliserManures[" + index + "].P2O5"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPhosphateError != null && totalPhosphateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationForFertiliserManures[" + index + "].P2O5"].RawValue, Resource.lblP2O5)))
                {
                    ModelState["ApplicationForFertiliserManures[" + index + "].P2O5"].Errors.Clear();
                    ModelState["ApplicationForFertiliserManures[" + index + "].P2O5"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPhosphateP2O5));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationForFertiliserManures[" + index + "].K2O"))
            {
                var totalPotassiumError = ModelState["ApplicationForFertiliserManures[" + index + "].K2O"].Errors.Count > 0 ?
                                ModelState["ApplicationForFertiliserManures[" + index + "].K2O"].Errors[0].ErrorMessage.ToString() : null;

                if (totalPotassiumError != null && totalPotassiumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationForFertiliserManures[" + index + "].K2O"].RawValue, Resource.lblK2O)))
                {
                    ModelState["ApplicationForFertiliserManures[" + index + "].K2O"].Errors.Clear();
                    ModelState["ApplicationForFertiliserManures[" + index + "].K2O"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPotashK2O));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationForFertiliserManures[" + index + "].SO3"))
            {
                var sulphurSO3Error = ModelState["ApplicationForFertiliserManures[" + index + "].SO3"].Errors.Count > 0 ?
                                ModelState["ApplicationForFertiliserManures[" + index + "].SO3"].Errors[0].ErrorMessage.ToString() : null;

                if (sulphurSO3Error != null && sulphurSO3Error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationForFertiliserManures[" + index + "].SO3"].RawValue, Resource.lblSO3)))
                {
                    ModelState["ApplicationForFertiliserManures[" + index + "].SO3"].Errors.Clear();
                    ModelState["ApplicationForFertiliserManures[" + index + "].SO3"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblSulphurSO3));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ApplicationForFertiliserManures[" + index + "].Lime"))
            {
                var limeError = ModelState["ApplicationForFertiliserManures[" + index + "].Lime"].Errors.Count > 0 ?
                                ModelState["ApplicationForFertiliserManures[" + index + "].Lime"].Errors[0].ErrorMessage.ToString() : null;

                if (limeError != null && limeError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ApplicationForFertiliserManures[" + index + "].Lime"].RawValue, Resource.lblLime)))
                {
                    ModelState["ApplicationForFertiliserManures[" + index + "].Lime"].Errors.Clear();
                    ModelState["ApplicationForFertiliserManures[" + index + "].Lime"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblLime));
                }
            }

            if (model.ApplicationForFertiliserManures[index].N == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].N", Resource.MsgEnterValidAmountForEachNutrient);
                //return View(model);
            }
            if (model.ApplicationForFertiliserManures[index].P2O5 == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].P2O5", Resource.MsgEnterValidAmountForEachNutrient);
            }
            if (model.ApplicationForFertiliserManures[index].K2O == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].K2O", Resource.MsgEnterValidAmountForEachNutrient);
            }
            if (model.ApplicationForFertiliserManures[index].SO3 == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].SO3", Resource.MsgEnterValidAmountForEachNutrient);
            }
            if (model.ApplicationForFertiliserManures[index].Lime == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].Lime", Resource.MsgEnterValidAmountForEachNutrient);
            }

            if (model.ApplicationForFertiliserManures[index].N != null)
            {
                if (model.ApplicationForFertiliserManures[index].N < 0 || model.ApplicationForFertiliserManures[index].N > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrogen, 9999));
                }
            }
            if (model.ApplicationForFertiliserManures[index].P2O5 != null)
            {
                if (model.ApplicationForFertiliserManures[index].P2O5 < 0 || model.ApplicationForFertiliserManures[index].P2O5 > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5, 9999));
                }
            }
            if (model.ApplicationForFertiliserManures[index].K2O != null)
            {
                if (model.ApplicationForFertiliserManures[index].K2O < 0 || model.ApplicationForFertiliserManures[index].K2O > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2O, 9999));
                }
            }
            if (model.ApplicationForFertiliserManures[index].SO3 != null)
            {
                if (model.ApplicationForFertiliserManures[index].SO3 < 0 || model.ApplicationForFertiliserManures[index].SO3 > 9999)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3, 9999));
                }
            }
            if (model.ApplicationForFertiliserManures[index].Lime != null)
            {
                if (model.ApplicationForFertiliserManures[index].Lime < 0 || model.ApplicationForFertiliserManures[index].Lime > 99.9m)
                {
                    ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].Lime", string.Format(Resource.MsgMinMaxValidation, Resource.lblLime, 99.9));
                }
            }

            if (!ModelState.IsValid)
            {
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
                                                FertilizerN = recData.Recommendation.FertilizerN,
                                                FertilizerP2O5 = recData.Recommendation.FertilizerP2O5,
                                                FertilizerK2O = recData.Recommendation.FertilizerK2O,
                                                FertilizerSO3 = recData.Recommendation.FertilizerSO3,
                                                FertilizerLime = recData.Recommendation.FertilizerLime,
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
                        TempData["NutrientValuesError"] = ex.Message;
                        return View(model);
                    }

                }
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);


            return RedirectToAction("QuestionForSpreadInorganicFertiliser");
        }

        [HttpGet]
        public async Task<IActionResult> QuestionForSpreadInorganicFertiliser(string q)//counter
        {
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                int currentCounter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(q));
                model.Counter = currentCounter;
                model.EncryptedCounter = q;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> QuestionForSpreadInorganicFertiliser(FertiliserManureViewModel model)
        {
            int index = 0;
            if (model.ApplicationForFertiliserManures != null && model.ApplicationForFertiliserManures.Count > 0)
            {
                index = model.ApplicationForFertiliserManures.FindIndex(x => x.Counter == model.Counter);
            }
            if (model.ApplicationForFertiliserManures[index].QuestionForSpreadInorganicFertiliser == null)
            {
                ModelState.AddModelError("ApplicationForFertiliserManures[" + index + "].QuestionForSpreadInorganicFertiliser", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
            if (!model.ApplicationForFertiliserManures[index].QuestionForSpreadInorganicFertiliser.Value)
            {

                for (int i = 0; i < model.ApplicationForFertiliserManures.Count; i++)
                {
                    if (model.ApplicationForFertiliserManures[i].ApplicationDate == null && model.ApplicationForFertiliserManures[i].N == null && model.ApplicationForFertiliserManures[i].P2O5 == null
                            && model.ApplicationForFertiliserManures[i].K2O == null && model.ApplicationForFertiliserManures[i].SO3 == null && model.ApplicationForFertiliserManures[i].Lime == null)
                    {
                        model.ApplicationForFertiliserManures.RemoveAt(i);
                    }
                }
                return RedirectToAction("CheckYourAnswer");
            }
            else
            {
                var appForFertiliserManure = model.ApplicationForFertiliserManures.OrderByDescending(x => x.Counter).FirstOrDefault();
                if (appForFertiliserManure != null)
                {
                    if (appForFertiliserManure.Counter == model.Counter)
                    {
                        int counter = Convert.ToInt32(_fertiliserManureProtector.Unprotect(model.EncryptedCounter));
                        model.EncryptedCounter = _fertiliserManureProtector.Protect((counter + 1).ToString());
                        model.Counter = counter + 1;
                        // }
                        var AppForFertManure = new ApplicationForFertiliserManure
                        {
                            EncryptedCounter = model.EncryptedCounter,
                            Counter = model.Counter
                        };
                        model.ApplicationForFertiliserManures.Add(AppForFertManure);
                    }
                    else
                    {
                        model.EncryptedCounter = _fertiliserManureProtector.Protect((model.Counter + 1).ToString());
                        model.Counter += 1;
                    }
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                return RedirectToAction("InOrgnaicManureDuration");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckYourAnswer()
        {
            FertiliserManureViewModel model = new FertiliserManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FertiliserManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserManure");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (model.FertiliserManures.Count > 0 && model.ApplicationForFertiliserManures.Count > 0)
            {
                List<FertiliserManure> updatedFertiliserManures = new List<FertiliserManure>();
                foreach (var fertiliserManure in model.FertiliserManures)
                {
                    foreach (var application in model.ApplicationForFertiliserManures)
                    {
                        var newFertiliserManure = new FertiliserManure
                        {
                            ManagementPeriodId = fertiliserManure.ManagementPeriodId,
                            ApplicationDate = application.ApplicationDate,
                            N = application.N,
                            P2O5 = application.P2O5,
                            K2O = application.K2O,
                            SO3 = application.SO3,
                            Lime = application.Lime,
                        };
                        updatedFertiliserManures.Add(newFertiliserManure);
                    }
                }

                model.FertiliserManures = updatedFertiliserManures;
        }
            return View(model);
    }


}
}
