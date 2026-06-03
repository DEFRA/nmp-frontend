using Fluid.Parser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
namespace NMP.Portal.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FertiliserManureController(ILogger<FertiliserManureController> logger, IDataProtectionProvider dataProtectionProvider,
    IFertiliserManureLogicDependencies logicDependencies) : Controller
{
    private readonly ILogger<FertiliserManureController> _logger = logger;
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
    private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
    private readonly IFarmLogic _farmLogic = logicDependencies.FarmLogic;
    private readonly IFertiliserManureLogic _fertiliserManureLogic = logicDependencies.FertiliserManureLogic;
    private readonly ICropLogic _cropLogic = logicDependencies.CropLogic;
    private readonly IFieldLogic _fieldLogic = logicDependencies.FieldLogic;
    private readonly IOrganicManureLogic _organicManureLogic = logicDependencies.OrganicManureLogic;
    private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
    private readonly IWarningLogic _warningLogic = logicDependencies.WarningLogic;
    private const string _fertiliserManureSessionKey = "FertiliserManure";
    private const string _harvestYearOverviewActionName = "HarvestYearOverview";
    private const string _checkAnswerActionName = "CheckAnswer";
    private const string _defoliationActionName = "Defoliation";
    private const string _doubleCropActionName = "DoubleCrop";
    private const string _recommendationsActionName = "Recommendations";
    private const string _fieldsActionName = "Fields";
    private const string _fieldGroupActionName = "FieldGroup";
    private const string _fertiliserManureBeforeUpdateSessionKey = "FertiliserManureBeforeUpdate";  //FieldGroupError
    private const string _fieldGroupErrorTempDataKey = "FieldGroupError";
    private const string _fieldErrorTempDataKey = "FieldError";
    private const string _inOrgnaicManureDurationErrorTempDataKey = "InOrgnaicManureDurationError";
    private const string _checkYourAnswerErrorDataKey = "CheckYourAnswerError";
    private const string _pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
    private const string _inOrgnaicManureDurationActionName = "InOrgnaicManureDuration";
    private const string _errorOnHarvestYearOverview = "ErrorOnHarvestYearOverview";
    private const string _nutrientRecommendationsError = "NutrientRecommendationsError";
    private const string _isSameDefoliationForAllActionName = "IsSameDefoliationForAll";  //NutrientValuesError
    private const string _nutrientValuesActionName = "NutrientValues";
    private const string _nutrientValuesError = "NutrientValuesError";
    private const string _twoParamStringFormat = "{0} {1}";

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

        return BackActionForInOrganicAndDoubleCrop(model);

    }

    private IActionResult BackActionForInOrganicAndDoubleCrop(FertiliserManureViewModel model)
    {
        if (model == null)
            return RedirectToAction(_fieldGroupActionName);

        if (model.FieldGroup == Resource.lblSelectSpecificFields)
        {
            if (model.IsComingFromRecommendation &&
                model.FieldList?.Count == 1)
            {
                var fieldId = model.FieldList[0];

                return RedirectToAction(_recommendationsActionName, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(fieldId),
                    s = model.EncryptedHarvestYear
                });
            }

            if (!model.IsComingFromRecommendation)
            {
                return RedirectToAction(_fieldsActionName);
            }
        }

        return RedirectToAction(_fieldGroupActionName);
    }
    private IActionResult RedirectForFieldGroupGet(FertiliserManureViewModel? model)
    {
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
        return RedirectToAction(_inOrgnaicManureDurationActionName);
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
                (model, error) = await BindFarmData(model, error);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_errorOnHarvestYearOverview] = error.Message;
                    ClearTempErrors(_fieldGroupErrorTempDataKey, _fieldErrorTempDataKey);
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

                    (error, model) = await BindFertiliserListForFieldGroup(model, fieldId);
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        TempData[_nutrientRecommendationsError] = error.Message;
                        ClearTempErrors(_fieldGroupErrorTempDataKey, _fieldErrorTempDataKey);
                        return RedirectToAction(_recommendationsActionName, "Crop", new { q = q, r = s, s = r });
                    }

                    SetFertiliserManureToSession(model);


                    model = await BindGrassAndDoubleCropForFieldGroup(model);
                    return RedirectForFieldGroupGet(model);
                }
            }

            (_, _, error) = await BindFieldGroupList(model, error);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_errorOnHarvestYearOverview] = error.Message;
                ClearTempErrors(_fieldGroupErrorTempDataKey, _fieldErrorTempDataKey);

                SetFertiliserManureToSession(model);
                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in FieldGroup() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_errorOnHarvestYearOverview] = ex.Message;

            ClearTempErrors(_fieldGroupErrorTempDataKey, _fieldErrorTempDataKey);
            if (model != null)
            {
                SetFertiliserManureToSession(model);
                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
            }

        }

        SetFertiliserManureToSession(model);
        return View("Views/FertiliserManure/FieldGroup.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FieldGroup(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : FieldGroup() post action called");
        Error? error = null;
        if (model.FieldGroup == null)
        {
            ModelState.AddModelError(_fieldGroupActionName, Resource.MsgSelectAnOptionBeforeContinuing);
        }
        try
        {
            (List<ManureCropTypeResponse> cropGroupList, List<SelectListItem> selectListItem, error) = await BindFieldGroupList(model, error);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_fieldGroupErrorTempDataKey] = error.Message;
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
                    model.CropOrder = Convert.ToInt32(cropOrderList[0]);
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
            _logger.LogTrace(ex, "Farm Controller : Exception in FieldGroup() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_fieldGroupErrorTempDataKey] = ex.Message;
            return View("Views/FertiliserManure/FieldGroup.cshtml", model);
        }
        return RedirectToAction(_fieldsActionName);
    }
    private async Task<(FertiliserManureViewModel model, Error? error)> BindFarmData(FertiliserManureViewModel? model, Error? error)
    {
        (FarmResponse? farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
        if (string.IsNullOrWhiteSpace(error?.Message))
        {
            model.FarmName = farm?.Name;
            model.FarmRB209CountryID = farm?.RB209CountryID;
            model.FarmCountryId = farm?.CountryID;
            SetFertiliserManureToSession(model);
        }
        else
        {
            TempData[_errorOnHarvestYearOverview] = error.Message;
            ClearTempErrors(_fieldGroupErrorTempDataKey, _fieldErrorTempDataKey);
            return (model, error);
        }

        return (model, error);
    }

    private async Task<(List<ManureCropTypeResponse>, List<SelectListItem>, Error?)> BindFieldGroupList(FertiliserManureViewModel? model, Error? error)
    {
        List<SelectListItem> selectListItem = new List<SelectListItem>();
        (List<ManureCropTypeResponse> cropGroupList, error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
        cropGroupList = cropGroupList.DistinctBy(x => x.CropGroupName).ToList();

        if (error == null && cropGroupList.Count > 0)
        {
            selectListItem = cropGroupList.Select(f => new SelectListItem
            {
                Value = f.CropGroupName.ToString(),
                Text = string.Format(Resource.lblGroupNameFieldsWithCropTypeName, f.CropGroupName.ToString(), f.CropType.ToString())
            }).ToList();

            selectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
            selectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });
            ViewBag.FieldGroupList = selectListItem;
        }

        return (cropGroupList, selectListItem, error);
    }

    private async Task<FertiliserManureViewModel?> BindGrassAndDoubleCropForFieldGroup(FertiliserManureViewModel? model)
    {
        if (model.FertiliserManures != null)
        {
            int fertiliserCounter = 1;
            foreach (var fertiliser in model.FertiliserManures)
            {
                (ManagementPeriod? managementPeriod, _) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
                if (managementPeriod != null)
                {
                    (Crop? crop, _) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                    if (crop != null && crop.DefoliationSequenceID != null && crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                    {
                        model.IsAnyCropIsGrass = true;
                        fertiliser.IsGrass = true;
                    }
                    fertiliser.EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString());
                    fertiliserCounter++;
                }
            }
        }
        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
        {
            (List<HarvestYearPlanResponse> cropPlans, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);

            model = await ApplyGrassCropLogicAsync(model, cropPlans);

            model.IsSameDefoliationForAll = true;
            SetFertiliserManureToSession(model);
        }

        return model;
    }

    private async Task<(Error?, FertiliserManureViewModel)> BindFertiliserListForFieldGroup(FertiliserManureViewModel? model, string fieldId)
    {
        Error? error = null;
        (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fieldId), model.HarvestYear.Value);
        await BindDoubleCropAndDefoliationEncryptedData(model, fieldId, cropList);

        (List<int> managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId, null, 1);// 1 id cropOrder
        if (error == null && managementIds.Count > 0)
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

        return (error, model);
    }

    private async Task BindDoubleCropAndDefoliationEncryptedData(FertiliserManureViewModel model, string fieldId, List<Crop> cropList)
    {
        if (cropList.Count > 0)
        {
            if (cropList.Count == 2)
            {
                model.IsDoubleCropAvailable = true;
                model.DoubleCropCurrentCounter = 0;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
            }
            if (cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                model.IsAnyCropIsGrass = true;
                model.DefoliationCurrentCounter = 0;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Fields()
    {
        _logger.LogTrace("Fertiliser Manure Controller : Fields() action called");
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        if (model == null)
        {
            return RedirectNoSessionFound();
        }
        try
        {
            IActionResult? value = null;

            model.CropOrder = 1;
            (bool flowControl, value) = await HandleFieldDataGet(model, value);
            if (!flowControl && value != null)
            {
                return value;
            }

            model = RemoveFieldsFromDoubleCropList(model);
            var result = ProcessFertiliserManureModel(model);

            if (result != null)
            {
                return result;
            }
            SetFertiliserManureToSession(model);
            if (model.FieldGroup != null && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                SetFertiliserManureToSession(model);
                return RedirectToAction(_inOrgnaicManureDurationActionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Fertiliser Controller : Exception in Fields() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            if (string.IsNullOrWhiteSpace(model?.EncryptedFertId))
            {
                TempData[_fieldGroupErrorTempDataKey] = ex.Message;
                ClearTempErrors(_fieldErrorTempDataKey);
                return RedirectToAction(_fieldGroupActionName);
            }

            TempData[_checkYourAnswerErrorDataKey] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);

        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fields(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Fields() post action called");

        try
        {
            (List<CommonResponse> fieldList, Error? error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
            if (fieldList.Count > 0)
            {
                (bool isSucess, IActionResult? action, List<SelectListItem>? selectListItem, List<HarvestYearPlanResponse> cropPlans) = await BindSelectedListItemsForField(model, fieldList);
                if (!isSucess && action != null)
                {
                    return action;
                }

                ValidateFieldsProperty(model);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                FertiliserManureViewModel? fertiliserManureViewModel;
                (bool flowControl, IActionResult? value, fertiliserManureViewModel) = await HandleFieldDataForPost(model, fieldList, error, selectListItem, cropPlans);
                if (!flowControl && value != null)
                {
                    return value;
                }

                model = IsAnyChangesInField(model, fertiliserManureViewModel);
                model = await BindDefoliationListForField(model, cropPlans);
            }
            else
            {
                return RedirectForFieldError(model, error);
            }

            model = RemoveFieldsFromDoubleCropList(model);
            var result = ProcessFertiliserManureModel(model);

            if (result != null)
            {
                return result;
            }
            return RedirectToAction(_inOrgnaicManureDurationActionName);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in Fields() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_fieldErrorTempDataKey] = ex.Message;
            return View(model);
        }
    }

    private async Task<(bool flowControl, IActionResult? value)> HandleFieldDataGet(FertiliserManureViewModel model, IActionResult? value)
    {
        if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
        {
            (bool flowControl, value) = await BindFieldDataForAdd(model);
            if (!flowControl && value != null)
            {
                return (flowControl: false, value: value);
            }
        }
        else
        {
            int decryptedId = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId));
            bool isAllDataAvailable = decryptedId > 0 && model.FarmId != null && model.HarvestYear != null;
            if (isAllDataAvailable)
            {
                (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, Error error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(decryptedId, model.FarmId.Value, model.HarvestYear.Value);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    SetFertiliserManureToSession(model);
                    TempData[_checkYourAnswerErrorDataKey] = error.Message;
                    return (flowControl: false, value: RedirectToAction(_checkAnswerActionName));
                }
                (var redirect, _) = BindFieldViewBegGet(fertiliserResponse, null, true, true);
                if (redirect != null)
                {
                    return (flowControl: false, value: View(_fieldsActionName, model));
                }
            }
        }

        return (flowControl: true, value: null);
    }

    private IActionResult RedirectNoSessionFound()
    {

        _logger.LogError("Fertiliser Manure Controller : Session not found in FieldGroup() action");
        return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);

    }

    private async Task<(bool flowControl, IActionResult? value)> BindFieldDataForAdd(FertiliserManureViewModel? model)
    {
        (List<CommonResponse> fieldList, Error? error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
        (bool flowControl, IActionResult? value) = BindErrorAndRedirectForField(error);
        if (!flowControl && value != null)
        {
            return (flowControl: false, value: value);
        }

        if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
        {
            BindFieldViewBegGet(null, fieldList, false, true);
            return (flowControl: false, value: View(_fieldsActionName, model));
        }
        else
        {
            (bool isSccuss, IActionResult? actionResult) = await BindFieldData(model, error, fieldList);
            if (!isSccuss && actionResult != null)
            {
                return (flowControl: false, value: actionResult);
            }

            SetFertiliserManureToSession(model);
        }

        return (flowControl: true, value: null);
    }

    private async Task<(bool flowControl, IActionResult? value)> BindFieldData(FertiliserManureViewModel? model, Error error, List<CommonResponse> fieldList)
    {
        FertiliserManureViewModel? fertiliserManureViewModel = GetFertiliserManureFromSession();
        if (fertiliserManureViewModel == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in Fields() action");
            return (flowControl: false, value: Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
        }

        (List<HarvestYearPlanResponse> cropPlans, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);
        (bool isSucess, IActionResult? iActionResult) = BindErrorAndRedirectForField(error);
        if (!isSucess && iActionResult != null)
        {
            return (flowControl: false, value: iActionResult);
        }

        (bool flowControlForField, IActionResult? valueForField, model) = await BindFieldDataForGet(model, error, fieldList, fertiliserManureViewModel, cropPlans);
        if (!flowControlForField && valueForField != null)
        {
            return (flowControl: false, value: valueForField);
        }

        return (flowControl: true, value: null);
    }

    private (bool flowControl, IActionResult? value) BindErrorAndRedirectForField(Error? error)
    {
        if (error != null)
        {
            TempData[_fieldGroupErrorTempDataKey] = error.Message;
            ClearTempErrors(_fieldErrorTempDataKey);
            return (flowControl: false, value: RedirectToAction(_fieldGroupActionName));
        }

        return (flowControl: true, value: null);
    }

    private async Task<(bool flowControl, IActionResult? value, FertiliserManureViewModel)> BindFieldDataForGet(FertiliserManureViewModel? model, Error error, List<CommonResponse> fieldList, FertiliserManureViewModel? fertiliserManureViewModel, List<HarvestYearPlanResponse> cropPlans)
    {
        if (fieldList.Count > 0)
        {
            model.IsAnyCropIsGrass = false;
            model.FieldList = fieldList.Select(x => x.Id.ToString()).ToList();
            model.IsDoubleCropAvailable = false;
            foreach (string field in model.FieldList)
            {
                List<HarvestYearPlanResponse> cropList = cropPlans.Where(x => x.FieldID == Convert.ToInt32(field)).ToList();
                model = await BindGrassProperty(model, cropList, Convert.ToInt32(field), fieldList, true);
            }

            string fieldIds = string.Join(",", model.FieldList);
            List<int> managementIds = new List<int>();
            (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldIds, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? null : model.FieldGroup, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? 1 : null);
            if (error != null)
            {
                TempData[_fieldGroupErrorTempDataKey] = error.Message;
                ClearTempErrors(_fieldErrorTempDataKey);
                return (flowControl: false, value: RedirectToAction(_fieldGroupActionName), model);
            }
            (model, fertiliserManureViewModel) = await BuildFertiliserManureList(managementIds, model, fertiliserManureViewModel, cropPlans);
        }



        SetFertiliserManureToSession(model);
        bool isNeedtoApplyGrassLogic = model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value && model.FieldList != null && model.FertiliserManures != null;
        if (isNeedtoApplyGrassLogic)
        {
            model = await ApplyGrassCropLogicAsync(model, cropPlans);
        }
        else
        {
            model.GrassCropCount = null;
            model.IsSameDefoliationForAll = null;
            model.IsAnyChangeInSameDefoliationFlag = false;
            model.DefoliationList = null;
        }

        model = IsAnyChangesInField(model, fertiliserManureViewModel);
        model = await BindDefoliationListForField(model, cropPlans);
        return (flowControl: true, value: null, model);
    }

    private static FertiliserManureViewModel IsAnyChangesInField(FertiliserManureViewModel model, FertiliserManureViewModel? fertiliserManureViewModel)
    {
        bool anyNewManId = false;
        if (fertiliserManureViewModel != null && fertiliserManureViewModel.FertiliserManures != null)
        {
            anyNewManId = model.FertiliserManures.Any(newId => !fertiliserManureViewModel.FertiliserManures.Contains(newId));
            if (anyNewManId)
            {
                model.IsAnyChangeInField = true;
            }
        }

        return model;
    }

    private (IActionResult?, List<SelectListItem>?) BindFieldViewBegGet(
    List<FertiliserAndOrganicManureUpdateResponse>? fertiliserResponse,
    List<CommonResponse>? fieldList,
    bool isUpdate,
    bool isGet)
    {
        IEnumerable<SelectListItem> selectListItems = !isUpdate
            ? fieldList!.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Name?.ToString()
            })
            : fertiliserResponse!
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name?.ToString()
                })
                .DistinctBy(x => x.Value);

        var orderedList = selectListItems
            .OrderBy(x => x.Text)
            .ToList();

        ViewBag.FieldList = orderedList;

        return isGet
            ? (View(_fieldsActionName), null)
            : (null, orderedList);
    }
    private static FertiliserManureViewModel RemoveFieldsFromDoubleCropList(FertiliserManureViewModel model)
    {
        //remove fields that's not in fieldList
        if (model.FieldList != null && model.FieldList.Any() && model.DoubleCrop != null && model.DoubleCrop.Count > 0 &&
        model.DoubleCrop.Any(dc => !model.FieldList.Contains(dc.FieldID.ToString())))
        {
            model.DoubleCrop?.RemoveAll(dc => !model.FieldList.Contains(dc.FieldID.ToString()));
        }
        return model;
    }
    private async Task<FertiliserManureViewModel> BindGrassProperty(FertiliserManureViewModel model, List<HarvestYearPlanResponse> cropList, int fieldId, List<CommonResponse> fieldList, bool isFieldGet)
    {
        if (cropList.Count > 0)
        {
            if (!model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                cropList = cropList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).ToList();
            }
            else if (isFieldGet)
            {
                cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();
            }
            if (cropList.Count > 0 && cropList.Count == 2)
            {
                model.IsDoubleCropAvailable = true;
                model.DoubleCropCurrentCounter = 0;
                model.FieldName = fieldList?.FirstOrDefault(x => x.Id == fieldId)?.Name;

                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
            }
            else if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
            {
                model.DoubleCrop.RemoveAll(x => x.FieldID == fieldId);
            }
            if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                model.IsAnyCropIsGrass = true;
                model.DefoliationCurrentCounter = 0;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
            }
        }
        return await Task.FromResult(model);
    }

    private async Task<(bool flowControl, IActionResult? value, List<SelectListItem>?, List<HarvestYearPlanResponse>)> BindSelectedListItemsForField(FertiliserManureViewModel model, List<CommonResponse> fieldList)
    {
        List<HarvestYearPlanResponse> cropPlans = new List<HarvestYearPlanResponse>();
        List<SelectListItem>? selectListItem = null;
        Error? error = null;
        (_, selectListItem) = BindFieldViewBegGet(null, fieldList, false, false);
        if (!string.IsNullOrWhiteSpace(model.EncryptedFertId))
        {
            (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId)), model.FarmId.Value, model.HarvestYear.Value);
            if (error != null)
            {
                return (flowControl: false, value: RedirectForFieldError(model, error), selectListItem, cropPlans);
            }
            (_, selectListItem) = BindFieldViewBegGet(fertiliserResponse, null, true, false);
        }
        (cropPlans, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);
        if (!string.IsNullOrWhiteSpace(error?.Message))
        {
            return (flowControl: false, value: RedirectForFieldError(model, error), selectListItem, cropPlans);
        }

        return (flowControl: true, value: null, selectListItem, cropPlans);
    }

    private async Task<(bool flowControl, IActionResult? value, FertiliserManureViewModel)> HandleFieldDataForPost(FertiliserManureViewModel model, List<CommonResponse> fieldList, Error error, List<SelectListItem>? selectListItem, List<HarvestYearPlanResponse> cropPlans)
    {
        BindFieldDataIfSelectAll(model, selectListItem);

        FertiliserManureViewModel? fertiliserManureViewModel = GetFertiliserManureFromSession();
        if (fertiliserManureViewModel == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in Fields() post action");
            return (flowControl: false, value: Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict), fertiliserManureViewModel);
        }
        model.IsAnyCropIsGrass = false;
        model.IsDoubleCropAvailable = false;
        (bool flowControl, IActionResult? value) = await BindFieldDataForPost(model, error, fieldList, cropPlans, fertiliserManureViewModel);
        if (!flowControl && value != null)
        {
            return (flowControl: false, value: value, fertiliserManureViewModel);
        }

        return (flowControl: true, value: null, fertiliserManureViewModel);
    }

    private IActionResult RedirectForFieldError(FertiliserManureViewModel model, Error error)
    {
        TempData[_fieldErrorTempDataKey] = error.Message;
        return View(model);
    }

    private async Task<(bool flowControl, IActionResult? value)> BindFieldDataForPost(FertiliserManureViewModel model, Error error, List<CommonResponse> fieldList, List<HarvestYearPlanResponse> cropPlans, FertiliserManureViewModel? fertiliserManureViewModel)
    {
        foreach (string field in model.FieldList)
        {
            List<HarvestYearPlanResponse> cropList = cropPlans.Where(x => x.FieldID == Convert.ToInt32(field)).ToList();
            model = await BindGrassProperty(model, cropList, Convert.ToInt32(field), fieldList, false);
        }

        string fieldIds = string.Join(",", model.FieldList);

        List<int> managementIds = new List<int>();
        (managementIds, error) = await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldIds, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? null : model.FieldGroup, (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) ? 1 : null);

        if (error != null)
        {
            TempData[_fieldErrorTempDataKey] = error.Message;
            return (flowControl: false, value: View(model));
        }
        (model, fertiliserManureViewModel) = await BuildFertiliserManureList(managementIds, model, fertiliserManureViewModel, cropPlans);
        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
        {
            (model, error) = await BindGrassDataForPost(model, error, cropPlans);
        }
        else
        {
            model.GrassCropCount = null;
            model.IsSameDefoliationForAll = null;
            model.IsAnyChangeInSameDefoliationFlag = false;
        }
        return (flowControl: true, value: null);
    }

    private async Task<(FertiliserManureViewModel model, Error error)> BindGrassDataForPost(FertiliserManureViewModel model, Error error, List<HarvestYearPlanResponse> cropPlans)
    {
        int grassCropCounter = 0;
        foreach (var field in model.FieldList)
        {
            List<HarvestYearPlanResponse> cropList = cropPlans.Where(x => x.FieldID == Convert.ToInt32(field) && x.CropOrder == 1).ToList();

            if (cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                (List<ManagementPeriod> managementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropList[0].CropID, false);

                var filteredFertiliserManure = model.FertiliserManures?.Where(fm => managementPeriod.Any(mp => mp.ID == fm.ManagementPeriodID) &&
                fm.Defoliation == null).ToList();
                if (filteredFertiliserManure != null && filteredFertiliserManure.Count == managementPeriod.Count)
                {
                    model = RemoveListItem(model, managementPeriod);
                }
                grassCropCounter++;
                model.IsAnyCropIsGrass = true;
            }
        }
        model.GrassCropCount = grassCropCounter;


        return (model, error);
    }

    private static void BindFieldDataIfSelectAll(FertiliserManureViewModel model, List<SelectListItem>? selectListItem)
    {
        if (model.FieldList?.Count > 0 && model.FieldList.Contains(Resource.lblSelectAll))
        {
            model.FieldList = selectListItem.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
        }
    }

    private void ValidateFieldsProperty(FertiliserManureViewModel model)
    {
        if (model.FieldList == null || model.FieldList.Count == 0)
        {
            ModelState.AddModelError("FieldList", Resource.MsgSelectAtLeastOneField);
        }
    }

    private IActionResult? ProcessFertiliserManureModel(FertiliserManureViewModel model)
    {
        Functions.BindCounter(model.DefoliationList, _fieldDataProtector,
             (item, count, enc) =>
             {
                 item.Counter = count;
                 item.EncryptedCounter = enc;
             });
        Functions.BindCounter(model.DoubleCrop, _fieldDataProtector,
         (item, count, enc) =>
         {
             item.Counter = count;
             item.EncryptedCounter = enc;
         });
        SetFertiliserManureToSession(model);

        // Check Answer flow
        if (model.IsCheckAnswer && !model.IsAnyChangeInField)
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


        model.DoubleCrop = null;
        SetFertiliserManureToSession(model);


        if (model.IsAnyCropIsGrass.HasValue && (model.IsAnyCropIsGrass.Value))
        {
            if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
            {
                return RedirectToAction(_isSameDefoliationForAllActionName);
            }

            model.IsSameDefoliationForAll = true;
            SetFertiliserManureToSession(model);
            return RedirectToAction(_defoliationActionName);
        }


        return null;
    }

    private async Task<FertiliserManureViewModel> BindDefoliationListForField(FertiliserManureViewModel model, List<HarvestYearPlanResponse> cropPlans)
    {
        int fertiliserCounter = 1;
        if (model.FertiliserManures != null)
        {
            model.FertiliserManures = await BindFertiliserData(model, cropPlans, fertiliserCounter);

            var grass = model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).ToHashSet();
            if (model.DefoliationList != null)
            {
                model.DefoliationList = model.DefoliationList.Where(d => grass.Contains(d.FieldID)).ToList();
            }
            else
            {
                model.DefoliationList = null;
            }
        }


        return model;
    }

    private async Task<List<FertiliserManureDataViewModel>> BindFertiliserData(FertiliserManureViewModel model, List<HarvestYearPlanResponse> cropPlans, int fertiliserCounter)
    {
        foreach (var fertiliser in model.FertiliserManures)
        {
            (ManagementPeriod? managementPeriod, _) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
            if (managementPeriod != null && managementPeriod.CropID != null)
            {
                HarvestYearPlanResponse? crop = cropPlans.FirstOrDefault(x => x.CropID == managementPeriod.CropID.Value);
                if (crop != null)
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
        return model.FertiliserManures;
    }

    private static FertiliserManureViewModel RemoveListItem(FertiliserManureViewModel model, List<ManagementPeriod> managementPeriod)
    {
        var managementPeriodIdsToRemove = managementPeriod
         .Skip(1)
         .Where(mp => mp.ID.HasValue)
         .Select(mp => mp.ID.Value)
         .ToList();
        model.FertiliserManures?.RemoveAll(fm => managementPeriodIdsToRemove.Contains(fm.ManagementPeriodID));
        return model;
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
            await SetClosedPeriodAndNVZAsync(model);

        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in InOrgnaicManureDuration() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            if (model != null && model.FieldGroup != null && model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                TempData[_fieldErrorTempDataKey] = ex.Message;
                ClearTempErrors(_inOrgnaicManureDurationErrorTempDataKey);
                return RedirectToAction(_fieldsActionName);
            }
            else
            {
                TempData[_fieldGroupErrorTempDataKey] = ex.Message;
                ClearTempErrors(_inOrgnaicManureDurationErrorTempDataKey);
                return RedirectToAction(_fieldGroupActionName);
            }
        }

        if (model.FieldList != null && model.FieldList.Count == 1)
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

            DateTime maxDate = new DateTime(model.HarvestYear.Value + 1, 12, 31, 0, 0, 0, DateTimeKind.Local);
            DateTime minDate = new DateTime(model.HarvestYear.Value - 1, 01, 01, 0, 0, 0, DateTimeKind.Local);

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
                await SetClosedPeriodAndNVZAsync(model);
                return View(model);
            }

            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Farm Controller : Exception in InOrgnaicManureDuration() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_inOrgnaicManureDurationErrorTempDataKey] = ex.Message;
            return View(model);
        }

        if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction(_nutrientValuesActionName);
    }

    private static Dictionary<int, string> GetMonthDictionary()
    {
        return new Dictionary<int, string>
        {
            { 0, Resource.lblJanuary },
            { 1, Resource.lblFebruary },
            { 2, Resource.lblMarch },
            { 3, Resource.lblApril },
            { 4, Resource.lblMay },
            { 5, Resource.lblJune },
            { 6, Resource.lblJuly },
            { 7, Resource.lblAugust },
            { 8, Resource.lblSeptember },
            { 9, Resource.lblOctober },
            { 10, Resource.lblNovember },
            { 11, Resource.lblDecember }
        };
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
                    if (int.TryParse(model.FieldList[0], out fieldId))
                    {
                        (fieldId, ViewBag.CropTypeId, ViewBag.DefoliationSequenceName, model) = await PopulateRecommendationData(model, error, fieldId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Farm Controller : Exception in NutrientValues() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                    TempData[_inOrgnaicManureDurationErrorTempDataKey] = ex.Message;
                    return RedirectToAction(_inOrgnaicManureDurationActionName, model);
                }
            }

            model.IsNitrogenExceedWarning = false;
            model.IsNMaxLimitWarning = false;
            model.IsWarningMsgNeedToShow = false;
            model.IsClosedPeriodWarning = false;
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            TempData[_inOrgnaicManureDurationActionName] = ex.Message;
            return RedirectToAction(_inOrgnaicManureDurationActionName);
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
                (bool isSuccess, IActionResult? action) = await ValidateNutrientValuesProperties(model, error);
                if (!isSuccess && action != null)
                {
                    return action;
                }
                return View(model);
            }

            BindValuesForNutrientProperties(model);

            if (model.FieldList.Count >= 1)
            {
                FertiliserManureViewModel? fertiliserManureViewModel = GetFertiliserManureFromSession();
                if (fertiliserManureViewModel == null)
                {
                    _logger.LogError("Fertiliser Manure Controller : Session not found in NutrientValues() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                if (model.N != fertiliserManureViewModel.N)
                {
                    model.IsWarningMsgNeedToShow = false;
                }
                (bool isSuccess, IActionResult action, model) = await BindWarningForFertiliser(model, error);
                if (!isSuccess && action != null)
                {
                    return action;
                }
            }
            (bool flowControl, IActionResult? value, model) = RedirectForNutrientValues(model);
            if (!flowControl && value != null)
            {
                return value;
            }
            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            TempData[_nutrientValuesError] = ex.Message;
            return View(model);
        }
        return RedirectToAction(_checkAnswerActionName);
    }

    private static void BindValuesForNutrientProperties(FertiliserManureViewModel model)
    {
        model.IsNitrogenExceedWarning = false;
        model.IsNMaxLimitWarning = false;
        model.IsClosedPeriodWarning = false;
        if (model.Lime != null)
        {
            model.Lime = Math.Round(model.Lime.Value, 1);
        }
    }

    private async Task<(bool flowControl, IActionResult value, FertiliserManureViewModel)> BindWarningForFertiliser(FertiliserManureViewModel model, Error error)
    {
        if (model.FertiliserManures != null)
        {
            foreach (var fertiliser in model.FertiliserManures)
            {
                (bool isSuccessForCalculation, IActionResult? actionResult, model) = await CalculateNitrogenWarning(model, error, fertiliser);
                if (!isSuccessForCalculation && actionResult != null)
                {
                    return (flowControl: false, value: actionResult, model);
                }
            }
        }

        return (flowControl: true, value: null, model);
    }

    private (bool flowControl, IActionResult? value, FertiliserManureViewModel) RedirectForNutrientValues(FertiliserManureViewModel model)
    {
        if (model.IsNitrogenExceedWarning || model.IsNMaxLimitWarning || model.IsClosedPeriodWarning)
        {
            if (!model.IsWarningMsgNeedToShow)
            {
                model.IsWarningMsgNeedToShow = true;
                SetFertiliserManureToSession(model);
                return (flowControl: false, value: View(model), model);
            }
        }
        else
        {
            model.IsNitrogenExceedWarning = false;
            model.IsNMaxLimitWarning = false;
            model.IsClosedPeriodWarning = false;
            model.IsWarningMsgNeedToShow = false;
        }

        return (flowControl: true, value: null, model);
    }

    private async Task<(bool flowControl, IActionResult? value, FertiliserManureViewModel)> CalculateNitrogenWarning(FertiliserManureViewModel model, Error? error, FertiliserManureDataViewModel fertiliser)
    {
        int? fieldId = fertiliser.FieldID ?? null;
        if (fieldId == null)
        {
            return (flowControl: true, value: null, model);
        }
        Field field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
        if (field == null)
        {
            return (flowControl: true, value: null, model);
        }
        model.FieldID = fieldId.Value;
        bool isFieldIsInNVZ = field.IsWithinNVZ.Value;
        if (!isFieldIsInNVZ)
        {
            return (flowControl: true, value: null, model);
        }
        (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);
        if (!string.IsNullOrWhiteSpace(error?.Message) || managementPeriod == null)
        {
            TempData[_nutrientValuesError] = error?.Message;
            return (flowControl: false, value: RedirectToAction(_nutrientValuesActionName, model), model);
        }
        (Crop? crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
        if (!string.IsNullOrWhiteSpace(error?.Message) || crop == null)
        {
            TempData[_nutrientValuesError] = error?.Message;
            return (flowControl: false, value: RedirectToAction(_nutrientValuesActionName, model), model);
        }

        int year = model.HarvestYear.Value;
        (string? closedPeriod, error) = await _fertiliserManureLogic.FetchFertiliserManureClosedPeriod(model.FarmCountryId ?? 0, crop.CropTypeID.Value, field.NVZProgrammeID);

        (bool flowControl, string? errorMessage, model) = await BindNitrogenWarningData(model, fertiliser, field, crop, year, closedPeriod);
        if (!flowControl && !string.IsNullOrWhiteSpace(errorMessage))
        {
            TempData[_nutrientValuesError] = errorMessage;
            return (flowControl: false, value: RedirectToAction(_nutrientValuesActionName, model), model);
        }


        return (flowControl: true, value: null, model);
    }

    private async Task<(bool flowControl, string?, FertiliserManureViewModel)> BindNitrogenWarningData(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliser, Field field, Crop crop, int year, string? closedPeriod)
    {
        Regex regex = new Regex(_pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
        if (closedPeriod != null)
        {
            Match match = regex.Match(closedPeriod);
            if (match.Success)
            {
                GetStartAndEndDateForWarning(year, match, out DateTime startDate, out DateTime endDate);
                (bool flowControl, string? errorMessage, model) = await HandleNitrogenWarning(model, fertiliser, field, crop, startDate, endDate);
                if (!flowControl && !string.IsNullOrWhiteSpace(errorMessage))
                {
                    return (flowControl: false, errorMessage, model);
                }

            }
        }

        return (flowControl: true, null, model);
    }

    private async Task<(bool flowControl, string?, FertiliserManureViewModel)> HandleNitrogenWarning(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliser, Field field, Crop crop, DateTime startDate, DateTime endDate)
    {
        Error? error = null;
        if (model.N > 0)
        {
            (model, error) = await IsNitrogenExceedWarning(model, fertiliser.ManagementPeriodID, crop.CropTypeID.Value
                , startDate, endDate, Convert.ToInt32(field.ID));

            (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
            if (error == null)
            {
                if (cropTypeLinkingResponse != null && cropTypeLinkingResponse.NMaxLimitEngland != 0 && field.IsWithinNVZ.Value)
                {
                    (model, error) = await IsClosedPeriodWarningMessageShow(model, crop.CropTypeID.Value);
                }
            }
            else
            {
                return (flowControl: false, error.Message, model);
            }

        }

        return (flowControl: true, null, model);
    }

    private async Task<(bool flowControl, IActionResult? value)> ValidateNutrientValuesProperties(FertiliserManureViewModel model, Error? error)
    {
        if (model.FieldList != null && model.FieldList.Count == 1)
        {
            int fieldId;
            try
            {
                if (int.TryParse(model.FieldList[0], out fieldId))
                {
                    (fieldId, ViewBag.CropTypeId, ViewBag.DefoliationSequenceName, model) = await PopulateRecommendationData(model, error, fieldId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Farm Controller : Exception in NutrientValues() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_nutrientValuesError] = ex.Message;
                return (flowControl: false, value: View(model));
            }
        }

        return (flowControl: true, value: null);
    }

    private static void GetStartAndEndDateForWarning(int year, Match match, out DateTime startDate, out DateTime endDate)
    {
        Dictionary<int, string> dtfi;
        WarningWithinPeriod.BindDatesForWarning(match, out int startDay, out int endDay, out dtfi, out int startMonth, out int endMonth);

        startDate = new DateTime(DateTime.Now.Ticks, DateTimeKind.Utc);
        endDate = new DateTime(DateTime.Now.Ticks, DateTimeKind.Utc);
        if (startMonth <= endMonth)
        {
            startDate = new DateTime(year - 1, startMonth, startDay, 00, 00, 00, DateTimeKind.Unspecified);
            endDate = new DateTime(year - 1, endMonth, endDay, 00, 00, 00, DateTimeKind.Unspecified);
        }
        else if (startMonth >= endMonth)
        {
            startDate = new DateTime(year - 1, startMonth, startDay, 00, 00, 00, DateTimeKind.Unspecified);
            endDate = new DateTime(year, endMonth, endDay, 00, 00, 00, DateTimeKind.Unspecified);
        }
    }

    private async Task<(int fieldId, int? cropTypeId, string? defoliationSequenceName, FertiliserManureViewModel model)>
    PopulateRecommendationData(FertiliserManureViewModel model, Error? error, int fieldId)
    {
        model.FieldName = (await _fieldLogic.FetchFieldByFieldId(fieldId)).Name;

        (List<RecommendationHeader> recommendationsHeader, error) = await _cropLogic.FetchRecommendationByFieldIdAndYear(fieldId, model.HarvestYear.Value);
        if (error != null || recommendationsHeader == null || !recommendationsHeader.Any())
            return (fieldId, null, null, model);

        var manId = model.FertiliserManures?.FirstOrDefault()?.ManagementPeriodID;
        if (manId == null) return (fieldId, null, null, model);

        var matchedHeader = FindMatchedHeader(recommendationsHeader, manId.Value);
        if (matchedHeader == null || matchedHeader.Crops == null) return (fieldId, null, null, model);

        var cropTypeId = matchedHeader.Crops.CropTypeID;
        string? defoliationSequenceName = null;

        if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass)
        {
            defoliationSequenceName = await GetDefoliationSequenceName(matchedHeader.Crops.DefoliationSequenceID, model.FertiliserManures.FirstOrDefault()?.Defoliation);
        }

        model = BindRecommendation(model, matchedHeader, manId.Value);

        return (fieldId, cropTypeId, defoliationSequenceName, model);
    }

    private static RecommendationHeader? FindMatchedHeader(
     IEnumerable<RecommendationHeader> headers,
     int manId)
    {
        return headers.FirstOrDefault(header =>
            header.RecommendationData?.Any(rd => rd.ManagementPeriod?.ID == manId) == true);
    }

    private async Task<string?> GetDefoliationSequenceName(int? sequenceId, int? defoliationIndex)
    {
        if (sequenceId == null || defoliationIndex == null) return null;

        var (sequence, error) = await _cropLogic.FetchDefoliationSequencesById(sequenceId.Value);
        if (error != null || sequence == null) return null;

        var parts = sequence.DefoliationSequenceDescription?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts == null || defoliationIndex.Value - 1 >= parts.Length) return null;

        var part = parts[defoliationIndex.Value - 1].Trim();
        return string.IsNullOrWhiteSpace(part) ? string.Empty : char.ToUpper(part[0]) + part[1..];
    }

    private static FertiliserManureViewModel BindRecommendation(FertiliserManureViewModel model, RecommendationHeader matchedHeader, int manId)
    {
        if (matchedHeader.RecommendationData != null)
        {
            matchedHeader.RecommendationData = matchedHeader.RecommendationData.Where(x => x.ManagementPeriod.ID == manId).ToList();
            foreach (var recData in matchedHeader.RecommendationData)
            {
                model = FetchRecommendation(model, recData);
            }
        }
        return model;
    }

    private static FertiliserManureViewModel FetchRecommendation(FertiliserManureViewModel model, RecommendationData recData)
    {
        CommonHelpers commonHelpers = new CommonHelpers();
        if (recData.Recommendation != null)
        {
            var rec = commonHelpers.FetchRecommendation(recData.Recommendation);
            model.Recommendation = rec;
        }
        return model;
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

            if (limeError != null && ModelState["Lime"] != null && limeError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Lime"]?.RawValue, Resource.lblLime)))
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
        ModelStateGenericValidations("MgO", Resource.lblMagnesiumMgO, Resource.lblMgO);

        if (model.MgO != null && (model.MgO < 0 || model.MgO > 9999))
        {
            ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblMagnesiumMgO, 9999));
        }
    }

    private void ModelStateGenericValidations(string nutrientErrorKey, string nutrientNameWithFormula, string nutrientFormula)
    {
        if ((!ModelState.IsValid) && ModelState.ContainsKey(nutrientErrorKey))
        {
            var nutrientError = ModelState[nutrientErrorKey]?.Errors.FirstOrDefault()?.ErrorMessage;

            if (nutrientError != null && nutrientError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[nutrientErrorKey]?.RawValue, nutrientFormula)))
            {
                var rawValue = ModelState[nutrientErrorKey]?.RawValue?.ToString();
                var errors = ModelState[nutrientErrorKey]?.Errors;

                if (!string.IsNullOrWhiteSpace(rawValue) && errors != null)
                {
                    bool isDecimal = decimal.TryParse(rawValue, out _);
                    errors.Clear();
                    if (isDecimal)
                    {
                        errors.Add(string.Format(Resource.MsgEnterTheValueAmountUsingIntValueOnly, nutrientNameWithFormula));
                    }
                    else
                    {
                        errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, nutrientNameWithFormula));
                    }
                }
            }
        }
    }

    private void SO3Validations(FertiliserManureViewModel model)
    {
        ModelStateGenericValidations("SO3", Resource.lblSulphurSO3, Resource.lblSO3);

        if (model.SO3 != null && (model.SO3 < 0 || model.SO3 > 9999))
        {
            ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3Lowercase, 9999));
        }
    }

    private void K2OValidations(FertiliserManureViewModel model)
    {
        ModelStateGenericValidations("K2O", Resource.lblPotashK2O, Resource.lblK2O);

        if (model.K2O != null && (model.K2O < 0 || model.K2O > 9999))
        {
            ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2OLowecase, 9999));
        }
    }

    private void P2O5Validations(FertiliserManureViewModel model)
    {
        ModelStateGenericValidations("P2O5", Resource.lblPhosphateP2O5, Resource.lblP2O5);

        if (model.P2O5 != null && (model.P2O5 < 0 || model.P2O5 > 9999))
        {
            ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5Lowercase, 9999));
        }
    }

    private void NValidations(FertiliserManureViewModel model)
    {
        ModelStateGenericValidations("N", Resource.lblNitrogen, Resource.lblN);

        if (model.N != null && (model.N < 0 || model.N > 9999))
        {
            ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrogenLowercase, 9999));
        }
    }
    private async Task CreateDefoliationItem(FertiliserManureViewModel? model, FertiliserManureDataViewModel fertiliserManure, ManagementPeriod managementPeriod, string defoliationName, Crop crop)
    {
        var defoliationList = new DefoliationList
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
        model.DefoliationList.Add(defoliationList);
    }

    private async Task PrepareDoubleCropList(FertiliserManureViewModel? model, string cropTypeName, int fertiliserCounter, Crop crop)
    {
        var doubleCropData = new DoubleCrop
        {
            CropID = crop.ID.Value,
            CropName = cropTypeName,
            CropOrder = crop.CropOrder.Value,
            FieldID = crop.FieldID.Value,
            FieldName = (await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value)).Name,
            EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString()), //model.DoubleCropEncryptedCounter,
            Counter = model.DoubleCropCurrentCounter,
        };
        model.DoubleCrop.Add(doubleCropData);
    }


    [HttpGet]
    public async Task<IActionResult> CheckAnswer(string? q, string? r, string? s, string? t, string? u)
    {
        _logger.LogTrace("Fertiliser Manure Controller : CheckAnswer() action called");

        var model = new FertiliserManureViewModel();
        Error? error = null;

        try
        {
            if (IsQueryValid(q, r, s))
            {
                await InitializeModel(q, r, u, model);
                error = await LoadFertiliserData(q!, r!, s!, t!, model);
            }
            else
            {
                model = GetModelFromSessionOrFail();
                if (model == null)
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            HandleDefoliationCounters(model);
            await ProcessWarnings(model);

            FinalizeModel(model);

            await BindFieldsForCheckAnswer(s, model, error);
            PersistModel(q, r, s, model);

            ViewBag.IsDataChange = CheckIfDataChanged(model);

            return View(model);
        }
        catch (Exception ex)
        {
            return RedirectForErrorOnCheckAnswer(model, ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckAnswer(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : CheckAnswer() post action called");

        try
        {
            (List<HarvestYearPlanResponse> cropPlans, Error error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_checkYourAnswerErrorDataKey] = error.Message;
                return View(model);
            }

            await ValidatePropertiesForCheckAnswer(model, cropPlans);
            await BindViewBegForCheckAnswerPost(model);
            if (!ModelState.IsValid)
            {
                model.IsCheckAnswer = false;
                SetFertiliserManureToSession(model);
                return View(model);
            }

            if (model.FertiliserManures?.Count > 0)
            {
                List<FertiliserManure> fertiliserList = new List<FertiliserManure>();
                var FertiliserManure = new List<object>();
                foreach (FertiliserManureDataViewModel fertiliserManure in model.FertiliserManures)
                {
                    List<WarningMessage> warningMessageList = new List<WarningMessage>();
                    FertiliserManure fertManure = BuildFertiliserBodyForSaveAndUpdate(model, fertiliserManure, null);

                    fertiliserList.Add(fertManure);
                    warningMessageList.AddRange(await GetWarningMessages(model, fertiliserManure));

                    FertiliserManure.Add(new
                    {
                        FertiliserManure = fertManure,
                        WarningMessages = warningMessageList.Count > 0 ? warningMessageList : null,
                    });
                }

                var jsonData = new { FertiliserManure };
                string jsonString = JsonConvert.SerializeObject(jsonData);

                (List<FertiliserManure> fertiliserResponse, error) = await _fertiliserManureLogic.AddFertiliserManureAsync(jsonString);

                if (error == null && fertiliserResponse != null)
                {
                    string successMsg = Resource.lblFertilisersHavebeenSuccessfullyAdded;
                    string successMsgSecond = Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation;
                    bool success = true;
                    RemoveFertiliserManureSession();
                    return RedirectForCheckAnswerSuccess(model, successMsg, successMsgSecond, success);
                }
                TempData[_checkYourAnswerErrorDataKey] = error?.Message;

            }
        }
        catch (Exception ex)
        {
            TempData[_checkYourAnswerErrorDataKey] = ex.Message;
            return View(model);
        }
        return View(model);
    }

    private static bool IsQueryValid(string? q, string? r, string? s)
    {
        return !string.IsNullOrWhiteSpace(q) &&
               !string.IsNullOrWhiteSpace(r) &&
               !string.IsNullOrWhiteSpace(s);
    }
    private async Task InitializeModel(string q, string r, string? u, FertiliserManureViewModel model)
    {
        model.IsComingFromRecommendation = !string.IsNullOrWhiteSpace(u);
        model.EncryptedFertId = q;

        int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(r));
        model.FarmId = farmId;

        var (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);
        if (string.IsNullOrWhiteSpace(error?.Message))
        {
            model.FarmName = farm?.Name;
            model.FarmCountryId = farm?.CountryID;
            model.FarmRB209CountryID = farm?.RB209CountryID;
        }
    }
    private async Task<Error?> LoadFertiliserData(string q, string r, string s, string t, FertiliserManureViewModel model)
    {
        int fertId = Convert.ToInt32(_cropDataProtector.Unprotect(q));
        int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(r));
        int year = Convert.ToInt32(_farmDataProtector.Unprotect(s));

        if (fertId <= 0) return null;

        var (fertiliser, error) = await _fertiliserManureLogic.FetchFertiliserByIdAsync(fertId);
        if (!string.IsNullOrWhiteSpace(error?.Message) || fertiliser == null)
            return error;

        var (responses, err) =
            await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(fertId, farmId, year);

        if (!string.IsNullOrWhiteSpace(err?.Message) || responses == null || responses.Count == 0)
            return err;

        BuildFieldList(model, fertiliser, responses, t);
        await HandleDoubleCrop(model, fertiliser, year);
        await ProcessDefoliation(model, fertiliser);
        PopulateFertiliserData(model, fertiliser, farmId, year, r, s);

        return null;
    }
    private FertiliserManureViewModel GetModelFromSessionOrFail()
    {
        return GetFertiliserManureFromSession();
    }
    private void HandleDefoliationCounters(FertiliserManureViewModel model)
    {
        if (model.DefoliationList == null || model.DefoliationList.Count == 0)
            return;

        model.DefoliationCurrentCounter = model.IsSameDefoliationForAll == true
            ? 1
            : model.DefoliationList.Count;

        model.DefoliationEncryptedCounter =
            _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
    }
    private async Task ProcessWarnings(FertiliserManureViewModel model)
    {
        if (model?.FertiliserManures == null || model.N <= 0)
            return;

        foreach (var fert in model.FertiliserManures)
        {
            if (fert.FieldID == null) continue;

            var field = await _fieldLogic.FetchFieldByFieldId(fert.FieldID.Value);
            if (field == null || !field.IsWithinNVZ.Value) continue;

            await EvaluateWarnings(model, fert, field);
        }
    }
    private void BuildFieldList(
    FertiliserManureViewModel model,
    FertiliserManureDataViewModel fertiliserManure,
    List<FertiliserAndOrganicManureUpdateResponse> responses,
    string t)
    {
        model.UpdatedFertiliserIds = responses;

        if (model.IsComingFromRecommendation)
        {
            model.FieldGroup = Resource.lblSelectSpecificFields;
            model.UpdatedFertiliserIds.RemoveAll(x => x.FertiliserId != fertiliserManure.ID);
            responses.RemoveAll(x => x.FertiliserId != fertiliserManure.ID);
        }

        var selectList = responses.Select(f => new SelectListItem
        {
            Value = f.Id.ToString(),
            Text = f.Name
        }).DistinctBy(x => x.Value);

        ViewBag.Fields = selectList.OrderBy(x => x.Text).ToList();

        var fieldName = _cropDataProtector.Unprotect(t);
        ViewBag.SelectedFields = new List<string> { fieldName };

        var filtered = selectList
            .Where(item => item.Text.Contains(fieldName))
            .ToList();

        if (filtered.Any())
        {
            model.FieldName = filtered.Select(x => x.Text).First();
            model.FieldList = filtered.Select(x => x.Value).ToList();
            model.FieldID = filtered.Select(x => Convert.ToInt32(x.Value)).First();
        }
    }
    private async Task HandleDoubleCrop(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure, int year)
    {
        if (model.FieldList == null) return;

        foreach (string field in model.FieldList)
        {
            var cropList = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(field));
            cropList = cropList.Where(x => x.Year == year).ToList();

            if (cropList.Count == 2)
            {
                model.FieldID = Convert.ToInt32(field);
                model.IsDoubleCropAvailable = true;
                model.FieldName =
                    (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(field))).Name;
            }
        }

        if (!model.IsDoubleCropAvailable) return;

        var (managementPeriod, _) = await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);

        var (crop, _) =
        await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

        var (cropListFull, _) = await _cropLogic.FetchCropPlanByFieldIdAndYear(crop.FieldID.Value, year);
        if (cropListFull == null || cropListFull.Count == 2)
        {
            var cropTypeName =
            await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
            if (model.DoubleCrop == null)
                model.DoubleCrop = new List<DoubleCrop>();

            await PrepareDoubleCropList(model, cropTypeName, 1, crop);
        }

    }
    private async Task ProcessDefoliation(
    FertiliserManureViewModel model,
    FertiliserManureDataViewModel fertiliserManure)
    {
        var (managementPeriod, error) =
            await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);

        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData[_checkYourAnswerErrorDataKey] = error.Message;
            return;
        }

        var (crop, _) =
            await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

        if (crop?.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)
            return;

        fertiliserManure.IsGrass = true;
        model.IsAnyCropIsGrass = true;

        if (model.DefoliationList == null)
            model.DefoliationList = new List<DefoliationList>();

        var (sequence, seqError) =
            await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);

        if (seqError != null) return;

        string defoliationName = CommonHelpers.BindDefoliationName(
            managementPeriod.Defoliation.Value,
            sequence.DefoliationSequenceDescription);

        await CreateDefoliationItem(
            model,
            fertiliserManure,
            managementPeriod,
            defoliationName,
            crop);

        fertiliserManure.Defoliation = managementPeriod.Defoliation;
        fertiliserManure.DefoliationName = defoliationName;
    }
    private void PopulateFertiliserData(
    FertiliserManureViewModel model,
    FertiliserManureDataViewModel fertiliserManure,
    int farmId,
    int year,
    string r,
    string s)
    {
        fertiliserManure.FieldID = model.FieldID;
        fertiliserManure.FieldName = model.FieldName;

        if (model.FertiliserManures == null)
            model.FertiliserManures = new List<FertiliserManureDataViewModel>();

        model.FertiliserManures.Add(fertiliserManure);

        model.IsSameDefoliationForAll = true;
        model.HarvestYear = year;
        model.FarmId = farmId;

        model.EncryptedHarvestYear = s;
        model.EncryptedFarmId = r;

        model.N = fertiliserManure.N;
        model.P2O5 = fertiliserManure.P2O5;
        model.K2O = fertiliserManure.K2O;
        model.MgO = fertiliserManure.MgO;
        model.SO3 = fertiliserManure.SO3;
        model.Lime = fertiliserManure.Lime;

        model.Date = fertiliserManure.ApplicationDate.Value.ToLocalTime();
        model.FieldGroup = Resource.lblSelectSpecificFields;

        SetFertiliserManureToSession(model);
    }
    private async Task EvaluateWarnings(
    FertiliserManureViewModel model,
    FertiliserManureDataViewModel fertiliser,
    Field field)
    {
        var (managementPeriod, error) =
            await _cropLogic.FetchManagementperiodById(fertiliser.ManagementPeriodID);

        if (error != null || managementPeriod?.CropID == null)
            return;

        var (crop, cropError) =
            await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

        if (cropError != null || crop?.CropTypeID == null)
            return;

        var (linking, linkError) =
            await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);

        if (linkError != null) return;

        var (closedPeriod, _) =
            await _fertiliserManureLogic.FetchFertiliserManureClosedPeriod(
                model.FarmCountryId ?? 0,
                crop.CropTypeID.Value,
                field.NVZProgrammeID);

        if (closedPeriod == null) return;

        Regex regex = new Regex(_pattern, RegexOptions.NonBacktracking, TimeSpan.FromMicroseconds(100));
        Match match = regex.Match(closedPeriod);

        if (!match.Success) return;

        GetStartAndEndDateForWarning(model.HarvestYear.Value, match, out DateTime start, out DateTime end);

        if (linking != null && linking.NMaxLimitEngland != 0)
        {
            (model, _) = await IsClosedPeriodWarningMessageShow(model, crop.CropTypeID.Value);
        }

        if (model.N > 0)
        {
            (model, _) = await IsNitrogenExceedWarning(
                model,
                fertiliser.ManagementPeriodID,
                crop.CropTypeID.Value,
                start,
                end,
                fertiliser.FieldID.Value);
        }

        model.IsWarningMsgNeedToShow =
            model.IsClosedPeriodWarning ||
            model.IsNitrogenExceedWarning ||
            model.IsNMaxLimitWarning;
    }

    private static void FinalizeModel(FertiliserManureViewModel model)
    {
        model.IsDoubleCropValueChange = false;
        model.IsCheckAnswer = true;
        model.IsAnyChangeInField = false;
        model.IsAnyChangeInSameDefoliationFlag = false;

        if (model.IsClosedPeriodWarningOnlyForGrassAndOilseed ||
            model.IsClosedPeriodWarning ||
            model.IsNitrogenExceedWarning ||
            model.IsNMaxLimitWarning)
        {
            model.IsWarningMsgNeedToShow = true;
        }
    }
    private void PersistModel(string? q, string? r, string? s, FertiliserManureViewModel model)
    {
        SetFertiliserManureToSession(model);

        if (!string.IsNullOrWhiteSpace(q) &&
            !string.IsNullOrWhiteSpace(r) &&
            !string.IsNullOrWhiteSpace(s))
        {
            SetFertiliserManureBeforeUpdateToSession(model);
        }
    }
    private bool CheckIfDataChanged(FertiliserManureViewModel model)
    {
        var previous = GetFertiliserManureBeforeUpdateFromSession();
        if (previous == null) return false;

        string oldJson = JsonConvert.SerializeObject(previous);
        string newJson = JsonConvert.SerializeObject(model);

        return !string.Equals(oldJson, newJson, StringComparison.Ordinal);
    }


    private async Task<Error> BindFieldsForCheckAnswer(string? s, FertiliserManureViewModel? model, Error error)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            (List<CommonResponse> fieldList, error) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
            if (error == null && (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll)) && fieldList.Count > 0)
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
                if (model.FieldList != null && model.FieldList.Count == 1)
                {
                    model.FieldName = fieldNames.FirstOrDefault();
                }

            }
            error = await BindFieldsForFertiliserUpdate(model, error);
        }

        return error;
    }

    private async Task<Error> BindFieldsForFertiliserUpdate(FertiliserManureViewModel model, Error error)
    {
        if (!string.IsNullOrWhiteSpace(model.EncryptedFertId))
        {
            (List<FertiliserAndOrganicManureUpdateResponse> fertiliserResponse, error) = await _fertiliserManureLogic.FetchFieldWithSameDateAndNutrient(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedFertId)), model.FarmId.Value, model.HarvestYear.Value);
            if (error == null && fertiliserResponse != null && fertiliserResponse.Count > 0)
            {
                var SelectListItem = fertiliserResponse.Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name.ToString()
                }).DistinctBy(x => x.Value);
                ViewBag.Fields = SelectListItem.OrderBy(x => x.Text).ToList();
            }
        }

        return error;
    }



    private IActionResult RedirectForErrorOnCheckAnswer(FertiliserManureViewModel? model, string message)
    {
        if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
        {
            TempData[_nutrientValuesError] = message;
            return RedirectToAction(_nutrientValuesActionName);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && (model.IsComingFromRecommendation))
            {
                TempData[_nutrientRecommendationsError] = message;
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
                TempData[_errorOnHarvestYearOverview] = message;
                return RedirectToAction(_harvestYearOverviewActionName, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
        }
    }


    private IActionResult RedirectForCheckAnswerSuccess(FertiliserManureViewModel model, string successMsg, string successMsgSecond, bool success)
    {
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

    private async Task ValidatePropertiesForCheckAnswer(FertiliserManureViewModel model, List<HarvestYearPlanResponse> cropPlans)
    {
        await ValidateDoubleCropPropertiesForCheckAnswer(model, cropPlans);
        if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
        {
            ValidateGrassPropertiesForCheckAnswer(model);
        }
    }

    private async Task ValidateDoubleCropPropertiesForCheckAnswer(FertiliserManureViewModel model, List<HarvestYearPlanResponse> cropPlans)
    {
        if (model.DoubleCrop == null && model.IsDoubleCropAvailable)
        {
            int index = 0;

            if (model.DoubleCrop == null)
            {
                foreach (string fieldId in model.FieldList)
                {
                    List<HarvestYearPlanResponse> cropList = cropPlans.Where(x => x.FieldID == Convert.ToInt32(fieldId)).ToList();
                    if (cropList.Count == 2)
                    {
                        ModelState.AddModelError("FieldName", string.Format(_twoParamStringFormat, string.Format(Resource.lblWhichCropIsThisManureApplication, (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name), Resource.lblNotSet));
                        index++;
                    }

                }
            }
        }
    }

    private void ValidateGrassPropertiesForCheckAnswer(FertiliserManureViewModel model)
    {

        if (model.GrassCropCount.HasValue && model.GrassCropCount > 1 && model.IsSameDefoliationForAll == null)
        {
            ModelState.AddModelError(_isSameDefoliationForAllActionName, string.Format(_twoParamStringFormat, Resource.lblForMultipleDefoliation, Resource.lblNotSet));
        }

        int i = 0;
        foreach (var defoliation in model.DefoliationList)
        {
            if (model.IsSameDefoliationForAll.HasValue && (model.IsSameDefoliationForAll.Value) && (model.GrassCropCount > 1) && defoliation.Defoliation == null)
            {
                ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format(_twoParamStringFormat, Resource.lblWhichCutOrGrazingInThisInorganicApplicationForAllField, Resource.lblNotSet));
            }
            else if (defoliation.Defoliation == null)
            {
                ModelState.AddModelError(string.Concat("DefoliationList[", i, "].Defoliation"), string.Format(_twoParamStringFormat, string.Format(Resource.lblWhichCutOrGrazingInThisInorganicApplicationForInField, defoliation.FieldName), Resource.lblNotSet));
            }
        }

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
        return RedirectToAction(_nutrientValuesActionName);
    }

    private async Task<(FertiliserManureViewModel, Error?)> IsClosedPeriodWarningMessageShow(FertiliserManureViewModel model, int cropTypeId)
    {
        Error? error = null;

        //warning excel sheet row no. 23
        HashSet<int> filterCrops = WarningWithinPeriod.FilteredCropForWarning();

        int? fieldId = model.FieldID ?? null;
        Field field = await _fieldLogic.FetchFieldByFieldId(fieldId ?? 0);
        (string? closedPeriod, error) = await _fertiliserManureLogic.FetchFertiliserManureClosedPeriod(model.FarmCountryId ?? 0, cropTypeId, field.NVZProgrammeID);
        bool isWithinClosedPeriod = WarningWithinPeriod.IsApplicationWithinWarningPeriod(model.Date.Value, closedPeriod);

        bool isScotland = model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;

        bool isCropAllowed = isScotland ? WarningWithinPeriod.BrassicaCrops().Contains(cropTypeId) : filterCrops.Contains(cropTypeId);

        if (!isCropAllowed && isWithinClosedPeriod)
        {
            WarningResponse warningResponse = await _warningLogic
                .FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0,
                    NMP.Commons.Enums.WarningKey.NitroFertClosedPeriod.ToString());

            model.IsClosedPeriodWarning = true;
            model.ClosedPeriodWarningHeader = warningResponse.Header;
            model.ClosedPeriodWarningCodeID = warningResponse.WarningCodeID;
            model.ClosedPeriodWarningLevelID = warningResponse.WarningLevelID;
            model.ClosedPeriodWarningPara1 = warningResponse.Para1;
            model.ClosedPeriodWarningPara3 = warningResponse.Para3;
        }

        //warning excel sheet row no. 28
        if (model.FarmCountryId != (int)NMP.Commons.Enums.FarmCountry.Scotland && (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape || cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass))
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
            bool isWithinWarningPeriod = WarningWithinPeriod.IsApplicationWithinWarningPeriod(model.Date.Value, warningPeriod);

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


        return (model, error);
    }



    private async Task<(FertiliserManureViewModel, Error?)> IsNitrogenExceedWarning(FertiliserManureViewModel model, int managementId, int cropTypeId, DateTime startDate, DateTime endDate, int fieldId)
    {
        Error? error = null;
        decimal totalNitrogen = 0;
        model.IsNitrogenExceedWarning = false;
        //if we are coming for update then we will exclude the fertiliserId.
        WarningResponse warningResponse = new WarningResponse();
        (totalNitrogen, error) = await FetchNitrogenAsync(fieldId, startDate, endDate, model, managementId, _fertiliserManureLogic.FetchTotalNBasedOnFieldIdAndAppDate);
        if (error == null)
        {
            totalNitrogen = totalNitrogen + Convert.ToDecimal(model.N);
            HashSet<int> brassicaCrops = WarningWithinPeriod.BrassicaCrops();
            Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
            string? closedPeriod = await GetClosedPeriodAsync(model, cropTypeId, field.NVZProgrammeID ?? 0, model.HarvestYear ?? 0);

            bool isWithinClosedPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.Date.Value, startDate, endDate);
            bool isCropBrassicaAndWithInClosedPeriod = brassicaCrops.Contains(cropTypeId) && isWithinClosedPeriod;
            (string startPeriod, string endPeriod) = _fertiliserManureLogic.BindStartPeriodAndEndPeriod(closedPeriod);
            //warning excel sheet row no. 25
            if (isCropBrassicaAndWithInClosedPeriod)
            {
                DateTime fourWeekDate = model.Date.Value.AddDays(-27);
                decimal nitrogenInFourWeek = 0;
                //if we are coming for update then we will exclude the fertiliserId.
                (error, nitrogenInFourWeek) = await _fertiliserManureLogic.BindNitrogenInFourWeekForWarning(model, managementId, fieldId, error, fourWeekDate, nitrogenInFourWeek);

                (model, warningResponse) = await _fertiliserManureLogic.WarningForBrassicaCrop(model, totalNitrogen, warningResponse, startPeriod, endPeriod, nitrogenInFourWeek);

            }

            //warning excel sheet row no. 24
            (model, warningResponse) = await _fertiliserManureLogic.BindNmaxWarningInModelForAsparagusAndOnionCrops(model, cropTypeId, totalNitrogen, isWithinClosedPeriod, startPeriod, endPeriod);

            (string warningPeriod, startPeriod, endPeriod) = _fertiliserManureLogic.BindStartEndDateAndWarningPeriod(model, endDate, closedPeriod);

            bool isWithinWarningPeriod = WarningWithinPeriod.IsApplicationWithinWarningPeriod(model.Date.Value, warningPeriod);

            DateTime endOfOctober = new DateTime(model.Date.Value.Year, 10, 31, 00, 00, 00, DateTimeKind.Unspecified);
            decimal PreviousApplicationsNitrogen = 0;
            //if we are coming for update then we will exclude the fertiliserId.
            (error, PreviousApplicationsNitrogen) = await _fertiliserManureLogic.BindPreviousYearNitrogen(model, managementId, startDate, fieldId, error, endOfOctober, PreviousApplicationsNitrogen);

            //warning excel sheet row no. 26


            (model, warningResponse) = await _fertiliserManureLogic.BindOilseedRapeWarnings(model, managementId, totalNitrogen, startPeriod, PreviousApplicationsNitrogen, isWithinWarningPeriod, cropTypeId);
            //warning excel sheet row no. 27
            bool isThisGrassCropAndInWarningPeriod = (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass && isWithinWarningPeriod);
            if (isThisGrassCropAndInWarningPeriod)
            {
                bool isNitrogenRateExceeded = false;
                string startString = $"{startPeriod} {startDate.Year}";
                DateTime start = DateTime.ParseExact(startString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                string endString = $"{endPeriod} {startDate.Year}";  //because closed period start and 31 october will be in same year
                DateTime end = DateTime.ParseExact(endString, "d MMMM yyyy", CultureInfo.InvariantCulture);
                decimal nitrogenWithinWarningPeriod = 0;
                //if we are coming for update then we will exclude the fertiliserId.
                (error, nitrogenWithinWarningPeriod) = await _fertiliserManureLogic.BindNitrogenWithInWarningPeriod(model, managementId, fieldId, error, start, end, nitrogenWithinWarningPeriod);
                (model, warningResponse, isNitrogenRateExceeded) = await _fertiliserManureLogic.WarningForGrass(model, warningResponse, startPeriod, isNitrogenRateExceeded, nitrogenWithinWarningPeriod);
            }


            //warning excel sheet row no. 8

            //NMax limit for crop logic
            (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(managementId);
            int cropId = managementPeriod?.CropID ?? 0;
            decimal previousApplicationsN = 0;
            decimal currentApplicationNitrogen = Convert.ToDecimal(model.N);

            //if we are coming for update then we will exclude the fertiliserId.
            (error, previousApplicationsN) = await _fertiliserManureLogic.BindPreviousApplicationN(model, managementId, error, cropId, previousApplicationsN);
            if (managementPeriod?.CropID != null)
            {
                (bool flowControl, (FertiliserManureViewModel, Error?) value) = await NmaxLogicForCrop(model, managementId, fieldId, error, managementPeriod, previousApplicationsN, currentApplicationNitrogen);
                if (!flowControl)
                {
                    return value;
                }
            }

        }
        else
        {
            return (model, error);
        }

        return (model, error);
    }




    private async Task<(bool flowControl, (FertiliserManureViewModel, Error?) value)> NmaxLogicForCrop(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, ManagementPeriod? managementPeriod, decimal previousApplicationsN, decimal currentApplicationNitrogen)
    {
        int farmCountryId = model.FarmCountryId ?? 0;
        int scotland = (int)NMP.Commons.Enums.FarmCountry.Scotland;

        (Crop? crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

        (_, CropTypeLinkingResponse cropTypeLinking, int? scotlandNmax, int residueGroup, bool isWinterOilseedRapeAutumn) = await _fertiliserManureLogic.BindDataForNmaxWarning(model, managementId, fieldId, error, farmCountryId, scotland, crop);

        BindNmaxLimitOrIsAppliedNmaxWarning(model, scotland, cropTypeLinking, scotlandNmax, out int? nmaxLimitEnglandOrWales, out bool isAppliedNMaxWarning);
        if (isAppliedNMaxWarning)
        {
            (FieldDetailResponse fieldDetail, _) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);

            decimal nMaxLimit = nmaxLimitEnglandOrWales ?? 0;
            if (model.FarmCountryId != scotland)
            {
                (bool isSucessForWarning, (FertiliserManureViewModel, Error?) value, nMaxLimit) = await NmaxLimitBindForEnglandAndWales(model, fieldId, error, crop, fieldDetail, nMaxLimit);
                if (!isSucessForWarning)
                {
                    return (flowControl: false, value);
                }
            }
            else
            {
                (bool isSucess, error, int? winterRainfall) = await BindWinterRainfallForNmaxLimit(model);
                if (!isSucess)
                {
                    return (flowControl: false, value: (model, error));
                }
                nMaxLimit = OrganicManureNMaxLimitLogic.NMaxLimitScotland(Convert.ToInt32(scotlandNmax), crop.Yield ?? null, fieldDetail.SoilTypeName, crop.CropInfo1 ?? null, crop.CropTypeID.Value, crop.PotentialCut ?? 0, crop.DefoliationSequenceID, winterRainfall, residueGroup, isWinterOilseedRapeAutumn);
            }

            decimal totalNitrogenApplied = previousApplicationsN + currentApplicationNitrogen;

            (bool flowControl, (FertiliserManureViewModel, Error?) _) = await _fertiliserManureLogic.BindNmaxWarnings(model, totalNitrogenApplied, farmCountryId, crop, scotlandNmax, nmaxLimitEnglandOrWales, nMaxLimit);
            if (!flowControl)
            {
                return (flowControl: false, value: (model, error));
            }


        }

        return (flowControl: true, value: default);
    }

    private async Task<(bool flowControl, (FertiliserManureViewModel, Error?) value, decimal)> NmaxLimitBindForEnglandAndWales(FertiliserManureViewModel model, int fieldId, Error? error, Crop? crop, FieldDetailResponse fieldDetail, decimal nMaxLimit)
    {
        (List<int> currentYearManureTypeIds, _) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
        (List<int> previousYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
        if (error == null)
        {
            bool hasSpecialManure = Functions.HasSpecialManure(currentYearManureTypeIds, null) || Functions.HasSpecialManure(previousYearManureTypeIds, null);
            nMaxLimit = OrganicManureNMaxLimitLogic.NMaxLimit(Convert.ToInt32(nMaxLimit), crop.Yield == null ? null : crop.Yield.Value, fieldDetail.SoilTypeName, crop.CropInfo1 == null ? null : crop.CropInfo1.Value, crop.CropTypeID.Value, crop.PotentialCut ?? 0, hasSpecialManure, crop.DefoliationSequenceID);
        }
        else
        {
            return (flowControl: false, value: (model, error), nMaxLimit);
        }

        return (flowControl: true, value: default, nMaxLimit);
    }

    private static void BindNmaxLimitOrIsAppliedNmaxWarning(FertiliserManureViewModel model, int scotland, CropTypeLinkingResponse cropTypeLinking, int? scotlandNmax, out int? nmaxLimitEnglandOrWales, out bool isAppliedNMaxWarning)
    {
        nmaxLimitEnglandOrWales = (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Wales ? cropTypeLinking.NMaxLimitWales : cropTypeLinking.NMaxLimitEngland);
        isAppliedNMaxWarning = ((model.FarmCountryId != scotland && nmaxLimitEnglandOrWales != null) || (model.FarmCountryId == scotland && scotlandNmax != null));
    }

    private async Task<(bool flowControl, Error?, int?)> BindWinterRainfallForNmaxLimit(FertiliserManureViewModel model)
    {
        int? winterRainfall = null;
        (ExcessRainfalls excessRainfalls, Error error) = await _farmLogic.FetchExcessRainfallsAsync(model.FarmId ?? 0, model.HarvestYear ?? 0);
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            return (flowControl: false, error, winterRainfall);
        }
        else
        {
            winterRainfall = excessRainfalls != null ? excessRainfalls.WinterRainfall : null;
        }
        return (flowControl: true, null, winterRainfall);
    }

    private static async Task<(decimal, Error?)> FetchNitrogenAsync(int fieldId, DateTime from,
    DateTime to, FertiliserManureViewModel model, int managementId, Func<int, DateTime, DateTime, int?, bool, Task<(decimal, Error?)>> fetchFunc)
    {
        int? fertiliserId = model.UpdatedFertiliserIds?
            .Where(x => x.ManagementPeriodId == managementId)
            .Select(x => x.FertiliserId)
            .FirstOrDefault();

        return await fetchFunc(fieldId, from, to, fertiliserId, false);
    }




    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFertiliser(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : UpdateFertiliser() post action called");
        Error? error = null;
        try
        {
            (List<HarvestYearPlanResponse> cropPlans, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_checkYourAnswerErrorDataKey] = error.Message;
                return RedirectToAction(_checkAnswerActionName);
            }

            await ValidatePropertiesForCheckAnswer(model, cropPlans);
            if (!ModelState.IsValid)
            {
                SetFertiliserManureToSession(model);
                await BindViewBegForCheckAnswerPost(model);
                return RedirectToAction(_checkAnswerActionName);
            }

            bool isUpdate = (!string.IsNullOrWhiteSpace(model.EncryptedFertId) && model.FertiliserManures?.Count > 0 && model.UpdatedFertiliserIds?.Count > 0);
            if (isUpdate)
            {
                (error, List<FertiliserManure> fertiliser) = await HandleUpdateFertiliserRequest(model, error);
                if (error == null && fertiliser.Count > 0)
                {
                    bool success = true;
                    RemoveFertiliserManureSession();
                    return RedirectForUpdateSuccess(model, success);
                }

                TempData[_checkYourAnswerErrorDataKey] = error?.Message;
                return RedirectToAction(_checkAnswerActionName);

            }
        }
        catch (Exception ex)
        {
            TempData[_checkYourAnswerErrorDataKey] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
        return RedirectToAction(_checkAnswerActionName);
    }

    private async Task<(Error? error, List<FertiliserManure> fertiliser)> HandleUpdateFertiliserRequest(FertiliserManureViewModel model, Error? error)
    {
        List<FertiliserManure> fertiliserList = new List<FertiliserManure>();
        var FertiliserManure = new List<object>();
        foreach (FertiliserManureDataViewModel fertiliserManure in model.FertiliserManures)
        {
            int? fertID = model.UpdatedFertiliserIds != null ? (model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId.Value == fertiliserManure.ManagementPeriodID).Select(x => x.FertiliserId.Value).FirstOrDefault()) : 0;
            FertiliserManure fertManure = BuildFertiliserBodyForSaveAndUpdate(model, fertiliserManure, fertID);
            fertiliserList.Add(fertManure);

            List<WarningMessage> warningMessageList = await GetWarningMessages(model, fertiliserManure);
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
        return (error, fertiliser);
    }

    private IActionResult RedirectForUpdateSuccess(FertiliserManureViewModel model, bool success)
    {
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
                    w = _fieldDataProtector.Protect(model.FieldList[0])
                }) + Resource.lblInorganicFertiliserApplicationsForSorting);
            }
            else
            {
                return RedirectToAction(_recommendationsActionName, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(model.FieldList[0]),
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

    private static FertiliserManure BuildFertiliserBodyForSaveAndUpdate(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure, int? fertID)
    {
        return new FertiliserManure
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
    }

    private async Task BindViewBegForCheckAnswerPost(FertiliserManureViewModel model)
    {
        if (!model.IsComingFromRecommendation)
        {
            (List<CommonResponse> fieldList, _) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) ? null : model.FieldGroup);
            if (model.FieldGroup.Equals(Resource.lblSelectSpecificFields) || model.FieldGroup.Equals(Resource.lblAll) && fieldList.Count > 0)
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
                if (model.FieldList != null && model.FieldList.Count == 1)
                {
                    model.FieldName = fieldNames.FirstOrDefault();
                }
            }
        }

    }

    private FertiliserManureViewModel BindPropertiesForRemove(string q, string r, string s, string? t, string? u, FertiliserManureViewModel? model)
    {
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
        return model;
    }

    private async Task BindViewBegForField(FertiliserManureViewModel? model)
    {
        if (model.FieldList != null && model.FieldList.Count > 0)
        {
            (List<CommonResponse> fieldList, _) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
            if (fieldList.Count > 0)
            {
                PopulateFieldNames(model, fieldList);
                ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList[0]);
            }
        }

    }
    [HttpGet]
    public async Task<IActionResult> RemoveFertiliser(string q, string r, string s, string? t, string? u, string? v)
    {
        _logger.LogTrace("Fertiliser Manure Controller : RemoveFertiliser() action called");
        FertiliserManureViewModel? model = new FertiliserManureViewModel();
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
                await BindViewBegForField(model);
            }
            else
            {
                model.IsComingFromRecommendation = true;
                model = BindPropertiesForRemove(q, r, s, t, u, model);
                SetFertiliserManureToSession(model);
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "OrganicManure Controller : Exception in RemoveFertiliser() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);

            if (model != null && model.IsComingFromRecommendation)
            {
                TempData[_nutrientRecommendationsError] = ex.Message;
                return RedirectToAction(_recommendationsActionName, "Crop", new { q = model.EncryptedFarmId, r, s = model.EncryptedHarvestYear });
            }

            TempData[_checkYourAnswerErrorDataKey] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFertiliser(FertiliserManureViewModel model)
    {
        _logger.LogTrace($"Fertiliser Manure Controller : RemoveFertiliser() post action called");

        if (model.IsDeleteFertliser == null)
        {
            ModelState.AddModelError("IsDeleteFertliser", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            await BindViewBegForField(model);
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
                await BindFertiliserIdsForRemoval(model, fertiliserIds);

                if (fertiliserIds.Count > 0)
                {
                    var result = new
                    {
                        fertliserManureIds = fertiliserIds
                    };
                    string jsonString = JsonConvert.SerializeObject(result);
                    (_, Error error) = await _fertiliserManureLogic.DeleteFertiliserByIdAsync(jsonString);
                    return await RedirectForRemove(model, error);

                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "OrganicManure Controller : Exception in RemoveFertiliser() post action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["RemoveFertiliserError"] = ex.Message;
            return View(model);
        }
        return View(model);
    }

    private async Task<IActionResult> RedirectForRemove(FertiliserManureViewModel model, Error error)
    {
        if (string.IsNullOrWhiteSpace(error?.Message))
        {
            RemoveFertiliserManureSession();

            if (model.IsComingFromRecommendation && model.FieldList?.Count > 0)
            {
                string encryptedFieldId = _fieldDataProtector.Protect(model.FieldList[0]);
                if (!string.IsNullOrWhiteSpace(encryptedFieldId))
                {
                    return RedirectToAction(_recommendationsActionName, "Crop", new { q = model.EncryptedFarmId, r = encryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved) });
                }
            }
            return Redirect(Url.Action(_harvestYearOverviewActionName, "Crop", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.MsgInorganicFertiliserApplicationRemoved) }) + Resource.lblInorganicFertiliserApplicationsForSorting);

        }

        SetFertiliserManureToSession(model);
        if (model.FieldList?.Count > 0)
        {
            (List<CommonResponse> fieldList, Error fieldListError) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
            if (fieldListError != null)
            {
                TempData["RemoveFertiliserError"] = fieldListError.Message;
                return View("RemoveFertiliser", model);
            }
            if (fieldList.Count > 0)
            {
                PopulateFieldNames(model, fieldList);
            }
        }
        TempData["RemoveFertiliserError"] = error.Message;
        return View("RemoveFertiliser", model);

    }

    private async Task BindFertiliserIdsForRemoval(FertiliserManureViewModel model, List<int> fertiliserIds)
    {
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

                fertiliserIds.AddRange(model.UpdatedFertiliserIds.Where(f => fieldName.Equals(f.Name) && f.FertiliserId.HasValue).Select(f => f.FertiliserId!.Value));
            }
        }
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
            _logger.LogTrace(ex, "Fertiliser Manure Controller : Exception in Cancel() action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData[_checkYourAnswerErrorDataKey] = ex.Message;
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





    private async Task<(bool flowControl, IActionResult? value)> PrepareDefoliationList(FertiliserManureViewModel? model, List<Crop> cropList)
    {
        int counter = model.DefoliationList.Count + 1;
        Error? error = null;
        foreach (int? fieldId in model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID))
        {
            bool isFieldAlreadyPresent = model.DefoliationList.Any(dc => dc.FieldID == fieldId);
            if (isFieldAlreadyPresent)
            {
                continue;
            }

            (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId.Value, model.HarvestYear.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return (flowControl: false, value: BindErrorForDefoliationGet(model, error.Message));
            }

            if (cropList.Count > 0)
            {
                var grassCrop = cropList.FirstOrDefault(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass);
                int cropId = 0;
                if (grassCrop != null && grassCrop.ID.HasValue)
                {
                    cropId = grassCrop.ID.Value;
                }

                (bool flowControl, IActionResult? value) = await BindDefoliationList(model, error, counter, fieldId, cropId);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: value);
                }
            }
            counter++;
        }


        return (flowControl: true, value: null);
    }

    private async Task<(bool flowControl, IActionResult? value)> BindDefoliationList(FertiliserManureViewModel? model, Error error, int counter, int? fieldId, int cropId)
    {
        (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            return (flowControl: false, value: BindErrorForDefoliationGet(model, error.Message));
        }
        if (managementPeriodList.Count > 0)
        {
            var field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
            var firstManagement = managementPeriodList.FirstOrDefault();

            if (firstManagement == null || firstManagement.ID == null)
            {
                return (flowControl: false, value: Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            var defoliationList = new DefoliationList
            {
                CropID = cropId,
                ManagementPeriodID = firstManagement.ID.Value,
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
        }

        return (flowControl: true, value: null);
    }

    private IActionResult BindErrorForDefoliationGet(FertiliserManureViewModel? model, string message)
    {

        if (string.IsNullOrWhiteSpace(model.EncryptedFertId))
        {
            if (model.IsDoubleCropAvailable)
            {
                TempData["DoubleCropError"] = message;
                return RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter });
            }
        }
        else
        {
            TempData[_checkYourAnswerErrorDataKey] = message;
            return RedirectToAction(_checkAnswerActionName);
        }
        TempData[_fieldGroupErrorTempDataKey] = message;
        return RedirectToAction(_fieldGroupActionName);

    }

    private (bool flowControl, IActionResult? value) RedirectForDefoliationGet(FertiliserManureViewModel? model, int itemCount)
    {
        if (itemCount == 0)
        {
            model.DefoliationCurrentCounter = 0;
            model.DefoliationEncryptedCounter = string.Empty;
            SetFertiliserManureToSession(model);

            bool isNeedToShowAllDefoliation = (model.GrassCropCount != null && model.GrassCropCount.Value > 1 && model.NeedToShowSameDefoliationForAll);
            bool isThisSelectSpecificAndcomingFromRecommendation = model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation;
            return HandleRedirectForDefoliationGet(model, isNeedToShowAllDefoliation, isThisSelectSpecificAndcomingFromRecommendation);
        }

        bool needToRedirectToDoubleCropAction = (model.IsCheckAnswer && model.IsDoubleCropAvailable && model.IsDoubleCropValueChange && (!model.NeedToShowSameDefoliationForAll));
        if (needToRedirectToDoubleCropAction)
        {
            return (flowControl: false, value: RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter }));
        }

        return (flowControl: true, value: null);
    }

    private (bool flowControl, IActionResult? value) HandleRedirectForDefoliationGet(FertiliserManureViewModel? model, bool isNeedToShowAllDefoliation, bool isThisSelectSpecificAndcomingFromRecommendation)
    {
        if (isNeedToShowAllDefoliation)
        {
            return (flowControl: false, value: RedirectToAction(_isSameDefoliationForAllActionName));
        }
        if (model.IsDoubleCropAvailable || model.IsDoubleCropValueChange)
        {
            return (flowControl: false, value: RedirectToAction(_doubleCropActionName, new { q = model.DoubleCropEncryptedCounter }));
        }
        if (isThisSelectSpecificAndcomingFromRecommendation && model.FieldList.Count == 1)
        {
            string fieldId = model.FieldList[0];
            return (flowControl: false, value: RedirectToAction(_recommendationsActionName, "Crop", new
            {
                q = model.EncryptedFarmId,
                r = _fieldDataProtector.Protect(fieldId),
                s = model.EncryptedHarvestYear

            }));

        }
        else if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
        {
            return (flowControl: false, value: RedirectToAction(_fieldsActionName));
        }
        return (flowControl: false, value: RedirectToAction(_fieldGroupActionName));
    }

    private async Task BindDefoliationData(FertiliserManureViewModel? model)
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
            model.FieldID = model.FertiliserManures?.Where(x => x.IsGrass && x.FieldID.HasValue).Select(x => x.FieldID.Value).First();
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
        }
        SetFertiliserManureToSession(model);
    }


    [HttpGet]
    public async Task<IActionResult> Defoliation(string q)
    {
        _logger.LogTrace("Fertiliser Manure Controller : Defoliation({Q}) action called", q);
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();

        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in Defoliation() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        try
        {
            if (_fertiliserManureLogic.IsComingFirstTimeForDefoliationGet(model, q))
            {
                await BindDefoliationData(model);
            }
            else if (_fertiliserManureLogic.IsRedirectRequestForDefoliationGet(model, q))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;
                (bool flowControl, IActionResult? value) = RedirectForDefoliationGet(model, itemCount);
                if (!flowControl && value != null)
                {
                    return value;
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
                (model, List<Crop> cropList) = await _fertiliserManureLogic.HandleDefoliationList(model);
                (bool flowControl, IActionResult? value) = await PrepareDefoliationList(model, cropList);
                if (!flowControl && value != null)
                {
                    return value;
                }
            }
            await BindViewBegForDefoliationList(model);
            SetFertiliserManureToSession(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Fertiliser Controller : Exception in Defoliation() action : {0}, {1}", ex.Message, ex.StackTrace);
            return BindErrorForDefoliationGet(model, ex.Message);
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
                await BindViewBegForDefoliationList(model);
                return View(model);
            }


            if (!model.NeedToShowSameDefoliationForAll || (model.IsSameDefoliationForAll.HasValue && !model.IsSameDefoliationForAll.Value))
            {
                for (int i = 0; i < model.DefoliationList.Count; i++)
                {
                    if (model.FieldID == model.DefoliationList[i].FieldID)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(model.DefoliationList[i].CropID);
                        if (error == null && crop != null && crop.DefoliationSequenceID != null)
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
                                    int filteredManId = managementPeriodList.Where(fm => model.UpdatedFertiliserIds.Any(mp => mp.ManagementPeriodId == fm.ID)).Select(x => x.ID.Value).FirstOrDefault();

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
                    if (error == null && managementPeriod != null)
                    {
                        (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                        if (error == null && crop != null && crop.DefoliationSequenceID != null)
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
                return RedirectToAction(_inOrgnaicManureDurationActionName);
            }
            model.GrassCropCount = model.DefoliationList.Count;
            SetFertiliserManureToSession(model);
            if (model.DefoliationCurrentCounter == model.DefoliationList.Count)
            {
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction(_inOrgnaicManureDurationActionName);
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
                    TempData["DefoliationError"] = error?.Message;
                }
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Fertiliser Controller : Exception in Defoliation() post action : {0}, {1}", ex.Message, ex.StackTrace);
            TempData["DefoliationError"] = ex.Message;
            return View(model);
        }
    }

    private async Task BindViewBegForDefoliationList(FertiliserManureViewModel model)
    {
        (List<SelectListItem> defoliationList, _) = await GetDefoliationList(model);
        if (defoliationList.Count > 0)
        {
            ViewBag.DefoliationList = defoliationList.Select(f => new SelectListItem
            {
                Value = f.Value,
                Text = f.Text.ToString()
            }).ToList();
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
            return RedirectToAction(_isSameDefoliationForAllActionName);
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



    private async Task<(bool flowControl, IActionResult? value)> RedirectForIsSameDefoliationForAll(FertiliserManureViewModel model, List<List<SelectListItem>> allDefoliations, List<List<string>> defoliationSequenceList)
    {
        List<string> commonDefoliations = defoliationSequenceList.Count > 0
        ? defoliationSequenceList.Aggregate((prev, next) => prev.Intersect(next).ToList())
        : new List<string>();
        if (commonDefoliations.Count > 0)
        {
            List<SelectListItem> flattenedList = allDefoliations.SelectMany(list => list).ToList();

            if (flattenedList.Count > 0)
            {
                model.NeedToShowSameDefoliationForAll = true;
            }
        }
        else
        {
            if (model.IsCheckAnswer && model.IsDoubleCropValueChange && (model.DefoliationList != null && model.FertiliserManures
            .Where(x => x.IsGrass).Select(x => x.FieldID).Any(fieldId => fieldId.HasValue && !model.DefoliationList.Select(d => d.FieldID)
            .Contains(fieldId.Value))))
            {
                var defoIds = model.DefoliationList
                .Select(d => d.FieldID)
                .ToList();


                model.FieldID = model.FertiliserManures
                    .Where(x => x.IsGrass)
                    .Select(x => x.FieldID)
                    .FirstOrDefault(fid => fid != null && !defoIds.Contains(fid.Value));
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;

                model.DefoliationCurrentCounter = model.DefoliationList.Count;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            }
            model.IsSameDefoliationForAll = false;
            model.NeedToShowSameDefoliationForAll = false;
            SetFertiliserManureToSession(model);
            return (flowControl: false, value: RedirectToAction(_defoliationActionName));
        }

        return (flowControl: true, value: null);
    }

    private async Task<List<List<SelectListItem>>> BindAllDefoliation(Error error, FertiliserManureViewModel model, List<FertiliserManureDataViewModel> fertiliserGrassList)
    {
        List<List<SelectListItem>> allDefoliations = new List<List<SelectListItem>>();
        foreach (var fertiliser in fertiliserGrassList)
        {
            (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(fertiliser.FieldID), model.HarvestYear.Value);
            bool isThisCropGrass = cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null);
            if (isThisCropGrass)
            {
                var cropId = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.ID.Value).FirstOrDefault();
                int? defoliationSequenceID = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.DefoliationSequenceID).FirstOrDefault();
                (List<ManagementPeriod> managementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);

                if (managementPeriod != null)
                {
                    List<int> defoliationList = managementPeriod.Select(x => x.Defoliation.Value).ToList();
                    (Crop? crop, error) = await _cropLogic.FetchCropById(cropId);
                    allDefoliations = await PrepairAllDefoiliationList(error, allDefoliations, defoliationSequenceID, defoliationList, crop);
                }
            }
        }

        return allDefoliations;
    }

    private async Task<List<List<SelectListItem>>> PrepairAllDefoiliationList(Error error, List<List<SelectListItem>> allDefoliations, int? defoliationSequenceID, List<int> defoliationList, Crop? crop)
    {
        if (crop != null && defoliationSequenceID != null)
        {
            (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
            if (defoliationSequence != null)
            {
                List<SelectListItem> allDefoliationWithName = CommonHelpers.BindAllDefoliationWithName(defoliationList, defoliationSequence);
                allDefoliations.Add(allDefoliationWithName);
            }
        }

        return allDefoliations;
    }

    [HttpGet]
    public async Task<IActionResult> IsSameDefoliationForAll()
    {
        _logger.LogTrace($"Fertiliser Controller : IsSameDefoliationForAll() action called");
        Error error = new Error();

        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        if (model == null)
        {
            _logger.LogError("Fertiliser Manure Controller : Session not found in IsSameDefoliationForAll() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        if (model.IsAnyChangeInSameDefoliationFlag)
        {
            model.IsAnyChangeInSameDefoliationFlag = false;
        }

        List<FertiliserManureDataViewModel> fertiliserGrassList = model.FertiliserManures.Where(x => x.IsGrass).ToList();
        List<List<SelectListItem>> allDefoliations = await BindAllDefoliation(error, model, fertiliserGrassList);

        if (allDefoliations.Count > 0)
        {
            List<List<string>> defoliationSequenceList = allDefoliations
        .Select(list => list.Select(item => item.Text).ToList())
        .ToList();


            (bool flowControl, IActionResult? value) = await RedirectForIsSameDefoliationForAll(model, allDefoliations, defoliationSequenceList);
            if (!flowControl && value != null)
            {
                return value;
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
            ModelState.AddModelError(_isSameDefoliationForAllActionName, Resource.MsgSelectAnOptionBeforeContinuing);
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
            if (model.IsSameDefoliationForAll != fertiliserManureViewModel.IsSameDefoliationForAll)
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



    private async Task<(bool flowControl, IActionResult? value)> PrepareDoubleCroppingList(FertiliserManureViewModel? model)
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
            (cropList, cropTypeName) = await BindDoubleCroppingListForGet(model, cropList, cropTypeName);
        }
        RemoveFieldFromDoubleCrop(model);
        (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID), model.HarvestYear.Value);
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            return (flowControl: false, value: BindErrorForDoubleCropping(model, error.Message));
        }
        await BindDoubleCropViewBeg(model, cropList);
        if (model.DoubleCropCurrentCounter == 0)
        {
            model.FieldID = model.DoubleCrop[0].FieldID;
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[0].FieldID)).Name;
        }

        return (flowControl: true, value: null);
    }

    private static void RemoveFieldFromDoubleCrop(FertiliserManureViewModel? model)
    {
        if (model.DoubleCrop != null && model.DoubleCrop.Count > 0 &&
        model.DoubleCrop.Any(dc => !model.FieldList.Contains(dc.FieldID.ToString())))
        {
            model.DoubleCrop?.RemoveAll(dc => !model.FieldList.Contains(dc.FieldID.ToString()));
        }
    }

    private async Task BindDoubleCropViewBeg(FertiliserManureViewModel? model, List<Crop> cropList)
    {
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

    private IActionResult BindErrorForDoubleCropping(FertiliserManureViewModel? model, string message)
    {
        if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation)
        {
            if (model.FieldList.Count > 0 && model.FieldList.Count == 1)
            {
                TempData[_nutrientRecommendationsError] = message;
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
            TempData[_fieldErrorTempDataKey] = message;
            return RedirectToAction(_fieldsActionName);
        }
        TempData[_fieldGroupErrorTempDataKey] = message;
        return RedirectToAction(_fieldGroupActionName);
    }

    private async Task<(List<Crop> cropList, string cropTypeName)> BindDoubleCroppingListForGet(FertiliserManureViewModel? model, List<Crop> cropList, string cropTypeName)
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

            (cropList, _) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId, model.HarvestYear.Value);
            if (cropList != null && cropList.Count == 2)
            {
                var cropTypeId = cropList[0]?.CropTypeID;
                if (cropTypeId.HasValue)
                {
                    cropTypeName = await _fieldLogic.FetchCropTypeById(cropTypeId.Value);
                    var field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                    var doubleCrop = new DoubleCrop
                    {
                        CropName = cropTypeName,
                        CropOrder = cropList[0].CropOrder ?? 1,
                        FieldID = fieldId,
                        FieldName = field.Name ?? string.Empty,
                        EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                        Counter = counter,
                    };

                    model.DoubleCrop.Add(doubleCrop);
                    counter++;
                }
            }
        }

        return (cropList, cropTypeName);
    }


    [HttpGet]
    public async Task<IActionResult> DoubleCrop(string q)
    {
        _logger.LogTrace("Fertiliser Manure Controller : DoubleCrop({0}) action called", q);
        FertiliserManureViewModel? model = GetFertiliserManureFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Fertiliser Manure Controller : Session not found in DoubleCrop() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (_fertiliserManureLogic.IsInitialLoadAfterFieldChange(model, q))
            {
                model.DoubleCropCurrentCounter = 0;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
                SetFertiliserManureToSession(model);
            }
            else if (_fertiliserManureLogic.IsRedirectWithDoubleCropData(model, q))
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

                    return BackActionForInOrganicAndDoubleCrop(model);
                }
                model.FieldID = model.DoubleCrop[index].FieldID;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[index].FieldID)).Name;
                model.DoubleCropCurrentCounter = index;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
            }
            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                (bool flowControl, IActionResult value) = await PrepareDoubleCroppingList(model);
                if (!flowControl && value != null)
                {
                    return value;
                }
            }

            SetFertiliserManureToSession(model);
        }
        catch (Exception ex)
        {
            return BindErrorForDoubleCropping(model, ex.Message);
        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoubleCrop(FertiliserManureViewModel model)
    {
        _logger.LogTrace("Fertiliser Manure Controller : DoubleCrop() post action called");

        ValidateDoubleCropSelection(model);

        try
        {
            if (!ModelState.IsValid)
                return await HandleInvalidModel(model);

            var sessionModel = GetFertiliserManureFromSession() ?? new FertiliserManureViewModel();

            await UpdateSelectedCrop(model);
            await UpdateManagementPeriod(model);

            await HandleDefoliationRemoval(model);
            await MoveToNextField(model);

            PersistDoubleCropState(model);

            await ProcessGrassAndCounters(model, sessionModel);

            return HandleNavigation(model);
        }
        catch (Exception ex)
        {
            TempData["DoubleCropError"] = ex.Message;
            return View(model);
        }
    }
    private void ValidateDoubleCropSelection(FertiliserManureViewModel model)
    {
        if (model.DoubleCrop?[model.DoubleCropCurrentCounter]?.CropID == null ||
            model.DoubleCrop[model.DoubleCropCurrentCounter].CropID == 0)
        {
            ModelState.AddModelError(
                $"DoubleCrop[{model.DoubleCropCurrentCounter}].CropID",
                Resource.MsgSelectAnOptionBeforeContinuing);
        }
    }
    private async Task<IActionResult> HandleInvalidModel(FertiliserManureViewModel model)
    {
        if (model.FieldList != null && model.HarvestYear.HasValue)
        {
            var (cropList, error) =
                await _cropLogic.FetchCropPlanByFieldIdAndYear(
                    Convert.ToInt32(model.DoubleCrop?[model.DoubleCropCurrentCounter].FieldID),
                    model.HarvestYear.Value);

            if (!string.IsNullOrWhiteSpace(error?.Message))
                TempData["DoubleCropError"] = error.Message;

            model.DoubleCrop ??= new List<DoubleCrop>();

            await BindDoubleCropViewBeg(model, cropList);
        }

        return View(model);
    }
    private async Task UpdateSelectedCrop(FertiliserManureViewModel model)
    {
        if (!model.DoubleCrop.Any(x => x.FieldID == model.FieldID))
            return;

        var cropList = await _cropLogic.FetchCropsByFieldId(model.FieldID.Value);
        cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();

        if (cropList.Count != 2) return;

        var selected = cropList
            .Where(x => x.ID == model.DoubleCrop[model.DoubleCropCurrentCounter].CropID)
            .ToList();

        if (!selected.Any()) return;

        var crop = selected[0];

        model.DoubleCrop[model.DoubleCropCurrentCounter].CropOrder = crop.CropOrder.Value;
        model.DoubleCrop[model.DoubleCropCurrentCounter].CropName =
            await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
    }
    private async Task UpdateManagementPeriod(FertiliserManureViewModel model)
    {
        if (model.DoubleCrop.Count == 0) return;

        var (periods, _) =
            await _cropLogic.FetchManagementperiodByCropId(
                model.DoubleCrop[model.DoubleCropCurrentCounter].CropID, true);

        if (periods == null || periods.Count == 0) return;

        var periodId = periods.Select(x => x.ID.Value).First();
        SetManagementPeriodInFert(model, periodId);
    }

    private static void SetManagementPeriodInFert(FertiliserManureViewModel model, int periodId)
    {
        foreach (var fert in model.FertiliserManures)
        {
            if (fert.FieldID != model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID)
                continue;

            if (model.IsCheckAnswer && !string.IsNullOrWhiteSpace(model.EncryptedFertId)
                && model.UpdatedFertiliserIds != null)
            {
                foreach (var item in model.UpdatedFertiliserIds)
                {
                    if (fert.FieldName == item.Name)
                    {
                        item.ManagementPeriodId = periodId;
                        break;
                    }
                }
            }

            fert.ManagementPeriodID = periodId;
            break;
        }
    }

    private async Task HandleDefoliationRemoval(FertiliserManureViewModel model)
    {
        var (crop, _) = await _cropLogic.FetchCropById(
            model.DoubleCrop[model.DoubleCropCurrentCounter].CropID);

        if (crop == null ||
            crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
            return;

        if (model.DefoliationList == null) return;

        int fieldId = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID;

        model.DefoliationList.RemoveAll(x => x.FieldID == fieldId);
    }
    private async Task MoveToNextField(FertiliserManureViewModel model)
    {
        for (int i = 0; i < model.DoubleCrop.Count; i++)
        {
            if (model.FieldID != model.DoubleCrop[i].FieldID) continue;

            model.DoubleCropCurrentCounter++;

            if (i + 1 < model.DoubleCrop.Count)
            {
                model.FieldID = model.DoubleCrop[i + 1].FieldID;
                model.FieldName =
                    (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
            }

            break;
        }
    }
    private void PersistDoubleCropState(FertiliserManureViewModel model)
    {
        model.DoubleCropEncryptedCounter =
            _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());

        SetFertiliserManureToSession(model);
    }
    private async Task ProcessGrassAndCounters(FertiliserManureViewModel model, FertiliserManureViewModel sessionModel)
    {
        if (!(model.IsCheckAnswer || model.DoubleCrop.Count == model.DoubleCropCurrentCounter))
            return;

        int counter = 0;

        foreach (var cropId in model.DoubleCrop.Where(x => x.CropID > 0).Select(x => x.CropID))
        {
            var (crop, _) = await _cropLogic.FetchCropById(cropId);

            if (crop == null || model.FertiliserManures == null)
                continue;

            int index = model.FertiliserManures
                .FindIndex(f => f.FieldID == crop.FieldID);

            if (index < 0) continue;

            if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                model.FertiliserManures[index].IsGrass = true;
                model.IsAnyCropIsGrass = true;
                counter++;
            }
            else if (model.FertiliserManures[index].IsGrass)
            {
                model.FertiliserManures[index].IsGrass = false;
                model.FertiliserManures[index].Defoliation = null;
                model.FertiliserManures[index].DefoliationName = null;
            }
        }

        model.IsAnyCropIsGrass = model.FertiliserManures?.Any(x => x.IsGrass) ?? false;
        model.GrassCropCount = model.FertiliserManures?.Count(x => x.IsGrass) ?? counter;

        HandleDoubleCropChangeFlags(model, sessionModel);
    }
    private static void HandleDoubleCropChangeFlags(FertiliserManureViewModel model, FertiliserManureViewModel oldModel)
    {
        if (!model.IsCheckAnswer || oldModel?.DoubleCrop == null)
            return;

        var newItem = model.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID);
        var oldItem = oldModel.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID);

        if (newItem != null && oldItem != null && newItem.CropOrder != oldItem.CropOrder)
        {
            model.IsDoubleCropValueChange = true;
        }
    }
    private IActionResult HandleNavigation(FertiliserManureViewModel model)
    {
        if (ShouldGoToCheckAnswer(model))
            return RedirectToAction(_checkAnswerActionName);

        if (!model.IsAnyCropIsGrass.GetValueOrDefault())
            return RedirectToAction(_inOrgnaicManureDurationActionName);

        if (model.GrassCropCount > 1)
        {
            if (model.FertiliserManures.Any(x => x.IsGrass && x.Defoliation == null))
                model.IsSameDefoliationForAll = null;

            return RedirectToAction(_isSameDefoliationForAllActionName);
        }

        return RedirectToAction(_defoliationActionName);
    }
    private static bool ShouldGoToCheckAnswer(FertiliserManureViewModel model)
    {
        return model.IsCheckAnswer &&
               !model.IsAnyChangeInField &&
               (!model.IsAnyCropIsGrass.HasValue || !model.IsAnyCropIsGrass.Value ||
                (model.DefoliationList != null &&
                 model.FertiliserManures
                     .Where(x => x.IsGrass)
                     .Select(x => x.FieldID)
                     .All(id => model.DefoliationList.Select(d => d.FieldID)
                     .Contains(id.Value))));
    }
    private async Task<List<WarningMessage>> GetWarningMessages(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure)
    {
        List<WarningMessage> warningMessages = new List<WarningMessage>();
        try
        {
            if (model != null && model.N > 0 && model.FertiliserManures != null && model.FertiliserManures.Count > 0)
            {
                (ManagementPeriod? managementPeriod, _) = await _cropLogic.FetchManagementperiodById(fertiliserManure.ManagementPeriodID);

                ClosedPeriodWarning(model, fertiliserManure, warningMessages, managementPeriod);
                NitrogenExceedWarning(model, fertiliserManure, warningMessages, managementPeriod);
                NMaxLimitWarning(model, fertiliserManure, warningMessages, managementPeriod);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "OrganicManure Controller : Exception in GetWarningMessages() method : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
        }
        return warningMessages;
    }

    private static void NMaxLimitWarning(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure, List<WarningMessage> warningMessages, ManagementPeriod? managementPeriod)
    {
        if (model.IsNMaxLimitWarning)
        {
            WarningMessage warningMessage = new WarningMessage();

            warningMessage.FieldID = fertiliserManure.FieldID ?? 0;
            warningMessage.CropID = managementPeriod == null ? 0 : managementPeriod.CropID ?? 0;
            warningMessage.JoiningID = null;
            warningMessage.WarningLevelID = model.CropNmaxLimitWarningLevelID;
            warningMessage.WarningCodeID = model.CropNmaxLimitWarningCodeID;
            warningMessage.Header = model.CropNmaxLimitWarningHeader;
            warningMessage.Para1 = model.CropNmaxLimitWarningPara1;
            warningMessage.Para2 = model.CropNmaxLimitWarningPara2;
            warningMessage.Para3 = model.CropNmaxLimitWarningPara3;
            warningMessages.Add(warningMessage);
        }
    }

    private static void ClosedPeriodWarning(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure, List<WarningMessage> warningMessages, ManagementPeriod? managementPeriod)
    {
        if (!string.IsNullOrWhiteSpace(model.ClosedPeriodWarningPara1))
        {
            WarningMessage warningMessage = new WarningMessage();
            warningMessage.FieldID = fertiliserManure.FieldID ?? 0;
            warningMessage.CropID = managementPeriod == null ? 0 : managementPeriod.CropID ?? 0;
            warningMessage.JoiningID = null;
            warningMessage.WarningLevelID = model.ClosedPeriodWarningLevelID;
            warningMessage.WarningCodeID = model.ClosedPeriodWarningCodeID;
            warningMessage.Header = model.ClosedPeriodWarningHeader;
            warningMessage.Para1 = model.ClosedPeriodWarningPara1;
            warningMessage.Para2 = null;
            warningMessage.Para3 = model.ClosedPeriodWarningPara3;
            warningMessages.Add(warningMessage);
        }
    }

    private static void NitrogenExceedWarning(FertiliserManureViewModel model, FertiliserManureDataViewModel fertiliserManure, List<WarningMessage> warningMessages, ManagementPeriod? managementPeriod)
    {
        if (model.IsNitrogenExceedWarning)
        {
            WarningMessage warningMessage = new WarningMessage();
            warningMessage.FieldID = fertiliserManure.FieldID ?? 0;
            warningMessage.CropID = managementPeriod == null ? 0 : managementPeriod.CropID ?? 0;
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
        Error? error = null;
        (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(defoliationSequenceID);
        if (error == null && defoliationSequence != null)
        {
            string description = defoliationSequence.DefoliationSequenceDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                selectedDefoliation = CommonHelpers.BindDefoliationName(defoliation, description);
            }
        }
        return (selectedDefoliation, error);
    }

    private FertiliserManureViewModel? GetFertiliserManureBeforeUpdateFromSession()
    {
        if (HttpContext.Session.Exists(_fertiliserManureBeforeUpdateSessionKey))
        {
            return HttpContext.Session.GetObjectFromJson<FertiliserManureViewModel>(_fertiliserManureBeforeUpdateSessionKey);
        }
        return null;
    }
    private void SetFertiliserManureBeforeUpdateToSession(FertiliserManureViewModel fertiliserManureViewModel)
    {
        HttpContext.Session.SetObjectAsJson(_fertiliserManureBeforeUpdateSessionKey, fertiliserManureViewModel);
    }
    private async Task<FertiliserManureViewModel> ApplyGrassCropLogicAsync(FertiliserManureViewModel model, List<HarvestYearPlanResponse>? cropPlans)
    {
        int grassCropCounter = 0;
        foreach (var field in model.FieldList)
        {
            List<HarvestYearPlanResponse> cropList = cropPlans.Where(x => x.FieldID == Convert.ToInt32(field)).ToList();

            if (cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                (List<ManagementPeriod> managementPeriod, _) = await _cropLogic.FetchManagementperiodByCropId(cropList.Select(x => x.CropID).FirstOrDefault(), false);

                if (model.FertiliserManures != null)
                {
                    var filteredFertiliserManure = model.FertiliserManures
                    .Where(fm => managementPeriod.Any(mp => mp.ID == fm.ManagementPeriodID) &&
                    fm.Defoliation == null).ToList();
                    if (filteredFertiliserManure.Any() && filteredFertiliserManure.Count == managementPeriod.Count)
                    {
                        model = RemoveListItem(model, managementPeriod);
                    }
                }
                grassCropCounter++;
                model.IsAnyCropIsGrass = true;
            }
        }
        model.GrassCropCount = grassCropCounter;
        return model;
    }
    private async Task<(FertiliserManureViewModel, FertiliserManureViewModel?)> BuildFertiliserManureList(List<int> managementIds, FertiliserManureViewModel model, FertiliserManureViewModel? fertiliserManureViewModel, List<HarvestYearPlanResponse> cropPlans)
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
                if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value && fertiliserManureViewModel != null && fertiliserManureViewModel.FertiliserManures != null && fertiliserManureViewModel.FertiliserManures.Count > 0)
                {
                    fertiliserManure = await BindGrassData(fertiliserManureViewModel.FertiliserManures, fertiliserManure, cropPlans, manIds);
                }
                model.FertiliserManures.Add(fertiliserManure);
            }
            model.DefoliationCurrentCounter = 0;
        }
        return (model, fertiliserManureViewModel);
    }
    private async Task<FertiliserManureDataViewModel> BindGrassData(List<FertiliserManureDataViewModel> fertiliserManureViewModel, FertiliserManureDataViewModel fertiliserManure, List<HarvestYearPlanResponse> cropPlans, int manIds)
    {
        for (int i = 0; i < fertiliserManureViewModel.Count; i++)
        {
            if (fertiliserManureViewModel[i].ManagementPeriodID == manIds)
            {
                fertiliserManure.Defoliation = fertiliserManureViewModel[i].Defoliation;
                if (fertiliserManure.Defoliation != null)
                {
                    Error? error = null;
                    fertiliserManure = await BindDefoliationName(cropPlans, manIds, error, fertiliserManure);
                }
            }
        }
        return fertiliserManure;
    }

    private async Task<FertiliserManureDataViewModel> BindDefoliationName(List<HarvestYearPlanResponse> cropPlans, int manIds, Error? error, FertiliserManureDataViewModel fertiliserManure)
    {
        (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(manIds);
        if (error == null && managementPeriod != null)
        {
            HarvestYearPlanResponse? crop = cropPlans.FirstOrDefault(x => x.CropID == managementPeriod.CropID);
            if (crop != null && crop.DefoliationSequenceID != null)
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
        return fertiliserManure;
    }
    private async Task<string?> GetClosedPeriodAsync(FertiliserManureViewModel model, int cropTypeId, int nvzProgrammeId, int harvestYear)
    {
        Error? error;

        (string? closedPeriod, error) = await _fertiliserManureLogic.FetchFertiliserManureClosedPeriod(model.FarmCountryId ?? 0, cropTypeId, nvzProgrammeId);

        if (error != null || string.IsNullOrWhiteSpace(closedPeriod))
            return null;

        Regex regex = new(_pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

        Match match = regex.Match(closedPeriod);
        if (!match.Success)
            return null;

        Dictionary<int, string> dtfi;
        WarningWithinPeriod.BindDatesForWarning(match, out int startDay, out int endDay, out dtfi, out int startMonth, out int endMonth);

        DateTime startDate;
        DateTime endDate;

        if (startMonth <= endMonth)
        {
            startDate = new DateTime(harvestYear - 1, startMonth, startDay, 0, 0, 0, DateTimeKind.Unspecified);
            endDate = new DateTime(harvestYear - 1, endMonth, endDay, 0, 0, 0, DateTimeKind.Unspecified);
        }
        else
        {
            startDate = new DateTime(harvestYear - 1, startMonth, startDay, 0, 0, 0, DateTimeKind.Unspecified);
            endDate = new DateTime(harvestYear, endMonth, endDay, 0, 0, 0, DateTimeKind.Unspecified);
        }

        return $"{startDate:d MMMM yyyy} to {endDate:d MMMM yyyy}";
    }
    private async Task SetClosedPeriodAndNVZAsync(FertiliserManureViewModel model)
    {
        if (model.FertiliserManures == null || model.FertiliserManures.Count == 0)
            return;

        foreach (var fertiliser in model.FertiliserManures)
        {
            var field = await GetFieldAsync(fertiliser.FieldID);
            if (field == null) continue;

            var crop = await GetCropAsync(fertiliser.ManagementPeriodID);
            if (crop?.CropTypeID == null) continue;

            await SetClosedPeriodIfApplicable(model, crop.CropTypeID.Value, field);

            if (field.IsWithinNVZ == true)
                model.IsWithinNVZ = true;
        }
    }
    private async Task<Field?> GetFieldAsync(int? fieldId)
    {
        if (fieldId == null) return null;

        return await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
    }
    private async Task<Crop?> GetCropAsync(int managementPeriodId)
    {
        var (managementPeriod, error) =
            await _cropLogic.FetchManagementperiodById(managementPeriodId);

        if (error != null || managementPeriod?.CropID == null)
            return null;

        var (crop, cropError) =
            await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

        if (cropError != null)
            return null;

        return crop;
    }
    private async Task SetClosedPeriodIfApplicable(
    FertiliserManureViewModel model,
    int cropTypeId,
    Field field)
    {
        var (cropTypeLinkingResponse, error) =
            await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(cropTypeId);

        if (error != null || cropTypeLinkingResponse.NMaxLimitEngland == 0)
            return;

        string? closedPeriod = await GetClosedPeriodAsync(
            model,
            cropTypeId,
            field.NVZProgrammeID ?? 0,
            model.HarvestYear ?? 0);

        if (!string.IsNullOrWhiteSpace(closedPeriod))
        {
            ViewBag.ClosedPeriod = closedPeriod;
        }

    }
    private void ClearTempErrors(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (TempData[key] != null)
            {
                TempData[key] = null;
            }
        }
    }
    private void PopulateFieldNames(FertiliserManureViewModel model, List<CommonResponse> fieldList)
    {
        var names = fieldList
            .Where(f => model.FieldList.Contains(f.Id.ToString()))
            .OrderBy(f => f.Name)
            .Select(f => f.Name)
            .ToList();

        if (names.Count == 1)
        {
            model.FieldName = names[0];
        }
        else
        {
            ViewBag.SelectedFields = names;
            model.FieldName = string.Empty;
        }
    }

}