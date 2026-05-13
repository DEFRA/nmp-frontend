using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Execution;
using Newtonsoft.Json;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Helpers;
using Parlot.Fluent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Commons.ServiceResponses.Error;
namespace NMP.Portal.Controllers;

[Authorize]
public class CropController(ILogger<CropController> logger, IDataProtectionProvider dataProtectionProvider,
     IFarmLogic farmLogic, IFieldLogic fieldLogic, ICropLogic cropLogic,
     IPreviousCroppingLogic previousCroppingLogic, IMannerLogic mannerLogic) : Controller
{
    private readonly ILogger<CropController> _logger = logger;
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
    private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
    private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
    private readonly IFarmLogic _farmLogic = farmLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IPreviousCroppingLogic _previousCroppingLogic = previousCroppingLogic;
    private readonly IMannerLogic _mannerLogic = mannerLogic;
    private const string _cropInfoTwoActionName = "CropInfoTwo";
    private const string _cropDataSessionKey = "CropData";
    private const string _plansAndRecordsOverviewActionName = "PlansAndRecordsOverview";
    private const string _cropGroupsActionName = "CropGroups";
    private const string _farmSummaryActionName = "FarmSummary";
    private const string _harvestYearOverviewActionName = "HarvestYearOverview";
    private const string _tempDataErrorKey = "Error";
    private const string _checkAnswerActionName = "CheckAnswer";
    private const string _harvestYearForPlanActionName = "HarvestYearForPlan";
    private const string _cropDataBeforeUpdateSessionKey = "CropDataBeforeUpdate";
    private const string _defoliationActionName = "Defoliation";
    private const string _cropTypeTempErrorName = "CropTypeError";
    private const string _cropPrefix = "Crops[";
    private const string _yieldPrefix = "].Yield";
    private const string _grassGrowthClassActionName = "GrassGrowthClass";
    private const string _copyCheckAnswerActionName = "CopyCheckAnswer";
    private PlanViewModel? GetCropFromSession()
    {
        if (HttpContext.Session.Exists(_cropDataSessionKey))
        {
            return HttpContext.Session.GetObjectFromJson<PlanViewModel>(_cropDataSessionKey);
        }
        return null;
    }

    private void SetCropToSession(PlanViewModel plan)
    {
        HttpContext.Session.SetObjectAsJson(_cropDataSessionKey, plan);
    }

    private void RemoveCropSession()
    {
        if (HttpContext.Session.Exists(_cropDataSessionKey))
        {
            HttpContext.Session.Remove(_cropDataSessionKey);
        }
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogTrace("Crop Controller : Index() action called");
        return await Task.FromResult(View());
    }

    public async Task<IActionResult> CreateCropPlanCancel(string q)
    {
        _logger.LogTrace("Crop Controller : CreateCropPlanCancel({Q}) action called", q);
        RemoveCropSession();

        if (!string.IsNullOrWhiteSpace(q))
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            List<PlanSummaryResponse> planSummaryResponse = await _cropLogic.FetchPlanSummaryByFarmId(farmId, 0);

            if (planSummaryResponse.Count > 0)
            {
                return await Task.FromResult(RedirectToAction(_plansAndRecordsOverviewActionName, "Crop", new { id = q }));
            }
        }
        return await Task.FromResult(RedirectToAction(_farmSummaryActionName, "Farm", new { Id = q }));
    }



    [HttpGet]
    [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
    public async Task<IActionResult> HarvestYearForPlan(string q, string? year, bool? isPlanRecord)
    {
        _logger.LogTrace("Crop Controller : HarvestYearForPlan({Q}, {Year}, {IsPlanRecord}) action called", q, year, isPlanRecord);

        if (IsMissingParameters(q, year))
        {
            return HandleBadRequest();
        }

        var model = GetCropFromSession() ?? new PlanViewModel();

        try
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                await InitializeFarmContextAsync(model, q);

                if (!string.IsNullOrWhiteSpace(year))
                {
                    return HandleHarvestYearSelection(model, year, isPlanRecord);
                }

                SetCropToSession(model);
            }

            return await ResolveNavigationAsync(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in HarvestYearForPlan() action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_tempDataErrorKey] = ex.Message;
            return RedirectToAction(_farmSummaryActionName, "Farm", new { id = q });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult HarvestYearForPlan(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : HarvestYearForPlan() action posted");

        if (!model.Year.HasValue)
        {
            ModelState.AddModelError("Year", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblYear.ToLower()));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        SetCropToSession(model);

        if (model.IsCheckAnswer)
        {
            for (int i = 0; i < model.Crops?.Count; i++)
            {
                model.Crops[i].Year = model.Year ?? 0;
            }

            SetCropToSession(model);

            return RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction(_cropGroupsActionName);
    }

    private IActionResult HandleHarvestYearSelection(
    PlanViewModel model,
    string encryptedYear,
    bool? isPlanRecord)
    {
        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedYear));

        model.IsAddAnotherCrop = isPlanRecord != true;
        model.IsPlanRecord = isPlanRecord == true;

        SetCropToSession(model);
        return RedirectToAction(_cropGroupsActionName);
    }

    private async Task<IActionResult> ResolveNavigationAsync(PlanViewModel model)
    {
        var plans = await _cropLogic.FetchPlanSummaryByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), 0);

        if (!plans.Any() && model.IsPlanRecord != true)
        {
            return RedirectToAction(_farmSummaryActionName, "Farm", new { id = model.EncryptedFarmId });
        }

        if (model.IsPlanRecord == true)
        {
            return RedirectToAction(_plansAndRecordsOverviewActionName, "Crop",
                new
                {
                    id = model.EncryptedFarmId,
                    year = _farmDataProtector.Protect(plaintext: model.Year.ToString())
                });
        }

        if (model.IsAddAnotherCrop)
        {
            return RedirectToAction(_harvestYearOverviewActionName, "Crop",
                new
                {
                    id = model.EncryptedFarmId,
                    year = _farmDataProtector.Protect(model.Year.ToString())
                });
        }

        return View("HarvestYearForPlan", model);
    }

    private async Task InitializeFarmContextAsync(PlanViewModel model, string encryptedFarmId)
    {
        int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedFarmId));
        model.EncryptedFarmId = encryptedFarmId;

        var (farm, _) = await _farmLogic.FetchFarmByIdAsync(farmId);
        model.FarmRB209CountryID = farm?.RB209CountryID;
    }

    private IActionResult HandleBadRequest()
    {
        _logger.LogError("Crop Controller : Parameter missing in HarvestYearForPlan() action");
        return Functions.RedirectToErrorHandler((int)HttpStatusCode.BadRequest);
    }

    private static bool IsMissingParameters(string q, string? year)
    => string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(year);


    [HttpGet]
    public async Task<IActionResult> CropGroups()
    {
        _logger.LogTrace("Crop Controller : CropGroups() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in HarvestYearForPlan() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (FarmResponse? farm, _) = await _farmLogic.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            model.FarmRB209CountryID = farm?.RB209CountryID;
            model.CountryId = farm?.CountryID;
            if (model.FarmRB209CountryID.HasValue)
            {
                ViewBag.CropGroupList =
                    await GetCropGroups(model.FarmRB209CountryID.Value);
            }

            if (model.IsCropGroupChange)
            {
                model.IsCropGroupChange = false;
                SetCropToSession(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CropGroups() action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYear"] = ex.Message;
            return RedirectToAction(_harvestYearForPlanActionName);
        }

        return View(model);
    }

    private async Task<List<CropGroupResponse>> GetCropGroups(int farmRB209CountryID)
    {
        List<CropGroupResponse> cropGroups = await _fieldLogic.FetchCropGroups();
        var cropGroupsList = cropGroups.Where(x => x.CountryId == farmRB209CountryID || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).ToList();
        return cropGroupsList.OrderBy(c => c.CropGroupName).ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropGroups(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropGroups() action posted");
        try
        {
            if (model.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                ViewBag.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                if (model.FarmRB209CountryID.HasValue)
                {
                    ViewBag.CropGroupList =
                        await GetCropGroups(model.FarmRB209CountryID.Value);
                }
                return View(model);
            }

            PlanViewModel? CropData = GetCropFromSession();

            if (CropData == null)
            {
                _logger.LogError("Crop Controller : Session not found in CropGroups() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            else
            {
                if (CropData.CropGroupId != model.CropGroupId)
                {
                    model.CropType = string.Empty;
                    model.CropTypeID = null;
                    model.CropInfo1 = null;
                    model.CropInfo2 = null;
                    model.CropInfo1Name = null;
                    model.CropInfo2Name = null;
                    model.IsCropGroupChange = true;
                    SetCropToSession(model);
                }
                else if ((CropData.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ||
                            CropData.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other)
                            && model.IsCheckAnswer && (!model.IsCropGroupChange))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }

            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other)
            {
                model.CropInfo1 = null;
                model.CropInfo2 = null;
                model.CropInfo1Name = null;
                model.CropInfo2Name = null;
            }
            else
            {
                model.OtherCropName = null;
            }

            if (model.CropGroupId != null)
            {
                model.CropGroup = await _fieldLogic.FetchCropGroupById(model.CropGroupId.Value);
            }


            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CropGroups() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropGroupError"] = ex.Message;
            return View(model);
        }

        return await BindPropertiesForGrass(model);
    }

    private async Task<PlanViewModel> BindCropTypeId(PlanViewModel model)
    {
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            model.CropTypeID = await _cropLogic.FetchCropTypeByGroupId(model.CropGroupId ?? 0);
        }
        return model;
    }
    private async Task<IActionResult> BindPropertiesForGrass(PlanViewModel model)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass && model.CropTypeID.HasValue)
        {
            model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
        }
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            model.CropType = Resource.lblGrass;
            model = await BindCropTypeId(model);

            //Fetch fields allowed for second crop based on first crop
            if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
                List<SelectListItem> selectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();


                (List<HarvestYearPlanResponse> harvestYearPlanResponse, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                (model, selectListItem, List<int> fieldsAllowedForSecondCrop) = await BindCropOrder(model, selectListItem, harvestYearPlanResponse);
                bool success = RedirectCropGroupWitherror(model, harvestYearPlanResponse, selectListItem, fieldsAllowedForSecondCrop);
                if (!success)
                {
                    return RedirectToAction(_cropGroupsActionName);
                }
            }

            SetCropToSession(model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == true)
            {
                return RedirectToAction("CropGroupName");
            }
            return RedirectToAction("CropFields");
        }
        model = ResetGrassProperties(model);

        return RedirectToAction("CropTypes");
    }

    private bool RedirectCropGroupWitherror(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<SelectListItem> selectListItem, List<int> fieldsAllowedForSecondCrop)
    {
        bool success = true;
        if (model.CropTypeID != null)
        {
            SetCropToSession(model);
            if (harvestYearPlanResponse.Count > 0)
            {
                var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                if (selectListItem.Count == 0)
                {
                    TempData["CropGroupError"] = Resource.lblNoFieldsAreAvailable;
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                    success = false;

                }
            }
        }
        return success;
    }
    private PlanViewModel ResetGrassProperties(PlanViewModel model)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            model.CurrentSward = null;
            model.GrassSeason = null;
            model.GrassGrowthClassCounter = 0;
            model.GrassGrowthClassEncryptedCounter = null;
            model.GrassGrowthClassQuestion = null;
            model.DryMatterYieldCounter = 0;
            model.DryMatterYieldEncryptedCounter = null;
            model.SwardTypeId = null;
            model.SwardManagementId = null;
            model.PotentialCut = null;
            model.DefoliationSequenceId = null;
            SetCropToSession(model);
        }
        return model;
    }

    private async Task<(PlanViewModel, List<SelectListItem>, List<int>)> BindCropOrder(PlanViewModel model, List<SelectListItem> selectListItem, List<HarvestYearPlanResponse> harvestYearPlanResponse)
    {
        List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse;

        (List<int> fieldsAllowedForSecondCrop, _) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, model, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate), model.Crops, model.FarmRB209CountryID ?? 3);
        PlanViewModel? cropData = GetCropFromSession();

        model.IsCropTypeChange = model.IsCropGroupChange || (cropData != null && cropData.CropTypeID != model.CropTypeID);

        if (harvestYearPlanResponse.Count > 0 || selectListItem.Count == 1)
        {
            var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
            selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
            if (!model.IsCheckAnswer && model.Crops != null && model.Crops.Count > 0)
            {
                foreach (var crop in model.Crops.Where(c => c.FieldID.HasValue))
                {
                    crop.CropOrder = fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) ? 2 : 1;
                }
            }

        }
        return (model, selectListItem, fieldsAllowedForSecondCrop);
    }
    [HttpGet]
    public async Task<IActionResult> CropTypes()
    {
        _logger.LogTrace("Crop Controller : CropTypes() action called");

        try
        {
            PlanViewModel? model = GetCropFromSession();
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in CropTypes() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            ViewBag.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            await FetchCroptypes(model);

            model.IsCropTypeChange = false;
            SetCropToSession(model);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CropTypes() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropGroupError"] = ex.Message;
            return RedirectToAction(_cropGroupsActionName);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropTypes(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropTypes() post action called");
        try
        {
            model = await BindCropTypeId(model);

            ValidateCropTypes(model);
            if (!ModelState.IsValid)
            {
                ViewBag.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                await FetchCroptypes(model);
                return View(model);
            }

            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<Field> fieldList = new List<Field>(allFields);
            Error? error = null;
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse;
            //filter list if update
            (fieldList, harvestYearPlanResponse) = await BindFieldList(fieldList, model, harvestYearPlanResponse);

            List<SelectListItem> selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();

            PlanViewModel? cropData = GetCropFromSession();
            SetCropToSession(model);
            List<int> fieldsAllowedForSecondCrop = new List<int>();


            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForUpdate(model, selectListItem, harvestYearPlanResponse, cropPlanForFirstCropFilter, allFields);
            }
            else
            {
                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForInsert(model, selectListItem, harvestYearPlanResponse, cropPlanForFirstCropFilter);
            }


            model = BindCropOrder(model, cropData, harvestYearPlanResponse, selectListItem, fieldsAllowedForSecondCrop);
            SetCropToSession(model);
            var result = await ValidateSecondCrop(model, harvestYearPlanResponse, fieldsAllowedForSecondCrop, selectListItem);

            if (result != null)
            {
                return RedirectToAction("CropTypes");
            }

            return await RedirectForCropType(model, cropData, harvestYearPlanResponse, fieldsAllowedForSecondCrop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CropTypes() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_cropTypeTempErrorName] = ex.Message;
            return View(model);
        }
    }

    private async Task<(List<SelectListItem>, List<int>)> FilterSelectListItemForFieldForInsert(PlanViewModel model, List<SelectListItem> selectListItem, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<HarvestYearPlanResponse> cropPlanForFirstCropFilter)
    {
        //Fetch fields allowed for second crop based on first crop
        (List<int> fieldsAllowedForSecondCrop, _) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, model, !string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate), model.Crops, model.FarmRB209CountryID ?? 3);

        if (harvestYearPlanResponse.Count > 0 || selectListItem.Count == 1)
        {
            var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
            selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
        }
        return (selectListItem, fieldsAllowedForSecondCrop);
    }
    private static async Task<(List<Field>, List<HarvestYearPlanResponse>)> BindFieldList(List<Field> fieldList, PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter)
    {
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
        {
            harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
            fieldList = FilterFieldList(fieldList, harvestYearPlanResponseForFilter);
        }
        return (fieldList, harvestYearPlanResponseForFilter);
    }
    private async Task FetchCroptypes(PlanViewModel model)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other)
        {
            List<CropTypeResponse> cropTypes = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryID);
            ViewBag.CropTypeList = cropTypes;
        }
    }
    private void ValidateCropTypes(PlanViewModel model)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Potatoes && model.CropTypeID == null)
        {
            ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
        }
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Potatoes && model.CropTypeID == null)
        {
            ModelState.AddModelError("CropTypeID", Resource.MsgSelectAPotatoVarietyGroup);
        }
        //Other crop validation
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other && model.OtherCropName == null)
        {
            ModelState.AddModelError("OtherCropName", string.Format(Resource.lblEnterTheCropName, Resource.lblCropType.ToLower()));
        }
    }
    private async Task<IActionResult?> ValidateSecondCrop(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<int> fieldsAllowedForSecondCrop, List<SelectListItem> selectListItem)
    {
        if (model.CropTypeID != null)
        {
            var result = await FetchNoFieldsAreAvailable(model, harvestYearPlanResponse, fieldsAllowedForSecondCrop, selectListItem);
            if (result != null)
            {
                return RedirectToAction("CropTypes");
            }
        }
        return null;
    }
    private async Task<IActionResult?> FetchNoFieldsAreAvailable(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<int> fieldsAllowedForSecondCrop, List<SelectListItem> selectListItem)
    {
        model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
        SetCropToSession(model);
        if (harvestYearPlanResponse.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
            }
            if (selectListItem.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    model.CropTypeID = null;
                    model.CropType = null;
                    SetCropToSession(model);
                }
                TempData[_cropTypeTempErrorName] = Resource.lblNoFieldsAreAvailable;
                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                return RedirectToAction("CropTypes");
            }
        }
        return null;
    }
    private async Task<IActionResult> RedirectForCropType(PlanViewModel model, PlanViewModel? cropData, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<int> fieldsAllowedForSecondCrop)
    {
        if (model.IsCheckAnswer)
        {
            if (cropData != null && cropData.CropTypeID == model.CropTypeID && !model.IsCropGroupChange)
            {
                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                return RedirectToAction(_checkAnswerActionName);
            }
            else
            {
                model = await BindCropData(model, fieldsAllowedForSecondCrop);
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].CropTypeID = model.CropTypeID.Value;
                    model.Crops[i].CropInfo1 = null;
                    model.Crops[i].CropInfo2 = null;
                }
                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                SetCropToSession(model);

            }
        }
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == true)
        {
            return RedirectToAction("CropGroupName");
        }
        return RedirectToAction("CropFields");
    }

    private async Task<PlanViewModel> BindCropData(PlanViewModel model, List<int> fieldsAllowedForSecondCrop)
    {
        if (model.Crops != null && model.Crops.Count > 0)
        {
            var allowedFields = fieldsAllowedForSecondCrop ?? new List<int>();
            bool hasAllowedFields = allowedFields.Any();

            var cropsToRemove = model.Crops
            .Where(crop =>
                crop.CropOrder == 2 &&
                (!hasAllowedFields ||
                 !allowedFields.Contains(crop.FieldID.Value)))
            .ToList();

            if (model.FieldList?.Any() == true)
            {
                foreach (var fieldId in cropsToRemove
                             .Where(c => c?.FieldID != null)
                             .Select(c => c.FieldID.Value.ToString()))
                {
                    model.FieldList.Remove(fieldId);
                }

                if (cropsToRemove.Any())
                {
                    model.CropGroupName = string.Empty;
                }
            }
            model.Crops.RemoveAll(crop => cropsToRemove.Contains(crop));
        }

        model = ResetCropInfoProperty(model);
        model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
        return model;
    }
    private static PlanViewModel ResetCropInfoProperty(PlanViewModel model)
    {
        model.CropInfo1 = null;
        model.CropInfo2 = null;
        model.CropInfo1Name = null;
        model.CropInfo2Name = null;
        return model;
    }
    private static PlanViewModel BindCropOrder(PlanViewModel model, PlanViewModel? cropData, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<SelectListItem> selectListItem, List<int> fieldsAllowedForSecondCrop)
    {
        model.IsCropTypeChange = model.IsCropGroupChange || (cropData != null && cropData.CropTypeID != model.CropTypeID);

        if ((harvestYearPlanResponse.Any() || selectListItem.Count == 1) &&
            string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && !model.IsCheckAnswer
            && (model.Crops != null && model.Crops.Count > 0))
        {
            foreach (var crop in model.Crops)
            {
                if (crop.FieldID != null)
                {
                    crop.CropOrder = fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) ? 2 : 1;
                }
            }
        }
        return model;
    }
    [HttpGet]
    public IActionResult VarietyName()
    {
        _logger.LogTrace("Crop Controller : VarietyName() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in VarietyName() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in VarietyName() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropGroupNameError"] = ex.Message;
            return RedirectToAction("CropGroupName");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VarietyName(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : VarietyName() post action called");
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Variety))
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Variety = model.Variety;
                }
            }
            SetCropToSession(model);

            if (model.IsCheckAnswer)
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Variety = model.Variety;
                    SetCropToSession(model);
                }

                if (model.IsCropTypeChange || model.IsAnyChangeInField)
                {
                    return RedirectToAction("SowingDateQuestion");
                }
                return RedirectToAction(_checkAnswerActionName);
            }
            return await Task.FromResult(RedirectToAction("SowingDateQuestion"));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in VarietyName() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnVariety"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CropFields()
    {
        _logger.LogTrace("Crop Controller : CropFields() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in CropFields() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            Error? error = null;
            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<Field> fieldList = new List<Field>(allFields);
            List<HarvestYearPlanResponse> harvestYearPlanResponse = new List<HarvestYearPlanResponse>();
            (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse;
            (fieldList, harvestYearPlanResponse) = FilterFieldListForCropUpdate(model, fieldList, harvestYearPlanResponse);

            List<SelectListItem> selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();

            List<int> fieldsAllowedForSecondCrop = new List<int>();


            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                if (model.FieldList != null)
                {
                    selectListItem.RemoveAll(item => !model.FieldList.Contains(item.Value));
                }
                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForUpdate(model, selectListItem, harvestYearPlanResponse, cropPlanForFirstCropFilter, allFields);
            }
            else
            {
                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForInsert(model, selectListItem, harvestYearPlanResponse, cropPlanForFirstCropFilter);

            }

            ViewBag.fieldList = selectListItem.Count > 0 ? selectListItem.OrderBy(x => x.Text).ToList() : null;
            if (model.IsAnyChangeInField)
            {
                model.IsAnyChangeInField = false;
                SetCropToSession(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropFields() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_cropTypeTempErrorName] = ex.Message;
            return RedirectToAction("CropTypes");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropFields(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropFields() post action called");
        try
        {
            Error? error = null;
            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter = new List<HarvestYearPlanResponse>();
            List<Field> fieldList = new List<Field>(allFields);
            (harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponseForFilter;
            (fieldList, harvestYearPlanResponseForFilter) = FilterFieldListForCropUpdate(model, fieldList, harvestYearPlanResponseForFilter);
            List<SelectListItem> selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();
            List<int> fieldsAllowedForSecondCrop = new List<int>();

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                if (model.FieldList != null)
                {
                    selectListItem.RemoveAll(item => !model.FieldList.Contains(item.Value));
                }

                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForUpdate(model, selectListItem, harvestYearPlanResponseForFilter, cropPlanForFirstCropFilter, allFields);
            }
            else
            {
                //Fetch fields allowed for second crop based on first crop

                (selectListItem, fieldsAllowedForSecondCrop) = await FilterSelectListItemForFieldForInsert(model, selectListItem, harvestYearPlanResponseForFilter, cropPlanForFirstCropFilter);
            }
            ValidateFieldSelection(model);
            if (!ModelState.IsValid)
            {
                ViewBag.fieldList = selectListItem.Count > 0 ? selectListItem.OrderBy(x => x.Text).ToList() : null;
                return View(model);
            }

            PlanViewModel? planViewModel = GetCropFromSession();
            bool success = BuildCropList(model, allFields, harvestYearPlanResponseForFilter, fieldsAllowedForSecondCrop, selectListItem);
            if (!success)
            {
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            (model, var redirect, bool matchFound) = BindSowingOrYieldProperty(model, planViewModel);
            if (redirect != null)
            {
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return RedirectForField(model, harvestYearPlanResponseForFilter, matchFound);

        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropFields() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnSelectField"] = ex.Message;
            return View(model);
        }
    }
    private async Task<(List<SelectListItem>, List<int>)> FilterSelectListItemForFieldForUpdate(PlanViewModel model, List<SelectListItem> selectListItem, List<HarvestYearPlanResponse> harvestYearPlanResponse, List<HarvestYearPlanResponse> cropPlanForFirstCropFilter, List<Field> allFields)
    {

        List<Field> allFieldList = new List<Field>(allFields);
        if (allFieldList.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
        {
            var fieldIdsToRemove = harvestYearPlanResponse
                .Select(x => x.FieldID)
                .ToList();


            allFieldList.RemoveAll(field => fieldIdsToRemove.Contains(field.ID.Value));
            foreach (var field in allFieldList)
            {
                if (!selectListItem.Any(x => x.Value == field.ID.Value.ToString()))
                {
                    selectListItem.Add(new SelectListItem
                    {
                        Value = field.ID.ToString(),
                        Text = field.Name
                    });
                }
            }
        }
        (List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, model, !string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate), model.Crops, model.FarmRB209CountryID ?? 3);
        selectListItem = BindSelectItemList(fieldsAllowedForSecondCrop, fieldRemoveList, selectListItem, allFields);
        return (selectListItem, fieldsAllowedForSecondCrop);
    }
    private bool BuildCropList(
    PlanViewModel model,
    List<Field> allFields, List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter,
    List<int> fieldsAllowedForSecondCrop, List<SelectListItem> selectListItem)
    {
        if (model.FieldList?.Count > 0)
        {
            if (model.FieldList.Contains(Resource.lblSelectAll))
            {
                model.FieldList = selectListItem.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
            }
            model.Crops = new List<Crop>();
            int counter = 1;
            foreach (var field in model.FieldList)
            {
                int fieldId = Convert.ToInt32(field);
                var crop = new Crop
                {
                    Year = model.Year.Value,
                    CropTypeID = model.CropTypeID,
                    OtherCropName = model.OtherCropName,
                    FieldID = fieldId,
                    Variety = model.Variety,
                    EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                    CropOrder = fieldsAllowedForSecondCrop.Contains(fieldId) ? 2 : 1
                };
                counter++;
                crop = BindCropInfoOneTwoOrCropGroupName(model, crop, harvestYearPlanResponseForFilter, allFields, fieldId);
                (crop, var result) = BindSowingDateAndYield(crop, fieldId);
                if (result != null)
                {
                    return false;
                }
                model.Crops.Add(crop);
                if (model.FieldList.Count == 1)
                {
                    Field? defaultField = allFields.FirstOrDefault(x => x.ID == fieldId);
                    model.FieldName = defaultField?.Name;
                }
            }
            SetCropToSession(model);
        }
        return true;
    }
    private void ValidateFieldSelection(PlanViewModel model)
    {
        if (model.FieldList == null || model.FieldList.Count == 0)
        {
            ModelState.AddModelError("FieldList",
                string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
        }
    }
    private (PlanViewModel, IActionResult?, bool) BindSowingOrYieldProperty(PlanViewModel model, PlanViewModel? planViewModel)
    {
        bool matchFound = false;
        if (model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            if (planViewModel?.Crops?.Any() == true && model.Crops != null)
            {
                (model, matchFound) = BindSowingOrYieldQuestionProperty(model, planViewModel);
            }
            else
            {
                _logger.LogError("Crop Controller : Session not found in CropFields() post action");
                return (model, Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict), matchFound);
            }
        }

        return (model, null, matchFound);
    }

    private (PlanViewModel, bool) BindSowingOrYieldQuestionProperty(PlanViewModel model, PlanViewModel? planViewModel)
    {
        bool matchFound = false;
        bool isSingleCrop = model.Crops.Count == 1;
        matchFound = model.Crops.All(crop1 => planViewModel.Crops.Any(crop2 => crop2.FieldID == crop1.FieldID));
        if (matchFound && isSingleCrop)
        {
            if (model.SowingDateQuestion != (int)NMP.Commons.Enums.SowingDateQuestion.NoIWillEnterTheDateLater)
            {
                model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields;
            }
            model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
            SetCropToSession(model);
        }
        if (!matchFound || model.Crops.Count != planViewModel.Crops.Count)
        {
            model.IsAnyChangeInField = true;
        }

        if ((model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ||
           model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField) &&
           isSingleCrop)
        {
            model.SowingDateQuestion = model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ? null : model.SowingDateQuestion;
            model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
            SetCropToSession(model);
        }
        return (model, matchFound);
    }
    private IActionResult RedirectForField(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, bool matchFound)
    {
        if (harvestYearPlanResponseForFilter?.Count > 0)
        {
            var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID.ToString()).ToList();

            model.IsAnyChangeInField = fieldIds.Except(model.FieldList ?? new List<string>()).Any() || (model.FieldList ?? new List<string>()).Except(fieldIds).Any();
        }

        SetCropToSession(model);
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsAnyChangeInField)
        {
            return (RedirectToAction("AddOrRemoveField"));
        }
        if (matchFound && (!model.IsAnyChangeInField) && model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            SetCropToSession(model);
            return (RedirectToAction(_checkAnswerActionName));
        }
        return RedirectToAction("CropGroupName");
    }
    private (Crop, IActionResult?) BindSowingDateAndYield(Crop crop, int fieldId)
    {
        PlanViewModel? planViewModel = GetCropFromSession();
        if (planViewModel != null)
        {
            if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
            {
                for (int i = 0; i < planViewModel.Crops.Count; i++)
                {
                    if (planViewModel.Crops[i].FieldID == fieldId)
                    {
                        crop.SowingDate = planViewModel.Crops[i].SowingDate;
                        crop.Yield = planViewModel.Crops[i].Yield; break;
                    }
                }
            }
        }
        else
        {
            _logger.LogError("Crop Controller : Session not found in CropFields() post action");
            return (crop, Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
        }
        return (crop, null);
    }

    private static Crop BindCropInfoOneTwoOrCropGroupName(PlanViewModel model, Crop crop, List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, List<Field> allFields, int fieldId)
    {
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && harvestYearPlanResponseForFilter != null && harvestYearPlanResponseForFilter.Any(x => x.FieldID == fieldId))
        {

            crop.ID = harvestYearPlanResponseForFilter.Where(x => x.FieldID == fieldId).Select(x => x.CropID).FirstOrDefault();
            crop.CropOrder = harvestYearPlanResponseForFilter.Where(x => x.FieldID == fieldId).Select(x => x.CropOrder).FirstOrDefault();

        }
        crop.FieldName = allFields.Where(x => x.ID == fieldId).Select(x => x.Name).FirstOrDefault();
        if (model.CropInfo1.HasValue)
        {
            crop.CropInfo1 = model.CropInfo1.Value;
        }
        if (model.CropInfo2.HasValue)
        {
            crop.CropInfo2 = model.CropInfo2.Value;
        }
        if (!string.IsNullOrWhiteSpace(model.CropGroupName))
        {
            crop.CropGroupName = model.CropGroupName;
        }
        return crop;
    }
    private static (List<Field>, List<HarvestYearPlanResponse>) FilterFieldListForCropUpdate(PlanViewModel model, List<Field> fieldList, List<HarvestYearPlanResponse> harvestYearPlanResponse)
    {
        if (harvestYearPlanResponse.Count > 0 && !string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
        {
            harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
            fieldList = FilterFieldList(fieldList, harvestYearPlanResponse);

        }
        return (fieldList, harvestYearPlanResponse);
    }
    private static List<SelectListItem> BindSelectItemList(List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList, List<SelectListItem> selectListItem, List<Field> allFields)
    {
        foreach (var removeFieldId in fieldRemoveList)
        {
            selectListItem.RemoveAll(x => x.Value == removeFieldId.ToString());
        }
        if (fieldsAllowedForSecondCrop.Any(addFieldId => !selectListItem.Any(x => x.Value == addFieldId.ToString())))
        {
            foreach (int addFieldId in fieldsAllowedForSecondCrop)
            {
                if (!selectListItem.Any(x => x.Value == addFieldId.ToString()))
                {
                    selectListItem.Add(new SelectListItem
                    {
                        Value = addFieldId.ToString(),
                        Text = allFields.Where(x => x.ID == addFieldId).Select(x => x.Name).FirstOrDefault()
                    });
                }
            }
        }
        return selectListItem;
    }
    private static List<Field> FilterFieldList(List<Field> fieldList, List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter)
    {
        if (harvestYearPlanResponseForFilter != null)
        {
            var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID).ToList();
            fieldList = fieldList.Where(x => fieldIds.Contains(x.ID.Value)).ToList();
        }
        return fieldList;
    }
    [HttpGet]
    public async Task<IActionResult> SowingDateQuestion()
    {
        _logger.LogTrace("Crop Controller : SowingDateQuestion() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in SowingDateQuestion() action");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            if (model.IsQuestionChange)
            {
                model.IsQuestionChange = false;
                SetCropToSession(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in SowingDateQuestion() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnSelectField"] = ex.Message;
            return RedirectToAction("CropFields");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SowingDateQuestion(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : SowingDateQuestion() action called");
        model = ValidateSowingDateQuestion(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            var redirect = HandleCheckAnswerFlowForSowingDateQuestion(model);
            if (redirect != null)
            {
                return redirect;
            }

            SetCropToSession(model);
            return BindSowingDateQuestion(model);
        }
        catch (Exception ex)
        {
            TempData["SowingDateQuestionError"] = ex.Message;
            return View(model);
        }
    }
    private IActionResult? HandleCheckAnswerFlowForSowingDateQuestion(PlanViewModel model)
    {
        if (!model.IsCheckAnswer)
        {
            return null;
        }

        var sessionModel = GetCropFromSession();

        if (sessionModel == null)
        {
            _logger.LogError("Crop Controller : Session not found in SowingDateQuestion() post action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        if (IsSameSowingQuestionWithNoChanges(sessionModel, model))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        if (sessionModel.SowingDateQuestion != model.SowingDateQuestion)
        {
            model.IsQuestionChange = true;
            model.SowingDateCurrentCounter = 0;
        }

        return null;
    }
    private static bool IsSameSowingQuestionWithNoChanges(PlanViewModel oldModel, PlanViewModel newModel)
    {
        return oldModel.SowingDateQuestion == newModel.SowingDateQuestion &&
               !newModel.IsAnyChangeInField &&
               !newModel.IsCropGroupChange &&
               !newModel.IsCropTypeChange;
    }
    private PlanViewModel ValidateSowingDateQuestion(PlanViewModel model)
    {
        if (model.SowingDateQuestion == null)
        {
            ModelState.AddModelError("SowingDateQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
        }

        return model;
    }
    private IActionResult BindSowingDateQuestion(PlanViewModel model)
    {
        bool isNoDateOption = model.SowingDateQuestion ==
                              (int)NMP.Commons.Enums.SowingDateQuestion.NoIWillEnterTheDateLater;

        if (isNoDateOption)
        {
            return HandleNoDateOption(model);
        }

        return HandleDateEntryOption(model);
    }
    private IActionResult HandleNoDateOption(PlanViewModel model)
    {
        ResetSowingDates(model);
        SetCropToSession(model);

        if (IsCheckAnswerValid(model))
            return RedirectToAction(_checkAnswerActionName);

        if (IsGrass(model))
            return RedirectToAction("SwardType");

        return RedirectToAction("YieldQuestion");
    }
    private IActionResult HandleDateEntryOption(PlanViewModel model)
    {
        if (model.IsCheckAnswer)
        {
            model.SowingDateCurrentCounter = 0;
            SetCropToSession(model);
        }

        return RedirectToAction("SowingDate");
    }
    private static void ResetSowingDates(PlanViewModel model)
    {
        if (model.Crops == null) return;

        foreach (var crop in model.Crops)
        {
            crop.SowingDate = null;
        }
    }
    private static bool IsCheckAnswerValid(PlanViewModel model)
    {
        return model.IsCheckAnswer &&
               !model.IsAnyChangeInField &&
               !model.IsCropGroupChange &&
               !model.IsCropTypeChange &&
               !model.IsCurrentSwardChange;
    }

    private static bool IsGrass(PlanViewModel model)
    {
        return model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass;
    }
    [HttpGet]
    public async Task<IActionResult> SowingDate(string q)
    {
        _logger.LogTrace("Crop Controller : SowingDate action called");
        PlanViewModel? model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogError("Crop Controller : Session not found in SowingDate() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        try
        {
            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
                if (model.SowingDateCurrentCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                }
                SetCropToSession(model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;//index of list
                if (itemCount == 0)
                {
                    model.SowingDateCurrentCounter = 0;
                    model.SowingDateEncryptedCounter = string.Empty;
                    SetCropToSession(model);
                    return RedirectToAction("SowingDateQuestion");
                }
                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.SowingDateCurrentCounter = index;
                model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
            }
        }
        catch (Exception ex)
        {
            TempData["SowingDateQuestionError"] = ex.Message;
            return RedirectToAction("SowingDateQuestion");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SowingDate(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : SowingDate() post action called");
        try
        {
            model = await ValidateSowingDatePost(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = BindSowingDateData(model);
            if (result != null)
            {
                return result;
            }

            return RedirectSowingDate(model);

        }
        catch (Exception ex)
        {
            TempData["SowingDateError"] = ex.Message;
            return View(model);
        }
    }

    private IActionResult? BindSowingDateData(PlanViewModel model)
    {
        if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
        {
            (model, var result) = BindSowingDateAreDifferent(model);
            if (result != null)
            {
                return result;
            }
        }
        else if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
        {
            (model, var result) = BindSowingDateWhenSingle(model);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
    private IActionResult RedirectSowingDate(PlanViewModel model)
    {
        if (model.SowingDateCurrentCounter == model.Crops.Count)
        {
            if (model.IsCheckAnswer && (!model.IsAnyChangeInField))
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                if (model.IsCheckAnswer && !model.IsCropGroupChange && !model.IsAnyChangeInField && !model.IsCurrentSwardChange)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction("SwardType");
            }
            return RedirectToAction("YieldQuestion");
        }
        else
        {
            return View(model);
        }
    }
    private (PlanViewModel, IActionResult?) BindSowingDateAreDifferent(PlanViewModel model)
    {
        for (int i = 0; i < model.Crops.Count; i++)
        {
            if (model.FieldID == model.Crops[i].FieldID.Value)
            {
                model.SowingDateCurrentCounter++;
                if (i + 1 < model.Crops.Count)
                {
                    model.FieldID = model.Crops[i + 1].FieldID.Value;
                }

                break;
            }
        }

        model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
        SetCropToSession(model);
        if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsQuestionChange) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            return (model, RedirectToAction(_checkAnswerActionName));
        }
        return (model, null);
    }

    private (PlanViewModel, IActionResult?) BindSowingDateWhenSingle(PlanViewModel model)
    {
        model.SowingDateCurrentCounter = 1;
        for (int i = 0; i < model.Crops.Count; i++)
        {
            model.Crops[i].SowingDate = model.Crops[0].SowingDate;
        }
        model.SowingDateEncryptedCounter = _fieldDataProtector.Protect(model.SowingDateCurrentCounter.ToString());
        SetCropToSession(model);

        if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            SetCropToSession(model);
            return (model, RedirectToAction(_checkAnswerActionName));
        }

        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            if (model.IsCheckAnswer && !model.IsCropGroupChange && !model.IsAnyChangeInField && !model.IsCurrentSwardChange)
            {
                return (model, RedirectToAction(_checkAnswerActionName));
            }
            return (model, RedirectToAction("SwardType"));
        }
        return (model, RedirectToAction("YieldQuestion"));
    }
    private async Task<PlanViewModel> ValidateSowingDatePost(PlanViewModel model)
    {
        ValidateDateFormatErrors(model);
        ValidateRequiredDate(model);
        await ValidateDateRangeRules(model);
        ValidateCropSpecificRules(model);

        return model;
    }
    private void ValidateDateFormatErrors(PlanViewModel model)
    {
        var key = GetCropKey(model);

        if (!ModelState.IsValid && ModelState.ContainsKey(key))
        {
            var entry = ModelState[key];
            var error = entry?.Errors.FirstOrDefault()?.ErrorMessage;

            if (error != null && IsDateFormatError(error))
            {
                entry.Errors.Clear();
                entry.Errors.Add(Resource.MsgTheDateMustInclude);
            }
        }
    }
    private static bool IsDateFormatError(string error)
    {
        string[] patterns =
        {
        Resource.MsgDateMustBeARealDate,
        Resource.MsgDateMustIncludeAMonth,
        Resource.MsgDateMustIncludeAMonthAndYear,
        Resource.MsgDateMustIncludeADayAndYear,
        Resource.MsgDateMustIncludeAYear,
        Resource.MsgDateMustIncludeADay,
        Resource.MsgDateMustIncludeADayAndMonth
    };

        return patterns.Any(p => error.Equals(string.Format(p, "SowingDate")));
    }
    private void ValidateRequiredDate(PlanViewModel model)
    {
        if (model.Crops[model.SowingDateCurrentCounter].SowingDate == null)
        {
            ModelState.AddModelError(GetCropKey(model), Resource.MsgEnterADateBeforeContinuing);
        }
    }
    private async Task ValidateDateRangeRules(PlanViewModel model)
    {
        bool isPerennial = await _cropLogic.FetchIsPerennialByCropTypeId(model.CropTypeID.Value);

        var date = model.Crops[model.SowingDateCurrentCounter].SowingDate;
        var year = model.Year.Value;

        DateTime maxDate = new DateTime(year, 12, 31, 00, 00, 00, DateTimeKind.Unspecified);

        if (date > maxDate)
        {
            ModelState.AddModelError(GetCropKey(model),
                string.Format(Resource.MsgPlantingDateAfterHarvestYear, year, maxDate.ToString("dd MMMM yyyy")));
        }

        if (!isPerennial)
        {
            DateTime minDate = new DateTime(year - 1, 01, 01, 00, 00, 00, DateTimeKind.Unspecified);


            if (date < minDate)
            {
                ModelState.AddModelError(GetCropKey(model),
                    string.Format(Resource.MsgPlantingDateBeforeHarvestYear, year, minDate.ToString("dd MMMM yyyy")));
            }
        }
    }
    private void ValidateCropSpecificRules(PlanViewModel model)
    {
        var cropType = model.CropTypeID;

        bool isWinterCrop =
            cropType == (int)NMP.Commons.Enums.CropTypes.WinterWheat ||
            cropType == (int)NMP.Commons.Enums.CropTypes.WinterTriticale ||
            cropType == (int)NMP.Commons.Enums.CropTypes.ForageWinterTriticale ||
            cropType == (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat;

        var date = model.Crops[model.SowingDateCurrentCounter].SowingDate;

        if (isWinterCrop && date != null && date.Value.Month is >= 2 and <= 6)
        {
            ModelState.AddModelError(GetCropKey(model),
                string.Format(Resource.MsgForSowingDate, model.CropType));
        }
    }
    private static string GetCropKey(PlanViewModel model)
    {
        return $"Crops[{model.SowingDateCurrentCounter}].SowingDate";
    }
    [HttpGet]
    public async Task<IActionResult> YieldQuestion()
    {
        _logger.LogTrace("Crop Controller : YieldQuestion() action called");
        PlanViewModel? model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogError("Crop Controller : Session not found in YieldQuestion() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        try
        {
            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            ViewBag.DefaultYield = defaultYield;
            if (model.IsQuestionChange)
            {
                model.IsQuestionChange = false;
                SetCropToSession(model);
            }
        }
        catch (Exception ex)
        {
            TempData["SowingDateError"] = ex.Message;
            return RedirectToAction("SowingDate", new { q = model.SowingDateEncryptedCounter });
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YieldQuestion(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : YieldQuestion() post action called");
        if (model.YieldQuestion == null)
        {
            ModelState.AddModelError("YieldQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            ViewBag.DefaultYield = defaultYield;
            return View(model);
        }
        try
        {
            if (model.IsCheckAnswer)
            {
                PlanViewModel planViewModel = GetCropFromSession();
                if (planViewModel == null)
                {
                    _logger.LogError("Crop Controller : Session not found in YieldQuestion() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                if (planViewModel.YieldQuestion == model.YieldQuestion && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    model.IsQuestionChange = true;
                    model.YieldCurrentCounter = 0;
                }
            }

            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            ViewBag.DefaultYield = defaultYield;
            if (defaultYield == 0 && model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.NoDoNotEnterAYield)
            {
                model.Yield = null;
                if (model.Crops != null && model.Crops.Any(x => x.Yield != null))
                {
                    model.Crops.ForEach(c => c.Yield = null);
                }
                SetCropToSession(model);
                return RedirectToAction("CropInfoOne");
            }
            SetCropToSession(model);
            return RedirectToAction("Yield");
        }
        catch (Exception ex)
        {
            TempData["YieldQuestionError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Yield(string q)
    {
        _logger.LogTrace("Crop Controller : Yield({0}) action called", q);
        PlanViewModel model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogError("Crop Controller : Session not found in Yield() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        try
        {
            bool hasCrops = model.Crops != null && model.Crops.Count > 0;
            decimal defaultYieldForCropType = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYieldForCropType > 0)
            {
                ViewBag.IsYieldOptional = Resource.lblYes;
                ViewBag.DefaultYield = defaultYieldForCropType;
            }
            if (string.IsNullOrWhiteSpace(q) && hasCrops)
            {
                model = BindFieldAndYieldCounter(model);
            }
            else if (hasCrops)
            {
                return await RedirectGetYieldAction(model, defaultYieldForCropType, q);
            }
            if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields)
            {
                (model, var result) = BindYieldValueForStandardYield(model, defaultYieldForCropType);
                return result;
            }
        }
        catch (Exception ex)
        {
            TempData["YieldQuestionError"] = ex.Message;
            return RedirectToAction("YieldQuestion");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yield(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : Yield() post action called");
        try
        {
            model = await ValidateYield(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
            {
                (model, var result) = BindYieldForDifferentFigure(model);
                if (result != null)
                {
                    return result;
                }
            }
            else if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
            {
                (model, var result) = BindYieldForSingleFigure(model);
                if (result != null)
                {
                    return result;
                }
            }

            return RedirectYield(model);
        }
        catch (Exception ex)
        {
            TempData["ErrorOnYield"] = ex.Message;
            return RedirectToAction("Yield", new { q = model.YieldEncryptedCounter });
        }
    }

    private IActionResult RedirectYield(PlanViewModel model)
    {
        if (model.YieldCurrentCounter == model.Crops.Count)
        {
            return RedirectYieldAction(model);
        }
        else
        {
            return View(model);
        }
    }
    private (PlanViewModel, IActionResult?) BindYieldForDifferentFigure(PlanViewModel model)
    {
        for (int i = 0; i < model.Crops.Count; i++)
        {
            if (model.FieldID == model.Crops[i].FieldID.Value)
            {
                model.YieldCurrentCounter++;
                if (i + 1 < model.Crops.Count)
                {
                    model.FieldID = model.Crops[i + 1].FieldID.Value;
                }
                break;
            }
        }

        model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
        SetCropToSession(model);
        if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsQuestionChange) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            return (model, RedirectToAction(_checkAnswerActionName));
        }
        return (model, null);
    }
    private (PlanViewModel, IActionResult?) BindYieldForSingleFigure(PlanViewModel model)
    {
        model.YieldCurrentCounter = 1;
        for (int i = 0; i < model.Crops.Count; i++)
        {
            model.Crops[i].Yield = model.Crops[0].Yield;
        }
        model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
        SetCropToSession(model);
        return (model, RedirectYieldAction(model));
    }
    private async Task<PlanViewModel> ValidateYield(PlanViewModel model)
    {
        decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
        if (defaultYield == 0 && model.Crops[model.YieldCurrentCounter].Yield == null)
        {

            ModelState.AddModelError(_cropPrefix + model.YieldCurrentCounter + _yieldPrefix, string.Format(Resource.MsgEnterExpectedYieldforCropinField, model.CropType, model.FieldName));

        }
        if (model.Crops[model.YieldCurrentCounter].Yield != null)
        {
            if (model.Crops[model.YieldCurrentCounter].Yield > Convert.ToInt32(Resource.lblFiveDigit))
            {
                ModelState.AddModelError(_cropPrefix + model.YieldCurrentCounter + _yieldPrefix, Resource.MsgEnterAValueOfNoMoreThan5Digits);
            }
            if (model.Crops[model.YieldCurrentCounter].Yield < 0)
            {
                ModelState.AddModelError(_cropPrefix + model.YieldCurrentCounter + _yieldPrefix, string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblYield));
            }
        }

        if (defaultYield > 0)
        {
            ViewBag.IsYieldOptional = Resource.lblYes;
        }
        return model;
    }
    private PlanViewModel BindFieldAndYieldCounter(PlanViewModel model)
    {

        model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
        if (model.YieldCurrentCounter == 0)
        {
            model.FieldID = model.Crops[0].FieldID.Value;
        }
        SetCropToSession(model);

        return model;
    }
    private (PlanViewModel, IActionResult) BindYieldValueForStandardYield(PlanViewModel model, decimal defaultYieldForCropType)
    {
        model.YieldCurrentCounter = 1;
        for (int i = 0; i < model.Crops.Count; i++)
        {
            model.Crops[i].Yield = defaultYieldForCropType;
        }
        SetCropToSession(model);
        return (model, RedirectYieldAction(model));
    }
    private async Task<IActionResult> RedirectGetYieldAction(PlanViewModel model, decimal defaultYieldForCropType, string q)
    {
        int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
        int index = itemCount - 1;//index of list
        if (itemCount == 0)
        {
            model.YieldCurrentCounter = 0;
            model.YieldEncryptedCounter = string.Empty;
            SetCropToSession(model);
            return RedirectToAction("YieldQuestion");
        }

        model.FieldID = model.Crops[index].FieldID.Value;
        model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
        model.YieldCurrentCounter = index;
        model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
        if (defaultYieldForCropType > 0)
        {
            ViewBag.DefaultYield = defaultYieldForCropType;
        }
        SetCropToSession(model);
        return View(model);
    }

    private IActionResult RedirectYieldAction(PlanViewModel model)
    {
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || (model.IsCheckAnswer))
        {
            if (model.IsAnyChangeInField && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
            {
                model.IsAnyChangeInField = false;
            }
            SetCropToSession(model);
            return model.IsCropTypeChange
           ? RedirectToAction("CropInfoOne")
           : RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction("CropInfoOne");

    }
    [HttpGet]
    public async Task<IActionResult> CropInfoOne()
    {
        _logger.LogTrace("Crop Controller : CropInfoOne() action called");
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in CropInfoOne() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<CropInfoOneResponse> cropInfoOneList = await GetCropInfoOneList(model);
            await PopulateCropInfoOneViewData(model, cropInfoOneList);
            if (cropInfoOneList != null && cropInfoOneList.Any(x => x.CropInfo1Name == Resource.lblNone))
            {
                model.CropInfo1Name = cropInfoOneList
                .FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone)
                ?.CropInfo1Name;
                model.CropInfo1 = cropInfoOneList
                .FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone)?.CropInfo1Id;

                model.Crops.ForEach(c => c.CropInfo1 = model.CropInfo1);


                SetCropToSession(model);
                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals)
                {
                    return RedirectToAction(_cropInfoTwoActionName);
                }
                else
                {
                    model.IsCropTypeChange = false;
                    model.IsCropGroupChange = false;
                    model.CropInfo2 = null;
                    model.CropInfo2Name = null;
                    SetCropToSession(model);
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller : Exception in CropInfoOne() action: {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnYield"] = ex.Message;
            return RedirectToAction("Yield");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropInfoOne(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropInfoOne() post action called");

        try
        {
            if (model.CropInfo1 == null)
            {
                ModelState.AddModelError(
                    "CropInfo1",
                    Resource.MsgSelectAnOptionBeforeContinuing);
            }
            List<CropInfoOneResponse> cropInfoOneList = await GetCropInfoOneList(model);


            if (!ModelState.IsValid)
            {
                await PopulateCropInfoOneViewData(model, cropInfoOneList);
                return View(model);
            }

            model = SetCropInfoOne(model, cropInfoOneList);

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropInfoOne() post action : {Message}, {StackTrace}",
                ex.Message, ex.StackTrace);
            TempData["CropInfoOneError"] = ex.Message;
            return RedirectToAction("CropInfoOne");
        }

        return GetNextAction(model);
    }


    private async Task PopulateCropInfoOneViewData(
        PlanViewModel model,
        List<CropInfoOneResponse> cropInfoOneList)
    {
        string? question =
            await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(
                model.CropTypeID ?? 0, model.FarmRB209CountryID ?? 1);

        if (!string.IsNullOrWhiteSpace(question))
        {
            ViewBag.CropInfoOneQuestion =
                (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions ||
                 model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions)
                    ? string.Format(question, model.CropType)
                    : question;
        }

        ViewBag.CropInfoOneList =
            cropInfoOneList.OrderBy(c => c.CropInfo1Id);
    }

    private static PlanViewModel SetCropInfoOne(
        PlanViewModel model,
        List<CropInfoOneResponse> cropInfoOneList)
    {
        model.CropInfo1Name = cropInfoOneList
            .FirstOrDefault(x => x.CropInfo1Id == model.CropInfo1)
            ?.CropInfo1Name;

        model.Crops.ForEach(c => c.CropInfo1 = model.CropInfo1);
        return model;
    }

    private IActionResult GetNextAction(PlanViewModel model)
    {
        if (model.IsCheckAnswer &&
            !model.IsCropGroupChange &&
            !model.IsCropTypeChange)
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        if (model.CropGroupId ==
            (int)NMP.Commons.Enums.CropGroup.Cereals)
        {
            return RedirectToAction(_cropInfoTwoActionName);
        }

        ResetCropInfoTwo(model);
        return RedirectToAction(_checkAnswerActionName);
    }

    private void ResetCropInfoTwo(PlanViewModel model)
    {
        model.IsCropTypeChange = false;
        model.IsCropGroupChange = false;
        model.CropInfo2 = null;
        model.CropInfo2Name = null;

        SetCropToSession(model);
    }



    [HttpGet]
    public async Task<IActionResult> CropInfoTwo()
    {
        _logger.LogTrace("Crop Controller : CropInfoTwo() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in CropInfoTwo() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<CropInfoTwoResponse> cropInfoTwoList = await GetFilteredCropInfoTwoList(model);
            ViewBag.CropInfoTwoList = cropInfoTwoList.OrderBy(c => c.CropInfo2);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropInfoTwo() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropInfoOneError"] = ex.Message;
            return RedirectToAction("CropInfoOne");
        }

        return View(model);
    }

    private async Task<List<CropInfoTwoResponse>> GetFilteredCropInfoTwoList(PlanViewModel model)
    {
        List<CropInfoTwoResponse> cropInfoTwoResponse = await _cropLogic.FetchCropInfoTwoByCropTypeId();
        if (model.FarmRB209CountryID.HasValue)
        {
            cropInfoTwoResponse = cropInfoTwoResponse.Where(x => x.CountryId == model.FarmRB209CountryID || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).ToList();
        }
        return cropInfoTwoResponse;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropInfoTwo(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropInfoTwo() post action called");
        try
        {
            if (model.CropInfo2 == null)
            {
                ModelState.AddModelError("CropInfo2", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            List<CropInfoTwoResponse> cropInfoTwoList = await GetFilteredCropInfoTwoList(model);
            if (!ModelState.IsValid)
            {
                ViewBag.CropInfoTwoList = cropInfoTwoList.OrderBy(c => c.CropInfo2);
                return View(model);
            }
            var cropInfoTwoListItem = cropInfoTwoList.FirstOrDefault(x => x.CropInfo2Id == model.CropInfo2);
            model.CropInfo2Name = cropInfoTwoListItem?.CropInfo2;
            for (int i = 0; i < model.Crops?.Count; i++)
            {
                model.Crops[i].CropInfo2 = model.CropInfo2;
            }
            model.IsCropTypeChange = false;
            model.IsCropGroupChange = false;

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropInfoTwo() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropInfoTwoError"] = ex.Message;
            return RedirectToAction(_cropInfoTwoActionName);
        }

        return RedirectToAction(_checkAnswerActionName);
    }

    [HttpGet]
    public async Task<IActionResult> CheckAnswer(string? q, string? r, string? t, string? u, string? v, string? w)
    {
        _logger.LogTrace("Crop Controller : CheckAnswer() action called");
        PlanViewModel model = new PlanViewModel();
        Error? error = null;
        List<HarvestYearPlanResponse>? harvestYearPlanResponse = null;
        string? sowingQuestion = null;
        bool isBasePlan = false;
        bool allSowingAreSame = true;
        DateTime? firstSowingDate = null;
        FarmResponse? farm = null;
        int farmID = 0;
        try
        {
            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) &&
                !string.IsNullOrWhiteSpace(t) && !string.IsNullOrWhiteSpace(u))
            {
                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.CropType = _cropDataProtector.Unprotect(q);
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    model.CropGroupName = _cropDataProtector.Unprotect(r);
                    model.EncryptedCropGroupName = r;
                }
                if (!string.IsNullOrWhiteSpace(t))
                {
                    model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(t));
                    model.EncryptedHarvestYear = t;
                }
                if (!string.IsNullOrWhiteSpace(u))
                {
                    model.EncryptedFarmId = u;
                }
                if (!string.IsNullOrWhiteSpace(v))
                {
                    model.EncryptedFieldId = v;
                    model.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(v));
                    model.IsComingFromRecommendation = true;
                }
                else
                {
                    model.IsComingFromRecommendation = null;
                }
                if (!string.IsNullOrWhiteSpace(w))
                {
                    model.CropOrder = Convert.ToInt32(_cropDataProtector.Unprotect(w));
                }

                farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmID);
                if (farm != null && string.IsNullOrWhiteSpace(error?.Message))
                {
                    model.FarmRB209CountryID = farm.RB209CountryID;
                    model.CountryId = farm.CountryID;
                }
                (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (error == null && harvestYearPlanResponse.Count > 0)
                {
                    harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropTypeName == model.CropType && x.CropGroupName == model.CropGroupName).ToList();
                    if (harvestYearPlanResponse != null)
                    {
                        if (model.CropOrder != null)
                        {
                            harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.FieldID == model.FieldID && x.CropOrder == model.CropOrder).ToList();
                        }
                        model.Crops = new List<Crop>();
                        model.FieldList = new List<string>();
                        int counter = 1;
                        decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(harvestYearPlanResponse.First().CropTypeID, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                        List<decimal?> yields = new List<decimal?>();
                        for (int i = 0; i < harvestYearPlanResponse.Count; i++)
                        {
                            var crop = new Crop();
                            model.FieldName = harvestYearPlanResponse[i].FieldName;
                            crop.Year = harvestYearPlanResponse[i].Year;
                            model.FieldList.Add(harvestYearPlanResponse[i].FieldID.ToString());
                            crop.CropOrder = harvestYearPlanResponse[i].CropOrder;
                            crop.FieldID = harvestYearPlanResponse[i].FieldID;
                            crop.CropGroupName = harvestYearPlanResponse[i].CropGroupName;
                            crop.FieldName = harvestYearPlanResponse[i].FieldName;
                            crop.CropTypeID = harvestYearPlanResponse[0].CropTypeID;
                            crop.SwardManagementID = harvestYearPlanResponse[0].SwardManagementID;
                            crop.DefoliationSequenceID = harvestYearPlanResponse[0].DefoliationSequenceID;
                            crop.SwardTypeID = harvestYearPlanResponse[0].SwardTypeID;
                            crop.PotentialCut = harvestYearPlanResponse[0].PotentialCut;
                            model.SwardManagementId = harvestYearPlanResponse[0].SwardManagementID;
                            model.DefoliationSequenceId = harvestYearPlanResponse[0].DefoliationSequenceID;
                            model.SwardTypeId = harvestYearPlanResponse[0].SwardTypeID;
                            model.PotentialCut = harvestYearPlanResponse[0].PotentialCut;
                            if (harvestYearPlanResponse[i].CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                            {
                                model.CurrentSward = (harvestYearPlanResponse[0].Establishment == null || harvestYearPlanResponse[0].Establishment == 0) ? (int)NMP.Commons.Enums.CurrentSward.ExistingSward : (int)NMP.Commons.Enums.CurrentSward.NewSward;
                            }
                            else
                            {
                                model.CurrentSward = null;
                            }
                            model.GrassSeason = harvestYearPlanResponse[0].Establishment;
                            crop.EncryptedCounter = _fieldDataProtector.Protect(counter.ToString());
                            if (decimal.TryParse(harvestYearPlanResponse[i].Yield, out decimal yield))
                            {
                                crop.Yield = yield;
                                model.Yield = yield;
                                yields.Add(yield);
                            }
                            else
                            {
                                crop.Yield = null;
                                yields.Add(null);
                            }

                            if (harvestYearPlanResponse[i].IsBasePlan != null && (harvestYearPlanResponse[i].IsBasePlan.Value))
                            {
                                isBasePlan = true;
                            }

                            if (harvestYearPlanResponse[i].SowingDate == null)
                            {
                                crop.SowingDate = null;
                                model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.NoIWillEnterTheDateLater;
                            }
                            else
                            {
                                if (firstSowingDate == null)
                                {
                                    firstSowingDate = harvestYearPlanResponse[i].SowingDate;
                                    model.SowingDate = firstSowingDate.Value;
                                }
                                else if (firstSowingDate != harvestYearPlanResponse[i].SowingDate)
                                {
                                    allSowingAreSame = false;
                                    model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields;
                                }
                                if (harvestYearPlanResponse[i].SowingDate != null)
                                {
                                    crop.SowingDate = harvestYearPlanResponse[i].SowingDate.Value.ToLocalTime();
                                }

                            }
                            crop.Variety = harvestYearPlanResponse[i].CropVariety;
                            crop.ID = harvestYearPlanResponse[i].CropID;
                            crop.OtherCropName = harvestYearPlanResponse[i].OtherCropName;
                            crop.CropInfo1 = harvestYearPlanResponse[i].CropInfo1;
                            crop.CropInfo2 = harvestYearPlanResponse[i].CropInfo2;
                            List<CropTypeResponse> cropTypeResponseList = (await _fieldLogic.FetchAllCropTypes());
                            if (cropTypeResponseList != null)
                            {
                                CropTypeResponse? cropTypeResponse = cropTypeResponseList.FirstOrDefault(x => x.CropTypeId == crop.CropTypeID);
                                if (cropTypeResponse != null)
                                {
                                    model.CropGroupId = cropTypeResponse.CropGroupId;
                                    if (model.CropGroupId != null)
                                    {
                                        model.CropGroup = await _fieldLogic.FetchCropGroupById(model.CropGroupId.Value);
                                    }

                                }
                            }
                            counter++;
                            model.Crops.Add(crop);
                        }

                        if (model.Crops != null && model.Crops.All(x => x.SowingDate != null) && model.SowingDateQuestion == null && allSowingAreSame && harvestYearPlanResponse.Count >= 1)
                        {
                            model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields;

                        }
                        model.CropInfo1 = harvestYearPlanResponse[0].CropInfo1;
                        model.CropInfo2 = harvestYearPlanResponse[0].CropInfo2;

                        model.CropTypeID = harvestYearPlanResponse[0].CropTypeID;
                        if (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                        {

                            bool allYieldsSame = model.Crops
                            .Where(c => c.Yield != null)
                            .Select(c => c.Yield.Value)
                            .Distinct()
                            .Count() == 1;
                            model.GrassGrowthClassQuestion = allYieldsSame ? (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields : (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField;
                        }
                        else
                        {

                            bool allAreDefault = yields.All(y => y.HasValue && y.Value == defaultYield);
                            bool allSame = yields.Distinct().Count() == 1;
                            bool allAreNull = yields.All(y => !y.HasValue);

                            if (allAreDefault)
                            {
                                model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields;
                            }
                            else if (!allSame && harvestYearPlanResponse.Count > 1)
                            {
                                model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField;
                            }
                            else
                            {
                                model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                            }
                            if (allAreNull && defaultYield == 0)
                            {
                                model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.NoDoNotEnterAYield;
                            }
                        }
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
                        model.CropType = harvestYearPlanResponse[0].CropTypeName;
                        model.Variety = harvestYearPlanResponse[0].CropVariety;
                        model.CropGroupName = harvestYearPlanResponse[0].CropGroupName;
                        model.PreviousCropGroupName = model.CropGroupName;
                        model.OtherCropName = harvestYearPlanResponse[0].OtherCropName;
                        model = await BindCropInfo1AndCropInfo2(model);
                    }
                    model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                    model.EncryptedIsCropUpdate = _cropDataProtector.Protect(Resource.lblTrue);
                }
            }
            else
            {
                model = GetCropFromSession();
                if (model == null)
                {
                    _logger.LogError("Crop Controller : Session not found in CheckAnswer() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }


            model.IsCurrentSwardChange = false;
            farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

            //fetch all fields
            List<Field> allFieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = new List<HarvestYearPlanResponse>();
            (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmID);
            if (error == null && harvestYearPlanResponse.Count > 0)
            {
                cropPlanForFirstCropFilter = harvestYearPlanResponse
                    .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))
                    ).ToList();
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && (!model.IsFieldToBeRemoved.HasValue || (model.IsFieldToBeRemoved.HasValue && !model.IsFieldToBeRemoved.Value))
                && string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r) && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    //filter the plan list based on the crop group
                    harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponse != null)
                    {

                        //Get the fields that we unchecked.
                        List<HarvestYearPlanResponse> removableFields = harvestYearPlanResponse.Where(f => !model.FieldList.Contains(f.FieldID.ToString())).ToList();

                        //Remove the fields that already have a plan from the field list.
                        var fieldIdsToRemove = harvestYearPlanResponse
                            .Select(x => x.FieldID)
                            .ToList();
                        List<Field> newlyAddedFields = allFieldList.Where(field => !fieldIdsToRemove.Contains(field.ID.Value)).ToList();
                        if (model.Crops != null && model.Crops.Any(x => newlyAddedFields.Any(y => x.FieldID == y.ID.Value)))
                        {
                            foreach (var newlyAddedField in newlyAddedFields)
                            {
                                if (model.Crops.Any(c => c.FieldID == newlyAddedField.ID.Value))
                                {
                                    // If a match is found, remove the corresponding row from model.Crops
                                    var cropToDelete = model.Crops.First(c => c.FieldID == newlyAddedField.ID.Value);
                                    model.Crops.Remove(cropToDelete);
                                    if (model.FieldList != null && model.FieldList.Contains(newlyAddedField.ID.Value.ToString()))
                                    {
                                        model.FieldList.Remove(newlyAddedField.ID.ToString());
                                    }
                                }
                            }
                        }

                        if (model.Crops != null && removableFields.Any(newField => !model.Crops.Any(crop => crop.FieldID == newField.FieldID)))
                        {
                            foreach (var removableField in removableFields)
                            {
                                if (!model.Crops.Any(x => x.FieldID == removableField.FieldID))
                                {
                                    (Crop? crop, error) = await _cropLogic.FetchCropById(removableField.CropID);
                                    if (error == null && crop != null)
                                    {
                                        crop.FieldName = allFieldList.Where(x => x.ID == removableField.FieldID).Select(x => x.Name).FirstOrDefault();
                                        model.Crops.Add(crop);
                                    }
                                    if (model.FieldList != null && !model.FieldList.Contains(removableField.FieldID.ToString()))
                                    {
                                        model.FieldList.Add(removableField.FieldID.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (model.CropTypeID.HasValue)
            {
                decimal defaultYieldForCropType = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                if (defaultYieldForCropType > 0)
                {
                    ViewBag.IsYieldOptional = Resource.lblYes;
                    ViewBag.DefaultYield = defaultYieldForCropType;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                model.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
            }

            if (!string.IsNullOrWhiteSpace(model.CropType))
            {
                model.EncryptedCropType = _cropDataProtector.Protect(model.CropType);
            }
            if (model.CropOrder != null && model.CropOrder > 0)
            {
                model.EncryptedCropOrder = _cropDataProtector.Protect(model.CropOrder.ToString());
            }
            List<int> fieldsAllowedForSecondCrop = new List<int>();
            if (harvestYearPlanResponse.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
            {
                harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                if (harvestYearPlanResponse != null && harvestYearPlanResponse.Count == 1)
                {
                    List<Field> allFieldListForFilter = [.. allFieldList];
                    if (allFieldListForFilter.Count > 0)
                    {
                        var fieldIdsToRemove = harvestYearPlanResponse
                            .Select(x => x.FieldID)
                            .ToList();

                        allFieldListForFilter.RemoveAll(field => fieldIdsToRemove.Contains(field.ID.Value));
                        if (allFieldListForFilter.Count == 0)
                        {
                            ViewBag.IsFieldChange = true;
                        }
                        else
                        {
                            ViewBag.IsFieldChange = false;
                        }
                    }
                    else
                    {
                        ViewBag.IsFieldChange = true;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
            }

            if (!string.IsNullOrWhiteSpace(model.CropType))
            {
                ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
            }

            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmID);
            if (farm != null && string.IsNullOrWhiteSpace(error?.Message))
            {
                model.FarmRB209CountryID = farm.RB209CountryID;
            }

            List<Field> fieldList = [.. allFieldList];
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                var fieldIds = model.Crops.Select(c => c.FieldID).Distinct();
                fieldList = fieldList.Where(x => fieldIds.Contains(x.ID)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
            {
                List<int> updatedFields = harvestYearPlanResponse
                .Where(x => x.CropGroupName == model.PreviousCropGroupName)
                .Select(x => x.FieldID)
                .ToList();

                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> allFieldListForFilter = [.. allFieldList];
                if (allFieldListForFilter.Count > 0)
                {
                    var fieldIdsToRemove = harvestYearPlanResponse
                        .Select(x => x.FieldID)
                        .ToList();

                    allFieldListForFilter.RemoveAll(field => fieldIdsToRemove.Contains(field.ID.Value));
                    updatedFields.AddRange(allFieldListForFilter.Select(x => x.ID.Value));
                    ViewBag.FieldOptions = updatedFields;
                }
                else
                {
                    ViewBag.FieldOptions = harvestYearPlanResponse
                        .Where(x => x.CropGroupName == model.PreviousCropGroupName)
                        .Select(x => x.FieldID)
                        .ToList();
                }

                var fieldIdsForFilter = fieldList.Select(f => f.ID);
                harvestYearPlanResponse = harvestYearPlanResponse
                    .Where(x => fieldIdsForFilter.Contains(x.FieldID))
                    .ToList();
            }
            else
            {
                ViewBag.FieldOptions = fieldList;
            }

            //Fetch fields allowed for second crop based on first crop
            var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;

            (fieldsAllowedForSecondCrop, _) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, model, !string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate), model.Crops, model.FarmRB209CountryID ?? 3);

            (model, _) = await BindGrassProperties(model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    if (model.Crops[i].IsBasePlan)
                    {
                        isBasePlan = true;
                    }

                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                    if (harvestYearPlanResponse.Count > 0)
                    {
                        List<string> fieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        if (fieldIds.Count > 0 && fieldIds.Any(fieldId => model.FieldList.Contains(fieldId)) && string.IsNullOrWhiteSpace(model.CropGroupName))
                        {

                            model.CropGroupName = model.PreviousCropGroupName;
                            model.Crops[i].CropGroupName = model.PreviousCropGroupName;

                        }
                    }
                }
            }

            model.IsCheckAnswer = true;

            if (model.CropTypeID != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                string? cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0, model.FarmRB209CountryID ?? 1);
                if (!string.IsNullOrWhiteSpace(cropInfoOneQuestion))
                {
                    ViewBag.CropInfoOneQuestion = (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions || model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions) ? string.Format(cropInfoOneQuestion, model.CropType) : cropInfoOneQuestion;
                }
                if (cropInfoOneQuestion == null)
                {
                    List<CropInfoOneResponse> cropInfoOneResponse = await _cropLogic.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0, model.FarmRB209CountryID);
                    ViewBag.CropInfoOneList = cropInfoOneResponse.OrderBy(c => c.CropInfo1Name);

                    if (cropInfoOneResponse.Count > 0)
                    {
                        model.CropInfo1Name = cropInfoOneResponse.FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone)?.CropInfo1Name;
                        model.CropInfo1 = cropInfoOneResponse.FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone)?.CropInfo1Id;

                        for (int i = 0; i < model.Crops?.Count; i++)
                        {
                            model.Crops[i].CropInfo1 = model.CropInfo1;
                        }
                    }

                }
            }
            if (model.CropGroupId != null && model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                model.CropGroup = Resource.lblGrass;
            }
            if (isBasePlan)
            {
                ViewBag.IsBasePlan = isBasePlan;
            }
            model.IsAnyChangeInField = false;

            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) &&
                !string.IsNullOrWhiteSpace(t) && !string.IsNullOrWhiteSpace(u))
            {
                HttpContext.Session.SetObjectAsJson(_cropDataBeforeUpdateSessionKey, model);
            }
            SetCropToSession(model);

            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(t) && !string.IsNullOrWhiteSpace(u))
            {
                SetCropToSession(model);
            }

            var previousModel = HttpContext.Session.GetObjectFromJson<PlanViewModel>(_cropDataBeforeUpdateSessionKey);

            bool isDataChanged = false;

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
            string action = string.Empty;
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == null)
            {
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction(_harvestYearOverviewActionName, new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            else if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value))
            {
                TempData["NutrientRecommendationsError"] = ex.Message;
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction("Recommendations", new
                {
                    q = model.EncryptedFarmId,
                    r = model.EncryptedFieldId,
                    s = model.EncryptedHarvestYear
                });
            }
            List<CropInfoOneResponse> cropInfoOneList = new List<CropInfoOneResponse>();
            if (model.CropGroupId != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other
                && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                cropInfoOneList = await GetCropInfoOneList(model);
            }

            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals)
            {
                TempData["CropInfoTwoError"] = ex.Message;
                action = _cropInfoTwoActionName;
            }
            else if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || cropInfoOneList.Count == 1)
            {
                action = model.YieldQuestion != (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields
                    ? "Yield"
                    : "YieldQuestion";
                if (model.YieldQuestion != (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields)
                {
                    TempData["ErrorOnYield"] = ex.Message;
                }
                else
                {
                    TempData["YieldQuestionError"] = ex.Message;
                }
            }
            else
            {
                TempData["CropInfoOneError"] = ex.Message;
                action = "CropInfoOne";
            }

            return RedirectToAction(action, new { q = model.YieldEncryptedCounter });
        }

        return View(model);
    }

    private async Task<List<CropInfoOneResponse>> GetCropInfoOneList(PlanViewModel model)
    {
        List<CropInfoOneResponse> cropInfoOneResponse = await _cropLogic.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0, model.FarmRB209CountryID);

        return cropInfoOneResponse;
    }

    public async Task<IActionResult> BackCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : BackCheckAnswer() action called");
        PlanViewModel? model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogError("Crop Controller : Session not found in BackCheckAnswer() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        string action = string.Empty;
        try
        {
            bool isComingFromRec = (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value);
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == null)
            {
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction(_harvestYearOverviewActionName, new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            else if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && isComingFromRec)
            {
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction("Recommendations", new
                {
                    q = model.EncryptedFarmId,
                    r = model.EncryptedFieldId,
                    s = model.EncryptedHarvestYear
                });
            }

            action = await BindActionForBackCheckAnswer(model);
            model.IsCheckAnswer = false;
            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in BackCheckAnswer() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName, model);
        }

        string encryptedCounter = FetchEncryptedCounter(model);

        return RedirectToAction(action, new { q = encryptedCounter });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckAnswer(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CheckAnswer() post action called");
        try
        {
            await ValidateCropData(model);

            if (!ModelState.IsValid)
            {
                model = await BindModelInvalidPropertiesForCheckAnswer(model, false);
                return View(_checkAnswerActionName, model);
            }


            Error? error = null;
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);

            int lastGroupNumber = await BindLastGroupName(model);

            List<CropData> cropEntries = await BindCropDataForCheckAnswer(model, lastGroupNumber, userId);
            CropDataWrapper cropDataWrapper = new CropDataWrapper
            {
                Crops = cropEntries
            };

            (bool success, error) = await _cropLogic.AddCropNutrientManagementPlan(cropDataWrapper);

            if (string.IsNullOrWhiteSpace(error?.Message) && success)
            {
                return BackActionForCopyCheckAnswer(model, success);
            }
            else
            {
                TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;
                return RedirectToAction(_checkAnswerActionName);
            }

        }
        catch (Exception ex)
        {
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
    }
    private async Task<List<CropData>> BindCropDataForCheckAnswer(PlanViewModel model, int? lastGroupNumber, int userId)
    {
        List<CropData> cropEntries = new List<CropData>();
        foreach (Crop crop in model.Crops)
        {
            crop.IsBasePlan = false;
            crop.CreatedOn = DateTime.Now;
            crop.CreatedByID = userId;
            crop.FieldName = null;
            crop.EncryptedCounter = null;
            crop.FieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;

            if (string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                if (lastGroupNumber != null)
                {
                    crop.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, (lastGroupNumber + 1));
                }
                else
                {
                    crop.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, 1);
                }
            }
            else
            {
                crop.CropGroupName = model.CropGroupName;
            }
            CropData cropData
                = await BindCropEntryForPost(crop, model, userId);
            cropEntries.Add(cropData);
        }
        return cropEntries;
    }
    private async Task<CropData> BindCropEntryForPost(Crop crop, PlanViewModel model, int userId)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            return new CropData
            {
                Crop = crop,
                ManagementPeriods = new List<ManagementPeriod>
                    {
                        new ManagementPeriod
                        {
                            Defoliation=1,
                            Utilisation1ID=2,
                            CreatedOn=DateTime.Now,
                            CreatedByID=userId
                        }
                    }
            };
        }
        else
        {
            (crop, string defoliationSequence) = await BindDataForGrass(model, crop);

            int i = 1;
            int utilisation1 = 0;
            List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
            if (defoliationSequence != null)
            {
                foreach (char c in defoliationSequence)
                {
                    utilisation1 = BindUtilisation1(c);
                    managementPeriods.Add(new ManagementPeriod
                    {
                        Defoliation = i,
                        Utilisation1ID = utilisation1,
                        Yield = crop.Yield ?? 0 / model.PotentialCut,
                        CreatedOn = DateTime.Now,
                        CreatedByID = userId
                    });
                    i++;
                }
            }

            return new CropData
            {
                Crop = crop,
                ManagementPeriods = managementPeriods
            };
        }
    }
    private async Task<(Crop, string)> BindDataForGrass(PlanViewModel model, Crop crop)
    {
        crop.Yield = crop.Yield ?? 0;

        crop.DefoliationSequenceID = model.DefoliationSequenceId;
        crop.SwardTypeID = model.SwardTypeId;
        crop.SwardManagementID = model.SwardManagementId;
        crop.PotentialCut = model.PotentialCut;
        crop.Establishment = model.GrassSeason ?? 0;

        string defoliationSequence = "";
        (DefoliationSequenceResponse defoliationSequenceResponse, _) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId.Value);
        if (defoliationSequenceResponse != null)
        {
            defoliationSequence = defoliationSequenceResponse.DefoliationSequence;
        }
        return (crop, defoliationSequence);
    }
    private static int BindUtilisation1(char defoliationSequence)
    {
        int utilisation1 = 0;

        if (defoliationSequence == 'E')
        {
            utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Establishment;
        }
        else if (defoliationSequence == 'G')
        {
            utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Grazing;
        }
        else if (defoliationSequence == 'S')
        {
            utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Silage;
        }
        else if (defoliationSequence == 'H')
        {
            utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Hay;
        }



        return utilisation1;
    }
    private async Task<int> BindLastGroupName(PlanViewModel model)
    {
        int lastGroupNumber = 0;
        if (string.IsNullOrWhiteSpace(model.CropGroupName))
        {
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (harvestYearPlanResponse != null)
            {
                var lastGroup = harvestYearPlanResponse.Where(cg => !string.IsNullOrEmpty(cg.CropGroupName) && cg.CropGroupName.StartsWith("Crop group") &&
                                 int.TryParse(cg.CropGroupName.Split(' ')[2], out _))
                                .OrderByDescending(cg => int.Parse(cg.CropGroupName.Split(' ')[2]))
                                .FirstOrDefault();
                if (lastGroup != null)
                {
                    lastGroupNumber = int.Parse(lastGroup.CropGroupName.Split(' ')[2]);
                }
            }
        }
        return lastGroupNumber;
    }
    private async Task<PlanViewModel> BindModelInvalidPropertiesForCheckAnswer(PlanViewModel model, bool isUpdate)
    {


        if (model.CropTypeID != null)
        {
            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYield > 0)
            {
                ViewBag.DefaultYield = defaultYield;
            }
        }

        await BindFieldListForPost(model, isUpdate);

        if (!string.IsNullOrWhiteSpace(model.CropGroupName))
        {
            ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
        }
        if (!string.IsNullOrWhiteSpace(model.CropType))
        {
            ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
        }
        if (!string.IsNullOrWhiteSpace(model.CropType))
        {
            model.EncryptedCropType = _cropDataProtector.Protect(model.CropType);
        }
        if (model.CropOrder != null && model.CropOrder > 0)
        {
            model.EncryptedCropOrder = _cropDataProtector.Protect(model.CropOrder.ToString());
        }

        return model;
    }

    private async Task BindFieldListForPost(PlanViewModel model, bool isUpdate)
    {
        int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
        List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
        if (isUpdate)
        {
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error harvestYearError) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);
            if (string.IsNullOrWhiteSpace(harvestYearError.Message))
            {
                List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                    .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))
                    ).ToList();


            }

            ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();

        }
        else
        {
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                var fieldIds = model.Crops.Select(c => c.FieldID).Distinct();
                fieldList = fieldList.Where(x => fieldIds.Contains(x.ID)).ToList();
            }
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                var fieldIdsForFilter = fieldList.Select(f => f.ID);
                harvestYearPlanResponse = harvestYearPlanResponse
                    .Where(x => fieldIdsForFilter.Contains(x.FieldID))
                    .ToList();
            }


            ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
        }
    }
    private static string FetchEncryptedCounter(PlanViewModel model)
    {
        string encryptedCounter = string.Empty;
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            if (model.GrassGrowthClassQuestion != null)
            {
                encryptedCounter = model.DryMatterYieldEncryptedCounter;
            }
            else
            {
                encryptedCounter = model.GrassGrowthClassEncryptedCounter;
            }
        }
        else
        {
            encryptedCounter = model.YieldEncryptedCounter;
        }
        return encryptedCounter;
    }
    private async Task<string> BindActionForBackCheckAnswer(PlanViewModel model)
    {
        string action = string.Empty;

        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            action = BindActionForBackCheckAnswerGrass(model);
        }
        else
        {
            List<CropInfoOneResponse> cropInfoOneList = await _cropLogic.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0, model.FarmRB209CountryID);
            action = await BindActionForBackCheckAnswerForCereal(model, cropInfoOneList);
        }

        return action;
    }
    private static string BindActionForBackCheckAnswerGrass(PlanViewModel model)
    {
        string action = string.Empty;
        bool isGrazeSilageAndHay = (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay);
        if (isGrazeSilageAndHay)
        {
            if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
            {
                bool isDryMatterAction = (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1);
                action = isDryMatterAction ? "DryMatterYield" : _grassGrowthClassActionName;
            }
            else
            {
                action = "DefoliationSequence";
            }
        }

        bool isGrazeCutAndSilageOnly = (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly);
        if (isGrazeCutAndSilageOnly)
        {
            if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
            {
                action = _grassGrowthClassActionName;
            }
            else
            {
                action = _defoliationActionName;
            }
        }
        return action;
    }
    private static async Task<string> BindActionForBackCheckAnswerForCereal(PlanViewModel model, List<CropInfoOneResponse> cropInfoOneList)
    {
        string action = string.Empty;
        bool isCereal = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals;
        if (isCereal)
        {
            return _cropInfoTwoActionName;
        }
        else
        {
            bool isUseStandardOrNoYield =
            (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields || model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.NoDoNotEnterAYield);

            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || cropInfoOneList.Count == 1)
            {
                action = isUseStandardOrNoYield
                         ? "YieldQuestion"
                         : "Yield";
            }
            else
            {
                action = "CropInfoOne";
            }

        }
        return action;
    }

    private IActionResult BackActionForCopyCheckAnswer(PlanViewModel model, bool success)
    {
        model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
        RemoveCropSession();
        return RedirectToAction(_harvestYearOverviewActionName, new
        {
            id = model.EncryptedFarmId,
            year = model.EncryptedHarvestYear,
            q = _farmDataProtector.Protect(success.ToString()),
            r = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? _cropDataProtector.Protect(string.Format(Resource.MsgCropsAddedForYear, Resource.lblGrass, model.Year)) : _cropDataProtector.Protect(string.Format(Resource.MsgCropsAddedForYear, Resource.lblCrops, model.Year)),
            v = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsNutrientRecommendations) : _cropDataProtector.Protect(Resource.MsgForSuccessCrop)
        });
    }

