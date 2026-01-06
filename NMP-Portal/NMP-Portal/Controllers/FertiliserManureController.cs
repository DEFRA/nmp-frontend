using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Commons.Enums;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using NMP.Commons.Helpers;
using NMP.Application;
namespace NMP.Portal.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FertiliserManureController(ILogger<FertiliserManureController> logger, IDataProtectionProvider dataProtectionProvider,
    IFarmLogic farmLogic, IFertiliserManureLogic fertiliserManureLogic, ICropLogic cropLogic, IFieldLogic fieldLogic, IOrganicManureLogic organicManureLogic, IWarningLogic warningLogic) : Controller
{
    private readonly ILogger<FertiliserManureController> _logger = logger;
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
    private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
    private readonly IFarmLogic _farmLogic = farmLogic;
    private readonly IFertiliserManureLogic _fertiliserManureLogic = fertiliserManureLogic;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
    private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
    private readonly IWarningLogic _warningLogic = warningLogic;
    private const string _fertiliserManureSessionKey = "FertiliserManure";
    private const string _harvestYearOverviewActionName = "HarvestYearOverview";
    private const string _checkAnswerActionName = "CheckAnswer";
    private const string _defoliationActionName = "Defoliation";
    private const string _doubleCropActionName = "DoubleCrop";
    private const string _recommendationsActionName = "Recommendations";
    private const string _fieldsActionName = "Fields";
    private const string _fieldGroupActionName = "FieldGroup";
    private FertiliserManureViewModel? GetFertiliserManureFromSession()
    {
        if (HttpContext.Session.Exists(_fertiliserManureSessionKey))
        {
            return HttpContext.Session.GetObjectFromJson<FertiliserManureViewModel>(_fertiliserManureSessionKey);
        }
        return null;
    }

    private void SetFertiliserManureToSession(FertiliserManureViewModel fertiliserManureViewModel)
    {
        HttpContext.Session.SetObjectAsJson(_fertiliserManureSessionKey, fertiliserManureViewModel);
    }

    private void RemoveFertiliserManureSession()
    {
        HttpContext.Session.Remove(_fertiliserManureSessionKey);
    }

    public IActionResult Index()
    {
        _logger.LogTrace("Fertiliser Manure Controller : Index() action called");
        return View();
    }

    public IActionResult CreateFertiliserManureCancel(string q, string r)
    {
        _logger.LogTrace("Fertiliser Manure Controller : CreateFertiliserManureCancel({0}, {1}) action called", q, r);
        RemoveFertiliserManureSession();
        return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { Id = q, year = r });
    }

    [HttpGet]
    public IActionResult BackActionForInOrganicManure()
    {
        _logger.LogTrace("Fertiliser Manure Controller : BackActionForInOrganicManure() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();

        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in BackActionForInOrganicManure() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
        {
            return RedirectToAction(_defoliationActionName, new { q = model.DefoliationEncryptedCounter });
        }

        if (model.IsDoubleCropAvailable)
        {
            return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
        }

        if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
        {
            if (model.FieldList != null && model.FieldList.Count > 0 && model.FieldList.Count == 1)
            {
                string fieldId = model.FieldList[0];
                return RedirectToAction(_recommendationsActionName, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(fieldId),
                    s = model.EncryptedHarvestYear
                });
            }
        }
        else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
        {
            return RedirectToAction(_fieldsActionName);
        }

        return RedirectToAction(_fieldGroupActionName);
    }

    [HttpGet]
    public async Task<IActionResult> FieldGroup(string q, string r, string? s)//q=FarmId,r=harvestYear,s=fieldId
    {
        _logger.LogTrace("Fertiliser Manure Controller : FieldGroup({Q}, {R}, {S}) action called", q, r, s);
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        Error? error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r) && model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in FieldGroup() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r))
            {
                model = new FertiliserManureViewModel();
                model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                model.EncryptedFarmId = q;
                model.EncryptedHarvestYear = r;
                model.CropOrder = 1;
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (error.Message == null)
                {
                    model.FarmName = farm.Name;
                    model.isEnglishRules = farm.EnglishRules;
                    model.FarmCountryId = farm.CountryID;
                    SetFertiliserManureToSession(model);
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
                    return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
                }
                if (!string.IsNullOrWhiteSpace(s))
                {
                    model.IsAnyCropIsGrass = false;
                    model.FieldList = new List<string>();
                    model.FieldGroup = Resource.lblSelectSpecificFields;
                    model.IsComingFromRecommendation = true;
                    string fieldId = _fieldDataProtector.Unprotect(s);
                    model.FieldList.Add(fieldId);
                    (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fieldId), model.HarvestYear.Value);
                    if (!string.IsNullOrWhiteSpace(error.Message))
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
                        return RedirectToAction(_recommendationsActionName, "Crop", new { q = q, r = s, s = r });
                    }
                    if (cropList.Count > 0)
                    {
                        if (cropList.Count > 0 && cropList.Count == 2)
                        {
                            model.IsDoubleCropAvailable = true;
                            model.DoubleCropCurrentCounter = 0;
                            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name;
                            model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                        }
                        if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                        {
                            model.IsAnyCropIsGrass = true;
                            model.DefoliationCurrentCounter = 0;
                            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                        }
                    }

                    (List<int> managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, null, 1);// 1 id cropOrder
                    if (error == null)
                    {
                        if (managementIds.Count > 0)
                        {
                            if (model.FertiliserManures == null)
                            {
                                model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                            }
                            if (model.FertiliserManures.Count > 0)
                            {
                                model.FertiliserManures.Clear();
                            }
                            int counter = 1;
                            foreach (var manIds in managementIds)
                            {
                                var fertiliserManure = new FertiliserManureDataViewModel
                                {
                                    ManagementPeriodID = manIds,
                                    EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                                    FieldID = Convert.ToInt32(fieldId),
                                    FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name
                                };
                                counter++;
                                model.FertiliserManures.Add(fertiliserManure);
                            }
                            model.DefoliationCurrentCounter = 0;
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
                        return RedirectToAction(_recommendationsActionName, "Crop", new { q = q, r = s, s = r });
                    }

                    SetFertiliserManureToSession(model);
                    if (model.FertiliserManures != null)
                    {
                        foreach (var fertiliser in model.FertiliserManures)
                        {
                            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
                            if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                            {
                                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                if (string.IsNullOrWhiteSpace(error.Message) && crop != null)
                                {
                                    if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && crop.DefoliationSequenceID != null)
                                    {
                                        model.IsAnyCropIsGrass = true;
                                    }
                                }
                            }
                        }
                    }

                    if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                    {
                        int grassCropCounter = 0;
                        foreach (var field in model.FieldList)
                        {
                            if (cropList.Count > 0)
                            {
                                cropList = cropList.Where(x => x.CropOrder == 1).ToList();
                            }
                            if (cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                            {
                                (List<ManagementPeriod> managementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropList.Select(x => x.ID.Value).FirstOrDefault(), false);

                                var filteredFertiliserManure = model.FertiliserManures
                                .Where(fm => managementPeriod.Any(mp => mp.ID == fm.ManagementPeriodID) &&
                                fm.Defoliation == null).ToList();
                                if (filteredFertiliserManure != null && filteredFertiliserManure.Count == managementPeriod.Count)
                                {
                                    var managementPeriodIdsToRemove = managementPeriod
                                   .Skip(1)
                                   .Select(mp => mp.ID.Value)
                                   .ToList();
                                    model.FertiliserManures.RemoveAll(fm => managementPeriodIdsToRemove.Contains(fm.ManagementPeriodID));
                                }
                                grassCropCounter++;
                                model.IsAnyCropIsGrass = true;
                            }
                        }
                        model.GrassCropCount = grassCropCounter;
                        model.IsSameDefoliationForAll = true;
                        SetFertiliserManureToSession(model);
                    }
                    int fertiliserCounter = 1;
                    foreach (var fertiliser in model.FertiliserManures)
                    {
                        (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
                        if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                        {
                            (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                            if (string.IsNullOrWhiteSpace(error.Message) && crop != null)
                            {
                                fertiliser.EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString());
                                fertiliserCounter++;
                                if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                                {
                                    fertiliser.IsGrass = true;
                                }
                            }
                        }
                    }
                    if (model.IsDoubleCropAvailable)
                    {
                        SetFertiliserManureToSession(model);
                        return RedirectToAction(_doubleCropActionName);
                    }
                    if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                    {
                        SetFertiliserManureToSession(model);
                        return RedirectToAction(_defoliationActionName);
                    }
                    else
                    {
                        model.GrassCropCount = null;
                        model.IsSameDefoliationForAll = null;
                        model.IsAnyChangeInSameDefoliationFlag = false;
                        SetFertiliserManureToSession(model);
                    }

                    SetFertiliserManureToSession(model);
                    return RedirectToAction("InOrgnaicManureDuration");
                }
            }

            (List<ManureCropTypeResponse> cropTypeList, error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
            cropTypeList = cropTypeList.DistinctBy(x => x.CropGroupName).ToList();

            if (error == null && cropTypeList.Count > 0)
            {
                var SelectListItem = cropTypeList.Select(f => new SelectListItem
                {
                    Value = string.IsNullOrWhiteSpace(f.CropGroupName) ? "Crop Group 1" : f.CropGroupName,
                    Text = string.Format(Resource.lblGroupNameFieldsWithCropTypeName, string.IsNullOrWhiteSpace(f.CropGroupName) ? "Crop Group 1" : f.CropGroupName, f.CropType.ToString())
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

                SetFertiliserManureToSession(model);
                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in FieldGroup() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;

            if (TempData["FieldGroupError"] != null)
            {
                TempData["FieldGroupError"] = null;
            }

            if (TempData["FieldError"] != null)
            {
                TempData["FieldError"] = null;
            }

            SetFertiliserManureToSession(model);
            return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
        }

        SetFertiliserManureToSession(model);
        return View("Views/FertiliserManure/FieldGroup.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldGroup(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : FieldGroup() post action called");
        Error error = null;
        if (model.FieldGroup == null)
        {
            ModelState.AddModelError(_fieldGroupActionName, Resource.MsgSelectAnOptionBeforeContinuing);
        }
        try
        {
            var selectListItem = new List<SelectListItem>();
            (List<ManureCropTypeResponse> cropGroupList, error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
            if (error == null && cropGroupList.Count > 0)
            {
                var SelectListItem = cropGroupList.Select(f => new SelectListItem
                {
                    Value = string.IsNullOrWhiteSpace(f.CropGroupName) ? "Crop Group 1" : f.CropGroupName,
                    Text = string.Format(Resource.lblGroupNameFieldsWithCropTypeName, string.IsNullOrWhiteSpace(f.CropGroupName) ? "Crop Group 1" : f.CropGroupName, f.CropType.ToString())
                }).ToList();
                selectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
                selectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });
                ViewBag.FieldGroupList = selectListItem;
            }
            else
            {
                TempData["FieldGroupError"] = error.Message;
            }
            if (!ModelState.IsValid)
            {

                return View("Views/FertiliserManure/FieldGroup.cshtml", model);
            }
            if (cropGroupList.Count > 0 && !model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                string cropGroupName = cropGroupList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).Select(x => x.CropGroupName).FirstOrDefault();
                model.CropGroupName = selectListItem.Where(x => x.Value == cropGroupName).Select(x => x.Text).First();

                List<string> cropOrderList = cropGroupList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).Select(x => x.CropOrder).ToList();
                if (cropOrderList.Count == 1)
                {
                    model.CropOrder = Convert.ToInt32(cropOrderList.FirstOrDefault());
                }
                else
                {
                    model.CropOrder = 1;
                }
            }
            model.IsComingFromRecommendation = false;
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Farm Controller : Exception in FieldGroup() post action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["FieldGroupError"] = ex.Message;
            return View("Views/FertiliserManure/FieldGroup.cshtml", model);
        }
        return RedirectToAction(_fieldsActionName);
    }

    [HttpGet]
    public async Task<IActionResult> Fields()
    {
        _logger.LogTrace("Fertiliser Manure Controller : Fields() action called");
        FertiliserManureViewModel model = GetFertiliserManureFromSession();
        Error? error = null;
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in FieldGroup() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            model.CropOrder = 1;
            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
            {
                (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
                if (error == null)
                {
                    if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                    {
                        if (fieldList.Count > 0)
                        {
                            var selectListItem = fieldList.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name.ToString()
                            }).ToList();
                            ViewBag.FieldList = selectListItem.OrderBy(x => x.Text).ToList();
                        }
                        return View(model);
                    }
                    else
                    {
                        FertiliserManureViewModel fertiliserManureViewModel = GetFertiliserManureFromSession();
                        if (fertiliserManureViewModel == null)
                        {
                            _logger.LogError("Fertiliser Manure Controller : Session not found in Fields() action");
                            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                        }

                        if (fieldList.Count > 0)
                        {
                            model.IsAnyCropIsGrass = false;
                            model.FieldList = fieldList.Select(x => x.Id.ToString()).ToList();
                            model.IsDoubleCropAvailable = false;
                            foreach (string field in model.FieldList)
                            {
                                (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(field), model.HarvestYear.Value);
                                if (!string.IsNullOrWhiteSpace(error.Message))
                                {
                                    TempData["FieldGroupError"] = error.Message;
                                    return RedirectToAction(_fieldGroupActionName);
                                }

                                if (cropList.Count > 0)
                                {
                                    if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                                    {
                                        cropList = cropList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).ToList();
                                    }
                                    else
                                    {
                                        cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();
                                    }
                                    if (cropList.Count > 0 && cropList.Count == 2)
                                    {
                                        model.IsDoubleCropAvailable = true;
                                        model.DoubleCropCurrentCounter = 0;
                                        model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(field))).Name;
                                        model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                                    }
                                    else if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
                                    {
                                        model.DoubleCrop.RemoveAll(x => x.FieldID == Convert.ToInt32(field));
                                    }
                                    if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                                    {
                                        model.IsAnyCropIsGrass = true;
                                        model.DefoliationCurrentCounter = 0;
                                        model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                                    }
                                }
                            }
                            string fieldIds = string.Join(",", model.FieldList);
                            List<int> managementIds = new List<int>();
                            (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldIds, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? null : model.FieldGroup, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? 1 : null);
                            if (error == null)
                            {
                                if (managementIds.Count > 0)
                                {
                                    if (model.FertiliserManures == null)
                                    {
                                        model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                                    }
                                    if (model.FertiliserManures.Count > 0)
                                    {
                                        model.FertiliserManures.Clear();
                                    }
                                    int counter = 1;
                                    foreach (var manIds in managementIds)
                                    {
                                        var fertiliserManure = new FertiliserManureDataViewModel
                                        {
                                            ManagementPeriodID = manIds,
                                            EncryptedCounter = _fieldDataProtector.Protect(counter.ToString())
                                        };
                                        counter++;
                                        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                                        {
                                            if (fertiliserManureViewModel.FertiliserManures != null && fertiliserManureViewModel.FertiliserManures.Count > 0)
                                            {
                                                for (int i = 0; i < fertiliserManureViewModel.FertiliserManures.Count; i++)
                                                {
                                                    if (fertiliserManureViewModel.FertiliserManures[i].ManagementPeriodID == manIds)
                                                    {
                                                        fertiliserManure.Defoliation = fertiliserManureViewModel.FertiliserManures[i].Defoliation;
                                                        if (fertiliserManure.Defoliation != null)
                                                        {
                                                            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manIds);
                                                            if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                                                            {
                                                                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                                                                if (string.IsNullOrWhiteSpace(error.Message) && crop != null && crop.DefoliationSequenceID != null)
                                                                {
                                                                    (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                                                    if (error == null && defoliationSequence != null)
                                                                    {
                                                                        string description = defoliationSequence.DefoliationSequenceDescription;

                                                                        string[] defoliationParts = description.Split(',')
                                                                                                               .Select(x => x.Trim())
                                                                                                               .ToArray();

                                                                        string selectedDefoliation = (fertiliserManure.Defoliation.Value > 0 && fertiliserManure.Defoliation.Value <= defoliationParts.Length)
                                                                            ? $"{Enum.GetName(typeof(PotentialCut), fertiliserManure.Defoliation.Value)} ({defoliationParts[fertiliserManure.Defoliation.Value - 1]})"
                                                                            : $"{fertiliserManure.Defoliation.Value}";

                                                                        fertiliserManure.DefoliationName = selectedDefoliation;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                        model.FertiliserManures.Add(fertiliserManure);
                                    }
                                    model.DefoliationCurrentCounter = 0;
                                }
                            }
                            else
                            {
                                TempData["FieldGroupError"] = error.Message;
                                if (TempData["FieldError"] != null)
                                {
                                    TempData["FieldError"] = null;
                                }
                                return RedirectToAction(_fieldGroupActionName);
                            }
                        }

                        bool anyNewManId = false;

                        SetFertiliserManureToSession(model);
                        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                        {
                            int grassCropCounter = 0;
                            foreach (var field in model.FieldList)
                            {
                                (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(field), model.HarvestYear.Value);
                                if (!string.IsNullOrWhiteSpace(error.Message))
                                {
                                    TempData["FieldGroupError"] = error.Message;
                                    if (TempData["FieldError"] != null)
                                    {
                                        TempData["FieldError"] = null;
                                    }
                                    return RedirectToAction(_fieldGroupActionName);
                                }
                                if (cropList.Count > 0)
                                {
                                    if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                                    {
                                        cropList = cropList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).ToList();
                                    }
                                    else
                                    {
                                        cropList = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).ToList();
                                    }
                                }
                                if (cropList.Count > 0)
                                {
                                    if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                                    {
                                        grassCropCounter++;
                                        (List<ManagementPeriod> ManagementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropList.Select(x => x.ID.Value).FirstOrDefault(), false);
                                        var managementPeriodIdsToRemove = ManagementPeriod
                                        .Skip(1)
                                        .Select(mp => mp.ID.Value)
                                        .ToList();
                                        model.FertiliserManures.RemoveAll(fm => managementPeriodIdsToRemove.Contains(fm.ManagementPeriodID));
                                        model.IsAnyCropIsGrass = true;
                                    }
                                }
                            }
                            model.GrassCropCount = grassCropCounter;
                        }
                        else
                        {
                            model.GrassCropCount = null;
                            model.IsSameDefoliationForAll = null;
                            model.IsAnyChangeInSameDefoliationFlag = false;
                            model.DefoliationList = null;
                        }
                        if (fertiliserManureViewModel != null && fertiliserManureViewModel.FertiliserManures != null)
                        {
                            anyNewManId = model.FertiliserManures.Any(newId => !fertiliserManureViewModel.FertiliserManures.Contains(newId));
                            if (anyNewManId)
                            {
                                model.IsAnyChangeInField = true;
                            }
                        }
                        int fertiliserCounter = 1;
                        foreach (var fertiliser in model.FertiliserManures)
                        {
                            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
                            if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                            {
                                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                if (string.IsNullOrWhiteSpace(error.Message) && crop != null)
                                {
                                    fertiliser.FieldID = crop.FieldID;
                                    fertiliser.FieldName = (await _fieldLogic.FetchFieldByFieldId(fertiliser.FieldID.Value)).Name;
                                    fertiliser.EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString());
                                    fertiliserCounter++;
                                    if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                                    {
                                        fertiliser.IsGrass = true;
                                    }
                                    else if (model.DefoliationList != null && model.DefoliationList.Any(x => x.FieldID == crop.FieldID))
                                    {
                                        model.DefoliationList.RemoveAll(x => x.FieldID == crop.FieldID);
                                    }
                                }
                            }
                        }
                        var grass = model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).ToHashSet();
                        if (grass != null && model.DefoliationList != null)
                        {
                            model.DefoliationList = model.DefoliationList.Where(d => grass.Contains(d.FieldID)).ToList();
                        }
                        else
                        {
                            model.DefoliationList = null;
                        }
                        SetFertiliserManureToSession(model);
                    }
                }
                else
                {
                    TempData["FieldGroupError"] = error.Message;
                    if (TempData["FieldError"] != null)
                    {
                        TempData["FieldError"] = null;
                    }
                    return RedirectToAction(_fieldGroupActionName);
                }
            }
            else
            {
                int decryptedId = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId));
                if (decryptedId > 0 && model.FarmId != null && model.HarvestYear != null)
                {
                    (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(decryptedId, model.FarmId.Value, model.HarvestYear.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && fertiliserResponse != null && fertiliserResponse.Count > 0)
                    {
                        var SelectListItem = fertiliserResponse.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).ToList().DistinctBy(x => x.Value);
                        ViewBag.FieldList = SelectListItem.OrderBy(x => x.Text).ToList();
                        return View(model);
                    }
                    else
                    {
                        SetFertiliserManureToSession(model);
                        TempData["CheckYourAnswerError"] = error.Message;
                        return RedirectToAction(_checkAnswerActionName);
                    }
                }
            }

            if (model.DefoliationList != null && model.DefoliationList.Count > 0)
            {
                int counter = 1;
                model.DefoliationList.ForEach(d =>
                {
                    d.Counter = counter;
                    d.EncryptedCounter = _fieldDataProtector.Protect($"{counter++}");
                });
            }
            if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
            {
                int counter = 1;
                model.DoubleCrop.ForEach(d =>
                {
                    d.Counter = counter;
                    d.EncryptedCounter = _fieldDataProtector.Protect($"{counter++}");
                });
            }
            if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
            {
                if (model.IsAnyCropIsGrass.HasValue && (!model.IsAnyCropIsGrass.Value))
                {
                    model.GrassCropCount = null;
                    model.IsSameDefoliationForAll = null;
                    model.IsAnyChangeInSameDefoliationFlag = false;
                    SetFertiliserManureToSession(model);
                }
                SetFertiliserManureToSession(model);
                return RedirectToAction(_checkAnswerActionName);
            }
            if (model.IsDoubleCropAvailable)
            {
                SetFertiliserManureToSession(model);
                return RedirectToAction(_doubleCropActionName);
            }
            else
            {
                model.DoubleCrop = null;
            }

            SetFertiliserManureToSession(model);
            if (model.IsAnyCropIsGrass.HasValue && (model.IsAnyCropIsGrass.Value))
            {
                model.FieldID = model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).First();
                model.FieldName = model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldName).First();
                if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
                {
                    SetFertiliserManureToSession(model);
                    return RedirectToAction("IsSameDefoliationForAll");
                }
                model.IsSameDefoliationForAll = true;
                SetFertiliserManureToSession(model);
                return RedirectToAction(_defoliationActionName);
            }
            SetFertiliserManureToSession(model);
            if (model.FieldGroup != null && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                SetFertiliserManureToSession(model);
                return RedirectToAction("InOrgnaicManureDuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Farm Controller : Exception in Fields() action : {0}, {1}", ex.Message, ex.StackTrace);
            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
            {
                TempData["FieldGroupError"] = ex.Message;
                if (TempData["FieldError"] != null)
                {
                    TempData["FieldError"] = null;
                }
            }
            else
            {
                TempData["CheckYourAnswerError"] = ex.Message;
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction(_fieldGroupActionName);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fields(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Fields() post action called");
        Error? error = null;
        try
        {
            (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
            if (error == null)
            {
                var selectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name.ToString()
                }).ToList();
                ViewBag.FieldList = selectListItem.OrderBy(x => x.Text).ToList();
                if (!string.IsNullOrWhiteSpace(model.EncryptedFertId))
                {
                    (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId)), model.FarmId.Value, model.HarvestYear.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && fertiliserResponse != null && fertiliserResponse.Count > 0)
                    {
                        selectListItem = new List<SelectListItem>();
                        selectListItem = fertiliserResponse.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).GroupBy(x => x.Value).Select(g => g.First()).ToList();
                        ViewBag.FieldList = selectListItem.OrderBy(x => x.Text).ToList();
                    }
                    else
                    {
                        TempData["FieldError"] = error.Message;
                        return View(model);
                    }
                }

                if (model.FieldList == null || model.FieldList.Count == 0)
                {
                    ModelState.AddModelError("FieldList", Resource.MsgSelectAtLeastOneField);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (model.FieldList.Count > 0 && model.FieldList.Contains(Resource.lblSelectAll))
                {
                    model.FieldList = selectListItem.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
                }
                FertiliserManureViewModel fertiliserManureViewModel = GetFertiliserManureFromSession();
                if (fertiliserManureViewModel == null)
                {
                    _logger.LogError("Fertiliser Manure Controller : Session not found in Fields() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                model.IsAnyCropIsGrass = false;
                foreach (string field in model.FieldList)
                {
                    (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(field), model.HarvestYear.Value);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["FieldGroupError"] = error.Message;
                        return RedirectToAction(_fieldGroupActionName);
                    }

                    if (cropList.Count > 0)
                    {
                        if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                        {
                            cropList = cropList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).ToList();
                        }
                        if (cropList.Count > 0 && cropList.Count == 2)
                        {
                            model.IsDoubleCropAvailable = true;
                            model.DoubleCropCurrentCounter = 0;
                            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(field))).Name;
                            model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                        }
                        else if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
                        {
                            model.DoubleCrop.RemoveAll(x => x.FieldID == Convert.ToInt32(field));
                        }
                        if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                        {
                            model.IsAnyCropIsGrass = true;
                            model.DefoliationCurrentCounter = 0;
                            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                        }
                    }
                }
                string fieldIds = string.Join(",", model.FieldList);
                List<int> managementIds = new List<int>();
                (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldIds, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? null : model.FieldGroup, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? 1 : null);
                if (error == null)
                {
                    if (managementIds.Count > 0)
                    {
                        if (model.FertiliserManures == null)
                        {
                            model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                        }
                        if (model.FertiliserManures.Count > 0)
                        {
                            model.FertiliserManures.Clear();
                        }
                        int counter = 1;
                        foreach (var manIds in managementIds)
                        {
                            var fertiliserManure = new FertiliserManureDataViewModel
                            {
                                ManagementPeriodID = manIds,
                                EncryptedCounter = _fieldDataProtector.Protect(counter.ToString())
                            };
                            counter++;
                            if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                            {
                                if (fertiliserManureViewModel.FertiliserManures != null && fertiliserManureViewModel.FertiliserManures.Count > 0)
                                {
                                    for (int i = 0; i < fertiliserManureViewModel.FertiliserManures.Count; i++)
                                    {
                                        if (fertiliserManureViewModel.FertiliserManures[i].ManagementPeriodID == manIds)
                                        {
                                            fertiliserManure.Defoliation = fertiliserManureViewModel.FertiliserManures[i].Defoliation;
                                            if (fertiliserManure.Defoliation != null)
                                            {
                                                (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manIds);
                                                if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                                                {
                                                    (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                                                    if (string.IsNullOrWhiteSpace(error.Message) && crop != null && crop.DefoliationSequenceID != null)
                                                    {
                                                        (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                                        if (error == null && defoliationSequence != null)
                                                        {
                                                            string description = defoliationSequence.DefoliationSequenceDescription;

                                                            string[] defoliationParts = description.Split(',')
                                                                                                   .Select(x => x.Trim())
                                                                                                   .ToArray();

                                                            string selectedDefoliation = (fertiliserManure.Defoliation.Value > 0 && fertiliserManure.Defoliation.Value <= defoliationParts.Length)
                                                                ? $"{Enum.GetName(typeof(PotentialCut), fertiliserManure.Defoliation.Value)} ({defoliationParts[fertiliserManure.Defoliation.Value - 1]})"
                                                                : $"{fertiliserManure.Defoliation.Value}";

                                                            fertiliserManure.DefoliationName = selectedDefoliation;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            model.FertiliserManures.Add(fertiliserManure);
                        }
                        model.DefoliationCurrentCounter = 0;
                    }
                }
                else
                {
                    TempData["FieldError"] = error.Message;
                    return View(model);
                }
                if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                {
                    int grassCropCounter = 0;
                    foreach (var field in model.FieldList)
                    {
                        (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(field), model.HarvestYear.Value);
                        if (!string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["FieldError"] = error.Message;
                            return View(model);
                        }
                        if (cropList.Count > 0)
                        {
                            cropList = cropList.Where(x => x.CropOrder == 1).ToList();
                        }
                        if (cropList.Count > 0 && cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
                        {
                            (List<ManagementPeriod> managementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropList.Select(x => x.ID.Value).FirstOrDefault(), false);

                            var filteredFertiliserManure = model.FertiliserManures
                        .Where(fm => managementPeriod.Any(mp => mp.ID == fm.ManagementPeriodID) &&
                            fm.Defoliation == null).ToList();
                            if (filteredFertiliserManure != null && filteredFertiliserManure.Count == managementPeriod.Count)
                            {
                                var managementPeriodIdsToRemove = managementPeriod
                               .Skip(1)
                               .Select(mp => mp.ID.Value)
                               .ToList();
                                model.FertiliserManures.RemoveAll(fm => managementPeriodIdsToRemove.Contains(fm.ManagementPeriodID));
                            }
                            grassCropCounter++;
                            model.IsAnyCropIsGrass = true;
                        }
                    }
                    model.GrassCropCount = grassCropCounter;
                }
                else
                {
                    model.GrassCropCount = null;
                    model.IsSameDefoliationForAll = null;
                    model.IsAnyChangeInSameDefoliationFlag = false;
                }

                bool anyNewManId = false;
                if (fertiliserManureViewModel != null && fertiliserManureViewModel.FertiliserManures != null)
                {
                    anyNewManId = model.FertiliserManures.Any(newId => !fertiliserManureViewModel.FertiliserManures.Contains(newId));
                    if (anyNewManId)
                    {
                        model.IsAnyChangeInField = true;
                    }
                }
                int fertiliserCounter = 1;
                foreach (var fertiliser in model.FertiliserManures)
                {
                    (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
                    if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                        if (string.IsNullOrWhiteSpace(error.Message) && crop != null)
                        {
                            fertiliser.FieldID = crop.FieldID;
                            fertiliser.FieldName = (await _fieldLogic.FetchFieldByFieldId(fertiliser.FieldID.Value)).Name;
                            fertiliser.EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString());
                            fertiliserCounter++;
                            if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                            {
                                fertiliser.IsGrass = true;
                            }
                            else if (model.DefoliationList != null && model.DefoliationList.Any(x => x.FieldID == crop.FieldID))
                            {
                                model.DefoliationList.RemoveAll(x => x.FieldID == crop.FieldID);
                            }
                        }
                        var grass = model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).ToHashSet();
                        if (grass != null && model.DefoliationList != null)
                        {
                            model.DefoliationList = model.DefoliationList.Where(d => grass.Contains(d.FieldID)).ToList();
                        }
                        else
                        {
                            model.DefoliationList = null;
                        }
                    }
                }
            }
            else
            {
                TempData["FieldError"] = error.Message;
                return View(model);
            }
            if (model.DefoliationList != null && model.DefoliationList.Count > 0)
            {
                int counter = 1;
                model.DefoliationList.ForEach(d =>
                {
                    d.Counter = counter;
                    d.EncryptedCounter = _fieldDataProtector.Protect($"{counter++}");
                });
            }
            if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
            {
                int counter = 1;
                model.DoubleCrop.ForEach(d =>
                {
                    d.Counter = counter;
                    d.EncryptedCounter = _fieldDataProtector.Protect($"{counter++}");
                });
            }
            SetFertiliserManureToSession(model);
            if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
            {
                if (model.IsAnyCropIsGrass.HasValue && (!model.IsAnyCropIsGrass.Value))
                {
                    model.GrassCropCount = null;
                    model.IsSameDefoliationForAll = null;
                    model.IsAnyChangeInSameDefoliationFlag = false;
                    SetFertiliserManureToSession(model);
                }
                return RedirectToAction(_checkAnswerActionName);
            }
            if (model.IsDoubleCropAvailable)
            {
                return RedirectToAction(_doubleCropActionName);
            }
            else
            {
                model.DoubleCrop = null;
            }
            SetFertiliserManureToSession(model);
            if (model.IsAnyCropIsGrass.HasValue && (model.IsAnyCropIsGrass.Value))
            {
                if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
                {
                    return RedirectToAction("IsSameDefoliationForAll");
                }
                model.IsSameDefoliationForAll = true;
                SetFertiliserManureToSession(model);
                return RedirectToAction(_defoliationActionName);
            }
            return RedirectToAction("InOrgnaicManureDuration");
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in Fields() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["FieldError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> InOrgnaicManureDuration()
    {
        _logger.LogTrace("Fertiliser Manure Controller : InOrgnaicManureDuration() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        Error? error = null;
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in InOrgnaicManureDuration() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<InOrganicManureDurationResponse> OrganicManureDurationList, error) = await _fertiliserManureLogic.FetchInOrganicManureDurations();
            if (error == null && OrganicManureDurationList.Count > 0)
            {
                var SelectListItem = OrganicManureDurationList.Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name.ToString()
                }).ToList();
                ViewBag.InOrganicManureDurationsList = SelectListItem;
            }

            foreach (var fieldId in model.FieldList)
            {
                (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                if (error == null)
                {
                    WarningWithinPeriod warning = new WarningWithinPeriod();
                    string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId);

                    if (!string.IsNullOrWhiteSpace(closedPeriod))
                    {
                        int harvestYear = model.HarvestYear ?? 0;
                        string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                        Regex regex = new(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
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
                                int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1;
                                int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;
                                DateTime? closedPeriodStartDate = null;
                                DateTime? closedPeriodEndDate = null;
                                if (startMonth <= endMonth)
                                {
                                    closedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                    closedPeriodEndDate = new DateTime(harvestYear - 1, endMonth, endDay);
                                }
                                else if (startMonth >= endMonth)
                                {
                                    closedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                    closedPeriodEndDate = new DateTime(harvestYear, endMonth, endDay);
                                }

                                string formattedStartDate = closedPeriodStartDate?.ToString("d MMMM yyyy");
                                string formattedEndDate = closedPeriodEndDate?.ToString("d MMMM yyyy");

                                Crop crop = null;
                                CropTypeLinkingResponse cropTypeLinkingResponse = new CropTypeLinkingResponse();
                                if (model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(fieldId)))
                                {
                                    int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(fieldId)).Select(x => x.ManagementPeriodID).FirstOrDefault();

                                    (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                                    (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                                    (cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID ?? 0);
                                }

                                //NMaxLimitEngland is 0 for England and Whales for crops Winter beans​ ,Spring beans​, Peas​ ,Market pick peas
                                if (cropTypeLinkingResponse.NMaxLimitEngland != 0)
                                {
                                    ViewBag.ClosedPeriod = $"{formattedStartDate} to {formattedEndDate}";
                                }
                            }
                        }
                    }
                }

                Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                if (field != null && field.IsWithinNVZ == true)
                {
                    model.IsWithinNVZ = true;
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace("Farm Controller : Exception in InOrgnaicManureDuration() action : {0}, {1}", ex.Message, ex.StackTrace);
            if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                TempData["FieldError"] = ex.Message;
                if (TempData["InOrgnaicManureDurationError"] != null)
                {
                    TempData["InOrgnaicManureDurationError"] = null;
                }
                return RedirectToAction(_fieldsActionName);
            }
            else
            {
                TempData["FieldGroupError"] = ex.Message;
                if (TempData["InOrgnaicManureDurationError"] != null)
                {
                    TempData["InOrgnaicManureDurationError"] = null;
                }
                return RedirectToAction(_fieldGroupActionName);
            }
        }

        if (model.FieldList.Count == 1)
        {
            Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(model.FieldList[0]));
            model.FieldName = field.Name;
        }
        model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
        model.IsWarningMsgNeedToShow = false;
        SetFertiliserManureToSession(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InOrgnaicManureDuration(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : InOrgnaicManureDuration() post action called");
        Error? error = null;
        try
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
            {
                var dateError = ModelState["Date"]?.Errors.Count > 0 ?
                                ModelState["Date"]?.Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                 dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                 dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                 dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                 dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                 dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["Date"]?.Errors.Clear();
                    ModelState["Date"]?.Errors.Add(Resource.MsgTheDateMustInclude);
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
                foreach (var fieldId in model.FieldList)
                {
                    (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                    if (error == null)
                    {
                        WarningWithinPeriod warning = new WarningWithinPeriod();
                        string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId);

                        //model.ClosedPeriod = closedPeriod;
                        if (!string.IsNullOrWhiteSpace(closedPeriod))
                        {
                            int harvestYear = model.HarvestYear ?? 0;
                            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                            Regex regex = new Regex(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
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
                                    DateTime? closedPeriodStartDate = null;
                                    DateTime? closedPeriodEndDate = null;
                                    if (startMonth <= endMonth)
                                    {
                                        closedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                        closedPeriodEndDate = new DateTime(harvestYear - 1, endMonth, endDay);
                                    }
                                    else if (startMonth >= endMonth)
                                    {
                                        closedPeriodStartDate = new DateTime(harvestYear - 1, startMonth, startDay);
                                        closedPeriodEndDate = new DateTime(harvestYear, endMonth, endDay);
                                    }
                                    string formattedStartDate = closedPeriodStartDate?.ToString("d MMMM yyyy");
                                    string formattedEndDate = closedPeriodEndDate?.ToString("d MMMM yyyy");

                                    Crop crop = null;
                                    CropTypeLinkingResponse cropTypeLinkingResponse = new CropTypeLinkingResponse();
                                    if (model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(fieldId)))
                                    {
                                        int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(fieldId)).Select(x => x.ManagementPeriodID).FirstOrDefault();

                                        (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                                        (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                                        (cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID ?? 0);
                                    }

                                    //NMaxLimitEngland is 0 for England and Whales for crops Winter beans​ ,Spring beans​, Peas​ ,Market pick peas
                                    if (cropTypeLinkingResponse.NMaxLimitEngland != 0)
                                    {
                                        ViewBag.ClosedPeriod = $"{formattedStartDate} to {formattedEndDate}";
                                    }
                                }
                            }
                        }
                    }
                }

                return View(model);
            }

            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in InOrgnaicManureDuration() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["InOrgnaicManureDurationError"] = ex.Message;
            return View(model);
        }

        if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction("NutrientValues");
    }

    [HttpGet]
    public async Task<IActionResult> NutrientValues()
    {
        _logger.LogTrace("Fertiliser Manure Controller : NutrientValues() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in NutrientValues() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        try
        {
            if (model.FieldList != null && model.FieldList.Count == 1)
            {                
                Error? error = null;
                int fieldId;
                try
                {
                    (fieldId, ViewBag.CropTypeId, ViewBag.DefoliationSequenceName) = await PopulateRecommendationData(model, error);
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Farm Controller : Exception in NutrientValues() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                    TempData["InOrgnaicManureDurationError"] = ex.Message;
                    return RedirectToAction("InOrgnaicManureDuration", model);
                }
            }

            model.IsNitrogenExceedWarning = false;
            model.IsWarningMsgNeedToShow = false;
            model.IsClosedPeriodWarning = false;
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            TempData["InOrgnaicManureDuration"] = ex.Message;
            return RedirectToAction("InOrgnaicManureDuration");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NutrientValues(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : NutrientValues() post action called");
        Error? error = null;
        try
        {
            ValidateNutrientValues(model);

            if (!ModelState.IsValid)
            {
                if (model.FieldList != null && model.FieldList.Count == 1)
                {                    
                    int fieldId;
                    try
                    {
                        (fieldId, ViewBag.CropTypeId, ViewBag.DefoliationSequenceName) = await PopulateRecommendationData(model, error);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Farm Controller : Exception in NutrientValues() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                        TempData["NutrientValuesError"] = ex.Message;
                        return View(model);
                    }
                }
                return View(model);
            }

            model.IsNitrogenExceedWarning = false;
            model.IsClosedPeriodWarning = false;
            if (model.Lime != null)
            {
                model.Lime = Math.Round(model.Lime.Value, 1);
            }

            if (model.FieldList.Count >= 1)
            {
                FertiliserManureViewModel? fertiliserManureViewModel = GetFertiliserManureFromSession();
                if (fertiliserManureViewModel == null)
                {
                    _logger.LogError("Fertiliser Manure Controller : Session not found in NutrientValues() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
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
                        Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                        if (field != null)
                        {
                            bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                            if (isFieldIsInNVZ)
                            {
                                (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                if (error == null)
                                {
                                    int year = model.HarvestYear.Value;
                                    WarningWithinPeriod warning = new WarningWithinPeriod();
                                    string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId) ?? string.Empty;

                                    string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                                    Regex regex = new Regex(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
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

                                            DateTime startDate = new DateTime();
                                            DateTime endDate = new DateTime();

                                            if (startMonth <= endMonth)
                                            {
                                                startDate = new DateTime(year - 1, startMonth, startDay);
                                                endDate = new DateTime(year - 1, endMonth, endDay);
                                            }
                                            else if (startMonth >= endMonth)
                                            {
                                                startDate = new DateTime(year - 1, startMonth, startDay);
                                                endDate = new DateTime(year, endMonth, endDay);
                                            }


                                            Crop crop = null;
                                            if (model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(fieldId)))
                                            {
                                                int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(fieldId)).Select(x => x.ManagementPeriodID).FirstOrDefault();

                                                (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                                                (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                            }

                                            int cropOrder = model.DoubleCrop?.FirstOrDefault(x => x.FieldID == Convert.ToInt32(fieldId))?.CropOrder ?? crop?.CropOrder.Value ?? 1;
                                            List<int> managementIds = new List<int>();
                                            if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                                            {
                                                (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, cropOrder);//1 is CropOrder
                                            }
                                            else
                                            {
                                                (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);//1 is CropOrder
                                            }
                                            if (error == null)
                                            {
                                                if (managementIds.Count > 0 && model.N > 0)
                                                {
                                                    (model, error) = await IsNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, model.N.Value, startDate, endDate, cropTypeResponse.CropType, false, Convert.ToInt32(fieldId));

                                                    CropTypeLinkingResponse cropTypeLinkingResponse = null;
                                                    if (model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(model.FieldList[0])))
                                                    {
                                                        int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(model.FieldList[0])).Select(x => x.ManagementPeriodID).FirstOrDefault();

                                                        (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                                                        (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                                                        (cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID ?? 0);
                                                    }
                                                    if (cropTypeLinkingResponse != null && cropTypeLinkingResponse.NMaxLimitEngland != 0)
                                                    {
                                                        (model, error) = await IsClosedPeriodWarningMessageShow(model, false);
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
            if (model.IsNitrogenExceedWarning || model.IsClosedPeriodWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    SetFertiliserManureToSession(model);
                    return View(model);
                }
            }
            else
            {
                model.IsNitrogenExceedWarning = false;
                model.IsClosedPeriodWarning = false;
                model.IsWarningMsgNeedToShow = false;
            }
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            TempData["NutrientValuesError"] = ex.Message;
            return View(model);
        }
        return RedirectToAction(_checkAnswerActionName);
    }

    private async Task<(int fieldId, int? cropTypeId, string? defoliationSequenceName)> PopulateRecommendationData(FertiliserManureViewModel model, Error? error)
    {
        int? cropTypeId = null;
        string? defoliationSequenceName = null;
        int fieldId;
        if (model.FieldList != null && int.TryParse(model.FieldList[0], out fieldId))
        {
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(fieldId)).Name;
            List<RecommendationHeader>? recommendationsHeader = null;

            (recommendationsHeader, error) = await _cropLogic.FetchRecommendationByFieldIdAndYear(fieldId, model.HarvestYear.Value);
            if (error == null && recommendationsHeader.Count > 0)
            {
                int manId = model.FertiliserManures.FirstOrDefault().ManagementPeriodID;

                var matchedHeader = recommendationsHeader?
                .FirstOrDefault(header => header.RecommendationData != null &&
                header.RecommendationData.Any(rd => rd.ManagementPeriod != null &&
                                                   rd.ManagementPeriod.ID == manId));

                if (matchedHeader != null)
                {
                    if (matchedHeader.Crops != null)
                    {
                        cropTypeId = matchedHeader.Crops.CropTypeID;
                        if (matchedHeader.Crops.CropTypeID != null && matchedHeader.Crops.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                        {
                            (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(matchedHeader.Crops.DefoliationSequenceID.Value);
                            if (error == null && defoliationSequence != null)
                            {
                                int? defoliation = model.FertiliserManures?.FirstOrDefault()?.Defoliation;
                                if (defoliation != null)
                                {
                                    var parts = defoliationSequence.DefoliationSequenceDescription?
                                   .Split(',', StringSplitOptions.RemoveEmptyEntries);

                                    var part = parts?[defoliation.Value - 1].Trim();
                                    defoliationSequenceName = string.IsNullOrWhiteSpace(part)
                                                                        ? string.Empty
                                                                        : char.ToUpper(part[0]) + part[1..];
                                }
                            }
                        }
                    }

                    if (matchedHeader.RecommendationData != null)
                    {
                        matchedHeader.RecommendationData = matchedHeader.RecommendationData.Where(x => x.ManagementPeriod.ID == manId).ToList();
                        foreach (var recData in matchedHeader.RecommendationData)
                        {
                            model.Recommendation = new Recommendation
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
                                PBalance = recData.Recommendation.PBalance,
                                SBalance = recData.Recommendation.SBalance,
                                KBalance = recData.Recommendation.KBalance,
                                MgBalance = recData.Recommendation.MgBalance,
                                LimeBalance = recData.Recommendation.LimeBalance,
                                NaBalance = recData.Recommendation.NaBalance,
                                NBalance = recData.Recommendation.NBalance,
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
                        }
                    }
                }
            }
        }

        return (fieldId, cropTypeId, defoliationSequenceName);
    }

    private void ValidateNutrientValues(FertiliserManureViewModel model)
    {
        NValidations(model);
        P2O5Validations(model);
        K2OValidations(model);
        SO3Validations(model);
        MgOValidations(model);
        LimeValidations(model);

        bool hasValidationErrors = ModelState.Values.Any(v => v.Errors.Count > 0);
        if ((!hasValidationErrors) && model.N == null && model.P2O5 == null
            && model.K2O == null && model.SO3 == null && model.MgO == null
            && model.Lime == null)
        {
            ModelState.AddModelError("AllNutrientNull", Resource.MsgEnterAnAmountForAMinimumOfOneNutrientBeforeContinuing);
            ViewData["IsPostRequest"] = true;
        }
    }

    private void LimeValidations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("Lime"))
        {
            var limeError = ModelState["Lime"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (limeError != null && ModelState["Lime"] != null && limeError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Lime"].RawValue, Resource.lblLime)))
            {
                ModelState["Lime"]?.Errors.Clear();
                ModelState["Lime"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblLime));
            }
        }

        if (model.Lime != null)
        {
            if (model.Lime < 0 || model.Lime > 99.9m)
            {
                ModelState.AddModelError("Lime", string.Format(Resource.MsgMinMaxValidation, Resource.lblLime.ToLower(), 99.9));
            }

            if (ModelState.ContainsKey("Lime") && Math.Round(model.Lime.Value, 1) != model.Lime)
            {
                ModelState.AddModelError("Lime", string.Format(Resource.lblNutrientCanHaveOnlyOneDecimalPlace, Resource.lblLime));
            }
        }
    }

    private void MgOValidations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("MgO"))
        {
            var magnesiumMgOError = ModelState["MgO"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (magnesiumMgOError != null && magnesiumMgOError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["MgO"].RawValue, Resource.lblMgO)))
            {
                var rawValue = ModelState["MgO"]?.RawValue?.ToString();
                var errors = ModelState["MgO"]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, Resource.lblMagnesiumMgO));
                    }
                    else if (!isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblMagnesiumMgO));
                    }
                }
            }
        }

        if (model.MgO != null && (model.MgO < 0 || model.MgO > 9999))
        {
            ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblMagnesiumMgO, 9999));
        }
    }

    private void SO3Validations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("SO3"))
        {
            var sulphurSO3Error = ModelState["SO3"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (sulphurSO3Error != null && sulphurSO3Error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SO3"].RawValue, Resource.lblSO3)))
            {
                var rawValue = ModelState["SO3"]?.RawValue?.ToString();
                var errors = ModelState["SO3"]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, Resource.lblSulphurSO3));
                    }
                    else if (!isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblSulphurSO3));
                    }
                }
            }
        }

        if (model.SO3 != null && (model.SO3 < 0 || model.SO3 > 9999))
        {
            ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3Lowercase, 9999));
        }
    }

    private void K2OValidations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("K2O"))
        {
            var totalPotassiumError = ModelState["K2O"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (totalPotassiumError != null && totalPotassiumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["K2O"].RawValue, Resource.lblK2O)))
            {
                var rawValue = ModelState["K2O"]?.RawValue?.ToString();
                var errors = ModelState["K2O"]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, Resource.lblPotashK2O));
                    }
                    else if (!isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPotashK2O));
                    }
                }
            }
        }

        if (model.K2O != null && (model.K2O < 0 || model.K2O > 9999))
        {
            ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2OLowecase, 9999));
        }
    }

    private void P2O5Validations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("P2O5"))
        {
            var totalPhosphateError = ModelState["P2O5"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (totalPhosphateError != null && totalPhosphateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["P2O5"].RawValue, Resource.lblP2O5)))
            {
                var rawValue = ModelState["P2O5"]?.RawValue?.ToString();
                var errors = ModelState["P2O5"]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, Resource.lblPhosphateP2O5));
                    }
                    else if (!isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblPhosphateP2O5));
                    }
                }
            }
        }

        if (model.P2O5 != null && (model.P2O5 < 0 || model.P2O5 > 9999))
        {
            ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5Lowercase, 9999));
        }
    }

    private void NValidations(FertiliserManureViewModel model)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey("N"))
        {
            var totalNitrogenError = ModelState["N"]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (totalNitrogenError != null && totalNitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["N"].RawValue, Resource.lblN)))
            {
                var rawValue = ModelState["N"]?.RawValue?.ToString();
                var errors = ModelState["N"]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, Resource.lblNitrogen));
                    }
                    else if (!isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblNitrogen));
                    }
                }
            }
        }

        if (model.N != null && (model.N < 0 || model.N > 9999))
        {
            ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrogenLowercase, 9999));
        }
    }

    [HttpGet]
    public async Task<IActionResult> CheckAnswer(string? q, string? r, string? s, string? t, string? u)
    {
        _logger.LogTrace("Fertiliser Manure Controller : CheckAnswer() action called");
        FertiliserManureViewModel? model = new FertiliserManureViewModel();

        Error? error = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
            {
                if (!string.IsNullOrWhiteSpace(u))
                {
                    model.IsComingFromRecommendation = true;
                }
                model.EncryptedFertId = q;
                int decryptedId = Convert.ToInt32(_cropDataProtector.Unprotect(q));
                int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                model.FarmId = decryptedFarmId;
                int decryptedHarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (error.Message == null)
                {
                    model.FarmCountryId = farm.CountryID;
                }
                if (decryptedId > 0)
                {
                    (FertiliserManureDataViewModel fertiliserManure, error) = await _fertiliserManureLogic.FetchFertiliserByIdAsync(decryptedId);

                    int counter = 1;
                    if (string.IsNullOrWhiteSpace(error.Message) && fertiliserManure != null)
                    {
                        (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(decryptedId, decryptedFarmId, decryptedHarvestYear);
                        if (string.IsNullOrWhiteSpace(error.Message) && fertiliserResponse != null && fertiliserResponse.Count > 0)
                        {
                            model.UpdatedFertiliserIds = fertiliserResponse;
                            if (model.IsComingFromRecommendation)
                            {
                                model.FieldGroup = Resource.lblSelectSpecificFields;
                                model.UpdatedFertiliserIds.RemoveAll(x => x.FertiliserId != fertiliserManure.ID);
                                fertiliserResponse.RemoveAll(x => x.FertiliserId != fertiliserManure.ID);
                            }

                            var selectListItem = fertiliserResponse.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name.ToString()
                            }).ToList().DistinctBy(x => x.Value);
                            ViewBag.Fields = selectListItem.OrderBy(x => x.Text).ToList();
                            List<string> fieldName = new List<string>();
                            fieldName.Add(_cropDataProtector.Unprotect(t));
                            ViewBag.SelectedFields = fieldName;
                            if (selectListItem != null)
                            {
                                var filteredList = selectListItem
                              .Where(item => item.Text.Contains(_cropDataProtector.Unprotect(t)))
                              .ToList();
                                if (filteredList != null)
                                {
                                    model.FieldName = filteredList.Select(item => item.Text).FirstOrDefault();
                                    model.FieldList = filteredList.Select(item => item.Value).ToList();
                                    model.FieldID = filteredList.Select(x => Convert.ToInt32(x.Value)).FirstOrDefault();
                                }
                            }
                            foreach (string field in model.FieldList)
                            {
                                List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(field));
                                cropList = cropList.Where(x => x.Year == decryptedHarvestYear).ToList();

                                if (cropList != null && cropList.Count == 2)
                                {
                                    model.FieldID = Convert.ToInt32(field);
                                    model.IsDoubleCropAvailable = true;
                                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(field))).Name;
                                }
                            }

                            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);
                            if (model.IsDoubleCropAvailable)
                            {
                                string cropTypeName = string.Empty;
                                if (model.DoubleCrop == null)
                                {
                                    model.DoubleCrop = new List<DoubleCrop>();
                                }
                                int fertiliserCounter = 1;

                                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                if (string.IsNullOrWhiteSpace(error.Message))
                                {
                                    (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(crop.FieldID.Value, decryptedHarvestYear);
                                    if (string.IsNullOrWhiteSpace(error.Message))
                                    {
                                        if (cropList != null && cropList.Count == 2)
                                        {
                                            if (managementPeriod != null && (string.IsNullOrWhiteSpace(error.Message)))
                                            {
                                                cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                                                var doubleCrop = new DoubleCrop
                                                {
                                                    CropID = crop.ID.Value,
                                                    CropName = cropTypeName,
                                                    CropOrder = crop.CropOrder.Value,
                                                    FieldID = crop.FieldID.Value,
                                                    FieldName = (await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value)).Name,
                                                    EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString()), //model.DoubleCropEncryptedCounter,
                                                    Counter = model.DoubleCropCurrentCounter,
                                                };
                                                model.DoubleCrop.Add(doubleCrop);
                                                counter++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                                        {
                                            TempData["NutrientValuesError"] = error.Message;
                                            return RedirectToAction("NutrientValues");
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
                                            {
                                                TempData["NutrientRecommendationsError"] = error.Message;
                                                string fieldId = model.FieldList[0];
                                                return RedirectToAction(_recommendationsActionName, "Crop", new
                                                {
                                                    q = model.EncryptedFarmId,
                                                    r = _fieldDataProtector.Protect(fieldId),
                                                    s = model.EncryptedHarvestYear

                                                });
                                            }
                                            else
                                            {
                                                TempData["ErrorOnHarvestYearOverview"] = error.Message;
                                                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                                                {
                                                    id = model.EncryptedFarmId,
                                                    year = model.EncryptedHarvestYear
                                                });

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                                    {
                                        TempData["NutrientValuesError"] = error.Message;
                                        return RedirectToAction("NutrientValues");
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
                                        {
                                            TempData["NutrientRecommendationsError"] = error.Message;
                                            string fieldId = model.FieldList[0];
                                            return RedirectToAction(_recommendationsActionName, "Crop", new
                                            {
                                                q = model.EncryptedFarmId,
                                                r = _fieldDataProtector.Protect(fieldId),
                                                s = model.EncryptedHarvestYear
                                            });
                                        }
                                        else
                                        {
                                            TempData["ErrorOnHarvestYearOverview"] = error.Message;
                                            return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                                            {
                                                id = model.EncryptedFarmId,
                                                year = model.EncryptedHarvestYear
                                            });
                                        }
                                    }
                                }
                            }
                            int fieldIdForUpdate = Convert.ToInt32(model.FieldList.FirstOrDefault());
                            if (model.FertiliserManures == null)
                            {
                                model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                            }
                            int? defoliation = null;
                            string defoliationName = string.Empty;

                            if (!string.IsNullOrWhiteSpace(error.Message))
                            {
                                TempData["CheckYourAnswerError"] = error.Message;
                            }
                            else
                            {
                                defoliation = managementPeriod.Defoliation;
                                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                                {
                                    fertiliserManure.IsGrass = true;
                                    model.IsAnyCropIsGrass = true;

                                    int grassCounter = 1;
                                    if (model.DefoliationList == null)
                                    {
                                        model.DefoliationList = new List<DefoliationList>();
                                    }

                                    if (!string.IsNullOrWhiteSpace(error.Message))
                                    {
                                        if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                                        {
                                            TempData["NutrientValuesError"] = error.Message;
                                            return RedirectToAction("NutrientValues");
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
                                            {
                                                TempData["NutrientRecommendationsError"] = error.Message;
                                                string fieldId = model.FieldList[0];
                                                return RedirectToAction(_recommendationsActionName, "Crop", new
                                                {
                                                    q = model.EncryptedFarmId,
                                                    r = _fieldDataProtector.Protect(fieldId),
                                                    s = model.EncryptedHarvestYear
                                                });
                                            }
                                            else
                                            {
                                                TempData["ErrorOnHarvestYearOverview"] = error.Message;
                                                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                                                {
                                                    id = model.EncryptedFarmId,
                                                    year = model.EncryptedHarvestYear
                                                });
                                            }
                                        }
                                    }

                                    (managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);
                                    if (managementPeriod != null && (string.IsNullOrWhiteSpace(error.Message)))
                                    {
                                        (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                        if (error == null && defoliationSequence != null)
                                        {
                                            string description = defoliationSequence.DefoliationSequenceDescription;

                                            string[] defoliationParts = description.Split(',')
                                                                                   .Select(x => x.Trim())
                                                                                   .ToArray();
                                            string selectedDefoliation = (defoliation > 0 && defoliation.Value <= defoliationParts.Length)
                                            ? $"{Enum.GetName(typeof(PotentialCut), defoliation.Value)} - {defoliationParts[defoliation.Value - 1]}"
                                            : $"{defoliation}";
                                            var parts = selectedDefoliation.Split('-');
                                            if (parts.Length == 2)
                                            {
                                                var left = parts[0].Trim();
                                                var right = parts[1].Trim();

                                                if (!string.IsNullOrWhiteSpace(right))
                                                {
                                                    right = char.ToUpper(right[0]) + right.Substring(1);
                                                }

                                                selectedDefoliation = $"{left} - {right}";
                                            }
                                            defoliationName = selectedDefoliation;
                                            var defList = new DefoliationList
                                            {
                                                CropID = crop.ID.Value,
                                                ManagementPeriodID = fertiliserManure.ManagementPeriodID,
                                                FieldID = crop.FieldID.Value,
                                                FieldName = (await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value)).Name,
                                                EncryptedCounter = _fieldDataProtector.Protect(model.DefoliationList.Count + 1.ToString()), //model.DoubleCropEncryptedCounter,
                                                Counter = model.DefoliationList.Count + 1,
                                                Defoliation = managementPeriod.Defoliation,
                                                DefoliationName = defoliationName
                                            };
                                            model.DefoliationList.Add(defList);
                                            fertiliserManure.Defoliation = managementPeriod.Defoliation;
                                            fertiliserManure.DefoliationName = defoliationName;
                                        }
                                    }
                                }
                            }
                            fertiliserManure.FieldID = model.FieldID;
                            fertiliserManure.FieldName = model.FieldName;
                            model.FertiliserManures.Add(fertiliserManure);
                            counter++;
                        }

                        model.IsSameDefoliationForAll = true;
                        model.HarvestYear = decryptedHarvestYear;
                        model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                        model.FarmId = decryptedFarmId;
                        model.EncryptedHarvestYear = s;
                        model.EncryptedFarmId = r;
                        model.N = fertiliserManure.N;
                        model.P2O5 = fertiliserManure.P2O5;
                        model.MgO = fertiliserManure.MgO;
                        model.Lime = fertiliserManure.Lime;
                        model.SO3 = fertiliserManure.SO3;
                        model.K2O = fertiliserManure.K2O;
                        model.Date = fertiliserManure.ApplicationDate.Value.ToLocalTime();
                        model.FieldGroup = Resource.lblSelectSpecificFields;
                        SetFertiliserManureToSession(model);
                    }
                }
            }
            else
            {
                model = GetFertiliserManureFromSession();
                if (model == null)
                {
                    _logger.LogError("Fertiliser Manure Controller : Session not found in CheckAnswer() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }


            if (model.DefoliationList != null && model.DefoliationList.Count > 0)
            {
                if (model.IsSameDefoliationForAll.HasValue && model.IsSameDefoliationForAll.Value)
                {
                    model.DefoliationCurrentCounter = 1;
                }
                else
                {
                    model.DefoliationCurrentCounter = model.DefoliationList.Count;
                }
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            }
            if (model != null && model.FieldList != null)
            {
                model.IsClosedPeriodWarningOnlyForGrassAndOilseed = false;
                model.IsClosedPeriodWarning = false;
                if (model.N > 0)
                {
                    if (model.FieldList.Count >= 1)
                    {
                        Crop crop = null;
                        CropTypeLinkingResponse cropTypeLinkingResponse = null;
                        if (model.FertiliserManures != null && model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(model.FieldList[0])))
                        {
                            int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(model.FieldList[0])).Select(x => x.ManagementPeriodID).FirstOrDefault();

                            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                            (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                            (cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID ?? 0);
                        }

                        //NMaxLimitEngland is 0 for England and Whales for crops Winter beans​ ,Spring beans​, Peas​ ,Market pick peas
                        if (cropTypeLinkingResponse != null && cropTypeLinkingResponse.NMaxLimitEngland != 0)
                        {
                            (model, error) = await IsClosedPeriodWarningMessageShow(model, false);
                        }
                    }

                    foreach (var fieldId in model.FieldList)
                    {
                        Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                        if (field != null)
                        {
                            bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                            if (isFieldIsInNVZ)
                            {
                                (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                if (error == null)
                                {
                                    int year = model.HarvestYear.Value;
                                    WarningWithinPeriod warning = new WarningWithinPeriod();
                                    string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId) ?? string.Empty;

                                    string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
                                    Regex regex = new Regex(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMicroseconds(100));
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

                                            DateTime startDate = new DateTime();
                                            DateTime endDate = new DateTime();

                                            if (startMonth <= endMonth)
                                            {
                                                startDate = new DateTime(year - 1, startMonth, startDay);
                                                endDate = new DateTime(year - 1, endMonth, endDay);
                                            }
                                            else if (startMonth >= endMonth)
                                            {
                                                startDate = new DateTime(year - 1, startMonth, startDay);
                                                endDate = new DateTime(year, endMonth, endDay);
                                            }

                                            Crop crop = null;
                                            if (model.FertiliserManures.Any(x => x.FieldID == Convert.ToInt32(fieldId)))
                                            {
                                                int manId = model.FertiliserManures.Where(x => x.FieldID == Convert.ToInt32(fieldId)).Select(x => x.ManagementPeriodID).FirstOrDefault();

                                                (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manId);
                                                (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                                            }
                                            int cropOrder = model.DoubleCrop?.FirstOrDefault(x => x.FieldID == Convert.ToInt32(fieldId))?.CropOrder
                                               ?? crop?.CropOrder.Value ?? 1;

                                            List<int> managementIds = new List<int>();
                                            if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
                                            {
                                                (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, cropOrder);//1 is CropOrder
                                            }
                                            else
                                            {
                                                (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup, null);//1 is CropOrder
                                            }
                                            if (error == null)
                                            {
                                                if (managementIds.Count > 0 && model.N > 0)
                                                {
                                                    (model, error) = await IsNitrogenExceedWarning(model, managementIds[0], cropTypeResponse.CropTypeId, model.N.Value, startDate, endDate, cropTypeResponse.CropType, false, Convert.ToInt32(fieldId));
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
                                    if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                                    {
                                        TempData["NutrientValuesError"] = error.Message;
                                        return RedirectToAction("NutrientValues", model);
                                    }
                                    else
                                    {
                                        TempData["CheckYourAnswerError"] = error.Message;
                                        return View(model);
                                    }
                                }
                                if (model.IsNitrogenExceedWarning)
                                {
                                    if (!model.IsWarningMsgNeedToShow)
                                    {
                                        model.IsWarningMsgNeedToShow = true;
                                    }
                                }
                                else
                                {
                                    model.IsNitrogenExceedWarning = false;
                                    model.IsWarningMsgNeedToShow = false;
                                }
                            }
                        }
                    }
                }

            }
            model.IsDoubleCropValueChange = false;
            model.IsCheckAnswer = true;
            model.IsAnyChangeInField = false;
            model.IsAnyChangeInSameDefoliationFlag = false;
            if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed || model.IsClosedPeriodWarning || model.IsNitrogenExceedWarning)
            {
                model.IsWarningMsgNeedToShow = true;
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
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
                            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                            {
                                ViewBag.Fields = fieldList;
                            }
                            if (model.FieldList != null && model.FieldList.Count == 1 && fieldNames != null)
                            {
                                model.FieldName = fieldNames.FirstOrDefault();
                            }
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(model.EncryptedFertId))
                {
                    (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId)), model.FarmId.Value, model.HarvestYear.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && fertiliserResponse != null && fertiliserResponse.Count > 0)
                    {
                        var SelectListItem = fertiliserResponse.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name.ToString()
                        }).ToList().DistinctBy(x => x.Value);
                        ViewBag.Fields = SelectListItem.OrderBy(x => x.Text).ToList();
                    }
                }
            }
            SetFertiliserManureToSession(model);

            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
            {
                SetFertiliserManureToSession(model);
            }

            bool isDataChanged = false;
            var previousModel = HttpContext.Session.GetObjectFromJson<FertiliserManureViewModel>("FertiliserDataBeforeUpdate");
            if (previousModel != null)
            {
                string oldJson = JsonConvert.SerializeObject(previousModel);
                string newJson = JsonConvert.SerializeObject(model);

                isDataChanged = !string.Equals(oldJson, newJson, StringComparison.Ordinal);
            }
            ViewBag.IsDataChange = isDataChanged;

        }
        catch (Exception ex)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
            {
                TempData["NutrientValuesError"] = ex.Message;
                return RedirectToAction("NutrientValues");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
                {
                    TempData["NutrientRecommendationsError"] = ex.Message;
                    string fieldId = model.FieldList[0];
                    return RedirectToAction(_recommendationsActionName, "Crop", new
                    {
                        q = model.EncryptedFarmId,
                        r = _fieldDataProtector.Protect(fieldId),
                        s = model.EncryptedHarvestYear

                    });
                }
                else
                {
                    TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                    return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear
                    });
                }
            }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckAnswer(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : CheckAnswer() post action called");
        Error error = new Error();
        try
        {
            if (model.DoubleCrop == null && model.IsDoubleCropAvailable)
            {
                int index = 0;
                List<Crop> cropList = new List<Crop>();
                string cropTypeName = string.Empty;
                if (model.DoubleCrop == null)
                {
                    foreach (string fieldId in model.FieldList)
                    {
                        (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fieldId), model.HarvestYear.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            if (cropList != null && cropList.Count == 2)
                            {
                                ModelState.AddModelError("FieldName", string.Format("{0} {1}", string.Format(Resource.lblWhichCropIsThisManureApplication, (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name), Resource.lblNotSet));
                                index++;
                            }
                        }
                        else
                        {
                            TempData["CheckYourAnswerError"] = error.Message;
                            return View(model);
                        }
                    }
                }
            }

            if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
            {
                if (model.GrassCropCount.HasValue && model.GrassCropCount > 1 && model.IsSameDefoliationForAll == null)
                {
                    ModelState.AddModelError("IsSameDefoliationForAll", string.Format("{0} {1}", Resource.lblForMultipleDefoliation, Resource.lblNotSet));
                }

                int i = 0;
                foreach (var defoliation in model.DefoliationList)
                {
                    if (model.IsSameDefoliationForAll.HasValue && (model.IsSameDefoliationForAll.Value) && (model.GrassCropCount > 1) && defoliation.Defoliation == null)
                    {
                        ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format("{0} {1}", Resource.lblWhichCutOrGrazingInThisInorganicApplicationForAllField, Resource.lblNotSet));
                    }
                    else if (defoliation.Defoliation == null)
                    {
                        ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format("{0} {1}", string.Format(Resource.lblWhichCutOrGrazingInThisInorganicApplicationForInField, defoliation.FieldName), Resource.lblNotSet));
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                model.IsCheckAnswer = false;
                SetFertiliserManureToSession(model);
                return View(model);
            }
            if (model.FertiliserManures.Count > 0)
            {
                foreach (var fertiliserManure in model.FertiliserManures)
                {
                    fertiliserManure.ManagementPeriodID = fertiliserManure.ManagementPeriodID;
                    fertiliserManure.ApplicationDate = model.Date.Value;
                    fertiliserManure.N = model.N ?? 0;
                    fertiliserManure.P2O5 = model.P2O5 ?? 0;
                    fertiliserManure.K2O = model.K2O ?? 0;
                    fertiliserManure.SO3 = model.SO3 ?? 0;
                    fertiliserManure.Lime = model.Lime ?? 0;
                    fertiliserManure.MgO = model.MgO ?? 0;
                    fertiliserManure.ApplicationRate = 1;
                }
            }
            if (model.FertiliserManures.Count > 0)
            {
                List<FertiliserManure> fertiliserList = new List<FertiliserManure>();
                List<WarningMessage> warningMessageList = new List<WarningMessage>();
                var FertiliserManure = new List<object>();
                foreach (FertiliserManure fertiliserManure in model.FertiliserManures)
                {
                    FertiliserManure fertManure = new FertiliserManure
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

                    fertiliserList.Add(fertManure);
                    warningMessageList.AddRange(await GetWarningMessages(model));

                    FertiliserManure.Add(new
                    {
                        FertiliserManure = fertManure,
                        WarningMessages = warningMessageList.Count > 0 ? warningMessageList : null,
                    });
                }

                var jsonData = new { FertiliserManure };
                string jsonString = JsonConvert.SerializeObject(jsonData);

                (List<FertiliserManure> fertiliserResponse, error) = await _fertiliserManureLogic.AddFertiliserManureAsync(jsonString);

                if (error == null)
                {
                    string successMsg = Resource.lblFertilisersHavebeenSuccessfullyAdded;
                    string successMsgSecond = Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation;
                    bool success = true;
                    RemoveFertiliserManureSession();
                    if (!model.IsComingFromRecommendation)
                        return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                        {
                            id = model.EncryptedFarmId,
                            year = model.EncryptedHarvestYear,
                            q = _farmDataProtector.Protect(success.ToString()),
                            r = _cropDataProtector.Protect(successMsg),
                            v = _cropDataProtector.Protect(successMsgSecond)
                        });
                    else
                    {
                        string fieldId = model.FieldList[0];
                        return RedirectToAction(_recommendationsActionName, "Crop", new
                        {
                            q = model.EncryptedFarmId,
                            r = _fieldDataProtector.Protect(fieldId),
                            s = model.EncryptedHarvestYear,
                            t = _cropDataProtector.Protect(successMsg),
                            u = _cropDataProtector.Protect(successMsgSecond)

                        });
                    }
                }
                else
                {
                    TempData["CheckYourAnswerError"] = error.Message;
                }
            }
        }
        catch (Exception ex)
        {
            TempData["CheckYourAnswerError"] = ex.Message;
            return View(model);
        }
        return View(model);
    }
    public IActionResult BackCheckAnswer()
    {
        _logger.LogTrace("Fertiliser Manure Controller : BackCheckAnswer() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in BackCheckAnswer() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        model.IsCheckAnswer = false;
        SetFertiliserManureToSession(model);
        if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (!model.IsComingFromRecommendation))
        {
            RemoveFertiliserManureSession();
            return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
            {
                id = model.EncryptedFarmId,
                year = model.EncryptedHarvestYear
            });
        }
        else if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
        {
            RemoveFertiliserManureSession();
            string fieldId = model.FieldList[0];
            return RedirectToAction(_recommendationsActionName, "Crop", new
            {
                q = model.EncryptedFarmId,
                r = _fieldDataProtector.Protect(fieldId),
                s = model.EncryptedHarvestYear

            });
        }
        return RedirectToAction("NutrientValues");
    }

    private async Task<(FertiliserManureViewModel, Error?)> IsClosedPeriodWarningMessageShow(FertiliserManureViewModel model, bool isGetCheckAnswer)
    {
        Error? error = null;
        string warningMsg = string.Empty;
        foreach (var fieldId in model.FieldList)
        {
            Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
            if (field != null)
            {
                bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
                if (isFieldIsInNVZ)
                {
                    (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                    if (error == null)
                    {
                        (FieldDetailResponse fieldDetail, error) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                        if (error == null)
                        {
                            //warning excel sheet row no. 23
                            HashSet<int> filterCrops = new HashSet<int>
                            {
                                (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape,
                                (int)NMP.Commons.Enums.CropTypes.Asparagus,
                                (int)NMP.Commons.Enums.CropTypes.ForageRape,
                                (int)NMP.Commons.Enums.CropTypes.ForageSwedesRootsLifted,
                                (int)NMP.Commons.Enums.CropTypes.KaleGrazed,
                                (int)NMP.Commons.Enums.CropTypes.StubbleTurnipsGrazed,
                                (int)NMP.Commons.Enums.CropTypes.SwedesGrazed,
                                (int)NMP.Commons.Enums.CropTypes.TurnipsRootLifted,
                                (int)NMP.Commons.Enums.CropTypes.BrusselSprouts,
                                (int)NMP.Commons.Enums.CropTypes.Cabbage,
                                (int)NMP.Commons.Enums.CropTypes.Calabrese,
                                (int)NMP.Commons.Enums.CropTypes.Cauliflower,
                                (int)NMP.Commons.Enums.CropTypes.Radish,
                                (int)NMP.Commons.Enums.CropTypes.WildRocket,
                                (int)NMP.Commons.Enums.CropTypes.Swedes,
                                (int)NMP.Commons.Enums.CropTypes.Turnips,
                                (int)NMP.Commons.Enums.CropTypes.BulbOnions,
                                (int)NMP.Commons.Enums.CropTypes.SaladOnions,
                                (int)NMP.Commons.Enums.CropTypes.Grass
                            };


                            WarningWithinPeriod warning = new WarningWithinPeriod();
                            string closedPeriod = warning.ClosedPeriodForFertiliser(cropTypeResponse.CropTypeId) ?? string.Empty;
                            bool isWithinClosedPeriod = warning.IsFertiliserApplicationWithinWarningPeriod(model.Date.Value, closedPeriod);

                            if (!filterCrops.Contains(cropTypeResponse.CropTypeId))
                            {
                                if (isWithinClosedPeriod)
                                {
                                    WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.NitroFertClosedPeriod.ToString());

                                    model.IsClosedPeriodWarning = true;
                                    model.ClosedPeriodWarningHeader = warningResponse.Header;
                                    model.ClosedPeriodWarningCodeID = warningResponse.WarningCodeID;
                                    model.ClosedPeriodWarningLevelID = warningResponse.WarningLevelID;
                                    model.ClosedPeriodWarningPara1 = warningResponse.Para1;
                                    model.ClosedPeriodWarningPara3 = warningResponse.Para3;

                                }
                            }
                            //warning excel sheet row no. 28
                            if (cropTypeResponse.CropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape || cropTypeResponse.CropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass)
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
                                    WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgFertDateOnly.ToString());
                                    model.IsClosedPeriodWarning = true;
                                    model.ClosedPeriodWarningHeader = warningResponse.Header;
                                    model.ClosedPeriodWarningCodeID = warningResponse.WarningCodeID;
                                    model.ClosedPeriodWarningLevelID = warningResponse.WarningLevelID;
                                    model.ClosedPeriodWarningPara1 = warningResponse.Para1;
                                    model.ClosedPeriodWarningPara3 = warningResponse.Para3;
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
    private async Task<(FertiliserManureViewModel, Error?)> IsNitrogenExceedWarning(FertiliserManureViewModel model, int managementId, int cropTypeId, decimal appNitrogen, DateTime startDate, DateTime endDate, string cropType, bool isGetCheckAnswer, int fieldId)
    {
        Error? error = null;
        string nitrogenExceedMessageTitle = string.Empty;
        string warningMsg = string.Empty;
        string nitrogenExceedFirstAdditionalMessage = string.Empty;
        string nitrogenExceedSecondAdditionalMessage = string.Empty;
        decimal totalNitrogen = 0;
        //if we are coming for update then we will exclude the fertiliserId.
        if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
        {
            (totalNitrogen, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endDate, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
        }
        else
        {
            (totalNitrogen, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endDate, null, false);

        }
        if (error == null)
        {
            WarningWithinPeriod warningMessage = new WarningWithinPeriod();
            string message = string.Empty;
            totalNitrogen = totalNitrogen + Convert.ToDecimal(model.N);

            HashSet<int> brassicaCrops = new HashSet<int>
            {
                (int)NMP.Commons.Enums.CropTypes.ForageRape,
                (int)NMP.Commons.Enums.CropTypes.ForageSwedesRootsLifted,
                (int)NMP.Commons.Enums.CropTypes.KaleGrazed,
                (int)NMP.Commons.Enums.CropTypes.StubbleTurnipsGrazed,
                (int)NMP.Commons.Enums.CropTypes.SwedesGrazed,
                (int)NMP.Commons.Enums.CropTypes.TurnipsRootLifted,
                (int)NMP.Commons.Enums.CropTypes.BrusselSprouts,
                (int)NMP.Commons.Enums.CropTypes.Cabbage,
                (int)NMP.Commons.Enums.CropTypes.Calabrese,
                (int)NMP.Commons.Enums.CropTypes.Cauliflower,
                (int)NMP.Commons.Enums.CropTypes.Radish,
                (int)NMP.Commons.Enums.CropTypes.WildRocket,
                (int)NMP.Commons.Enums.CropTypes.Swedes,
                (int)NMP.Commons.Enums.CropTypes.Turnips
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
            //warning excel sheet row no. 25
            if (brassicaCrops.Contains(cropTypeId) && isWithinClosedPeriod)
            {
                DateTime fourWeekDate = model.Date.Value.AddDays(-28);
                decimal nitrogenInFourWeek = 0;
                //if we are coming for update then we will exclude the fertiliserId.
                if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                {
                    (nitrogenInFourWeek, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, fourWeekDate, model.Date.Value, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
                }
                else
                {
                    (nitrogenInFourWeek, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, fourWeekDate, model.Date.Value, null, false);

                }

                if (error == null)
                {

                    if (totalNitrogen > 100 || model.N.Value > 50 || nitrogenInFourWeek > 0)  //nitrogenInFourWeek>0 means check Nitrogen applied within 28 days
                    {
                        WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateBrassica.ToString());

                        model.IsNitrogenExceedWarning = true;
                        model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
                        model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
                        model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
                        model.ClosedPeriodNitrogenExceedWarningPara1 = warningResponse.Para1;
                        model.ClosedPeriodNitrogenExceedWarningPara2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, startPeriod, endPeriod) : null;
                        model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;
                    }
                }
                else
                {
                    return (model, error);
                }
            }

            //warning excel sheet row no. 24
            if ((cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Commons.Enums.CropTypes.SaladOnions) && isWithinClosedPeriod)
            {
                bool isNitrogenRateExceeded = false;
                int maxNitrogenRate = 0;
                if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus)
                {
                    if (totalNitrogen > 50)
                    {
                        isNitrogenRateExceeded = true;
                        maxNitrogenRate = 50;
                    }
                }
                if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions)
                {
                    if (totalNitrogen > 40)
                    {
                        isNitrogenRateExceeded = true;
                        maxNitrogenRate = 40;
                    }
                }
                if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.SaladOnions)
                {
                    if (totalNitrogen > 40)
                    {
                        isNitrogenRateExceeded = true;
                        maxNitrogenRate = 40;
                    }
                }
                if (isNitrogenRateExceeded)
                {
                    WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRate.ToString());

                    model.IsNitrogenExceedWarning = true;
                    model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
                    model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
                    model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
                    model.ClosedPeriodNitrogenExceedWarningPara1 = warningResponse.Para1;
                    model.ClosedPeriodNitrogenExceedWarningPara2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, startPeriod, endPeriod, maxNitrogenRate) : null;
                    model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;
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
            decimal PreviousApplicationsNitrogen = 0;
            //if we are coming for update then we will exclude the fertiliserId.
            if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
            {
                (PreviousApplicationsNitrogen, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endOfOctober, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
            }
            else
            {
                (PreviousApplicationsNitrogen, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endOfOctober, null, false);
            }

            //warning excel sheet row no. 26
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape && isWithinWarningPeriod)
            {
                bool isNitrogenRateExceeded = false;

                if ((PreviousApplicationsNitrogen + model.N.Value) > 30)
                {
                    isNitrogenRateExceeded = true;
                }

                if (isNitrogenRateExceeded)
                {
                    WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateOSR.ToString());

                    model.IsNitrogenExceedWarning = true;
                    model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
                    model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
                    model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
                    model.ClosedPeriodNitrogenExceedWarningPara1 = Resource.MsgClosedPeriodNitrogenExceedWarningHeadingEngland;
                    model.ClosedPeriodNitrogenExceedWarningPara2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, startPeriod) : null;
                    model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;

                }
            }
            //warning excel sheet row no. 27
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass && isWithinWarningPeriod)
            {
                bool isNitrogenRateExceeded = false;
                string startString = $"{startPeriod} {startDate.Year}";
                DateTime start = DateTime.ParseExact(startString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                string endString = $"{endPeriod} {startDate.Year}";  //because closed period start and 31 october will be in same year
                DateTime end = DateTime.ParseExact(endString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                decimal nitrogenWithinWarningPeriod = 0;
                //if we are coming for update then we will exclude the fertiliserId.
                if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                {
                    (nitrogenWithinWarningPeriod, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, start, end, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
                }
                else
                {
                    (nitrogenWithinWarningPeriod, error) = await _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate(fieldId, start, end, null, false);

                }

                if (model.N.Value > 40 || (nitrogenWithinWarningPeriod + model.N.Value) > 80)
                {
                    isNitrogenRateExceeded = true;
                }

                if (isNitrogenRateExceeded)
                {
                    WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateGrass.ToString());

                    model.IsNitrogenExceedWarning = true;
                    model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
                    model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
                    model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
                    model.ClosedPeriodNitrogenExceedWarningPara1 = warningResponse.Para1;
                    model.ClosedPeriodNitrogenExceedWarningPara2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, startPeriod) : null;
                    model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;
                }
            }
            //warning excel sheet row no. 8

            //NMax limit for crop logic
            decimal previousApplicationsN = 0;
            decimal currentApplicationNitrogen = Convert.ToDecimal(model.N);
            //if we are coming for update then we will exclude the fertiliserId.
            if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
            {
                (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), null);
            }
            else
            {
                (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(managementId, false, null, null);
            }

            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(fieldId));
            var crop = cropsResponse.Where(x => x.Year == model.HarvestYear && x.Confirm == false).ToList();
            if (crop != null)
            {
                (CropTypeLinkingResponse cropTypeLinking, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop[0].CropTypeID.Value);
                if (error == null)
                {
                    int? nmaxLimitEnglandOrWales = (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Wales ? cropTypeLinking.NMaxLimitWales : cropTypeLinking.NMaxLimitEngland);
                    if (nmaxLimitEnglandOrWales != null)
                    {
                        (FieldDetailResponse fieldDetail, error) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                        if (error == null)
                        {
                            if (error == null)
                            {
                                decimal nMaxLimit = 0;

                                (List<int> currentYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
                                (List<int> previousYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
                                if (error == null)
                                {
                                    nMaxLimit = nmaxLimitEnglandOrWales ?? 0;

                                    OrganicManureNMaxLimitLogic organicManureNMaxLimitLogic = new OrganicManureNMaxLimitLogic();
                                    bool hasSpecialManure = Functions.HasSpecialManure(currentYearManureTypeIds, null) || Functions.HasSpecialManure(previousYearManureTypeIds, null);
                                    nMaxLimit = organicManureNMaxLimitLogic.NMaxLimit(Convert.ToInt32(nMaxLimit), crop[0].Yield == null ? null : crop[0].Yield.Value, fieldDetail.SoilTypeName, crop[0].CropInfo1 == null ? null : crop[0].CropInfo1.Value, crop[0].CropTypeID.Value, crop[0].PotentialCut ?? 0, hasSpecialManure);

                                    //correction begin for user story NMPT-1742
                                    decimal totalNitrogenApplied = 0;
                                    if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                                    {
                                        totalNitrogenApplied = previousApplicationsN;
                                    }
                                    else
                                    {
                                        totalNitrogenApplied = previousApplicationsN + currentApplicationNitrogen;
                                    }
                                    //end
                                    if (totalNitrogenApplied > nMaxLimit)
                                    {
                                        model.IsNitrogenExceedWarning = true;
                                        (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);

                                        WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(farm.CountryID ?? 0, NMP.Commons.Enums.WarningKey.NMaxLimit.ToString());

                                        model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
                                        model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
                                        model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
                                        model.ClosedPeriodNitrogenExceedWarningPara1 = warningResponse.Para1;
                                        model.ClosedPeriodNitrogenExceedWarningPara2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, nMaxLimit) : null;
                                        model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFertiliser(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : UpdateFertiliser() post action called");
        Error error = new Error();
        try
        {
            if (model.DoubleCrop == null && model.IsDoubleCropAvailable)
            {
                int index = 0;
                List<Crop> cropList = new List<Crop>();
                if (model.DoubleCrop == null)
                {
                    foreach (string fieldId in model.FieldList)
                    {
                        (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fieldId), model.HarvestYear.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            if (cropList != null && cropList.Count == 2)
                            {
                                ModelState.AddModelError("FieldName", string.Format("{0} {1}", string.Format(Resource.lblWhichCropIsThisManureApplication, (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name), Resource.lblNotSet));
                                index++;
                            }
                        }
                        else
                        {
                            TempData["CheckYourAnswerError"] = error.Message;
                            return View(model);
                        }
                    }
                }
            }

            if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
            {
                if (model.GrassCropCount.HasValue && model.GrassCropCount > 1 && model.IsSameDefoliationForAll == null)
                {
                    ModelState.AddModelError("IsSameDefoliationForAll", string.Format("{0} {1}", Resource.lblForMultipleDefoliation, Resource.lblNotSet));
                }

                int i = 0;
                foreach (var defoliation in model.DefoliationList)
                {
                    if (model.IsSameDefoliationForAll.HasValue && (model.IsSameDefoliationForAll.Value) && (model.GrassCropCount > 1) && defoliation.Defoliation == null)
                    {
                        ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format("{0} {1}", Resource.lblWhichCutOrGrazingInThisInorganicApplicationForAllField, Resource.lblNotSet));
                    }
                    else if (defoliation.Defoliation == null)
                    {
                        ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format("{0} {1}", string.Format(Resource.lblWhichCutOrGrazingInThisInorganicApplicationForInField, defoliation.FieldName), Resource.lblNotSet));
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                SetFertiliserManureToSession(model);
                return RedirectToAction(_checkAnswerActionName);
            }

            if (!string.IsNullOrWhiteSpace(model.EncryptedFertId))
            {
                if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                {
                    if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                    {
                        List<FertiliserManure> fertiliserList = new List<FertiliserManure>();
                        List<WarningMessage> warningMessageList = new List<WarningMessage>();
                        var FertiliserManure = new List<object>();
                        foreach (FertiliserManureDataViewModel fertiliserManure in model.FertiliserManures)
                        {
                            int? fertID = model.UpdatedFertiliserIds != null ? (model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId.Value == fertiliserManure.ManagementPeriodID).Select(x => x.FertiliserId.Value).FirstOrDefault()) : 0;

                            FertiliserManure fertManure = new FertiliserManure
                            {
                                ID = fertID,
                                ManagementPeriodID = fertiliserManure.ManagementPeriodID,
                                ApplicationDate = model.Date,
                                ApplicationRate = 1,
                                Confirm = fertiliserManure.Confirm,
                                N = model.N ?? 0,
                                P2O5 = model.P2O5 ?? 0,
                                K2O = model.K2O ?? 0,
                                SO3 = model.SO3 ?? 0,
                                Lime = model.Lime ?? 0,
                                MgO = model.MgO ?? 0,
                                Na2O = fertiliserManure.Na2O ?? 0,
                                NFertAnalysisPercent = fertiliserManure.NFertAnalysisPercent ?? 0,
                                P2O5FertAnalysisPercent = fertiliserManure.P2O5FertAnalysisPercent ?? 0,
                                K2OFertAnalysisPercent = fertiliserManure.K2OFertAnalysisPercent ?? 0,
                                MgOFertAnalysisPercent = fertiliserManure.MgOFertAnalysisPercent ?? 0,
                                SO3FertAnalysisPercent = fertiliserManure.SO3FertAnalysisPercent ?? 0,
                                Na2OFertAnalysisPercent = fertiliserManure.Na2OFertAnalysisPercent ?? 0,
                                NH4N = fertiliserManure.NH4N ?? 0,
                                NO3N = fertiliserManure.NO3N ?? 0,
                            };
                            fertiliserList.Add(fertManure);

                            warningMessageList = new List<WarningMessage>();
                            warningMessageList = await GetWarningMessages(model);
                            warningMessageList.ForEach(x => x.JoiningID = x.WarningCodeID != (int)NMP.Commons.Enums.WarningCode.NMaxLimit ? fertID : fertiliserManure.FieldID);
                            FertiliserManure.Add(new
                            {
                                FertiliserManure = fertManure,
                                WarningMessages = warningMessageList.Count > 0 ? warningMessageList : null,
                            });
                        }
                        var jsonData = new
                        {
                            FertiliserManure
                        };
                        string jsonString = JsonConvert.SerializeObject(jsonData);
                        (List<FertiliserManure> fertiliser, error) = await _fertiliserManureLogic.UpdateFertiliser(jsonString);
                        if (string.IsNullOrWhiteSpace(error.Message) && fertiliser.Count > 0)
                        {
                            bool success = true;
                            RemoveFertiliserManureSession();

                            if (model.FieldList != null && model.FieldList.Count == 1)
                            {
                                if (!model.IsComingFromRecommendation)
                                {
                                    return Redirect(Url.Action(_harvestYearOverviewActionName, "Crop", new
                                    {
                                        id = model.EncryptedFarmId,
                                        year = model.EncryptedHarvestYear,
                                        q = _farmDataProtector.Protect(success.ToString()),
                                        r = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationUpdated),
                                        w = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault())
                                    }) + Resource.lblInorganicFertiliserApplicationsForSorting);
                                }
                                else
                                {
                                    return RedirectToAction(_recommendationsActionName, "Crop", new
                                    {
                                        q = model.EncryptedFarmId,
                                        r = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault()),
                                        s = model.EncryptedHarvestYear,
                                        t = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationUpdated),
                                        u = _cropDataProtector.Protect(Resource.MsgNutrientRecommendationsMayBeUpdated)

                                    });
                                }
                            }
                            else
                            {
                                return Redirect(Url.Action(_harvestYearOverviewActionName, "Crop", new
                                {
                                    id = model.EncryptedFarmId,
                                    year = model.EncryptedHarvestYear,
                                    q = _farmDataProtector.Protect(success.ToString()),
                                    r = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationUpdated),
                                    v = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations)
                                }) + Resource.lblInorganicFertiliserApplicationsForSorting);
                            }
                        }
                        else
                        {
                            TempData["CheckYourAnswerError"] = error.Message;
                            return RedirectToAction(_checkAnswerActionName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TempData["CheckYourAnswerError"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
        return RedirectToAction(_checkAnswerActionName);
    }
    [HttpGet]
    public async Task<IActionResult> RemoveFertiliser(string q, string r, string s, string? t, string? u, string? v)
    {
        _logger.LogTrace("Fertiliser Manure Controller : RemoveFertiliser() action called");
        FertiliserManureViewModel? model = new FertiliserManureViewModel();
        Error? error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                model = GetFertiliserManureFromSession();
                if (model == null)
                {
                    _logger.LogError("Fertiliser Manure Controller : Session not found in RemoveFertiliser() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                if (model != null)
                {
                    if (model.FieldList != null && model.FieldList.Count > 0)
                    {
                        (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
                        if (error == null)
                        {
                            if (fieldList.Count > 0)
                            {
                                var fieldNames = fieldList
                                                 .Where(field => model.FieldList.Contains(field.Id.ToString())).OrderBy(field => field.Name)
                                                 .Select(field => field.Name)
                                                 .ToList();

                                if (fieldNames != null && fieldNames.Count == 1)
                                {
                                    model.FieldName = fieldNames.FirstOrDefault();
                                }
                                else if (fieldNames != null)
                                {
                                    model.FieldName = string.Empty;
                                    ViewBag.SelectedFields = fieldNames.OrderBy(name => name).ToList();
                                }
                                ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                            }
                        }
                    }
                }
            }
            else
            {
                model.IsComingFromRecommendation = true;
                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.EncryptedFertId = q;
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    ViewBag.EncryptedFieldId = r;
                    model.FieldList = [_fieldDataProtector.Unprotect(r)];
                }
                if (!string.IsNullOrWhiteSpace(s))
                {
                    model.FieldName = _cropDataProtector.Unprotect(s);
                }

                if (!string.IsNullOrWhiteSpace(t))
                {
                    model.EncryptedFarmId = t;
                    model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(t));
                }

                if (!string.IsNullOrWhiteSpace(u))
                {
                    model.EncryptedHarvestYear = u;
                    model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(u));
                }
                SetFertiliserManureToSession(model);
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace("OrganicManure Controller : Exception in RemoveFertiliser() action : {0}, {1}", ex.Message, ex.StackTrace);
            if (model.IsComingFromRecommendation)
            {
                TempData["NutrientRecommendationsError"] = ex.Message;
                return RedirectToAction(_recommendationsActionName, "Crop", new { q = model.EncryptedFarmId, r, s = model.EncryptedHarvestYear });
            }

            TempData["CheckYourAnswerError"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFertiliser(FertiliserManureViewModel model)
    {
        _logger.LogTrace($"Fertiliser Manure Controller : RemoveFertiliser() post action called");
        Error error = null;
        if (model.IsDeleteFertliser == null)
        {
            ModelState.AddModelError("IsDeleteFertliser", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
                if (error == null)
                {
                    if (fieldList.Count > 0)
                    {
                        var fieldNames = fieldList
                                         .Where(field => model.FieldList.Contains(field.Id.ToString())).OrderBy(field => field.Name)
                                         .Select(field => field.Name)
                                         .ToList();

                        if (fieldNames != null && fieldNames.Count == 1)
                        {
                            model.FieldName = fieldNames.FirstOrDefault();
                        }
                        else if (fieldNames != null)
                        {
                            model.FieldName = string.Empty;
                            ViewBag.SelectedFields = fieldNames.OrderBy(name => name).ToList();
                        }
                        ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                    }
                }
            }
            return View(model);
        }
        try
        {
            if (!model.IsDeleteFertliser.Value)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            else
            {
                List<int> fertiliserIds = new List<int>();
                if (model.IsComingFromRecommendation && (!string.IsNullOrWhiteSpace(model.EncryptedFertId)))
                {
                    ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                    fertiliserIds.Add(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId)));
                }
                else if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0 && model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                {
                    foreach (string fieldId in model.FieldList)
                    {
                        string fieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name;
                        foreach (var FertManure in model.UpdatedFertiliserIds)
                        {
                            if (fieldName.Equals(FertManure.Name))
                            {
                                fertiliserIds.Add(FertManure.FertiliserId.Value);
                            }
                        }
                    }
                }

                if (fertiliserIds.Count > 0)
                {
                    var result = new
                    {
                        fertliserManureIds = fertiliserIds
                    };
                    string jsonString = JsonConvert.SerializeObject(result);
                    (string success, error) = await _fertiliserManureLogic.DeleteFertiliserByIdAsync(jsonString);

                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        RemoveFertiliserManureSession();

                        if (model.IsComingFromRecommendation)
                        {
                            if (model.FieldList != null && model.FieldList.Count > 0)
                            {
                                string encryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                                if (!string.IsNullOrWhiteSpace(encryptedFieldId))
                                {
                                    return RedirectToAction(_recommendationsActionName, "Crop", new { q = model.EncryptedFarmId, r = encryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved) });
                                }
                            }
                        }
                        else
                        {
                            return Redirect(Url.Action(_harvestYearOverviewActionName, "Crop", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved) }) + Resource.lblInorganicFertiliserApplicationsForSorting);
                        }
                    }
                    else
                    {
                        if (model.FieldList != null && model.FieldList.Count > 0)
                        {
                            (List<CommonResponse> fieldList, Error fieldListError) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
                            if (fieldListError == null)
                            {
                                if (fieldList.Count > 0)
                                {
                                    var fieldNames = fieldList
                                                     .Where(field => model.FieldList.Contains(field.Id.ToString())).OrderBy(field => field.Name)
                                                     .Select(field => field.Name)
                                                     .ToList();

                                    if (fieldNames != null && fieldNames.Count == 1)
                                    {
                                        model.FieldName = fieldNames.FirstOrDefault();
                                    }
                                    else if (fieldNames != null)
                                    {
                                        model.FieldName = string.Empty;
                                        ViewBag.SelectedFields = fieldNames.OrderBy(name => name).ToList();
                                    }
                                }
                            }
                            else
                            {
                                TempData["RemoveFertiliserError"] = fieldListError.Message;
                            }
                        }
                        TempData["RemoveFertiliserError"] = error.Message;
                        return View(model);
                    }
                }
            }
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace("OrganicManure Controller : Exception in RemoveFertiliser() post action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["RemoveFertiliserError"] = ex.Message;
            return View(model);
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        _logger.LogTrace("Fertiliser Manure Controller : Cancel() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in Cancel() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Fertiliser Manure Controller : Exception in Cancel() action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["CheckYourAnswerError"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Cancel() post action called");
        if (model.IsCancel == null)
        {
            ModelState.AddModelError("IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            return View("Cancel", model);
        }
        if (model.IsCancel.HasValue && !model.IsCancel.Value)
        {
            return RedirectToAction(_checkAnswerActionName);
        }
        else
        {
            RemoveFertiliserManureSession();
            if (!model.IsComingFromRecommendation)
            {
                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            else
            {
                string fieldId = model.FieldList[0];
                return RedirectToAction(_recommendationsActionName, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(fieldId),
                    s = model.EncryptedHarvestYear

                });
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Defoliation(string q)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Defoliation({q}) action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        Error error = null;
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in Defoliation() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (string.IsNullOrWhiteSpace(q) && model != null && (model.DefoliationList == null || (model.DefoliationList != null && model.DefoliationList.Count == 0) || (model.IsAnyChangeInSameDefoliationFlag && model.DefoliationCurrentCounter == 0) || (model.IsAnyChangeInField || model.IsCropGroupChange)))
            {
                model.DefoliationCurrentCounter = 0;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                if (model.DefoliationList != null && model.DefoliationList.Count > 0)
                {
                    model.FieldID = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;
                    model.FieldName = model.DefoliationList[model.DefoliationCurrentCounter].FieldName;
                }
                else
                {
                    model.FieldID = model.FertiliserManures.Where(x => x.IsGrass && x.FieldID.HasValue).Select(x => x.FieldID.Value).First();
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
                }
                SetFertiliserManureToSession(model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && model != null && (model.FertiliserManures != null && model.FertiliserManures.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;
                if (itemCount == 0)
                {
                    model.DefoliationCurrentCounter = 0;
                    model.DefoliationEncryptedCounter = string.Empty;
                    SetFertiliserManureToSession(model);

                    if (model.GrassCropCount != null && model.GrassCropCount.Value > 1 && model.NeedToShowSameDefoliationForAll)
                    {
                        return RedirectToAction("IsSameDefoliationForAll");
                    }
                    if (model.IsDoubleCropAvailable || model.IsDoubleCropValueChange)
                    {
                        return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
                    }
                    if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
                    {
                        if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
                        {
                            string fieldId = model.FieldList[0];
                            return RedirectToAction(_recommendationsActionName, "Crop", new
                            {
                                q = model.EncryptedFarmId,
                                r = _fieldDataProtector.Protect(fieldId),
                                s = model.EncryptedHarvestYear

                            });
                        }
                    }
                    else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
                    {
                        return RedirectToAction(_fieldsActionName);
                    }
                    return RedirectToAction(_fieldGroupActionName);
                }
                if (model.IsCheckAnswer && model.IsDoubleCropAvailable && model.IsDoubleCropValueChange && (!model.NeedToShowSameDefoliationForAll))
                {
                    return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
                }
                model.FieldID = model.DefoliationList[index].FieldID;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DefoliationList[index].FieldID)).Name;
                model.DefoliationCurrentCounter = index;
                model.IsSameDefoliationForAll = model.IsSameDefoliationForAll ?? false;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                SetFertiliserManureToSession(model);
            }
            if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
            {
                if (model.DefoliationList != null && model.DefoliationList.Count > 0 && model.DefoliationCurrentCounter < model.DefoliationList.Count)
                {
                    model.FieldID = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;
                    model.FieldName = model.DefoliationList[model.DefoliationCurrentCounter].FieldName;
                }
                List<Crop> cropList = new List<Crop>();
                string cropTypeName = string.Empty;
                if (model.DefoliationList == null || model.IsAnyChangeInField ||
                (model.DefoliationList != null && model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).Any(fieldId => !model.DefoliationList.Select(d => d.FieldID).Contains(fieldId.Value))))
                {
                    if (model.DefoliationList == null)
                    {
                        model.DefoliationList = new List<DefoliationList>();
                    }

                    int counter = model.DefoliationList.Count + 1;

                    foreach (int? fieldId in model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID))
                    {
                        bool isFieldAlreadyPresent = model.DefoliationList.Any(dc => dc.FieldID == fieldId);
                        if (isFieldAlreadyPresent)
                        {
                            continue;
                        }

                        (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId.Value, model.HarvestYear.Value);
                        if (!string.IsNullOrWhiteSpace(error.Message))
                        {
                            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                            {
                                if (model.IsDoubleCropAvailable)
                                {
                                    TempData["DoubleCropError"] = error.Message;
                                    return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
                                }
                            }
                            else
                            {
                                TempData["CheckYourAnswerError"] = error.Message;
                                return RedirectToAction(_checkAnswerActionName);
                            }
                            TempData["FieldGroupError"] = error.Message;
                            return RedirectToAction(_fieldGroupActionName);
                        }

                        if (cropList.Count > 0)
                        {
                            var grassCrop = cropList.FirstOrDefault(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass);
                            int cropId = 0;
                            if (grassCrop != null && grassCrop.ID.HasValue)
                            {
                                cropId = grassCrop.ID.Value;
                            }


                            (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);
                            if (!string.IsNullOrWhiteSpace(error.Message))
                            {
                                if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
                                {
                                    if (model.IsDoubleCropAvailable)
                                    {
                                        TempData["DoubleCropError"] = error.Message;
                                        return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
                                    }
                                }
                                else
                                {
                                    TempData["CheckYourAnswerError"] = error.Message;
                                    return RedirectToAction(_checkAnswerActionName);
                                }

                                TempData["FieldGroupError"] = error.Message;
                                return RedirectToAction(_fieldGroupActionName);
                            }
                            if (managementPeriodList.Count > 0)
                            {
                                var field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);

                                var defoliationList = new DefoliationList
                                {
                                    CropID = cropId,
                                    ManagementPeriodID = managementPeriodList.FirstOrDefault().ID.Value,
                                    Defoliation = (model.DefoliationList != null && model.DefoliationList.Count > 0)
                                    ? model.DefoliationList
                                        .Where(x => managementPeriodList.Any(m => m.ID == x.ManagementPeriodID))
                                        .Select(x => x.Defoliation)
                                        .FirstOrDefault()
                                    : null,
                                    FieldID = fieldId.Value,
                                    FieldName = field?.Name,
                                    EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                                    Counter = counter,
                                };
                                model.DefoliationList.Add(defoliationList);
                                counter++;
                            }
                        }
                    }
                }
            }
            (List<SelectListItem> defoliationsList, error) = await GetDefoliationList(model);
            if (error == null && defoliationsList.Count > 0)
            {
                ViewBag.DefoliationList = defoliationsList.Select(f => new SelectListItem
                {
                    Value = f.Value,
                    Text = f.Text.ToString()
                }).ToList();
            }
            SetFertiliserManureToSession(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Fertiliser Controller : Exception in Defoliation() action : {0}, {1}", ex.Message, ex.StackTrace);
            if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
            {
                if (model.IsDoubleCropAvailable)
                {
                    TempData["DoubleCropError"] = ex.Message;
                    return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
                }
            }
            else
            {
                TempData["CheckYourAnswerError"] = ex.Message;
                return RedirectToAction(_checkAnswerActionName);
            }
            TempData["FieldGroupError"] = ex.Message;
            return RedirectToAction(_fieldGroupActionName);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Defoliation(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Defoliation() post action called");
        Error? error = null;
        try
        {
            if (model.DefoliationList[model.DefoliationCurrentCounter].Defoliation == null)
            {
                ModelState.AddModelError("DefoliationList[" + model.DefoliationCurrentCounter + "].Defoliation", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                (List<SelectListItem> defoliationList, error) = await GetDefoliationList(model);
                if (error == null && defoliationList.Count > 0)
                {
                    ViewBag.DefoliationList = defoliationList.Select(f => new SelectListItem
                    {
                        Value = f.Value,
                        Text = f.Text.ToString()
                    }).ToList();
                }
                else
                {
                    TempData["DefoliationError"] = error.Message;
                }
                return View(model);
            }


            if (!model.NeedToShowSameDefoliationForAll || (model.IsSameDefoliationForAll.HasValue && !model.IsSameDefoliationForAll.Value))
            {
                for (int i = 0; i < model.DefoliationList.Count; i++)
                {
                    if (model.FieldID == model.DefoliationList[i].FieldID)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(model.DefoliationList[i].CropID);
                        if (string.IsNullOrWhiteSpace(error.Message) && crop != null && crop.DefoliationSequenceID != null)
                        {
                            if (crop.DefoliationSequenceID != null && model.DefoliationList[i].Defoliation != null)
                            {
                                (string? selectedDefoliation, error) = await GetDefoliationName(model.DefoliationList[i].Defoliation.Value, crop.DefoliationSequenceID.Value);
                                if (error == null && !string.IsNullOrWhiteSpace(selectedDefoliation))
                                {
                                    model.DefoliationList[i].DefoliationName = selectedDefoliation;
                                    if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                                    {
                                        int index = model.FertiliserManures
                                        .FindIndex(f => f.IsGrass && f.FieldID == crop.FieldID);

                                        if (index >= 0)
                                        {
                                            model.FertiliserManures[index].Defoliation = model.DefoliationList[model.DefoliationCurrentCounter].Defoliation;
                                            model.FertiliserManures[index].DefoliationName = selectedDefoliation;
                                        }
                                    }
                                }
                            }
                            (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
                            if (managementPeriodList != null)
                            {
                                if (model.IsCheckAnswer && (!string.IsNullOrWhiteSpace(model.EncryptedFertId)))
                                {
                                    int filteredManId = managementPeriodList
                                 .Where(fm => model.UpdatedFertiliserIds.Any(mp => mp.ManagementPeriodId == fm.ID))
                                 .Select(x => x.ID.Value)
                                 .FirstOrDefault();

                                    if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                                    {
                                        foreach (var item in model.UpdatedFertiliserIds)
                                        {
                                            if (item.ManagementPeriodId == filteredManId)
                                            {
                                                item.ManagementPeriodId = managementPeriodList.Where(x => x.Defoliation == model.DefoliationList[i].Defoliation).Select(x => x.ID.Value).First();
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                                {
                                    int index = model.FertiliserManures
                                    .FindIndex(f => f.IsGrass && f.FieldID == crop.FieldID);

                                    if (index >= 0)
                                    {
                                        model.FertiliserManures[index].ManagementPeriodID = managementPeriodList.Where(x => x.Defoliation == model.DefoliationList[i].Defoliation).Select(x => x.ID.Value).First();
                                    }
                                }
                            }
                        }

                        model.DefoliationCurrentCounter++;

                        if (i + 1 < model.DefoliationList.Count)
                        {
                            model.FieldID = model.DefoliationList[i + 1].FieldID;
                            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
                        }
                        break;
                    }
                }
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                SetFertiliserManureToSession(model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInSameDefoliationFlag) && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            else if (model.IsSameDefoliationForAll.HasValue && (model.IsSameDefoliationForAll.Value))
            {
                model.DefoliationCurrentCounter = 1;
                for (int i = 0; i < model.DefoliationList.Count; i++)
                {
                    (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(model.DefoliationList[i].ManagementPeriodID);
                    if (string.IsNullOrWhiteSpace(error.Message) && managementPeriod != null)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                        if (string.IsNullOrWhiteSpace(error.Message) && crop != null && crop.DefoliationSequenceID != null)
                        {
                            int fieldId = crop.FieldID.Value;
                            (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(managementPeriod.CropID.Value, false);

                            if (managementPeriodList.Count > 0)
                            {
                                if (model.IsCheckAnswer && (!string.IsNullOrWhiteSpace(model.EncryptedFertId)))
                                {
                                    int filteredManId = managementPeriodList
                                 .Where(fm => model.UpdatedFertiliserIds.Any(mp => mp.ManagementPeriodId == fm.ID))
                                 .Select(x => x.ID.Value)
                                 .FirstOrDefault();

                                    if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
                                    {
                                        foreach (var item in model.UpdatedFertiliserIds)
                                        {
                                            if (item.ManagementPeriodId == filteredManId)
                                            {
                                                item.ManagementPeriodId = managementPeriodList.Where(x => x.Defoliation == model.DefoliationList[0].Defoliation).Select(x => x.ID.Value).First();
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                                {
                                    int index = model.FertiliserManures
                                    .FindIndex(f => f.IsGrass && f.FieldID == fieldId);

                                    if (index >= 0)
                                    {
                                        model.FertiliserManures[index].ManagementPeriodID = managementPeriodList.Where(x => x.Defoliation == model.DefoliationList[0].Defoliation).Select(x => x.ID.Value).First();
                                    }
                                }
                            }
                            if (crop.DefoliationSequenceID != null && model.DefoliationList[0].Defoliation != null)
                            {
                                (string? selectedDefoliation, error) = await GetDefoliationName(model.DefoliationList[0].Defoliation.Value, crop.DefoliationSequenceID.Value);
                                if (error == null && !string.IsNullOrWhiteSpace(selectedDefoliation))
                                {
                                    model.DefoliationList[i].DefoliationName = selectedDefoliation;
                                    model.DefoliationList[i].Defoliation = model.DefoliationList[0].Defoliation;
                                    if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                                    {
                                        int index = model.FertiliserManures
                                        .FindIndex(f => f.IsGrass && f.FieldID == fieldId);

                                        if (index >= 0)
                                        {
                                            model.FertiliserManures[index].Defoliation = model.DefoliationList[0].Defoliation;
                                            model.FertiliserManures[index].DefoliationName = selectedDefoliation;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());

                SetFertiliserManureToSession(model);
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction("InOrgnaicManureDuration");
            }
            model.GrassCropCount = model.DefoliationList.Count;
            SetFertiliserManureToSession(model);
            if (model.DefoliationCurrentCounter == model.DefoliationList.Count)
            {
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction("InOrgnaicManureDuration");
            }
            else
            {
                (List<SelectListItem> defoliationList, error) = await GetDefoliationList(model);
                if (error == null && defoliationList.Count > 0)
                {
                    ViewBag.DefoliationList = defoliationList.Select(f => new SelectListItem
                    {
                        Value = f.Value,
                        Text = f.Text.ToString()
                    }).ToList();
                }
                else
                {
                    TempData["DefoliationError"] = error.Message;
                }
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Fertiliser Controller : Exception in Defoliation() post action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["DefoliationError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult BackActionForDefoliation()
    {
        _logger.LogTrace("Fertiliser Manure Controller : BackActionForDefoliation() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in BackActionForDefoliation() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        if (model.IsCheckAnswer && (!model.IsAnyChangeInSameDefoliationFlag) && (!model.IsAnyChangeInField))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        SetFertiliserManureToSession(model);
        if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
        {
            return RedirectToAction("IsSameDefoliationForAll");
        }

        if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
        {
            if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
            {
                string fieldId = model.FieldList[0];
                return RedirectToAction(_recommendationsActionName, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(fieldId),
                    s = model.EncryptedHarvestYear

                });
            }
        }
        else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
        {
            return RedirectToAction(_fieldsActionName);
        }
        return RedirectToAction(_fieldGroupActionName);
    }

    [HttpGet]
    public async Task<IActionResult> IsSameDefoliationForAll()
    {
        _logger.LogTrace($"Fertiliser Controller : IsSameDefoliationForAll() action called");
        Error error = new Error();

        FertiliserManureViewModel model = GetFertiliserManureFromSession();
        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in IsSameDefoliationForAll() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        if (model.IsAnyChangeInSameDefoliationFlag)
        {
            model.IsAnyChangeInSameDefoliationFlag = false;
        }
        List<List<SelectListItem>> allDefoliations = new List<List<SelectListItem>>();
        List<FertiliserManureDataViewModel> fertiliserGrassList = model.FertiliserManures.Where(x => x.IsGrass).ToList();
        foreach (var fertiliser in fertiliserGrassList)
        {
            (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fertiliser.FieldID), model.HarvestYear.Value);
            if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                var cropId = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.ID.Value).FirstOrDefault();
                int? defoliationSequenceID = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.DefoliationSequenceID).FirstOrDefault();
                (List<ManagementPeriod> ManagementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);

                if (ManagementPeriod != null)
                {
                    List<int> defoliationList = ManagementPeriod.Select(x => x.Defoliation.Value).ToList();
                    List<SelectListItem> defoliationSelectList = new List<SelectListItem>();
                    (Crop crop, error) = await _cropLogic.FetchCropById(cropId);
                    if (string.IsNullOrWhiteSpace(error.Message) && defoliationSequenceID != null)
                    {
                        (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                        if (error == null && defoliationSequence != null)
                        {
                            string description = defoliationSequence.DefoliationSequenceDescription;
                            string[] defoliationParts = description.Split(',')
                                                                    .Select(x => x.Trim())
                                                                    .ToArray();
                            List<SelectListItem> allDefoliationWithName = new List<SelectListItem>();
                            foreach (int defoliation in defoliationList)
                            {
                                string text = (defoliation > 0 && defoliation <= defoliationParts.Length)
                                ? $"{Enum.GetName(typeof(PotentialCut), defoliation)} - {defoliationParts[defoliation - 1]}"
                                : defoliation.ToString();

                                allDefoliationWithName.Add(new SelectListItem
                                {
                                    Text = text,
                                    Value = defoliation.ToString()
                                });
                            }
                            allDefoliations.Add(allDefoliationWithName);
                        }
                    }
                }
            }
        }


        if (allDefoliations.Count > 0)
        {
            List<List<string>> defoliationSequenceList = allDefoliations
        .Select(list => list.Select(item => item.Text).ToList())
        .ToList();

            if (defoliationSequenceList.Count > 0)
            {
                List<string> commonDefoliations = defoliationSequenceList.Count > 0
                ? defoliationSequenceList.Aggregate((prev, next) => prev.Intersect(next).ToList())
                : new List<string>();
                if (commonDefoliations.Count > 0)
                {
                    List<SelectListItem> flattenedList = allDefoliations.SelectMany(list => list).ToList();

                    if (flattenedList.Count > 0)
                    {
                        List<SelectListItem> commonDefoliationItems = flattenedList
                        .Where(item => commonDefoliations.Contains(item.Text))
                        .GroupBy(item => item.Text)
                        .Select(g => g.First())
                        .ToList();
                        model.NeedToShowSameDefoliationForAll = true;
                    }
                }
                else
                {
                    if (model.IsCheckAnswer && model.IsDoubleCropValueChange && (model.DefoliationList != null && model.FertiliserManures
                    .Where(x => x.IsGrass).Select(x => x.FieldID).Any(fieldId => !model.DefoliationList.Select(d => d.FieldID)
                    .Contains(fieldId.Value))))
                    {
                        var defoIds = model.DefoliationList
                        .Select(d => d.FieldID)
                        .ToList();

                        if (defoIds != null)
                        {
                            model.FieldID = model.FertiliserManures
                                .Where(x => x.IsGrass)
                                .Select(x => x.FieldID)
                                .FirstOrDefault(fid => !defoIds.Contains(fid.Value));
                            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
                        }
                        model.DefoliationCurrentCounter = model.DefoliationList.Count;
                        model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                    }
                    model.IsSameDefoliationForAll = false;
                    model.NeedToShowSameDefoliationForAll = false;
                    SetFertiliserManureToSession(model);
                    return RedirectToAction(_defoliationActionName);
                }
            }
        }
        SetFertiliserManureToSession(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult IsSameDefoliationForAll(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Controller : IsSameDefoliationForAll() post action called");
        if (model.IsSameDefoliationForAll == null)
        {
            ModelState.AddModelError("IsSameDefoliationForAll", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            model.DefoliationCurrentCounter = 0;
            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            FertiliserManureViewModel? fertiliserManureViewModel = GetFertiliserManureFromSession();
            if (fertiliserManureViewModel == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in IsSameDefoliationForAll() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            if (fertiliserManureViewModel != null && model.IsSameDefoliationForAll != fertiliserManureViewModel.IsSameDefoliationForAll)
            {
                model.IsAnyChangeInSameDefoliationFlag = true;
            }
            else
            {
                model.IsAnyChangeInSameDefoliationFlag = false;
            }

            if (model.IsAnyChangeInSameDefoliationFlag)
            {
                foreach (var fertliser in model.FertiliserManures)
                {
                    fertliser.Defoliation = null;
                    fertliser.DefoliationName = null;
                }
            }
            SetFertiliserManureToSession(model);
            if (!model.IsAnyChangeInSameDefoliationFlag && model.IsCheckAnswer && (!model.IsAnyChangeInField))
            {
                return RedirectToAction(_checkAnswerActionName);
            }
        }
        catch (Exception ex)
        {
            TempData["IsSameDefoliationForAllError"] = ex.Message;
            return View(model);
        }
        return RedirectToAction(_defoliationActionName);
    }

    [HttpGet]
    public async Task<IActionResult> DoubleCrop(string q)
    {
        _logger.LogTrace("Fertiliser Manure Controller : DoubleCrop({0}) action called", q);
        FertiliserManureViewModel model = GetFertiliserManureFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in DoubleCrop() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (string.IsNullOrWhiteSpace(q) && model.FertiliserManures != null && model.FertiliserManures.Count > 0
  && (model.IsAnyChangeInField || model.IsCropGroupChange))
            {
                model.DoubleCropCurrentCounter = 0;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
                SetFertiliserManureToSession(model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.DoubleCrop != null && model.DoubleCrop.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;
                if (itemCount == 0)
                {
                    model.DoubleCropCurrentCounter = 0;
                    model.DoubleCropEncryptedCounter = string.Empty;
                    SetFertiliserManureToSession(model);
                    if (model.IsCheckAnswer && (!model.IsAnyChangeInSameDefoliationFlag) && (!model.IsAnyChangeInField))
                    {
                        return RedirectToAction(_checkAnswerActionName);
                    }
                    else if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
                    {
                        if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
                        {
                            string fieldId = model.FieldList[0];
                            return RedirectToAction(_recommendationsActionName, "Crop", new
                            {
                                q = model.EncryptedFarmId,
                                r = _fieldDataProtector.Protect(fieldId),
                                s = model.EncryptedHarvestYear

                            });
                        }
                    }
                    else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
                    {
                        return RedirectToAction(_fieldsActionName);
                    }
                    return RedirectToAction(_fieldGroupActionName);
                }
                model.FieldID = model.DoubleCrop[index].FieldID;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[index].FieldID)).Name;
                model.DoubleCropCurrentCounter = index;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
            }
            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                if (model.DoubleCrop != null && model.DoubleCrop.Count > 0 && model.DoubleCropCurrentCounter < model.DoubleCrop.Count)
                {
                    model.FieldID = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID;
                    model.FieldName = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldName;
                }
                List<Crop> cropList = new List<Crop>();
                string cropTypeName = string.Empty;
                Error error = new Error();
                if (model.DoubleCrop == null || model.IsAnyChangeInField)
                {
                    if (model.DoubleCrop == null)
                    {
                        model.DoubleCrop = new List<DoubleCrop>();
                    }

                    int counter = model.DoubleCrop.Count + 1;
                    foreach (string fieldIdStr in model.FieldList)
                    {
                        int fieldId = Convert.ToInt32(fieldIdStr);
                        bool isFieldAlreadyPresent = model.DoubleCrop.Any(dc => dc.FieldID == fieldId);
                        if (model.IsAnyChangeInField && isFieldAlreadyPresent)
                        {
                            continue;
                        }

                        (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId, model.HarvestYear.Value);
                        if (cropList != null && cropList.Count == 2)
                        {
                            var cropTypeId = cropList.FirstOrDefault()?.CropTypeID;
                            if (cropTypeId.HasValue)
                            {
                                cropTypeName = await _fieldLogic.FetchCropTypeById(cropTypeId.Value);
                                var field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                                var doubleCrop = new DoubleCrop
                                {
                                    CropName = cropTypeName,
                                    CropOrder = cropList.FirstOrDefault().CropOrder ?? 1,
                                    FieldID = fieldId,
                                    FieldName = field?.Name,
                                    EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                                    Counter = counter,
                                };

                                model.DoubleCrop.Add(doubleCrop);
                                counter++;
                            }
                        }
                    }

                }
                if (model.DoubleCrop != null && model.DoubleCrop.Count > 0 &&
                model.DoubleCrop.Any(dc => !model.FieldList.Contains(dc.FieldID.ToString())))
                {
                    model.DoubleCrop?.RemoveAll(dc => !model.FieldList.Contains(dc.FieldID.ToString()));
                }
                (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID), model.HarvestYear.Value);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
                    {
                        if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
                        {
                            TempData["NutrientRecommendationsError"] = error.Message;
                            string fieldId = model.FieldList[0];
                            return RedirectToAction(_recommendationsActionName, "Crop", new
                            {
                                q = model.EncryptedFarmId,
                                r = _fieldDataProtector.Protect(fieldId),
                                s = model.EncryptedHarvestYear

                            });
                        }
                    }
                    else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
                    {
                        TempData["FieldError"] = error.Message;
                        return RedirectToAction(_fieldsActionName);
                    }
                    TempData["FieldGroupError"] = error.Message;
                    return RedirectToAction(_fieldGroupActionName);
                }
                if (cropList != null && cropList.Count == 2)
                {
                    var cropOptions = new List<SelectListItem>();
                    foreach (var crop in cropList.OrderBy(x => x.CropOrder))
                    {
                        cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                        cropOptions.Add(new SelectListItem
                        {
                            Text = $"{Resource.lblCrop} {crop.CropOrder} : {cropTypeName}",
                            Value = crop.ID.ToString()
                        });
                    }

                    SetFertiliserManureToSession(model);
                    ViewBag.DoubleCropOptions = cropOptions;
                }
                if (model.DoubleCropCurrentCounter == 0)
                {
                    model.FieldID = model.DoubleCrop[0].FieldID;
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[0].FieldID)).Name;
                }
            }

            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
            {
                if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
                {
                    TempData["NutrientRecommendationsError"] = ex.Message;
                    string fieldId = model.FieldList[0];
                    return RedirectToAction(_recommendationsActionName, "Crop", new
                    {
                        q = model.EncryptedFarmId,
                        r = _fieldDataProtector.Protect(fieldId),
                        s = model.EncryptedHarvestYear
                    });
                }
            }
            else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
            {
                TempData["FieldError"] = ex.Message;
                return RedirectToAction(_fieldsActionName);
            }
            TempData["FieldGroupError"] = ex.Message;
            return RedirectToAction(_fieldGroupActionName);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoubleCrop(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : DoubleCrop() post action called");
        if (model.DoubleCrop[model.DoubleCropCurrentCounter]?.CropID == null || model.DoubleCrop[model.DoubleCropCurrentCounter].CropID == 0)
        {
            ModelState.AddModelError("DoubleCrop[" + model.DoubleCropCurrentCounter + "].CropID", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        Error error = new Error();
        try
        {
            if (!ModelState.IsValid)
            {
                if (model.FieldList != null && model.FieldList.Count > 0)
                {
                    (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID), model.HarvestYear.Value);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["DoubleCropError"] = error.Message;
                    }
                    if (model.DoubleCrop == null)
                    {
                        model.DoubleCrop = new List<DoubleCrop>();
                    }
                    if (cropList != null && cropList.Count == 2)
                    {
                        var cropOptions = new List<SelectListItem>();
                        foreach (var crop in cropList.OrderBy(x => x.CropOrder))
                        {
                            string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                            cropOptions.Add(new SelectListItem
                            {
                                Text = $"{Resource.lblCrop} {crop.CropOrder} : {cropTypeName}",
                                Value = crop.ID.ToString()
                            });
                        }
                        SetFertiliserManureToSession(model);
                        ViewBag.DoubleCropOptions = cropOptions;
                    }
                }
                return View(model);
            }


            FertiliserManureViewModel fertiliserManureViewModel = GetFertiliserManureFromSession() ?? new FertiliserManureViewModel();

            if (model.DoubleCrop.Any(x => x.FieldID == model.FieldID))
            {
                List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(model.FieldID.Value);
                cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();
                if (cropList != null && cropList.Count == 2)
                {
                    cropList = cropList.Where(x => x.ID == model.DoubleCrop[model.DoubleCropCurrentCounter].CropID).ToList();
                    if (cropList.Count > 0)
                    {
                        model.DoubleCrop[model.DoubleCropCurrentCounter].CropOrder = cropList.Select(x => x.CropOrder.Value).FirstOrDefault();
                        model.DoubleCrop[model.DoubleCropCurrentCounter].CropName = await _fieldLogic.FetchCropTypeById(Convert.ToInt32(cropList.Select(x => x.CropTypeID.Value).FirstOrDefault()));
                    }
                }
            }
            if (model.DoubleCrop.Count > 0)
            {
                (List<ManagementPeriod> managementPeriods, error) = await _cropLogic.FetchManagementperiodByCropId(model.DoubleCrop[model.DoubleCropCurrentCounter].CropID, true);
                if (string.IsNullOrWhiteSpace(error.Message) && managementPeriods != null && managementPeriods.Count > 0)
                {
                    foreach (var fertiliser in model.FertiliserManures)
                    {
                        if (fertiliser.FieldID == model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID)
                        {
                            if (model.IsCheckAnswer && (!string.IsNullOrWhiteSpace(model.EncryptedFertId)))
                            {

                                if (model.UpdatedFertiliserIds != null)
                                {
                                    foreach (var updatedFertIds in model.UpdatedFertiliserIds)
                                    {
                                        if (fertiliser.FieldName.Equals(updatedFertIds.Name))
                                        {
                                            updatedFertIds.ManagementPeriodId = managementPeriods.Select(x => x.ID.Value).FirstOrDefault();
                                            break;
                                        }
                                    }
                                }
                            }

                            fertiliser.ManagementPeriodID = managementPeriods.Select(x => x.ID.Value).FirstOrDefault();
                            break;
                        }
                    }
                }
            }
            (Crop cropData, error) = await _cropLogic.FetchCropById(model.DoubleCrop[model.DoubleCropCurrentCounter].CropID);
            if (string.IsNullOrWhiteSpace(error.Message) && cropData != null && cropData.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass &&
                model.DefoliationList != null && model.DefoliationList.Any(x => x.FieldID == model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID))
            {
                int fieldIdToRemove = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID;
                model.DefoliationList.RemoveAll(x => x.FieldID == fieldIdToRemove);
            }
            for (int i = 0; i < model.DoubleCrop.Count; i++)
            {
                if (model.FieldID == model.DoubleCrop[i].FieldID)
                {
                    model.DoubleCropCurrentCounter++;
                    if (i + 1 < model.DoubleCrop.Count)
                    {
                        model.FieldID = model.DoubleCrop[i + 1].FieldID;
                        model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
                    }

                    break;
                }
            }
            model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
            SetFertiliserManureToSession(model);


            if (model.IsCheckAnswer || model.DoubleCrop.Count == model.DoubleCropCurrentCounter)
            {

                int counter = 0;
                foreach (var doubleCrop in model.DoubleCrop)
                {
                    if (doubleCrop.CropID > 0)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(doubleCrop.CropID);
                        if (string.IsNullOrWhiteSpace(error.Message) && crop != null)
                        {
                            if (model.FertiliserManures != null && model.FertiliserManures.Count > 0)
                            {
                                int index = model.FertiliserManures
                                .FindIndex(f => f.FieldID == crop.FieldID);
                                if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && index >= 0)
                                {
                                    model.FertiliserManures[index].IsGrass = true;
                                    counter++;
                                    model.IsAnyCropIsGrass = true;
                                }
                                else if (model.FertiliserManures.Any(f => f.IsGrass && f.FieldID == crop.FieldID))
                                {
                                    model.FertiliserManures[index].IsGrass = false;
                                    model.FertiliserManures[index].Defoliation = null;
                                    model.FertiliserManures[index].DefoliationName = null;
                                }
                            }
                        }
                    }
                }

                if (model.FertiliserManures != null && !model.FertiliserManures.Any(x => x.IsGrass))
                {
                    model.IsAnyCropIsGrass = false;
                }

                model.GrassCropCount = model.FertiliserManures != null ? model.FertiliserManures.Count(x => x.IsGrass) : counter;
                if (model.IsCheckAnswer && fertiliserManureViewModel != null && fertiliserManureViewModel?.DoubleCrop != null && model?.DoubleCrop != null)
                {
                    int grassCount = model.FertiliserManures.Count(x => x.IsGrass);
                    if (model.DoubleCropCurrentCounter - 1 < model.DoubleCrop.Count && model.DefoliationList != null && grassCount != model.DefoliationList.Count())
                    {
                        model.FieldID = model.DoubleCrop[model.DoubleCropCurrentCounter - 1].FieldID;
                        model.FieldName = model.DoubleCrop[model.DoubleCropCurrentCounter - 1].FieldName;
                    }
                    var newItem = model.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID.Value);
                    var oldItem = fertiliserManureViewModel.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID.Value);
                    if (newItem != null)
                    {
                        if (newItem.CropOrder != oldItem.CropOrder)
                        {
                            model.IsDoubleCropValueChange = true;
                        }
                    }
                }
            }

            if (model.DoubleCropCurrentCounter == model.DoubleCrop.Count || (!model.IsAnyChangeInField && model.IsCheckAnswer))
            {
                if (model.IsCheckAnswer && (model.IsAnyCropIsGrass.HasValue && !model.IsAnyCropIsGrass.Value) && (!model.IsAnyChangeInField))
                {
                    SetFertiliserManureToSession(model);
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    SetFertiliserManureToSession(model);
                    if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (model.DefoliationList != null && model.FertiliserManures
                    .Where(x => x.IsGrass).Select(x => x.FieldID).All(fieldId => model.DefoliationList.Select(d => d.FieldID)
                    .Contains(fieldId.Value))))
                    {
                        model.IsAnyChangeInSameDefoliationFlag = false;
                        SetFertiliserManureToSession(model);
                        return RedirectToAction(_checkAnswerActionName);
                    }
                    else
                    {
                        if (model.IsAnyCropIsGrass == null || (model.IsAnyCropIsGrass.HasValue && !model.IsAnyCropIsGrass.Value))
                        {
                            SetFertiliserManureToSession(model);
                            return RedirectToAction("InOrgnaicManureDuration");
                        }

                        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
                        {
                            if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
                            {
                                if (model.FertiliserManures.Any(z => z.IsGrass && z.Defoliation == null))
                                {
                                    model.IsSameDefoliationForAll = null;
                                }
                                SetFertiliserManureToSession(model);
                                return RedirectToAction("IsSameDefoliationForAll");
                            }
                            model.IsSameDefoliationForAll = true;
                            SetFertiliserManureToSession(model);
                            return RedirectToAction(_defoliationActionName);
                        }
                    }

                    SetFertiliserManureToSession(model);
                    return RedirectToAction("InOrgnaicManureDuration");
                }
            }
            else
            {
                (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(model.FieldID.Value, model.HarvestYear.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && cropList.Count == 2)
                {
                    var cropOptions = new List<SelectListItem>();
                    foreach (var crop in cropList.OrderBy(x => x.CropOrder))
                    {
                        string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                        cropOptions.Add(new SelectListItem
                        {
                            Text = $"{Resource.lblCrop} {crop.CropOrder} : {cropTypeName}",
                            Value = crop.ID.ToString()
                        });
                    }

                    SetFertiliserManureToSession(model);
                    ViewBag.DoubleCropOptions = cropOptions;
                }
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["DoubleCropError"] = ex.Message;
            return View(model);
        }
    }

    private async Task<List<WarningMessage>> GetWarningMessages(FertiliserManureViewModel model)
    {
        List<WarningMessage> warningMessages = new List<WarningMessage>();
        try
        {
            if (model != null && model.N > 0 && model.FertiliserManures != null && model.FertiliserManures.Count > 0)
            {
                foreach (var fertiliserManure in model.FertiliserManures)
                {
                    (ManagementPeriod managementPeriod, Error error) = await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);
                    if (!string.IsNullOrWhiteSpace(model.ClosedPeriodWarningPara1))
                    {
                        WarningMessage warningMessage = new WarningMessage();
                        warningMessage.FieldID = fertiliserManure.FieldID ?? 0;
                        warningMessage.CropID = managementPeriod.CropID ?? 0;
                        warningMessage.JoiningID = null;
                        warningMessage.WarningLevelID = model.ClosedPeriodWarningLevelID;
                        warningMessage.WarningCodeID = model.ClosedPeriodWarningCodeID;
                        warningMessage.Header = model.ClosedPeriodWarningHeader;
                        warningMessage.Para1 = model.ClosedPeriodWarningPara1;
                        warningMessage.Para2 = null;
                        warningMessage.Para3 = model.ClosedPeriodWarningPara3;
                        warningMessages.Add(warningMessage);
                    }

                    if (model.IsNitrogenExceedWarning)
                    {
                        WarningMessage warningMessage = new WarningMessage();
                        warningMessage.FieldID = fertiliserManure.FieldID ?? 0;
                        warningMessage.CropID = managementPeriod.CropID ?? 0;
                        warningMessage.JoiningID = null;
                        warningMessage.WarningLevelID = model.ClosedPeriodNitrogenExceedWarningLevelID;
                        warningMessage.WarningCodeID = model.ClosedPeriodNitrogenExceedWarningCodeID;
                        warningMessage.Header = model.ClosedPeriodNitrogenExceedWarningHeader;
                        warningMessage.Para1 = model.ClosedPeriodNitrogenExceedWarningPara1;
                        warningMessage.Para2 = model.ClosedPeriodNitrogenExceedWarningPara2;
                        warningMessage.Para3 = model.ClosedPeriodNitrogenExceedWarningPara3;
                        warningMessages.Add(warningMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "OrganicManure Controller : Exception in GetWarningMessages() method : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
        }
        return warningMessages;
    }

    private async Task<(List<SelectListItem>, Error?)> GetDefoliationList(FertiliserManureViewModel model)
    {
        if (model.IsSameDefoliationForAll == true)
            return await GetDefoliationListForAll(model);

        return await GetDefoliationListSingleMode(model);
    }

    private async Task<(List<SelectListItem>, Error?)> GetDefoliationListForAll(FertiliserManureViewModel model)
    {
        var defoliationGroups = new List<List<SelectListItem>>();
        var grassFertilisers = model.FertiliserManures.Where(x => x.IsGrass).ToList();

        foreach (var fertiliser in grassFertilisers)
        {
            var (list, error) = await GetFieldDefoliationList(model, fertiliser.FieldID);
            if (error != null)
            {
                return (new List<SelectListItem>(), error);
            }
            if (list.Any())
            {
                defoliationGroups.Add(list);
            }
        }

        if (!defoliationGroups.Any())
        {
            return (new List<SelectListItem>(), null);
        }

        var commonItems = Functions.GetCommonDefoliations(defoliationGroups);
        var normalized = Functions.NormalizeDefoliationText(commonItems);

        ViewBag.DefoliationList = normalized;
        return (normalized, null);
    }

    private async Task<(List<SelectListItem>, Error?)> GetDefoliationListSingleMode(FertiliserManureViewModel model)
    {
        if (model.DefoliationCurrentCounter < 0)
        {
            return (new List<SelectListItem>(), null);
        }

        int fieldId = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;
        var (list, error) = await GetFieldDefoliationList(model, fieldId);

        if (error != null)
        {
            return (new List<SelectListItem>(), error);
        }

        var normalized = Functions.NormalizeDefoliationText(list);
        ViewBag.DefoliationList = normalized;
        return (normalized, null);
    }

    private async Task<(List<SelectListItem>, Error?)> GetFieldDefoliationList(FertiliserManureViewModel model, int? fieldId)
    {
        var empty = new List<SelectListItem>();
        if (!fieldId.HasValue) return (empty, null);

        var (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(
            fieldId.Value, model.HarvestYear!.Value);

        if (HasErrorOrNoGrass(cropList, error))
            return (empty, error);

        var grassCrop = cropList.First(x => x.CropTypeID == (int)CropTypes.Grass);
        if (!grassCrop.DefoliationSequenceID.HasValue) return (empty, null);

        return await BuildDefoliationSelectList(grassCrop);
    }

    private static bool HasErrorOrNoGrass(List<Crop> crops, Error? error)
    {
        return !string.IsNullOrWhiteSpace(error?.Message)
            || crops == null
            || !crops.Any(x => x.CropTypeID == (int)CropTypes.Grass);
    }

    private async Task<(List<SelectListItem>, Error?)> BuildDefoliationSelectList(Crop crop)
    {
        var empty = new List<SelectListItem>();

        var (mgmtList, error) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
        if (mgmtList == null) return (empty, error);

        var defoliationNumbers = mgmtList.Select(x => x.Defoliation.Value).ToList();

        var (sequence, errorSeq) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);

        if (sequence == null) return (empty, errorSeq);

        var parts = sequence.DefoliationSequenceDescription.Split(',')
                    .Select(x => x.Trim()).ToArray();

        var list = defoliationNumbers.Select(num => new SelectListItem
        {
            Text = GetDefoliationLabel(num, parts),
            Value = num.ToString()
        }).ToList();

        return (list, null);
    }

    private static string GetDefoliationLabel(int num, string[] parts)
    {
        return (num > 0 && num <= parts.Length)
            ? $"{Enum.GetName(typeof(PotentialCut), num)} - {parts[num - 1]}"
            : num.ToString();
    }

    private async Task<(string?, Error?)> GetDefoliationName(int defoliation, int defoliationSequenceID)
    {
        string selectedDefoliation = string.Empty;
        Error error = null;
        (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(defoliationSequenceID);
        if (error == null && defoliationSequence != null)
        {
            string description = defoliationSequence.DefoliationSequenceDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                string[] defoliationParts = description.Split(',').Select(x => x.Trim()).ToArray();
                selectedDefoliation = (defoliation > 0 && defoliation <= defoliationParts.Length)
                                     ? $"{Enum.GetName(typeof(PotentialCut), defoliation)} -{defoliationParts[defoliation - 1]}"
                                     : $"{defoliation}";
                var parts = selectedDefoliation.Split('-');
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();

                    if (!string.IsNullOrWhiteSpace(right))
                    {
                        right = char.ToUpper(right[0]) + right.Substring(1);
                    }

                    selectedDefoliation = $"{left} - {right}";
                }
            }
        }
        return (selectedDefoliation, error);
    }
}
