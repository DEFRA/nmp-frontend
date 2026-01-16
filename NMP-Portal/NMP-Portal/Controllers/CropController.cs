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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Error = NMP.Commons.ServiceResponses.Error;
namespace NMP.Portal.Controllers;

[Authorize]
public class CropController(ILogger<CropController> logger, IDataProtectionProvider dataProtectionProvider,
     IFarmLogic farmLogic, IFieldLogic fieldLogic, ICropLogic cropLogic, IOrganicManureLogic organicManureLogic,
     IPreviousCroppingLogic previousCroppingLogic) : Controller
{
    private readonly ILogger<CropController> _logger = logger;
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
    private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
    private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
    private readonly IFarmLogic _farmLogic = farmLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
    private readonly IPreviousCroppingLogic _previousCroppingLogic = previousCroppingLogic;
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

        if (model.Year == null)
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
            for (int i = 0; i < model?.Crops?.Count; i++)
            {
                model.Crops[i].Year = model.Year.Value;
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
                    year = _farmDataProtector.Protect(model.Year.ToString())
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
        model.FarmRB209CountryID = farm.RB209CountryID;
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

            (FarmResponse farm, _) = await _farmLogic.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            model.FarmRB209CountryID = farm.RB209CountryID;
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

            PlanViewModel CropData = GetCropFromSession();

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

        if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
        {
            model.CropType = Resource.lblGrass;
            model.CropTypeID = await _cropLogic.FetchCropTypeByGroupId(model.CropGroupId ?? 0);

            //Fetch fields allowed for second crop based on first crop
            if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
                var SelectListItem = fieldList.Select(f => new SelectListItem
                {
                    Value = f.ID.ToString(),
                    Text = f.Name
                }).ToList();


                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
                List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                    .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))).ToList();

                (List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
                PlanViewModel? cropData = GetCropFromSession();
                if (cropData != null && cropData.CropTypeID != model.CropTypeID)
                {
                    model.IsCropTypeChange = true;
                }
                if (harvestYearPlanResponse.Count() > 0 || SelectListItem.Count == 1)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                    if (!model.IsCheckAnswer)
                    {
                        if (model.Crops != null && model.Crops.Count > 0)
                        {
                            foreach (var crop in model.Crops)
                            {
                                if (crop.FieldID != null)
                                {
                                    crop.CropOrder = fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) ? 2 : 1;
                                }
                            }
                        }
                    }
                }

                if (model.CropTypeID != null)
                {
                    if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
                    {
                        model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
                    }

                    SetCropToSession(model);
                    if (harvestYearPlanResponse.Count > 0)
                    {
                        var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        SelectListItem = SelectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                        if (SelectListItem.Count == 0)
                        {
                            TempData["CropGroupError"] = Resource.lblNoFieldsAreAvailable;
                            ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                            return RedirectToAction(_cropGroupsActionName);
                        }
                    }
                }
            }

            SetCropToSession(model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == true)
            {
                return RedirectToAction("CropGroupName");
            }
            return RedirectToAction("CropFields");
        }

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

        return RedirectToAction("CropTypes");
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
            if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other)
            {
                List<CropTypeResponse> cropTypeList = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryID);
                ViewBag.CropTypeList = cropTypeList.OrderBy(c => c.CropType);
            }
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
            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other)
            {
                model.CropTypeID = await _cropLogic.FetchCropTypeByGroupId(model.CropGroupId ?? 0);
            }
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
            if (!ModelState.IsValid)
            {
                ViewBag.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other)
                {
                    List<CropTypeResponse> cropTypes = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryID);
                    ViewBag.CropTypeList = cropTypes;
                }
                return View(model);
            }

            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<Field> fieldList = new List<Field>(allFields);
            Error error = null;
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponseForFilter.Count > 0)
                {
                    harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponseForFilter != null)
                    {
                        var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID).ToList();
                        fieldList = fieldList.Where(x => fieldIds.Contains(x.ID.Value)).ToList();
                    }
                }
            }
            var selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();
            PlanViewModel? cropData = GetCropFromSession();

            List<int> fieldsAllowedForSecondCrop = new List<int>();
            List<int> fieldRemoveList = new List<int>();
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

            var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                .Where(x => x.IsBasePlan != null && (!x.IsBasePlan.Value)).ToList();

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> allFieldList = new List<Field>(allFields);
                if (allFieldList.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    var fieldIdsToRemove = harvestYearPlanResponse
                        .Select(x => x.FieldID)
                        .ToList();

                    allFieldList.RemoveAll(field => fieldIdsToRemove.Contains(field.ID.Value));
                    selectListItem.AddRange(allFieldList.Select(x => new SelectListItem
                    {
                        Value = x.ID.Value.ToString(),
                        Text = x.Name.ToString()
                    }));
                }
                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
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
                            continue;
                        }
                    }
                }
            }
            else
            {
                //Fetch fields allowed for second crop based on first crop
                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);

                if (harvestYearPlanResponse.Count() > 0 || selectListItem.Count == 1)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                }
            }

            if (cropData != null && cropData.CropTypeID != model.CropTypeID)
            {
                model.IsCropTypeChange = true;
            }
            if (harvestYearPlanResponse.Count() > 0 || selectListItem.Count == 1)
            {
                if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    if (!model.IsCheckAnswer)
                    {
                        if (model.Crops != null && model.Crops.Count > 0)
                        {
                            foreach (var crop in model.Crops)
                            {
                                if (crop.FieldID != null)
                                {
                                    crop.CropOrder = fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) ? 2 : 1;
                                }
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                int harvestYearPlanCount = 0;
                int cropPlanCounter = 0;
                if (harvestYearPlanResponse.Count() > 0)
                {
                    harvestYearPlanCount = harvestYearPlanResponse.Count();
                    foreach (var harvestYearPlan in harvestYearPlanResponse)
                    {
                        if (harvestYearPlan.IsBasePlan.Value)
                        {
                            cropPlanCounter++;
                        }
                    }
                }
                if (harvestYearPlanCount > 0 && cropPlanCounter > 0 && harvestYearPlanCount == cropPlanCounter)
                {
                    TempData["CropTypeError"] = Resource.MsgIfUserCreateSecondCropInBasicPlan;
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                    return RedirectToAction("CropTypes");
                }
            }
            if (model.CropTypeID != null)
            {
                model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
                SetCropToSession(model);
                if (harvestYearPlanResponse.Count() > 0)
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
                        TempData["CropTypeError"] = Resource.lblNoFieldsAreAvailable;
                        ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                        return RedirectToAction("CropTypes");
                    }
                }
            }

            if (model.IsCheckAnswer)
            {
                if (cropData != null && cropData.CropTypeID == model.CropTypeID)
                {
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    if (model.Crops != null && model.Crops.Count > 0)
                    {
                        var cropsToRemove = model.Crops
                        .Where(crop =>
                        (fieldsAllowedForSecondCrop.Count > 0 &&
                            !fieldsAllowedForSecondCrop.Contains(crop.FieldID.Value) &&
                            crop.CropOrder == 2) ||
                        (fieldsAllowedForSecondCrop.Count == 0 &&
                            crop.CropOrder == 2))
                        .ToList();

                        if (model.FieldList != null && model.FieldList.Count > 0)
                        {
                            foreach (var crop in cropsToRemove)
                            {
                                model.FieldList.Remove(crop.FieldID.ToString());
                                model.CropGroupName = string.Empty;
                            }
                        }

                        model.Crops.RemoveAll(crop => cropsToRemove.Contains(crop));
                    }

                    model.CropInfo1 = null;
                    model.CropInfo2 = null;
                    model.CropInfo1Name = null;
                    model.CropInfo2Name = null;
                    model.CropType = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);

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

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crop Controller: Exception in CropTypes() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CropTypeError"] = ex.Message;
            return View(model);
        }
        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == true)
        {
            return RedirectToAction("CropGroupName");
        }
        return RedirectToAction("CropFields");
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
            return RedirectToAction("SowingDateQuestion");
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in VarietyName() post action : {0}, {1}", ex.Message, ex.StackTrace);
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

            Error error = null;
            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<Field> fieldList = new List<Field>(allFields);
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = new List<HarvestYearPlanResponse>();
            List<HarvestYearPlanResponse> harvestYearPlanResponse = new List<HarvestYearPlanResponse>();
            (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
            {
                cropPlanForFirstCropFilter = harvestYearPlanResponse
                .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))).ToList();
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponse != null)
                    {
                        var fieldIds = harvestYearPlanResponse.Select(x => x.FieldID).ToList();
                        fieldList = fieldList.Where(x => fieldIds.Contains(x.ID.Value)).ToList();
                    }
                }
            }
            var selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();

            List<int> fieldsAllowedForSecondCrop = new List<int>();
            List<int> fieldRemoveList = new List<int>();
            var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                if (model.FieldList != null)
                {
                    selectListItem.RemoveAll(item => !model.FieldList.Contains(item.Value));
                }
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> allFieldList = new List<Field>(allFields);
                if (allFieldList.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    var fieldIdsToRemove = harvestYearPlanResponse
                        .Select(x => x.FieldID)
                        .ToList();

                    allFieldList.RemoveAll(field => cropPlanForFirstCropFilter
                     .Any(x => x.FieldID == field.ID.Value));

                    cropPlanForFirstCropFilter.RemoveAll(field => fieldIdsToRemove.Contains(field.FieldID));

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
                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
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
                            continue;
                        }
                    }
                }
            }
            else
            {
                //Fetch fields allowed for second crop based on first crop
                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
                if (harvestYearPlanResponse.Count() > 0 || selectListItem.Count == 1)
                {
                    var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                    selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                }
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
            TempData["CropTypeError"] = ex.Message;
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
            Error error = null;
            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            List<Field> allFields = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter = new List<HarvestYearPlanResponse>();
            List<Field> fieldList = new List<Field>(allFields);
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = new List<HarvestYearPlanResponse>();
            (harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
            if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponseForFilter.Count > 0)
            {
                cropPlanForFirstCropFilter = harvestYearPlanResponseForFilter.Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))).ToList();
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponseForFilter != null)
                    {
                        var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID).ToList();
                        fieldList = fieldList.Where(x => fieldIds.Contains(x.ID.Value)).ToList();
                    }
                }
            }

            var selectListItem = fieldList.Select(f => new SelectListItem
            {
                Value = f.ID.ToString(),
                Text = f.Name
            }).ToList();
            List<int> fieldsAllowedForSecondCrop = new List<int>();
            List<int> fieldRemoveList = new List<int>();
            var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                if (model.FieldList != null)
                {
                    selectListItem.RemoveAll(item => !model.FieldList.Contains(item.Value));
                }

                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> allFieldList = new List<Field>(allFields);
                if (allFieldList.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    var fieldIdsToRemove = harvestYearPlanResponseForFilter
                        .Select(x => x.FieldID)
                        .ToList();

                    // Step 1: Remove fields from allFieldList which are already in cropPlanForFirstCropFilter
                    allFieldList.RemoveAll(field => cropPlanForFirstCropFilter
                     .Any(x => x.FieldID == field.ID.Value));

                    cropPlanForFirstCropFilter.RemoveAll(field => fieldIdsToRemove.Contains(field.FieldID));

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

                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
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
                            continue;
                        }
                    }
                }
            }
            else
            {
                //Fetch fields allowed for second crop based on first crop
                (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);

                if (harvestYearPlanResponseForFilter.Count() > 0 || selectListItem.Count == 1)
                {
                    var harvestFieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID.ToString()).ToList();
                    selectListItem = selectListItem.Where(x => !harvestFieldIds.Contains(x.Value) || fieldsAllowedForSecondCrop.Contains(int.Parse(x.Value))).ToList();
                }
            }
            if (model.FieldList == null || model.FieldList.Count == 0)
            {
                ModelState.AddModelError("FieldList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                ViewBag.fieldList = selectListItem.Count > 0 ? selectListItem.OrderBy(x => x.Text).ToList() : null;
                return View(model);
            }
            if (model.FieldList.Count > 0 && model.FieldList.Contains(Resource.lblSelectAll))
            {
                model.FieldList = selectListItem.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
            }
            if (model.FieldList.Count > 0)
            {
                if (model.Crops == null)
                {
                    model.Crops = new List<Crop>();
                }
                if (model.Crops.Count > 0)
                {
                    model.Crops.Clear();
                }
                int counter = 1;
                foreach (var field in model.FieldList)
                {
                    if (int.TryParse(field, out int fieldId))
                    {
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
                        if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                        {
                            if (harvestYearPlanResponseForFilter != null && harvestYearPlanResponseForFilter.Any(x => x.FieldID == fieldId))
                            {
                                crop.ID = harvestYearPlanResponseForFilter.Where(x => x.FieldID == fieldId).Select(x => x.CropID).FirstOrDefault();
                                crop.CropOrder = harvestYearPlanResponseForFilter.Where(x => x.FieldID == fieldId).Select(x => x.CropOrder).FirstOrDefault();
                            }
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
                            return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                        }

                        model.Crops.Add(crop);
                        if (model.FieldList.Count == 1)
                        {
                            Field? defaultField = allFields.FirstOrDefault(x => x.ID == fieldId);
                            model.FieldName = defaultField?.Name;
                        }
                    }
                }
            }
            bool matchFound = false;
            if (model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
            {
                PlanViewModel planViewModel = GetCropFromSession();
                if (planViewModel != null)
                {
                    if (planViewModel.Crops != null && planViewModel.Crops.Count > 0)
                    {
                        foreach (var cropList1 in model.Crops)
                        {
                            matchFound = planViewModel.Crops.Any(cropList2 => cropList2.FieldID == cropList1.FieldID);
                            if (matchFound && model.Crops.Count == 1)
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
                                break;
                            }
                        }
                        if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ||
                           model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                        {
                            if (model.Crops.Count == 1)
                            {
                                model.SowingDateQuestion = model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields ? null : model.SowingDateQuestion;
                                model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                                SetCropToSession(model);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogError("Crop Controller : Session not found in CropFields() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }

            if (harvestYearPlanResponseForFilter.Count > 0)
            {
                var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID.ToString()).ToList();

                model.IsAnyChangeInField = fieldIds.Except(model.FieldList).Any() || model.FieldList.Except(fieldIds).Any();
            }

            SetCropToSession(model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsAnyChangeInField)
            {
                return RedirectToAction("AddOrRemoveField");
            }
            if (matchFound && (!model.IsAnyChangeInField) && model.IsCheckAnswer && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
            {
                SetCropToSession(model);
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("CropGroupName");
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CropFields() post action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnSelectField"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SowingDateQuestion()
    {
        _logger.LogTrace("Crop Controller : SowingDateQuestion() action called");
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogError("Crop Controller : Session not found in SowingDateQuestion() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
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
        if (model.SowingDateQuestion == null)
        {
            ModelState.AddModelError("SowingDateQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            if (model.IsCheckAnswer)
            {
                PlanViewModel? planViewModel = GetCropFromSession();
                if (planViewModel == null)
                {
                    _logger.LogError("Crop Controller : Session not found in SowingDateQuestion() post action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                if (planViewModel.SowingDateQuestion == model.SowingDateQuestion && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                else if (planViewModel.SowingDateQuestion != model.SowingDateQuestion)
                {
                    model.IsQuestionChange = true;
                    model.SowingDateCurrentCounter = 0;
                }
            }

            SetCropToSession(model);
            if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.NoIWillEnterTheDateLater)
            {
                if (model.Crops != null)
                {
                    for (int i = 0; i < model.Crops.Count; i++)
                    {
                        if (model.Crops[i].SowingDate != null)
                        {
                            model.Crops[i].SowingDate = null;
                        }
                    }
                    SetCropToSession(model);
                }
                if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange) && !model.IsCurrentSwardChange)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                SetCropToSession(model);

                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
                {
                    if (model.IsCheckAnswer && !model.IsCropGroupChange && !model.IsAnyChangeInField && !model.IsCurrentSwardChange)
                    {
                        return RedirectToAction(_checkAnswerActionName);
                    }
                    else
                    {
                        return RedirectToAction("SwardType");
                    }
                }
                return RedirectToAction("YieldQuestion");

            }
            else
            {
                if (!model.IsCheckAnswer)
                {
                    return RedirectToAction("SowingDate");
                }
                else
                {
                    model.SowingDateCurrentCounter = 0;
                    SetCropToSession(model);
                    return RedirectToAction("SowingDate");
                }
            }
        }
        catch (Exception ex)
        {
            TempData["SowingDateQuestionError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SowingDate(string q)
    {
        _logger.LogTrace("Crop Controller : SowingDate({q}) action called");
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
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Crops[" + model.SowingDateCurrentCounter + "].SowingDate"))
            {
                var dateError = ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"]?.Errors.Count > 0 ?
                                ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"]?.Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && (dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, "SowingDate")) ||
                    dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, "SowingDate")) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, "SowingDate"))))
                {
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"]?.Errors.Clear();
                    ModelState["Crops[" + model.SowingDateCurrentCounter + "].SowingDate"]?.Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }
            if (model.Crops[model.SowingDateCurrentCounter].SowingDate == null)
            {
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", Resource.MsgEnterADateBeforeContinuing);
            }

            bool isPerennial = await _organicManureLogic.FetchIsPerennialByCropTypeId(model.CropTypeID.Value);

            //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
            DateTime maxDate = new DateTime(model.Year.Value, 12, 31, 00, 00, 00, DateTimeKind.Unspecified);

            if (model.Crops[model.SowingDateCurrentCounter].SowingDate > maxDate)
            {
                //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgPlantingDateAfterHarvestYear, model.Year.Value, maxDate.Date.ToString("dd MMMM yyyy")));
            }

            if (!isPerennial)
            {
                DateTime minDate = new DateTime(model.Year.Value - 1, 01, 01, 00, 00, 00, DateTimeKind.Unspecified);
                if (model.Crops[model.SowingDateCurrentCounter].SowingDate < minDate)
                {
                    //Anil Yadav 23.01.2025 : NMPT1070 NMPT Date Validation Rules​: If perennial flag is true = no minimum date validation.Max date = end of calendar
                    ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgPlantingDateBeforeHarvestYear, model.Year.Value, minDate.Date.ToString("dd MMMM yyyy")));
                }
            }

            if ((model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.WinterWheat ||
                model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.WinterTriticale ||
                model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.ForageWinterTriticale ||
                model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat) && model.Crops[model.SowingDateCurrentCounter].SowingDate != null &&
                    model.Crops[model.SowingDateCurrentCounter]?.SowingDate.Value.Month >= 2 && model.Crops[model.SowingDateCurrentCounter].SowingDate.Value.Month <= 6)
            {
                ModelState.AddModelError("Crops[" + model.SowingDateCurrentCounter + "].SowingDate", string.Format(Resource.MsgForSowingDate, model.CropType));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
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
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            else if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
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
        catch (Exception ex)
        {
            TempData["SowingDateError"] = ex.Message;
            return View(model);
        }
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
            decimal defaultYieldForCropType = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYieldForCropType > 0)
            {
                ViewBag.IsYieldOptional = Resource.lblYes;
            }
            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                if (model.YieldCurrentCounter == 0)
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
                return View(model);
            }
            if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields)
            {
                model.YieldCurrentCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Yield = defaultYieldForCropType;
                }

                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                SetCropToSession(model);
                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    SetCropToSession(model);
                    if (model.IsCropTypeChange)
                    {
                        return RedirectToAction("CropInfoOne");
                    }
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }
            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYield > 0)
            {
                ViewBag.DefaultYield = defaultYield;
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
            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYield == 0)
            {
                if (model.Crops[model.YieldCurrentCounter].Yield == null)
                {
                    ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", string.Format(Resource.MsgEnterExpectedYieldforCropinField, model.CropType, model.FieldName));
                }
            }
            if (model.Crops[model.YieldCurrentCounter].Yield != null)
            {
                if (model.Crops[model.YieldCurrentCounter].Yield > Convert.ToInt32(Resource.lblFiveDigit))
                {
                    ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", Resource.MsgEnterAValueOfNoMoreThan5Digits);
                }
                if (model.Crops[model.YieldCurrentCounter].Yield < 0)
                {
                    ModelState.AddModelError("Crops[" + model.YieldCurrentCounter + "].Yield", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblYield));
                }
            }

            if (defaultYield > 0)
            {
                ViewBag.IsYieldOptional = Resource.lblYes;
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
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
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            else if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
            {
                model.YieldCurrentCounter = 1;
                for (int i = 0; i < model.Crops.Count; i++)
                {
                    model.Crops[i].Yield = model.Crops[0].Yield;
                }
                model.YieldEncryptedCounter = _fieldDataProtector.Protect(model.YieldCurrentCounter.ToString());
                SetCropToSession(model);
                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    SetCropToSession(model);
                    if (model.IsCropTypeChange)
                    {
                        return RedirectToAction("CropInfoOne");
                    }
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }

            if (model.YieldCurrentCounter == model.Crops.Count)
            {
                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || (model.IsCheckAnswer))
                {
                    if (model.IsAnyChangeInField && (!model.IsCropGroupChange) && (!model.IsCropTypeChange))
                    {
                        model.IsAnyChangeInField = false;
                    }
                    SetCropToSession(model);
                    if (model.IsAnyChangeInField || model.IsCropGroupChange || model.IsCropTypeChange)
                    {
                        return RedirectToAction("CropInfoOne");
                    }
                    return RedirectToAction(_checkAnswerActionName);
                }
                else
                {
                    return RedirectToAction("CropInfoOne");
                }
            }
            else
            {
                return View(model);
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorOnYield"] = ex.Message;
            return RedirectToAction("Yield", new { q = model.YieldEncryptedCounter });
        }
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
                model.CropInfo1Name = cropInfoOneList?
                .FirstOrDefault(x => x.CropInfo1Name == Resource.lblNone)
                ?.CropInfo1Name;
                model.CropInfo1 = cropInfoOneList?
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

            SetCropInfoOne(model, cropInfoOneList);

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
                model.CropTypeID ?? 0);

        if (!string.IsNullOrWhiteSpace(question))
        {
            ViewBag.CropInfoOneQuestion =
                (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions ||
                 model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions)
                    ? string.Format(question, model.CropType)
                    : question;
        }

        ViewBag.CropInfoOneList =
            cropInfoOneList.OrderBy(c => c.CropInfo1Name);
    }

    private static void SetCropInfoOne(
        PlanViewModel model,
        List<CropInfoOneResponse> cropInfoOneList)
    {
        model.CropInfo1Name = cropInfoOneList
            .FirstOrDefault(x => x.CropInfo1Id == model.CropInfo1)
            ?.CropInfo1Name;

        model.Crops.ForEach(c => c.CropInfo1 = model.CropInfo1);
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
            model.CropInfo2Name = cropInfoTwoList.FirstOrDefault(x => x.CropInfo2Id == model.CropInfo2).CropInfo2;
            for (int i = 0; i < model.Crops.Count; i++)
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
        Error error = null;
        List<HarvestYearPlanResponse> harvestYearPlanResponse = null;

        string yieldQuestion = null;
        string sowingQuestion = null;
        decimal? firstYield = null;
        bool isBasePlan = false;
        bool allYieldsAreSame = true;
        bool allSowingAreSame = true;
        DateTime? firstSowingDate = null;
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

                (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
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
                        decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(harvestYearPlanResponse.FirstOrDefault().CropTypeID, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
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
                            crop.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
                            crop.SwardManagementID = harvestYearPlanResponse.FirstOrDefault().SwardManagementID;
                            crop.DefoliationSequenceID = harvestYearPlanResponse.FirstOrDefault().DefoliationSequenceID;
                            crop.SwardTypeID = harvestYearPlanResponse.FirstOrDefault().SwardTypeID;
                            crop.PotentialCut = harvestYearPlanResponse.FirstOrDefault().PotentialCut;
                            model.SwardManagementId = harvestYearPlanResponse.FirstOrDefault().SwardManagementID; ;
                            model.DefoliationSequenceId = harvestYearPlanResponse.FirstOrDefault().DefoliationSequenceID;
                            model.SwardTypeId = harvestYearPlanResponse.FirstOrDefault().SwardTypeID;
                            model.PotentialCut = harvestYearPlanResponse.FirstOrDefault().PotentialCut;
                            if (harvestYearPlanResponse[i].CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                            {
                                model.CurrentSward = (harvestYearPlanResponse.FirstOrDefault().Establishment == null || harvestYearPlanResponse.FirstOrDefault().Establishment == 0) ? (int)NMP.Commons.Enums.CurrentSward.ExistingSward : (int)NMP.Commons.Enums.CurrentSward.NewSward;
                            }
                            else
                            {
                                model.CurrentSward = null;
                            }
                            model.GrassSeason = harvestYearPlanResponse.FirstOrDefault().Establishment;
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
                                sowingQuestion = Resource.lblNoIWillEnterTheDateLater;
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
                                CropTypeResponse cropTypeResponse = cropTypeResponseList.Where(x => x.CropTypeId == crop.CropTypeID).FirstOrDefault();
                                if (cropTypeResponse != null)
                                {
                                    model.CropGroupId = cropTypeResponse.CropGroupId;

                                }
                            }
                            counter++;
                            model.Crops.Add(crop);
                        }

                        if (model.Crops != null && model.Crops.All(x => x.SowingDate != null) && model.SowingDateQuestion == null && allSowingAreSame && harvestYearPlanResponse.Count >= 1)
                        {
                            model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields;

                        }
                        model.CropInfo1 = harvestYearPlanResponse.FirstOrDefault().CropInfo1;
                        model.CropInfo2 = harvestYearPlanResponse.FirstOrDefault().CropInfo2;

                        model.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
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
                        model.CropType = harvestYearPlanResponse.FirstOrDefault().CropTypeName;
                        model.Variety = harvestYearPlanResponse.FirstOrDefault().CropVariety;
                        model.CropGroupName = harvestYearPlanResponse.FirstOrDefault().CropGroupName;
                        model.PreviousCropGroupName = model.CropGroupName;
                        model.OtherCropName = harvestYearPlanResponse.FirstOrDefault().OtherCropName;
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
            int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

            //fetch all fields
            List<Field> allFieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
            List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = new List<HarvestYearPlanResponse>();
            (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmID);
            if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
            {
                cropPlanForFirstCropFilter = harvestYearPlanResponse
                    .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))
                    ).ToList();
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && ((!model.IsFieldToBeRemoved.HasValue || (model.IsFieldToBeRemoved.HasValue && !model.IsFieldToBeRemoved.Value)))
                && string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r) && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    //filter the plan list based on the crop group
                    harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponse != null)
                    {
                        var fieldIds = harvestYearPlanResponse.Select(x => x.FieldID).ToList();

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
                                    (Crop crop, error) = await _cropLogic.FetchCropById(removableField.CropID);
                                    if (string.IsNullOrWhiteSpace(error.Message))
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

            decimal defaultYieldForCropType = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
            if (defaultYieldForCropType > 0)
            {
                ViewBag.IsYieldOptional = Resource.lblYes;
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
            List<int> fieldRemoveList = new List<int>();
            if (harvestYearPlanResponse.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
            {
                harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                if (harvestYearPlanResponse != null && harvestYearPlanResponse.Count == 1)
                {
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
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
            (FarmResponse farm, error) = await _farmLogic.FetchFarmByIdAsync(farmID);
            if (farm != null && string.IsNullOrWhiteSpace(error.Message))
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

            (fieldsAllowedForSecondCrop, fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);

            if (harvestYearPlanResponse.Count() > 0 || fieldsAllowedForSecondCrop.Count() > 0)
            {
                var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString()) || fieldsAllowedForSecondCrop.Contains(x.ID ?? 0)).ToList();
            }

            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                model.IsCropTypeChange = false;
                model.IsCropGroupChange = false;

                (List<SwardTypeResponse> swardTypeResponses, error) = await _cropLogic.FetchSwardTypes();
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["SowingDateError"] = error.Message;
                    return RedirectToAction("SowingDate");
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

                if (model.SwardManagementId != null)
                {
                    (SwardManagementResponse swardManagementResponse, error) = await _cropLogic.FetchSwardManagementBySwardManagementId(model.SwardManagementId ?? 0);
                    if (error != null)
                    {
                        TempData["SwardManagementError"] = error.Message;
                        return RedirectToAction("SwardType");
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

                if (model.DefoliationSequenceId != null)
                {
                    (DefoliationSequenceResponse defoliationSequenceResponse, error) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId ?? 0);
                    if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["DefoliationSequenceError"] = error.Message;
                        return RedirectToAction("Defoliation");
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

                List<GrassSeasonResponse> grassSeasons = await _cropLogic.FetchGrassSeasons();
                grassSeasons.RemoveAll(g => g.SeasonId == 0);
                model.GrassSeasonName = grassSeasons.Where(x => x.SeasonId == model.GrassSeason).Select(x => x.SeasonName).SingleOrDefault();
            }
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
                        if (fieldIds.Count > 0 && fieldIds.Any(fieldId => model.FieldList.Contains(fieldId)))
                        {
                            if (string.IsNullOrWhiteSpace(model.CropGroupName))
                            {
                                model.CropGroupName = model.PreviousCropGroupName;
                                model.Crops[i].CropGroupName = model.PreviousCropGroupName;
                            }
                        }
                    }
                }
            }

            model.IsCheckAnswer = true;
            if (defaultYieldForCropType > 0)
            {
                ViewBag.DefaultYield = defaultYieldForCropType;
            }

            if (model.CropTypeID != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                string? cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
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
            if (model.CropGroupId != null)
            {
                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
                {
                    model.CropGroup = Resource.lblGrass;
                }
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
            string action = "YieldQuestion";
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

            action = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals ?
                 _cropInfoTwoActionName : (((model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other)
                 || cropInfoOneList.Count == 1) ?
                 ((model.YieldQuestion != (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields) ?
             "Yield" : "YieldQuestion") : "CropInfoOne");

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
        List<CropInfoOneResponse> cropInfoOneResponse = await _cropLogic.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0,model.FarmRB209CountryID);

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

        string action = "YieldQuestion";
        try
        {
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == null)
            {
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction(_harvestYearOverviewActionName, new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            else if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value))
            {
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction("Recommendations", new
                {
                    q = model.EncryptedFarmId,
                    r = model.EncryptedFieldId,
                    s = model.EncryptedHarvestYear
                });
            }
            List<CropInfoOneResponse> cropInfoOneList = await _cropLogic.FetchCropInfoOneByCropTypeId(model.CropTypeID ?? 0,model.FarmRB209CountryID);
            
           
            action = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals ?
               _cropInfoTwoActionName : (((model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other)
               || cropInfoOneList.Count == 1) ?
               ((model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields ||
               model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.NoDoNotEnterAYield) ?
           "YieldQuestion" : "Yield") : "CropInfoOne");

            if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Cereals)
            {
                action = _cropInfoTwoActionName;
            }
            else if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay)
                {
                    if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
                    {
                        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1)
                        {
                            action = "DryMatterYield";
                        }
                        else
                        {
                            action = "GrassGrowthClass";
                        }
                    }
                    else
                    {
                        action = "DefoliationSequence";
                    }
                }

                if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly)
                {
                    if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
                    {
                        action = "GrassGrowthClass";
                    }
                    else
                    {
                        action = "Defoliation";
                    }
                }
            }
            else if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Other || cropInfoOneList.Count == 1)
            {
                action = (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields ||
               model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.NoDoNotEnterAYield)
                    ? "YieldQuestion"
                    : "Yield";
            }
            else
            {
                action = "CropInfoOne";
            }
            model.IsCheckAnswer = false;
            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in BackCheckAnswer() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName, model);
        }

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
        return RedirectToAction(action, new { q = encryptedCounter });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckAnswer(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : CheckAnswer() post action called");
        try
        {
            if (model != null)
            {
                int i = 0;
                int otherGroupId = (int)NMP.Commons.Enums.CropGroup.Other;
                int cerealsGroupId = (int)NMP.Commons.Enums.CropGroup.Cereals;
                int potatoesGroupId = (int)NMP.Commons.Enums.CropGroup.Potatoes;
                if (model.Crops != null)
                {
                    foreach (var crop in model.Crops)
                    {
                        if (crop.SowingDate == null)
                        {
                            if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingSingleDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                                break;
                            }
                            else if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingDiffrentDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                            }
                        }
                        i++;
                    }
                }
                i = 0;
                if (model.Crops != null && model.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)
                {
                    foreach (var crop in model.Crops)
                    {

                        if (crop.Yield == null)
                        {
                            if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                            {
                                decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                                if (defaultYield == 0)
                                {
                                    ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheExpectedYieldForSingleNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                                }
                                break;
                            }
                            else if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                            {
                                decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                                if (defaultYield == 0)
                                {
                                    ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheDifferentExpectedYieldNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                                }
                            }
                        }
                    }
                    i++;
                }

                if (model.CropTypeID == null)
                {
                    ModelState.AddModelError("CropTypeID", Resource.MsgMainCropTypeNotSet);
                }
                if (model.CropInfo1 == null && model.CropGroupId != otherGroupId && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
                {
                    ModelState.AddModelError("CropInfo1", string.Format(Resource.MsgCropInfo1NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                }
                if (model.CropInfo2 == null && model.CropGroupId == cerealsGroupId)
                {
                    ModelState.AddModelError("CropInfo2", string.Format(Resource.MsgCropInfo2NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                }
                if (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                {
                    if (model.SwardManagementId == null)
                    {
                        ModelState.AddModelError("SwardManagementId", string.Format("{0} {1}", string.Format(Resource.lblHowWillTheseFieldsBeManaged, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields)), Resource.lblNotSet));
                    }
                    if (model.PotentialCut == null)
                    {
                        string fieldText = (model.FieldList?.Count == 1)
                            ? model.Crops[0].FieldName
                            : Resource.lblTheseFields;

                        string resourceText = model.SwardManagementId switch
                        {
                            (int)NMP.Commons.Enums.SwardManagement.GrazedOnly =>
                                Resource.lblHowManyGrazingsWillYouHaveInTheseFields,

                            (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly =>
                                Resource.lblHowManyCutsWillYouHaveInTheseFields,

                            (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly =>
                                Resource.lblHowManyCutsWillYouHaveInTheseFields,

                            (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage =>
                                Resource.lblHowManyCutsAndGrazingsWillYouHaveInTheseFields,

                            _ => Resource.lblHowManyCutsAndGrazingsWillYouHaveInTheseFields
                        };

                        string message = string.Format(resourceText, fieldText);

                        ModelState.AddModelError("PotentialCut", $"{message} {Resource.lblNotSet}");
                    }

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

                    if ((model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass))
                    {
                        foreach (var crop in model.Crops)
                        {
                            if (crop.Yield == null)
                            {
                                if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                                {
                                    ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForFields, crop.Year));

                                    break;
                                }
                                else if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                                {
                                    ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForField, crop.FieldName, crop.Year));

                                }
                            }
                            i++;
                        }
                    }
                    TempData["ModelStateErrorForGrass"] = true;
                }

            }
            if (!ModelState.IsValid)
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    var fieldIds = model.Crops.Select(c => c.FieldID).Distinct();
                    fieldList = fieldList.Where(x => fieldIds.Contains(x.ID)).ToList();
                }
                if (model.CropTypeID != null)
                {
                    decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                    if (defaultYield > 0)
                    {
                        ViewBag.DefaultYield = defaultYield;
                    }
                }
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error harvestYearError) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);

                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    var fieldIdsForFilter = fieldList.Select(f => f.ID);
                    harvestYearPlanResponse = harvestYearPlanResponse
                        .Where(x => fieldIdsForFilter.Contains(x.FieldID))
                        .ToList();
                }
                if (string.IsNullOrWhiteSpace(harvestYearError.Message))
                {
                    //Fetch fields allowed for second crop based on first crop
                    var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
                    List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                        .Where(x => x.IsBasePlan != null && (!x.IsBasePlan.Value)).ToList();

                    (List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
                    if (harvestYearPlanResponse.Count() > 0 || fieldsAllowedForSecondCrop.Count() > 0)
                    {
                        var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString()) || fieldsAllowedForSecondCrop.Contains(x.ID ?? 0)).ToList();
                    }
                }
                else
                {
                    TempData["ErrorCreatePlan"] = harvestYearError.Message;
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                    return RedirectToAction(_checkAnswerActionName);
                }

                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
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
                return View(_checkAnswerActionName, model);
            }


            Error error = null;
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);

            int? lastGroupNumber = null;
            if (string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (harvestYearPlanResponse != null && error.Message == null)
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
            List<CropData> cropEntries = new List<CropData>();
            foreach (Crop crop in model.Crops)
            {
                crop.IsBasePlan = false;
                crop.CreatedOn = DateTime.Now;
                crop.CreatedByID = userId;
                crop.FieldName = null;
                crop.EncryptedCounter = null;
                crop.FieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;
                //crop.CropOrder = 1;
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
                if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
                {
                    CropData cropEntry = new CropData
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
                    cropEntries.Add(cropEntry);
                }
                else
                {
                    //if yield null then save as 0
                    crop.Yield = crop.Yield;

                    crop.DefoliationSequenceID = model.DefoliationSequenceId;
                    crop.SwardTypeID = model.SwardTypeId;
                    crop.SwardManagementID = model.SwardManagementId;
                    crop.PotentialCut = model.PotentialCut;
                    crop.Establishment = model.GrassSeason ?? 0;

                    List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
                    string defoliationSequence = "";
                    (DefoliationSequenceResponse defoliationSequenceResponse, error) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId.Value);

                    if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["ErrorCreatePlan"] = error.Message;
                        return RedirectToAction(_checkAnswerActionName);
                    }
                    else
                    {
                        defoliationSequence = defoliationSequenceResponse.DefoliationSequence;
                    }

                    int i = 1;
                    int utilisation1 = 0;
                    if (defoliationSequence != null)
                    {
                        foreach (char c in defoliationSequence)
                        {
                            if (c == 'E')
                            {
                                utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Establishment;
                            }
                            else if (c == 'G')
                            {
                                utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Grazing;
                            }
                            else if (c == 'S')
                            {
                                utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Silage;
                            }
                            else if (c == 'H')
                            {
                                utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Hay;
                            }

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

                    CropData cropEntry = new CropData
                    {
                        Crop = crop,
                        ManagementPeriods = managementPeriods
                    };
                    cropEntries.Add(cropEntry);
                }
            }
            CropDataWrapper cropDataWrapper = new CropDataWrapper
            {
                Crops = cropEntries
            };
            (bool success, error) = await _cropLogic.AddCropNutrientManagementPlan(cropDataWrapper);
            if ((error == null || string.IsNullOrWhiteSpace(error.Message)) && success)
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
            else
            {
                TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater; //error.Message; //
                return RedirectToAction(_checkAnswerActionName);
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorCreatePlan"] = ex.Message;
            return RedirectToAction(_checkAnswerActionName);
        }
    }

    [HttpGet]
    public async Task<IActionResult> HarvestYearOverview(string id, string year, string? q, string? r, string? s, string? t, string? u, string? v, string? w)//w is a link
    {
        _logger.LogTrace("Crop Controller : HarvestYearOverview({0}, {1}, {2}, {3}) action called", id, year, q, r);
        PlanViewModel? model = null;
        try
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
            if (HttpContext.Session.Exists("ReportData"))
            {
                HttpContext.Session.Remove("ReportData");
            }
            if (HttpContext.Session.Exists("StorageCapacityData"))
            {
                HttpContext.Session.Remove("StorageCapacityData");
            }
            if (HttpContext.Session.Exists("FertiliserManure"))
            {
                HttpContext.Session.Remove("FertiliserManure");
            }
            if (HttpContext.Session.Exists("OrganicManure"))
            {
                HttpContext.Session.Remove("OrganicManure");
            }

            RemoveCropSession();

            if (!string.IsNullOrWhiteSpace(q))
            {
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
                        if (decryptedFieldId > 0)
                        {
                            Field field = await _fieldLogic.FetchFieldByFieldId(decryptedFieldId);
                            if (field != null)
                            {
                                TempData["fieldName"] = field.Name;
                            }
                        }
                        TempData["successMsgLink"] = w;
                    }
                }
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Success = false;
                RemoveCropSession();
            }

            if (string.IsNullOrWhiteSpace(s) && string.IsNullOrWhiteSpace(u))
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    model = new PlanViewModel();
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                    int harvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(year));

                    (FarmResponse farm, Error error) = await _farmLogic.FetchFarmByIdAsync(farmId);
                    if (farm != null)
                    {
                        model.FarmName = farm.Name;
                    }
                    (ExcessRainfalls excessRainfalls, error) = await _farmLogic.FetchExcessRainfallsAsync(farmId, harvestYear);

                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["ErrorOnHarvestYearOverview"] = error.Message;
                        return View(_harvestYearOverviewActionName, model);
                    }
                    else
                    {
                        if (excessRainfalls != null && excessRainfalls.WinterRainfall != null)
                        {
                            model.ExcessWinterRainfallValue = excessRainfalls.WinterRainfall.Value;
                            model.AnnualRainfall = excessRainfalls.WinterRainfall.Value;
                            model.IsExcessWinterRainfallUpdated = true;
                            (List<CommonResponse> excessWinterRainfallOption, error) = await _farmLogic.FetchExcessWinterRainfallOptionAsync();
                            if (string.IsNullOrWhiteSpace(error.Message) && excessWinterRainfallOption != null && excessWinterRainfallOption.Count > 0)
                            {
                                string excessRainfallName = (excessWinterRainfallOption.FirstOrDefault(x => x.Value == model.ExcessWinterRainfallValue)).Name;
                                string[] parts = excessRainfallName.Split(new string[] { " - " }, StringSplitOptions.None);
                                model.ExcessWinterRainfallName = $"{parts[0]} ({parts[1]})";
                                model.ExcessWinterRainfallId = (excessWinterRainfallOption.FirstOrDefault(x => x.Value == model.ExcessWinterRainfallValue)).Id;
                            }

                            ViewBag.ExcessRainfallContentFirst = string.Format(Resource.lblExcessWinterRainfallWithValue, model.ExcessWinterRainfallName);
                            ViewBag.ExcessRainfallContentSecond = Resource.lblUpdateExcessWinterRainfall;
                        }
                        else
                        {
                            model.AnnualRainfall = farm.Rainfall.Value;
                            model.IsExcessWinterRainfallUpdated = false;
                            ViewBag.ExcessRainfallContentFirst = Resource.lblYouHaveNotEnteredAnyExcessWinterRainfall;
                            ViewBag.ExcessRainfallContentSecond = string.Format(Resource.lblAddExcessWinterRainfallForHarvestYear, harvestYear);
                        }
                    }

                    List<string> fields = new List<string>();

                    (HarvestYearResponseHeader harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansDetailsByFarmId(harvestYear, farmId);
                    model.Year = harvestYear;

                    if (harvestYearPlanResponse != null && error.Message == null)
                    {
                        List<CropDetailResponse> allCropDetails = harvestYearPlanResponse.CropDetails ?? new List<CropDetailResponse>().ToList();
                        if (allCropDetails != null)
                        {
                            var latestDate = allCropDetails
                              .Where(x => x.LastModifiedOn.HasValue)
                              .OrderByDescending(x => x.LastModifiedOn)
                              .FirstOrDefault();

                            model.LastModifiedOn = latestDate?.LastModifiedOn?.ToString("dd MMM yyyy");
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
                            model.FieldCount = allCropDetails.Select(h => h.FieldID).Distinct().Count();
                            List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmId);
                            bool isSecondCropAllowed = await IsSecondCropAllowed(allCropDetails);
                            if (harvestYearPlanResponse.CropDetails.Count() > 0)
                            {
                                var harvestFieldIds = allCropDetails.Select(x => x.FieldID.ToString()).ToList();
                                fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString())).ToList();
                                if (fieldList.Count > 0)
                                {
                                    ViewBag.PendingField = true;
                                }
                                else
                                {
                                    ViewBag.PendingField = isSecondCropAllowed;
                                }
                            }

                            model.AnnualRainfall = harvestYearPlanResponse.farmDetails.Rainfall;
                            var harvestYearPlans = new HarvestYearPlans
                            {

                                FieldData = new List<HarvestYearPlanFields>(),
                                OrganicManureList = new List<OrganicManureResponse>(),
                                InorganicFertiliserList = new List<InorganicFertiliserResponse>(),
                            };
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
                                        Variety = plan.CropVariety
                                    };

                                    if (plan.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && !string.IsNullOrWhiteSpace(plan.Management))
                                    {
                                        List<string> defoliationList = plan.Management
                                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();
                                        fieldDetail.Management = ShorthandDefoliationSequence(defoliationList);
                                    }

                                    newField.FieldData.Add(fieldDetail);
                                }

                                harvestYearPlans.FieldData.Add(newField);
                            }

                            if (harvestYearPlanResponse.OrganicMaterial.Count > 0)
                            {
                                harvestYearPlans.OrganicManureList = harvestYearPlanResponse.OrganicMaterial.OrderByDescending(x => x.ApplicationDate).ToList();
                                harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedId = _cropDataProtector.Protect(m.ID.ToString()));
                                ViewBag.Organic = _cropDataProtector.Protect(Resource.lblOrganic);
                                harvestYearPlans.OrganicManureList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));
                                foreach (var organic in harvestYearPlans.OrganicManureList)
                                {
                                    (ManureType? manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(organic.ManureTypeId.Value);
                                    if (error == null && manureType != null)
                                    {
                                        organic.RateUnit = manureType.IsLiquid.HasValue && manureType.IsLiquid.Value ? Resource.lblCubicMeters : Resource.lbltonnes;
                                    }
                                    else
                                    {
                                        TempData["ErrorOnHarvestYearOverview"] = error.Message;
                                        return View(_harvestYearOverviewActionName, model);
                                    }
                                }
                            }

                            if (harvestYearPlanResponse.InorganicFertiliserApplication.Count > 0)
                            {
                                harvestYearPlans.InorganicFertiliserList = harvestYearPlanResponse.InorganicFertiliserApplication.OrderByDescending(x => x.ApplicationDate).ToList();
                                harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFertId = _cropDataProtector.Protect(m.ID.ToString()));
                                ViewBag.Fertliser = _cropDataProtector.Protect(Resource.lblFertiliser);
                                harvestYearPlans.InorganicFertiliserList.ForEach(m => m.EncryptedFieldName = _cropDataProtector.Protect(m.Field.ToString()));
                            }

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
                            ViewBag.InOrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                            ViewBag.InOrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                            ViewBag.OrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                            ViewBag.OrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                            ViewBag.OrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
                            ViewBag.InOrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
                            model.HarvestYearPlans = harvestYearPlans;
                            model.EncryptedFarmId = id;
                            model.EncryptedHarvestYear = year;
                            model.Year = harvestYear;
                        }
                        else
                        {
                            TempData["ErrorOnHarvestYearOverview"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;//error.Message; //
                            model = null;
                        }
                    }

                    HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                }
            }
            else
            {
                if (HttpContext.Session.Keys.Contains("HarvestYearPlan"))
                {
                    model = HttpContext.Session.GetObjectFromJson<PlanViewModel>("HarvestYearPlan");
                }
                else
                {
                    _logger.LogError("Crop Controller : Session not found in HarvestYearOverview() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                if (model != null)
                {
                    if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(u))
                    {
                        string decrypSortBy = _cropDataProtector.Unprotect(s);
                        string decrypOrder = _cropDataProtector.Unprotect(u);
                        if (!string.IsNullOrWhiteSpace(decrypSortBy) && !string.IsNullOrWhiteSpace(decrypOrder))
                        {
                            if (!string.IsNullOrWhiteSpace(t))
                            {
                                string decryptTabName = _cropDataProtector.Unprotect(t);
                                if (!string.IsNullOrWhiteSpace(decryptTabName))
                                {
                                    if (decryptTabName == Resource.lblOrganicMaterialApplicationsForSorting && model.HarvestYearPlans.OrganicManureList != null)
                                    {
                                        if (decrypOrder == Resource.lblDesc)
                                        {
                                            model.EncryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblField);
                                            model.EncryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDate);
                                            if (decrypSortBy == Resource.lblField)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderByDescending(x => x.Field).ToList();
                                                model.EncryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortOrganicListOrderByFieldName = Resource.lblDesc;
                                                model.SortOrganicListOrderByDate = null;
                                                model.SortOrganicListOrderByCropType = null;
                                            }
                                            else if (decrypSortBy == Resource.lblDate)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderByDescending(x => x.ApplicationDate).ToList();
                                                model.EncryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortOrganicListOrderByDate = Resource.lblDesc;
                                                model.SortOrganicListOrderByFieldName = null;
                                                model.SortOrganicListOrderByCropType = null;

                                            }
                                            else if (decrypSortBy == Resource.lblCropType)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderByDescending(x => x.Crop).ToList();
                                                model.EncryptSortOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortOrganicListOrderByCropType = Resource.lblDesc;
                                                model.SortOrganicListOrderByFieldName = null;
                                                model.SortOrganicListOrderByDate = null;

                                            }
                                        }
                                        else
                                        {
                                            if (decrypSortBy == Resource.lblField)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderBy(x => x.Field).ToList();
                                                model.EncryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortOrganicListOrderByFieldName = Resource.lblAsc;
                                                model.SortOrganicListOrderByDate = null;
                                                model.SortOrganicListOrderByCropType = null;
                                            }
                                            else if (decrypSortBy == Resource.lblDate)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderBy(x => x.ApplicationDate).ToList();
                                                model.EncryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortOrganicListOrderByDate = Resource.lblAsc;
                                                model.SortOrganicListOrderByFieldName = null;
                                                model.SortOrganicListOrderByCropType = null;

                                            }
                                            else if (decrypSortBy == Resource.lblCropType)
                                            {
                                                model.HarvestYearPlans.OrganicManureList = model.HarvestYearPlans.OrganicManureList.OrderBy(x => x.Crop).ToList();
                                                model.EncryptSortOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortOrganicListOrderByCropType = Resource.lblAsc;
                                                model.SortOrganicListOrderByFieldName = null;
                                                model.SortOrganicListOrderByDate = null;

                                            }
                                        }
                                    }
                                    else if (decryptTabName == Resource.lblInorganicFertiliserApplicationsForSorting && model.HarvestYearPlans.InorganicFertiliserList != null)
                                    {
                                        if (decrypOrder == Resource.lblDesc)
                                        {
                                            if (decrypSortBy == Resource.lblField)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderByDescending(x => x.Field).ToList();
                                                model.EncryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortInOrganicListOrderByFieldName = Resource.lblDesc;
                                                model.SortInOrganicListOrderByDate = null;
                                                model.SortInOrganicListOrderByCropType = null;
                                            }
                                            else if (decrypSortBy == Resource.lblDate)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderByDescending(x => x.ApplicationDate).ToList();
                                                model.EncryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortInOrganicListOrderByDate = Resource.lblDesc;
                                                model.SortInOrganicListOrderByFieldName = null;
                                                model.SortInOrganicListOrderByCropType = null;

                                            }
                                            else if (decrypSortBy == Resource.lblCropType)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderByDescending(x => x.Crop).ToList();
                                                model.EncryptSortInOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
                                                model.SortInOrganicListOrderByCropType = Resource.lblDesc;
                                                model.SortInOrganicListOrderByDate = null;
                                                model.SortInOrganicListOrderByFieldName = null;

                                            }
                                        }
                                        else
                                        {
                                            if (decrypSortBy == Resource.lblField)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderBy(x => x.Field).ToList();
                                                model.EncryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortInOrganicListOrderByFieldName = Resource.lblAsc;
                                                model.SortInOrganicListOrderByDate = null;
                                                model.SortInOrganicListOrderByCropType = null;
                                            }
                                            else if (decrypSortBy == Resource.lblDate)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderBy(x => x.ApplicationDate).ToList();
                                                model.EncryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortInOrganicListOrderByDate = Resource.lblAsc;
                                                model.SortInOrganicListOrderByFieldName = null;
                                                model.SortInOrganicListOrderByCropType = null;

                                            }
                                            else if (decrypSortBy == Resource.lblCropType)
                                            {
                                                model.HarvestYearPlans.InorganicFertiliserList = model.HarvestYearPlans.InorganicFertiliserList.OrderBy(x => x.Crop).ToList();
                                                model.EncryptSortInOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblAsc);
                                                model.SortInOrganicListOrderByCropType = Resource.lblAsc;
                                                model.SortInOrganicListOrderByDate = null;
                                                model.SortInOrganicListOrderByFieldName = null;

                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        model.SortOrganicListOrderByDate = Resource.lblDesc;
                        model.SortInOrganicListOrderByDate = Resource.lblDesc;
                        model.SortOrganicListOrderByFieldName = null;
                        model.SortInOrganicListOrderByFieldName = null;
                        model.SortOrganicListOrderByCropType = null;
                        model.SortInOrganicListOrderByCropType = null;
                        model.EncryptSortInOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                        model.EncryptSortOrganicListOrderByDate = _cropDataProtector.Protect(Resource.lblDesc);
                        model.EncryptSortOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                        model.EncryptSortInOrganicListOrderByFieldName = _cropDataProtector.Protect(Resource.lblDesc);
                        model.EncryptSortOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
                        model.EncryptSortInOrganicListOrderByCropType = _cropDataProtector.Protect(Resource.lblDesc);
                    }
                    ViewBag.InOrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                    ViewBag.InOrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                    ViewBag.InOrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
                    ViewBag.OrganicListSortByFieldName = _cropDataProtector.Protect(Resource.lblField);
                    ViewBag.OrganicListSortByDate = _cropDataProtector.Protect(Resource.lblDate);
                    ViewBag.OrganicListSortByCropType = _cropDataProtector.Protect(Resource.lblCropType);
                    HttpContext.Session.SetObjectAsJson("HarvestYearPlan", model);
                }
            }
            if (HttpContext.Session.Exists("ReportData"))
            {
                HttpContext.Session.Remove("ReportData");
            }
            if (HttpContext.Session.Exists("StorageCapacityData"))
            {
                HttpContext.Session.Remove("StorageCapacityData");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in HarvestYearOverview() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;
            model = null;
        }
        return View(model);
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

    [HttpGet]
    public async Task<IActionResult> PlansAndRecordsOverview(string id, string? year, string? q)
    {
        _logger.LogTrace("Crop Controller : PlansAndRecordsOverview({Id}, {Year}) action called", id, year);
        PlanViewModel model = new PlanViewModel();
        if (HttpContext.Session.Exists("FertiliserManure"))
        {
            HttpContext.Session.Remove("FertiliserManure");
        }
        if (HttpContext.Session.Exists("OrganicManure"))
        {
            HttpContext.Session.Remove("OrganicManure");
        }

        RemoveCropSession();
        if (!string.IsNullOrWhiteSpace(q))
        {
            TempData["successMsg"] = _cropDataProtector.Unprotect(q);
            ViewBag.Success = true;
        }
        if (!string.IsNullOrWhiteSpace(id))
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
            (FarmResponse farm, Error error) = await _farmLogic.FetchFarmByIdAsync(farmId);
            model.FarmName = farm.Name;
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

            }
            ViewBag.PlanSummaryList = planSummaryResponse;

            //fetch oldest previous cropping
            (int? topPrevCroppingYear, error) = await _previousCroppingLogic.FetchPreviousCroppingYearByFarmdId(farmId);
            if (string.IsNullOrWhiteSpace(error.Message) && topPrevCroppingYear > 0)
            {
                DateTime currentDate = DateTime.Now;
                DateTime harvestYearEndDate = new DateTime(currentDate.Year, 7, 31);
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
                if (planSummaryResponse != null && planSummaryResponse.Count > 0)
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
                        model.HarvestYear.Add(harvestNewYear);
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

                            if (minYear == i)
                            {
                                harvestYear.IsThisOldYear = true;
                            }
                            model.HarvestYear.Add(harvestYear);
                        }
                    }
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
                        model.HarvestYear.Add(harvestYear);
                    }
                }
            }
            if (model.HarvestYear.Count > 0)
            {
                model.HarvestYear = model.HarvestYear.OrderByDescending(x => x.Year).ToList();
            }
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

    [HttpGet]
    public async Task<IActionResult> Recommendations(string q, string r, string? s, string? t, string? u, string? sns)//q=farmId,r=fieldId,s=harvestYear
    {
        _logger.LogTrace("Crop Controller : Recommendations({Q}, {R}, {S}) action called", q, r, s);
        RecommendationViewModel model = new RecommendationViewModel();
        Error error = null;
        int decryptedFarmId = 0;
        int decryptedFieldId = 0;
        int decryptedHarvestYear = 0;
        List<RecommendationHeader> recommendations = null;
        List<Crop> crops = null;
        try
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

            //string q, 
            if (!string.IsNullOrWhiteSpace(t))
            {
                ViewBag.Success = true;
                TempData["successMsg"] = _cropDataProtector.Unprotect(t);
                if (!string.IsNullOrWhiteSpace(u))
                {
                    TempData["successMsgSecond"] = _cropDataProtector.Unprotect(u);
                }
            }

            if (HttpContext.Session.Exists("PreviousCroppingData"))
            {
                HttpContext.Session.Remove("PreviousCroppingData");
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (FarmResponse farm, error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
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

            if (!string.IsNullOrWhiteSpace(sns))
            {
                TempData["successSnsAnalysis"] = _cropDataProtector.Unprotect(sns);
            }

            (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(decryptedHarvestYear, decryptedFarmId);
            if (harvestYearPlanResponse != null && error.Message == null)
            {
                bool isAllBasePlan = harvestYearPlanResponse.All(h => ((h.IsBasePlan != null) && (h.IsBasePlan.Value)));
                if (isAllBasePlan)
                {
                    ViewBag.AddMannerDisabled = true;
                }
                if (decryptedFieldId > 0 && decryptedHarvestYear > 0)
                {
                    (recommendations, error) = await _cropLogic.FetchRecommendationByFieldIdAndYear(decryptedFieldId, decryptedHarvestYear);
                    if (error == null)
                    {
                        ViewBag.IsComingFromRecommendation = _cropDataProtector.Protect(Resource.lblFalse.ToString());
                        if (model.Crops == null)
                        {
                            model.Crops = new List<CropViewModel>();
                        }
                        if (model.ManagementPeriods == null)
                        {
                            model.ManagementPeriods = new List<ManagementPeriodViewModel>();
                        }
                        if (model.Recommendations == null)
                        {
                            model.Recommendations = new List<Recommendation>();
                        }
                        if (model.RecommendationComments == null)
                        {
                            model.RecommendationComments = new List<RecommendationComment>();
                        }
                        if (model.OrganicManures == null)
                        {
                            model.OrganicManures = new List<OrganicManureDataViewModel>();
                        }
                        if (model.FertiliserManures == null)
                        {
                            model.FertiliserManures = new List<FertiliserManureDataViewModel>();
                        }

                        int cropCounter = 0;
                        string firstCropName = recommendations.FirstOrDefault().Crops.CropTypeID == 140 ? NMP.Commons.Enums.CropTypes.GetName(typeof(CropTypes), recommendations.FirstOrDefault().Crops.CropTypeID) : await _fieldLogic.FetchCropTypeById(recommendations.FirstOrDefault().Crops.CropTypeID.Value);
                        foreach (var recommendation in recommendations)
                        {
                            //check sns already exist or not in SnsAnalyses table by cropID
                            SnsAnalysis snsData = await _cropLogic.FetchSnsAnalysisByCropIdAsync(recommendation.Crops.ID ?? 0);
                            var crop = new CropViewModel
                            {
                                ID = recommendation.Crops.ID,
                                EncryptedCropId = _cropDataProtector.Protect(recommendation.Crops.ID.ToString()),
                                Year = recommendation.Crops.Year,
                                CropTypeID = recommendation.Crops.CropTypeID,
                                FieldID = recommendation.Crops.FieldID,
                                EncryptedFieldId = _fieldDataProtector.Protect(recommendation.Crops.FieldID.ToString()),
                                Variety = recommendation.Crops.Variety,
                                CropInfo1 = recommendation.Crops.CropInfo1,
                                CropInfo2 = recommendation.Crops.CropInfo2,
                                Yield = recommendation.Crops.Yield,
                                SowingDate = recommendation.Crops.SowingDate,
                                OtherCropName = recommendation.Crops.OtherCropName,
                                CropTypeName = recommendation.Crops.CropTypeID == 140 ? NMP.Commons.Enums.CropTypes.GetName(typeof(CropTypes), recommendation.Crops.CropTypeID) : await _fieldLogic.FetchCropTypeById(recommendation.Crops.CropTypeID.Value),
                                IsSnsExist = (snsData.CropID != null && snsData.CropID > 0) ? true : false,
                                SnsAnalysisData = snsData,
                                SwardManagementName = recommendation.Crops.SwardManagementName,
                                EstablishmentName = recommendation.Crops.EstablishmentName,
                                SwardTypeName = recommendation.Crops.SwardTypeName,
                                DefoliationSequenceName = recommendation.Crops.DefoliationSequenceName,
                                CropGroupName = recommendation.Crops.CropGroupName,
                                SwardManagementID = recommendation.Crops.SwardManagementID,
                                Establishment = recommendation.Crops.Establishment,
                                SwardTypeID = recommendation.Crops.SwardTypeID,
                                DefoliationSequenceID = recommendation.Crops.DefoliationSequenceID,
                                PotentialCut = recommendation.Crops.PotentialCut
                            };
                            cropCounter++;
                            if (recommendation.Crops.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && !string.IsNullOrWhiteSpace(recommendation.Crops.DefoliationSequenceName))
                            {
                                List<string> defoliationList = recommendation.Crops.DefoliationSequenceName
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .ToList();
                                crop.DefoliationSequenceName = ShorthandDefoliationSequence(defoliationList);
                            }

                            if (!string.IsNullOrWhiteSpace(crop.CropTypeName))
                            {
                                crop.EncryptedCropTypeName = _cropDataProtector.Protect(crop.CropTypeName);
                            }

                            if (!string.IsNullOrWhiteSpace(crop.CropGroupName))
                            {
                                crop.EncryptedCropGroupName = _cropDataProtector.Protect(crop.CropGroupName);
                            }

                            if (!string.IsNullOrWhiteSpace(recommendation.Crops.CropOrder.ToString()))
                            {
                                crop.EncryptedCropOrder = _cropDataProtector.Protect(recommendation.Crops.CropOrder.ToString());
                            }

                            if (recommendation.Crops.CropInfo1 != null)
                            {
                                crop.CropInfo1Name = await _cropLogic.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(recommendation.Crops.CropTypeID.Value, recommendation.Crops.CropInfo1.Value);
                            }

                            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(recommendation.Crops.FieldID.Value)).Name;
                            if (!string.IsNullOrWhiteSpace(model.FieldName))
                            {
                                crop.EncryptedFieldName = _cropDataProtector.Protect(model.FieldName);
                            }

                            List<CropTypeResponse> cropTypeResponseList = (await _fieldLogic.FetchAllCropTypes());
                            if (cropTypeResponseList != null)
                            {
                                CropTypeResponse cropTypeResponse = cropTypeResponseList.Where(x => x.CropTypeId == crop.CropTypeID).FirstOrDefault();
                                if (cropTypeResponse != null)
                                {
                                    crop.CropGroupID = cropTypeResponse.CropGroupId;
                                }
                            }

                            if (recommendation.Crops.CropInfo2 != null && crop.CropGroupID == (int)NMP.Commons.Enums.CropGroup.Cereals)
                            {
                                crop.CropInfo2Name = await _cropLogic.FetchCropInfo2NameByCropInfo2Id(crop.CropInfo2.Value);
                            }

                            if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && crop.PotentialCut != null)
                            {
                                var potentialCuts = new[]
                                {
                                    Resource.lblOne.ToLower(), Resource.lblTwo.ToLower(), Resource.lblThree.ToLower(), Resource.lblFour.ToLower(),
                                    Resource.lblFive.ToLower(), Resource.lblSix.ToLower(), Resource.lblSeven.ToLower(), Resource.lblEight.ToLower(), Resource.lblNine.ToLower()
                                };

                                if (cropCounter == 1)
                                {
                                    (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                    if (error == null && defoliationSequence.DefoliationSequenceId != null)
                                    {
                                        if (defoliationSequence.DefoliationSequenceDescription.Contains(Resource.lblEstablishment))
                                        {
                                            ViewBag.GrassHeadingCropOne = string.Format(Resource.lblThereAreCountCutsAndGrazingsPlusEstablishment, potentialCuts[(int)crop.PotentialCut - 1]);
                                        }
                                        else
                                        {
                                            ViewBag.GrassHeadingCropOne = string.Format(Resource.lblThereAreCountCutsAndGrazings, potentialCuts[(int)crop.PotentialCut - 1]);
                                        }
                                    }

                                }
                                else if (cropCounter == 2)
                                {
                                    (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                    if (error == null && defoliationSequence.DefoliationSequenceId != null)
                                    {
                                        if (defoliationSequence.DefoliationSequenceDescription.Contains(Resource.lblEstablishment))
                                        {
                                            ViewBag.GrassHeadingCropTwo = string.Format(Resource.lblThereAreCountCutsAndGrazingsPlusEstablishment, potentialCuts[(int)crop.PotentialCut - 1]);
                                        }
                                        else
                                        {
                                            ViewBag.GrassHeadingCropTwo = string.Format(Resource.lblThereAreCountCutsAndGrazings, potentialCuts[(int)crop.PotentialCut - 1]);
                                        }
                                    }
                                }
                            }

                            model.Crops.Add(crop);
                            if (recommendation.PKBalance != null)
                            {
                                model.PKBalance = new PKBalance();
                                model.PKBalance.PBalance = recommendation.PKBalance.PBalance;
                                model.PKBalance.KBalance = recommendation.PKBalance.KBalance;
                            }

                            string defolicationName = string.Empty;
                            if (recommendation.Crops.SwardTypeID != null && recommendation.Crops.PotentialCut != null && recommendation.Crops.DefoliationSequenceID != null)
                            {
                                if ((string.IsNullOrWhiteSpace(defolicationName)) && recommendation.Crops.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                                {
                                    (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                                    if (error == null && defoliationSequence.DefoliationSequenceId != null)
                                    {
                                        defolicationName = defoliationSequence.DefoliationSequenceDescription;
                                    }
                                }
                            }

                            var defolicationParts = (!string.IsNullOrWhiteSpace(defolicationName)) ? defolicationName.Split(',') : null;
                            int defIndex = 0;
                            if (recommendation.RecommendationData.Count > 0)
                            {
                                foreach (var recData in recommendation.RecommendationData)
                                {
                                    string part = (defolicationParts != null && defIndex < defolicationParts.Length) ? defolicationParts[defIndex].Trim() : string.Empty;
                                    string defoliationSequenceName = (!string.IsNullOrWhiteSpace(part)) ? char.ToUpper(part[0]).ToString() + part.Substring(1) : string.Empty;
                                    var ManagementPeriods = new ManagementPeriodViewModel
                                    {
                                        ID = recData.ManagementPeriod.ID,
                                        CropID = recData.ManagementPeriod.CropID,
                                        Defoliation = recData.ManagementPeriod.Defoliation,
                                        DefoliationSequenceName = defoliationSequenceName,
                                        Utilisation1ID = recData.ManagementPeriod.Utilisation1ID,
                                        Utilisation2ID = recData.ManagementPeriod.Utilisation2ID,
                                        PloughedDown = recData.ManagementPeriod.PloughedDown
                                    };
                                    model.ManagementPeriods.Add(ManagementPeriods);

                                    defIndex++;
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
                                        SIndex = recData.Recommendation.SIndex,
                                        LimeIndex = recData.Recommendation.PH,
                                        KIndex = recData.Recommendation.KIndex != null ? (recData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (recData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : recData.Recommendation.KIndex)) : null,
                                        MgIndex = recData.Recommendation.MgIndex,
                                        PIndex = recData.Recommendation.PIndex,
                                        NaIndex = recData.Recommendation.NaIndex,
                                        NIndex = recData.Recommendation.NIndex,
                                        CreatedOn = recData.Recommendation.CreatedOn,
                                        ModifiedOn = recData.Recommendation.ModifiedOn,
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
                                    model.Recommendations.Add(rec);

                                    if (recData.RecommendationComments.Count > 0)
                                    {
                                        foreach (var item in recData.RecommendationComments)
                                        {
                                            var recCom = new RecommendationComment
                                            {
                                                ID = item.ID,
                                                RecommendationID = item.RecommendationID,
                                                Nutrient = item.Nutrient,
                                                Comment = item.Comment
                                            };
                                            model.RecommendationComments.Add(recCom);
                                        }
                                    }

                                    if (recData.OrganicManures.Count > 0)
                                    {
                                        foreach (var item in recData.OrganicManures)
                                        {
                                            (ManureType? manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(item.ManureTypeID);
                                            if (error == null && manureType != null)
                                            {
                                                var orgManure = new OrganicManureDataViewModel
                                                {
                                                    ID = item.ID,
                                                    ManureTypeName = item.ManureTypeName,
                                                    ApplicationMethodName = item.ApplicationMethodName,
                                                    ApplicationDate = item.ApplicationDate,
                                                    ApplicationRate = item.ApplicationRate,
                                                    EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
                                                    EncryptedFieldName = _cropDataProtector.Protect(model.FieldName),
                                                    EncryptedManureTypeName = _cropDataProtector.Protect(item.ManureTypeName),
                                                    RateUnit = manureType.IsLiquid.Value ? Resource.lblCubicMeters : Resource.lbltonnes
                                                };

                                                model.OrganicManures.Add(orgManure);
                                            }
                                            else
                                            {
                                                return RedirectToAction(_harvestYearOverviewActionName, new
                                                {
                                                    id = q,
                                                    year = s
                                                });
                                            }
                                        }

                                        ViewBag.OrganicManure = _cropDataProtector.Protect(Resource.lblOrganic);
                                        model.OrganicManures = model.OrganicManures.OrderByDescending(x => x.ApplicationDate).ToList();
                                    }
                                    if (recData.FertiliserManures.Count > 0)
                                    {
                                        foreach (var item in recData.FertiliserManures)
                                        {
                                            var fertiliserManure = new FertiliserManureDataViewModel
                                            {
                                                ID = item.ID,
                                                ManagementPeriodID = item.ManagementPeriodID,
                                                ApplicationDate = item.ApplicationDate,
                                                ApplicationRate = item.ApplicationRate,
                                                Confirm = item.Confirm,
                                                N = item.N,
                                                P2O5 = item.P2O5,
                                                K2O = item.K2O,
                                                MgO = item.MgO,
                                                SO3 = item.SO3,
                                                Na2O = item.Na2O,
                                                Lime = item.Lime,
                                                NH4N = item.NH4N,
                                                NO3N = item.NO3N,
                                                EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
                                                EncryptedFieldName = _cropDataProtector.Protect(model.FieldName)
                                            };

                                            ViewBag.Fertiliser = _cropDataProtector.Protect(Resource.lblFertiliser);
                                            model.FertiliserManures.Add(fertiliserManure);
                                        }

                                        model.FertiliserManures = model.FertiliserManures.OrderByDescending(x => x.ApplicationDate).ToList();
                                    }
                                }
                            }
                        }

                        (List<NutrientResponseWrapper> nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
                        if (error == null && nutrients.Count > 0)
                        {
                            model.Nutrients = new List<NutrientResponseWrapper>();
                            model.Nutrients = nutrients;
                        }

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
                            if (string.IsNullOrWhiteSpace(error.Message))
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

    private async Task<(List<int>, List<int>)> FetchAllowedFieldsForSecondCrop(List<HarvestYearPlanResponse> harvestYearPlanResponse, int harvestYear, int cropTypeId, bool isUpdate = false, List<Crop>? updatedCrop = null)
    {
        List<int> secondCropList = new List<int>();
        List<int> fieldRemoveList = new List<int>();
        List<int> fieldsAllowedForSecondCrop = new List<int>();
        if (updatedCrop != null)
        {
            foreach (var crop in updatedCrop)
            {
                if (crop.CropOrder == 2)
                {
                    List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(crop.FieldID.Value);
                    int cropPlanCount = cropsResponse.Count(x => x.Year == harvestYear && !x.Confirm);
                    int firstCropType = cropsResponse.Where(x => x.Year == harvestYear && !x.Confirm && x.CropOrder == 1).Select(c => c.CropTypeID.Value).FirstOrDefault();
                    if (isUpdate && cropPlanCount == 2)
                    {
                        secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropType);
                        if (secondCropList.Count > 0)
                        {
                            if (secondCropList.Any(x => x == cropTypeId))
                            {
                                continue;
                            }
                            else
                            {
                                fieldRemoveList.Add(crop.FieldID.Value);
                                continue;
                            }
                        }
                        else
                        {
                            fieldRemoveList.Add(crop.FieldID.Value);
                            continue;
                        }
                    }
                }
            }
        }

        foreach (var firstCropPlans in harvestYearPlanResponse)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(firstCropPlans.FieldID);
            int cropPlanCount = cropsResponse.Count(x => x.Year == harvestYear && x.Confirm == false);
            int firstCropType = cropsResponse.FirstOrDefault(x => x.Year == harvestYear && x.Confirm == false && x.CropOrder == 1).CropTypeID.Value;
            if (cropPlanCount == 1)
            {
                secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID);
                if (secondCropList.Count > 0)
                {
                    foreach (int secondCropTypeId in secondCropList)
                    {
                        if (secondCropTypeId == cropTypeId)
                        {
                            fieldsAllowedForSecondCrop.Add(firstCropPlans.FieldID);
                        }
                    }
                }
            }
        }

        return (fieldsAllowedForSecondCrop, fieldRemoveList);
    }

    private async Task<bool> IsSecondCropAllowed(List<CropDetailResponse> CropDetailResponse)
    {
        List<int> secondCropList = new List<int>();
        bool isSecondCropAllowed = false;
        foreach (var firstCropPlans in CropDetailResponse)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(firstCropPlans.FieldID);
            secondCropList = await _cropLogic.FetchSecondCropListByFirstCropId(firstCropPlans.CropTypeID);
            if (secondCropList.Count > 0)
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

        if (!string.IsNullOrWhiteSpace(q) && model != null)
        {
            string decrypt = _cropDataProtector.Unprotect(q);
            if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblField)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortOrganicListOrderByDate = null;
                    model.SortOrganicListOrderByCropType = null;
                }
            }
            else if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblDate)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortOrganicListOrderByFieldName = null;
                    model.SortOrganicListOrderByCropType = null;
                }
            }
            else if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblCropType)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortOrganicListOrderByFieldName = null;
                    model.SortOrganicListOrderByDate = null;
                }
            }
        }

        SetCropToSession(model);
        return Redirect(Url.Action(_harvestYearOverviewActionName, new { year = year, id = id, s = q, t = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationsForSorting), u = r }) + Resource.lblOrganicMaterialApplicationsForSorting);
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

        if (!string.IsNullOrWhiteSpace(q) && model != null)
        {
            string decrypt = _cropDataProtector.Unprotect(q);
            if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblField)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortInOrganicListOrderByDate = null;
                    model.SortInOrganicListOrderByCropType = null;
                }
            }
            else if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblDate)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortInOrganicListOrderByFieldName = null;
                    model.SortInOrganicListOrderByCropType = null;
                }
            }
            else if (!string.IsNullOrWhiteSpace(decrypt) && decrypt == Resource.lblCropType)
            {
                if (!string.IsNullOrWhiteSpace(r))
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
                    model.SortInOrganicListOrderByFieldName = null;
                    model.SortInOrganicListOrderByDate = null;
                }
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
            Error error = null;
            bool isThereAnyPreviousFieldLeft = false;
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                (List<HarvestYearPlanResponse> crops, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmId);
                if (string.IsNullOrWhiteSpace(error.Message) && crops.Count > 0)
                {
                    List<string> fieldIds = crops.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID.ToString()).ToList();
                    if (fieldIds.Count > 0 && fieldIds.Any(fieldId => model.FieldList.Contains(fieldId)))
                    {
                        isThereAnyPreviousFieldLeft = true;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                if (string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) || (!isThereAnyPreviousFieldLeft) || (model.IsFieldToBeRemoved.HasValue && model.IsFieldToBeRemoved.Value))
                {
                    (List<HarvestYearPlanResponse> harvestYearPlanResponses, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                    if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponses.Count > 0)
                    {
                        bool cropGroupNameExists = harvestYearPlanResponses
                           .Any(harvest =>
                           !string.IsNullOrEmpty(harvest.CropGroupName) && harvest.CropGroupName.Equals(model.CropGroupName)
                           && harvest.Year == model.Year);

                        if (cropGroupNameExists)
                        {
                            ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                            return View(model);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.Crops != null && model.Crops.Count > 0)
                {
                    string cropIds = string.Join(",", model.Crops.Select(x => x.ID));
                    (bool groupNameExist, error) = await _cropLogic.IsCropsGroupNameExistForUpdate(cropIds, model.CropGroupName, model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                    if (string.IsNullOrWhiteSpace(error.Message) && groupNameExist)
                    {
                        ModelState.AddModelError("CropGroupName", Resource.lblThisCropGroupNameAlreadyExists);
                        return View(model);
                    }
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
            ViewBag.EncryptedCropType = q;
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.EncryptedCropGroupName = r;
            }

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Crop Controller : Exception in RemoveCrop() action : {Message} : {StackTrace}", ex.Message, ex.StackTrace);
            if (string.IsNullOrWhiteSpace(s) || (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value))))
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    var fieldIds = model.Crops.Select(c => c.FieldID).Distinct();
                    fieldList = fieldList.Where(x => fieldIds.Contains(x.ID)).ToList();
                }

                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error harvestYearError) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);
                if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate))
                {
                    var fieldIdsForFilter = fieldList.Select(f => f.ID);
                    harvestYearPlanResponse = harvestYearPlanResponse
                        .Where(x => fieldIdsForFilter.Contains(x.FieldID))
                        .ToList();
                }

                if (string.IsNullOrWhiteSpace(harvestYearError.Message))
                {
                    //Fetch fields allowed for second crop based on first crop                        
                    var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
                    List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                        .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))
                        ).ToList();

                    (List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);
                    if (harvestYearPlanResponse.Count() > 0 || fieldsAllowedForSecondCrop.Count() > 0)
                    {
                        var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString()) || fieldsAllowedForSecondCrop.Contains(x.ID ?? 0)).ToList();
                    }
                }
                else
                {
                    TempData["ErrorCreatePlan"] = harvestYearError.Message;
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
                    return RedirectToAction(_checkAnswerActionName);
                }
                string? cropInfoOneQuestion = string.Empty;
                if (model.CropTypeID != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other)
                {
                    cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
                    if (!string.IsNullOrWhiteSpace(cropInfoOneQuestion))
                    {
                        ViewBag.CropInfoOneQuestion = (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions || model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions) ? string.Format(cropInfoOneQuestion, model.CropType) : cropInfoOneQuestion;
                    }
                }

                TempData["ErrorCreatePlan"] = ex.Message;
                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();
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
            if (model.RemoveCrop == null)
            {
                ModelState.AddModelError("RemoveCrop", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("RemoveCrop", model);
            }
            if (!model.RemoveCrop.Value)
            {
                if (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value)
                {
                    return RedirectToAction("Recommendations", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            else
            {
                Error error = new Error();
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                {
                    if (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value)))
                    {
                        harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    }
                    else
                    {
                        harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.FieldID == model.FieldID &&
                        x.Year == model.Year && x.CropOrder == model.CropOrder.Value).ToList();
                    }
                    if (harvestYearPlanResponse.Count > 0)
                    {
                        List<int> cropIds = harvestYearPlanResponse.Select(x => x.CropID).ToList();
                        (string message, error) = await _cropLogic.RemoveCropPlan(cropIds);
                        if (!string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["RemoveGroupError"] = error.Message;
                            return View(model);
                        }
                        else
                        {
                            (harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                            if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                            {
                                return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value))) ? _cropDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) : _cropDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                            }
                            else
                            {
                                List<PlanSummaryResponse> planSummaryResponse = await _cropLogic.FetchPlanSummaryByFarmId(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), 0);
                                if (planSummaryResponse != null && planSummaryResponse.Count > 0)
                                {
                                    return RedirectToAction(_plansAndRecordsOverviewActionName, "Crop", new { id = model.EncryptedFarmId, year = _farmDataProtector.Protect(model.Year.ToString()), q = (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value))) ? _cropDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) : _cropDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                                }
                                else
                                {
                                    return RedirectToAction(_farmSummaryActionName, "Farm", new { id = model.EncryptedFarmId, q = _farmDataProtector.Protect(Resource.lblTrue), r = (model.IsComingFromRecommendation == null || (model.IsComingFromRecommendation.HasValue && (!model.IsComingFromRecommendation.Value))) ? _farmDataProtector.Protect(string.Format(Resource.MsgCropGroupNameRemoves, model.CropGroupName)) : _farmDataProtector.Protect(string.Format(Resource.lblCropTypeNameRemoveFromFieldName, model.CropType, model.FieldName)) });
                                }
                            }
                        }
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

    [HttpGet]
    public IActionResult UpdateExcessWinterRainfall()
    {
        _logger.LogTrace("Crop Controller : UpdateExcessWinterRainfall() action called");
        PlanViewModel model = GetHarvestYearPlanFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : session not found in UpdateExcessWinterRainfall() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in UpdateExcessWinterRainfall() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;
            return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateExcessWinterRainfall(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : UpdateExcessWinterRainfall() post action called");
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
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                if (excessWinterRainfallOption.Count > 0)
                {
                    var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Name
                    }).ToList();

                    ViewBag.ExcessRainFallOptions = SelectListItem;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfall() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["UpdateExcessWinterRainfallError"] = ex.Message;
            return RedirectToAction("UpdateExcessWinterRainfall");
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
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (excessWinterRainfallOption.Count > 0)
                    {
                        var SelectListItem = excessWinterRainfallOption.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name
                        }).ToList();

                        ViewBag.ExcessRainFallOptions = SelectListItem;
                    }
                }
                return View(model);
            }
            SetHarvestYearPlanToSession(model);
            return RedirectToAction("ExcessWinterRainfallCheckAnswer", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfall() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessWinterRainfallError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExcessWinterRainfallCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : ExcessWinterRainfallCheckAnswer() action called");
        PlanViewModel model = GetHarvestYearPlanFromSession();

        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : session not found in ExcessWinterRainfallCheckAnswer() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (CommonResponse commonResponse, Error error) = await _farmLogic.FetchExcessWinterRainfallOptionByIdAsync(model.ExcessWinterRainfallId.Value);
            if (string.IsNullOrWhiteSpace(error.Message) && commonResponse != null)
            {
                model.ExcessWinterRainfallName = commonResponse.Name;
                model.ExcessWinterRainfallValue = commonResponse.Value;
            }
            model.IsExcessWinterRainfallCheckAnswer = true;
            SetHarvestYearPlanToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfallCheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessWinterRainfallError"] = ex.Message;
            return RedirectToAction("ExcessWinterRainfall", model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcessWinterRainfallCheckAnswer(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : ExcessWinterRainfallCheckAnswer() action called");

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
                _logger.LogTrace("Crop Controller :HarvestYearPlan session not found in ExcessWinterRainfallCheckAnswer() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
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
            (ExcessRainfalls excessRainfall, Error error) = await _farmLogic.AddExcessWinterRainfallAsync(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), model.Year.Value, jsonData, model.IsExcessWinterRainfallUpdated.Value);
            if (string.IsNullOrWhiteSpace(error.Message) && excessRainfall != null)
            {
                return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(string.Format(Resource.MsgAddExcessWinterRainfallContentOne, model.Year.Value)), v = _cropDataProtector.Protect(string.Format(Resource.MsgAddExcessWinterRainfallContentSecond, model.Year.Value)) });
            }
            else
            {
                TempData["ExcessWinterRainfallCheckAnswerError"] = error.Message;
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in ExcessWinterRainfallCheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ExcessWinterRainfallCheckAnswerError"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> UpdateCropGroupNameCheckAnswer(string? q, string? r, string? t, string? u, string? s)
    {
        _logger.LogTrace("Crop Controller : UpdateCropGroupNameCheckAnswer() action called");
        PlanViewModel model = GetCropFromSession() ?? new PlanViewModel();

        try
        {
            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r))
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                model.CropType = _cropDataProtector.Unprotect(q);

            }

            if (!string.IsNullOrWhiteSpace(r))
            {
                model.CropGroupName = _cropDataProtector.Unprotect(r);
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
            Error error = new Error();
            bool allYieldsAreSame = true;
            bool allSowingAreSame = true;
            string? yieldQuestion = null;
            string? sowingQuestion = null;
            bool isBasePlan = false;
            decimal? firstYield = null;
            DateTime? firstSowingDate = null;
            if (string.IsNullOrWhiteSpace(s))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                {
                    harvestYearPlanResponse = harvestYearPlanResponse.Where(x => x.CropGroupName == model.CropGroupName).ToList();
                    if (harvestYearPlanResponse != null)
                    {
                        model.Crops = new List<Crop>();

                        var cropTypeId = model.Crops.FirstOrDefault()?.CropTypeID;

                        decimal? defaultYield = cropTypeId.HasValue
                            ? await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(cropTypeId.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland) : null;

                        for (int i = 0; i < harvestYearPlanResponse.Count; i++)
                        {
                            var crop = new Crop();
                            crop.FieldID = harvestYearPlanResponse[i].FieldID;
                            crop.FieldName = harvestYearPlanResponse[i].FieldName;
                            crop.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
                            if (decimal.TryParse(harvestYearPlanResponse[i].Yield, out decimal yield))
                            {
                                crop.Yield = yield;
                                model.Yield = yield;
                                yieldQuestion = yield == defaultYield ? string.Format(Resource.lblUseTheStandardFigure, defaultYield) : null;
                                if (string.IsNullOrWhiteSpace(yieldQuestion))
                                {
                                    if (firstYield == null)
                                    {
                                        firstYield = yield;
                                    }
                                    else if (firstYield != yield)
                                    {
                                        model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField;
                                        allYieldsAreSame = false;
                                    }
                                }
                                if (yieldQuestion == string.Format(Resource.lblUseTheStandardFigure, defaultYield))
                                {
                                    model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.UseTheStandardFigureForAllTheseFields;
                                }
                            }
                            else
                            {
                                crop.Yield = null;
                            }
                            if (harvestYearPlanResponse[i].IsBasePlan != null && harvestYearPlanResponse[i].IsBasePlan.Value)
                            {
                                isBasePlan = true;
                            }

                            if (harvestYearPlanResponse[i].SowingDate == null)
                            {
                                sowingQuestion = Resource.lblNoIWillEnterTheDateLater;
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
                                crop.SowingDate = harvestYearPlanResponse[i].SowingDate;

                            }
                            crop.ID = harvestYearPlanResponse[i].CropID;
                            crop.CropInfo1 = harvestYearPlanResponse[i].CropInfo1;
                            model.Crops.Add(crop);
                        }

                        if (model.Crops != null && model.Crops.All(x => x.Yield != null) && allYieldsAreSame && harvestYearPlanResponse.Count > 1)
                        {
                            model.YieldQuestion = (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields;
                            ViewBag.allYieldsAreSame = allYieldsAreSame;
                        }
                        if (model.Crops != null && model.Crops.All(x => x.SowingDate != null) && allSowingAreSame && harvestYearPlanResponse.Count > 1)
                        {
                            model.SowingDateQuestion = (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields;
                            ViewBag.allSowingAreSame = allSowingAreSame;
                        }
                        model.CropInfo1 = harvestYearPlanResponse.FirstOrDefault().CropInfo1;
                        model.CropInfo2 = harvestYearPlanResponse.FirstOrDefault().CropInfo2;
                        ViewBag.SowingQuestion = sowingQuestion;
                        ViewBag.YieldQuestion = yieldQuestion;
                        model.CropTypeID = harvestYearPlanResponse.FirstOrDefault().CropTypeID;
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
                        model.CropType = harvestYearPlanResponse.FirstOrDefault().CropTypeName;
                        model.Variety = harvestYearPlanResponse.FirstOrDefault().CropVariety;
                        model.CropGroupName = harvestYearPlanResponse.FirstOrDefault().CropGroupName;
                        model.PreviousCropGroupName = model.CropGroupName;
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
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.CropGroupName))
                {
                    model.CropGroupName = model.PreviousCropGroupName;
                }

                for (int i = 0; i < model.Crops.Count; i++)
                {
                    if (model.Crops[i].IsBasePlan)
                    {
                        isBasePlan = true;
                    }

                    var cropTypeId = model.Crops.FirstOrDefault()?.CropTypeID;

                    decimal? defaultYield = cropTypeId.HasValue
                        ? await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(cropTypeId.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland) : null;

                    yieldQuestion = model.Crops[i].Yield == defaultYield ? string.Format(Resource.lblUseTheStandardFigure, defaultYield) : null;
                    if (string.IsNullOrWhiteSpace(yieldQuestion))
                    {
                        if (firstYield == null)
                        {
                            firstYield = model.Crops[i].Yield;
                        }
                        else if (firstYield != model.Crops[i].Yield)
                        {
                            allYieldsAreSame = false;
                        }
                    }

                    if (model.Crops[i].SowingDate == null)
                    {
                        sowingQuestion = Resource.lblNoIWillEnterTheDateLater;
                    }
                    else
                    {
                        if (firstSowingDate == null)
                        {
                            firstSowingDate = model.Crops[i].SowingDate;
                        }
                        else if (firstSowingDate != model.Crops[i].SowingDate)
                        {
                            allSowingAreSame = false;
                        }
                    }
                }

                if (model.Crops != null && model.Crops.All(x => x.Yield != null) && allYieldsAreSame && model.Crops.Count > 1)
                {
                    ViewBag.allYieldsAreSame = allYieldsAreSame;
                }

                if (model.Crops != null && model.Crops.All(x => x.Yield != null) && allSowingAreSame && model.Crops.Count > 1)
                {
                    ViewBag.allSowingAreSame = allSowingAreSame;
                }

                ViewBag.SowingQuestion = sowingQuestion;
                ViewBag.YieldQuestion = yieldQuestion;
            }

            ViewBag.isBasePlan = isBasePlan;
            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            model.EncryptedIsCropUpdate = _cropDataProtector.Protect(Resource.lblTrue);
            string? cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
            if (!string.IsNullOrWhiteSpace(cropInfoOneQuestion))
            {
                ViewBag.CropInfoOneQuestion = (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions || model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions) ? string.Format(cropInfoOneQuestion, model.CropType) : cropInfoOneQuestion;
            }
            if (!string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                ViewBag.EncryptedCropGroupName = _cropDataProtector.Protect(model.CropGroupName);
            }

            if (!string.IsNullOrWhiteSpace(model.CropType))
            {
                ViewBag.EncryptedCropTypeId = _cropDataProtector.Protect(model.CropType);
            }

            SetCropToSession(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in UpdateCropGroupNameCheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["ErrorOnHarvestYearOverview"] = ex.Message;
            return RedirectToAction(_harvestYearOverviewActionName, new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCrop(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : UpdateCrop() post action called");
        try
        {
            Error error = null;
            int i = 0;
            int otherGroupId = (int)NMP.Commons.Enums.CropGroup.Other;
            int cerealsGroupId = (int)NMP.Commons.Enums.CropGroup.Cereals;
            int potatoesGroupId = (int)NMP.Commons.Enums.CropGroup.Potatoes;
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
            foreach (var crop in model.Crops)
            {
                if (crop.SowingDate == null)
                {
                    if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveASingleDateForAllTheseFields)
                    {
                        ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingSingleDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                        break;
                    }
                    else if (model.SowingDateQuestion == (int)NMP.Commons.Enums.SowingDateQuestion.YesIHaveDifferentDatesForEachOfTheseFields)
                    {
                        ModelState.AddModelError(string.Concat("Crops[", i, "].SowingDate"), string.Format(Resource.lblSowingDiffrentDateNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                    }
                }
                i++;
            }
            i = 0;
            if (model.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                foreach (var crop in model.Crops)
                {

                    if (crop.Yield == null)
                    {
                        if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                        {
                            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                            if (defaultYield == 0)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheExpectedYieldForSingleNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
                            }
                            break;
                        }
                        else if (model.YieldQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                        {
                            decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID.Value, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                            if (defaultYield == 0)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheDifferentExpectedYieldNotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType, crop.FieldName));
                            }
                        }
                    }
                }
                i++;
            }

            if (model.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", Resource.MsgMainCropTypeNotSet);
            }

            string? cropInfoOneQuestion = string.Empty;
            if (model.CropTypeID != null && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Other && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                cropInfoOneQuestion = await _cropLogic.FetchCropInfoOneQuestionByCropTypeId(model.CropTypeID ?? 0);
                if (!string.IsNullOrWhiteSpace(cropInfoOneQuestion))
                {
                    ViewBag.CropInfoOneQuestion = (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.BulbOnions || model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.SaladOnions) ? string.Format(cropInfoOneQuestion, model.CropType) : cropInfoOneQuestion;
                }
            }

            if (model.CropInfo1 == null && model.CropGroupId != otherGroupId && model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
            {
                ModelState.AddModelError("CropInfo1", string.Format("{0} {1}", cropInfoOneQuestion, Resource.lblNotSet.ToLower()));
            }

            if (model.CropInfo2 == null && model.CropGroupId == cerealsGroupId)
            {
                ModelState.AddModelError("CropInfo2", string.Format(Resource.MsgCropInfo2NotSet, model.CropGroupId == otherGroupId ? model.OtherCropName : model.CropType));
            }

            if (model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                if (model.SwardManagementId == null)
                {
                    ModelState.AddModelError("SwardManagementId", string.Format("{0} {1}", string.Format(Resource.lblHowWillTheseFieldsBeManaged, (model.FieldList.Count == 1 ? model.Crops[0].FieldName : Resource.lblTheseFields)), Resource.lblNotSet));
                }
                if (model.PotentialCut == null)
                {
                    string fieldText = (model.FieldList?.Count == 1)
                           ? model.Crops[0].FieldName
                           : Resource.lblTheseFields;

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

                if ((model.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass))
                {
                    foreach (var crop in model.Crops)
                    {
                        if (crop.Yield == null)
                        {
                            if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForFields, crop.Year));

                                break;
                            }
                            else if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
                            {
                                ModelState.AddModelError(string.Concat("Crops[", i, "].Yield"), string.Format(Resource.lblWhatIsTheTotalTargetDryMatterYieldForField, crop.FieldName, crop.Year));

                            }
                        }
                        i++;
                    }
                }
                TempData["ModelStateErrorForGrass"] = true;
            }
            if (!ModelState.IsValid)
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                List<Field> fieldList = await _fieldLogic.FetchFieldsByFarmId(farmID);
                if (model.CropTypeID != null)
                {
                    decimal defaultYield = await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(model.CropTypeID ?? 0, model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland);
                    if (defaultYield > 0)
                    {
                        ViewBag.DefaultYield = defaultYield;
                    }
                }
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error harvestYearError) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year ?? 0, farmID);
                if (string.IsNullOrWhiteSpace(harvestYearError.Message))
                {

                    var grassTypeId = (int)NMP.Commons.Enums.CropTypes.Grass;
                    List<HarvestYearPlanResponse> cropPlanForFirstCropFilter = harvestYearPlanResponse
                        .Where(x => (x.IsBasePlan != null && (!x.IsBasePlan.Value))
                        ).ToList();

                    (List<int> fieldsAllowedForSecondCrop, List<int> fieldRemoveList) = await FetchAllowedFieldsForSecondCrop(cropPlanForFirstCropFilter, model.Year ?? 0, model.CropTypeID ?? 0, string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) ? false : true, model.Crops);

                    if (harvestYearPlanResponse.Count() > 0 || fieldsAllowedForSecondCrop.Count() > 0)
                    {
                        var harvestFieldIds = harvestYearPlanResponse.Select(x => x.FieldID.ToString()).ToList();
                        fieldList = fieldList.Where(x => !harvestFieldIds.Contains(x.ID.ToString()) || fieldsAllowedForSecondCrop.Contains(x.ID ?? 0)).ToList();
                    }
                }
                else
                {
                    TempData["ErrorCreatePlan"] = harvestYearError.Message;
                    ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();

                    return RedirectToAction(_checkAnswerActionName);
                }

                ViewBag.FieldOptions = harvestYearPlanResponse.Where(x => x.CropGroupName == model.PreviousCropGroupName).Select(x => x.FieldID).ToList();

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

                if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
                {
                    model.IsCropTypeChange = false;
                    model.IsCropGroupChange = false;
                    (List<SwardTypeResponse> swardTypeResponses, error) = await _cropLogic.FetchSwardTypes();
                    if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["SowingDateError"] = error.Message;
                        return RedirectToAction("SowingDate");
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

                    if (model.SwardManagementId != null)
                    {
                        (SwardManagementResponse swardManagementResponse, error) = await _cropLogic.FetchSwardManagementBySwardManagementId(model.SwardManagementId ?? 0);
                        if (error != null)
                        {
                            TempData["SwardManagementError"] = error.Message;
                            return RedirectToAction("SwardType");
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

                    if (model.DefoliationSequenceId != null)
                    {
                        (DefoliationSequenceResponse defoliationSequenceResponse, error) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId ?? 0);
                        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["DefoliationSequenceError"] = error.Message;
                            return RedirectToAction("Defoliation");
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

                    List<GrassSeasonResponse> grassSeasons = await _cropLogic.FetchGrassSeasons();
                    grassSeasons.RemoveAll(g => g.SeasonId == 0);
                    model.GrassSeasonName = grassSeasons.Where(x => x.SeasonId == model.GrassSeason).Select(x => x.SeasonName).SingleOrDefault();
                }

                ViewData["ModelStateErrors"] = ModelState;
                return View(_checkAnswerActionName, model);
            }

            int? lastGroupNumber = null;

            if (string.IsNullOrWhiteSpace(model.CropGroupName))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));
                if (harvestYearPlanResponse != null && (error != null && !string.IsNullOrWhiteSpace(error.Message)))
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
            if (model.Crops != null && model.Crops.Count > 0)
            {
                string cropIds = string.Join(",", model.Crops.Select(x => x.ID));
                if (string.IsNullOrWhiteSpace(model.Variety))
                {
                    model.Variety = null;
                }


                List<CropData> cropEntries = new List<CropData>();
                foreach (Crop crop in model.Crops)
                {
                    crop.IsBasePlan = false;
                    if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
                    {
                        crop.CropTypeID = model.CropTypeID;
                        crop.CropInfo1 = model.CropInfo1;
                        crop.CropInfo2 = model.CropInfo2;
                        crop.DefoliationSequenceID = model.DefoliationSequenceId;
                        crop.SwardTypeID = model.SwardTypeId;
                        crop.SwardManagementID = model.SwardManagementId;
                        crop.PotentialCut = model.PotentialCut;
                        crop.Establishment = model.GrassSeason ?? 0;
                    }
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

                    List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
                    List<ManagementPeriod> managementPeriodList = new List<ManagementPeriod>();
                    if (crop.ID != null)
                    {
                        (managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
                    }
                    if (model.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass)
                    {
                        managementPeriods.Add(new ManagementPeriod
                        {
                            Defoliation = 1,
                            Utilisation1ID = 2
                        });
                        if (managementPeriodList != null && managementPeriodList.Any())
                        {
                            if (crop.ID != null)
                            {
                                foreach (var managementPeriod in managementPeriods)
                                {
                                    managementPeriod.ModifiedOn = DateTime.Now;
                                    managementPeriod.ModifiedByID = userId;
                                }
                            }
                        }
                    }
                    if (model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass)
                    {

                        string defoliationSequence = "";
                        (DefoliationSequenceResponse defoliationSequenceResponse, error) = await _cropLogic.FetchDefoliationSequencesById(model.DefoliationSequenceId.Value);
                        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["ErrorCreatePlan"] = error.Message;
                            return RedirectToAction(_checkAnswerActionName);
                        }
                        else
                        {
                            defoliationSequence = defoliationSequenceResponse.DefoliationSequence;
                        }
                        int defoliation = 1;
                        int utilisation1 = 0;
                        if (defoliationSequence != null)
                        {
                            foreach (char c in defoliationSequence)
                            {
                                if (c == 'E')
                                {
                                    utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Establishment;
                                }
                                else if (c == 'G')
                                {
                                    utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Grazing;
                                }
                                else if (c == 'S')
                                {
                                    utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Silage;
                                }
                                else if (c == 'H')
                                {
                                    utilisation1 = (int)NMP.Commons.Enums.Utilisation1.Hay;
                                }
                                managementPeriods.Add(new ManagementPeriod
                                {
                                    Defoliation = defoliation,
                                    Utilisation1ID = utilisation1,
                                    Yield = crop.Yield ?? 0 / model.PotentialCut
                                });
                                if (managementPeriodList != null && managementPeriodList.Any())
                                {
                                    if (crop.ID != null)
                                    {
                                        foreach (var managementPeriod in managementPeriods)
                                        {
                                            managementPeriod.ModifiedOn = DateTime.Now;
                                            managementPeriod.ModifiedByID = userId;
                                        }
                                    }
                                }
                                defoliation++;
                            }
                        }
                    }
                    if (error == null || string.IsNullOrWhiteSpace(error.Message))
                    {
                        crop.FieldName = null;
                        crop.EncryptedCounter = null;
                        crop.FieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;
                        //crop.CropOrder = 1;
                        CropData cropEntry = new CropData
                        {
                            Crop = crop,
                            ManagementPeriods = managementPeriods
                        };
                        cropEntries.Add(cropEntry);
                    }
                }
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                (List<HarvestYearPlanResponse> harvestYearPlanResponseForFilter, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, farmId);
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponseForFilter.Count > 0 && string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    //filter the plan list based on the crop group
                    harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                    if (harvestYearPlanResponseForFilter != null)
                    {
                        var fieldIds = harvestYearPlanResponseForFilter.Select(x => x.FieldID).ToList();

                        //Get the fields that we unchecked.
                        var removableFields = harvestYearPlanResponseForFilter.Where(f => !model.FieldList.Contains(f.FieldID.ToString())).ToList();
                        if (removableFields != null && removableFields.Count > 0)
                        {
                            foreach (var field in removableFields)
                            {
                                Crop crop = new Crop
                                {
                                    ID = harvestYearPlanResponseForFilter.Where(x => x.FieldID == field.FieldID).Select(x => x.CropID).FirstOrDefault(),
                                    FieldID = harvestYearPlanResponseForFilter.Where(x => x.FieldID == field.FieldID).Select(x => x.FieldID).FirstOrDefault(),
                                    FieldType = 1,
                                    IsBasePlan = false,
                                    Year = harvestYearPlanResponseForFilter.Where(x => x.FieldID == field.FieldID).Select(x => x.Year).FirstOrDefault(),
                                    Confirm = false,
                                    CropOrder = harvestYearPlanResponseForFilter.Where(x => x.FieldID == field.FieldID).Select(x => x.CropOrder).FirstOrDefault(),
                                    IsDeleted = true
                                };
                                List<ManagementPeriod> managementPeriods = new List<ManagementPeriod>();
                                managementPeriods.Add(new ManagementPeriod
                                {
                                    CropID = harvestYearPlanResponseForFilter.Where(x => x.FieldID == field.FieldID).Select(x => x.CropID).FirstOrDefault(),
                                    Defoliation = 1,
                                    Utilisation1ID = 2
                                });
                                CropData cropEntry = new CropData
                                {
                                    Crop = crop,
                                    ManagementPeriods = managementPeriods
                                };
                                cropEntries.Add(cropEntry);
                            }
                        }
                    }
                }
                CropDataWrapper cropDataWrapper = new CropDataWrapper
                {
                    Crops = cropEntries
                };
                string jsonData = JsonConvert.SerializeObject(cropDataWrapper);
                (bool success, error) = await _cropLogic.MergeCrop(jsonData);
                if (string.IsNullOrWhiteSpace(error.Message) && success)
                {
                    model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                    RemoveCropSession();
                    if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && model.IsComingFromRecommendation == null)
                    {
                        return RedirectToAction(_harvestYearOverviewActionName, new
                        {
                            id = model.EncryptedFarmId,
                            year = model.EncryptedHarvestYear,
                            q = Resource.lblTrue,
                            r = _cropDataProtector.Protect(Resource.lblCropPlanUpdated),
                            v = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations)
                        });
                    }
                    else if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value))
                    {
                        return RedirectToAction("Recommendations", new
                        {
                            q = model.EncryptedFarmId,
                            r = model.EncryptedFieldId,
                            s = model.EncryptedHarvestYear,
                            t = _cropDataProtector.Protect(Resource.lblCropPlanUpdated)
                            //u = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations)
                        });
                    }
                }
                else
                {
                    TempData["ErrorCreatePlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater; //error.Message; //
                    return RedirectToAction(_checkAnswerActionName);
                }
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


    [HttpGet]
    public async Task<IActionResult> CurrentSward()
    {
        _logger.LogTrace("Crop Controller : CurrentSward() action called");
        PlanViewModel model = new PlanViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("CropData"))
            {
                model = HttpContext.Session.GetObjectFromJson<PlanViewModel>("CropData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
                if (planViewModel.CurrentSward != model.CurrentSward)
                {
                    model.IsCurrentSwardChange = true;
                }
            }

            if (model.IsCurrentSwardChange && (!model.IsCropTypeChange && !model.IsCropGroupChange))
            {
                (List<DefoliationSequenceResponse> defoliationSequenceResponses, Error error) = await _cropLogic.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(model.SwardTypeId.Value, model.SwardManagementId ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? model.PotentialCut.Value + 1 : model.PotentialCut ?? 0, model.CurrentSward == (int)NMP.Commons.Enums.CurrentSward.NewSward ? true : false);
                if (error == null)
                {
                    ViewBag.DefoliationSequenceResponses = defoliationSequenceResponses;
                    if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly)
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
                        }
                    }
                    else if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly)
                    {
                        model.DefoliationSequenceId = defoliationSequenceResponses[0].DefoliationSequenceId;

                        SetCropToSession(model);
                        if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
                        {
                            model.GrassGrowthClassCounter = 0;
                            SetCropToSession(model);
                        }
                    }
                    else
                    {
                        model.DefoliationSequenceId = null;
                    }
                }

                model.Yield = null;
                model.Crops.ForEach(x => x.Yield = null);
                model.GrassSeason = null;
            }
            SetCropToSession(model);
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
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CurrentSward() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CurrentSwardError"] = ex.Message;
            return RedirectToAction("CurrentSward");
        }
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
        PlanViewModel model = GetCropFromSession();
        try
        {
            if (model == null)
            {
                _logger.LogTrace("Crop Controller : SwardType() action called - CropData session is null");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<SwardTypeResponse> swardTypeResponses, Error error) = await _cropLogic.FetchSwardTypes();
            if (!string.IsNullOrWhiteSpace(error.Message))
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
            return RedirectToAction("Defoliation");
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
                return RedirectToAction("Defoliation");
            }
            else
            {
                ViewBag.DefoliationSequenceResponses = defoliationSequenceResponses;
            }

            //temporary code until api returns correct defoliation sequence for these
            if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly)
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
                    return RedirectToAction("GrassGrowthClass");
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly)
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
                    return RedirectToAction("GrassGrowthClass");
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }

            if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly)
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
                    return RedirectToAction("GrassGrowthClass");
                }
                else
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in DefoliationSequence() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["DefoliationError"] = ex.Message;
            return RedirectToAction("Defoliation");
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
                    return RedirectToAction("GrassGrowthClass");
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

            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
                if (model.GrassGrowthClassCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                    model.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                    ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                    ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;
                    (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
                    if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["GrassGrowthClassError"] = error.Message;
                        return RedirectToAction("DefoliationSequence");
                    }
                    else
                    {
                        if (yieldRangesEnglandAndWalesResponses != null && yieldRangesEnglandAndWalesResponses.Count > 0)
                        {
                            ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
                            ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
                            ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                        }
                    }
                }

                SetCropToSession(model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && (model.Crops != null && model.Crops.Count > 0))
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;//index of list
                if (itemCount == 0)
                {
                    model.GrassGrowthClassCounter = 0;
                    model.GrassGrowthClassEncryptedCounter = string.Empty;
                    SetCropToSession(model);

                    //back button logic start
                    if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndSilage || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazingAndHay)
                    {
                        return RedirectToAction("DefoliationSequence");
                    }
                    else if (model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.GrazedOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForHayOnly || model.SwardManagementId == (int)NMP.Commons.Enums.SwardManagement.CutForSilageOnly)
                    {
                        if (model.SwardTypeId == (int)NMP.Commons.Enums.SwardType.Grass)
                        {
                            return RedirectToAction("Defoliation");
                        }

                    }

                    return RedirectToAction("DefoliationSequence");
                }

                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.GrassGrowthClassCounter = index;

                model.GrassGrowthClassEncryptedCounter = _fieldDataProtector.Protect(model.GrassGrowthClassCounter.ToString());
                model.FieldID = model.Crops[model.GrassGrowthClassCounter].FieldID;
                ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
                ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;

                (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["GrassGrowthClassError"] = error.Message;
                    return RedirectToAction("DefoliationSequence");
                }
                else
                {
                    ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
                    ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
                    ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                }
                if (model.GrassGrowthClassQuestion != null)
                {
                    return RedirectToAction("DefoliationSequence");
                }
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
        List<int> grassGrowthClassIds = new List<int>();
        Error error = new Error();
        List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>();
        PlanViewModel planViewModelBeforeUpdate = GetCropFromSession() ?? new PlanViewModel();
        if (model.Crops.Count == 1 || model.GrassGrowthClassDistinctCount > 1)
        {
            if (model.Crops[model.GrassGrowthClassCounter].Yield == null)
            {
                ModelState.AddModelError("Crops[" + model.GrassGrowthClassCounter + "].Yield", Resource.MsgSelectAnOptionBeforeContinuing);
            }
        }
        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1)
        {
            if (model.GrassGrowthClassQuestion == null)
            {
                ModelState.AddModelError("GrassGrowthClassQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
        }
        if (!ModelState.IsValid)
        {
            foreach (var crop in model.Crops)
            {
                fieldIds.Add(crop.FieldID ?? 0);
            }

            (grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
            model.FieldID = model.Crops[0].FieldID.Value;
            model.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
            ViewBag.FieldName = model.Crops[model.GrassGrowthClassCounter].FieldName;
            ViewBag.GrassGrowthClass = grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassName;
            (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["GrassGrowthClassError"] = error.Message;
                return RedirectToAction("DefoliationSequence");
            }
            else
            {
                ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
                ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
                ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
            }
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

        (grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);

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
                    (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[i + 1].GrassGrowthClassId);
                    if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["GrassGrowthClassError"] = error.Message;
                        return RedirectToAction("GrassGrowthClass");
                    }
                    else
                    {
                        ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
                        ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
                        ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                    }
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

        SetCropToSession(model);
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

    [HttpGet]
    public async Task<IActionResult> DryMatterYield(string q)
    {
        _logger.LogTrace("Crop Controller : DryMatterYield({Q}) action called", q);
        PlanViewModel model = GetCropFromSession();
        List<int> fieldIds = new List<int>();
        List<int> grassGrowthClassIds = new List<int>();
        Error error = new Error();

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
            else
            {
                foreach (var grassGrowthClass in grassGrowthClasses)
                {
                    grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
                }
            }

            if (string.IsNullOrWhiteSpace(q) && model.Crops != null && model.Crops.Count > 0)
            {
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
                if (model.DryMatterYieldCounter == 0)
                {
                    model.FieldID = model.Crops[0].FieldID.Value;
                }

                (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[0].GrassGrowthClassId);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["GrassGrowthClassError"] = error.Message;
                    return RedirectToAction("GrassGrowthClass");
                }
                else
                {
                    ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                }

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
                    return RedirectToAction("GrassGrowthClass");
                }
                model.FieldID = model.Crops[index].FieldID.Value;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.Crops[index].FieldID.Value)).Name;
                model.DryMatterYieldCounter = index;
                model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());

                (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[index].GrassGrowthClassId);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["GrassGrowthClassError"] = error.Message;
                    return RedirectToAction("GrassGrowthClass");
                }
                else
                {
                    ViewBag.YieldMin = yieldRangesEnglandAndWalesResponses.First();
                    ViewBag.YieldMax = yieldRangesEnglandAndWalesResponses.Last();
                    ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                }
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in DryMatterYield() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["DryMatterYieldError"] = ex.Message;
            return RedirectToAction("GrassGrowthClass");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DryMatterYield(PlanViewModel model)
    {
        _logger.LogTrace("Crop Controller : DryMatterYield() post action called");

        List<int> fieldIds = new List<int>();
        List<int> grassGrowthClassIds = new List<int>();
        Error error = new Error();
        List<GrassGrowthClassResponse> grassGrowthClasses = new List<GrassGrowthClassResponse>();

        if (model.Crops.Count > 1 && model.GrassGrowthClassDistinctCount == 1)
        {
            if (model.Crops[model.DryMatterYieldCounter].Yield == null)
            {
                ModelState.AddModelError("Crops[" + model.DryMatterYieldCounter + "].Yield", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                foreach (var crop in model.Crops)
                {
                    fieldIds.Add(crop.FieldID ?? 0);
                }

                (grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);
                (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[model.GrassGrowthClassCounter].GrassGrowthClassId);

                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["GrassGrowthClassError"] = error.Message;
                    return RedirectToAction("DefoliationSequence");
                }
                else
                {
                    ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                }

                return View(model);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        foreach (var crop in model.Crops)
        {
            fieldIds.Add(crop.FieldID ?? 0);
        }

        (grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIds);

        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["DryMatterYieldError"] = error.Message;
            return RedirectToAction("GrassGrowthClass");
        }
        else
        {
            foreach (var grassGrowthClass in grassGrowthClasses)
            {
                grassGrowthClassIds.Add(grassGrowthClass.GrassGrowthClassId);
            }
        }

        model.GrassGrowthClassCounter = 0;
        if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterDifferentFiguresForEachField)
        {

            for (int i = 0; i < model.Crops.Count; i++)
            {
                if (model.FieldID == model.Crops[i].FieldID.Value)
                {
                    model.DryMatterYieldCounter++;
                    if (i + 1 < model.Crops.Count)
                    {
                        model.FieldID = model.Crops[i + 1].FieldID.Value;
                        (List<YieldRangesEnglandAndWalesResponse> yieldRangesEnglandAndWalesResponses, error) = await _cropLogic.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(model.DefoliationSequenceId ?? 0, grassGrowthClasses[i + 1].GrassGrowthClassId);

                        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["DryMatterYieldError"] = error.Message;
                            return RedirectToAction("GrassGrowthClass");
                        }
                        else
                        {
                            ViewBag.YieldRanges = yieldRangesEnglandAndWalesResponses.OrderByDescending(x => x.YieldId);
                        }
                    }

                    break;
                }
            }

            model.DryMatterYieldEncryptedCounter = _fieldDataProtector.Protect(model.DryMatterYieldCounter.ToString());
            SetCropToSession(model);
            if (model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsCropGroupChange) && (!model.IsCropTypeChange) && (!model.IsCurrentSwardChange))
            {
                SetCropToSession(model);
                return RedirectToAction(_checkAnswerActionName);
            }
        }
        else if (model.GrassGrowthClassQuestion == (int)NMP.Commons.Enums.YieldQuestion.EnterASingleFigureForAllTheseFields)
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

        if (model.DryMatterYieldCounter == model.Crops.Count ||
            (model.IsCheckAnswer && model.Crops.Where((crop, index) => index != model.DryMatterYieldCounter - 1).All(crop => crop != null && crop.Yield != null)))
        {
            SetCropToSession(model);
            return RedirectToAction(_checkAnswerActionName);
        }
        return View(model);
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
            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            (List<HarvestYearPlanResponse> harvestYearPlanResponse, Error error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate) && (model.IsComingFromRecommendation == null))
            {
                if (string.IsNullOrWhiteSpace(error.Message))
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
                else
                {
                    TempData["CancelPageError"] = error.Message;
                    return View("Cancel", model);
                }
            }
            else if ((!string.IsNullOrWhiteSpace(model.EncryptedIsCropUpdate)) && (model.IsComingFromRecommendation != null && model.IsComingFromRecommendation.Value))
            {
                return RedirectToAction("Recommendations", new
                {
                    q = model.EncryptedFarmId,
                    r = model.EncryptedFieldId,
                    s = model.EncryptedHarvestYear
                });
            }

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
    }

    [HttpGet]
    public async Task<IActionResult> CheckYourPlanData(string? year, string? q)
    {
        _logger.LogTrace("Crop Controller : CheckYourPlanData() action called");
        PlanViewModel model = new PlanViewModel();
        int farmId = 0;
        FarmResponse farm = new FarmResponse();
        Error error = new Error();
        try
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);
                model.EncryptedFarmId = q;
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(year));
            }

            model.FarmName = farm.Name;
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
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear));
            }

            ViewBag.PlanSummaryList = planSummaryResponse;
            (int? topPrevCroppingYear, error) = await _previousCroppingLogic.FetchPreviousCroppingYearByFarmdId(farmId);

            if (string.IsNullOrWhiteSpace(error.Message) && topPrevCroppingYear > 0)
            {
                DateTime currentDate = DateTime.Now;
                DateTime harvestYearEndDate = new DateTime(currentDate.Year, 7, 31, 0, 0, 0, DateTimeKind.Local);
                int currentHarvestYear = currentDate > harvestYearEndDate ? currentDate.Year + 1 : currentDate.Year;
                List<int> yearList = new List<int>();
                if (planSummaryResponse != null && planSummaryResponse.Count > 0)
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
                        model.HarvestYear.Add(harvestNewYear);
                    }

                    int minYear = topPrevCroppingYear < planSummaryResponse.Min(x => x.Year) ? topPrevCroppingYear ?? 0 : planSummaryResponse.Min(x => x.Year) - 1;
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
                            model.HarvestYear.Add(harvestYear);
                        }
                    }

                    if (model.HarvestYear.Count > 0)
                    {
                        model.HarvestYear = model.HarvestYear.OrderByDescending(x => x.Year).ToList();
                    }

                    SetCropToSession(model);
                    bool isPreviousYearPlanExist = false;

                    if (model.HarvestYear.Any(x => x.Year < model.Year && x.IsAnyPlan == true))
                    {
                        isPreviousYearPlanExist = true;
                    }
                    else
                    {
                        isPreviousYearPlanExist = false;
                    }

                    if (isPreviousYearPlanExist)
                    {
                        //to remove base year from HarvestYear list
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

                        //end
                        if (model.HarvestYear.All(x => !x.IsAnyPlan))
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
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Crop Controller : Exception in CheckYourPlanData() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
            TempData["CheckYourPlanDataError"] = ex.Message;
            return RedirectToAction(_plansAndRecordsOverviewActionName, "Crop", new { id = q });
        }

        return View(model);
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

        if (model.CopyYear == null)
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
            for (int i = 0; i < model?.Crops?.Count; i++)
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
        Error? error = null;

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
            TempData[_tempDataErrorKey] = string.Concat(error == null ? "" : error.Message, ex.Message);
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
        return RedirectToAction("CopyCheckAnswer");
    }

    [HttpGet]
    public async Task<IActionResult> CopyCheckAnswer()
    {
        _logger.LogTrace("Crop Controller : CopyOrganicInorganicApplications() action called");
        PlanViewModel? model = GetCropFromSession();
        Error? error = null;
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
            TempData[_tempDataErrorKey] = string.Concat(error == null ? "" : error.Message, ex.Message);
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
        }

        if (!ModelState.IsValid)
        {
            return View("CopyCheckAnswer", model);
        }

        Error? error = null;
        bool isOrganic = false;
        bool isFertiliser = false;
        isOrganic = (model.OrganicInorganicCopy & OrganicInorganicCopyOptions.OrganicMaterial) != 0;
        isFertiliser = (model.OrganicInorganicCopy & OrganicInorganicCopyOptions.InorganicFertiliser) != 0;
        (bool success, error) = await _cropLogic.CopyCropNutrientManagementPlan(Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)), model.Year ?? 0, model.CopyYear ?? 0, isOrganic, isFertiliser);

        if (error.Message == null && success)
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
        else
        {
            TempData["ErrorCopyPlan"] = Resource.MsgWeCouldNotCreateYourPlanPleaseTryAgainLater;
            return RedirectToAction("CopyCheckAnswer");
        }
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

    private static string ShorthandDefoliationSequence(List<string> data)
    {
        if (data == null || data.Count == 0)
        {
            return "";
        }

        Dictionary<string, int> defoliationSequence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string item in data)
        {
            string name = item.Trim().ToLower();
            if (defoliationSequence.ContainsKey(name))
            {
                defoliationSequence[name]++;
            }
            else
            {
                defoliationSequence[name] = 1;
            }
        }

        List<string> result = FormatDefoliationSequenceEntries(defoliationSequence);

        return string.Join(", ", result);
    }

    private static List<string> FormatDefoliationSequenceEntries(Dictionary<string, int> defoliationSequence)
    {
        List<string> result = new List<string>();

        foreach (var entry in defoliationSequence)
        {
            string word = entry.Key;

            if (entry.Value > 1)
            {
                if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
                    word.EndsWith("sh") || word.EndsWith("ch"))
                {
                    word += "es";
                }
                else
                {
                    word += "s";
                }
            }


            word = char.ToUpper(word[0]) + word.Substring(1);
            result.Add($"{entry.Value} {word}");
        }

        return result;
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
            if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponseForFilter.Count > 0)
            {
                //filter the plan list based on the crop group
                harvestYearPlanResponseForFilter = harvestYearPlanResponseForFilter.Where(x => x.CropGroupName == model.PreviousCropGroupName).ToList();
                if (harvestYearPlanResponseForFilter != null)
                {
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

        if (!string.IsNullOrWhiteSpace(error.Message) || plans.Count == 0)
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
                model.CropTypeID ?? 0,
                isCropUpdate,
                model.Crops);

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