#pragma warning disable S107
    [HttpGet]
    public async Task<IActionResult> HarvestYearOverview(string id, string year, string? q, string? r, string? s, string? t, string? u, string? v, string? w)//w is a link
    {
        _logger.LogTrace("Crop Controller : HarvestYearOverview({Id}, {Year}, {Q}, {R}) action called", id, year, q, r);
        PlanViewModel? model = null;
        try
        {
            RemoveOrganicDataBeforeUpdateFromSession();
            RemoveFertiliserDataBeforeUpdateFromSession();
            RemoveCropDataBeforeUpdateFromSession();
            RemoveReportDataFromSession();
            RemoveStorageCapacityDataFromSession();
            RemoveFertiliserManureFromSession();
            RemoveOrganicManureFromSession();
            RemoveCropSession();
            await BindSuccessMsgForHarvestYearOverviewPage(q, r, v, w);

            if (string.IsNullOrWhiteSpace(s) && string.IsNullOrWhiteSpace(u))
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    model = new PlanViewModel();
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                    int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));
                    Error? error = null;
                    (model, FarmResponse? farm) = await BindFarmDataForHarvestYearOverview(model, farmId);

                    (bool flowControl, IActionResult? value) = await BindRainfallDataForHarvestYearOverview(model, farmId, harvestYear, error, farm);
                    if (!flowControl && value != null)
                    {
                        return value;
                    }

                    model.Year = harvestYear;
                    (HarvestYearResponseHeader? harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansDetailsByFarmId(harvestYear, farmId);

                    if (harvestYearPlanResponse != null)
                    {
                        List<CropDetailResponse> allCropDetails = harvestYearPlanResponse.CropDetails ?? new List<CropDetailResponse>().ToList();
                        if (allCropDetails.Any())
                        {
                            BindLastModifiedDate(model, allCropDetails);

                            model.AnnualRainfall = harvestYearPlanResponse.farmDetails?.Rainfall;
                            var harvestYearPlans = new HarvestYearPlans
                            {
                                FieldData = new List<HarvestYearPlanFields>(),
                                OrganicManureList = new List<OrganicManureResponse>(),
                                InorganicFertiliserList = new List<InorganicFertiliserResponse>(),
                            };

                            BindFieldDataForHarvestYearOverviewPage(allCropDetails, harvestYearPlans);

                            (bool isSuccess, IActionResult? actionResult) = await BindOrganicManureDataForHarvestYearOverviewPage(model, error, harvestYearPlanResponse, harvestYearPlans);
                            if (!isSuccess && actionResult != null)
                            {
                                return actionResult;
                            }

                            BindFertilizerDataForHarvestYearOverviewPage(harvestYearPlanResponse, harvestYearPlans);
                            BindSortingProperties(model);

                            BindViewBegForSortingList();
                            model.HarvestYearPlans = harvestYearPlans;
                            model.EncryptedFarmId = id;
                            model.EncryptedHarvestYear = year;
                            model.Year = harvestYear;
                            HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                        }
                        else
                        {
                            TempData["ErrorOnHarvestYearOverview"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;//error.Message; //
                            model = null;
                        }
                    }
                }
            }
            else
            {
                (bool flowControl, IActionResult value) = BindSessionDataForHarvestYearOverview(ref model);
                if (!flowControl && value != null)
                {
                    return value;
                }
                if (model != null)
                {
                    model = BindSortingData(s, t, u, model);
                    BindViewBegForSortingList();
                }
            }
            HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in HarvestYearOverview() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;
            model = null;
        }
        return View(model);
    }

    private (bool flowControl, IActionResult? value) BindSessionDataForHarvestYearOverview(ref PlanViewModel? model)
    {
        if (HttpContext.Session.Keys.Contains("HarvestYearPlan"))
        {
            model = HttpContext.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
        }
        else
        {
            _logger.LogError("Crop Controller : Session not found in HarvestYearOverview() action");
            return (flowControl: false, value: Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
        }

        return (flowControl: true, value: null);
    }

    private PlanViewModel BindSortingData(string? s, string? t, string? u, PlanViewModel? model)
    {
        if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(u))
        {
            model = _cropLogic.FilterOrganicAndInorganicListForHarvestYearOverview(model, s, u, t);
        }
        else
        {
            BindSortingProperties(model);
        }

        return model;
    }

    private static void BindLastModifiedDate(PlanViewModel? model, List<CropDetailResponse> allCropDetails)
    {
        var latestDate = allCropDetails
          .Where(x => x.LastModifiedOn.HasValue)
          .OrderByDescending(x => x.LastModifiedOn)
          .FirstOrDefault();

        model.LastModifiedOn = latestDate?.LastModifiedOn?.ToString("dd MMM yyyy");
    }

    private async Task<(bool flowControl, IActionResult? value)> BindRainfallDataForHarvestYearOverview(PlanViewModel? model, int farmId, int harvestYear, Error? error, FarmResponse farm)
    {
        string winterRainfallFirstContent = string.Empty;
        bool isScotland = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland;
        (ExcessRainfalls excessRainfalls, error) = await _farmLogic.FetchExcessRainfallsAsync(farmId, harvestYear);

        if (!string.IsNullOrWhiteSpace(error.Message))
        {
            ViewBag.Error = error.Message;
            return (flowControl: false, value: View(_plansAndRecordsOverviewActionName, new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) }));
        }
        if (excessRainfalls.WinterRainfall != null)
        {
            await BindExcessRainfallDataForHarvestYearOverviewPage(model, excessRainfalls);
            winterRainfallFirstContent = string.Format(Resource.lblWinterRainfallIs450OrMoreOrLess, model.WinterRainfallName);
            string winterRainfallSecondContent = string.Format(Resource.lblExcessWinterRainfallWithValue, model.ExcessWinterRainfallName);
            string winterRainfallThirdContent = string.Format(Resource.lblChangeWinterRainfallForHarvestYear, harvestYear);
            ViewBag.ExcessRainfallContentFirst = (isScotland ? winterRainfallFirstContent : winterRainfallSecondContent);
            ViewBag.ExcessRainfallContentSecond = (isScotland ? winterRainfallThirdContent : Resource.lblUpdateExcessWinterRainfall);

            return (flowControl: true, value: null);
        }

        model.AnnualRainfall = farm?.Rainfall;
        model.IsExcessOrWinterRainfallUpdated = false;
        winterRainfallFirstContent = string.Format(Resource.lblEnterWinterRainfallForHarvestYear, harvestYear);
        ViewBag.ExcessRainfallContentFirst = (isScotland ? Resource.lblYouHaveNotEnteredWinterRainfall : Resource.lblYouHaveNotEnteredAnyExcessWinterRainfall);
        ViewBag.ExcessRainfallContentSecond = (isScotland ? winterRainfallFirstContent : string.Format(Resource.lblAddExcessWinterRainfallForHarvestYear, harvestYear));



        return (flowControl: true, value: null);
    }

    private async Task<(PlanViewModel, FarmResponse)> BindFarmDataForHarvestYearOverview(PlanViewModel? model, int farmId)
    {
        (FarmResponse? farm, _) = await _farmLogic.FetchFarmByIdAsync(farmId);
        if (farm != null)
        {
            model.FarmName = farm.Name;
            model.FarmRB209CountryID = farm.RB209CountryID;
        }

        return (model, farm);
    }

    private void BindViewBegForSortingList()
    {
        ViewBag.InOrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
        ViewBag.InOrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
        ViewBag.InOrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
        ViewBag.OrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
        ViewBag.OrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
        ViewBag.OrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
    }

    private void BindSortingProperties(PlanViewModel model)
    {
        model.EncryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
        model.EncryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
        model.EncryptSortOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
        model.SortInOrganicListOrderByDate = Resource.lblDesc;
        model.SortOrganicListOrderByDate = Resource.lblDesc;
        model.EncryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
        model.EncryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
        model.EncryptSortInOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
        model.SortInOrganicListOrderByFieldName = null;
        model.SortOrganicListOrderByFieldName = null;
        model.SortInOrganicListOrderByCropType = null;
        model.SortOrganicListOrderByCropType = null;

    }

    private void BindFertilizerDataForHarvestYearOverviewPage(HarvestYearResponseHeader harvestYearPlanResponse, HarvestYearPlans harvestYearPlans)
    {
        if (harvestYearPlanResponse.InorganicFertiliserApplication.Count > 0)
        {
            harvestYearPlans.InorganicFertiliserList = harvestYearPlanResponse.InorganicFertiliserApplication.OrderByDescending(x => x.ApplicationDate).ToList();
            harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFertId = _cropDataProtector.Protect(m.ID.ToString()));
            ViewBag.Fertliser = _cropDataProtector.Protect(Resource.lblFertiliser);
            harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));
        }
    }

    private async Task<(bool flowControl, IActionResult? value)> BindOrganicManureDataForHarvestYearOverviewPage(PlanViewModel? model, Error? error, HarvestYearResponseHeader harvestYearPlanResponse, HarvestYearPlans harvestYearPlans)
    {
        if (harvestYearPlanResponse.OrganicMaterial.Count > 0)
        {
            harvestYearPlans.OrganicManureList = harvestYearPlanResponse.OrganicMaterial.OrderByDescending(x => x.ApplicationDate).ToList();
            harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedId = _cropDataProtector.Protect(m.ID.ToString()));
            ViewBag.Organic = _cropDataProtector.Protect(Resource.lblOrganic);
            harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));
            foreach (var organic in harvestYearPlans.OrganicManureList)
            {
                (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(organic.ManureTypeId.Value);
                if (error == null && manureType != null)
                {
                    organic.RateUnit = manureType.IsLiquid.HasValue && manureType.IsLiquid.Value ? Resource.lblCubicMeters : Resource.lbltonnes;
                }
                else
                {
                    ViewBag.Error = error?.Message;
                    return (flowControl: false, value: View(_plansAndRecordsOverviewActionName, new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()) }));
                }
            }
        }

        return (flowControl: true, value: null);
    }

    private void BindFieldDataForHarvestYearOverviewPage(List<CropDetailResponse> allCropDetails, HarvestYearPlans harvestYearPlans)
    {
        var groupedResult = allCropDetails
                            .GroupBy(crop => new { crop.CropTypeName, crop.CropGroupName, crop.CropTypeID })
                            .Select(g => new
                            {
                                CropTypeName = g.Key.CropTypeName,
                                CropGroupName = g.Key.CropGroupName,
                                CropTypeID = g.Key.CropTypeID,
                                HarvestPlans = g.ToList()
                            })
                            .OrderBy(g => g.CropTypeName);
        foreach (var group in groupedResult)
        {
            var newField = new HarvestYearPlanFields
            {
                CropTypeID = group.CropTypeID,
                CropTypeName = group.CropTypeName,
                CropGroupName = group.CropGroupName,
                EncryptedCropTypeName = _cropDataProtector.Protect((group.CropTypeName)),
                EncryptedCropGroupName = string.IsNullOrWhiteSpace(group.CropGroupName) ? null : _cropDataProtector.Protect((group.CropGroupName)),
                FieldData = new List<FieldDetails>()
            };
            foreach (var plan in group.HarvestPlans)
            {
                var fieldDetail = new FieldDetails
                {
                    EncryptedFieldId = _fieldDataProtector.Protect(plan.FieldID.ToString()), // Encrypt field ID
                    FieldName = plan.FieldName,
                    PlantingDate = plan.PlantingDate,
                    Yield = plan.Yield,
                    Variety = plan.CropVariety,
                    CropOrder = plan.CropOrder
                };

                if (plan.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && !string.IsNullOrWhiteSpace(plan.Management))
                {
                    List<string> defoliationList = plan.Management
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

                    fieldDetail.Management = CommonHelpers.ShorthandDefoliationSequence(defoliationList);
                }

                newField.FieldData.Add(fieldDetail);
            }
            harvestYearPlans.FieldData.Add(newField);
        }
    }

    private async Task BindExcessRainfallDataForHarvestYearOverviewPage(PlanViewModel? model, ExcessRainfalls excessRainfalls)
    {
        model.ExcessWinterRainfallValue = excessRainfalls.WinterRainfall.Value;
        if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland)
        {
            model.IsWinterRainfallMoreThan450 = excessRainfalls.WinterRainfall == 500;
            if (model.IsWinterRainfallMoreThan450.Value)
            {
                model.WinterRainfallName = Resource.lbl450OrMore;
            }
            else
            {
                model.WinterRainfallName = Resource.lblLessThan450;
            }
        }
        model.AnnualRainfall = excessRainfalls.WinterRainfall.Value;
        model.IsExcessOrWinterRainfallUpdated = true;
        (List<CommonResponse> excessWinterRainfallOption, _) = await _farmLogic.FetchExcessWinterRainfallOptionAsync();
        if (excessWinterRainfallOption != null && excessWinterRainfallOption.Count > 0)
        {
            CommonResponse? selectedOption = excessWinterRainfallOption.FirstOrDefault(x => x.Value == model.ExcessWinterRainfallValue);
            if (selectedOption != null)
            {
                string excessRainfallName = selectedOption.Name;
                string[] parts = excessRainfallName.Split(new string[] { " - " }, StringSplitOptions.None);
                model.ExcessWinterRainfallName = $"{parts[0]} ({parts[1]})";
                model.ExcessWinterRainfallId = selectedOption.Id;
            }
        }

    }

    private async Task BindSuccessMsgForHarvestYearOverviewPage(string? q, string? r, string? v, string? w)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            ViewBag.Success = false;
            RemoveCropSession();
            return;
        }

        if (!string.IsNullOrWhiteSpace(r))
        {
            TempData["successMsg"] = _cropDataProtector.Unprotect(r);
            if (!string.IsNullOrWhiteSpace(v))
            {
                TempData["successMsgSecond"] = _cropDataProtector.Unprotect(v);
            }
            if (!string.IsNullOrWhiteSpace(w))
            {
                int decryptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(w));

                Field field = await _fieldLogic.FetchFieldByFieldId(decryptedFieldId);
                if (field != null)
                {
                    TempData["fieldName"] = field.Name;
                }
                TempData["successMsgLink"] = w;
            }
        }
        ViewBag.Success = true;
    }
