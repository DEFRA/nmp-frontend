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

        public FertiliserManureController(ILogger<FertiliserManureController> logger, IDataProtectionProvider dataProtectionProvider,
            IHttpContextAccessor httpContextAccessor, IFarmService farmService, IFertiliserManureService fertiliserManureService)
        {
            _logger = logger;
            _fertiliserManureProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FertiliserManureController");
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _farmService = farmService;
            _fertiliserManureService = fertiliserManureService;
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
        public async Task<IActionResult> backActionForFertiliserWhen()
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
                        TempData["FieldGroupError"] = error.Message;
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
                                    model.FertiliserManures = new List<FertiliserManures>();
                                }
                                if (model.FertiliserManures.Count > 0)
                                {
                                    model.FertiliserManures.Clear();
                                }
                                foreach (var manIds in managementIds)
                                {
                                    var fertiliserManures = new FertiliserManures
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
                            return RedirectToAction("Recommendations", "Crop", new { q = q, r = s, s = r });
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        return RedirectToAction("ManureGroup");
                    }


                }
                (List<ManureCropTypeResponse> cropTypeList, error) = await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
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
                TempData["FieldGroupError"] = ex.Message;
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
                                        model.FertiliserManures = new List<FertiliserManures>();
                                    }
                                    if (model.FertiliserManures.Count > 0)
                                    {
                                        model.FertiliserManures.Clear();
                                    }
                                    foreach (var manIds in managementIds)
                                    {
                                        var fertiliserManure = new FertiliserManures
                                        {
                                            ManagementPeriodID = manIds
                                        };
                                        model.FertiliserManures.Add(fertiliserManure);
                                    }
                                    if (model.IsCheckAnswer && model.FertiliserManures.Count > 0)
                                    {

                                    }
                                }
                            }
                            else
                            {
                                TempData["FieldGroupError"] = error.Message;
                                return View("FieldGroup", model);
                            }
                        }
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FertiliserManure", model);
                        return RedirectToAction("FertiliserWhen");
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
                                model.FertiliserManures = new List<FertiliserManures>();
                            }
                            if (model.FertiliserManures.Count > 0)
                            {
                                model.FertiliserManures.Clear();
                            }
                            foreach (var manIds in managementIds)
                            {
                                var fertiliserManure = new FertiliserManures
                                {
                                    ManagementPeriodID = manIds
                                };
                                model.FertiliserManures.Add(fertiliserManure);
                            }

                            if (model.IsCheckAnswer && model.FertiliserManures.Count > 0)
                            {

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
            return RedirectToAction("FertiliserWhen");

        }

        [HttpGet]
        public async Task<IActionResult> FertiliserWhen()
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
            }
            catch (Exception ex)
            {
                TempData["FieldError"] = ex.Message;
                return RedirectToAction("FieldGroup", model);
            }

            return View(model);
        }
    }
}