#pragma warning restore S107
    private void RemoveOrganicManureFromSession()
    {
        if (HttpContext.Session.Exists("OrganicManure"))
        {
            HttpContext.Session.Remove("OrganicManure");
        }
    }

    private void RemoveFertiliserManureFromSession()
    {
        if (HttpContext.Session.Exists("FertiliserManure"))
        {
            HttpContext.Session.Remove("FertiliserManure");
        }
    }

    private void RemoveStorageCapacityDataFromSession()
    {
        if (HttpContext.Session.Exists("StorageCapacityData"))
        {
            HttpContext.Session.Remove("StorageCapacityData");
        }
    }

    private void RemoveReportDataFromSession()
    {
        if (HttpContext.Session.Exists("ReportData"))
        {
            HttpContext.Session.Remove("ReportData");
        }
    }

    private void RemoveCropDataBeforeUpdateFromSession()
    {
        if (HttpContext.Session.Exists(_cropDataBeforeUpdateSessionKey))
        {
            HttpContext.Session.Remove(_cropDataBeforeUpdateSessionKey);
        }
    }

    private void RemoveFertiliserDataBeforeUpdateFromSession()
    {
        if (HttpContext.Session.Exists("FertiliserDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("FertiliserDataBeforeUpdate");
        }
    }

    private void RemoveOrganicDataBeforeUpdateFromSession()
    {
        if (HttpContext.Session.Exists("OrganicDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("OrganicDataBeforeUpdate");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HarvestYearOverview(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : HarvestYearOverview() post action called");
        if (!ModelState.IsValid)
        {
            return await Task.FromResult(View(model));
        }
        return await Task.FromResult(View(model));
    }
    private void RemoveInoganicAndOrganicSession()
    {
        if (HttpContext.Session.Exists("FertiliserManure"))
        {
            HttpContext.Session.Remove("FertiliserManure");
        }
        if (HttpContext.Session.Exists("OrganicManure"))
        {
            HttpContext.Session.Remove("OrganicManure");
        }
    }


    private async Task<PlanViewModel> FetchOldestPrviousCropping(PlanViewModel model, int farmId, List<PlanSummaryResponse> planSummaryResponse)
    {
        (int? topPrevCroppingYear, _) = await _previousCroppingLogic.FetchPreviousCroppingYearByFarmdId(farmId);
        if (topPrevCroppingYear > 0)
        {
            DateTime currentDate = DateTime.Now;
            DateTime harvestYearEndDate = new DateTime(currentDate.Year, 7, 31, 00, 00, 00, DateTimeKind.Unspecified);
            int currentHarvestYear = 0;
            if (currentDate > harvestYearEndDate)
            {
                currentHarvestYear = currentDate.Year + 1;
            }
            else
            {
                currentHarvestYear = currentDate.Year;
            }

            //To show the list Create Plan for year (2023,2024,..) 
            List<int> yearList = new List<int>();
            model = FetchDataForPlanAndRecords(model, planSummaryResponse, topPrevCroppingYear, currentHarvestYear, yearList);
        }

        if (model.HarvestYear?.Count > 0)
        {
            model.HarvestYear = model.HarvestYear.OrderByDescending(x => x.Year).ToList();
        }

        return model;
    }

    private PlanViewModel FetchDataForPlanAndRecords(PlanViewModel model, List<PlanSummaryResponse> planSummaryResponse, int? topPrevCroppingYear, int currentHarvestYear, List<int> yearList)
    {
        if (planSummaryResponse != null && planSummaryResponse.Count > 0)
        {
            model = FetchHarvestYearList(model, planSummaryResponse, topPrevCroppingYear, currentHarvestYear, yearList, true);
        }
        else
        {
            for (int i = topPrevCroppingYear.GetValueOrDefault(); i <= currentHarvestYear; i++)
            {
                var harvestYear = new HarvestYear
                {
                    Year = i,
                    EncryptedYear = _farmDataProtector.Protect(i.ToString()),
                    IsAnyPlan = false
                };

                if (topPrevCroppingYear == i)
                {
                    harvestYear.IsThisOldYear = true;
                }
                model.HarvestYear?.Add(harvestYear);
            }
        }

        return model;
    }


    private async Task<(PlanViewModel, List<PlanSummaryResponse>)> FetchPlanAndCropYourPlanData(string? year, PlanViewModel model, int farmId, bool isCropPlanData)
    {
        List<PlanSummaryResponse> planSummaryResponse = await _cropLogic.FetchPlanSummaryByFarmId(farmId, 0);
        planSummaryResponse.RemoveAll(x => x.Year == 0);
        planSummaryResponse = planSummaryResponse.OrderByDescending(x => x.Year).ToList();
        model.EncryptedHarvestYearList = new List<string>();
        foreach (var planSummary in planSummaryResponse)
        {
            model.EncryptedHarvestYearList.Add(_farmDataProtector.Protect(planSummary.Year.ToString()));
        }
        if (!string.IsNullOrWhiteSpace(year))
        {
            model.EncryptedHarvestYear = year;
            if (isCropPlanData)
            {
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
            }
        }

        ViewBag.PlanSummaryList = planSummaryResponse;
        return (model, planSummaryResponse);
    }
    [HttpGet]
    public async Task<IActionResult> PlansAndRecordsOverview(string id, string? year, string? q)
    {
        _logger.LogTrace("Crop Controller : PlansAndRecordsOverview({Id}, {Year}) action called", id, year);
        PlanViewModel model = new PlanViewModel();

        RemoveInoganicAndOrganicSession();
        RemoveCropSession();
        if (!string.IsNullOrWhiteSpace(q))
        {
            TempData["successMsg"] = _cropDataProtector.Unprotect(q);
            ViewBag.Success = true;
        }
        if (!string.IsNullOrWhiteSpace(id))
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
            (FarmResponse? farm, _) = await _farmLogic.FetchFarmByIdAsync(farmId);
            model.FarmName = farm?.Name;
            (model, List<PlanSummaryResponse> planSummaryResponse) = await FetchPlanAndCropYourPlanData(year, model, farmId, false);

            //fetch oldest previous cropping
            model = await FetchOldestPrviousCropping(model, farmId, planSummaryResponse);

            model.EncryptedFarmId = id;

        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlansAndRecordsOverview(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : PlansAndRecordsOverview() post action called");
        if (!ModelState.IsValid)
        {
            return await Task.FromResult(View(model));
        }
        return View(model);
    }
    private PlanViewModel FetchHarvestYearList(PlanViewModel model, List<PlanSummaryResponse> planSummaryResponse, int? topPrevCroppingYear, int currentHarvestYear, List<int> yearList, bool isComingFromPlanAndOverview)
    {
        foreach (var item in planSummaryResponse)
        {
            yearList.Add(item.Year);
        }

        for (int j = 0; j < planSummaryResponse.Count; j++)
        {
            var harvestNewYear = new HarvestYear
            {
                Year = planSummaryResponse[j].Year,
                EncryptedYear = _farmDataProtector.Protect(planSummaryResponse[j].Year.ToString()),
                LastModifiedOn = planSummaryResponse[j].LastModifiedOn,
                IsAnyPlan = true
            };
            model.HarvestYear?.Add(harvestNewYear);
        }

        int minYear = topPrevCroppingYear < planSummaryResponse.Min(x => x.Year) ? topPrevCroppingYear.GetValueOrDefault() : planSummaryResponse.Min(x => x.Year) - 1;
        int maxYear = planSummaryResponse.Max(x => x.Year) < currentHarvestYear ? currentHarvestYear : planSummaryResponse.Max(x => x.Year) + 1;
        for (int i = minYear; i <= maxYear; i++)
        {
            if (!yearList.Contains(i))
            {
                var harvestYear = new HarvestYear
                {
                    Year = i,
                    EncryptedYear = _farmDataProtector.Protect(i.ToString()),
                    IsAnyPlan = false
                };
                if (isComingFromPlanAndOverview && minYear == i)
                {
                    harvestYear.IsThisOldYear = true;
                }
                model.HarvestYear?.Add(harvestYear);
            }
        }
        return model;
    }


    [HttpGet]
    public async Task<IActionResult> Recommendations(string q, string r, string? s, string? t, string? u, string? sns)//q=farmId,r=fieldId,s=harvestYear
    {
        _logger.LogTrace("Crop Controller : Recommendations({Q}, {R}, {S}) action called", q, r, s);
        RecommendationViewModel model = new RecommendationViewModel();
        Error? error = null;
        int decryptedFarmId = 0;
        int decryptedFieldId = 0;
        int decryptedHarvestYear = 0;
        List<RecommendationHeader>? recommendations = null;
        try
        {
            RemoveSessionForRecommendation();

            BindSuccessMsgForRecommendation(sns, t, u);

            (model, decryptedFarmId, decryptedFieldId, decryptedHarvestYear) = await BindParameterPropertiesForRecommendation(q, r, s, model, decryptedFarmId, decryptedFieldId, decryptedHarvestYear);


            (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(decryptedHarvestYear, decryptedFarmId);
            if (harvestYearPlanResponse != null && error == null)
            {
                bool isAllBasePlan = harvestYearPlanResponse.All(h => ((h.IsBasePlan != null) && (h.IsBasePlan.Value)));
                if (isAllBasePlan)
                {
                    ViewBag.AddMannerDisabled = true;
                }
                if (decryptedFieldId > 0 && decryptedHarvestYear > 0)
                {
                    (recommendations, error) = await _cropLogic.FetchRecommendationByFieldIdAndYear(decryptedFieldId, decryptedHarvestYear);
                    if (recommendations != null && recommendations.Any())
                    {
                        ViewBag.IsComingFromRecommendation = _cropDataProtector.Protect(Resource.lblFalse.ToString());
                        string firstCropName = string.Empty;
                        (bool flowControl, IActionResult? value, model, firstCropName) = await BindAllRecommendationData(q, s, model, error, recommendations, firstCropName);
                        if (!flowControl && value != null)
                        {
                            return value;
                        }

                        await BindPreviousCropping(s, model, error, decryptedFieldId, decryptedHarvestYear, firstCropName);
                    }
                }
            }
            else
            {
                TempData["ErrorOnHarvestYearOverview"] = error.Message;
                return RedirectToAction(_harvestYearOverviewActionName, new
                {
                    id = q,
                    year = s
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in Recommendations() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = string.Concat(error != null ? error.Message : "", ex.Message);
            return RedirectToAction(_harvestYearOverviewActionName, new
            {
                id = q,
                year = s
            });
        }

        return View(model);
    }

    private async Task BindPreviousCropping(string? s, RecommendationViewModel model, Error? error, int decryptedFieldId, int decryptedHarvestYear, string firstCropName)
    {
        int count = 0;
        List<int> yearsToCheck = new List<int> { decryptedHarvestYear - 1, decryptedHarvestYear - 2, decryptedHarvestYear - 3 };
        List<int> missingYears = new List<int>();
        List<Crop> planList = await _cropLogic.FetchCropsByFieldId(decryptedFieldId);
        if (planList.Count > 0)
        {
            count = planList.Count(x => yearsToCheck.Contains(x.Year));
            missingYears = yearsToCheck.Where(year => !planList.Any(x => x.Year == year)).ToList();
        }

        if (count < 3 && missingYears.Count > 0)
        {
            (List<PreviousCroppingData> previousCropping, error) = await _previousCroppingLogic.FetchDataByFieldId(decryptedFieldId, null);
            if (previousCropping != null)
            {
                if (previousCropping.Count > 0)
                {
                    previousCropping = previousCropping.Where(x => missingYears.Contains(x.HarvestYear.Value)).ToList();
                }
                if (missingYears.Count != previousCropping.Count)
                {
                    foreach (var missingYear in missingYears)
                    {
                        string encryptedYear = _farmDataProtector.Protect((decryptedHarvestYear - 1).ToString());
                        ViewBag.PreviousYear = s;
                        ViewBag.IsThereAnyPreviousCropping = false;
                        TempData["PreviousCroppingContentOne"] = Resource.lblRecommendationNotAvailable;
                        TempData["PreviousCroppingContentSecond"] = string.Format(Resource.lblPreviousCroppingContentOnRecommendation, firstCropName, decryptedHarvestYear, model.FieldName);
                        TempData["PreviousCroppingContentThird"] = string.Format(Resource.lblAddYearCropDetailsForFieldName, model.FieldName);
                    }
                }
            }
        }


    }

    private void BindSuccessMsgForRecommendation(string? sns, string? t, string? u)
    {
        if (!string.IsNullOrWhiteSpace(sns))
        {
            TempData["successSnsAnalysis"] = _cropDataProtector.Unprotect(sns);
        }

        if (!string.IsNullOrWhiteSpace(t))
        {
            ViewBag.Success = true;
            TempData["successMsg"] = _cropDataProtector.Unprotect(t);
            if (!string.IsNullOrWhiteSpace(u))
            {
                TempData["successMsgSecond"] = _cropDataProtector.Unprotect(u);
            }
        }

    }
    public void RemoveSessionForRecommendation()
    {
        if (HttpContext.Session.Exists("OrganicDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("OrganicDataBeforeUpdate");
        }
        if (HttpContext.Session.Exists("FertiliserDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("FertiliserDataBeforeUpdate");
        }
        if (HttpContext.Session.Exists(_cropDataBeforeUpdateSessionKey))
        {
            HttpContext.Session.Remove(_cropDataBeforeUpdateSessionKey);
        }
        if (HttpContext.Session.Exists("PreviousCroppingData"))
        {
            HttpContext.Session.Remove("PreviousCroppingData");
        }
    }
    private async Task<(bool flowControl, IActionResult? value, RecommendationViewModel, string)> BindAllRecommendationData(string q, string? s, RecommendationViewModel model, Error? error, List<RecommendationHeader> recommendations, string firstCropName)
    {
        int cropCounter = 0;

        (model, firstCropName) = await _cropLogic.BindDataForRecommendation(q, s, model, error, recommendations, firstCropName);

        foreach (var recommendation in recommendations)
        {
            cropCounter++;
            var crop = recommendation.Crops;
            if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && !string.IsNullOrWhiteSpace(recommendation.Crops.DefoliationSequenceName))
            {
                List<string> defoliationList = recommendation.Crops.DefoliationSequenceName
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                crop.DefoliationSequenceName = CommonHelpers.ShorthandDefoliationSequence(defoliationList);
            }
            await BindViewBegForRecommendation(cropCounter, crop);
            string defolicationName = await _cropLogic.BindDefoliationNameForRecommendation(recommendation, crop);


            string[]? defolicationParts = defolicationName?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int defIndex = 0;
            if (recommendation.RecommendationData?.Any() == true)
            {
                foreach (var recData in recommendation.RecommendationData)
                {
                    string defoliationSequenceName = _cropLogic.BindDefoliationSequenceNameForRecommendation(defolicationParts, defIndex);
                    model = _cropLogic.BindManagementPeriodForRecommendation(model, recData, defoliationSequenceName);

                    defIndex++;
                    (bool flowControl, IActionResult? value, model, firstCropName) = await BindRecOrgAndFertForRecommendation(q, s, model, error, firstCropName, recData);
                    if (!flowControl && value != null)
                    {
                        return (flowControl, value, model, firstCropName);
                    }
                }
            }
        }

        return (flowControl: true, value: null, model, firstCropName);
    }

    private async Task BindViewBegForRecommendation(int cropCounter, CropViewModel crop)
    {
        if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && crop.PotentialCut != null)
        {
            var potentialCuts = new[]
            {
                                    Resource.lblOne.ToLower(), Resource.lblTwo.ToLower(), Resource.lblThree.ToLower(), Resource.lblFour.ToLower(),
                                    Resource.lblFive.ToLower(), Resource.lblSix.ToLower(), Resource.lblSeven.ToLower(), Resource.lblEight.ToLower(), Resource.lblNine.ToLower()
                                };


            (DefoliationSequenceResponse defoliationSequence, _) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
            bool defoliationSequenceDescription = defoliationSequence.DefoliationSequenceDescription.Contains(Resource.lblEstablishment);
            if (defoliationSequenceDescription)
            {
                string grassHeading = string.Format(Resource.lblThereAreCountCutsAndGrazingsPlusEstablishment, potentialCuts[(int)crop.PotentialCut - 1]);
                if (cropCounter == 1)
                {
                    ViewBag.GrassHeadingCropOne = grassHeading;
                }
                else if (cropCounter == 2)
                {
                    ViewBag.GrassHeadingCropTwo = grassHeading;
                }
            }
            else
            {
                string grassHeading = string.Format(Resource.lblThereAreCountCutsAndGrazings, potentialCuts[(int)crop.PotentialCut - 1]);
                if (cropCounter == 1)
                {
                    ViewBag.GrassHeadingCropOne = grassHeading;
                }
                else if (cropCounter == 2)
                {
                    ViewBag.GrassHeadingCropTwo = grassHeading;
                }
            }
        }

    }

    private async Task<(bool flowControl, IActionResult? value, RecommendationViewModel, string)> BindRecOrgAndFertForRecommendation(string q, string? s, RecommendationViewModel model, Error? error, string firstCropName, RecommendationData recData)
    {
        CommonHelpers commonHelpers = new CommonHelpers();
        if (recData.Recommendation != null)
        {
            var rec = commonHelpers.FetchRecommendation(recData.Recommendation);
            model.Recommendations.Add(rec);

        }
        if (recData.RecommendationComments != null && recData.RecommendationComments.Any())
        {
            model = _cropLogic.BindRecommendationCommentForRecommendation(model, recData);
        }

        if (recData.OrganicManures.Count > 0)
        {
            foreach (var item in recData.OrganicManures)
            {
                (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(item.ManureTypeID);
                if (error == null && manureType != null)
                {
                    model = _cropLogic.BindOrganicManureDataForRecommendation(model, item, manureType);
                }
                else
                {
                    return (flowControl: false, value: RedirectToAction(_harvestYearOverviewActionName, new
                    {
                        id = q,
                        year = s
                    }), model, firstCropName);
                }
            }

            ViewBag.OrganicManure = _cropDataProtector.Protect(Resource.lblOrganic);
            model.OrganicManures = model.OrganicManures.OrderByDescending(x => x.ApplicationDate).ToList();
        }
        if (recData.FertiliserManures.Count > 0)
        {
            model = _cropLogic.BindFertiliserDataForRecommendation(model, recData);
            ViewBag.Fertiliser = _cropDataProtector.Protect(Resource.lblFertiliser);
            model.FertiliserManures = model.FertiliserManures.OrderByDescending(x => x.ApplicationDate).ToList();

        }

        return (flowControl: true, value: null, model, firstCropName);
    }

    private async Task<(RecommendationViewModel model, int decryptedFarmId, int decryptedFieldId, int decryptedHarvestYear)> BindParameterPropertiesForRecommendation(string q, string r, string? s, RecommendationViewModel model, int decryptedFarmId, int decryptedFieldId, int decryptedHarvestYear)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            (FarmResponse? farm, _) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
            if (farm != null)
            {
                model.FarmName = farm.Name;
                model.FarmRB209CountryID = farm.RB209CountryID;
            }
            model.EncryptedFarmId = q;
        }

        if (!string.IsNullOrWhiteSpace(r))
        {
            decryptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(r));
            model.EncryptedFieldId = r;
        }

        if (!string.IsNullOrWhiteSpace(s))
        {
            decryptedHarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
            model.EncryptedHarvestYear = s;
        }

        return (model, decryptedFarmId, decryptedFieldId, decryptedHarvestYear);
    }

    private async Task<(List<int>, List<int>)> FetchAllowedFieldsForSecondCrop(List<HarvestYearPlanResponse> harvestYearPlanResponse, int harvestYear, int cropTypeId, PlanViewModel model, bool isUpdate = false, List<Crop>? updatedCrop = null, int? rb209CountryId = 3)
    {

        List<int> fieldRemoveList = await BindFieldRemoveListForSecondCrop(cropTypeId, harvestYear, isUpdate, rb209CountryId, updatedCrop);

        List<int> fieldsAllowedForSecondCrop = await BindFieldsAllowedForSecondCrop(harvestYearPlanResponse, harvestYear, cropTypeId, model, rb209CountryId ?? 3);
        return (fieldsAllowedForSecondCrop, fieldRemoveList);
    }

    private async Task<List<int>> BindFieldRemoveListForSecondCrop(int cropTypeId, int harvestYear, bool isUpdate = false, int? rb209CountryId = 3, List<Crop>? updatedCrop = null)
    {
        List<int> fieldRemoveList = new List<int>();
        if (updatedCrop != null)
        {
            foreach (var crop in updatedCrop)
            {
                if (crop.CropOrder == 2)
                {
                    List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(crop.FieldID.Value);
                    int cropPlanCount = cropsResponse.Count(x => x.Year == harvestYear && !x.Confirm);
                    int firstCropType = cropsResponse.Where(x => x.Year == harvestYear && !x.Confirm && x.CropOrder == 1).Select(c => c.CropTypeID.Value).FirstOrDefault();
                    fieldRemoveList = await BindFieldRemoveList(cropPlanCount, cropTypeId, firstCropType, crop, fieldRemoveList, rb209CountryId, isUpdate);
                }
            }
        }
        return fieldRemoveList;
    }

    private async Task<List<int>> BindFieldRemoveList(int cropPlanCount, int cropTypeId, int firstCropType, Crop crop, List<int> fieldRemoveList, int? rb209CountryId = 3, bool isUpdate = false)
    {
        if (isUpdate && cropPlanCount == 2)
        {
            List<int>? secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropType, rb209CountryId ?? 3);
            if (!secondCropList.Any() || !secondCropList.Contains(cropTypeId))
            {
                fieldRemoveList.Add(crop.FieldID.Value);
            }
        }
        return fieldRemoveList;
    }
    private async Task<List<int>> BindFieldsAllowedForSecondCrop(List<HarvestYearPlanResponse> harvestYearPlanResponse, int harvestYear, int cropTypeId, PlanViewModel model, int? rb209CountryId = 3)
    {
        List<int> fieldsAllowedForSecondCrop = new List<int>();
        foreach (var firstCropPlans in harvestYearPlanResponse)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(firstCropPlans.FieldID);
            int cropPlanCount = cropsResponse.Count(x => x.Year == harvestYear && !x.Confirm);
            if (cropPlanCount == 1 || (model.FieldList != null && !model.FieldList.Contains(firstCropPlans.FieldID.ToString())))
            {
                List<int> secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID, rb209CountryId ?? 3);
                if (secondCropList.Contains(cropTypeId))
                {
                    fieldsAllowedForSecondCrop.Add(firstCropPlans.FieldID);
                }
            }
        }
        return fieldsAllowedForSecondCrop;
    }

    private async Task<bool> IsSecondCropAllowed(List<CropDetailResponse> CropDetailResponse, int rb209CountryId)
    {
        bool isSecondCropAllowed = false;
        foreach (var firstCropPlans in CropDetailResponse)
        {
            List<int> secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID, rb209CountryId);
            if (secondCropList?.Any() == true)
            {
                isSecondCropAllowed = true;
            }
        }

        return isSecondCropAllowed;
    }

    [HttpGet]
    public IActionResult SortOrganicList(string year, string id, string q, string r)
    {
        _logger.LogTrace("Crop Controller : SortOrganicList() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();

        if (model == null)
        {
            _logger.LogTrace("Crop Controller : SortOrganicList() - HarvestYearPlan not found in session, redirecting to FarmList");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            string decrypt = _cropDataProtector.Unprotect(q);
            switch (decrypt)
            {
                case var d when d == Resource.lblField:
                    model.SortOrganicListOrderByDate = null;
                    model.SortOrganicListOrderByCropType = null;
                    break;

                case var d when d == Resource.lblDate:
                    model.SortOrganicListOrderByFieldName = null;
                    model.SortOrganicListOrderByCropType = null;
                    break;

                case var d when d == Resource.lblCropType:
                    model.SortOrganicListOrderByFieldName = null;
                    model.SortOrganicListOrderByDate = null;
                    break;
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                r = BindSortOrder(r);
            }
        }

        SetCropToSession(model);
        return Redirect(Url.Action(_harvestYearOverviewActionName, new { year = year, id = id, s = q, t = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationsForSorting), u = r }) + Resource.lblOrganicMaterialApplicationsForSorting);
    }

    private string BindSortOrder(string r)
    {
        string decryptOrderBy = _cropDataProtector.Unprotect(r);
        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblDesc)
        {
            r = _cropDataProtector.Protect(Resource.lblAsc);
        }
        if (!string.IsNullOrWhiteSpace(decryptOrderBy) && decryptOrderBy == Resource.lblAsc)
        {
            r = _cropDataProtector.Protect(Resource.lblDesc);
        }

        return r;
    }

    [HttpGet]
    public IActionResult SortInOrganicList(string year, string id, string q, string r)
    {
        _logger.LogTrace("Crop Controller : SortInOrganicList() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();

        if (model == null)
        {
            _logger.LogTrace("Crop Controller : session not found in SortInOrganicList() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            string decrypt = _cropDataProtector.Unprotect(q);
            switch (decrypt)
            {
                case var d when d == Resource.lblField:
                    model.SortInOrganicListOrderByDate = null;
                    model.SortInOrganicListOrderByCropType = null;
                    break;

                case var d when d == Resource.lblDate:
                    model.SortInOrganicListOrderByFieldName = null;
                    model.SortInOrganicListOrderByCropType = null;
                    break;

                case var d when d == Resource.lblCropType:
                    model.SortInOrganicListOrderByFieldName = null;
                    model.SortInOrganicListOrderByDate = null;
                    break;
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                r = BindSortOrder(r);
            }

        }

        SetCropToSession(model);
        return Redirect(Url.Action(_harvestYearOverviewActionName, new { year = year, id = id, s = q, t = _cropDataProtector.Protect(Resource.lblInorganicFertiliserApplicationsForSorting), u = r }) + Resource.lblInorganicFertiliserApplicationsForSorting);
    }

    [HttpGet]
    public async Task<IActionResult> CropGroupName()
    {
        _logger.LogTrace("Crop Controller : CropGroupName() action called");

        PlanViewModel model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogTrace("Crop Controller : session not found in CropGroupName() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }

        try
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            bool isCropUpdate = !string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate);

            var fieldList = await GetFilteredFields(model, farmId, isCropUpdate);
            var selectListItems = MapToSelectList(fieldList);

            await ApplySecondCropRules(model, farmId, isCropUpdate, selectListItems);

            ViewBag.fieldList = selectListItems;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropGroupName() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnSelectField"] = ex.Message;
            return RedirectToAction("CropFields");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CropGroupName(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropGroupName() post action called");
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Error? error = null;
            bool isThereAnyPreviousFieldLeft = await IsThereAnyPeriousFieldLeftFilter(model);
            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                (bool flowControl, IActionResult? value) = await FetchCropGroupNameExist(model, error, isThereAnyPreviousFieldLeft);
                if (!flowControl && value != null)
                {
                    return value;
                }
            }

            for (int i = 0; i < model.Crops.Count; i++)
            {
                model.Crops[i].CropGroupName = model.CropGroupName;
            }

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropGroupName() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropGroupNameError"] = ex.Message;
            return RedirectToAction("CropGroupName");
        }

        if (model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange) && (!model.IsAnyChangeInField))
        {
            return RedirectToAction(_checkAnswerActionName);
        }

        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            return RedirectToAction("CurrentSward");
        }
        return RedirectToAction("VarietyName");
    }

    private async Task<(bool flowControl, IActionResult? value)> FetchCropGroupNameExist(PlanViewModel model, Error? error, bool isThereAnyPreviousFieldLeft)
    {
        if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) || (!isThereAnyPreviousFieldLeft) || (model.IsFieldToBeRemoved.HasValue && model.IsFieldToBeRemoved.Value))
        {
            (List<HarvestYearPlanResponse> harvestYearPlanResponses, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (error == null && harvestYearPlanResponses.Count > 0)
            {
                bool cropGroupNameExists = harvestYearPlanResponses
                   .Any(harvest =>
                   !string.IsNullOrEmpty(harvest.CropGroupName) && harvest.CropGroupName.Equals(model.CropGroupName)
                   && harvest.Year == model.Year);

                if (cropGroupNameExists)
                {
                    ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                    return (flowControl: false, value: View(model));
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.Crops != null && model.Crops.Count > 0)
        {
            string cropIds = string.Join(",", model.Crops.Select(x => x.ID));
            (bool groupNameExist, error) = await _cropLogic.IsCropsGroupNameExistForUpdate(cropIds, model.CropGroupName, model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (string.IsNullOrWhiteSpace(error?.Message) && groupNameExist)
            {
                ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                return (flowControl: false, value: View(model));
            }
        }

        return (flowControl: true, value: null);
    }

    private async Task<bool> IsThereAnyPeriousFieldLeftFilter(PlanViewModel model)
    {
        bool isThereAnyPreviousFieldLeft = false;
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (List<HarvestYearPlanResponse> crops, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmId);
            if (crops.Count > 0)
            {
                List<string> fieldIds = crops.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID.ToString()).ToList();
                if (fieldIds.Count > 0 && fieldIds.Any(fieldId => model.FieldList.Contains(fieldId)))
                {
                    isThereAnyPreviousFieldLeft = true;
                }
            }
        }

        return isThereAnyPreviousFieldLeft;
    }

    [HttpGet]
    public async Task<IActionResult> RemoveCrop(string? q, string? r = null, string? s = null, string? t = null, string? u = null, string? v = null, string? w = null)
    {
        _logger.LogTrace("Crop Controller : RemoveCrop() action called");
        PlanViewModel? model = GetCropFromSession() ?? new PlanViewModel();
        try
        {
            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r))
            {
                _logger.LogTrace("Crop Controller : session not found in RemoveCrop() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (!string.IsNullOrWhiteSpace(v))
            {
                model.EncryptedFarmId = v;

            }
            model = BindCropDataForRemove(q, r, s, t, u, w, model);
            ViewBag.EncryptedCropType = q;
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.EncryptedCropGroupName = r;
            }

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in RemoveCrop() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            if (string.IsNullOrWhiteSpace(s) || (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value))))
            {
                TempData["ErrorCreatePlan"] = ex.Message;
                return RedirectToAction(_checkAnswerActionName);
            }
            else
            {
                TempData["NutrientRecommendationsError"] = ex.Message;
                return RedirectToAction("Recommendations", new { q = v, r = t, s = w });
            }
        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCrop(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : RemoveCrop() post action called");
        try
        {

            ValidateRemoveAction(model);

            if (!ModelState.IsValid)
            {
                return View("RemoveCrop", model);
            }

            if (!model.RemoveCrop.Value)
            {
                return await RedirectForRemoveCrop(model, false, null);
            }
            else
            {
                (List<HarvestYearPlanResponse>? harvestYearPlanResponse, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (harvestYearPlanResponse.Any())
                {
                    harvestYearPlanResponse = FilterHarvestYearPlanResponseList(model, harvestYearPlanResponse);
                    if (harvestYearPlanResponse.Any())
                    {
                        List<int> cropIds = harvestYearPlanResponse.Select(x => x.CropID).ToList();
                        (_, Error? error) = await _cropLogic.RemoveCropPlan(cropIds);
                        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["RemoveGroupError"] = error.Message;
                            return View(model);
                        }
                        return await RedirectForRemoveCrop(model, true, harvestYearPlanResponse);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in RemoveCrop() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["RemoveGroupError"] = ex.Message;
        }
        return View(model);
    }
    private static List<HarvestYearPlanResponse> FilterHarvestYearPlanResponseList(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse)
    {
        bool isNotComingFromRecommendation = model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value));

        harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();

        if (!isNotComingFromRecommendation)
        {
            harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.FieldID == model.FieldID &&
            x.CropOrder == model.CropOrder.Value).ToList();
        }
        return harvestYearPlanResponse;
    }
    private void ValidateRemoveAction(PlanViewModel model)
    {
        if (model.RemoveCrop == null)
        {
            ModelState.AddModelError("RemoveCrop", Resource.MsgSelectAnOptionBeforeContinuing);
        }
    }
    private async Task<IActionResult> RedirectForRemoveCrop(PlanViewModel model, bool isRemove, List<HarvestYearPlanResponse>? harvestYearPlanResponse)
    {
        if (!isRemove)
        {
            if (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value)
            {
                return RedirectToAction("Recommendations", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }

            return RedirectToAction(_checkAnswerActionName);

        }
        else
        {
            return await RedirectOnRemoveSucces(model, harvestYearPlanResponse);
        }
    }

    private async Task<IActionResult> RedirectOnRemoveSucces(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse)
    {
        string successMsg = string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName);
        string successMsgForCrop = _cropDataProtector.Protect(successMsg);
        string successMsgForFarm = _farmDataProtector.Protect(successMsg);
        string successMsgContent2 = string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName);
        string successMsgContent2ForFarm = _farmDataProtector.Protect(successMsgContent2);
        string successMsgContent2ForCrop = _cropDataProtector.Protect(successMsgContent2);
        int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
        bool isNotComingFromRecommendation = (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value)));
        (harvestYearPlanResponse, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, decryptedFarmId);
        if (harvestYearPlanResponse.Count > 0)
        {
            return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = isNotComingFromRecommendation ? successMsgForCrop : successMsgContent2ForCrop });
        }
        else
        {
            List<PlanSummaryResponse> planSummaryResponse = await _cropLogic.FetchPlanSummaryByFarmId(decryptedFarmId, 0);
            if (planSummaryResponse != null && planSummaryResponse.Count > 0)
            {
                return RedirectToAction(_plansAndRecordsOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()), q = isNotComingFromRecommendation ? successMsgForCrop : successMsgContent2ForCrop });
            }
            return RedirectToAction(_farmSummaryActionName, "Farm", new { id = model.EncryptedFarmId, q = _farmDataProtector.Protect(Resource.lblTrue), r = isNotComingFromRecommendation ? successMsgForFarm : successMsgContent2ForFarm });

        }
    }

    private PlanViewModel BindCropDataForRemove(string? q, string? r, string? s, string? t, string? u, string? w, PlanViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(w))
        {
            model.EncryptedHarvestYear = w;
            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(w));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            model.CropType = _cropDataProtector.Unprotect(q);

        }
        if (!string.IsNullOrWhiteSpace(r))
        {
            model.CropGroupName = _cropDataProtector.Unprotect(r);
        }
        if (!string.IsNullOrWhiteSpace(s))
        {
            model.FieldName = _cropDataProtector.Unprotect(s);
        }
        if (!string.IsNullOrWhiteSpace(t))
        {
            model.EncryptedFieldId = t;
            model.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(t));
        }
        if (!string.IsNullOrWhiteSpace(u))
        {
            model.IsComingFromRecommendation = true;
            model.CropOrder = Convert.ToInt32(_cropDataProtector.Unprotect(u));
        }
        else
        {
            model.IsComingFromRecommendation = null;
        }
        return model;
    }



    [HttpGet]
    public IActionResult UpdateExcessOrWinterRainfall()
    {
        _logger.LogTrace("Crop Controller : UpdateExcessOrWinterRainfall() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();
        if (model == null)
        {
            _logger.LogTrace("Crop Controller : session not found in UpdateExcessOrWinterRainfall() action");
            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
        }
        try
        {
            return View("UpdateExcessOrWinterRainfall", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in UpdateExcessOrWinterRainfall() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;
            return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
        }


    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateExcessOrWinterRainfall(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : UpdateExcessOrWinterRainfall() post action called");
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExcessWinterRainfall()
    {
        _logger.LogTrace("Crop Controller : ExcessWinterRainfall() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : session not found in ExcessWinterRainfall() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<CommonResponse> excessWinterRainfallOption, Error error) = await _farmLogic.FetchExcessWinterRainfallOptionAsync();
            if (string.IsNullOrWhiteSpace(error?.Message) && excessWinterRainfallOption.Count > 0)
            {
                var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name
                }).ToList();

                ViewBag.ExcessRainFallOptions = SelectListItem;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfall() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["UpdateExcessOrWinterRainfallError"] = ex.Message;
            return RedirectToAction("UpdateExcessOrWinterRainfall");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcessWinterRainfall(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : ExcessWinterRainfall() post action called");
        try
        {
            if (model.ExcessWinterRainfallId == null)
            {
                ModelState.AddModelError("ExcessWinterRainfallId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (List<CommonResponse> excessWinterRainfallOption, Error error) = await _farmLogic.FetchExcessWinterRainfallOptionAsync();
                if (string.IsNullOrWhiteSpace(error?.Message) && excessWinterRainfallOption.Count > 0)
                {
                    var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Name
                    }).ToList();

                    ViewBag.ExcessRainFallOptions = SelectListItem;

                }
                return View(model);
            }
            SetHarvestYearPlanToSession(model);
            return RedirectToAction("ExcessOrWinterRainfallCheckAnswer", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfall() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessWinterRainfallError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult IsWinterRainfallMoreThan450()
    {
        _logger.LogTrace("Crop Controller : IsWinterRainfallMoreThan450() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : session not found in IsWinterRainfallMoreThan450() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in IsWinterRainfallMoreThan450() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["UpdateExcessOrWinterRainfallError"] = ex.Message;
            return RedirectToAction("UpdateExcessOrWinterRainfall");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult IsWinterRainfallMoreThan450(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : IsWinterRainfallMoreThan450() post action called");

        if (model.IsWinterRainfallMoreThan450 == null)
        {
            ModelState.AddModelError("IsWinterRainfallMoreThan450", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        else if (model.IsWinterRainfallMoreThan450.Value)
        {
            model.WinterRainfallName = Resource.lbl450OrMore;
        }
        else
        {
            model.WinterRainfallName = Resource.lblLessThan450;
        }
        try
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetHarvestYearPlanToSession(model);
            return RedirectToAction("ExcessOrWinterRainfallCheckAnswer", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in IsWinterRainfallMoreThan450() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["IsWinterRainfallMoreThan450Error"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExcessOrWinterRainfallCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : ExcessOrWinterRainfallCheckAnswer() action called");
        PlanViewModel? model = GetHarvestYearPlanFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : session not found in ExcessOrWinterRainfallCheckAnswer() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (model.FarmRB209CountryID != (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                (CommonResponse commonResponse, Error error) = await _farmLogic.FetchExcessWinterRainfallOptionByIdAsync(model.ExcessWinterRainfallId.Value);
                if (error?.Message == null && commonResponse != null)
                {
                    model.ExcessWinterRainfallName = commonResponse.Name;
                    model.ExcessWinterRainfallValue = commonResponse.Value;
                }
            }
            model.IsExcessOrWinterRainfallCheckAnswer = true;
            SetHarvestYearPlanToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessOrWinterRainfallCheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessWinterRainfallError"] = ex.Message;
            return RedirectToAction("ExcessWinterRainfall", model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcessOrWinterRainfallCheckAnswer(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : ExcessOrWinterRainfallCheckAnswer() action called");

        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (HttpContext.Session.Keys.Contains("HarvestYearPlan"))
            {
                model = GetHarvestYearPlanFromSession();
            }
            else
            {
                _logger.LogTrace("Crop Controller :HarvestYearPlan session not found in ExcessOrWinterRainfallCheckAnswer() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            string successMsg = string.Format(Resource.MsgAddExcessWinterRainfallContentOne, model.Year.Value);
            if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                successMsg = string.Format(Resource.MsgAddWinterRainfallContentOne, model.Year.Value);
                model.ExcessWinterRainfallValue = (model.IsWinterRainfallMoreThan450.HasValue && model.IsWinterRainfallMoreThan450.Value) ? 500 : 400;
            }
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
            var excessRainfalls = new ExcessRainfalls
            {
                FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)),
                Year = model.Year,
                ExcessRainfall = 0,
                WinterRainfall = model.ExcessWinterRainfallValue,
                CreatedOn = DateTime.Now,
                CreatedByID = userId
            };

            string jsonData = JsonConvert.SerializeObject(excessRainfalls);
            (ExcessRainfalls? excessRainfall, Error? error) = await _farmLogic.AddExcessWinterRainfallAsync(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), model.Year.Value, jsonData, model.IsExcessOrWinterRainfallUpdated.Value);
            if (excessRainfall != null)
            {
                return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(successMsg), v = _cropDataProtector.Protect(string.Format(Resource.MsgAddExcessWinterRainfallContentSecond, model.Year.Value)) });
            }
            else
            {
                TempData["ExcessOrWinterRainfallCheckAnswerError"] = error?.Message;
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessOrWinterRainfallCheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessOrWinterRainfallCheckAnswerError"] = ex.Message;
            return View(model);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCrop(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : UpdateCrop() post action called");
        try
        {
            Error? error = null;

            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
            await ValidateCropData(model);
            if (!ModelState.IsValid)
            {
                model = await BindModelInvalidPropertiesForCheckAnswer(model, true);
                (model, _) = await BindGrassProperties(model);

                ViewData["ModelStateErrors"] = ModelState;
                return View(_checkAnswerActionName, model);
            }


            if (model.Crops != null && model.Crops.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(model.Variety))
                {
                    model.Variety = null;
                }

                List<CropData> cropEntries = await BindCropDataForUpdate(model, userId);
                cropEntries = await BindCropForDelete(model, cropEntries);

                CropDataWrapper cropDataWrapper = new CropDataWrapper
                {
                    Crops = cropEntries
                };
                string jsonData = JsonConvert.SerializeObject(cropDataWrapper);
                (bool success, error) = await _cropLogic.MergeCrop(jsonData);
                if (!success || !string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;
                    return RedirectToAction(_checkAnswerActionName);
                }

                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                RemoveCropSession();

                if (model.IsComingFromRecommendation == true)
                {
                    return RedirectToAction("Recommendations", new
                    {
                        q = model.EncryptedFarmId,
                        r = model.EncryptedFieldId,
                        s = model.EncryptedHarvestYear,
                        t = _cropDataProtector.Protect(Resource.lblCropPlanUpdated)
                    });
                }

                return RedirectToAction(_harvestYearOverviewActionName, new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear,
                    q = Resource.lblTrue,
                    r = _cropDataProtector.Protect(Resource.lblCropPlanUpdated),
                    v = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations)
                });

            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in UpdateCrop() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
        return View(model);
    }
    private async Task<List<CropData>> BindCropForDelete(PlanViewModel model, List<CropData> cropEntries)
    {
        int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

        var (plans, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmId);

        if (plans.Count == 0 || !string.IsNullOrWhiteSpace(model.EncryptedFieldId))
            return cropEntries;

        var filteredPlans = plans
            .Where(x => x.CropGroupName == model.PreviousCropGroupName)
            .ToList();

        //filter fieldIds that we uncheck
        var removableFields = filteredPlans
            .Where(f => !model.FieldList.Contains(f.FieldID.ToString()))
            .ToList();

        foreach (var plan in removableFields)
        {
            var crop = new Crop
            {
                ID = plan.CropID,
                FieldID = plan.FieldID,
                FieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable,
                IsBasePlan = false,
                Year = plan.Year,
                Confirm = false,
                CropOrder = plan.CropOrder,
                IsDeleted = true
            };

            var cropEntry = new CropData
            {
                Crop = crop,
                ManagementPeriods = new List<ManagementPeriod>
            {
                new ManagementPeriod
                {
                    CropID = plan.CropID,
                    Defoliation = 1,
                    Utilisation1ID = 2
                }
            }
            };

            cropEntries.Add(cropEntry);
        }

        return cropEntries;
    }
    private async Task<List<CropData>> BindCropDataForUpdate(PlanViewModel model, int userId)
    {
        List<CropData> cropEntries = new List<CropData>();
        int lastGroupNumber = await BindLastGroupName(model);
        foreach (Crop crop in model.Crops)
        {
            crop.IsBasePlan = false;

            crop.CropGroupName = string.IsNullOrWhiteSpace(model.CropGroupName)
            ? string.Format(Resource.lblCropGroupWithCounter, lastGroupNumber + 1)
            : model.CropGroupName;
            List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
            List<ManagementPeriod> managementPeriodList = new List<ManagementPeriod>();
            if (crop.ID != null)
            {
                (managementPeriodList, _) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
            }
            CropData cropEntry = await BindCropForUpdate(model, userId, crop, managementPeriods, managementPeriodList);
            cropEntries.Add(cropEntry);
        }
        return cropEntries;
    }

    private async Task<CropData> BindCropForUpdate(PlanViewModel model, int userId, Crop crop, List<ManagementPeriod> managementPeriods, List<ManagementPeriod> managementPeriodList)
    {
        if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            managementPeriods.Add(new ManagementPeriod
            {
                Defoliation = 1,
                Utilisation1ID = 2
            });
        }
        else if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            (crop, managementPeriods) = await BindGrassDataForUpdate(model, userId, crop, managementPeriodList);

        }

        crop.FieldName = null;
        crop.EncryptedCounter = null;
        crop.FieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;

        CropData cropEntry = new CropData
        {
            Crop = crop,
            ManagementPeriods = managementPeriods
        };
        return cropEntry;

    }
    private async Task<(Crop, List<ManagementPeriod>)> BindGrassDataForUpdate(PlanViewModel model, int userId, Crop crop, List<ManagementPeriod> managementPeriodList)
    {
        crop.CropTypeID = model.CropTypeID;
        crop.CropInfo1 = model.CropInfo1;
        crop.CropInfo2 = model.CropInfo2;
        crop.DefoliationSequenceID = model.DefoliationSequenceId;
        crop.SwardTypeID = model.SwardTypeId;
        crop.SwardManagementID = model.SwardManagementId;
        crop.PotentialCut = model.PotentialCut;
        crop.Establishment = model.GrassSeason ?? 0;

        string defoliationSequence = "";
        (DefoliationSequenceResponse defoliationSequenceResponse, _) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId.Value);
        if (defoliationSequenceResponse != null)
        {
            defoliationSequence = defoliationSequenceResponse.DefoliationSequence;
        }
        int defoliation = 1;
        int utilisation1 = 0;
        List<ManagementPeriod> managementPeriods = BindManageperiodListForGrass(model, userId, crop, managementPeriodList, defoliationSequence, ref defoliation, ref utilisation1);

        return (crop, managementPeriods);
    }

    private List<ManagementPeriod> BindManageperiodListForGrass(PlanViewModel model, int userId, Crop crop, List<ManagementPeriod> managementPeriodList, string defoliationSequence, ref int defoliation, ref int utilisation1)
    {
        List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
        if (defoliationSequence != null)
        {
            foreach (char c in defoliationSequence)
            {
                utilisation1 = BindUtilisation1(c);
                managementPeriods.Add(new ManagementPeriod
                {
                    Defoliation = defoliation,
                    Utilisation1ID = utilisation1,
                    Yield = crop.Yield ?? 0 / model.PotentialCut
                });
                if (managementPeriodList != null && managementPeriodList.Any() && crop.ID != null)
                {
                    foreach (var managementPeriod in managementPeriods)
                    {
                        managementPeriod.ModifiedOn = DateTime.Now;
                        managementPeriod.ModifiedByID = userId;
                    }
                }
                defoliation++;
            }
        }
        return managementPeriods;
    }

    private List<ManagementPeriod> BindNonGrassDataForUpdate()
    {
        return new List<ManagementPeriod>
    {
        new ManagementPeriod
        {
            Defoliation = 1,
            Utilisation1ID = 2
        }
    };
    }

    private async Task ValidateCropData(PlanViewModel model)
    {
        if (model != null)
        {
            int otherGroupId = (int)NMP.Commons.Enums.CropGroup.Other;
            int cerealsGroupId = (int)NMP.Commons.Enums.CropGroup.Cereals;
            int potatoesGroupId = (int)NMP.Commons.Enums.CropGroup.Potatoes;
            int i = 0;
            ValidateSowingDate(model, i, otherGroupId);
            if (model.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                await ValidateYieldForCheckAsnwer(model, otherGroupId);
            }

            if (model.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", Resource.MsgMainCropTypeNotSet);
            }
            await ValidateCropInfoOne(model, otherGroupId);



            if (model.CropInfo2 == null && model.CropGroupId == cerealsGroupId)
            {
                ModelState.AddModelError("CropInfo2", string.Format(Resource.MsgCropInfo2NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
            }

            if (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                ValidateGrassProperties(model);
            }
        }
    }
    private async Task ValidateCropInfoOne(PlanViewModel model, int otherGroupId)
    {
        string? cropInfoOneQuestion = string.Empty;
        if (model.CropTypeID != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0, model.FarmRB209CountryID ?? 1);
            if (!string.IsNullOrWhiteSpace(cropInfoOneQuestion))
            {
                ViewBag.CropInfoOneQuestion = (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions || model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions) ? string.Format(cropInfoOneQuestion, model.CropType) : cropInfoOneQuestion;
            }
        }
        if (model.CropInfo1 == null && model.CropGroupId != otherGroupId && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            ModelState.AddModelError("CropInfo1", $"{cropInfoOneQuestion} {Resource.lblNotSet.ToLower()}");
        }


    }
    private void ValidateSowingDate(PlanViewModel model, int i, int otherGroupId)
    {

        foreach (var crop in model.Crops)
        {
            if (crop.SowingDate == null)
            {
                if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
                {
                    ModelState.AddModelError(string.Concat(_cropPrefix, i, "].SowingDate"), string.Format(Resource.lblSowingSingleDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                    break;
                }
                else if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
                {
                    ModelState.AddModelError(string.Concat(_cropPrefix, i, "].SowingDate"), string.Format(Resource.lblSowingDiffrentDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                }
            }
            i++;
        }

    }
    private async Task ValidateYieldForCheckAsnwer(PlanViewModel model, int otherGroupId)
    {

        int i = 0;

        decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(
            model.CropTypeID.Value,
            model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);

        foreach (var crop in model.Crops)
        {
            if (crop.Yield == null && defaultYield == 0)
            {
                if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                {
                    ModelState.AddModelError(
                        $"Crops[{i}].Yield",
                        string.Format(Resource.lblWhatIsTheExpectedYieldForSingleNotSet,
                        model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));

                    break;
                }
                else if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                {
                    ValidateDifferentYieldMessage(model, otherGroupId, crop, i);
                }
            }

            i++;
        }

    }
    private void ValidateDifferentYieldMessage(PlanViewModel model, int otherGroupId, Crop crop, int i)
    {
        ModelState.AddModelError(
                       $"Crops[{i}].Yield",
                       string.Format(Resource.lblWhatIsTheDifferentExpectedYieldNotSet,
                       model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType,
                       crop.FieldName));
    }
    private void ValidateGrassProperties(PlanViewModel model)
    {
        if (model.SwardManagementId == null)
        {
            ModelState.AddModelError("SwardManagementId", string.Format("{0} {1}", string.Format(Resource.lblHowWillTheseFieldsBeManaged, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields)), Resource.lblNotSet));
        }
        ValidatePotentialCut(model);

        ValidateSwardTypeDefoliationOrSeason(model);
        if ((model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass))
        {
            ValidateGrassYield(model);
        }
        TempData["ModelStateErrorForGrass"] = true;
    }
    private void ValidatePotentialCut(PlanViewModel model)
    {
        if (model.PotentialCut == null)
        {
            string fieldText = (model.FieldList != null && model.FieldList.Count == 1) ? model.Crops[0]?.FieldName ?? string.Empty : Resource.lblTheseFields;

            string resourceText = model.SwardManagementId switch
            {
                (int)NMP.Commons.Enums.SwardManagement.GrazedOnly => Resource.lblHowManyGrazingsWillYouHaveInTheseFields,

                (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly => Resource.lblHowManyCutsWillYouHaveInTheseFields,

                (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly => Resource.lblHowManyCutsWillYouHaveInTheseFields,

                (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage => Resource.lblHowManyCutsAndGrazingsWillYouHaveInTheseFields,

                _ => Resource.lblHowManyCutsAndGrazingsWillYouHaveInTheseFields
            };

            string message = string.Format(resourceText, fieldText);

            ModelState.AddModelError("PotentialCut", $"{message} {Resource.lblNotSet}");
        }
    }
    private void ValidateGrassYield(PlanViewModel model)
    {
        int i = 0;
        if (model.Crops != null)
        {
            foreach (var crop in model.Crops)
            {
                if (crop.Yield == null)
                {
                    if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                    {
                        ModelState.AddModelError(string.Concat(_cropPrefix, i, _yieldPrefix), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForFields, crop.Year));

                        break;
                    }
                    else if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                    {
                        ModelState.AddModelError(string.Concat(_cropPrefix, i, _yieldPrefix), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForField, crop.FieldName, crop.Year));

                    }
                }
                i++;
            }
        }
    }
    private void ValidateSwardTypeDefoliationOrSeason(PlanViewModel model)
    {
        if (model.SwardTypeId == null)
        {
            ModelState.AddModelError("SwardTypeId", string.Format("{0} {1}", string.Format(Resource.lblWhatIsTheSwardTypeForTheseFields, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields), model.Crops[0].Year), Resource.lblNotSet));
        }
        if (model.GrassSeason == null && model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward)
        {
            ModelState.AddModelError("GrassSeason", string.Format("{0} {1}", string.Format(Resource.lblWhenIsTheSwardInTheseFields, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields), model.Year), Resource.lblNotSet));
        }
        if (model.DefoliationSequenceId == null && (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay))
        {
            ModelState.AddModelError("DefoliationSequenceId", string.Format("{0} {1}", string.Format(Resource.lblHowManyCutsAndGrazingsWillYouHaveInTheseFields, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields)), Resource.lblNotSet));
        }
    }
    private async Task<(PlanViewModel, Error?)> BindGrassProperties(PlanViewModel model)
    {
        Error? error = null;
        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            model.IsCropTypeChange = false;
            model.IsCropGroupChange = false;

            (model, error) = await BindSwardType(model);
            (model, error) = await BindSwardManagement(model);
            (model, error) = await BindDefoliationSequence(model);

            List<GrassSeasonResponse> grassSeasons = await _cropLogic.FetchGrassSeasons();
            grassSeasons.RemoveAll(g => g.SeasonId == 0);
            model.GrassSeasonName = grassSeasons.Where(x => x.SeasonId == model.GrassSeason).Select(x => x.SeasonName).SingleOrDefault();
        }
        return (model, error);
    }

    private async Task<(PlanViewModel, Error?)> BindSwardType(PlanViewModel model)
    {
        Error? error = null;
        (List<SwardTypeResponse> swardTypeResponses, error) = await _cropLogic.FetchSwardTypes();
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            return (model, error);
        }
        else
        {
            if (swardTypeResponses.FirstOrDefault(x => x.SwardTypeId == model.SwardTypeId) != null)
            {
                ViewBag.SwardType = swardTypeResponses.FirstOrDefault(x => x.SwardTypeId == model.SwardTypeId)?.SwardType;
            }
            else
            {
                model.SwardTypeId = null;
            }
        }
        return (model, error);
    }
    private async Task<(PlanViewModel, Error?)> BindSwardManagement(PlanViewModel model)
    {
        Error? error = null;
        if (model.SwardManagementId != null)
        {
            (SwardManagementResponse swardManagementResponse, error) = await _cropLogic.FetchSwardManagementBySwardManagementId(model.SwardManagementId ?? 0);
            if (error != null)
            {
                return (model, error);
            }
            else
            {
                if (swardManagementResponse != null)
                {
                    ViewBag.SwardManagementName = swardManagementResponse.SwardManagement;

                }
                else
                {
                    model.SwardManagementId = null;
                }
            }
        }
        else
        {
            model.SwardManagementId = null;
        }

        return (model, error);
    }
    private async Task<(PlanViewModel, Error?)> BindDefoliationSequence(PlanViewModel model)
    {
        Error? error = null;
        if (model.DefoliationSequenceId != null)
        {
            (DefoliationSequenceResponse defoliationSequenceResponse, error) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId ?? 0);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return (model, error);
            }
            else
            {
                if (defoliationSequenceResponse != null)
                {
                    var defoliations = defoliationSequenceResponse.DefoliationSequenceDescription;
                    string[] arrDefoliations = defoliations.Split(',').Select(s => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.Trim()))
                                                   .ToArray();
                    ViewBag.DefoliationSequenceName = arrDefoliations;
                }
                else
                {
                    model.DefoliationSequenceId = null;
                }
            }
        }
        else
        {
            model.DefoliationSequenceId = null;
        }

        return (model, error);
    }
    [HttpGet]
    public async Task<IActionResult> CurrentSward()
    {
        _logger.LogTrace("Crop Controller : CurrentSward() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                return await Task.FromResult(RedirectToAction("FarmList", "Farm"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CurrentSward() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CurrentSwardError"] = ex.Message;
            return RedirectToAction("CropGroupName");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CurrentSward(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CropInfoTwo() post action called");
        try
        {

            if (model.CurrentSward == null)
            {
                ModelState.AddModelError("CurrentSward", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

                if (planViewModel.CurrentSward == model.CurrentSward && !model.IsAnyChangeInField)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                if (planViewModel.CurrentSward != null && planViewModel.CurrentSward != model.CurrentSward)
                {
                    model.IsCurrentSwardChange = true;
                }
            }

            if (model.IsCurrentSwardChange && (!model.IsCropTypeChange && !model.IsCropGroupChange))
            {
                model = await BindDefoliationSequenceForCurrentSward(model);
            }

            SetCropToSession(model);

            return RedirectForCurrentSward(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CurrentSward() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CurrentSwardError"] = ex.Message;
            return RedirectToAction("CurrentSward");
        }
    }

    private IActionResult RedirectForCurrentSward(PlanViewModel model)
    {
        if (model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward)
        {
            return RedirectToAction("GrassSeason");
        }
        else
        {
            model.GrassSeason = null;
            model.SowingDateQuestion = null;
            model.SowingDate = null;
            SetCropToSession(model);
            if (model.IsCheckAnswer && (!model.IsFieldToBeRemoved.HasValue))
            {
                if (model.SwardTypeId == null || model.Crops.Any(x => x.Yield == null))
                {
                    return RedirectToAction("SwardType");
                }
                return RedirectToAction(_checkAnswerActionName);
            }
            else
            {
                return RedirectToAction("SwardType");
            }
        }
    }

    private async Task<PlanViewModel> BindDefoliationSequenceForCurrentSward(PlanViewModel model)
    {
        (List<DefoliationSequenceResponse> defoliationSequenceResponses, _) = await _cropLogic.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(model.SwardTypeId.Value, model.SwardManagementId ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? model.PotentialCut.Value + 1 : model.PotentialCut ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward);
        if (defoliationSequenceResponses != null && defoliationSequenceResponses.Count > 0)
        {
            model.DefoliationSequenceId = defoliationSequenceResponses[0].DefoliationSequenceId;
            ViewBag.DefoliationSequenceResponses = defoliationSequenceResponses;
            bool isGrazedOnlyOrCutSilage = (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly);
            if (isGrazedOnlyOrCutSilage && model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass && model.IsCheckAnswer)
            {
                model.GrassGrowthClassCounter = 0;
            }
            else if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly && model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass && model.IsCheckAnswer)
            {
                model.GrassGrowthClassCounter = 0;

            }
            else
            {
                model.DefoliationSequenceId = null;
            }
        }

        model.Yield = null;
        model.Crops.ForEach(x => x.Yield = null);
        model.GrassSeason = null;
        SetCropToSession(model);
        return model;
    }

    [HttpGet]
    public async Task<IActionResult> GrassSeason()
    {
        _logger.LogTrace("Crop Controller : GrassSeason() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : GrassSeason() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<GrassSeasonResponse> grassSeasons = await _cropLogic.FetchGrassSeasons();
            grassSeasons.RemoveAll(g => g.SeasonId == 0);
            ViewBag.GrassSeason = grassSeasons.OrderByDescending(x => x.SeasonId);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in GrassSeason() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["GrassSeasonError"] = ex.Message;
            return RedirectToAction("CurrentSward");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrassSeason(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : GrassSeason() post action called");
        try
        {
            if (model.GrassSeason == null)
            {
                ModelState.AddModelError("GrassSeason", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<GrassSeasonResponse> grassSeasons = await _cropLogic.FetchGrassSeasons();
                grassSeasons.RemoveAll(g => g.SeasonId == 0);
                ViewBag.GrassSeason = grassSeasons.OrderByDescending(x => x.SeasonId);
                return View(model);
            }

            PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

            if (model.IsCheckAnswer && planViewModel.GrassSeason == model.GrassSeason && !model.IsAnyChangeInField)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            else if (planViewModel.GrassSeason != model.GrassSeason)
            {
                model.SowingDateQuestion = null;
                model.SowingDate = null;
            }
            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in GrassSeason() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["GrassSeasonError"] = ex.Message;
            return RedirectToAction("GrassSeason");
        }

        return RedirectToAction("SowingDateQuestion");
    }

    public async Task<IActionResult> SwardType()
    {
        _logger.LogTrace("Crop Controller : SwardType() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : SwardType() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<SwardTypeResponse> swardTypeResponses, Error error) = await _cropLogic.FetchSwardTypes();
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["SowingDateError"] = error.Message;
                return RedirectToAction("SowingDate");
            }
            else
            {
                ViewBag.SwardType = swardTypeResponses;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in SwardType() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["SowingDateError"] = ex.Message;
            return RedirectToAction("SowingDate");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwardType(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : SwardType() post action called");
        if (model.SwardTypeId == null)
        {
            ModelState.AddModelError("SwardTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            (List<SwardTypeResponse> swardTypeResponses, Error error) = await _cropLogic.FetchSwardTypes();
            ViewBag.SwardType = swardTypeResponses;
            return View(model);
        }

        PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

        if (model.IsCheckAnswer && planViewModel.SwardTypeId == model.SwardTypeId && !model.IsAnyChangeInField)
        {
            return RedirectToAction(_checkAnswerActionName);
        }
        else if (planViewModel.SwardTypeId != model.SwardTypeId)
        {
            model.SwardManagementId = null;
            model.PotentialCut = null;
            model.DefoliationSequenceId = null;
            model.GrassGrowthClassQuestion = null;
            model.Yield = null;
            model.Crops.ForEach(x => x.Yield = null);
        }

        SetCropToSession(model);
        return RedirectToAction("GrassManagement");
    }

    [HttpGet]
    public async Task<IActionResult> GrassManagement()
    {
        _logger.LogTrace("Crop Controller : GrassManagement() action called");
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : GrassManagement() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<SwardManagementResponse> swardManagementResponses, Error error) = await _cropLogic.FetchSwardManagementBySwardTypeId(model.SwardTypeId ?? 0);
            if (error != null)
            {
                TempData["SwardManagementError"] = error.Message;
                return RedirectToAction("SwardType");
            }
            else
            {
                ViewBag.SwardManagement = swardManagementResponses;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in GrassManagement() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["SwardManagementError"] = ex.Message;
            return RedirectToAction("SwardType");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrassManagement(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : GrassManagement() post action called");
        try
        {
            if (model.SwardManagementId == null)
            {
                ModelState.AddModelError("SwardManagementId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (List<SwardManagementResponse> swardManagementResponses, Error error) = await _cropLogic.FetchSwardManagements();
                ViewBag.SwardManagement = swardManagementResponses;
                return View(model);
            }
            PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

            if (model.IsCheckAnswer && planViewModel.SwardManagementId == model.SwardManagementId && !model.IsAnyChangeInField)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            else if (!model.IsAnyChangeInField)
            {
                model.PotentialCut = null;
                model.DefoliationSequenceId = null;
                model.GrassGrowthClassQuestion = null;
                model.Yield = null;
                model.Crops.ForEach(x => x.Yield = null);
            }

            SetCropToSession(model);
            return RedirectToAction(_defoliationActionName);
        }
        catch (Exception ex)
        {
            TempData["GrassManagementError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Defoliation()
    {
        _logger.LogTrace("Crop Controller : Defoliation() action called");
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : Defoliation() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<PotentialCutResponse> potentialCuts, Error error) = await _cropLogic.FetchPotentialCutsBySwardTypeIdAndSwardManagementId(model.SwardTypeId ?? 0, model.SwardManagementId ?? 0);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["SwardManagementError"] = error.Message;
                return RedirectToAction("SwardType");
            }
            else
            {
                ViewBag.PotentialCuts = potentialCuts;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in Defoliation() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["DefoliationError"] = ex.Message;
            return RedirectToAction("GrassManagement");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Defoliation(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : Defoliation() post action called");
        try
        {
            if (model.PotentialCut == null)
            {
                ModelState.AddModelError("PotentialCut", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                (List<PotentialCutResponse> potentialCuts, Error error) = await _cropLogic.FetchPotentialCutsBySwardTypeIdAndSwardManagementId(model.SwardTypeId ?? 0, model.SwardManagementId ?? 0);
                ViewBag.PotentialCuts = potentialCuts;
                return View(model);
            }

            PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

            if (model.IsCheckAnswer && planViewModel.PotentialCut == model.PotentialCut && !model.IsAnyChangeInField)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            else if (!model.IsAnyChangeInField)
            {
                model.DefoliationSequenceId = null;
                model.GrassGrowthClassQuestion = null;
                model.Yield = null;
                model.Crops.ForEach(x => x.Yield = null);
            }

            SetCropToSession(model);
            return RedirectToAction("DefoliationSequence");
        }
        catch (Exception ex)
        {
            TempData["DefoliationError"] = ex.Message;
            return View(model);
        }
    }




    private IActionResult RedirectGrazedOnlyOrCutForHayOrCurForSilage(PlanViewModel model, List<DefoliationSequenceResponse> defoliationSequenceResponses)
    {
        model.DefoliationSequenceId = defoliationSequenceResponses[0].DefoliationSequenceId;
        SetCropToSession(model);
        if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
        {
            if (model.IsCheckAnswer)
            {
                model.GrassGrowthClassCounter = 0;
                SetCropToSession(model);
            }
            return RedirectToAction(_grassGrowthClassActionName);
        }
        else
        {
            return RedirectToAction(_checkAnswerActionName);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DefoliationSequence()
    {
        _logger.LogTrace("Crop Controller : DefoliationSequence() action called");
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : DefoliationSequence() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<DefoliationSequenceResponse> defoliationSequenceResponses = new List<DefoliationSequenceResponse>();
            (defoliationSequenceResponses, Error error) = await _cropLogic.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(model.SwardTypeId.Value, model.SwardManagementId ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? model.PotentialCut.Value + 1 : model.PotentialCut ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? true : false);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["DefoliationError"] = error.Message;
                return RedirectToAction(_defoliationActionName);
            }
            else
            {
                ViewBag.DefoliationSequenceResponses = defoliationSequenceResponses;
            }

            bool isGrazedOnlyOrCutForHayOrCurForSilage = model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly ||
                model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly;
            //temporary code until api returns correct defoliation sequence for these
            if (isGrazedOnlyOrCutForHayOrCurForSilage)
            {
                return RedirectGrazedOnlyOrCutForHayOrCurForSilage(model, defoliationSequenceResponses);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in DefoliationSequence() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["DefoliationError"] = ex.Message;
            return RedirectToAction(_defoliationActionName);
        }

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DefoliationSequence(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : DefoliationSequence() post action called");
        model.GrassGrowthClassCounter = 0;
        try
        {
            if (model.DefoliationSequenceId == null)
            {
                ModelState.AddModelError("DefoliationSequenceId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (List<DefoliationSequenceResponse> defoliationSequenceResponses, Error error) = await _cropLogic.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(model.SwardTypeId.Value, model.SwardManagementId ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? model.PotentialCut.Value + 1 : model.PotentialCut ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? true : false);
                ViewBag.DefoliationSequenceResponses = defoliationSequenceResponses;
                return View(model);
            }
            PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();

            if (model.IsCheckAnswer && planViewModel.DefoliationSequenceId == model.DefoliationSequenceId && !model.IsAnyChangeInField)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            else if (!model.IsAnyChangeInField)
            {
                model.GrassGrowthClassQuestion = null;
                model.Yield = null;
                model.Crops.ForEach(x => x.Yield = null);
            }
            SetCropToSession(model);
            if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay)
            {
                if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
                {
                    return RedirectToAction(_grassGrowthClassActionName);
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }

            return RedirectToAction(_checkAnswerActionName);
        }
        catch (Exception ex)
        {
            TempData["GrassGrowthClassError"] = ex.Message;
            return View(model);
        }
    }


    private async Task<(bool flowControl, IActionResult? value)> BindCrassGrowthClassData(string? q, PlanViewModel model, List<GrassGrowthClassResponse> grassGrowthClasses)
    {
        if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
        {
            model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
            if (model.GrassGrowthClassCounter == 0)
            {
                model.FieldID = model.Crops[0].FieldID.Value;
                model.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;

                await FetchYieldRangeForEngAndWales(model, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
            }

            SetCropToSession(model);
        }
        else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
        {
            int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
            int index = itemCount - 1;//index of list
            if (itemCount == 0)
            {
                return (flowControl: false, value: RedirectForGrassGrowthClassGet(model));
            }

            model.FieldID = model.Crops[index].FieldID.Value;
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
            model.GrassGrowthClassCounter = index;

            model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
            model.FieldID = model.Crops[model.GrassGrowthClassCounter].FieldID;
            ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
            ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;

            await FetchYieldRangeForEngAndWales(model, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
            SetCropToSession(model);
            if (model.GrassGrowthClassQuestion != null)
            {
                return (flowControl: false, value: RedirectToAction("DefoliationSequence"));
            }
        }
        SetCropToSession(model);
        return (flowControl: true, value: null);
    }

    private IActionResult RedirectForGrassGrowthClassGet(PlanViewModel model)
    {
        model.GrassGrowthClassCounter = 0;
        model.GrassGrowthClassEncryptedCounter = string.Empty;
        SetCropToSession(model);

        //back button logic start
        if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay)
        {
            return RedirectToAction("DefoliationSequence");
        }
        else if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly && (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass))
        {

            return RedirectToAction(_defoliationActionName);

        }

        return RedirectToAction("DefoliationSequence");
    }

    [HttpGet]
    public async Task<IActionResult> GrassGrowthClass(string? q)
    {
        _logger.LogTrace("Crop Controller : GrassGrowthClass() action called");
        Error error = new Error();
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : GrassGrowthClass() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<int> fieldIds = new List<int>();
            List<int> grassGrowthClassIds = new List<int>();

            foreach (var crop in model.Crops)
            {
                fieldIds.Add(crop.FieldID ?? 0);
            }

            (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["GrassGrowthClassError"] = error.Message;
                return RedirectToAction("DefoliationSequence");
            }
            else
            {
                foreach (var grassGrowthClass in grassGrowthClasses)
                {
                    grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
                }
            }

            model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Distinct().Count();
            if (model.GrassGrowthClassDistinctCount > 1)
            {
                model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Count;
            }
            SetCropToSession(model);
            (bool flowControl, IActionResult? value) = await BindCrassGrowthClassData(q, model, grassGrowthClasses);
            if (!flowControl && value != null)
            {
                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in GrassGrowthClass() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["GrassGrowthClassError"] = ex.Message;
            return RedirectToAction("DefoliationSequence");
        }

        SetCropToSession(model);
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrassGrowthClass(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : GrassGrowthClass() post action called");

        List<int> fieldIds = new List<int>();
        Error? error = null;

        PlanViewModel planViewModelBeforeUpdate = GetCropFromSession() ?? new PlanViewModel();
        ValidateGrassGrowthClassProperties(model);
        if (!ModelState.IsValid)
        {
            model = await BindViewBegForGrassGrowthClass(model, fieldIds);
            return View(model);
        }

        if (model.Crops.Count == 1 || model.GrassGrowthClassDistinctCount > 1 && (model.IsCheckAnswer && planViewModelBeforeUpdate.Crops[model.GrassGrowthClassCounter].Yield == model.Crops[model.GrassGrowthClassCounter].Yield && !model.IsAnyChangeInField && !model.IsCurrentSwardChange))
        {
            model.GrassGrowthClassCounter = 1;
            model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
            SetCropToSession(model);
            return RedirectToAction(_checkAnswerActionName);
        }
        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1)
        {
            if (model.IsCheckAnswer && planViewModelBeforeUpdate.GrassGrowthClassQuestion == model.GrassGrowthClassQuestion && !model.IsAnyChangeInField && !model.IsCurrentSwardChange)
            {
                SetCropToSession(model);
                return RedirectToAction(_checkAnswerActionName);
            }
            else if (model.IsAnyChangeInField)
            {
                model.Yield = null;
                model.Crops.ForEach(x => x.Yield = null);
            }
        }

        foreach (var crop in model.Crops)
        {
            fieldIds.Add(crop.FieldID ?? 0);
        }


        (bool flowControl, IActionResult? value, List<int> grassGrowthClassIds, List<GrassGrowthClassResponse> grassGrowthClasses) = await FetchGrassGrowthClasses(fieldIds, error);
        if (!flowControl && value != null)
        {
            return value;
        }


        model = await BindPropertiesForGrassGrowthClass(model, grassGrowthClasses, grassGrowthClassIds);

        SetCropToSession(model);
        return RedirectForGrassGrowthClass(model);
    }

    private IActionResult RedirectForGrassGrowthClass(PlanViewModel model)
    {
        if (model.GrassGrowthClassDistinctCount == 1 && model.Crops.Count > 1)
        {
            return RedirectToAction("DryMatterYield");
        }
        if (model.GrassGrowthClassCounter == model.Crops.Count)
        {
            return RedirectToAction(_checkAnswerActionName);
        }
        else
        {
            if (model.IsCheckAnswer && model.GrassGrowthClassQuestion == null && !model.IsAnyChangeInField && model.GrassGrowthClassCounter == model.Crops.Count)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            if (model.IsCheckAnswer && !model.IsAnyChangeInField && model.GrassGrowthClassCounter < model.Crops.Count)
            {
                if (model.Crops[model.GrassGrowthClassCounter].Yield == null)
                {
                    return View(model);
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            return View(model);
        }
    }

    private async Task<PlanViewModel> BindPropertiesForGrassGrowthClass(PlanViewModel model, List<GrassGrowthClassResponse> grassGrowthClasses, List<int> grassGrowthClassIds)
    {
        model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Distinct().Count();
        if (model.GrassGrowthClassDistinctCount > 1)
        {
            model.GrassGrowthClassDistinctCount = grassGrowthClassIds.Count;
        }

        for (int i = 0; i < model.Crops.Count; i++)
        {
            if (model.FieldID == model.Crops[i].FieldID.Value)
            {
                model.GrassGrowthClassCounter++;
                if (i + 1 < model.Crops.Count)
                {
                    model.FieldID = model.Crops[i + 1].FieldID.Value;
                    model.FieldName = model.Crops[i + 1].FieldName;
                    ViewBag.FieldName = model.Crops[i + 1].FieldName;
                    ViewBag.GrassGrowthClass = grassGrowthClasses[i + 1].GrassGrowthClassName;

                    await FetchYieldRangeForEngAndWales(model, grassGrowthClasses[i + 1].GrassGrowthClassId);
                }
                break;
            }
        }
        model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());

        PlanViewModel planViewModel = GetCropFromSession() ?? new PlanViewModel();
        if (model.GrassGrowthClassQuestion != planViewModel.GrassGrowthClassQuestion)
        {
            model.DryMatterYieldCounter = 0;
            model.DryMatterYieldEncryptedCounter = _cropDataProtector.Protect(model.DryMatterYieldCounter.ToString());
            model.Crops.ForEach(x => x.Yield = null);
        }
        return model;
    }

    private async Task<(bool flowControl, IActionResult? value, List<int>, List<GrassGrowthClassResponse>)> FetchGrassGrowthClasses(List<int> fieldIds, Error? error)
    {
        (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
        List<int> grassGrowthClassIds = new List<int>();
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["GrassGrowthClassError"] = error.Message;
            return (flowControl: false, value: RedirectToAction("DefoliationSequence"), grassGrowthClassIds, grassGrowthClasses);
        }
        else
        {
            foreach (var grassGrowthClass in grassGrowthClasses)
            {
                grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
            }
        }

        return (flowControl: true, value: null, grassGrowthClassIds, grassGrowthClasses);
    }

    private async Task<PlanViewModel> BindViewBegForGrassGrowthClass(PlanViewModel model, List<int> fieldIds)
    {
        foreach (var crop in model.Crops)
        {
            fieldIds.Add(crop.FieldID ?? 0);
        }

        (List<GrassGrowthClassResponse> grassGrowthClasses, _) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
        model.FieldID = model.Crops[0].FieldID.Value;
        model.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
        ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
        if (grassGrowthClasses != null)
        {
            ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;
            await FetchYieldRangeForEngAndWales(model, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
        }
        return model;
    }

    private void ValidateGrassGrowthClassProperties(PlanViewModel model)
    {
        if ((model.Crops.Count == 1 || model.GrassGrowthClassDistinctCount > 1) && model.Crops[model.GrassGrowthClassCounter].Yield == null)
        {

            ModelState.AddModelError(_cropPrefix + model.GrassGrowthClassCounter + _yieldPrefix, Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1 && model.GrassGrowthClassQuestion == null)
        {

            ModelState.AddModelError("GrassGrowthClassQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
        }
    }

    private async Task FetchYieldRangeForEngAndWales(PlanViewModel model, int grassGrowthClassesId)
    {
        (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, _) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClassesId);
        if (yieldRangesEnglandAndWalesResponses != null && yieldRangesEnglandAndWalesResponses.Count > 0)
        {
            ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
            ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
            ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
        }


    }


    [HttpGet]
    public async Task<IActionResult> DryMatterYield(string q)
    {
        _logger.LogTrace("Crop Controller : DryMatterYield({Q}) action called", q);
        PlanViewModel model = GetCropFromSession();
        List<int> fieldIds = new List<int>();
        Error? error = null;

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : DryMatterYield() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            foreach (var crop in model.Crops)
            {
                fieldIds.Add(crop.FieldID ?? 0);
            }

            (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["GrassGrowthClassError"] = error.Message;
                return RedirectToAction("DefoliationSequence");
            }

            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                if (model.DryMatterYieldCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                }

                await BindYieldRange(model, grassGrowthClasses[0].GrassGrowthClassId);
                SetCropToSession(model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;//index of list
                if (itemCount == 0)
                {
                    model.DryMatterYieldCounter = 0;
                    model.DryMatterYieldEncryptedCounter = string.Empty;
                    SetCropToSession(model);
                    return RedirectToAction(_grassGrowthClassActionName);
                }
                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.DryMatterYieldCounter = index;
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());

                SetCropToSession(model);
                await FetchYieldRangeForEngAndWales(model, grassGrowthClasses[index].GrassGrowthClassId);
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in DryMatterYield() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["DryMatterYieldError"] = ex.Message;
            return RedirectToAction(_grassGrowthClassActionName);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DryMatterYield(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : DryMatterYield() post action called");

        Error? error = null;
        List<int> fieldIds = await ValidateDryMatterYield(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["DryMatterYieldError"] = error.Message;
            return View(model);
        }

        model.GrassGrowthClassCounter = 0;
        if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
        {
            (bool flowControl, IActionResult? value) = await RedirectForDryMatterIfDifferentFigure(model, grassGrowthClasses);
            if (!flowControl && value != null)
            {
                return value;
            }
        }
        else if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
        {
            return RedirectForDryMatterIfSingleFigure(model);
        }

        if (model.DryMatterYieldCounter == model.Crops.Count ||
            (model.IsCheckAnswer && model.Crops.Where((crop, index) => index != model.DryMatterYieldCounter - 1).All(crop => crop != null && crop.Yield != null)))
        {
            SetCropToSession(model);
            return RedirectToAction(_checkAnswerActionName);
        }
        return View(model);
    }

    private IActionResult RedirectForDryMatterIfSingleFigure(PlanViewModel model)
    {
        model.DryMatterYieldCounter = 1;
        for (int i = 0; i < model.Crops.Count; i++)
        {
            model.Crops[i].Yield = model.Crops[0].Yield;
        }

        model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
        SetCropToSession(model);

        if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
        {
            SetCropToSession(model);
            return RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction(_checkAnswerActionName);
    }

    private async Task<(bool flowControl, IActionResult? value)> RedirectForDryMatterIfDifferentFigure(PlanViewModel model, List<GrassGrowthClassResponse> grassGrowthClasses)
    {
        for (int i = 0; i < model.Crops.Count; i++)
        {
            if (model.FieldID == model.Crops[i].FieldID.Value)
            {
                model.DryMatterYieldCounter++;
                if (i + 1 < model.Crops.Count)
                {
                    model.FieldID = model.Crops[i + 1].FieldID.Value;
                    await BindYieldRange(model, grassGrowthClasses[i + 1].GrassGrowthClassId);

                }

                break;
            }
        }

        model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
        SetCropToSession(model);
        if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange) && (!model.IsCurrentSwardChange))
        {
            SetCropToSession(model);
            return (flowControl: false, value: RedirectToAction(_checkAnswerActionName));
        }

        return (flowControl: true, value: null);
    }

    private async Task BindYieldRange(PlanViewModel model, int grassGrowthClassId)
    {
        (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, _) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClassId);

        if (yieldRangesEnglandAndWalesResponses != null)
        {
            ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
        }

    }

    private async Task<List<int>> ValidateDryMatterYield(PlanViewModel model)
    {
        List<int> fieldIds = new List<int>();
        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1)
        {
            if (model.Crops[model.DryMatterYieldCounter].Yield == null)
            {
                ModelState.AddModelError(_cropPrefix + model.DryMatterYieldCounter + _yieldPrefix, Resource.MsgSelectAnOptionBeforeContinuing);
            }
            foreach (var crop in model.Crops)
            {
                fieldIds.Add(crop.FieldID ?? 0);
            }

            (List<GrassGrowthClassResponse> grassGrowthClasses, _) = await _cropLogic.FetchGrassGrowthClass(fieldIds);


            await BindYieldRange(model, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
        }
        return fieldIds;
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        _logger.LogTrace("Crop Controller : Cancel() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : Cancel() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : Cancel() post action called");
        if (model.IsCancel == null)
        {
            ModelState.AddModelError("IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            return View("Cancel", model);
        }
        if (!model.IsCancel.Value)
        {
            return RedirectToAction(_checkAnswerActionName);
        }
        else
        {
            RemoveCropSession();
            return await RedirectForCancel(model);
        }
    }
    private IActionResult RedirectForCancelNotComingFromRec(PlanViewModel model, List<HarvestYearPlanResponse> harvestYearPlanResponse)
    {
        if (harvestYearPlanResponse.Count > 0)
        {
            return RedirectToAction(_harvestYearOverviewActionName, new
            {
                id = model.EncryptedFarmId,
                year = model.EncryptedHarvestYear
            });
        }
        else
        {
            return RedirectToAction("FarmList", "Farm");
        }

    }

    private async Task<IActionResult> RedirectForCancel(PlanViewModel model)
    {
        model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
        (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
        if (!string.IsNullOrWhiteSpace(error?.Message))
        {
            TempData["CancelPageError"] = error.Message;
            return View("Cancel", model);
        }
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && (model.IsComingFromRecommendation == null))
        {
            return RedirectForCancelNotComingFromRec(model, harvestYearPlanResponse);
        }

        if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value))
        {
            return RedirectToAction("Recommendations", new
            {
                q = model.EncryptedFarmId,
                r = model.EncryptedFieldId,
                s = model.EncryptedHarvestYear
            });
        }

        return RedirectForCancelNotComingFromRec(model, harvestYearPlanResponse);
    }
    private async Task<PlanViewModel> BindCropInfo1AndCropInfo2(PlanViewModel model)
    {
        if (model.CropTypeID != null && model.CropInfo1 != null)
        {
            model.CropInfo1Name = await _cropLogic.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(model.CropTypeID.Value, model.CropInfo1.Value);
        }

        if (model.CropInfo2 != null)
        {
            model.CropInfo2Name = await _cropLogic.FetchCropInfo2NameByCropInfo2Id(model.CropInfo2.Value);
        }

        ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType.ToString());
        if (!string.IsNullOrWhiteSpace(model.CropGroupName))
        {
            ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
        }
        return model;
    }
    [HttpGet]
    public async Task<IActionResult> CheckYourPlanData(string? year, string? q)
    {
        _logger.LogTrace("Crop Controller : CheckYourPlanData() action called");
        PlanViewModel model = new PlanViewModel();
        int farmId = 0;
        FarmResponse? farm = new FarmResponse();
        Error? error = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);
                model.EncryptedFarmId = q;
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(year));
            }

            model.FarmName = farm?.Name;

            (model, List<PlanSummaryResponse> planSummaryResponse) = await FetchPlanAndCropYourPlanData(year, model, farmId, true);
            SetCropToSession(model);
            (int? topPrevCroppingYear, error) = await _previousCroppingLogic.FetchPreviousCroppingYearByFarmdId(farmId);

            if (string.IsNullOrWhiteSpace(error?.Message) && topPrevCroppingYear > 0)
            {
                return await RedirectForCheckYourData(q, model, error, planSummaryResponse, topPrevCroppingYear);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CheckYourPlanData() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CheckYourPlanDataError"] = ex.Message;
            return RedirectToAction(_plansAndRecordsOverviewActionName, "Crop", new { id = q });
        }

        return View(model);
    }

    private async Task<IActionResult> RedirectForCheckYourData(string? q, PlanViewModel model, Error error, List<PlanSummaryResponse> planSummaryResponse, int? topPrevCroppingYear)
    {
        DateTime currentDate = DateTime.Now;
        DateTime harvestYearEndDate = new DateTime(currentDate.Year, 7, 31, 0, 0, 0, DateTimeKind.Local);
        int currentHarvestYear = currentDate > harvestYearEndDate ? currentDate.Year + 1 : currentDate.Year;
        List<int> yearList = new List<int>();
        if (planSummaryResponse != null && planSummaryResponse.Count > 0)
        {
            model = FetchHarvestYearList(model, planSummaryResponse, topPrevCroppingYear, currentHarvestYear, yearList, false);

            if (model.HarvestYear?.Count > 0)
            {
                model.HarvestYear = model.HarvestYear.OrderByDescending(x => x.Year).ToList();
            }

            SetCropToSession(model);
            bool isPreviousYearPlanExist = false;

            isPreviousYearPlanExist = model.HarvestYear.Any(x => x.Year < model.Year && x.IsAnyPlan);

            if (isPreviousYearPlanExist)
            {
                //to remove base year from HarvestYear list
                model = await FilterHarvestYearList(model, error);
                SetCropToSession(model);
                if (model.HarvestYear != null && model.HarvestYear.All(x => !x.IsAnyPlan))
                {
                    return RedirectToAction(_harvestYearForPlanActionName, new { q = q, year = _farmDataProtector.Protect(model.Year.ToString()), isPlanRecord = false });
                }

                return View(model);
            }
            else
            {
                return RedirectToAction(_harvestYearForPlanActionName, new { q = q, year = _farmDataProtector.Protect(model.Year.ToString()), isPlanRecord = true });
            }
        }
        else
        {
            return RedirectToAction(_harvestYearForPlanActionName, new { q = q, year = _farmDataProtector.Protect(model.Year.ToString()), isPlanRecord = true });
        }
    }

    private async Task<PlanViewModel> FilterHarvestYearList(PlanViewModel model, Error error)
    {
        foreach (var yr in model.HarvestYear)
        {
            (List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(yr.Year, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (harvestYearPlanResponseForFilter.Count > 0)
            {
                var baseYearCrops = harvestYearPlanResponseForFilter.All(x => (x.IsBasePlan != null && (x.IsBasePlan.Value)));
                if (baseYearCrops)
                {
                    model.HarvestYear = model.HarvestYear.Where(x => x.Year != yr.Year).ToList();
                    SetCropToSession(model);
                }
            }
        }

        return model;
    }

    [HttpGet]
    public async Task<IActionResult> CopyExistingPlan(string q)
    {
        _logger.LogTrace("Crop Controller : CopyExistingPlan() action called");
        PlanViewModel model = GetCropFromSession() ?? new PlanViewModel();

        if (string.IsNullOrWhiteSpace(q))
        {
            _logger.LogTrace("Crop Controller : CopyExistingPlan() action called - Query string is null or empty");
            return await Task.FromResult(Functions.RedirectToErrorHandler((int)HttpStatusCode.BadRequest));
        }

        if (!string.IsNullOrEmpty(q))
        {
            model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            model.EncryptedFarmId = q;
        }

        SetCropToSession(model);
        return await Task.FromResult(View(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyExistingPlan(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CopyExistingPlan() post action called");

        if (model.CopyExistingPlan == null)
        {
            ModelState.AddModelError("CopyExistingPlan", Resource.MsgSelectAnOptionBeforeContinuing);
        }

        if (!ModelState.IsValid)
        {
            return await Task.FromResult(View(model));
        }

        SetCropToSession(model);

        if (model.IsCheckAnswer)
        {
            return await Task.FromResult(RedirectToAction(_checkAnswerActionName));
        }

        if (model.CopyExistingPlan != null && !(model.CopyExistingPlan.Value))
        {
            return await Task.FromResult(RedirectToAction(_cropGroupsActionName));
        }

        return await Task.FromResult(RedirectToAction("CopyPlanYears"));
    }

    [HttpGet]
    public async Task<IActionResult> CopyPlanYears()
    {
        _logger.LogTrace("Crop Controller : CopyPlanYears() action called");
        PlanViewModel? model = GetCropFromSession();
        Error? error = null;

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : CopyPlanYears() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            foreach (var year in model.HarvestYear)
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(year.Year, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (harvestYearPlanResponseForFilter.Count > 0)
                {
                    var baseYearCrops = harvestYearPlanResponseForFilter.All(x => (x.IsBasePlan != null && (x.IsBasePlan.Value)));
                    if (baseYearCrops)
                    {
                        model.HarvestYear = model.HarvestYear.Where(x => x.Year != year.Year).ToList();
                        SetCropToSession(model);
                    }
                }
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CopyPlanYears() action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_tempDataErrorKey] = string.Concat(error == null ? "" : error.Message, ex.Message);
            return RedirectToAction(_farmSummaryActionName, "Farm");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CopyPlanYears(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CopyPlanYears() action posted");

        if (!model.CopyYear.HasValue)
        {
            ModelState.AddModelError("CopyYear", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblYear.ToLower()));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        SetCropToSession(model);
        if (model.IsCheckAnswer)
        {
            for (int i = 0; i < model.Crops?.Count; i++)
            {
                model.Crops[i].Year = model.Year.Value;
            }

            SetCropToSession(model);
            return RedirectToAction(_checkAnswerActionName);
        }

        return RedirectToAction("CopyOrganicInorganicApplications");
    }

    [HttpGet]
    public async Task<IActionResult> CopyOrganicInorganicApplications()
    {
        _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action called");
        PlanViewModel? model = GetCropFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CopyOrganicInorganicApplications() action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_tempDataErrorKey] = ex.Message;
            return RedirectToAction(_farmSummaryActionName, "Farm");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CopyOrganicInorganicApplications(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action posted");
        if (model.OrganicInorganicCopy == null)
        {
            ModelState.AddModelError("OrganicInorganicCopy", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblYear.ToLower()));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        SetCropToSession(model);
        return RedirectToAction(_copyCheckAnswerActionName);
    }

    [HttpGet]
    public async Task<IActionResult> CopyCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action called - CropData session is null");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            model.IsCheckAnswer = true;
            SetCropToSession(model);
            return await Task.FromResult(View(model));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CopyOrganicInorganicApplications() action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
            TempData[_tempDataErrorKey] = ex.Message;
            return await Task.FromResult(RedirectToAction(_farmSummaryActionName, "Farm"));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyCheckAnswer(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CopyCheckAnswer() post action called");

        if (model != null)
        {
            if (model.CopyExistingPlan == null)
            {
                ModelState.AddModelError("CopyExistingPlan", Resource.lblWouldYouLikeToStartWithCopyOfPlanFromPreviousYearNotSet);
            }

            if (model.CopyYear == null)
            {
                ModelState.AddModelError("CopyYear", string.Format(Resource.lblWhichPlanWouldYouLikeToCopyForNotSet, model.Year));
            }

            if (model.OrganicInorganicCopy == null)
            {
                ModelState.AddModelError("OrganicInorganicCopy", Resource.lblDoYouWantToIncludeOrganicMaterialInorganicFertiliserApplicationsNotSet);
            }


            if (!ModelState.IsValid)
            {
                return View(_copyCheckAnswerActionName, model);
            }

            Error? error = null;
            bool isOrganic = false;
            bool isFertiliser = false;
            isOrganic = (model.OrganicInorganicCopy & OrganicInorganicCopyOptions.OrganicMaterial) != 0;
            isFertiliser = (model.OrganicInorganicCopy & OrganicInorganicCopyOptions.InorganicFertiliser) != 0;
            (bool success, error) = await _cropLogic.CopyCropNutrientManagementPlan(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), model.Year ?? 0, model.CopyYear ?? 0, isOrganic, isFertiliser);

            if (string.IsNullOrWhiteSpace(error?.Message) && success)
            {
                return BackActionForCopyCheckAnswer(model, success);
            }
            else
            {
                TempData["ErrorCopyPlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;
                return RedirectToAction(_copyCheckAnswerActionName);
            }
        }
        return View(_copyCheckAnswerActionName, model);
    }

    public async Task<IActionResult> BackCopyCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : BackCopyCheckAnswer() action called");
        PlanViewModel? model = GetCropFromSession();
        if (model == null)
        {
            _logger.LogTrace("Crop Controller : BackCopyCheckAnswer() action called - CropData session is null");
            return await Task.FromResult(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
        }

        model.IsCheckAnswer = false;
        SetCropToSession(model);
        return await Task.FromResult(RedirectToAction("CopyOrganicInorganicApplications"));
    }



    [HttpGet]
    public async Task<IActionResult> AddOrRemoveField()
    {
        _logger.LogTrace("Crop Controller : AddOrRemoveField() get action called");
        PlanViewModel? model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : Session not found in AddOrRemoveField() get action");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            (ViewBag.RemovableFields, ViewBag.NewlyAddedFields) = await SeparateFieldsAsync(model);
        }
        catch (Exception ex)
        {
            TempData["ErrorOnSelectField"] = ex.Message;
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOrRemoveField(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : AddOrRemoveField() post action called");

        if (model.IsFieldToBeRemoved == null)
        {
            ModelState.AddModelError("IsFieldToBeRemoved", Resource.MsgSelectAnOptionBeforeContinuing);
        }

        if (!ModelState.IsValid)
        {
            (ViewBag.RemovableFields, ViewBag.NewlyAddedFields) = await SeparateFieldsAsync(model);
        }

        SetCropToSession(model);

        if (model.IsFieldToBeRemoved.HasValue && model.IsFieldToBeRemoved.Value)
        {
            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                return RedirectToAction("CurrentSward");
            }
            return RedirectToAction("SowingDateQuestion");
        }
        else
        {
            return RedirectToAction("CropFields");
        }
    }

    private async Task<(List<HarvestYearPlanResponse>?, List<Field>?)> SeparateFieldsAsync(PlanViewModel model)
    {
        List<HarvestYearPlanResponse>? fieldsToBeRemoved = null;
        List<Field>? newFields = null;
        Error? error = null;
        int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

        //fetch all fields
        List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
        {
            //fetch plan of this year
            (List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmID);
            if (string.IsNullOrWhiteSpace(error?.Message) && harvestYearPlanResponseForFilter.Count > 0)
            {
                //filter the plan list based on the crop group
                harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();

                //Get the fields that we unchecked.
                var removableFields = harvestYearPlanResponseForFilter.Where(f => !model.FieldList.Contains(f.FieldID.ToString())).ToList();
                fieldsToBeRemoved = removableFields.Count > 0 ? removableFields : null;

                //Remove the fields that already have a plan from the field list.
                var fieldIdsToRemove = harvestYearPlanResponseForFilter
                    .Select(x => x.FieldID)
                    .ToList();
                var newlyAddedFields = fieldList.Where(field => !fieldIdsToRemove.Contains(field.ID.Value)).ToList();
                newlyAddedFields = newlyAddedFields.Where(x => model.FieldList.Contains(x.ID.Value.ToString())).ToList();
                newFields = newlyAddedFields.Count > 0 ? newlyAddedFields : null;

            }
        }

        return (fieldsToBeRemoved, newFields);
    }

    private PlanViewModel? GetHarvestYearPlanFromSession()
    {
        if (HttpContext.Session.Exists("HarvestYearPlan"))
        {
            return HttpContext.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
        }
        return null;
    }

    private void SetHarvestYearPlanToSession(PlanViewModel plan)
    {
        HttpContext.Session.SetObjectAsJson("HarvestYearPlan", plan);
    }

    private async Task<List<Field>> GetFilteredFields(PlanViewModel model, int farmId, bool isCropUpdate)
    {
        var fields = await _fieldLogic.FetchFieldsByFarmId(farmId);

        if (!isCropUpdate)
            return fields;

        var (plans, error) =
            await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmId);

        if ((error != null && !string.IsNullOrWhiteSpace(error.Message)) || plans.Count == 0)
            return fields;

        var fieldIds = plans
            .Where(x => x.CropGroupName == model.PreviousCropGroupName)
            .Select(x => x.FieldID)
            .ToList();

        return fields.Where(f => fieldIds.Contains(f.ID.Value)).ToList();
    }

    private static List<SelectListItem> MapToSelectList(List<Field> fields)
    {
        return fields.Select(f => new SelectListItem
        {
            Value = f.ID.ToString(),
            Text = f.Name
        }).ToList();
    }

    private async Task ApplySecondCropRules(PlanViewModel model, int farmId, bool isCropUpdate, List<SelectListItem> selectList)
    {
        Error error;
        var (plans, _) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmId);

        var firstCropPlans = plans
            .Where(x => x.IsBasePlan != null && !x.IsBasePlan.Value)
            .ToList();

        var (allowedFields, removeFields) =
            await FetchAllowedFieldsForSecondCrop(
                firstCropPlans,
                model.Year ?? 0,
                model.CropTypeID ?? 0, model,
                isCropUpdate,
                model.Crops, model.FarmRB209CountryID ?? 3);

        if (isCropUpdate)
        {
            RemoveFields(selectList, removeFields);
            return;
        }

        ApplyHarvestFieldFiltering(selectList, plans, allowedFields);
    }

    private static void RemoveFields(
    List<SelectListItem> selectList,
    List<int> removeFieldIds)
    {
        foreach (var id in removeFieldIds)
        {
            selectList.RemoveAll(x => x.Value == id.ToString());
        }
    }

    private static void ApplyHarvestFieldFiltering(List<SelectListItem> selectList, List<HarvestYearPlanResponse> plans, List<int> allowedFields)
    {
        if (!plans.Any() && selectList.Count != 1)
            return;

        var harvestedFieldIds = plans
            .Select(x => x.FieldID.ToString())
            .ToList();

        selectList.RemoveAll(x =>
            harvestedFieldIds.Contains(x.Value) &&
            !allowedFields.Contains(int.Parse(x.Value)));
    }
}
