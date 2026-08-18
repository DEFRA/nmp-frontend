using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Helpers;
using OpenTelemetry.Metrics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class OrganicManureController(ILogger<OrganicManureController> logger, IDataProtectionProvider dataProtectionProvider,
          IOrganicManureLogicDependencies dependencies) : Controller
    {
        private readonly ILogger<OrganicManureController> _logger = logger;
        private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
        private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
        private readonly IDataProtector _organicManureProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.OrganicManureController");
        private readonly IOrganicManureLogic _organicManureLogic = dependencies.OrganicManureLogic;
        private readonly IFarmLogic _farmLogic = dependencies.FarmLogic;
        private readonly ICropLogic _cropLogic = dependencies.CropLogic;
        private readonly IFieldLogic _fieldLogic = dependencies.FieldLogic;
        private readonly IMannerLogic _mannerLogic = dependencies.MannerLogic;
        private readonly IFertiliserManureLogic _fertiliserManureLogic = dependencies.FertiliserManureLogic;
        private readonly IWarningLogic _warningLogic = dependencies.WarningLogic;
        private const string _organicManureSessionKey = "OrganicManure";
        private const string _fieldGroup = "FieldGroup";
        private const string _checkAnswer = "CheckAnswer";
        private const string _harvestYearOverview = "HarvestYearOverview";
        private const string _fieldGroupError = "FieldGroupError";
        private const string _manureGroup = "ManureGroup";
        private const string _nutrientRecommendationsError = "NutrientRecommendationsError";
        private const string _recommendations = "Recommendations";
        private const string _addOrganicManureError = "AddOrganicManureError";
        private const string _farmList = "FarmList";
        private const string _fieldErrorTempDataKey = "FieldError";
        private const string _applicationDateKey = "ApplicationDate";
        private const string _soilDrainageEndDateKey = "SoilDrainageEndDate";
        private const string _totalRainfallKey = "TotalRainfall";
        private const string _areaKey = "Area";
        private const string _quantityKey = "Quantity";
        private const string _isSameDefoliationForAll = "IsSameDefoliationForAll";
        private const string _dryMatterPercentKey = "DryMatterPercent";
        private const string _applicationRateKey = "ApplicationRate";
        private const string _formatIndexKey = "{0} {1}";
        private const string _autumnCropNitrogenUptakeKey = "AutumnCropNitrogenUptake";
        private const string _applicationMethodErrorKey = "ApplicationMethodError";  //ManureTypeError
        private const string _manureGroupError = "ManureGroupError";
        private const string _manureTypeAction = "ManureType";
        private const string _doubleCropAction = "DoubleCrop";
        private const string _manureApplyingDateAction = "ManureApplyingDate";
        private const string _manureApplyingDateError = "ManureApplyingDateError";
        private const string _conditionsAffectingNutrients = "ConditionsAffectingNutrients";
        private const string _incorporationMethodAction = "IncorporationMethod";
        private const string _applicationRateMethodAction = "ApplicationRateMethod";
        private const string _incorporationMethodError = "IncorporationMethodError";
        private const string _incorporationDelayAction = "IncorporationDelay";
        private const string _incorporationDelayError = "IncorporationDelayError";
        private const string _dateStringLiteral = "yyyy-MM-dd";
        private const string _checkYourAnswerError = "CheckYourAnswerError";
        private const string _otherMaterialName = "OtherMaterialName";
        private const string _updateOrganicManureError = "UpdateOrganicManureError";
        private const string _manureTypeError = "ManureTypeError";  //Defoliation
        private const string _doubleCropError = "DoubleCropError";
        private const string _defoliationAction = "Defoliation";

        private OrganicManureViewModel? GetOrganicManureFromSession()
        {
            if (HttpContext.Session.Exists(_organicManureSessionKey))
            {
                return HttpContext.Session.GetObjectFromJson<OrganicManureViewModel>(_organicManureSessionKey);
            }
            return null;
        }

        private void SetOrganicManureToSession(OrganicManureViewModel organicManureViewModel)
        {
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, organicManureViewModel);
        }

        private void RemoveOrganicManureSession()
        {
            HttpContext.Session.Remove(_organicManureSessionKey);
        }

        public IActionResult Index()
        {
            _logger.LogTrace($"Organic Manure Controller : Index() action called");
            return View();
        }
        public IActionResult CreateManureCancel(string q, string r)
        {
            _logger.LogTrace("Organic Manure Controller : CreateManureCancel({Q}, {R}) action called", q, r);
            RemoveOrganicManureSession();
            return RedirectToAction(_harvestYearOverview, "Crop", new { Id = q, year = r });
        }

        [HttpGet]
        public async Task<IActionResult> FieldGroup(string q, string r, string? s)
        {
            _logger.LogTrace("Organic Manure Controller : FieldGroup({Q}, {R}, {S}) action called", q, r, s);

            OrganicManureViewModel? model = GetOrganicManureFromSession();

            try
            {
                if (!await ValidateQueryParametersAsync(q, r, model))
                {
                    _logger.LogTrace("Organic Manure Controller : FieldGroup() action - Invalid query parameters");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.InternalServerError);
                }

                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r))
                {
                    model = await InitializeModelAsync(q, r);
                }

                if (model != null)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        return await HandleSpecificFieldSelectionAsync(q, r, s, model);
                    }

                    await LoadCropTypeSelectionUIAsync(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in FieldGroup() action");
                TempData[_fieldGroupError] = ex.Message;
            }

            return FinalizeAndReturnView(model, s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldGroup(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : FieldGroup() post action called");
            AddErrorIfNull(model.FieldGroup, _fieldGroup, Resource.MsgSelectAnOptionBeforeContinuing);
            try
            {

                List<SelectListItem> cropgroupList = await BindFieldGroupList(model);
                ViewBag.FieldGroupList = cropgroupList;
                if (!ModelState.IsValid)
                {
                    return View("Views/OrganicManure/FieldGroup.cshtml", model);
                }
                model = await BindCropOrder(model, cropgroupList);

                model.IsComingFromRecommendation = false;
                SetOrganicManureToSession(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller: Exception in FieldGroup() post action");
                TempData[_fieldGroupError] = ex.Message;
                return View("Views/OrganicManure/FieldGroup.cshtml", model);
            }
            return RedirectToAction("Fields");

        }
        private static async Task<bool> ValidateQueryParametersAsync(string q, string r, OrganicManureViewModel? model)
        {
            if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r) && model == null)
            {
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }

        private async Task<OrganicManureViewModel> InitializeModelAsync(string q, string r)
        {

            OrganicManureViewModel? model = new OrganicManureViewModel();
            model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(r));
            model.EncryptedFarmId = q;
            model.EncryptedHarvestYear = r;
            (FarmResponse? farm, Error? error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (HasError(error))
            {
                TempData[_fieldGroupError] = error.Message;
                return model;
            }
            if (farm != null)
            {
                model.FarmName = farm.Name;
                model.FarmRB209CountryID = farm.RB209CountryID;
                model.FarmCountryId = farm.CountryID;
            }

            SetOrganicManureToSession(model);
            return model;
        }

        private async Task<IActionResult> HandleSpecificFieldSelectionAsync(string q, string r, string s, OrganicManureViewModel model)
        {
            await SetupSpecificFieldModeAsync(s, model);

            var result = await TryLoadFieldManureDataAsync(q, r, s, model);
            if (result != null) return result;

            await UpdateGrassCropSettingsAsync(model);
            await UpdateEncryptedCountersAsync(model);

            SetOrganicManureToSession(model);
            return RedirectToAction(_manureGroup);
        }

        private async Task SetupSpecificFieldModeAsync(string s, OrganicManureViewModel model)
        {
            model.FieldList = new List<string>();
            model.FieldGroup = Resource.lblSelectSpecificFields;
            model.CropGroupName = Resource.lblSelectSpecificFields;
            model.IsComingFromRecommendation = true;

            string fieldId = _fieldDataProtector.Unprotect(s);
            model.FieldList.Add(fieldId);

            var field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
            model.FieldName = field?.Name;
        }

        private async Task<IActionResult?> TryLoadFieldManureDataAsync(string q, string r, string s, OrganicManureViewModel model)
        {
            string fieldId = model.FieldList[0];
            var (manIds, error) = await _fertiliserManureLogic
                .FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(
                    model.HarvestYear.Value, fieldId, null, 1);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_nutrientRecommendationsError] = error.Message;
                return RedirectToAction(_recommendations, "Crop", new { q, r = s, s = r });
            }

            if (!manIds.Any())
                return null;

            ResetOrganicManures(model);

            int counter = 1;
            foreach (var id in manIds)
            {
                model.OrganicManures.Add(new OrganicManureDataViewModel
                {
                    ManagementPeriodID = id,
                    FieldID = Convert.ToInt32(fieldId),
                    FieldName = model.FieldName,
                    EncryptedCounter = _fieldDataProtector.Protect(counter.ToString())
                });
                counter++;
            }

            model.DefoliationCurrentCounter = 0;
            SetOrganicManureToSession(model);

            return null;
        }

        private async Task UpdateGrassCropSettingsAsync(OrganicManureViewModel model)
        {
            var grassCropsFound = false;
            int grassCropCounter = 0;

            foreach (var fieldId in model.FieldList)
            {
                var (cropList, _) = await _cropLogic.FetchCropPlanByFieldIdAndYear(
                    Convert.ToInt32(fieldId), model.HarvestYear.Value);

                if (!cropList.Any()) continue;

                cropList = cropList.Where(x => x.CropOrder == 1).ToList();
                if (!cropList.Any(x => x.CropTypeID == (int)CropTypes.Grass && x.DefoliationSequenceID != null))
                {
                    continue;
                }

                var grassCrop = cropList.FirstOrDefault();
                if (grassCrop == null || grassCrop.ID == null) continue;
                var (mgmtList, _) = await _cropLogic.FetchManagementperiodByCropId(grassCrop.ID.Value, false);
                if (mgmtList == null) continue;

                var toRemove = model.OrganicManures
                    .Where(fm => mgmtList.Any(mp => mp.ID == fm.ManagementPeriodID))
                    .Skip(1)
                    .Select(mp => mp.ManagementPeriodID)
                    .ToList();

                model.OrganicManures.RemoveAll(fm => toRemove.Contains(fm.ManagementPeriodID));

                grassCropCounter++;
                grassCropsFound = true;
            }

            if (!grassCropsFound) return;

            model.GrassCropCount = grassCropCounter;
            model.IsAnyCropIsGrass = true;
            model.IsSameDefoliationForAll = true;
            SetOrganicManureToSession(model);
        }

        private async Task UpdateEncryptedCountersAsync(OrganicManureViewModel model)
        {
            int index = 1;
            foreach (var organic in model.OrganicManures)
            {
                var (period, error1) = await _cropLogic.FetchManagementperiodById(organic.ManagementPeriodID);
                if (!string.IsNullOrWhiteSpace(error1?.Message)) continue;
                if (period?.CropID == null) continue;

                var (crop, error2) = await _cropLogic.FetchCropById(period.CropID.Value);
                if (!string.IsNullOrWhiteSpace(error2?.Message)) continue;

                organic.EncryptedCounter = _fieldDataProtector.Protect(index.ToString());
                organic.IsGrass = crop.CropTypeID == (int)CropTypes.Grass;

                index++;
            }

            SetOrganicManureToSession(model);
        }

        private async Task LoadCropTypeSelectionUIAsync(OrganicManureViewModel model)
        {
            var (cropTypes, error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);

            if ((HasError(error)) || !cropTypes.Any())
            {
                TempData[_fieldGroupError] = error?.Message;
                return;
            }

            var distinctCropTypes = cropTypes.DistinctBy(x => x.CropGroupName);
            var items = ToSelectList(distinctCropTypes, x => x.CropGroupName, x => string.Format(Resource.lblGroupNameFieldsWithCropTypeName, x.CropGroupName, x.CropType));

            items.Insert(0, new SelectListItem
            {
                Value = Resource.lblAll,
                Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear)
            });

            items.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });

            ViewBag.FieldGroupList = items;
        }

        private IActionResult FinalizeAndReturnView(OrganicManureViewModel model, string? s)
        {
            if (model.IsCheckAnswer && string.IsNullOrWhiteSpace(s))
            {
                model.IsFieldGroupChange = true;
            }
            SetOrganicManureToSession(model);
            return View("Views/OrganicManure/FieldGroup.cshtml", model);
        }


        private async Task<OrganicManureViewModel> BindCropOrder(OrganicManureViewModel model, List<SelectListItem> selectListItem)
        {
            (List<ManureCropTypeResponse> cropGroupList, Error error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
            if (error == null && cropGroupList.Count > 0 && model.FieldGroup != null && !model.FieldGroup.Equals(Resource.lblAll) && !model.FieldGroup.Equals(Resource.lblSelectSpecificFields))
            {
                string cropGroupName = cropGroupList.FirstOrDefault(x => x.CropGroupName == model.FieldGroup)?.CropGroupName;
                if (selectListItem != null && selectListItem.Count > 0)
                {
                    model.CropGroupName = selectListItem.Where(x => x.Value == cropGroupName).Select(x => x.Text).First();
                }
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
            return model;
        }
        private async Task<List<SelectListItem>> BindFieldGroupList(OrganicManureViewModel model)
        {
            List<SelectListItem> selectListItem = new List<SelectListItem>();
            (List<ManureCropTypeResponse> cropGroupList, Error error) = await _fertiliserManureLogic.FetchCropTypeByFarmIdAndHarvestYear(model.FarmId.Value, model.HarvestYear.Value);
            if (error == null && cropGroupList.Count > 0)
            {
                selectListItem = ToSelectList(cropGroupList, f => f.CropGroupName.ToString(), f => string.Format(Resource.lblGroupNameFieldsWithCropTypeName, f.CropGroupName.ToString(), f.CropType.ToString()));

                selectListItem.Insert(0, new SelectListItem { Value = Resource.lblAll, Text = string.Format(Resource.lblAllFieldsInTheYearPlan, model.HarvestYear) });
                selectListItem.Add(new SelectListItem { Value = Resource.lblSelectSpecificFields, Text = Resource.lblSelectSpecificFields });

            }
            else if (error != null)
            {
                TempData[_fieldGroupError] = error.Message;
            }
            return selectListItem;
        }

        private async Task<OrganicManureDataViewModel> BindDefoliationName(OrganicManureDataViewModel organicManure, OrganicManureViewModel? organicManureViewModel, int manId, List<HarvestYearPlanResponse> cropPlans, int currentIndex)
        {
            organicManure.Defoliation = organicManureViewModel?.OrganicManures?[currentIndex].Defoliation;
            if (organicManure.Defoliation == null)
                return organicManure;
            (ManagementPeriod? managementPeriod, Error? error) = await _cropLogic.FetchManagementperiodById(manId);
            if (error != null)
                return organicManure;
            HarvestYearPlanResponse? crop = cropPlans.FirstOrDefault(x => x.CropID == managementPeriod?.CropID);

            if (crop?.DefoliationSequenceID == null)
                return organicManure;

            (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
            if (error != null && defoliationSequence != null)
            {
                string description = defoliationSequence.DefoliationSequenceDescription;

                string[] defoliationParts = description.Split(',')
                                                       .Select(x => x.Trim())
                                                       .ToArray();

                string selectedDefoliation = (organicManure.Defoliation.Value > 0 && organicManure.Defoliation.Value <= defoliationParts.Length)
                    ? $"{Enum.GetName(typeof(PotentialCut), organicManure.Defoliation.Value)} ({defoliationParts[organicManure.Defoliation.Value - 1]})"
                    : $"{organicManure.Defoliation.Value}";

                organicManure.DefoliationName = selectedDefoliation;
            }



            return organicManure;
        }


        private async Task<OrganicManureViewModel> BindGrassPropertyForField(OrganicManureViewModel model, List<HarvestYearPlanResponse> cropPlans)
        {
            foreach (string field in model.FieldList)
            {
                var cropList = cropPlans
                    .Where(x => x.FieldID == Convert.ToInt32(field))
                    .ToList();

                model = await BindGrassProperty(model, cropList, Convert.ToInt32(field));
            }

            return model;
        }

        private IActionResult RedirectForFieldGet(OrganicManureViewModel model, string message)
        {
            TempData[string.IsNullOrWhiteSpace(model.EncryptedOrgManureId)
                ? _fieldGroupError
                : _addOrganicManureError] = message;

            return string.IsNullOrWhiteSpace(model.EncryptedOrgManureId)
                ? RedirectToAction(_fieldGroup, model)
                : RedirectToAction(_checkAnswer);
        }
        [HttpGet]
        public async Task<IActionResult> Fields()
        {
            _logger.LogTrace("Organic Manure Controller : Fields() action called");

            if (!TryGetSessionModel(nameof(Fields), out var model, out var redirect))
            {
                return redirect;
            }

            try
            {
                // Crop plans
                var (cropPlans, cropError) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.HarvestYear.Value, model.FarmId.Value);

                if (!string.IsNullOrWhiteSpace(cropError?.Message))
                {
                    return RedirectForFieldGet(model, cropError.Message);
                }

                // Field list
                var (fieldSelectList, fieldError) = await GetFieldSelectListAsync(model);
                if (fieldError != null)
                {
                    TempData[_fieldGroupError] = fieldError.Message;
                    return View(_fieldGroup, model);
                }

                ViewBag.FieldList = fieldSelectList;

                // Handle encrypted ID case
                var (encryptedFields, encError) = await GetFieldsForEncryptedIdAsync(model);

                if (encError != null)
                {
                    TempData[_addOrganicManureError] = encError.Message;
                    return RedirectToAction(_checkAnswer);
                }

                if (encryptedFields != null)
                {
                    ViewBag.FieldList = encryptedFields;
                    return View(model);
                }

                // No encryptedId → continue normal flow
                if (model.FieldGroup == Resource.lblSelectSpecificFields)
                    return View(model);

                // Assign all fields
                model.FieldList = fieldSelectList.Select(x => x.Value).ToList();

                ResetOrganicManures(model);
                model.IsDoubleCropAvailable = false;

                model = await BindGrassPropertyForField(model, cropPlans);

                if (!HttpContext.Session.Keys.Contains(_organicManureSessionKey))
                {
                    return RedirectToAction(_farmList, "Farm");
                }

                var sessionModel = GetOrganicManureFromSession();

                string fieldIds = string.Join(",", model.FieldList);

                var mgmtError = await PopulateManagementAsync(model, fieldIds, cropPlans, sessionModel);
                if (mgmtError != null)
                {
                    TempData[_fieldGroupError] = mgmtError.Message;
                    return View(_fieldGroup, model);
                }

                await PopulateGrassDataAsync(model, cropPlans, true);

                if (model.OrganicManures != null)
                {
                    await PopulateOrganicPipelineAsync(model, cropPlans);
                }

                // CheckAnswer logic
                await CheckAnswerLogicForField(model);

                // Field name
                if (model.FieldList?.Count == 1)
                {
                    model.FieldName = fieldSelectList
                        .FirstOrDefault(x => x.Value == model.FieldList[0])?.Text;
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                return RedirectToAction(_manureGroup);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Exception in Fields()");

                return RedirectForFieldGet(model, ex.Message);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fields(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : Fields() post action called");

            try
            {
                if (!TryGetSessionModel(nameof(Fields), out var sessionModel, out var redirect))
                    return redirect;

                // Field list
                var (fieldSelectList, fieldError) = await GetFieldSelectListAsync(model);
                if (fieldError != null)
                {
                    TempData[_fieldErrorTempDataKey] = fieldError.Message;
                    return View(model);
                }

                ViewBag.FieldList = fieldSelectList;

                // Encrypted ID check
                var (encList, encError) = await GetFieldsForEncryptedIdAsync(model);

                if (encError != null)
                {
                    TempData[_fieldErrorTempDataKey] = encError.Message;
                    return View(model);
                }

                if (encList != null)
                    ViewBag.FieldList = encList;

                // Validation
                if (model.FieldList == null || model.FieldList.Count == 0)
                {
                    ModelState.AddModelError("FieldList", Resource.MsgSelectAtLeastOneField);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);

                }

                // Select All
                SelectAllLogic(model, fieldSelectList);

                // Crop plans
                var (cropPlans, cropError) =
                    await _cropLogic.FetchHarvestYearPlansByFarmId(
                        model.HarvestYear.Value,
                        model.FarmId.Value);

                if (cropError != null)
                {
                    TempData[_fieldGroupError] = cropError.Message;
                    return RedirectToAction(_fieldGroup);
                }

                // Grass binding
                model = await BindGrassPropertyForField(model, cropPlans);

                string fieldIds = string.Join(",", model.FieldList);

                var mgmtError = await PopulateManagementAsync(model, fieldIds, cropPlans, sessionModel);
                if (mgmtError != null)
                {
                    TempData[_fieldErrorTempDataKey] = mgmtError.Message;
                    return View(model);
                }

                // CheckAnswer logic
                await CheckAnswerLogicForField(model);

                await PopulateGrassDataAsync(model, cropPlans, false);

                // Detect field change
                model = await FieldChangeLogic(model, sessionModel, cropPlans);

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Exception in POST Fields()");
                TempData[_fieldErrorTempDataKey] = ex.Message;
                return View(model);
            }

            return RedirectToAction(_manureGroup);
        }

        private async Task<OrganicManureViewModel> FieldChangeLogic(OrganicManureViewModel model, OrganicManureViewModel? sessionModel, List<HarvestYearPlanResponse> cropPlans)
        {
            if (sessionModel?.OrganicManures != null &&
                model.OrganicManures.Any(x => !sessionModel.OrganicManures.Contains(x)))
            {
                model.IsAnyChangeInField = true;
            }

            await PopulateOrganicPipelineAsync(model, cropPlans);

            if (model.IsCheckAnswer &&
                model.IsFieldGroupChange &&
                model.IsAnyChangeInField &&
                sessionModel?.FieldList?.Count > 0)
            {
                model = await BindFieldData(model);
            }

            return model;
        }

        private async Task CheckAnswerLogicForField(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer && model.OrganicManures?.Count > 0)
            {
                model.AutumnCropNitrogenUptakes = await BuildAutumnCropNitrogenUptakeAsync(model);

                for (int i = 0; i < model.OrganicManures.Count; i++)
                    ApplyCommonManureProperties(model, model.OrganicManures[i], i);
            }
        }



        private static void SelectAllLogic(OrganicManureViewModel model, List<SelectListItem> fieldSelectList)
        {
            if (model.FieldList.Contains(Resource.lblSelectAll))
                model.FieldList = fieldSelectList
                    .Where(x => x.Value != Resource.lblSelectAll)
                    .Select(x => x.Value)
                    .ToList();

            model.IsAnyCropIsGrass = false;
            model.IsDoubleCropAvailable = false;
        }

        private async Task<IActionResult?> HandleFieldGroupChangeForCheckAnswerAsync(
    OrganicManureViewModel model)
        {
            if (!ShouldHandleFieldGroupChange(model))
                return null;

            if (!ValidateSession())
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);

            var sessionModel = GetOrganicManureFromSession();

            if (!HasFieldChanges(sessionModel, model))
                return null;

            var crop = await GetCropAsync(model);

            if (crop == null)
                return null;

            int cropCategoryId = await GetCropCategoryIdAsync(crop);

            model.AutumnCropNitrogenUptake =
                await GetAutumnCropNitrogenUptakeAsync(model, cropCategoryId);

            return null;
        }

        private static bool ShouldHandleFieldGroupChange(OrganicManureViewModel model)
        {
            return model.IsCheckAnswer && model.IsFieldGroupChange;
        }

        private bool ValidateSession()
        {
            if (HttpContext.Session.Keys.Contains(_organicManureSessionKey))
                return true;

            _logger.LogError("Organic Manure Controller : Session not found in Fields() action");
            return false;
        }

        private static bool HasFieldChanges(
            OrganicManureViewModel? sessionModel,
            OrganicManureViewModel model)
        {
            if (sessionModel?.FieldList == null || sessionModel.FieldList.Count == 0)
                return false;

            return sessionModel.FieldList.Any(oldField => !model.FieldList.Contains(oldField));
        }

        private async Task<Crop?> GetCropAsync(OrganicManureViewModel model)
        {
            var cropsResponse = await _cropLogic.FetchCropsByFieldId(
                Convert.ToInt32(model.FieldList[0]));

            return cropsResponse
                .FirstOrDefault(x => x.Year == model.HarvestYear);
        }

        private async Task<int> GetCropCategoryIdAsync(Crop crop)
        {
            int cropTypeId = crop.CropTypeID ?? 0;

            int cropCategoryId =
                await _mannerLogic.FetchCategoryIdByCropTypeIdAsync(cropTypeId);

            return AdjustCropCategoryForSowingDate(cropCategoryId, crop.SowingDate);
        }

        private static int AdjustCropCategoryForSowingDate(
            int cropCategoryId,
            DateTime? sowingDate)
        {
            if (sowingDate == null)
                return cropCategoryId;

            bool isEarlyCategory =
                cropCategoryId == (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal ||
                cropCategoryId == (int)NMP.Commons.Enums.CropCategory.EarlyStablishedWinterOilseedRape;

            bool isLateSeptemberSowing =
                sowingDate.Value.Month == (int)NMP.Commons.Enums.Month.September &&
                sowingDate.Value.Day > 15;

            if (!isEarlyCategory || !isLateSeptemberSowing)
                return cropCategoryId;

            return cropCategoryId ==
                   (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal
                ? (int)NMP.Commons.Enums.CropCategory.LateSownWinterCereal
                : (int)NMP.Commons.Enums.CropCategory.LateStablishedWinterOilseedRape;
        }

        private async Task<int> GetAutumnCropNitrogenUptakeAsync(
            OrganicManureViewModel model,
            int cropCategoryId)
        {
            if (!IsAutumnApplication(model.ApplicationDate))
                return 0;

            return await _mannerLogic.FetchCropNUptakeDefaultAsync(cropCategoryId);
        }

        private static bool IsAutumnApplication(DateTime? applicationDate)
        {
            return applicationDate.HasValue &&
                   applicationDate.Value.Month >= (int)NMP.Commons.Enums.Month.August &&
                   applicationDate.Value.Month <= (int)NMP.Commons.Enums.Month.October;
        }

        private async Task<(List<SelectListItem> list, Error? error)> GetFieldSelectListAsync(OrganicManureViewModel model)
        {
            var (fieldList, error) =
                await _organicManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(
                    model.HarvestYear.Value,
                    model.FarmId.Value,
                    model.FieldGroup == Resource.lblSelectSpecificFields || model.FieldGroup == Resource.lblAll
                        ? null
                        : model.FieldGroup);

            if (error != null) return (null, error);

            var selectList = ToSelectList(fieldList, f => f.Id.ToString(), f => f.Name)
                                .OrderBy(x => x.Text)
                                .ToList();

            return (selectList, null);
        }
        private async Task<(List<SelectListItem>? list, Error? error)> GetFieldsForEncryptedIdAsync(
    OrganicManureViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
                return (null, null);

            int id = Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedOrgManureId));

            var (data, error) =
                await _organicManureLogic.FetchFieldWithSameDateAndManureType(
                    id, model.FarmId.Value, model.HarvestYear.Value);

            if (!string.IsNullOrWhiteSpace(error?.Message))
                return (null, error);

            var list = ToSelectList(data.DistinctBy(x => x.Id),
                            x => x.Id.ToString(),
                            x => x.Name.ToString())
                        .OrderBy(x => x.Text)
                        .ToList();

            return (list, null);
        }
        private async Task PopulateGrassDataAsync(
    OrganicManureViewModel model,
    List<HarvestYearPlanResponse> cropPlans,
    bool isGetFlow)
        {
            if (!ShouldProcessGrassData(model))
            {
                ResetGrassData(model);
                return;
            }

            int counter = 0;

            foreach (var field in model.FieldList)
            {
                var cropList = GetFilteredCropList(model, cropPlans, isGetFlow, field);

                if (!IsGrassAndHasDefoliation(cropList))
                    continue;

                await RemoveExtraManagementPeriodsAsync(model, cropList[0].CropID);

                model.IsAnyCropIsGrass = true;
                counter++;
            }

            model.GrassCropCount = counter;
        }

        private static bool ShouldProcessGrassData(OrganicManureViewModel model)
        {
            return (model.IsAnyCropIsGrass ?? false) && model.FieldList != null;
        }

        private static void ResetGrassData(OrganicManureViewModel model)
        {
            model.GrassCropCount = null;
            model.IsSameDefoliationForAll = null;
            model.IsAnyChangeInSameDefoliationFlag = false;
        }

        private static List<HarvestYearPlanResponse> GetFilteredCropList(
            OrganicManureViewModel model,
            List<HarvestYearPlanResponse> cropPlans,
            bool isGetFlow,
            string field)
        {
            var cropList = cropPlans
                .Where(x => x.FieldID == Convert.ToInt32(field))
                .ToList();

            if (isGetFlow)
            {
                return FilterGetFlowCropList(model, cropList);
            }

            return cropList.Count > 0
                ? cropList.Where(x => x.CropOrder == 1).ToList()
                : cropList;
        }

        private static List<HarvestYearPlanResponse> FilterGetFlowCropList(
            OrganicManureViewModel model,
            List<HarvestYearPlanResponse> cropList)
        {
            bool isSpecificFieldGroup =
                !model.FieldGroup.Equals(Resource.lblAll) &&
                !model.FieldGroup.Equals(Resource.lblSelectSpecificFields);

            return isSpecificFieldGroup
                ? cropList.Where(x => x.CropGroupName.Equals(model.FieldGroup)).ToList()
                : cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).ToList();
        }

        private async Task RemoveExtraManagementPeriodsAsync(
            OrganicManureViewModel model,
            int cropId)
        {
            var (periods, _) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);

            var removeIds = periods
                .Skip(1)
                .Where(p => p.ID.HasValue)
                .Select(p => p.ID.Value)
                .ToList();

            model.OrganicManures?.RemoveAll(x => removeIds.Contains(x.ManagementPeriodID));
        }
        private async Task PopulateOrganicPipelineAsync(
    OrganicManureViewModel model,
    List<HarvestYearPlanResponse> cropPlans)
        {
            await BindOrganicData(model, cropPlans);
            RemoveFieldsFromDoubleCropList(model);
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

        }
        private async Task<Error?> PopulateManagementAsync(OrganicManureViewModel model, string fieldIds, List<HarvestYearPlanResponse> cropPlans, OrganicManureViewModel sessionModel)
        {
            var (ids, error) =
                await _fertiliserManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(
                    model.HarvestYear.Value,
                    fieldIds,
                    model.FieldGroup == Resource.lblSelectSpecificFields || model.FieldGroup == Resource.lblAll
                        ? null
                        : model.FieldGroup,
                    1);

            if (error != null) return error;

            if (ids.Count > 0)
                await PopulateOrganicManuresAsync(model, ids, cropPlans, sessionModel);

            return null;
        }

        private async Task<OrganicManureViewModel> BindOrganicData(OrganicManureViewModel model, List<HarvestYearPlanResponse> cropPlans)
        {
            int organicCounter = 1;
            if (model.OrganicManures != null)
            {
                model.OrganicManures = await BindOrganicManureListData(model, cropPlans, organicCounter);

                var grass = model.OrganicManures.Where(x => x.IsGrass).Select(x => x.FieldID).ToHashSet();
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

        private async Task<List<OrganicManureDataViewModel>> BindOrganicManureListData(OrganicManureViewModel model, List<HarvestYearPlanResponse> cropPlans, int organicCounter)
        {

            model.OrganicManures = await BindOragnicManureData(model, cropPlans, organicCounter);


            var grass = model.OrganicManures.Where(x => x.IsGrass).Select(x => x.FieldID).ToHashSet();
            if (model.DefoliationList != null)
            {
                model.DefoliationList = model.DefoliationList.Where(d => grass.Contains(d.FieldID)).ToList();
            }
            else
            {
                model.DefoliationList = null;
            }
            return model.OrganicManures;
        }
        private async Task<List<OrganicManureDataViewModel>> BindOragnicManureData(OrganicManureViewModel model, List<HarvestYearPlanResponse> cropPlans, int organicCounter)
        {
            foreach (var organic in model.OrganicManures)
            {
                (ManagementPeriod managementPeriod, _) = await _cropLogic.FetchManagementperiodById(organic.ManagementPeriodID);
                if (managementPeriod != null)
                {
                    HarvestYearPlanResponse? crop =
managementPeriod.CropID.HasValue
? cropPlans.FirstOrDefault(x => x.CropID == managementPeriod.CropID.Value)
: null;

                    if (crop != null)
                    {
                        organic.FieldID = crop.FieldID;
                        organic.FieldName = (await _fieldLogic.FetchFieldByFieldId(organic.FieldID.Value)).Name;
                        organic.EncryptedCounter = _fieldDataProtector.Protect(organicCounter.ToString());
                        organicCounter++;
                        if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                        {
                            organic.IsGrass = true;
                        }
                        else if (model.DefoliationList != null && model.DefoliationList.Any(x => x.FieldID == crop.FieldID))
                        {
                            model.DefoliationList.RemoveAll(x => x.FieldID == crop.FieldID);
                        }
                    }
                }
            }
            return model.OrganicManures;
        }
        private async Task<OrganicManureViewModel> BindFieldData(OrganicManureViewModel model)
        {

            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var crop = cropsResponse.Where(x => x.Year == model.HarvestYear);
            int cropTypeId = crop.Select(x => x.CropTypeID).FirstOrDefault() ?? 0;
            int cropCategoryId = await _mannerLogic.FetchCategoryIdByCropTypeIdAsync(cropTypeId);

            //check early and late for winter cereals and winter oilseed rape
            //if sowing date after 15 sept then late
            DateTime? sowingDate = crop.Select(x => x.SowingDate).FirstOrDefault();

            cropCategoryId = BindCropCategory(cropCategoryId, sowingDate);
            if (model.ApplicationDate.Value.Month >= (int)NMP.Commons.Enums.Month.August && model.ApplicationDate.Value.Month <= (int)NMP.Commons.Enums.Month.October)
            {
                model.AutumnCropNitrogenUptake = await _mannerLogic.FetchCropNUptakeDefaultAsync(cropCategoryId);
            }
            else
            {
                model.AutumnCropNitrogenUptake = 0;
            }
            return model;
        }
        private static int BindCropCategory(int cropCategoryId, DateTime? sowingDate)
        {
            if ((cropCategoryId == (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal || cropCategoryId == (int)NMP.Commons.Enums.CropCategory.EarlyStablishedWinterOilseedRape) && sowingDate != null)
            {

                int day = sowingDate.Value.Day;
                int month = sowingDate.Value.Month;
                if (month == (int)NMP.Commons.Enums.Month.September && day > 15)
                {
                    if (cropCategoryId == (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal)
                    {
                        cropCategoryId = (int)NMP.Commons.Enums.CropCategory.LateSownWinterCereal;
                    }
                    else
                    {
                        cropCategoryId = (int)NMP.Commons.Enums.CropCategory.LateStablishedWinterOilseedRape;
                    }
                }

            }
            return cropCategoryId;
        }

        private static void RemoveFieldsFromDoubleCropList(OrganicManureViewModel model)
        {
            //remove fields that's not in fieldList
            if (model.FieldList != null && model.FieldList.Any() && model.DoubleCrop != null && model.DoubleCrop.Count > 0 &&
            model.DoubleCrop.Any(dc => !model.FieldList.Contains(dc.FieldID.ToString())))
            {
                model.DoubleCrop?.RemoveAll(dc => !model.FieldList.Contains(dc.FieldID.ToString()));
            }


        }
        private async Task<OrganicManureViewModel> BindGrassProperty(OrganicManureViewModel model, List<HarvestYearPlanResponse> cropList, int fieldId)
        {
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
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(fieldId)).Name;
                    model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                }
                else if (model.DoubleCrop != null && model.DoubleCrop.Count > 0)
                {
                    model.DoubleCrop.RemoveAll(x => x.FieldID == fieldId);
                }
                if (IsGrassAndHasDefoliation(cropList))
                {
                    model.IsAnyCropIsGrass = true;
                    model.DefoliationCurrentCounter = 0;
                    model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(0.ToString());
                }
            }
            return model;
        }


        private static void BindFarmManureGroupId(OrganicManureViewModel model, List<FarmManureTypeResponse> farmManureGroupList, bool isThisOtherManure)
        {
            if (farmManureGroupList.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(model.FarmGroupManureId) && model.ManureGroupIdForFilter != null)
                {
                    if (isThisOtherManure)
                    {
                        FarmManureTypeResponse? farmManureType = farmManureGroupList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId && x.ManureTypeName.Equals(model.OtherMaterialName));
                        if (farmManureType != null)
                        {
                            model.FarmGroupManureId = string.Format(Resource.lblFarmManureWithId, farmManureType.ID.ToString());
                        }
                    }
                    else
                    {
                        model.FarmGroupManureId = model.ManureGroupIdForFilter.ToString();
                    }
                }
            }
            else
            {
                model.FarmGroupManureId = model.ManureGroupIdForFilter.ToString();
            }
        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup()
        {
            _logger.LogTrace($"Organic Manure Controller : ManureGroup() action called");
            if (!TryGetSessionModel(nameof(ManureGroup), out var model, out var redirect))
            {
                return redirect;
            }

            try
            {

                await PopulateManureGroupListAsync();
                if (model.FarmId.HasValue)
                {
                    (var farmManureGroupList, Error? error) = await PopulateFarmManureListAsync(model);
                    if (error != null)
                    {
                        TempData[_fieldErrorTempDataKey] = error.Message;
                        return RedirectToAction("Fields", model);
                    }

                    bool isThisOtherManure = !string.IsNullOrWhiteSpace(model.OtherMaterialName) && (model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials);
                    BindFarmManureGroupId(model, farmManureGroupList, isThisOtherManure);
                }


            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureGroup() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_fieldErrorTempDataKey] = ex.Message;
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureGroup(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManureGroup() post action called");
            AddErrorIfNull(model.FarmGroupManureId, "FarmGroupManureId", Resource.MsgSelectAnOptionBeforeContinuing);
            Error? error = null;
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateManureGroupListAsync();
                    if (model.FarmId.HasValue)
                    {
                        (_, error) = await PopulateFarmManureListAsync(model);
                        if (error != null)
                        {
                            TempData[_manureGroupError] = error.Message;
                        }
                    }
                    return View(model);

                }

                model = await BindValuesForManureGroup(model, error);

                if (model.IsCheckAnswer)
                {
                    model.IsManureTypeChange = true;
                }
                if (IsOtherManureType(model.ManureGroupIdForFilter))
                {
                    return RedirectForManureGroup(model);
                }

                (CommonResponse manureGroup, error) = await _mannerLogic.FetchManureGroupById(model.ManureGroupIdForFilter.Value);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_manureGroupError] = error.Message;
                    return View(model);
                }
                if (manureGroup != null)
                {
                    model.ManureGroupName = manureGroup.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureGroup() post action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData[_manureGroupError] = ex.Message;
            }
            SetOrganicManureToSession(model);
            return RedirectToAction(_manureTypeAction);
        }

        private IActionResult RedirectForManureGroup(OrganicManureViewModel model)
        {
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            if (model.IsDoubleCropAvailable)
            {
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_doubleCropAction);
            }
            else
            {
                model.DoubleCrop = null;
            }
            if (model.IsAnyCropIsGrass == true)
            {
                return HandleGrassCrop(model);
            }
            model.GrassCropCount = null;
            model.IsSameDefoliationForAll = null;
            model.IsAnyChangeInSameDefoliationFlag = false;
            SetOrganicManureToSession(model);
            return RedirectToAction(_manureApplyingDateAction);
        }

        private async Task<OrganicManureViewModel> BindValuesForManureGroup(OrganicManureViewModel model, Error? error)
        {
            if (!string.IsNullOrWhiteSpace(model.FarmGroupManureId) && model.FarmGroupManureId.Contains(Resource.lblFarmManure))
            {
                int farmManureGroupId = Convert.ToInt32(model.FarmGroupManureId.Split('_')[1]);

                (FarmManureTypeResponse? farmManureType, error) = await _organicManureLogic.FetchFarmManureTypeById(farmManureGroupId);
                if (error == null && farmManureType != null)
                {
                    model.ManureGroupIdForFilter = farmManureType.ManureTypeID;
                    model.ManureGroupId = farmManureType.ManureTypeID;
                    model.ManureTypeId = farmManureType.ManureTypeID;
                    model.OtherMaterialName = farmManureType.ManureTypeName;
                    model.ManureTypeName = farmManureType.ManureTypeName;
                }
            }
            else
            {
                model.ManureGroupIdForFilter = Convert.ToInt32(model.FarmGroupManureId);
                model.ManureGroupId = Convert.ToInt32(model.FarmGroupManureId);
            }

            return model;
        }

        private async Task PopulateManureGroupListAsync()
        {
            var (manureGroupList, _) = await FetchManureGroup();
            ViewBag.ManureGroupList = manureGroupList;


        }

        private async Task<(List<FarmManureTypeResponse> farmManureGroupList, Error? error)> PopulateFarmManureListAsync(OrganicManureViewModel model)
        {
            var (farmManureGroupList, error) = await FetchFarmManureGroup(model.FarmId.Value);

            if (error == null && farmManureGroupList.Any())
            {
                var selectListItems = ToSelectList(
                    farmManureGroupList,
                    f => f.ID.ToString(),
                    f => f.ManureTypeName
                );

                ViewBag.FarmManureTypeList = selectListItems;
            }

            return (farmManureGroupList, error);
        }
        private static bool IsOtherManureType(int? manureId)
        {
            return manureId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials
                || manureId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials;
        }
        private static bool IsOtherManure(OrganicManureViewModel model)
        {
            return IsOtherManureType(model.ManureTypeId);
        }
        private static ManureType? GetAndApplyManureType(OrganicManureViewModel model, List<ManureType> manureTypeList, Error? error)
        {
            if (error != null || manureTypeList.Count == 0)
            {
                model.ManureTypeName = string.Empty;
                return null;
            }

            ManureType? manureType = manureTypeList
                .FirstOrDefault(x => x.Id == model.ManureTypeId);

            ApplyManureTypeName(model, manureType);

            if (manureType != null)
            {
                model.ManureTypeName = manureType.Name;
            }

            return manureType;
        }
        [HttpGet]
        public async Task<IActionResult> ManureApplyingDate()
        {
            _logger.LogTrace($"Organic Manure Controller : ManureApplyingDate() action called");
            if (!TryGetSessionModel(nameof(ManureApplyingDate), out var model, out var redirect))
            {
                return redirect;
            }
            try
            {
                if (IsOtherManureType(model.ManureGroupIdForFilter))
                {
                    return View(model);
                }
                model = await PrepareManureApplyingDateViewModelAsync(model);
                if (model.FieldList.Count == 1)
                {
                    Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(model.FieldList[0]));
                    model.FieldName = field.Name;
                }
                model.IsWarningMsgNeedToShow = false;
                model.IsClosedPeriodWarning = false;
                model.IsApplicationJulyToSeptWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureApplyingDate() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                FarmResponse? farm = new FarmResponse();
                Error error = new Error();
                AddErrorIfNull(model.ApplicationDate, _applicationDateKey, Resource.MsgEnterADateBeforeContinuing);
                ValidateManureApplyigDate(model);

                if (!ModelState.IsValid)
                {
                    model = await PrepareManureApplyingDateViewModelAsync(model);
                    return View(model);
                }

                //check for closed period warning.

                CheckClosedPeriodWarningForManureApplyingDate(model);

                if (model.FieldList.Count >= 1)
                {
                    model.IsWithinNVZ = await IsAnyFieldWithinNVZ(model.FieldList);
                    (farm, error) = await GetFarmAsync(model.EncryptedFarmId);
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        TempData[_manureApplyingDateError] = error.Message;
                        return View(model);
                    }
                    else if (farm != null)
                    {
                        (model, error) = await ProcessNVZClosedPeriodWarningAsync(model, farm);
                    }
                }

                (bool flowControl, IActionResult? value) = BindPropertiesForManureApplyingDate(model);
                if (!flowControl && value != null)
                {
                    return value;
                }

                if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange) && (!model.IsAnyChangeInField))
                {
                    return RedirectForManureApplyingDateIfCheckAsnwerTrue(model);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction("ApplicationMethod");
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureApplyingDate() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        private IActionResult RedirectForManureApplyingDateIfCheckAsnwerTrue(OrganicManureViewModel model)
        {
            if (model.IsApplicationDateChange.HasValue && model.IsApplicationDateChange.Value)
            {
                model.MoistureType = null;
                model.SoilDrainageEndDate = null;
                model.TotalRainfall = null;
                model.AutumnCropNitrogenUptake = null;
                model.AutumnCropNitrogenUptakes = null;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_conditionsAffectingNutrients);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_checkAnswer);
        }

        private void CheckClosedPeriodWarningForManureApplyingDate(OrganicManureViewModel model)
        {
            OrganicManureViewModel organicManureViewModel = GetOrganicManureFromSession();
            if (model.ApplicationDate != organicManureViewModel.ApplicationDate)
            {
                model.IsWarningMsgNeedToShow = false;
                model.IsApplicationDateChange = true;
            }

            model.IsClosedPeriodWarning = false;
            model.IsApplicationJulyToSeptWarning = false;
            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
        }

        private (bool flowControl, IActionResult? value) BindPropertiesForManureApplyingDate(OrganicManureViewModel model)
        {
            if (model.IsClosedPeriodWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks || model.IsApplicationJulyToSeptWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    return (flowControl: false, value: View(model));
                }
            }
            else
            {
                model.IsWarningMsgNeedToShow = false;
                model.IsClosedPeriodWarning = false;
                model.IsApplicationJulyToSeptWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
            }

            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.ApplicationDate = model.ApplicationDate.Value;
                }
            }

            return (flowControl: true, value: null);
        }

        private void ValidateManureApplyigDate(OrganicManureViewModel model)
        {
            if (model.ApplicationDate != null && (model.ApplicationDate.Value.Date.Year > model.HarvestYear + 2 || model.ApplicationDate.Value.Date.Year < model.HarvestYear - 2))
            {
                ModelState.AddModelError(_applicationDateKey, Resource.MsgEnterADateWithin2YearsOfTheHarvestYear);
            }

            DateTime minDate = new DateTime(model.HarvestYear.Value - 1, 8, 01, 0, 0, 0, DateTimeKind.Local);
            DateTime maxDate = new DateTime(model.HarvestYear.Value, 7, 31, 0, 0, 0, DateTimeKind.Local);

            if (model.ApplicationDate > maxDate)
            {
                ModelState.AddModelError(_applicationDateKey, string.Format(Resource.MsgManureApplicationMaxDate, model.HarvestYear.Value, maxDate.Date.ToString("dd MMMM yyyy")));
            }
            if (model.ApplicationDate < minDate)
            {
                ModelState.AddModelError(_applicationDateKey, string.Format(Resource.MsgManureApplicationMinDate, model.HarvestYear.Value, minDate.Date.ToString("dd MMMM yyyy")));
            }
        }

        private async Task SetClosedPeriodUIAsync(OrganicManureViewModel model)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));

            int cropTypeId = cropsResponse
                .Where(x => x.Year == model.HarvestYear)
                .Select(x => x.CropTypeID)
                .FirstOrDefault() ?? 0;

            var (cropTypeLinkingResponse, _) = await _organicManureLogic
                .FetchCropTypeLinkingByCropTypeId(cropTypeId);

            string formattedStartDate = model.ClosedPeriodStartDate?.ToString("d MMMM yyyy");
            string formattedEndDate = model.ClosedPeriodEndDate?.ToString("d MMMM yyyy");

            if (cropTypeLinkingResponse.NMaxLimitEngland != 0)
            {
                model.ClosedPeriodForUI = $"{formattedStartDate} to {formattedEndDate}";
            }
        }
        private async Task<(FarmResponse? farm, Error? error)> GetFarmAsync(string encryptedFarmId)
        {
            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedFarmId));
            return await _farmLogic.FetchFarmByIdAsync(farmId);
        }
        private async Task<bool> IsAnyFieldWithinNVZ(List<string> fieldList)
        {
            foreach (var fieldId in fieldList)
            {
                var field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId));
                if (field != null && field.IsWithinNVZ == true)
                {
                    return true;
                }
            }
            return false;
        }
        private async Task PopulateManureApplyingDateModel(OrganicManureViewModel model)
        {
            List<ManureType> manureTypeList = new List<ManureType>();
            Error? error = null;
            if (model.FarmRB209CountryID.HasValue && model.ManureGroupIdForFilter.HasValue)
            {
                (manureTypeList, error) = await FetchManureTypeList(model.ManureGroupIdForFilter.Value, model.FarmRB209CountryID.Value);
            }
            model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;


            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
            (FarmResponse? farm, Error? farmError) = await _farmLogic.FetchFarmByIdAsync(farmId);
            if (farmError != null && !string.IsNullOrWhiteSpace(farmError.Message))
            {
                TempData["Error"] = farmError.Message;
            }

            var manureType = GetAndApplyManureType(model, manureTypeList, error);
            bool isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
            model.HighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen;
            ViewBag.HighReadilyAvailableNitrogen = isHighReadilyAvailableNitrogen;

            if (farm != null)
            {
                string? closedPeriod = await GetClosedPeriod(model, farm, isHighReadilyAvailableNitrogen);
                ViewBag.ClosedPeriod = closedPeriod;
            }
        }
        private async Task<string?> GetClosedPeriod(OrganicManureViewModel model, Farm farm, bool? isHighReadilyAvailableNitrogen)
        {
            (FieldDetailResponse fieldDetail, _) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(
                    Convert.ToInt32(model.FieldList?[0]), model.HarvestYear ?? 0, false);
            bool isPerennial = false;
            int fieldId = Convert.ToInt32(model.FieldList?.FirstOrDefault());
            var (cropTypeResponse, cropTypeError) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear ?? 0, false);
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(fieldId);
            int cropTypeId = cropsResponse.FirstOrDefault(x => x.Year == model.HarvestYear)?.CropTypeID ?? 0;
            int finalCropTypeId = (farm.RegisteredOrganicProducer == false && isHighReadilyAvailableNitrogen == true && cropTypeError == null)
                ? cropTypeResponse.CropTypeId : cropTypeId;
            isPerennial = await _cropLogic.FetchIsPerennialByCropTypeId(finalCropTypeId);

            var request = BuildOrganicClosedPeriodRequest(fieldDetail, model, farm, cropTypeResponse, cropTypeId, isPerennial);

            (string? closedPeriod, Error? error) = await _organicManureLogic.FetchOrganicManureClosedPeriod(request);
            if (error == null)
            {
                return closedPeriod;
            }
            return null;
        }


        private void BindIsApplicationMethodChange(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange))
            {
                model.IsApplicationMethodChange = true;
            }
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
        }

        private (bool flowControl, IActionResult? value) RedirectForCheckAnswerApplicationMethod(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer)
            {
                OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();

                if (organicManureViewModel == null)
                {
                    return (flowControl: false, value: RedirectToAction(_farmList, "Farm"));
                }
                if (IsDeepAndShallowInjection(organicManureViewModel))
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
                if (!(model.IsFieldGroupChange) && (!model.IsAnyChangeInField) && (!model.IsManureTypeChange))
                {
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    return (flowControl: false, value: RedirectToAction(_checkAnswer));
                }
            }
            return (flowControl: true, value: null);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationMethod() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error? error = null;
            try
            {
                if (HttpContext.Session.Keys.Contains(_organicManureSessionKey))
                {
                    model = GetOrganicManureFromSession();
                }
                else
                {
                    return RedirectToAction(_farmList, "Farm");
                }

                List<ManureType> manureTypeList = new List<ManureType>();
                (manureTypeList, error) = await GetManureTypeList(model);

                var manureType = GetAndApplyManureType(model, manureTypeList, error);
                bool isLiquid = manureType?.IsLiquid ?? false;
                List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

                (List<ApplicationMethodResponse> applicationMethodList, error) = await _mannerLogic.FetchApplicationMethodList(fieldType ?? 0, isLiquid);
                if (applicationMethodList.Count > 0)
                {
                    ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(a => a.SortOrder).ToList();
                }

                model.ApplicationMethodCount = applicationMethodList.Count;
                if (applicationMethodList.Count == 1)
                {
                    model.ApplicationMethod = applicationMethodList[0].ID;
                    (model.ApplicationMethodName, error) = await _mannerLogic.FetchApplicationMethodById(model.ApplicationMethod.Value);
                    if (error != null)
                    {
                        TempData[_manureApplyingDateError] = error.Message;
                        return RedirectToAction(_manureApplyingDateAction, model);
                    }

                    if (model.OrganicManures.Count > 0)
                    {
                        foreach (var orgManure in model.OrganicManures)
                        {
                            orgManure.ApplicationMethodID = model.ApplicationMethod.Value;
                        }
                    }

                    (bool flowControl, IActionResult? value) = RedirectForCheckAnswerApplicationMethod(model);
                    if (!flowControl && value != null)
                    {
                        return value;
                    }


                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);


                    return RedirectToAction("DefaultNutrientValues");
                }
                BindIsApplicationMethodChange(model);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ApplicationMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return RedirectToAction(_manureApplyingDateAction);
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationMethod(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationMethod() post action called");
            Error? error = null;
            AddErrorIfNull(model.ApplicationMethod, "ApplicationMethod", Resource.MsgSelectAnOptionBeforeContinuing);
            try
            {
                List<ManureType> manureTypeList = new List<ManureType>();
                (manureTypeList, error) = await GetManureTypeList(model);

                if (!ModelState.IsValid)
                {
                    var manureType = GetAndApplyManureType(model, manureTypeList, error);
                    bool isLiquid = manureType?.IsLiquid ?? false;
                    List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                    var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();


                    (List<ApplicationMethodResponse> applicationMethodList, error) = await _mannerLogic.FetchApplicationMethodList(fieldType ?? 0, isLiquid);
                    ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(a => a.SortOrder).ToList();
                    model.ApplicationMethodCount = applicationMethodList.Count;
                    return View(model);
                }

                if (model.OrganicManures?.Count > 0)
                {
                    model.OrganicManures.ForEach(x =>
                    {
                        x.ApplicationMethodID = model.ApplicationMethod.Value;
                    });
                }

                (model.ApplicationMethodName, error) = await _mannerLogic.FetchApplicationMethodById(model.ApplicationMethod.Value);

                if ((model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.DeepInjection2530cm) || (model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.ShallowInjection57cm))
                {
                    string applicableFor = Resource.lblNull;

                    (bool flowControl, IActionResult? value) = await BindIncorporationMethodForApplicationMethod(model, error, applicableFor);
                    if (!flowControl && value != null)
                    {
                        return value;
                    }
                }
                else
                {
                    if (!TryGetSessionModel(nameof(ApplicationMethod), out var organicManureViewModel, out var redirect))
                    {
                        return redirect;
                    }
                    ResetIncorporationMethodAndDelay(model, organicManureViewModel);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                if (model.IsCheckAnswer && model.IsApplicationMethodChange)
                {
                    return RedirectToAction(_incorporationMethodAction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ApplicationMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_applicationMethodErrorKey] = ex.Message;
                return ViewBag(model);
            }
            return RedirectToAction("DefaultNutrientValues");
        }

        private static void ResetIncorporationMethodAndDelay(OrganicManureViewModel model, OrganicManureViewModel organicManureViewModel)
        {
            if (IsDeepAndShallowInjection(organicManureViewModel))
            {
                model.IncorporationDelay = null;
                model.IncorporationMethod = null;
                model.IncorporationDelayName = string.Empty;
                model.IncorporationMethodName = string.Empty;
            }
        }

        private async Task<(bool flowControl, IActionResult? value)> BindIncorporationMethodForApplicationMethod(OrganicManureViewModel model, Error? error, string applicableFor)
        {
            (List<IncorporationMethodResponse> incorporationMethods, error) = await _mannerLogic.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableFor);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_applicationMethodErrorKey] = error.Message;
                return (flowControl: false, value: View(model));
            }

            if (incorporationMethods.Count == 1)
            {
                model.IncorporationMethod = incorporationMethods[0].ID;
                (model.IncorporationMethodName, error) = await _mannerLogic.FetchIncorporationMethodById(model.IncorporationMethod.Value);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_applicationMethodErrorKey] = error.Message;
                    return (flowControl: false, value: View(model));
                }

                (bool flowControl, IActionResult? value) = await BindIncorporationDelayForApplicationMethod(model, error, applicableFor);
                if (!flowControl)
                {
                    return (flowControl: false, value: value);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }

            return (flowControl: true, value: null);
        }

        private async Task<(bool flowControl, IActionResult? value)> BindIncorporationDelayForApplicationMethod(OrganicManureViewModel model, Error? error, string applicableFor)
        {
            (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _mannerLogic.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_applicationMethodErrorKey] = error.Message;
                return (flowControl: false, value: View(model));
            }
            else
            {
                model.IncorporationDelay = incorporationDelaysList[0].ID;
                (model.IncorporationDelayName, error) = await _mannerLogic.FetchIncorporationDelayById(model.IncorporationDelay.Value);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_applicationMethodErrorKey] = error.Message;
                    return (flowControl: false, value: View(model));
                }

                if (model.OrganicManures?.Count > 0)
                {
                    model.OrganicManures.ForEach(x =>
                    {
                        x.IncorporationMethodID = model.IncorporationMethod.Value;
                        x.IncorporationDelayID = model.IncorporationDelay.Value;
                    });
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    if (model.IsCheckAnswer && model.IsApplicationMethodChange && (!model.IsAnyChangeInField) && (!model.IsManureTypeChange))
                    {
                        return (flowControl: false, value: RedirectToAction(_checkAnswer));
                    }
                }

            }

            return (flowControl: true, value: null);
        }

        private static FarmManureTypeResponse? GetFarmManure(List<FarmManureTypeResponse> list, int? manureTypeId, string? manureTypeName)
        {
            return list.FirstOrDefault(x =>
                x.ManureTypeID == manureTypeId &&
                x.ManureTypeName == manureTypeName);
        }

        private static void ApplyFarmManureValues(OrganicManureViewModel model, FarmManureTypeResponse farmManure)
        {
            CopyFarmManureToManureNutrientValues(model.ManureType, farmManure);
            model.DefaultFarmManureValueDate = farmManure.ModifiedOn ?? farmManure.CreatedOn;
        }

        private async Task SetManureTypeIfAvailable(OrganicManureViewModel model)
        {
            if (model.ManureTypeId == null) return;

            var (manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (error == null && manureType != null)
            {
                model.ManureType = manureType;
            }
        }



        private void BindFarmManureValues(OrganicManureViewModel model, FarmManureTypeResponse? farmManure)
        {
            if (model.IsDefaultValueChange)
            {
                model.IsDefaultValueChange = false;

                if (farmManure != null)
                {
                    ApplyFarmManureValues(model, farmManure);
                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                }
            }
            else if (farmManure != null)
            {
                model.DefaultFarmManureValueDate = farmManure.ModifiedOn ?? farmManure.CreatedOn;
                ViewBag.FarmManureApiOption = Resource.lblTrue;
                ViewBagForDefaultOrStandardValue(model, farmManure);
            }
        }

        private static void BindIsDefaultNutrientOptionChange(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer &&
                                !model.IsApplicationMethodChange &&
                                !model.IsFieldGroupChange &&
                                !model.IsManureTypeChange &&
                                !model.IsIncorporationMethodChange)
            {
                model.IsDefaultNutrientOptionChange = true;
            }
        }

        [HttpGet]
        public async Task<IActionResult> DefaultNutrientValues()
        {
            _logger.LogTrace($"Organic Manure Controller : DefaultNutrientValues() action called");

            var model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            try
            {
                BindIsDefaultNutrientOptionChange(model);

                var (farmManureList, error) =
                    await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                var farmManure = GetFarmManure(farmManureList, model.ManureTypeId,
                    IsOtherManureType(model.ManureGroupIdForFilter)
                        ? model.OtherMaterialName
                        : model.ManureTypeName);

                if (IsOtherManureType(model.ManureTypeId))
                {
                    if (!IsOtherManureType(model.ManureGroupIdForFilter))
                    {
                        model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
                        HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                        return RedirectToAction("ManualNutrientValues");
                    }

                    if (farmManure != null)
                        ApplyFarmManureValues(model, farmManure);
                    else
                        await SetManureTypeIfAvailable(model);
                }
                else
                {
                    await SetManureTypeIfAvailable(model);

                    if (error == null && farmManureList.Any())
                    {
                        BindFarmManureValues(model, farmManure);
                    }
                }

                model.IsDefaultNutrient = true;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefaultNutrientValues(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : DefaultNutrientValues() post action called");

            AddErrorIfNull(model.DefaultNutrientValue, "DefaultNutrientValue",
                Resource.MsgSelectAnOptionBeforeContinuing);

            try
            {
                var (farmManureList, _) =
                    await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                var farmManure = GetFarmManure(farmManureList, model.ManureTypeId, model.ManureTypeName);

                if (!ModelState.IsValid)
                {
                    await SetManureTypeIfAvailable(model);
                    BindFarmManureDataForModelStateInvalidForDefaultNutrient(ref model, farmManureList, ref farmManure);

                    return View(model);
                }

                // ✅ Manual entry
                if (model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
                {
                    if (model.DryMatterPercent == null)
                        BindNutrientsFromManureType(model);

                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    return RedirectToAction("ManualNutrientValues");
                }

                // ✅ Reset nutrients
                model.DryMatterPercent = model.N = model.P2O5 = model.NH4N =
                model.UricAcid = model.SO3 = model.K2O = model.MgO = model.NO3N = null;
                bool flowControl = false;
                IActionResult? value = null;
                OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();
                (flowControl, value) = await HandleDefaultNutrientValues(model, farmManure, organicManureViewModel);
                if (!flowControl && value != null)
                {
                    return value;
                }

                if (model.OrganicManures?.Count > 0)
                {
                    UpdateOrganicManuresFromModel(model, model.ManureType);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                (flowControl, value) = RedirectIfCheckAnswerForDefaultNutrientValues(model);
                if (!flowControl && value != null)
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Exception in POST DefaultNutrientValues");
                ViewBag.Error = ex.Message;
                return View(model);
            }

            return RedirectToAction(_applicationRateMethodAction);
        }

        private async Task<(bool flowControl, IActionResult? value)> HandleDefaultNutrientValues(OrganicManureViewModel model, FarmManureTypeResponse? farmManure, OrganicManureViewModel? organicManureViewModel)
        {
            bool flowControl = false;
            IActionResult? value = null;
            bool hasDefaultNutrientValue = !string.IsNullOrWhiteSpace(model.DefaultNutrientValue);
            if (!string.IsNullOrWhiteSpace(organicManureViewModel?.DefaultNutrientValue))
            {
                (flowControl, value) = await ProcessNutrientValueOptionAsync(model, farmManure, organicManureViewModel);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: value);
                }
            }
            else
            {
                if (hasDefaultNutrientValue && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                {
                    await HandleDefaultNutrientValueLogicIfSelectYesToDefault(model, farmManure);
                }
                else
                {
                    (ManureType manureType, _) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    model.ManureType = manureType;
                    if (hasDefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                    {
                        model.IsThisDefaultValueOfRB209 = true;
                        ViewBag.RB209ApiOption = Resource.lblTrue;
                        HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                        return (flowControl: false, value: View(model));
                    }

                }
            }

            return (flowControl: true, value: null);
        }

        private async Task<(bool flowControl, IActionResult? value)> ProcessNutrientValueOptionAsync(OrganicManureViewModel model, FarmManureTypeResponse? farmManure, OrganicManureViewModel organicManureViewModel)
        {
            bool flowControl = false; IActionResult? value = null;
            if (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes)
            {
                (flowControl, value) = BindDataIfWeSelectDefaultValueOption(model, farmManure, organicManureViewModel);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: value);
                }
            }
            else
            {
                await SetManureTypeIfAvailable(model);

                model.IsThisDefaultValueOfRB209 = true;
                (flowControl, value) = BindRB209ApiOptionViewBeg(model, organicManureViewModel);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: value);
                }
            }

            return (flowControl: true, value: null);
        }

        private async Task HandleDefaultNutrientValueLogicIfSelectYesToDefault(OrganicManureViewModel model, FarmManureTypeResponse? farmManure)
        {
            (List<FarmManureTypeResponse> farmManureTypeList, _) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
            if (farmManureTypeList.Count > 0)
            {
                if (farmManure != null)
                {
                    CopyFarmManureToManureNutrientValues(model.ManureType, farmManure);
                }
                if (model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                {
                    model.IsThisDefaultValueOfRB209 = false;
                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                }
            }
        }

        private (bool flowControl, IActionResult value) BindRB209ApiOptionViewBeg(OrganicManureViewModel model, OrganicManureViewModel organicManureViewModel)
        {
            if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
            {
                ViewBag.RB209ApiOption = Resource.lblTrue;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (organicManureViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || organicManureViewModel.DefaultNutrientValue != Resource.lblYesUseTheseValues)
                      && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                {
                    return (flowControl: false, value: View(model));
                }

            }
            if (organicManureViewModel.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
            {
                ViewBag.RB209ApiOption = Resource.lblTrue;
            }

            return (flowControl: true, value: null);
        }

        private (bool flowControl, IActionResult? value) BindDataIfWeSelectDefaultValueOption(OrganicManureViewModel model, FarmManureTypeResponse? farmManure, OrganicManureViewModel organicManureViewModel)
        {
            if (farmManure != null)
            {
                CopyFarmManureToManureNutrientValues(model.ManureType, farmManure);
            }

            model.IsThisDefaultValueOfRB209 = false;
            if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
            {
                if (farmManure != null)
                {
                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                (bool isSuccess, IActionResult? action) = RedirectToDefaultNutrientValues(model, organicManureViewModel);
                if (!isSuccess && action != null)
                {
                    return (flowControl: false, value: action);
                }
            }

            return (flowControl: true, value: null);
        }

        private (bool flowControl, IActionResult? value) RedirectToDefaultNutrientValues(OrganicManureViewModel model, OrganicManureViewModel organicManureViewModel)
        {
            if (organicManureViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (organicManureViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || organicManureViewModel.DefaultNutrientValue != Resource.lblYesUseTheseStandardNutrientValues)
                && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
            {
                return (flowControl: false, value: View(model));
            }

            return (flowControl: true, value: null);
        }

        private void BindFarmManureDataForModelStateInvalidForDefaultNutrient(ref OrganicManureViewModel model, List<FarmManureTypeResponse> farmManureList, ref FarmManureTypeResponse? farmManure)
        {
            if (IsOtherManureType(model.ManureTypeId))
            {
                (farmManure, model) = BindDefaultNutrientValuesIfManureIsOther(model, farmManureList, farmManure);
            }
            else if (farmManureList.Count > 0)
            {
                BindFarmManureApiOption(model, farmManure);
            }
        }

        private (bool flowControl, IActionResult? value) RedirectIfCheckAnswerForDefaultNutrientValues(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer &&
                model.IsDefaultNutrientOptionChange &&
                !model.IsApplicationMethodChange &&
                !model.IsFieldGroupChange &&
                !model.IsManureTypeChange &&
                !model.IsIncorporationMethodChange &&
                !model.IsAnyChangeInField)
            {
                return (flowControl: false, value: RedirectToAction(_checkAnswer));
            }

            return (flowControl: true, value: null);
        }

        private void BindFarmManureApiOption(OrganicManureViewModel model, FarmManureTypeResponse? farmManure)
        {
            if (model.IsDefaultValueChange || string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
            {
                model.IsDefaultValueChange = false;

                if (farmManure != null)
                {
                    ApplyFarmManureValues(model, farmManure);
                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                }
            }
            else if (farmManure != null)
            {
                ViewBagForDefaultOrStandardValue(model, farmManure);
            }
        }

        private static (FarmManureTypeResponse?, OrganicManureViewModel) BindDefaultNutrientValuesIfManureIsOther(OrganicManureViewModel model, List<FarmManureTypeResponse> farmManureList, FarmManureTypeResponse? farmManure)
        {
            if (IsOtherManureType(model.ManureGroupIdForFilter))
            {
                farmManure = GetFarmManure(farmManureList,
                    model.ManureTypeId, model.OtherMaterialName);

                if (farmManure != null)
                    ApplyFarmManureValues(model, farmManure);

                model.IsDefaultNutrient = true;
            }
            else
            {
                model.DefaultNutrientValue =
                    Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
            }

            return (farmManure, model);
        }

        private void ViewBagForDefaultOrStandardValue(OrganicManureViewModel model, FarmManureTypeResponse? farmManure)
        {
            if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseValues) || (model.IsThisDefaultValueOfRB209 != null && (!model.IsThisDefaultValueOfRB209.Value)))
            {
                ViewBag.FarmManureApiOption = Resource.lblTrue;

                ApplyFarmManureValues(model, farmManure);
            }
            else if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues) || (model.IsThisDefaultValueOfRB209 != null && (model.IsThisDefaultValueOfRB209.Value)))
            {
                ViewBag.FarmManureApiOption = null;
                ViewBag.RB209ApiOption = Resource.lblTrue;
            }
        }


        [HttpGet]
        public IActionResult ManualNutrientValues()
        {
            _logger.LogTrace($"Organic Manure Controller : ManualNutrientValues() post action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManualNutrientValues(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManualNutrientValues() post action called");
            try
            {
                if (!ModelState.IsValid)
                {
                    ReplaceNumericError(_dryMatterPercentKey, Resource.lblDryMatterPercent, Resource.lblDryMatter);
                    ReplaceNumericError("N", Resource.lblN, Resource.lblTotalNitrogen);
                    ReplaceNumericError("NH4N", Resource.lblNH4N, Resource.lblAmmonium);
                    ReplaceNumericError("UricAcid", Resource.lblUricAcidForError, Resource.lblUricAcid);
                    ReplaceNumericError("NO3N", Resource.lblNO3N, Resource.lblNitrogen);
                    ReplaceNumericError("P2O5", Resource.lblP2O5, Resource.lblTotalPhosphate);
                    ReplaceNumericError("K2O", Resource.lblK2O, Resource.lblTotalPotassium);
                    ReplaceNumericError("SO3", Resource.lblSO3, Resource.lblTotalSulphur);
                    ReplaceNumericError("MgO", Resource.lblMgO, Resource.lblMagnesiumMgO);
                }

                AddErrorIfNull(model.DryMatterPercent, _dryMatterPercentKey, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblDryMatter.ToLower()));

                AddErrorIfNull(model.N, "N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblTotalNitrogen.ToLower()));

                AddErrorIfNull(model.NH4N, "NH4N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAmmoniumForError));

                AddErrorIfNull(model.UricAcid, "UricAcid", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.MsgUricAcid));

                AddErrorIfNull(model.NO3N, "NO3N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblNitrateForErrorMsg));

                AddErrorIfNull(model.P2O5, "P2O5", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPhosphate.ToLower()));

                AddErrorIfNull(model.K2O, "K2O", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPotash.ToLower()));

                AddErrorIfNull(model.SO3, "SO3", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblSulphur.ToLower()));

                AddErrorIfNull(model.MgO, "MgO", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblMagnesiumMgO.ToLower()));


                ValidateNutrientValues(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                BindIsAnyNeedToStoreNutrientValueForFuture(model);

                if (model.OrganicManures?.Count > 0)
                {
                    UpdateOrganicManuresFromModel(model, null);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                (bool flowControl, IActionResult? value) = RedirectIfCheckAnswerForManualNutrientValues(model);
                if (!flowControl && value != null)
                {
                    return value;
                }

                return RedirectToAction(_applicationRateMethodAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ManualNutrientValues() post action : {Message}, {StackTrace}",
                   ex.Message, ex.StackTrace);

                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        private (bool flowControl, IActionResult? value) RedirectIfCheckAnswerForManualNutrientValues(OrganicManureViewModel model)
        {
            if (model.IsCheckAnswer && model.IsDefaultNutrientOptionChange && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
            && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange) && (!model.IsAnyChangeInField))
            {
                return (flowControl: false, value: RedirectToAction(_checkAnswer));
            }

            return (flowControl: true, value: null);
        }

        private static void BindIsAnyNeedToStoreNutrientValueForFuture(OrganicManureViewModel model)
        {
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
        }

        private void ValidateNutrientValues(OrganicManureViewModel model)
        {
            if (model.N != null && model.NH4N != null && model.UricAcid != null && model.NO3N != null)
            {
                decimal totalValue = model.NH4N.Value + model.UricAcid.Value + model.NO3N.Value;
                if (model.N < totalValue)
                {
                    ModelState.AddModelError("N", Resource.lblTotalNitrogenMustBeGreaterOrEqualToAmmoniumUricacidNitrate);
                }
            }

            ValidateDryMatter(model);

            if (model.N != null && (model.N < 0 || model.N > 297))
            {
                ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblTotalNitrogenN, 297));
            }
            ValidateNH4NUricAcidNO3NAndP2O5(model);

            ValidateK2OMgOAndSO3(model);
        }

        private void ValidateDryMatter(OrganicManureViewModel model)
        {
            if (model.DryMatterPercent != null)
            {
                if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.PigSlurry ||
                    model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.CattleSlurry)
                {
                    if (model.DryMatterPercent < 0 || model.DryMatterPercent > 25)
                    {
                        ModelState.AddModelError(_dryMatterPercentKey, string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter.ToLower(), 25));
                    }
                }
                else
                {
                    if (model.DryMatterPercent < 0 || model.DryMatterPercent > 99)
                    {
                        ModelState.AddModelError(_dryMatterPercentKey, string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter, 99));
                    }
                }
            }

        }

        private void ValidateNH4NUricAcidNO3NAndP2O5(OrganicManureViewModel model)
        {
            if (model.NH4N != null && (model.NH4N < 0 || model.NH4N > 99))
            {
                ModelState.AddModelError("NH4N", string.Format(Resource.MsgMinMaxValidation, Resource.lblAmmonium, 99));
            }

            if (model.UricAcid != null && (model.UricAcid < 0 || model.UricAcid > 99))
            {
                ModelState.AddModelError("UricAcid", string.Format(Resource.MsgMinMaxValidation, Resource.lblUricAcid, 99));
            }

            if (model.NO3N != null && (model.NO3N < 0 || model.NO3N > 99))
            {
                ModelState.AddModelError("NO3N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrate, 99));
            }

            if (model.P2O5 != null && (model.P2O5 < 0 || model.P2O5 > 99))
            {
                ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5, 99));
            }
        }

        private void ValidateK2OMgOAndSO3(OrganicManureViewModel model)
        {
            if (model.K2O != null && (model.K2O < 0 || model.K2O > 99))
            {
                ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2O, 99));
            }
            if (model.MgO != null && (model.MgO < 0 || model.MgO > 99))
            {
                ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblMagnesiumMgO, 99));
            }

            if (model.SO3 != null && (model.SO3 < 0 || model.SO3 > 99))
            {
                ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3, 99));
            }
        }

        private void ReplaceNumericError(string key, string validationLabel, string displayLabel)
        {
            if (!ModelState.ContainsKey(key) || ModelState[key].Errors.Count == 0)
            {
                return;
            }
            var errorMessage = ModelState[key].Errors[0].ErrorMessage;
            string expectedMessage = string.Format(Resource.lblEnterNumericValue, ModelState[key].RawValue, validationLabel);
            if (!string.Equals(errorMessage, expectedMessage))
            {
                return;
            }
            ModelState[key].Errors.Clear();
            ModelState[key].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, displayLabel));
        }


        [HttpGet]
        public IActionResult NutrientValuesStoreForFuture()
        {
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            if (IsOtherManure(model))
            {
                model.IsAnyNeedToStoreNutrientValueForFuture = true;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_applicationRateMethodAction);
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NutrientValuesStoreForFuture(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : NutrientValuesStoreForFuture() post action called");
            AddErrorIfNull(model.IsAnyNeedToStoreNutrientValueForFuture, "IsAnyNeedToStoreNutrientValueForFuture", Resource.MsgSelectAnOptionBeforeContinuing);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer && model.IsDefaultNutrientOptionChange && (!model.IsApplicationMethodChange) && (!model.IsFieldGroupChange)
               && (!model.IsManureTypeChange) && (!model.IsIncorporationMethodChange) && (!model.IsAnyChangeInField))
            {
                return RedirectToAction(_checkAnswer);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_applicationRateMethodAction);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationRateMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationRateMethod() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            try
            {
                if (!IsOtherManureType(model.ManureTypeId))
                {
                    List<ManureType> manureTypeList = new List<ManureType>();
                    Error? error = null;
                    (manureTypeList, error) = await GetManureTypeList(model);

                    if (error == null && manureTypeList.Count > 0)
                    {
                        var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                        ApplyManureTypeName(model, manureType);

                        model.ApplicationRateArable = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.ApplicationRateArable;
                    }
                    else
                    {
                        model.ManureTypeName = string.Empty;
                        ViewBag.Error = error?.Message;
                    }
                }


                (List<CommonResponse> manureGroupList, Error error1) = await _mannerLogic.FetchManureGroupList();
                model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
                if (error1 != null && (!string.IsNullOrWhiteSpace(error1.Message)))
                {
                    ViewBag.Error = error1.Message;
                }
                ResetWarnings(model, true);
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return RedirectToAction("DefaultNutrientValues");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ApplicationRateMethod() post action called");
            try
            {
                Error? error = null;
                AddErrorIfNull(model.ApplicationRateMethod, _applicationRateMethodAction, Resource.MsgSelectAnOptionBeforeContinuing);
                List<ManureType> manureTypeList = new List<ManureType>();
                (manureTypeList, error) = await GetManureTypeList(model);

                if (!ModelState.IsValid)
                {
                    BindApplicationRateArable(model, error, manureTypeList);
                    (List<CommonResponse> manureGroupList, _) = await _mannerLogic.FetchManureGroupList();
                    model.ManureGroupName = manureGroupList.Count > 0 ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
                    return View(_applicationRateMethodAction, model);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                bool flowControl = false; IActionResult? value = null;
                (var shouldContinue, var result, model) = await HandleApplicationRateMethodSelection(model, manureTypeList, error);

                if (!shouldContinue && result != null)
                {
                    return result;
                }
                (flowControl, value, model) = HandleWarningForApplicationRateMethod(model);
                if (!flowControl && value != null)
                {
                    return value;
                }
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_incorporationMethodAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        private async Task<(bool ShouldContinue, IActionResult? Result, OrganicManureViewModel Model)> HandleApplicationRateMethodSelection(
        OrganicManureViewModel model, List<ManureType> manureTypeList, Error? error)
        {
            switch ((NMP.Commons.Enums.ApplicationRate)model.ApplicationRateMethod.Value)
            {
                case NMP.Commons.Enums.ApplicationRate.EnterAnApplicationRate:
                    model.Area = null;
                    model.Quantity = null;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                    return (false, RedirectToAction("ManualApplicationRate"), model);

                case NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity:
                    model.ApplicationRate = null;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                    return (false, RedirectToAction("AreaQuantity"), model);

                case NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate:

                    model.ApplicationRate = manureTypeList
                        .FirstOrDefault(x => x.Id == model.ManureTypeId)?
                        .ApplicationRateArable;

                    model.Area = null;
                    model.Quantity = null;

                    if (model.OrganicManures?.Count > 0)
                    {
                        model.OrganicManures.ForEach(x =>
                        {
                            x.AreaSpread = null;
                            x.ManureQuantity = null;
                            x.ApplicationRate = model.ApplicationRate.Value;
                        });
                    }

                    ResetWarnings(model, false);

                    var (flowControl, value) = BindIsWarningMsgNeedToShow(model);

                    if (!flowControl && value != null)
                    {
                        return (false, value, model);
                    }

                    return await PrepareWarningMessageForApplicationRateMethod(
                        model,
                        error,
                        string.Empty);

                default:
                    return (true, null, model);
            }
        }
        private (bool flowControl, IActionResult value) BindIsWarningMsgNeedToShow(OrganicManureViewModel model)
        {
            OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();
            if (organicManureViewModel == null)
            {
                return (flowControl: false, value: RedirectToAction(_farmList, "Farm"));
            }
            if (model.ApplicationRateMethod != organicManureViewModel.ApplicationRateMethod)
            {
                model.IsWarningMsgNeedToShow = false;
            }

            return (flowControl: true, value: null);
        }

        private (bool flowControl, IActionResult? value, OrganicManureViewModel) HandleWarningForApplicationRateMethod(OrganicManureViewModel model)
        {
            bool hasAnyWarning = model.IsOrgManureNfieldLimitWarning || model.IsNMaxLimitWarning
                || model.IsEndClosedPeriodFebruaryWarning || model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150;

            if (hasAnyWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    return (flowControl: false, value: View(model), model);
                }
            }
            else
            {
                ResetWarnings(model, true);
            }
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return (flowControl: true, value: null, model);
        }

        private async Task<(bool flowControl, IActionResult? value, OrganicManureViewModel)> PrepareWarningMessageForApplicationRateMethod(OrganicManureViewModel model, Error? error, string message)
        {
            if (model.OrganicManures?.Count > 0)
            {
                (FarmResponse? farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                foreach (var organicManure in model.OrganicManures)
                {
                    int? fieldId = organicManure.FieldID ?? null;
                    if (fieldId != null && await GetIsFieldIsInNVZ(fieldId.Value))
                    {
                        (bool flowControl, string? errorMessage, model) = await BindWarningForApplicationRate(model, error, message, farm, organicManure, fieldId);
                        if (!flowControl && string.IsNullOrWhiteSpace(errorMessage))
                        {
                            TempData["ApplicationRateMethodError"] = errorMessage;
                            return (flowControl: false, value: View(model), model);
                        }
                    }

                }
            }

            return (flowControl: true, value: null, model);
        }

        private async Task<bool> GetIsFieldIsInNVZ(int fieldId)
        {
            bool isFieldIsInNVZ = false;
            Field field = await _fieldLogic.FetchFieldByFieldId(fieldId);
            if (field != null && field.IsWithinNVZ.HasValue)
            {
                isFieldIsInNVZ = field.IsWithinNVZ.Value;
            }
            return isFieldIsInNVZ;
        }
        private static void BindApplicationRateArable(OrganicManureViewModel model, Error? error, List<ManureType> manureTypeList)
        {
            if (error == null && manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                ApplyManureTypeName(model, manureType);
                model.ApplicationRateArable = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.ApplicationRateArable;
            }
            else
            {
                model.ManureTypeName = string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate()
        {
            _logger.LogTrace($"Organic Manure Controller : ManualApplicationRate() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            try
            {
                model = GetOrganicManureFromSession();
                if (model == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }


                List<ManureType> manureTypeList = new List<ManureType>();
                Error? error = null;
                (manureTypeList, error) = await GetManureTypeList(model);

                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                ApplyManureTypeName(model, manureType);

                (List<CommonResponse> manureGroupList, Error error1) = await _mannerLogic.FetchManureGroupList();
                model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;
                ResetWarnings(model, true);
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManualApplicationRate() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualApplicationRate(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManualApplicationRate() post action called");
            try
            {
                if (!ModelState.IsValid)
                {
                    ReplaceNumericError(_applicationRateKey, Resource.lblApplicationRate, string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblRate));
                }
                ValidateApplicationRate(model);

                if (!ModelState.IsValid)
                {
                    return View("ManualApplicationRate", model);
                }
                ResetWarnings(model, false);

                OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();
                if (organicManureViewModel == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }

                if (model.ApplicationRate != organicManureViewModel.ApplicationRate)
                {
                    model.IsWarningMsgNeedToShow = false;
                }

                IActionResult? earlyResult;
                (earlyResult, model) = await ProcessFieldWarningsAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                if (HasAnyWarning(model))
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                        return View(model);
                    }
                }
                else
                {
                    ResetWarnings(model, true);
                }

                FinalizeApplicationRate(model);

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange) && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswer);
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogTrace(hre, "Organic Manure Controller : Exception in ManualApplicationRate() post action : {Message}, {StackTrace}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManualApplicationRate() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["ManualApplicationRateError"] = ex.Message;
                return View(model);
            }

            return RedirectToAction(_incorporationMethodAction);
        }

        // Handles the per-field NVZ warning binding loop that used to live inline.
        // Returns a non-null EarlyResult when the caller should return immediately (mirrors original early-return behavior).
        private async Task<(IActionResult? EarlyResult, OrganicManureViewModel Model)> ProcessFieldWarningsAsync(OrganicManureViewModel model)
        {
            if (model.OrganicManures == null || model.OrganicManures.Count == 0)
            {
                return (null, model);
            }

            Error? error;
            FarmResponse? farm;
            (farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);

            string message = string.Empty;

            foreach (var organicManure in model.OrganicManures)
            {
                int? fieldId = organicManure.FieldID ?? null;
                if (fieldId == null)
                {
                    continue;
                }

                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
                if (field == null)
                {
                    continue;
                }

                bool isFieldIsInNVZ = field.IsWithinNVZ != null && field.IsWithinNVZ.Value;
                if (!isFieldIsInNVZ)
                {
                    continue;
                }

                bool flowControl;
                string? errorMessage;
                (flowControl, errorMessage, model) = await BindWarningForApplicationRate(model, error, message, farm, organicManure, fieldId);

                if (!flowControl && string.IsNullOrWhiteSpace(errorMessage))
                {
                    TempData["ManualApplicationRateError"] = errorMessage;
                    return (View(model), model);
                }
            }

            return (null, model);
        }

        private static bool HasAnyWarning(OrganicManureViewModel model)
        {
            return model.IsOrgManureNfieldLimitWarning
                || model.IsNMaxLimitWarning
                || model.IsEndClosedPeriodFebruaryWarning
                || model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150;
        }

        // Clears transient area/quantity fields and applies the confirmed application rate.
        private static void FinalizeApplicationRate(OrganicManureViewModel model)
        {
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
        }

        private async Task<(bool flowControl, string? value, OrganicManureViewModel)> BindWarningForApplicationRate(OrganicManureViewModel model, Error? error, string message, FarmResponse farm, OrganicManureDataViewModel organicManure, int? fieldId)
        {
            (model, error) = await IsNFieldLimitWarningMessage(model, organicManure.ManagementPeriodID, Convert.ToInt32(fieldId), farm);
            if (error != null)
            {
                return (flowControl: false, value: error.Message, model);
            }
            (FieldDetailResponse fieldDetail, error) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(fieldId.Value, model.HarvestYear.Value, false);
            if (error != null)
            {
                return (flowControl: false, value: error.Message, model);
            }
            (model, error) = await IsNMaxWarningMessage(model, Convert.ToInt32(fieldId), organicManure.ManagementPeriodID, false, farm, fieldDetail, organicManure);
            if (error != null)
            {
                return (flowControl: false, value: error.Message, model);
            }
            (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(organicManure.ManagementPeriodID);

            if (!(IsOtherManureType(model.ManureTypeId)))
            {
                (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, farm, managementPeriod.CropID.Value, fieldId.Value);

            }
            //Closed period and maximum application rate for high N organic manure on a registered organic farm message - Max Application Rate - Warning Message
            if (!(IsOtherManureType(model.ManureTypeId)))
            {
                (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, message, error) = await IsClosedPeriodStartAndEndFebExceedNRateException(model, Convert.ToInt32(fieldId), farm, organicManure.ManagementPeriodID);
                if (error != null)
                {
                    return (flowControl: false, value: error.Message, model);
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    TempData["AppRateExceeds150WithinClosedPeriodOrganic"] = message;
                }
            }

            return (flowControl: true, value: null, model);
        }

        [HttpGet]
        public IActionResult AreaQuantity()
        {
            _logger.LogTrace("Organic Manure Controller : AreaQuantity() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            ResetWarnings(model, true);
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AreaQuantity(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : AreaQuantity() post action called");

            ValidateAreaQuantity(model);
            if (!ModelState.IsValid)
            {
                return View("AreaQuantity", model);
            }

            model.ApplicationRate = Math.Round((model.Quantity.Value / model.Area.Value), 1);
            StampAreaQuantityOnManures(model);

            ResetWarnings(model, false);

            OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();
            if (organicManureViewModel == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (model.ApplicationRate != organicManureViewModel.ApplicationRate)
            {
                model.IsWarningMsgNeedToShow = false;
            }

            IActionResult? earlyResult;
            (earlyResult, model) = await ProcessFieldWarningsForAreaQuantityAsync(model);
            if (earlyResult != null)
            {
                return earlyResult;
            }

            if (HasAnyWarning(model))
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    return View(model);
                }
            }
            else
            {
                ResetWarnings(model, true);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange) && (!model.IsAnyChangeInField))
            {
                return RedirectToAction(_checkAnswer);
            }

            return RedirectToAction(_incorporationMethodAction);
        }

        // Stamps the entered area/quantity/rate onto every organic manure entry.
        private static void StampAreaQuantityOnManures(OrganicManureViewModel model)
        {
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.AreaSpread = model.Area.Value;
                    orgManure.ManureQuantity = model.Quantity.Value;
                    orgManure.ApplicationRate = model.ApplicationRate.Value;
                }
            }
        }

        // Handles the per-field NVZ warning binding loop that used to live inline.
        // Returns a non-null EarlyResult when the caller should return immediately (mirrors original early-return behavior).
        private async Task<(IActionResult? EarlyResult, OrganicManureViewModel Model)> ProcessFieldWarningsForAreaQuantityAsync(OrganicManureViewModel model)
        {
            if (model.OrganicManures == null || model.OrganicManures.Count == 0)
            {
                return (null, model);
            }

            Error error;
            FarmResponse farm;
            (farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);

            string message = string.Empty;

            foreach (var organicManure in model.OrganicManures)
            {
                int? fieldId = organicManure.FieldID ?? null;
                if (fieldId == null)
                {
                    continue;
                }

                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
                if (field == null)
                {
                    continue;
                }

                bool isFieldIsInNVZ = field.IsWithinNVZ ?? false;
                if (!isFieldIsInNVZ)
                {
                    continue;
                }

                bool flowControl;
                string? errorMessage;
                (flowControl, errorMessage, model) = await BindWarningForApplicationRate(model, error, message, farm, organicManure, fieldId);

                if (!flowControl && string.IsNullOrWhiteSpace(errorMessage))
                {
                    TempData["AreaAndQuantityError"] = errorMessage;
                    return (View(model), model);
                }
            }

            return (null, model);
        }


        [HttpGet]
        public async Task<IActionResult> IncorporationMethod()
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationMethod() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error? error = null;
            model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            if ((model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.DeepInjection2530cm) || (model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.ShallowInjection57cm))
            {
                return RedirectToAction(_conditionsAffectingNutrients);
            }
            try
            {
                List<ManureType> manureTypeList = new List<ManureType>();
                (manureTypeList, error) = await GetManureTypeList(model);

                List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
                var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
                string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
                (List<IncorporationMethodResponse> incorporationMethods, error) = await _mannerLogic.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
                if (error == null && incorporationMethods.Count > 0)
                {
                    ViewBag.IncorporationMethod = incorporationMethods.OrderBy(i => i.SortOrder).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in IncorporationMethod() : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                if (model.ApplicationRateMethod != (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
                {
                    TempData["ManualApplicationRateError"] = ex.Message;
                    return RedirectToAction("ManualApplicationRate");
                }
                else
                {
                    ViewBag.Error = ex.Message;
                    return RedirectToAction(_applicationRateMethodAction);
                }
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationMethod(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationMethod() post action called");
            AddErrorIfNull(model.IncorporationMethod, _incorporationMethodAction, Resource.MsgSelectAnOptionBeforeContinuing);

            try
            {
                Error? error;
                List<ManureType> manureTypeList;
                (manureTypeList, error) = await GetManureTypeList(model);

                if (!ModelState.IsValid)
                {
                    return await BuildIncorporationMethodInvalidViewAsync(model);
                }

                (model.IncorporationMethodName, error) = await _mannerLogic.FetchIncorporationMethodById(model.IncorporationMethod.Value);

                ApplyIncorporationMethodToManures(model);

                if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && (!model.IsApplicationMethodChange))
                {
                    model.IsIncorporationMethodChange = true;
                }

                if (model.IncorporationMethod == (int)NMP.Commons.Enums.IncorporationMethod.NotIncorporated)
                {
                    return await HandleNotIncorporatedAsync(model, error, manureTypeList);
                }

                IActionResult? earlyResult = HandleIncorporatedSelection(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in IncorporationMethod() : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_incorporationMethodError] = ex.Message;
                return View(model);
            }

            return RedirectToAction(_incorporationDelayAction);
        }

        // Fetches applicable incorporation methods for the model's field/harvest year/application method.
        // Shared by every place in this action that needs to re-populate ViewBag.IncorporationMethod.
        private async Task<(List<IncorporationMethodResponse> Methods, Error? Error)> FetchApplicableIncorporationMethodsAsync(OrganicManureViewModel model)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();
            string applicableForArableOrGrass = fieldType == 1 ? Resource.lblA : Resource.lblG;
            return await _mannerLogic.FetchIncorporationMethodsByApplicationId(model.ApplicationMethod.Value, applicableForArableOrGrass);
        }

        // Builds the view shown when the posted model state is invalid (mirrors original: no TempData,
        // ViewBag set unconditionally).
        private async Task<IActionResult> BuildIncorporationMethodInvalidViewAsync(OrganicManureViewModel model)
        {
            (List<IncorporationMethodResponse> incorporationMethods, _) = await FetchApplicableIncorporationMethodsAsync(model);
            ViewBag.IncorporationMethod = incorporationMethods;
            return View(model);
        }

        // Builds the error view shared by every failure path below: sets the TempData error message,
        // re-fetches applicable incorporation methods for the ViewBag, and returns the view.
        private async Task<IActionResult> BuildIncorporationMethodErrorViewAsync(OrganicManureViewModel model, string errorMessage)
        {
            TempData[_incorporationMethodError] = errorMessage;

            (List<IncorporationMethodResponse> incorporationMethods, Error? error) = await FetchApplicableIncorporationMethodsAsync(model);
            if (error == null && incorporationMethods.Count > 0)
            {
                ViewBag.IncorporationMethod = incorporationMethods;
            }
            return View(model);
        }

        // Stamps the selected incorporation method onto every organic manure entry.
        private static void ApplyIncorporationMethodToManures(OrganicManureViewModel model)
        {
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.IncorporationMethodID = model.IncorporationMethod.Value;
                }
            }
        }

        // Stamps the resolved incorporation delay onto every organic manure entry.
        private static void ApplyIncorporationDelayToManures(OrganicManureViewModel model)
        {
            if (model.OrganicManures.Count > 0)
            {
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.IncorporationDelayID = model.IncorporationDelay.Value;
                }
            }
        }

        // Handles the "Not Incorporated" branch: resolves the incorporation delay (if applicable) and
        // decides the final redirect/view, mirroring the original nested if/else-if/else logic exactly.
        private async Task<IActionResult> HandleNotIncorporatedAsync(OrganicManureViewModel model, Error? error, List<ManureType> manureTypeList)
        {
            if (error == null && manureTypeList.Count > 0)
            {
                string applicableFor = Resource.lblNull;
                List<IncorprationDelaysResponse> incorporationDelaysList;
                (incorporationDelaysList, error) = await _mannerLogic.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);

                if (error == null && incorporationDelaysList.Count == 1)
                {
                    model.IncorporationDelay = incorporationDelaysList[0].ID;
                    (model.IncorporationDelayName, error) = await _mannerLogic.FetchIncorporationDelayById(model.IncorporationDelay.Value);

                    if (error == null)
                    {
                        ApplyIncorporationDelayToManures(model);
                    }
                    else
                    {
                        return await BuildIncorporationMethodErrorViewAsync(model, error.Message);
                    }

                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                }
                else if (error != null)
                {
                    return await BuildIncorporationMethodErrorViewAsync(model, error.Message);
                }
            }
            else if (error != null)
            {
                return await BuildIncorporationMethodErrorViewAsync(model, error.Message);
            }

            if (model.IsCheckAnswer && (!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && (!model.IsAnyChangeInField))
            {
                return RedirectToAction(_checkAnswer);
            }

            return RedirectToAction(_conditionsAffectingNutrients);
        }

        // Handles the branch taken when an incorporation method other than "Not Incorporated" is selected.
        // Returns a non-null result when the caller should return immediately (session missing).
        private IActionResult? HandleIncorporatedSelection(OrganicManureViewModel model)
        {
            OrganicManureViewModel? organicManure = GetOrganicManureFromSession();
            if (organicManure == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (organicManure.IncorporationMethod != null && organicManure.IncorporationMethod == (int)NMP.Commons.Enums.IncorporationMethod.NotIncorporated)
            {
                model.IncorporationDelay = null;
                model.IncorporationDelayName = string.Empty;
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> IncorporationDelay()
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationDelay() action called");
            Error? error = null;
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            try
            {
                string applicableFor = string.Empty;
                List<ManureType> manureTypeList = new List<ManureType>();
                (manureTypeList, error) = await GetManureTypeList(model);

                var manureType = GetAndApplyManureType(model, manureTypeList, error);
                bool isLiquid = manureType?.IsLiquid ?? false;
                applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                if (manureType?.Id == (int)NMP.Commons.Enums.ManureTypes.PoultryManure)
                {
                    applicableFor = Resource.lblP;
                }

                if (IsOtherManureType(model.ManureTypeId))
                {
                    if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials)
                    {
                        applicableFor = Resource.lblL;
                    }
                    else
                    {
                        applicableFor = Resource.lblS;
                    }
                }

                (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _mannerLogic.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                if (error == null && incorporationDelaysList.Count > 0)
                {
                    ViewBag.IncorporationDelaysList = incorporationDelaysList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in IncorporationDelay() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_incorporationMethodError] = ex.Message;
                return View(model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationDelay(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : IncorporationDelay() post action called");
            Error? error = null;
            try
            {
                AddErrorIfNull(model.IncorporationDelay, _incorporationDelayAction, Resource.MsgSelectAnOptionBeforeContinuing);
                if (!ModelState.IsValid)
                {
                    string applicableFor = string.Empty;
                    List<ManureType> manureTypeList = new List<ManureType>();
                    (manureTypeList, error) = await GetManureTypeList(model);

                    var manureType = GetAndApplyManureType(model, manureTypeList, error);
                    bool isLiquid = manureType?.IsLiquid ?? false;
                    applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
                    if (manureType?.Id == (int)NMP.Commons.Enums.ManureTypes.PoultryManure)
                    {
                        applicableFor = Resource.lblP;
                    }


                    (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _mannerLogic.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethod ?? 0, applicableFor);
                    ViewBag.IncorporationDelaysList = incorporationDelaysList;
                    return View(model);
                }

                (model.IncorporationDelayName, error) = await _mannerLogic.FetchIncorporationDelayById(model.IncorporationDelay.Value);
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
                    TempData[_incorporationDelayError] = error.Message;
                    return View(model);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                if ((!model.IsFieldGroupChange) && (!model.IsManureTypeChange) && model.IsCheckAnswer && (!model.IsAnyChangeInField))// && model.IsApplicationMethodChange)
                {
                    return RedirectToAction(_checkAnswer);
                }

                return RedirectToAction(_conditionsAffectingNutrients);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in IncorporationDelay() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_incorporationDelayError] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ConditionsAffectingNutrients()
        {
            _logger.LogTrace($"Organic Manure Controller : ConditionsAffectingNutrients() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            try
            {
                if (model.AutumnCropNitrogenUptake == null)
                {
                    model.AutumnCropNitrogenUptakes = await BuildAutumnCropNitrogenUptakeAsync(model);
                }

                SetSoilDrainageEndDate(model);

                IActionResult? earlyResult = await SetRainfallWithinSixHoursAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                string halfPostCode;
                (earlyResult, halfPostCode) = await SetEffectiveRainfallFarmAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                await SetTotalRainfallAsync(model, halfPostCode);

                earlyResult = await SetWindspeedAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                earlyResult = await SetTopsoilMoistureAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ConditionsAffectingNutrients() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                return BuildApplicationMethodOrIncorporationRedirect(model, ex.Message);
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
                return View(_conditionsAffectingNutrients, model);
            }
            try
            {
                if (model.OrganicManures.Count > 0)
                {
                    int i = 0;
                    foreach (var orgManure in model.OrganicManures)
                    {
                        if (model.AutumnCropNitrogenUptakes != null && model.AutumnCropNitrogenUptakes.Count > 0)
                        {

                            var matchingUptake = model.AutumnCropNitrogenUptakes?
                         .FirstOrDefault(uptake => uptake.FieldName == orgManure.FieldName);

                            if (matchingUptake != null)
                            {
                                orgManure.AutumnCropNitrogenUptake = matchingUptake.AutumnCropNitrogenUptake;
                            }
                            else
                            {
                                orgManure.AutumnCropNitrogenUptake = 0;
                            }
                        }
                        orgManure.SoilDrainageEndDate = model.SoilDrainageEndDate.Value;
                        orgManure.RainfallWithinSixHoursID = model.RainfallWithinSixHoursID.Value;
                        orgManure.Rainfall = model.TotalRainfall.Value;
                        orgManure.WindspeedID = model.WindspeedID.Value;
                        orgManure.MoistureID = model.MoistureTypeId.Value;

                        i++;
                    }
                }
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Organic Manure Controller : Exception in ConditionsAffectingNutrients() : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["ConditionsAffectingNutrientsError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction(_checkAnswer);

        }

        // Shared redirect used by every error path: routes to the application-date or incorporation-delay
        // action depending on model.IsApplicationMethodChange, and sets the matching TempData error key.
        private IActionResult BuildApplicationMethodOrIncorporationRedirect(OrganicManureViewModel model, string errorMessage)
        {
            if (model.IsApplicationMethodChange)
            {
                TempData[_manureApplyingDateError] = errorMessage;
                return RedirectToAction(_manureApplyingDateAction);
            }
            else
            {
                TempData[_incorporationDelayError] = errorMessage;
                return RedirectToAction(_incorporationDelayAction);
            }
        }

        // Soil drainage end date calculation.
        private static void SetSoilDrainageEndDate(OrganicManureViewModel model)
        {
            if (model.SoilDrainageEndDate == null)
            {
                if (model.ApplicationDate.Value.Month >= 8)
                {
                    model.SoilDrainageEndDate = new DateTime(model.ApplicationDate.Value.AddYears(1).Year, (int)NMP.Commons.Enums.Month.March, 31, 0, 0, 0, DateTimeKind.Utc);
                }
                else
                {
                    model.SoilDrainageEndDate = new DateTime(model.ApplicationDate.Value.Year, (int)NMP.Commons.Enums.Month.March, 31, 0, 0, 0, DateTimeKind.Utc);
                }
            }
        }

        // Rainfall-within-6-hours: uses ViewBag.Error + return View(model) on failure (distinct from the
        // other sections, matching the original).
        private async Task<IActionResult?> SetRainfallWithinSixHoursAsync(OrganicManureViewModel model)
        {
            Error? error;
            RainTypeResponse rainType;

            if (model.RainfallWithinSixHoursID == null)
            {
                (rainType, error) = await _organicManureLogic.FetchRainTypeDefault();
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.Error = error.Message;
                    return View(model);
                }
                model.RainfallWithinSixHours = rainType.Name;
                model.RainfallWithinSixHoursID = rainType.ID;
            }
            else
            {
                (rainType, error) = await _organicManureLogic.FetchRainTypeById(model.RainfallWithinSixHoursID.Value);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.Error = error.Message;
                    return View(model);
                }
                model.RainfallWithinSixHours = rainType.Name;
            }

            return null;
        }

        // Fetches the farm and derives the half-postcode used for the rainfall lookup.
        private async Task<(IActionResult? EarlyResult, string HalfPostCode)> SetEffectiveRainfallFarmAsync(OrganicManureViewModel model)
        {
            (FarmResponse farm, Error? error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);

            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return (BuildApplicationMethodOrIncorporationRedirect(model, error.Message), string.Empty);
            }

            string halfPostCode = farm.ClimateDataPostCode.Substring(0, 4).Trim();
            return (null, halfPostCode);
        }

        // Fetches total rainfall for the application/drainage date range, if not already set.
        private async Task SetTotalRainfallAsync(OrganicManureViewModel model, string halfPostCode)
        {
            if (model.ApplicationDate.HasValue && model.SoilDrainageEndDate.HasValue)
            {
                var rainfallPostCodeApplication = new
                {
                    applicationDate = model.ApplicationDate.Value.ToString(_dateStringLiteral),
                    endOfSoilDrainageDate = model.SoilDrainageEndDate.Value.ToString(_dateStringLiteral),
                    climateDataPostcode = halfPostCode
                };

                string jsonString = JsonConvert.SerializeObject(rainfallPostCodeApplication);
                model.TotalRainfall = await _organicManureLogic.FetchRainfallByPostcodeAndDateRange(jsonString);
            }
        }

        // Windspeed during application.
        private async Task<IActionResult?> SetWindspeedAsync(OrganicManureViewModel model)
        {
            Error? error;
            WindspeedResponse? windspeed;

            if (model.WindspeedID == null)
            {
                (windspeed, error) = await _organicManureLogic.FetchWindspeedDataDefault();
            }
            else
            {
                (windspeed, error) = await _organicManureLogic.FetchWindspeedById(model.WindspeedID.Value);
            }

            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return BuildApplicationMethodOrIncorporationRedirect(model, error.Message);
            }

            model.WindspeedID = windspeed.ID;
            model.Windspeed = windspeed.Name;
            return null;
        }

        // Topsoil moisture.
        private async Task<IActionResult?> SetTopsoilMoistureAsync(OrganicManureViewModel model)
        {
            Error? error;
            MoistureTypeResponse moisterType;

            if (model.MoistureTypeId == null)
            {
                (moisterType, error) = await _organicManureLogic.FetchMoisterTypeDefaultByApplicationDate(model.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    return BuildApplicationMethodOrIncorporationRedirect(model, error.Message);
                }
                model.MoistureType = moisterType.Name;
                model.MoistureTypeId = moisterType.ID;
            }
            else
            {
                (moisterType, error) = await _organicManureLogic.FetchMoisterTypeById(model.MoistureTypeId.Value);
                if (error != null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    return BuildApplicationMethodOrIncorporationRedirect(model, error.Message);
                }
                model.MoistureType = moisterType.Name;
            }

            return null;
        }


        [HttpGet]
        public async Task<IActionResult> BackActionForManureGroup()
        {
            _logger.LogTrace($"Organic Manure Controller : BackActionForManureGroup() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (model.IsCheckAnswer)
            {
                model.ManureGroupIdForFilter = model.ManureGroupId;
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                (CommonResponse manureGroup, Error error) = await _mannerLogic.FetchManureGroupById(model.ManureGroupId.Value);
                if (error == null)
                {
                    if (manureGroup != null)
                    {
                        model.ManureGroupName = manureGroup.Name;
                        HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    }
                }
                else
                {
                    TempData[_manureGroupError] = error.Message;
                    return View(model);
                }

                if (!model.IsFieldGroupChange && (!model.IsAnyChangeInField))
                {
                    return RedirectToAction(_checkAnswer);
                }
            }
            if (model.FieldGroup == Resource.lblSelectSpecificFields && model.IsComingFromRecommendation && model.FieldList.Count > 0 && model.FieldList.Count == 1)
            {

                return RedirectToRecommendation(model);

            }

            if (model.FieldGroup == Resource.lblSelectSpecificFields && (!model.IsComingFromRecommendation))
            {
                return RedirectToAction("Fields");
            }
            return RedirectToAction(_fieldGroup);
        }


        /// <summary>
        /// Check Answer
        /// </summary>
        /// <param name="q">encryptedId</param>
        /// <param name="r">encryptedFramId</param>
        /// <param name="s">encryptedHarvestYear</param>
        /// <param name="t">encryptedFieldName</param>
        /// <param name="u">true/false</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> CheckAnswer(string? q, string? r, string? s, string? t, string? u)
        {
            _logger.LogTrace($"Organic Manure Controller : CheckAnswer() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();
            Error? error = null;
            FarmResponse? farm = null;

            try
            {
                IActionResult? earlyResult;
                (model, farm, error, earlyResult) = await LoadModelFromQueryOrSessionAsync(model, q, r, s, t, u);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                ApplyDefoliationCounterIfNeeded(model);

                if (string.IsNullOrWhiteSpace(s))
                {
                    (error, model) = await PrepareFieldDataAsync(model);
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        return HandleError(model, error);
                    }
                }

                ResetWarnings(model, false);
                model.IsAnyChangeInField = false;
                model.IsDoubleCropValueChange = false;

                (farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);

                (model, earlyResult) = await ProcessOrganicManuresNVZWarningsAsync(model, farm);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                model.IsClosedPeriodWarning = false;
                model.IsApplicationJulyToSeptWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;

                (model, earlyResult) = await ProcessNVZClosedPeriodIfApplicableAsync(model, farm, error);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                SetWarningMsgNeedToShowIfAnyWarning(model);
                FinalizeCheckAnswerFlags(model);

                SetOrganicManureToSession(model);

                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
                {
                    SetOrganicDataBeforeUodate(model);
                }

                ViewBag.IsDataChange = ComputeIsDataChanged(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in CheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                return HandleCheckAnswerException(model, ex);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(OrganicManureViewModel model)
        {
            _logger.LogTrace("Organic Manure Controller : CheckAnswer() post action called");

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                IActionResult? earlyResult;
                (model, earlyResult) = await ValidateCheckAnswerModelAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                if (model.OrganicManures != null)
                {
                    (model, earlyResult) = await CalculateManerNutrientsAsync(model);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }

                List<OrganicManure> organicManureList = BuildOrganicManureEntities(model);

                List<object> organicManuresPayload = await BuildOrganicManurePayloadAsync(model, organicManureList);

                IActionResult? saveEarlyResult;
                (bool success, _, saveEarlyResult) = await SaveOrganicManuresAsync(model, organicManuresPayload);
                if (saveEarlyResult != null)
                {
                    return saveEarlyResult;
                }

                if (success)
                {
                    return BuildSuccessRedirect(model, success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in CheckAnswer() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_addOrganicManureError] = Resource.MsgWeCounldNotAddOrganicManure;
                return View(model);
            }

            return View(model);
        }


        // ===================== Top-level orchestration helpers =====================

        private IActionResult HandleCheckAnswerException(OrganicManureViewModel model, Exception ex)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                TempData["ConditionsAffectingNutrientsError"] = ex.Message;
                return RedirectToAction(_conditionsAffectingNutrients);
            }
            else
            {
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                HttpContext.Session.Remove(_organicManureSessionKey);
                return RedirectToAction(_harvestYearOverview, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
        }

        private void ApplyDefoliationCounterIfNeeded(OrganicManureViewModel model)
        {
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
        }

        private static void SetWarningMsgNeedToShowIfAnyWarning(OrganicManureViewModel model)
        {
            if (model.IsNMaxLimitWarning || model.IsOrgManureNfieldLimitWarning || model.IsClosedPeriodWarning
                || model.IsApplicationJulyToSeptWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks)
            {
                model.IsWarningMsgNeedToShow = true;
            }
        }

        private static void FinalizeCheckAnswerFlags(OrganicManureViewModel model)
        {
            model.IsCheckAnswer = true;
            model.IsManureTypeChange = false;
            model.IsApplicationMethodChange = false;
            model.IsFieldGroupChange = false;
            model.IsIncorporationMethodChange = false;
            model.IsApplicationDateChange = false;
        }

        private bool ComputeIsDataChanged(OrganicManureViewModel model)
        {
            var previousModel = GetOrganicDataBeforeUpdateFromSession();
            if (previousModel == null)
            {
                return false;
            }

            string oldJson = JsonConvert.SerializeObject(previousModel);
            string newJson = JsonConvert.SerializeObject(model);
            return !string.Equals(oldJson, newJson, StringComparison.Ordinal);
        }

        // ===================== Loading model from query params or session =====================

        private async Task<(OrganicManureViewModel Model, FarmResponse? Farm, Error? Error, IActionResult? EarlyResult)>
            LoadModelFromQueryOrSessionAsync(OrganicManureViewModel model, string? q, string? r, string? s, string? t, string? u)
        {
            if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
            {
                return await PopulateModelFromEncryptedParamsAsync(model, q, r, s, t, u);
            }

            if (!TryGetSessionModel(nameof(CheckAnswer), out model, out var redirect))
            {
                return (model, null, null, redirect);
            }

            return (model, null, null, null);
        }

        private async Task<(OrganicManureViewModel Model, FarmResponse? Farm, Error? Error, IActionResult? EarlyResult)>
            PopulateModelFromEncryptedParamsAsync(OrganicManureViewModel model, string q, string r, string s, string? t, string? u)
        {
            if (!string.IsNullOrWhiteSpace(u))
            {
                model.IsComingFromRecommendation = true;
            }

            model.EncryptedOrgManureId = q;
            int decryptedId = Convert.ToInt32(_cropDataProtector.Unprotect(q));
            int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(r));
            model.FarmId = decryptedFarmId;
            int decryptedHarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));

            Error? error;
            FarmResponse? farm;
            (farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (string.IsNullOrWhiteSpace(error?.Message))
            {
                model.FarmCountryId = farm.CountryID;
                model.FarmRB209CountryID = farm.RB209CountryID;
            }

            if (decryptedId > 0)
            {
                IActionResult? earlyResult;
                (model, earlyResult) = await PopulateOrganicManureDetailsAsync(model, decryptedId, decryptedFarmId, decryptedHarvestYear, t, s, r);
                if (earlyResult != null)
                {
                    return (model, farm, error, earlyResult);
                }
            }

            return (model, farm, error, null);
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> PopulateOrganicManureDetailsAsync(
            OrganicManureViewModel model, int decryptedId, int decryptedFarmId, int decryptedHarvestYear, string? t, string s, string r)
        {
            (OrganicManureDataViewModel organicManure, Error? error) = await _organicManureLogic.FetchOrganicManureById(decryptedId);
            if (error != null || organicManure == null)
            {
                return (model, null);
            }

            IActionResult? earlyResult;
            (model, organicManure, earlyResult) = await ApplyFieldSelectionAndDoubleCropAsync(model, organicManure, decryptedId, decryptedFarmId, decryptedHarvestYear, t);
            if (earlyResult != null)
            {
                return (model, earlyResult);
            }

            (model, earlyResult) = await ApplyOrganicManureCommonFieldsAsync(model, organicManure, decryptedHarvestYear, decryptedFarmId, s, r);
            if (earlyResult != null)
            {
                return (model, earlyResult);
            }

            return (model, null);
        }

        // ===================== Field selection / double crop / defoliation =====================

        private async Task<(OrganicManureViewModel Model, OrganicManureDataViewModel OrganicManure, IActionResult? EarlyResult)>
            ApplyFieldSelectionAndDoubleCropAsync(OrganicManureViewModel model, OrganicManureDataViewModel organicManure,
                int decryptedId, int decryptedFarmId, int decryptedHarvestYear, string? t)
        {
            (List<FertiliserAndOrganicManureUpdateResponse> organicManureResponse, Error? error) =
                await _organicManureLogic.FetchFieldWithSameDateAndManureType(decryptedId, decryptedFarmId, decryptedHarvestYear);

            if (string.IsNullOrWhiteSpace(error?.Message) && organicManureResponse != null && organicManureResponse.Count > 0)
            {
                model.UpdatedOrganicIds = organicManureResponse;
                if (model.IsComingFromRecommendation)
                {
                    model.FieldGroup = Resource.lblSelectSpecificFields;
                    model.UpdatedOrganicIds.RemoveAll(x => x.OrganicManureId != organicManure.ID);
                    organicManureResponse.RemoveAll(x => x.OrganicManureId != organicManure.ID);
                }

                BuildFieldSelectionViewBag(model, organicManureResponse, t);

                model = await DetectDoubleCropFieldAsync(model, decryptedHarvestYear);

                (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(organicManure.ManagementPeriodID);

                if (model.IsDoubleCropAvailable)
                {
                    IActionResult? earlyResult;
                    (model, earlyResult) = await PopulateDoubleCropListAsync(model, managementPeriod, decryptedHarvestYear);
                    if (earlyResult != null)
                    {
                        return (model, organicManure, earlyResult);
                    }
                }

                int fieldIdForUpdate = Convert.ToInt32(model.FieldList.FirstOrDefault());
                if (model.OrganicManures == null)
                {
                    model.OrganicManures = new List<OrganicManureDataViewModel>();
                }

                (model, organicManure) = await ApplyDefoliationAndBuildOrganicEntryAsync(model, organicManure, managementPeriod, error, fieldIdForUpdate);
            }

            return (model, organicManure, null);
        }

        private void BuildFieldSelectionViewBag(OrganicManureViewModel model, List<FertiliserAndOrganicManureUpdateResponse> organicManureResponse, string? t)
        {
            var selectListItem = ToSelectList(organicManureResponse.DistinctBy(f => f.Id), f => f.Id.ToString(), f => f.Name);

            ViewBag.Fields = selectListItem.OrderBy(x => x.Text).ToList();
            List<string> fieldName = [];
            if (!string.IsNullOrWhiteSpace(t))
            {
                fieldName.Add(_cropDataProtector.Unprotect(t));
            }

            ViewBag.SelectedFields = fieldName;

            var filteredList = selectListItem
                .Where(item => item.Text.Contains(_cropDataProtector.Unprotect(t)))
                .ToList();

            model.FieldName = filteredList.Select(item => item.Text).FirstOrDefault();
            model.FieldList = filteredList.Select(item => item.Value).ToList();
            model.FieldID = filteredList.Select(item => Convert.ToInt32(item.Value)).FirstOrDefault();
        }

        private async Task<OrganicManureViewModel> DetectDoubleCropFieldAsync(OrganicManureViewModel model, int decryptedHarvestYear)
        {
            foreach (string field in model.FieldList)
            {
                List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(field));
                cropList = cropList.Where(x => x.Year == decryptedHarvestYear).ToList();

                if (cropList.Count == 2)
                {
                    model.FieldID = Convert.ToInt32(field);
                    model.IsDoubleCropAvailable = true;
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(field))).Name;
                }
            }

            return model;
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> PopulateDoubleCropListAsync(
            OrganicManureViewModel model, ManagementPeriod? managementPeriod, int decryptedHarvestYear)
        {
            string cropTypeName = string.Empty;
            if (model.DoubleCrop == null)
            {
                model.DoubleCrop = new List<DoubleCrop>();
            }
            int fertiliserCounter = 1;

            (Crop crop, Error? error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
            if (error != null)
            {
                return (model, BuildRecommendationOrHarvestYearRedirect(model, error));
            }

            (List<Crop> cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(crop.FieldID.Value, decryptedHarvestYear);
            if (error != null)
            {
                return (model, BuildRecommendationOrHarvestYearRedirect(model, error));
            }

            if (cropList != null && cropList.Count == 2)
            {
                cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                var doubleCrop = new DoubleCrop
                {
                    CropID = crop.ID.Value,
                    CropName = cropTypeName,
                    CropOrder = crop.CropOrder.Value,
                    FieldID = crop.FieldID.Value,
                    FieldName = (await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value)).Name,
                    EncryptedCounter = _fieldDataProtector.Protect(fertiliserCounter.ToString()),
                    Counter = model.DoubleCropCurrentCounter,
                };
                model.DoubleCrop.Add(doubleCrop);
            }

            return (model, null);
        }

        // Shared by both double-crop error branches: they were identical in the original code.
        private IActionResult BuildRecommendationOrHarvestYearRedirect(OrganicManureViewModel model, Error error)
        {
            if (!string.IsNullOrWhiteSpace(model.EncryptedOrgManureId) && model.IsComingFromRecommendation)
            {
                TempData[_nutrientRecommendationsError] = error.Message;
                return RedirectToRecommendation(model);
            }
            else
            {
                return RedirectToHarvestYearPage(model, error);
            }
        }

        private async Task<(OrganicManureViewModel Model, OrganicManureDataViewModel OrganicManure)> ApplyDefoliationAndBuildOrganicEntryAsync(
            OrganicManureViewModel model, OrganicManureDataViewModel organicManure, ManagementPeriod? managementPeriod, Error? error, int fieldIdForUpdate)
        {
            int? defoliation = null;

            if (HasError(error))
            {
                TempData[_checkYourAnswerError] = error.Message;
            }
            else
            {
                defoliation = managementPeriod.Defoliation;
                Crop crop;
                (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);

                if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                {
                    organicManure.IsGrass = true;
                    model.IsAnyCropIsGrass = true;

                    if (model.DefoliationList == null)
                    {
                        model.DefoliationList = new List<DefoliationList>();
                    }

                    (model, organicManure) = await ApplyDefoliationSequenceAsync(model, organicManure, crop, managementPeriod, defoliation);
                }
            }

            organicManure.FieldID = model.FieldID;
            organicManure.FieldName = model.FieldName;

            var organic = BuildOrganicManureEntry(model, organicManure, fieldIdForUpdate);
            model.OrganicManures.Add(organic);

            return (model, organicManure);
        }

        private async Task<(OrganicManureViewModel Model, OrganicManureDataViewModel OrganicManure)> ApplyDefoliationSequenceAsync(
     OrganicManureViewModel model, OrganicManureDataViewModel organicManure, Crop crop, ManagementPeriod managementPeriod,
     int? defoliation)
        {
            (DefoliationSequenceResponse defoliationSequence, Error? error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
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
                string defoliationName = selectedDefoliation;
                var defList = new DefoliationList
                {
                    CropID = crop.ID.Value,
                    ManagementPeriodID = organicManure.ManagementPeriodID,
                    FieldID = crop.FieldID.Value,
                    FieldName = (await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value)).Name,
                    EncryptedCounter = _fieldDataProtector.Protect(model.DefoliationList.Count + 1.ToString()),
                    Counter = model.DefoliationList.Count + 1,
                    Defoliation = managementPeriod.Defoliation,
                    DefoliationName = defoliationName
                };
                model.DefoliationList.Add(defList);
                organicManure.IsGrass = true;
                organicManure.Defoliation = managementPeriod.Defoliation;
                organicManure.DefoliationName = defoliationName;
            }
            return (model, organicManure);
        }

        private OrganicManureDataViewModel BuildOrganicManureEntry(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, int fieldIdForUpdate)
        {
            return new OrganicManureDataViewModel
            {
                ManagementPeriodID = organicManure.ManagementPeriodID,
                ManureTypeID = organicManure.ManureTypeID,
                ManureTypeName = organicManure.ManureTypeName,
                ApplicationDate = organicManure.ApplicationDate.Value.ToLocalTime(),
                Confirm = organicManure.Confirm,
                N = organicManure.N,
                P2O5 = organicManure.P2O5,
                K2O = organicManure.K2O,
                MgO = organicManure.MgO,
                SO3 = organicManure.SO3,
                AvailableN = organicManure.AvailableN,
                ApplicationRate = organicManure.ApplicationRate,
                DryMatterPercent = organicManure.DryMatterPercent,
                UricAcid = organicManure.UricAcid,
                EndOfDrain = organicManure.EndOfDrain.ToLocalTime(),
                Rainfall = organicManure.Rainfall,
                AreaSpread = organicManure.AreaSpread,
                ManureQuantity = organicManure.ManureQuantity,
                ApplicationMethodID = organicManure.ApplicationMethodID,
                IncorporationMethodID = organicManure.IncorporationMethodID,
                IncorporationDelayID = organicManure.IncorporationDelayID,
                NH4N = organicManure.NH4N,
                NO3N = organicManure.NO3N,
                AvailableP2O5 = organicManure.AvailableP2O5,
                AvailableK2O = organicManure.AvailableK2O,
                AvailableSO3 = organicManure.AvailableSO3,
                WindspeedID = organicManure.WindspeedID,
                RainfallWithinSixHoursID = organicManure.RainfallWithinSixHoursID,
                MoistureID = organicManure.MoistureID,
                AutumnCropNitrogenUptake = organicManure.AutumnCropNitrogenUptake,
                AvailableNForNMax = organicManure.AvailableNForNMax,
                FieldID = fieldIdForUpdate,
                Defoliation = organicManure.Defoliation,
                DefoliationName = organicManure.DefoliationName,
                EncryptedCounter = organicManure.EncryptedCounter,
                FieldName = model.FieldName,
                IsGrass = organicManure.IsGrass,
            };
        }

        // ===================== Common fields applied regardless of whether the inner block ran =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ApplyOrganicManureCommonFieldsAsync(
            OrganicManureViewModel model, OrganicManureDataViewModel organicManure, int decryptedHarvestYear, int decryptedFarmId, string s, string r)
        {
            model.IsSameDefoliationForAll = true;
            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            model.HarvestYear = decryptedHarvestYear;
            model.FarmId = decryptedFarmId;
            model.EncryptedHarvestYear = s;
            model.EncryptedFarmId = r;
            model.ManureTypeId = organicManure.ManureTypeID;
            model.ManureTypeName = organicManure.ManureTypeName;
            model.ApplicationDate = organicManure.ApplicationDate?.ToLocalTime();
            model.ApplicationMethod = organicManure.ApplicationMethodID;

            Error? error;
            (model.ApplicationMethodName, error) = await _mannerLogic.FetchApplicationMethodById(model.ApplicationMethod.Value);

            if (IsOtherManureType(model.ManureTypeId))
            {
                model.OtherMaterialName = model.ManureTypeName;
            }

            if (HasError(error))
            {
                HttpContext.Session.Remove(_organicManureSessionKey);
                return (model, RedirectToHarvestYearPage(model, error));
            }

            ManureType manureType;
            (model, manureType) = await ApplyManureTypeDetailsAsync(model, organicManure);

            model = ComputeDefaultNutrientValue(model, organicManure, manureType);

            model = FinalizeApplicationRateAndDates(model, organicManure);

            IActionResult? earlyResult;
            (model, earlyResult) = await FetchIncorporationAndMoistureDetailsAsync(model, organicManure);
            if (earlyResult != null)
            {
                return (model, earlyResult);
            }

            model = await BuildAutumnCropUptakeIfFieldListAsync(model, organicManure);

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            return (model, null);
        }

        private async Task<(OrganicManureViewModel Model, ManureType ManureType)> ApplyManureTypeDetailsAsync(
            OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            (ManureType manureType, Error? error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (error == null && manureType != null)
            {
                model.IsManureTypeLiquid = manureType.IsLiquid;
                model.ManureGroupId = manureType.ManureGroupId;
                model.ManureGroupIdForFilter = manureType.ManureGroupId;
                model.ApplicationRateArable = manureType.ApplicationRateArable;
            }

            model.N = organicManure.N;
            model.P2O5 = organicManure.P2O5;
            model.MgO = organicManure.MgO;
            model.NH4N = organicManure.NH4N;
            model.NO3N = organicManure.NO3N;
            model.SO3 = organicManure.SO3;
            model.K2O = organicManure.K2O;
            model.DryMatterPercent = organicManure.DryMatterPercent;
            model.UricAcid = organicManure.UricAcid;
            model.ManureType.TotalN = organicManure.N;
            model.ManureType.P2O5 = organicManure.P2O5;
            model.ManureType.MgO = organicManure.MgO;
            model.ManureType.NH4N = organicManure.NH4N;
            model.ManureType.NO3N = organicManure.NO3N;
            model.ManureType.SO3 = organicManure.SO3;
            model.ManureType.K2O = organicManure.K2O;
            model.ManureType.DryMatter = organicManure.DryMatterPercent;
            model.ManureType.Uric = organicManure.UricAcid;

            return (model, manureType);
        }

        private static bool NutrientValuesMatch(FarmManureTypeResponse farmManureType, OrganicManureViewModel model)
        {
            return farmManureType.TotalN == model.N && farmManureType.P2O5 == model.P2O5 &&
                   farmManureType.DryMatter == model.DryMatterPercent && farmManureType.Uric == model.UricAcid &&
                   farmManureType.NH4N == model.NH4N && farmManureType.NO3N == model.NO3N &&
                   farmManureType.SO3 == model.SO3 && farmManureType.K2O == model.K2O &&
                   farmManureType.MgO == model.MgO;
        }

        private OrganicManureViewModel ComputeDefaultNutrientValue(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, ManureType manureType)
        {
            model = ComputeDefaultNutrientValueFromFarmManureType(model, organicManure);

            if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
            {
                if (manureType.TotalN == model.N && manureType.MgO == model.MgO && manureType.P2O5 == model.P2O5 &&
                    manureType.NH4N == model.NH4N && manureType.NO3N == model.NO3N
                    && manureType.SO3 == model.SO3 && manureType.K2O == model.K2O
                    && manureType.DryMatter == model.DryMatterPercent && manureType.Uric == model.UricAcid)
                {
                    model.DefaultNutrientValue = Resource.lblYesUseTheseStandardNutrientValues;
                }
                else
                {
                    model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
                }
            }

            model.ManureTypeName = organicManure.ManureTypeName;
            return model;
        }

        private OrganicManureViewModel ComputeDefaultNutrientValueFromFarmManureType(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            (List<FarmManureTypeResponse> farmManureTypeResponse, Error? error) = _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId.Value).GetAwaiter().GetResult();

            if (error != null || farmManureTypeResponse == null)
            {
                return model;
            }

            if (farmManureTypeResponse.Count == 0)
            {
                model.DefaultNutrientValue = Resource.lblYes;
                return model;
            }

            FarmManureTypeResponse? farmManureType = farmManureTypeResponse
                .FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId && x.ManureTypeName == model.ManureTypeName);

            if (farmManureType == null)
            {
                model.DefaultNutrientValue = Resource.lblYes;
                return model;
            }

            ApplyFarmManureTypeMatch(model, organicManure, farmManureType);
            return model;
        }

        private static void ApplyFarmManureTypeMatch(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, FarmManureTypeResponse farmManureType)
        {
            bool isOtherManureSameName = IsOtherManureSameName(model, organicManure, farmManureType);

            if (NutrientValuesMatch(farmManureType, model))
            {
                model.DefaultNutrientValue = isOtherManureSameName
                    ? Resource.lblYes
                    : Resource.lblYesUseTheseValues;
            }

            if (isOtherManureSameName)
            {
                ApplyOtherManureGrouping(model, organicManure);
            }

            model.DefaultFarmManureValueDate = farmManureType.ModifiedOn ?? farmManureType.CreatedOn;
        }

        private static bool IsOtherManureSameName(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, FarmManureTypeResponse farmManureType)
        {
            return model.ManureTypeId != null
                && IsOtherManureType(model.ManureTypeId)
                && farmManureType.ManureTypeName.Equals(organicManure.ManureTypeName);
        }

        private static void ApplyOtherManureGrouping(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            model.ManureGroupId = organicManure.ManureTypeID;
            model.ManureGroupIdForFilter = organicManure.ManureTypeID;
            model.OrganicManures.ForEach(x => x.SoilDrainageEndDate = x.EndOfDrain.ToLocalTime());
        }

        private static OrganicManureViewModel FinalizeApplicationRateAndDates(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            model.ApplicationRate = organicManure.ApplicationRate;
            model.SoilDrainageEndDate = organicManure.EndOfDrain.ToLocalTime();

            if (organicManure.AreaSpread != null && organicManure.ManureQuantity != null)
            {
                model.Area = organicManure.AreaSpread;
                model.Quantity = organicManure.ManureQuantity;
                model.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity;
            }
            else if (model.ApplicationRateArable == model.ApplicationRate)
            {
                model.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate;
            }
            else
            {
                model.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.EnterAnApplicationRate;
            }

            return model;
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> FetchIncorporationAndMoistureDetailsAsync(
            OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            Error? error;

            model.IncorporationDelay = organicManure.IncorporationDelayID;
            (model.IncorporationDelayName, error) = await _mannerLogic.FetchIncorporationDelayById(model.IncorporationDelay.Value);
            if (HasError(error))
            {
                return (model, RedirectToHarvestYearPage(model, error));
            }

            model.IncorporationMethod = organicManure.IncorporationMethodID;
            (model.IncorporationMethodName, error) = await _mannerLogic.FetchIncorporationMethodById(model.IncorporationMethod.Value);
            if (HasError(error))
            {
                return (model, RedirectToHarvestYearPage(model, error));
            }

            model.MoistureTypeId = organicManure.MoistureID;
            (MoistureTypeResponse moistureTypeResponse, error) = await _organicManureLogic.FetchMoisterTypeById(model.MoistureTypeId.Value);
            if (HasError(error))
            {
                return (model, RedirectToHarvestYearPage(model, error));
            }
            else if (moistureTypeResponse != null)
            {
                model.MoistureType = moistureTypeResponse.Name;
            }

            model.RainfallWithinSixHoursID = organicManure.RainfallWithinSixHoursID;
            (RainTypeResponse rainTypeResponse, error) = await _organicManureLogic.FetchRainTypeById(model.RainfallWithinSixHoursID.Value);
            if (HasError(error))
            {
                return (model, RedirectToHarvestYearPage(model, error));
            }
            else if (rainTypeResponse != null)
            {
                model.RainfallWithinSixHours = rainTypeResponse.Name;
            }

            model.WindspeedID = organicManure.WindspeedID;
            (WindspeedResponse? windspeedResponse, error) = await _organicManureLogic.FetchWindspeedById(model.WindspeedID.Value);
            if (HasError(error))
            {
                return (model, RedirectToHarvestYearPage(model, error));
            }
            else if (windspeedResponse != null)
            {
                model.Windspeed = windspeedResponse.Name;
            }

            model.SoilDrainageEndDate = organicManure.EndOfDrain.ToLocalTime();
            model.TotalRainfall = organicManure.Rainfall;
            model.FieldGroup = Resource.lblSelectSpecificFields;

            return (model, null);
        }

        private async Task<OrganicManureViewModel> BuildAutumnCropUptakeIfFieldListAsync(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {

            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                (CropTypeResponse cropsResponse, _) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(
                    Convert.ToInt32(model.FieldList.FirstOrDefault()), model.HarvestYear.Value, false);

                if (model.AutumnCropNitrogenUptakes == null)
                {
                    model.AutumnCropNitrogenUptakes = new List<AutumnCropNitrogenUptakeDetail>();
                }

                var fieldData = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(model.FieldList.FirstOrDefault()));
                model.AutumnCropNitrogenUptakes.Add(new AutumnCropNitrogenUptakeDetail
                {
                    EncryptedFieldId = _organicManureProtector.Protect(model.FieldList[0]),
                    FieldName = fieldData.Name ?? string.Empty,
                    CropTypeId = cropsResponse.CropTypeId,
                    CropTypeName = cropsResponse.CropType,
                    AutumnCropNitrogenUptake = organicManure.AutumnCropNitrogenUptake
                });
            }

            return model;
        }

        // ===================== NVZ warnings loop over OrganicManures =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessOrganicManuresNVZWarningsAsync(
            OrganicManureViewModel model, FarmResponse? farm)
        {
            string message = string.Empty;

            if (model.OrganicManures == null || model.OrganicManures.Count == 0)
            {
                return (model, null);
            }

            foreach (var organicManure in model.OrganicManures)
            {
                int? fieldId = organicManure.FieldID ?? null;
                if (fieldId == null)
                {
                    continue;
                }

                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);
                if (field == null)
                {
                    continue;
                }

                bool isFieldIsInNVZ = field.IsWithinNVZ ?? false;
                if (!isFieldIsInNVZ)
                {
                    continue;
                }

                IActionResult? earlyResult;
                (model, earlyResult) = await ProcessSingleFieldNVZWarningsAsync(model, farm, organicManure, fieldId.Value, message);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            return (model, null);
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessSingleFieldNVZWarningsAsync(
            OrganicManureViewModel model, FarmResponse? farm, OrganicManureDataViewModel organicManure, int fieldId, string message)
        {
            List<ManureType> manureTypeList;
            Error? error;
            (manureTypeList, error) = await GetManureTypeList(model);

            var manureType = GetAndApplyManureType(model, manureTypeList, error);
            bool isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
            model.HighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen;

            (FieldDetailResponse fieldDetail, error) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear ?? 0, false);
            string? closedPeriod = await GetClosedPeriod(model, farm, isHighReadilyAvailableNitrogen);

            model.ClosedPeriod = closedPeriod;
            if (!string.IsNullOrWhiteSpace(closedPeriod))
            {
                model = await GetDatesFromClosedPeriod(model, closedPeriod);
            }

            (model, error) = await IsNFieldLimitWarningMessage(model, organicManure.ManagementPeriodID, fieldId, farm);
            if (error != null)
            {
                return (model, HandleError(model, error));
            }

            (model, error) = await IsNMaxWarningMessage(model, fieldId, organicManure.ManagementPeriodID, true, farm, fieldDetail, organicManure);
            if (error != null)
            {
                return (model, HandleError(model, error));
            }

            (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(organicManure.ManagementPeriodID);
            if (!IsOtherManureType(model.ManureTypeId))
            {
                (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, farm, managementPeriod.CropID.Value, fieldId);
                if (error != null)
                {
                    return (model, HandleError(model, error));
                }
            }

            if (!IsOtherManureType(model.ManureTypeId))
            {
                (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, message, error) =
                    await IsClosedPeriodStartAndEndFebExceedNRateException(model, fieldId, farm, organicManure.ManagementPeriodID);

                if (error != null)
                {
                    return (model, HandleError(model, error));
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    TempData["AppRateExceeds150WithinClosedPeriodOrganic"] = message;
                }
            }

            return (model, null);
        }

        // ===================== NVZ closed period (FieldList-level) warnings =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessNVZClosedPeriodIfApplicableAsync(
            OrganicManureViewModel model, FarmResponse? farm, Error? error)
        {
            if (model.FieldList == null || model.FieldList.Count < 1)
            {
                return (model, null);
            }

            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return (model, HandleError(model, error));
            }

            if (farm != null)
            {
                (model, error) = await ProcessNVZClosedPeriodWarningAsync(model, farm);
            }

            return (model, null);
        }


        // ===================== Validation =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ValidateCheckAnswerModelAsync(OrganicManureViewModel model)
        {
            AddErrorIfNull(model.ManureTypeId, "ManureTypeId", Resource.MsgManureTypeNotSet);
            Error? error = await ValidateDoubleCropSelectionAsync(model);

            if (error != null)
            {
                TempData[_checkYourAnswerError] = error.Message;
                return (model, View(model));
            }

            ValidateGrassDefoliation(model);
            ValidateManureModel(model);

            if (!ModelState.IsValid)
            {
                (error, model) = await PrepareFieldDataAsync(model);
                return (model, View(model));
            }

            return (model, null);
        }

        // ===================== Manner nutrient calculation =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> CalculateManerNutrientsAsync(OrganicManureViewModel model)
        {
            SetOrganicManureValues(model);

            // logic for AvailableNForNMax column that will be used to get sum of previous manure applications
            decimal? currentApplicationNitrogen = await CalculateCurrentApplicationNitrogenAsync(model);

            (FarmResponse farmData, Error? error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (farmData == null || !string.IsNullOrWhiteSpace(error?.Message))
            {
                return (model, await BuildManerFailureResultAsync(model));
            }

            foreach (var organic in model.OrganicManures)
            {
                IActionResult? earlyResult;
                (model, earlyResult) = await ApplyManerNutrientsToOrganicAsync(model, organic, farmData, currentApplicationNitrogen);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            return (model, null);
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ApplyManerNutrientsToOrganicAsync(
            OrganicManureViewModel model, OrganicManureDataViewModel organic, FarmResponse farmData, decimal? currentApplicationNitrogen)
        {
            (string? mannerJsonString, _) = await BindManureOutput(farmData, organic, model);
            if (string.IsNullOrWhiteSpace(mannerJsonString))
            {
                return (model, await BuildManerFailureResultAsync(model));
            }

            (MannerCalculateNutrientResponse mannerCalculateNutrientResponse, Error? error) = await _organicManureLogic.FetchMannerCalculateNutrient(mannerJsonString);
            if (error != null || mannerCalculateNutrientResponse == null)
            {
                return (model, await BuildManerFailureResultAsync(model));
            }

            organic.AvailableN = mannerCalculateNutrientResponse.CurrentCropAvailableN;
            organic.AvailableSO3 = mannerCalculateNutrientResponse.CropAvailableSO3;
            organic.AvailableP2O5 = mannerCalculateNutrientResponse.CropAvailableP2O5;
            organic.AvailableK2O = mannerCalculateNutrientResponse.CropAvailableK2O;
            organic.TotalN = mannerCalculateNutrientResponse.TotalN;
            organic.TotalP2O5 = mannerCalculateNutrientResponse.TotalP2O5;
            organic.TotalSO3 = mannerCalculateNutrientResponse.TotalSO3;
            organic.TotalK2O = mannerCalculateNutrientResponse.TotalK2O;
            organic.TotalMgO = mannerCalculateNutrientResponse.TotalMgO;
            organic.AvailableNForNextYear = mannerCalculateNutrientResponse.FollowingCropYear2AvailableN;
            organic.AvailableNForNextDefoliation = mannerCalculateNutrientResponse.NextGrassNCropCurrentYear;
            organic.AvailableNForNMax = currentApplicationNitrogen != null ? currentApplicationNitrogen : mannerCalculateNutrientResponse.CurrentCropAvailableN;

            return (model, null);
        }

        // Shared by every manner-nutrient failure path: same TempData message, same PrepareFieldDataAsync call, same View.
        private async Task<IActionResult> BuildManerFailureResultAsync(OrganicManureViewModel model)
        {
            TempData[_addOrganicManureError] = Resource.MsgWeCounldNotAddOrganicManure;
            await PrepareFieldDataAsync(model);
            return View(model);
        }

        // ===================== Mapping view-model entries to OrganicManure entities =====================

        private List<OrganicManure> BuildOrganicManureEntities(OrganicManureViewModel model)
        {
            List<OrganicManure> organicManureList = new List<OrganicManure>();

            if (model.OrganicManures != null && model.OrganicManures.Any())
            {
                foreach (var om in model.OrganicManures)
                {
                    organicManureList.Add(new OrganicManure
                    {
                        ManagementPeriodID = om.ManagementPeriodID,
                        N = om.N,
                        P2O5 = om.P2O5,
                        NH4N = om.NH4N,
                        K2O = om.K2O,
                        MgO = om.MgO,
                        NO3N = om.NO3N,
                        Confirm = om.Confirm,
                        SO3 = om.SO3,
                        DryMatterPercent = om.DryMatterPercent,
                        UricAcid = om.UricAcid,
                        Rainfall = om.Rainfall,
                        RainfallWithinSixHoursID = om.RainfallWithinSixHoursID,
                        WindspeedID = om.WindspeedID,
                        MoistureID = om.MoistureID,
                        ManureTypeID = om.ManureTypeID,
                        ManureTypeName = om.ManureTypeName,
                        ApplicationMethodID = om.ApplicationMethodID,
                        ApplicationDate = om.ApplicationDate,
                        ApplicationRate = om.ApplicationRate,
                        AreaSpread = om.AreaSpread,
                        ManureQuantity = om.ManureQuantity,
                        EndOfDrain = om.EndOfDrain,
                        SoilDrainageEndDate = om.SoilDrainageEndDate,
                        IncorporationDelayID = om.IncorporationDelayID,
                        IncorporationMethodID = om.IncorporationMethodID,
                        AutumnCropNitrogenUptake = om.AutumnCropNitrogenUptake,
                        AvailableN = om.AvailableN,
                        AvailableSO3 = om.AvailableSO3,
                        AvailableP2O5 = om.AvailableP2O5,
                        AvailableK2O = om.AvailableK2O,
                        TotalN = om.TotalN,
                        TotalP2O5 = om.TotalP2O5,
                        TotalSO3 = om.TotalSO3,
                        TotalK2O = om.TotalK2O,
                        TotalMgO = om.TotalMgO,
                        AvailableNForNextYear = om.AvailableNForNextYear,
                        AvailableNForNextDefoliation = om.AvailableNForNextDefoliation,
                        AvailableNForNMax = om.AvailableNForNMax
                    });
                }
            }

            return organicManureList;
        }

        // ===================== Building the payload (with warning messages) for the save call =====================

        private async Task<List<object>> BuildOrganicManurePayloadAsync(OrganicManureViewModel model, List<OrganicManure> organicManureList)
        {
            var organicManures = new List<object>();

            if (organicManureList.Count > 0)
            {
                foreach (var orgManure in organicManureList)
                {
                    int fieldTypeId = await ResolveFieldTypeIdAsync(orgManure);

                    OrganicManureDataViewModel? organicManureData = model.OrganicManures?
                        .FirstOrDefault(x => x.ManagementPeriodID == orgManure.ManagementPeriodID);

                    List<WarningMessage> warningMessageList = new List<WarningMessage>();
                    if (organicManureData != null)
                    {
                        warningMessageList = await GetWarningMessages(model, organicManureData);
                    }

                    organicManures.Add(new
                    {
                        OrganicManure = orgManure,
                        WarningMessages = warningMessageList.Count > 0 ? warningMessageList : null,
                        FarmID = model.FarmId,
                        FieldTypeID = fieldTypeId,
                        SaveDefaultForFarm = model.IsAnyNeedToStoreNutrientValueForFuture
                    });
                }
            }

            return organicManures;
        }

        // ===================== Building the save payload (field type + warning messages) =====================

        private async Task<List<object>> BuildOrganicManurePayloadAsync(OrganicManureViewModel model, List<OrganicManureUpdateData> organicManureList)
        {
            var organicManures = new List<object>();

            foreach (var orgManure in organicManureList)
            {
                (int fieldTypeId, int? fieldId) = await ResolveFieldTypeAndIdAsync(orgManure);

                OrganicManureDataViewModel? organicManureData = model.OrganicManures?
                    .FirstOrDefault(x => x.ManagementPeriodID == orgManure.ManagementPeriodID);

                List<WarningMessage> warningMessageList = new List<WarningMessage>();
                if (organicManureData != null)
                {
                    warningMessageList = await GetWarningMessages(model, organicManureData);
                }

                warningMessageList.ForEach(x => x.JoiningID = x.WarningCodeID != (int)NMP.Commons.Enums.WarningCode.NMaxLimit ? orgManure.ID : fieldId);

                organicManures.Add(new
                {
                    OrganicManure = orgManure,
                    WarningMessages = warningMessageList.Count > 0 ? warningMessageList : null,
                    FarmID = model.FarmId,
                    FieldTypeID = fieldTypeId,
                    SaveDefaultForFarm = model.IsAnyNeedToStoreNutrientValueForFuture
                });
            }

            return organicManures;
        }

        private async Task<int> ResolveFieldTypeIdAsync(OrganicManure orgManure)
        {
            int fieldTypeId = (int)NMP.Commons.Enums.FieldType.Arable;

            (ManagementPeriod manData, Error? error) = await _cropLogic.FetchManagementperiodById(orgManure.ManagementPeriodID);
            if (manData != null)
            {
                (Crop crop, error) = await _cropLogic.FetchCropById(manData.CropID.Value);
                if (crop != null)
                {
                    fieldTypeId = (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                        ? (int)NMP.Commons.Enums.FieldType.Grass
                        : (int)NMP.Commons.Enums.FieldType.Arable;
                }
            }

            return fieldTypeId;
        }

        // ===================== Save =====================

        private async Task<(bool Success, Error? Error, IActionResult? EarlyResult)> SaveOrganicManuresAsync(OrganicManureViewModel model, List<object> organicManures)
        {
            var jsonData = new { OrganicManures = organicManures };
            string jsonString = JsonConvert.SerializeObject(jsonData);

            (bool success, Error? error) = await _organicManureLogic.AddOrganicManuresAsync(jsonString);
            if (!success || error != null)
            {
                TempData[_addOrganicManureError] = Resource.MsgWeCounldNotAddOrganicManure;
                await PrepareFieldDataAsync(model);
                return (success, error, View(model));
            }

            return (success, error, null);
        }

        // ===================== Success redirect =====================

        private IActionResult BuildSuccessRedirect(OrganicManureViewModel model, bool success)
        {
            string successMsg = Resource.lblOrganicManureCreatedSuccessfullyForAllField;
            string successMsgSecond = Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation;
            HttpContext.Session.Remove(_organicManureSessionKey);

            if (!model.IsComingFromRecommendation)
            {
                return RedirectToAction(_harvestYearOverview, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear,
                    q = _farmDataProtector.Protect(success.ToString()),
                    r = _cropDataProtector.Protect(successMsg),
                    v = _cropDataProtector.Protect(successMsgSecond)
                });
            }
            else
            {
                string fieldId = model.FieldList[0];
                return RedirectToAction(_recommendations, "Crop", new
                {
                    q = model.EncryptedFarmId,
                    r = _fieldDataProtector.Protect(fieldId),
                    s = model.EncryptedHarvestYear,
                    t = _cropDataProtector.Protect(Resource.lblOrganicManureCreatedSuccessfullyForAllField),
                    u = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedNutrientRecommendation)
                });
            }
        }

        private async Task<(OrganicManureViewModel model, Error error)> ProcessNVZClosedPeriodWarningAsync(OrganicManureViewModel model, Farm farm)
        {
            Error error = null;

            if (model?.OrganicManures == null || !model.OrganicManures.Any())
                return (model, error);

            foreach (var organicManure in model.OrganicManures)
            {
                int? fieldId = organicManure.FieldID;

                if (!fieldId.HasValue)
                    continue;

                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId.Value);

                if (field == null ||
                    !field.IsWithinNVZ.GetValueOrDefault() ||
                    IsOtherManureType(model.ManureTypeId))
                {
                    continue;
                }

                (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(organicManure.ManagementPeriodID);

                if (managementPeriod == null)
                    continue;

                (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.GetValueOrDefault());

                if (crop == null)
                    continue;

                (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(
                        crop.CropTypeID ?? 0);

                // NMaxLimitEngland is 0 for:
                // Winter beans, Spring beans, Peas, Market pick peas
                if (cropTypeLinkingResponse?.NMaxLimitEngland != 0)
                {
                    (model, error) = await IsClosedPeriodWarningMessage(
                        model,
                        field.IsWithinNVZ.Value,
                        farm.RegisteredOrganicProducer.Value,
                        fieldId.Value,
                        farm,
                        crop.SowingDate,
                        managementPeriod.CropID.Value);
                }
            }

            return (model, error);
        }

        private OrganicManureViewModel? GetOrganicDataBeforeUpdateFromSession()
        {
            if (HttpContext.Session.Exists("OrganicDataBeforeUpdate"))
            {
                return HttpContext.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicDataBeforeUpdate");
            }

            return null;
        }

        private void SetOrganicDataBeforeUodate(OrganicManureViewModel model)
        {
            HttpContext.Session.SetObjectAsJson("OrganicDataBeforeUpdate", model);
        }

        private void ValidateGrassDefoliation(OrganicManureViewModel model)
        {
            if (!model.IsAnyCropIsGrass.HasValue || !model.IsAnyCropIsGrass.Value)
            {
                return;
            }

            if (model.GrassCropCount.HasValue &&
                model.GrassCropCount > 1 &&
                model.IsSameDefoliationForAll == null)
            {
                ModelState.AddModelError(
                    _isSameDefoliationForAll,
                    string.Format(
                        _formatIndexKey,
                        Resource.lblForMultipleDefoliation,
                        Resource.lblNotSet));
            }

            int i = 0;

            foreach (var defoliation in model.DefoliationList)
            {
                if (model.IsSameDefoliationForAll.HasValue &&
                    model.IsSameDefoliationForAll.Value &&
                    model.GrassCropCount > 1 &&
                    defoliation.Defoliation == null)
                {
                    ModelState.AddModelError(
                        $"DefoliationList[{i}].Defoliation",
                        string.Format(
                            _formatIndexKey,
                            Resource.lblWhichCutOrGrazingInThisInorganicApplicationForAllField,
                            Resource.lblNotSet));
                }
                else if (defoliation.Defoliation == null)
                {
                    ModelState.AddModelError(
                        $"DefoliationList[{i}].Defoliation",
                        string.Format(
                            _formatIndexKey,
                            string.Format(
                                Resource.lblWhichCutOrGrazingInThisInorganicApplicationForInField,
                                defoliation.FieldName),
                            Resource.lblNotSet));
                }

                i++;
            }
        }
        private async Task<Error> ValidateDoubleCropSelectionAsync(OrganicManureViewModel model)
        {
            if (model.DoubleCrop != null || !model.IsDoubleCropAvailable)
            {
                return null;
            }

            foreach (string fieldId in model.FieldList)
            {
                (List<Crop> cropList, Error error) =
                    await _cropLogic.FetchCropPlanByFieldIdAndYear(
                        Convert.ToInt32(fieldId),
                        model.HarvestYear.Value);

                if (error != null)
                {
                    return error;
                }

                if (cropList != null && cropList.Count == 2)
                {
                    var field =
                        await _fieldLogic.FetchFieldByFieldId(
                            Convert.ToInt32(fieldId));

                    ModelState.AddModelError(
                        "FieldName",
                        string.Format(
                            _formatIndexKey,
                            string.Format(
                                Resource.lblWhichCropIsThisManureApplication,
                                field.Name),
                            Resource.lblNotSet));

                    break;
                }
            }

            return null;
        }
        private IActionResult HandleError(OrganicManureViewModel model, Error error)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                TempData["ConditionsAffectingNutrientsError"] = error.Message;
                return RedirectToAction(_conditionsAffectingNutrients);
            }

            HttpContext.Session.Remove(_organicManureSessionKey);
            return RedirectToHarvestYearPage(model, error);
        }

        private async Task<(Error? error, OrganicManureViewModel model)> PrepareFieldDataAsync(
    OrganicManureViewModel model)
        {
            (List<CommonResponse> fieldList, var error) =
                await _organicManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(
                    model.HarvestYear.Value,
                    model.FarmId.Value,
                    GetCropGroupName(model));

            if (error != null)
            {
                return (error, model);
            }

            PopulateSelectedFields(model, fieldList);

            await PopulateExistingOrganicManureFieldsAsync(model);

            return (null, model);
        }

        private static string? GetCropGroupName(OrganicManureViewModel model)
        {
            return model.FieldGroup.Equals(Resource.lblSelectSpecificFields) ||
                   model.FieldGroup.Equals(Resource.lblAll)
                ? null
                : model.FieldGroup;
        }

        private void PopulateSelectedFields(
            OrganicManureViewModel model,
            List<CommonResponse> fieldList)
        {
            bool isSpecificFieldSelection =
                model.FieldGroup.Equals(Resource.lblSelectSpecificFields) ||
                model.FieldGroup.Equals(Resource.lblAll);

            if (!isSpecificFieldSelection || fieldList.Count == 0)
            {
                return;
            }

            var fieldNames = fieldList
                .Where(field => model.FieldList.Contains(field.Id.ToString()))
                .OrderBy(field => field.Name)
                .Select(field => field.Name)
                .ToList();

            ViewBag.SelectedFields = fieldNames;

            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                ViewBag.Fields = fieldList;
            }

            if (model.FieldList?.Count == 1)
            {
                model.FieldName = fieldNames.FirstOrDefault();
            }
        }

        private async Task PopulateExistingOrganicManureFieldsAsync(
            OrganicManureViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                return;
            }

            (List<FertiliserAndOrganicManureUpdateResponse> organicResponse, Error? error) =
                await _organicManureLogic.FetchFieldWithSameDateAndManureType(
                    Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedOrgManureId)),
                    model.FarmId.Value,
                    model.HarvestYear.Value);

            if (!string.IsNullOrWhiteSpace(error?.Message) ||
                organicResponse == null ||
                organicResponse.Count == 0)
            {
                return;
            }

            var selectListItem = ToSelectList(
                organicResponse.DistinctBy(f => f.Id),
                f => f.Id.ToString(),
                f => f.Name);

            ViewBag.Fields = selectListItem
                .OrderBy(x => x.Text)
                .ToList();
        }
        private IActionResult RedirectToHarvestYearPage(OrganicManureViewModel model, Error error)
        {
            TempData["ErrorOnHarvestYearOverview"] = error.Message;
            return RedirectToAction(_harvestYearOverview, "Crop", new
            {
                id = model.EncryptedFarmId,
                year = model.EncryptedHarvestYear
            });
        }
        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Organic Manure Controller : BackCheckAnswer() post action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            model.IsCheckAnswer = false;
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedOrgManureId) && (!model.IsComingFromRecommendation))
            {
                HttpContext.Session.Remove(_organicManureSessionKey);
                return RedirectToAction(_harvestYearOverview, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            else if (!string.IsNullOrWhiteSpace(model.EncryptedOrgManureId) && (model.IsComingFromRecommendation))
            {
                HttpContext.Session.Remove(_organicManureSessionKey);
                return RedirectToRecommendation(model);
            }
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> AutumnCropNitrogenUptake(string? f)
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptake() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (f != null)
            {
                int fieldId = Convert.ToInt32(_organicManureProtector.Unprotect(f));
                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                model.EncryptedFieldId = f;
                ViewBag.FieldName = field.Name;
                ViewBag.CropTypeName = model.CropTypeName;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes?.FirstOrDefault(x => x.EncryptedFieldId == f)?.AutumnCropNitrogenUptake;
            }
            if (model.FieldList.Count == 1)
            {
                Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(model.FieldList[0]));
                ViewBag.FieldName = field.Name;
                ViewBag.CropTypeName = model.CropTypeName;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes[0].AutumnCropNitrogenUptake;
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutumnCropNitrogenUptake(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptake() post action called");
            if (!ModelState.IsValid)
            {
                ReplaceNumericError(_autumnCropNitrogenUptakeKey, _autumnCropNitrogenUptakeKey, Resource.MsgEnterValidNumericValueBeforeContinuing);
            }

            if (model.AutumnCropNitrogenUptake == null)
            {
                ModelState.AddModelError(_autumnCropNitrogenUptakeKey, Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.AutumnCropNitrogenUptake != null && model.AutumnCropNitrogenUptake < 0)
            {
                ModelState.AddModelError(_autumnCropNitrogenUptakeKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }
            if (model.AutumnCropNitrogenUptake != null)
            {
                decimal value = model.AutumnCropNitrogenUptake.Value;

                if (value % 1 != 0)
                {
                    ModelState.AddModelError(_autumnCropNitrogenUptakeKey, Resource.lblEnterANumberWhichIsAnIntegerValue);
                }

            }

            if (!ModelState.IsValid)
            {
                Field field = await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(_organicManureProtector.Unprotect(model.EncryptedFieldId)));
                ViewBag.FieldName = field.Name;
                ViewBag.CropTypeName = model.CropTypeName;
                model.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptakes?.FirstOrDefault(x => x.EncryptedFieldId == model.EncryptedFieldId)?.AutumnCropNitrogenUptake;
                return View(_autumnCropNitrogenUptakeKey, model);
            }

            if (model.FieldList.Count == 1)
            {
                model.AutumnCropNitrogenUptakes[0].AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0;

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_conditionsAffectingNutrients);
            }
            else
            {
                model.AutumnCropNitrogenUptakes?
                     .Where(detail => detail.EncryptedFieldId == model.EncryptedFieldId)
                     .ToList()
                     .ForEach(detail => detail.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake ?? 0);

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction("AutumnCropNitrogenUptakeDetail");
            }

        }

        [HttpGet]
        public async Task<IActionResult> SoilDrainageEndDate()
        {
            _logger.LogTrace($"Organic Manure Controller : SoilDrainageEndDate() action called");

            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilDrainageEndDate(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : SoilDrainageEndDate() post action called");
            ValidateSoilDrainageEndDate();

            AddErrorIfNull(model.SoilDrainageEndDate, _soilDrainageEndDateKey, Resource.MsgEnterADateBeforeContinuing);
            ValidateMinMaxSoilDrainageDate(model);
            if (!ModelState.IsValid)
            {
                return View(_soilDrainageEndDateKey, model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        private void ValidateMinMaxSoilDrainageDate(OrganicManureViewModel model)
        {
            if (model.SoilDrainageEndDate != null)
            {
                if (DateTime.TryParseExact(model.SoilDrainageEndDate.Value.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    ModelState.AddModelError(_soilDrainageEndDateKey, Resource.MsgEnterValidDate);
                }

                if (!(model.SoilDrainageEndDate.Value.Month >= (int)NMP.Commons.Enums.Month.January && model.SoilDrainageEndDate.Value.Month <= (int)NMP.Commons.Enums.Month.April))
                {
                    ModelState.AddModelError(_soilDrainageEndDateKey, Resource.MsgSoilDrainageEndDate1stJan30Apr);
                }
            }
        }

        private void ValidateSoilDrainageEndDate()
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_soilDrainageEndDateKey))
            {
                var dateError = ModelState[_soilDrainageEndDateKey].Errors.Count > 0 ?
                                ModelState[_soilDrainageEndDateKey].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, _soilDrainageEndDateKey)))
                {
                    ModelState[_soilDrainageEndDateKey].Errors.Clear();
                    ModelState[_soilDrainageEndDateKey].Errors.Add(Resource.MsgEnterValidDate);
                }
                if (dateError != null && (
                    dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, _soilDrainageEndDateKey)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, _soilDrainageEndDateKey)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, _soilDrainageEndDateKey)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, _soilDrainageEndDateKey)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, _soilDrainageEndDateKey)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, _soilDrainageEndDateKey))))
                {
                    ModelState[_soilDrainageEndDateKey].Errors.Clear();
                    ModelState[_soilDrainageEndDateKey].Errors.Add(Resource.MsgTheDateMustInclude);
                }


            }
        }
        [HttpGet]
        public async Task<IActionResult> RainfallWithinSixHour()
        {
            _logger.LogTrace($"Organic Manure Controller : RainfallWithinSixHour() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            (List<RainTypeResponse> rainType, Error error) = await _organicManureLogic.FetchRainTypeList();
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
            AddErrorIfNull(model.RainfallWithinSixHoursID, "RainfallWithinSixHoursID", Resource.MsgSelectAnOptionBeforeContinuing);
            if (!ModelState.IsValid)
            {
                return View("RainfallWithinSixHour", model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfall()
        {
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfall() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
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

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfallManual()
        {
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfallManual() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EffectiveRainfallManual(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : EffectiveRainfallManual() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_totalRainfallKey))
            {
                var RainfallError = ModelState[_totalRainfallKey].Errors.Count > 0 ?
                                ModelState[_totalRainfallKey].Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_totalRainfallKey].RawValue, _totalRainfallKey)))
                {
                    ModelState[_totalRainfallKey].Errors.Clear();
                    decimal decimalValue;
                    if (decimal.TryParse(ModelState[_totalRainfallKey].RawValue.ToString(), out decimalValue))
                    {
                        ModelState[_totalRainfallKey].Errors.Add(Resource.MsgIfUserEnterDecimalValueInRainfall);
                    }
                    else
                    {
                        ModelState[_totalRainfallKey].Errors.Add(Resource.MsgForEffectiveRainfallManual);
                    }
                }
            }

            AddErrorIfNull(model.TotalRainfall, _totalRainfallKey, Resource.MsgEnterRainfallAmountBeforeContinuing);

            if (model.TotalRainfall != null && model.TotalRainfall < 0)
            {
                ModelState.AddModelError(_totalRainfallKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }

            if (!ModelState.IsValid)
            {
                return View("EffectiveRainfallManual", model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> Windspeed()
        {
            _logger.LogTrace($"Organic Manure Controller : Windspeed() action called");
            if (!TryGetSessionModel(nameof(Windspeed), out var model, out var redirect))
            {
                return redirect;
            }
            (List<WindspeedResponse> windspeeds, Error? error) = await _organicManureLogic.FetchWindspeedList();

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
            AddErrorIfNull(model.WindspeedID, "WindspeedID", Resource.MsgSelectAWindConditionBeforeContinuing);

            if (!ModelState.IsValid)
            {
                return await Task.FromResult(View("Windspeed", model));
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> TopsoilMoisture()
        {
            _logger.LogTrace($"Organic Manure Controller : TopsoilMoisture() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            (List<MoistureTypeResponse> moisterTypes, Error error) = await _organicManureLogic.FetchMoisterTypeList();
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
            AddErrorIfNull(model.MoistureTypeId, "MoistureTypeId", Resource.MsgSelectATopsoilWetnessConditionBeforeContinuing);

            if (!ModelState.IsValid)
            {
                return View("TopsoilMoisture", model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }
        private async Task<(OrganicManureViewModel, Error?)> IsNFieldLimitWarningMessage(OrganicManureViewModel model, int managementId, int fieldId, Farm farm)
        {
            Error? error = null;
            decimal defaultNitrogen = model.OrganicManures?.FirstOrDefault()?.N ?? 0;
            List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                (model, error) = await ApplyRow2NFieldLimitWarningAsync(model, managementId, fieldId, farm, warningList, defaultNitrogen);

                bool isScotland = model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;
                bool isCompost = model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenCompost || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost;

                if (isScotland || isCompost)
                {
                    bool hasEarlyError;
                    (model, error, hasEarlyError) = await ProcessCompostAndScotlandWarningsAsync(model, managementId, fieldId, farm, warningList, defaultNitrogen, isScotland, isCompost);
                    if (hasEarlyError)
                    {
                        return (model, error);
                    }
                }
            }

            return (model, error);
        }

        // Shared by every warning-application block: looks up the warning by key/country and stamps the Nmax fields onto the model.
        private static void ApplyNmaxWarning(OrganicManureViewModel model, List<WarningResponse> warningList, Farm farm, string warningKey)
        {
            WarningResponse? warning = warningList
                .FirstOrDefault(x => x.CountryID == farm.CountryID &&
                                     string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));

            if (warning != null)
            {
                model.NmaxWarningHeader = warning.Header;
                model.NmaxWarningCodeID = warning.WarningCodeID;
                model.NmaxWarningLevelID = warning.WarningLevelID;
                model.NmaxWarningPara1 = warning.Para1;
                model.NmaxWarningPara2 = warning.Para2;
                model.NmaxWarningPara3 = warning.Para3;
            }
        }

        // Shared lookup pattern used by every fetch call below: resolves the organic-manure id argument for a given management id.
        private static int? GetOrganicManureIdOrNull(OrganicManureViewModel model, int managementId)
        {
            if (model.UpdatedOrganicIds != null && model.UpdatedOrganicIds.Count > 0)
            {
                return model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.OrganicManureId).FirstOrDefault();
            }

            return null;
        }

        // Warning excel sheet row 2: >250 kg/ha total N in the last 365 days (non-compost manure types only).
        private async Task<(OrganicManureViewModel, Error?)> ApplyRow2NFieldLimitWarningAsync(
            OrganicManureViewModel model, int managementId, int fieldId, Farm farm, List<WarningResponse> warningList, decimal defaultNitrogen)
        {
            if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenCompost || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost)
            {
                return (model, null);
            }

            int? organicManureId = GetOrganicManureIdOrNull(model, managementId);
            decimal previousAppliedTotalN;
            Error? error;
            (previousAppliedTotalN, error) = await _organicManureLogic.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(
                fieldId, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, false, organicManureId);

            if (error == null)
            {
                decimal currentApplicationNitrogen = defaultNitrogen * model.ApplicationRate.Value;
                decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

                if (totalN > 250)
                {
                    model.IsOrgManureNfieldLimitWarning = true;
                    var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimit.ToString();
                    ApplyNmaxWarning(model, warningList, farm, warningKey);
                }
            }

            return (model, error);
        }

        // The isScotland || isCompost branch: rows 4, 6, and the Scotland PAS100 check.
        // Returns HasEarlyError = true only when the managementIds fetch fails, matching the original early return.

#pragma warning disable S107
        private async Task<(OrganicManureViewModel Model, Error? Error, bool HasEarlyError)> ProcessCompostAndScotlandWarningsAsync(
            OrganicManureViewModel model, int managementId, int fieldId, Farm farm, List<WarningResponse> warningList,
            decimal defaultNitrogen, bool isScotland, bool isCompost)
        {
            var cropTypeIdsForTrigger = new HashSet<int> {
        (int)NMP.Commons.Enums.CropTypes.CiderApples,
        (int)NMP.Commons.Enums.CropTypes.CulinaryApples,
        (int)NMP.Commons.Enums.CropTypes.DessertApples,
        (int)NMP.Commons.Enums.CropTypes.Cherries,
        (int)NMP.Commons.Enums.CropTypes.Pears,
        (int)NMP.Commons.Enums.CropTypes.Plums
    };
            bool showPAS100Warning = true;

            (List<int> managementIds, Error? error) = await _organicManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(
                model.HarvestYear.Value, fieldId.ToString(), null, null);

            if (error != null)
            {
                return (model, error, true);
            }

            int managementPeriodId = model.OrganicManures[0].ManagementPeriodID;
            int? organicManureId = null;

            if (model.UpdatedOrganicIds?.Count > 0)
            {
                int targetManagementId = managementIds.Count > 1 ? managementPeriodId : managementIds[0];
                organicManureId = model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId == targetManagementId).Select(x => x.OrganicManureId).FirstOrDefault();
            }

            (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear ?? 0, false);

            if (!cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId) || isScotland)
            {
                (model, showPAS100Warning) = await ApplyRow4WarningAsync(model, managementId, fieldId, farm, warningList, defaultNitrogen, isScotland, isCompost, organicManureId, showPAS100Warning);
            }

            if (cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId))
            {
                model = await ApplyRow6WarningAsync(model, managementId, fieldId, farm, warningList, defaultNitrogen);
            }

            if (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland && isCompost)
            {
                model = await ApplyPAS100WarningAsync(model, managementId, fieldId, farm, warningList, defaultNitrogen, showPAS100Warning);
            }

            return (model, null, false);
        }

#pragma warning restore S107

        // Warning excel sheet row 4: >500 total N from compost applications in the last 730 days.

#pragma warning disable S107
        private async Task<(OrganicManureViewModel Model, bool ShowPAS100Warning)> ApplyRow4WarningAsync(
            OrganicManureViewModel model, int managementId, int fieldId, Farm farm, List<WarningResponse> warningList,
            decimal defaultNitrogen, bool isScotland, bool isCompost, int? organicManureId, bool showPAS100Warning)
        {
            int? previousAppliedOrganicManureId = GetOrganicManureIdOrNull(model, managementId);

            decimal previousAppliedTotalN;
            Error? error;

            if (!isScotland)
            {
                (previousAppliedTotalN, error) = await _organicManureLogic.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(
                    fieldId, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, false, true, previousAppliedOrganicManureId);
            }
            else
            {
                (previousAppliedTotalN, error) = await _organicManureLogic.FetchTotalNBasedByFieldIdAppDate(
                    fieldId, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, false, previousAppliedOrganicManureId);
            }

            if (error == null)
            {
                decimal currentApplicationNitrogen = defaultNitrogen * model.ApplicationRate.Value;
                decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

                (bool isGreenCompostExistIn2Year, _) = await _organicManureLogic.CheckGreenCompostExistanceByDateRange(
                    fieldId, model.ApplicationDate.Value.AddDays(-729).ToString(_dateStringLiteral), model.ApplicationDate.Value.ToString(_dateStringLiteral), organicManureId);

                if ((!isScotland || isGreenCompostExistIn2Year || isCompost) && totalN > 500)
                {
                    model.IsOrgManureNfieldLimitWarning = true;
                    showPAS100Warning = false;
                    var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompost.ToString();
                    ApplyNmaxWarning(model, warningList, farm, warningKey);
                }
            }

            return (model, showPAS100Warning);
        }

#pragma warning restore S107
        // Warning excel sheet row 6: >1000 total N from compost/mulch applications in the last 1460 days (apple/cherry/pear/plum crop types).
        private async Task<OrganicManureViewModel> ApplyRow6WarningAsync(
            OrganicManureViewModel model, int managementId, int fieldId, Farm farm, List<WarningResponse> warningList, decimal defaultNitrogen)
        {
            int? organicManureId = GetOrganicManureIdOrNull(model, managementId);

            (decimal previousAppliedTotalN, Error? error) = await _organicManureLogic.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(
                fieldId, model.ApplicationDate.Value.AddDays(-1459), model.ApplicationDate.Value, false, true, organicManureId);

            if (error == null)
            {
                decimal currentApplicationNitrogen = defaultNitrogen * model.ApplicationRate.Value;
                decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

                if (totalN > 1000)
                {
                    model.IsOrgManureNfieldLimitWarning = true;
                    var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostMulch.ToString();
                    ApplyNmaxWarning(model, warningList, farm, warningKey);
                }
            }

            return model;
        }

        // Scotland PAS100 check: >250 total N from compost applications in the last 365 days, only when row 4 didn't already fire.
        private async Task<OrganicManureViewModel> ApplyPAS100WarningAsync(
            OrganicManureViewModel model, int managementId, int fieldId, Farm farm, List<WarningResponse> warningList,
            decimal defaultNitrogen, bool showPAS100Warning)
        {
            int? organicManureId = GetOrganicManureIdOrNull(model, managementId);

            (decimal previousAppliedTotalN, Error? error) = await _organicManureLogic.FetchTotalNBasedByFieldIdAppDateAndIsGreenCompost(
                fieldId, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, true, organicManureId);

            if (error == null)
            {
                decimal currentApplicationNitrogen = defaultNitrogen * model.ApplicationRate.Value;
                decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

                if (totalN > 250 && showPAS100Warning)
                {
                    model.IsOrgManureNfieldLimitWarning = true;
                    var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostPAS.ToString();
                    ApplyNmaxWarning(model, warningList, farm, warningKey);
                }
            }

            return model;
        }

        //warning excel sheet row no. 8
        private async Task<(OrganicManureViewModel, Error?)> IsNMaxWarningMessage(OrganicManureViewModel model, int fieldId, int managementId, bool isGetCheckAnswer, Farm farm, FieldDetailResponse fieldDetail, OrganicManureDataViewModel organicManure)
        {
            int farmCountryId = model.FarmCountryId ?? 0;
            bool isWinterOilseedRapeAutumn = false;
            decimal defaultNitrogen = DefaultNitrogenInitilise(model);
            Error? error = null;
            List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
            var (isApplicationRateAndDateAvailable, cropId) = await IsApplicationRateAndDateAvailable(model, managementId);
            if (!isApplicationRateAndDateAvailable)
            {
                return (model, error);
            }

            decimal totalN = 0;
            decimal previousApplicationsN = 0;
            (Crop crop, error) = await _cropLogic.FetchCropById(cropId);
            CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
            Recommendation? recommendation = null;
            int? scotlandNmax = null;
            int residueGroup = 0;
            bool isScotland = farmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;
            (_, cropTypeLinking, recommendation, scotlandNmax, residueGroup) = await BindScotlandNMaxAndNResidueGroup(fieldId, managementId, isScotland, crop, cropTypeLinking, recommendation);

            int? nmaxLimitEnglandOrWales = FetchNmaxLimitForEnglandAndWales(model, cropTypeLinking);
            bool flowControl = false;
            bool IsNmaxWarningNeeded = IsNmaxWarningRequired(scotlandNmax, isScotland, nmaxLimitEnglandOrWales);
            if (!IsNmaxWarningNeeded)
            {
                return (model, error);
            }

            (_, previousApplicationsN) = await FetchPreviousApplicationN(model, managementId, error, cropId, previousApplicationsN);

            int? percentOfTotalNForUseInNmaxCalculation = await BindPercentOfTotalNForUseInNmaxCalculation(model);
            decimal nMaxLimit = 0;
            if (percentOfTotalNForUseInNmaxCalculation != null)
            {
                (flowControl, (OrganicManureViewModel, Error?) value, totalN, nMaxLimit, bool hasSpecialManure) = await HandlePercentOfTotalNaxWarning(model, fieldId, defaultNitrogen, previousApplicationsN, percentOfTotalNForUseInNmaxCalculation);
                if (!flowControl)
                {
                    return value;
                }
                (flowControl, (OrganicManureViewModel, Error?) data, nMaxLimit) = await BindNmaxForIsNMaxWarningMessage(model, fieldDetail, isWinterOilseedRapeAutumn, crop, residueGroup, nmaxLimitEnglandOrWales, scotlandNmax, hasSpecialManure);
                if (!flowControl)
                {
                    return data;
                }
                await BindNmaxWarning(model, farm, totalN, crop, scotlandNmax, nmaxLimitEnglandOrWales, nMaxLimit);

                return (model, error);
            }
            if (isGetCheckAnswer)
            {
                (decimal? availableNFromMannerOutput, _) = await GetAvailableNFromMannerOutput(model, organicManure);

                (flowControl, (OrganicManureViewModel, Error?) value, nMaxLimit) = await BindNmaxWarningIfCheckAnswerTrue(model, fieldId, fieldDetail, isWinterOilseedRapeAutumn, crop, residueGroup, nmaxLimitEnglandOrWales, scotlandNmax);
                if (!flowControl)
                {
                    return value;
                }
                decimal? totalApplicationN = previousApplicationsN + availableNFromMannerOutput;
                bool isNeedToSetWarning = (farm.CountryID != (int)NMP.Commons.Enums.FarmCountry.Scotland && (crop.CropTypeID.Value != (int)NMP.Commons.Enums.CropTypes.Grass || crop.SwardTypeID == (int)NMP.Commons.Enums.SwardType.Grass));
                int? nmaxValue = isNeedToSetWarning ? nmaxLimitEnglandOrWales : scotlandNmax;
                await PrepareNMaxWarningIfCheckAnswerTrue(model, farm, warningList, totalApplicationN, crop, nmaxValue, nMaxLimit);

            }
            return (model, error);
        }

        private static decimal DefaultNitrogenInitilise(OrganicManureViewModel model)
        {
            return model.OrganicManures?
                    .FirstOrDefault()?
                    .N ?? 0;
        }

        private static int? FetchNmaxLimitForEnglandAndWales(OrganicManureViewModel model, CropTypeLinkingResponse cropTypeLinking)
        {
            return (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Wales ? cropTypeLinking.NMaxLimitWales : cropTypeLinking.NMaxLimitEngland);
        }

        private async Task<(bool, int)> IsApplicationRateAndDateAvailable(OrganicManureViewModel model, int managementId)
        {
            (ManagementPeriod managementPeriod, _) = await _cropLogic.FetchManagementperiodById(managementId);
            int cropId = managementPeriod.CropID ?? 0;

            bool isApplicationRateAndDateAvailable = (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue);

            return (isApplicationRateAndDateAvailable, cropId);

        }
#pragma warning disable S107
        private async Task<(bool flowControl, (OrganicManureViewModel, Error?) value, decimal)> BindNmaxWarningIfCheckAnswerTrue(OrganicManureViewModel model, int fieldId, FieldDetailResponse fieldDetail, bool isWinterOilseedRapeAutumn, Crop crop, int residueGroup, int? nmaxLimitEnglandOrWales, int? scotlandNmax)
        {
            decimal nMaxLimit = 0;
            (List<int> currentYearManureTypeIds, Error? error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);
            (List<int> previousYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return (flowControl: false, value: (model, error), nMaxLimit);

            }
            bool hasSpecialManure = Functions.HasSpecialManure(currentYearManureTypeIds, null) || Functions.HasSpecialManure(previousYearManureTypeIds, null);
            (bool flowControl, (OrganicManureViewModel, Error?) value, nMaxLimit) = await BindNmaxForIsNMaxWarningMessage(model, fieldDetail, isWinterOilseedRapeAutumn, crop, residueGroup, nmaxLimitEnglandOrWales, scotlandNmax, hasSpecialManure);
            if (!flowControl)
            {
                return (flowControl: false, value: value, nMaxLimit);
            }

            return (flowControl: true, value: default, nMaxLimit);
        }
#pragma warning restore S107
        private async Task PrepareNMaxWarningIfCheckAnswerTrue(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, decimal? totalApplicationN, Crop crop, int? nMaxValue, decimal nMaxLimit)
        {
            if (totalApplicationN > nMaxLimit)
            {
                string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                model.IsNMaxLimitWarning = true;
                var warningKey = NMP.Commons.Enums.WarningKey.NMaxLimit.ToString();

                WarningResponse? warning = warningList
                    .FirstOrDefault(x => x.CountryID == farm.CountryID &&
                                         string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));


                if (warning != null)
                {
                    SetNmaxLimitWarning(model, warning, string.Format(warning.Para2, cropTypeName, nMaxValue, nMaxLimit));
                }
            }
        }

        private async Task<(bool flowControl, (OrganicManureViewModel, Error?) value, decimal, decimal, bool)> HandlePercentOfTotalNaxWarning(OrganicManureViewModel model, int fieldId, decimal defaultNitrogen, decimal previousApplicationsN, int? percentOfTotalNForUseInNmaxCalculation)
        {

            decimal nMaxLimit = 0;
            bool hasSpecialManure = false;
            (bool flowControl, (OrganicManureViewModel, Error?) value, List<int> currentYearManureTypeIds, List<int> previousYearManureTypeIds, decimal totalN) = await CalculationWarningForPercentOfTotalN(model, fieldId, defaultNitrogen, previousApplicationsN, percentOfTotalNForUseInNmaxCalculation);
            if (!flowControl)
            {
                return (flowControl: false, value: (model, value.Item2), totalN, nMaxLimit, hasSpecialManure);
            }

            hasSpecialManure = Functions.HasSpecialManure(currentYearManureTypeIds, null) || Functions.HasSpecialManure(previousYearManureTypeIds, null);

            return (flowControl: true, value: default, totalN, nMaxLimit, hasSpecialManure);
        }

        private static bool IsNmaxWarningRequired(int? scotlandNmax, bool isScotland, int? nmaxLimitEnglandOrWales)
        {
            return ((!isScotland && nmaxLimitEnglandOrWales != null) || (isScotland && scotlandNmax != null));
        }

        private async Task<(bool flowControl, (OrganicManureViewModel, Error?) value, List<int>, List<int>, decimal)> CalculationWarningForPercentOfTotalN(OrganicManureViewModel model, int fieldId, decimal defaultNitrogen, decimal previousApplicationsN, int? percentOfTotalNForUseInNmaxCalculation)
        {
            decimal currentApplicationNitrogen = 0;

            decimal decimalOfTotalNForUseInNmaxCalculation = Convert.ToDecimal(percentOfTotalNForUseInNmaxCalculation / 100.0);
            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value * decimalOfTotalNForUseInNmaxCalculation);
            decimal totalN = previousApplicationsN + currentApplicationNitrogen;

            //fetch current year manure type ids
            (List<int> currentYearManureTypeIds, _) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value, false);

            //fetch previous year manure type ids
            (List<int> previousYearManureTypeIds, _) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), model.HarvestYear.Value - 1, false);

            return (flowControl: true, value: default, currentYearManureTypeIds, previousYearManureTypeIds, totalN);
        }

        private async Task BindNmaxWarning(OrganicManureViewModel model, Farm farm, decimal totalN, Crop crop, int? scotlandNmax, int? nmaxLimitEnglandOrWales, decimal nMaxLimit)
        {
            if (totalN > nMaxLimit)
            {
                List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
                bool isScotland = farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.Scotland;
                string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                model.IsNMaxLimitWarning = true;
                var warningKey = NMP.Commons.Enums.WarningKey.NMaxLimit.ToString();

                WarningResponse? warning = warningList
                    .FirstOrDefault(x => x.CountryID == farm.CountryID &&
                                         string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));
                if (warning != null)
                {
                    if (!isScotland && (crop.CropTypeID.Value != (int)NMP.Commons.Enums.CropTypes.Grass || crop.SwardTypeID == (int)NMP.Commons.Enums.SwardType.Grass))
                    {
                        SetNmaxLimitWarning(model, warning, string.Format(warning.Para2, cropTypeName, nmaxLimitEnglandOrWales, nMaxLimit));
                    }
                    if (isScotland)
                    {
                        SetNmaxLimitWarning(model, warning, string.Format(warning.Para2, cropTypeName, scotlandNmax, nMaxLimit));
                    }
                }
            }

        }

        private async Task<(Error?, CropTypeLinkingResponse cropTypeLinking, Recommendation? recommendation, int? scotlandNmax, int residueGroup)> BindScotlandNMaxAndNResidueGroup(int fieldId, int managementId, bool isScotland, Crop crop, CropTypeLinkingResponse cropTypeLinking, Recommendation? recommendation)
        {
            Error? error = null;
            int? scotlandNmax = null; int residueGroup = 0;
            if (isScotland)
            {
                Field field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                (recommendation, error) = await _cropLogic.FetchRecommendationByManagementPeriodId(managementId);

                if (recommendation != null)
                {
                    residueGroup = Convert.ToInt32(recommendation.NIndex);
                }

                (scotlandNmax, error) = await _organicManureLogic.FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroup(crop.CropTypeID.Value, field.SoilTypeID ?? 0, residueGroup);
                if (scotlandNmax == null)
                {
                    scotlandNmax = Convert.ToInt32(recommendation?.CropN);
                }
            }
            else
            {
                (cropTypeLinking, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
            }

            return (error, cropTypeLinking, recommendation, scotlandNmax, residueGroup);
        }

#pragma warning disable S107
        private async Task<(bool flowControl, (OrganicManureViewModel, Error?) value, decimal)> BindNmaxForIsNMaxWarningMessage(OrganicManureViewModel model, FieldDetailResponse fieldDetail, bool isWinterOilseedRapeAutumn, Crop crop, int residueGroup, int? nmaxLimitEnglandOrWales, int? scotlandNmax, bool hasSpecialManure)
        {
            decimal nMaxLimit = nmaxLimitEnglandOrWales ?? 0;

            if (model.FarmCountryId != (int)NMP.Commons.Enums.FarmCountry.Scotland)
            {
                nMaxLimit = OrganicManureNMaxLimitLogic.NMaxLimit(Convert.ToInt32(nMaxLimit), crop.Yield == null ? null : crop.Yield.Value, fieldDetail.SoilTypeName, crop.CropInfo1 == null ? null : crop.CropInfo1.Value, crop.CropTypeID.Value, crop.PotentialCut ?? 0, hasSpecialManure, crop.DefoliationSequenceID);

            }
            else
            {
                int? winterRainfall = null;
                (ExcessRainfalls excessRainfalls, Error error) = await _farmLogic.FetchExcessRainfallsAsync(model.FarmId ?? 0, model.HarvestYear ?? 0);
                if (HasError(error))
                {
                    return (flowControl: false, value: (model, error), nMaxLimit);
                }

                winterRainfall = excessRainfalls != null ? excessRainfalls.WinterRainfall : null;

                nMaxLimit = OrganicManureNMaxLimitLogic.NMaxLimitScotland(Convert.ToInt32(scotlandNmax), crop.Yield ?? null, fieldDetail.SoilTypeName, crop.CropInfo1 ?? null, crop.CropTypeID.Value, crop.PotentialCut ?? 0, crop.DefoliationSequenceID, winterRainfall, residueGroup, isWinterOilseedRapeAutumn);

            }

            return (flowControl: true, value: default, nMaxLimit);
        }

        private async Task<int?> BindPercentOfTotalNForUseInNmaxCalculation(OrganicManureViewModel model)
        {
            int? percentOfTotalNForUseInNmaxCalculation = null;
            (ManureType manureType, _) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
            if (manureType != null)
            {
                percentOfTotalNForUseInNmaxCalculation = manureType.PercentOfTotalNForUseInNmaxCalculation;
            }

            return percentOfTotalNForUseInNmaxCalculation;
        }

        private async Task<(Error? error, decimal previousApplicationsN)> FetchPreviousApplicationN(OrganicManureViewModel model, int managementId, Error? error, int cropId, decimal previousApplicationsN)
        {
            if (model.UpdatedOrganicIds != null && model.UpdatedOrganicIds.Count > 0)
            {
                (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(cropId, false, null, model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.OrganicManureId).FirstOrDefault());
            }
            else
            {
                (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(cropId, false, null, null);
            }

            return (error, previousApplicationsN);
        }

        private static void SetNmaxLimitWarning(OrganicManureViewModel model, WarningResponse warningResponse, string para2 = null)
        {
            model.CropNmaxLimitWarningHeader = warningResponse.Header;
            model.CropNmaxLimitWarningCodeID = warningResponse.WarningCodeID;
            model.CropNmaxLimitWarningLevelID = warningResponse.WarningLevelID;

            model.CropNmaxLimitWarningPara1 = warningResponse.Para1;
            model.CropNmaxLimitWarningPara2 = para2;
            model.CropNmaxLimitWarningPara3 = warningResponse.Para3;
        }


        private async Task<(OrganicManureViewModel, Error?)> IsEndClosedPeriodFebruaryWarningMessage(OrganicManureViewModel model, Farm farm, int cropId, int fieldId)
        {
            if (farm == null)
                return (model, null);

            var warningList = await _warningLogic.FetchAllWarningAsync();

            var (manureType, error) = await GetManureType(model);
            if (error != null)
                return (model, error);

            SetHighN(model, manureType);

            string? closedPeriod = await GetClosedPeriod(model, farm, model.HighReadilyAvailableNitrogen ?? false);

            bool isSlurry = IsSlurry(model.ManureTypeId);
            bool isPoultry = IsPoultryManure(model.ManureTypeId);


            if (IsNonScotland(model))
            {
                return HandleNonScotland(model, farm, warningList, closedPeriod, isSlurry, isPoultry);
            }

            return await HandleScotland(model, farm, warningList, cropId, fieldId, closedPeriod, isPoultry);
        }
        private static void SetHighN(OrganicManureViewModel model, ManureType? manureType)
        {
            model.HighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen;
        }

        private static bool IsNonScotland(OrganicManureViewModel model)
        {
            return model.FarmCountryId != (int)NMP.Commons.Enums.FarmCountry.Scotland;
        }
        private (OrganicManureViewModel, Error?) HandleNonScotland(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, string? closedPeriod, bool isSlurry, bool isPoultry)
        {
            if (!IsWithinClosedPeriodAndFeb(model, closedPeriod))
                return (model, null);

            ApplyWarnings(model, farm, warningList, isSlurry, isPoultry);

            return (model, null);
        }
#pragma warning disable S107
        private async Task<(OrganicManureViewModel, Error?)> HandleScotland(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, int cropId, int fieldId, string? closedPeriod, bool isPoultry)
        {
            var (organicManureId, error) = await GetOrganicManureId(model, fieldId);
            if (error != null)
                return (model, error);

            bool isRanExceptPoultry =
                (model.HighReadilyAvailableNitrogen ?? false) && !isPoultry;

            await ApplyScotlandWarningsIfNeeded(model, farm, warningList, cropId, closedPeriod, organicManureId, isRanExceptPoultry, isPoultry);

            return (model, null);
        }
#pragma warning restore S107
        private async Task<(int?, Error?)> GetOrganicManureId(OrganicManureViewModel model, int fieldId)
        {
            int managementPeriodId = model.OrganicManures[0].ManagementPeriodID;

            var (managementIds, error) =
                await _organicManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId.ToString(), null, null);

            if (error != null)
                return (null, error);

            if (model.UpdatedOrganicIds?.Count > 0)
            {
                int targetManagementId =
                    managementIds.Count > 1 ? managementPeriodId : managementIds[0];

                int? organicManureId = model.UpdatedOrganicIds
                    .Where(x => x.ManagementPeriodId == targetManagementId)
                    .Select(x => x.OrganicManureId)
                    .FirstOrDefault();

                return (organicManureId, null);
            }

            return (null, null);
        }
#pragma warning disable S107
        private async Task ApplyScotlandWarningsIfNeeded(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, int cropId, string? closedPeriod, int? organicManureId, bool isRanExceptPoultry, bool isPoultry)
        {
            if (!model.ApplicationDate.HasValue ||
                string.IsNullOrWhiteSpace(closedPeriod) ||
                !closedPeriod.Contains("to"))
                return;

            var parts = closedPeriod.Split(" to ", StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return;

            DateTime applicationDate = model.ApplicationDate.Value;
            int year = applicationDate.Year;

            DateTime closedStartDate = DateTime.ParseExact($"{parts[0]} {year}", "d MMMM yyyy", CultureInfo.InvariantCulture);

            // Feb window
            string period = $"{parts[1]} to 14 February";
            var (febStart, febEnd) =
                GetClosedPeriodDates(period, model.HarvestYear ?? 0);

            // 28-day pre-closed window
            DateTime preStart = closedStartDate.AddDays(-28);
            DateTime preEnd = closedStartDate.AddDays(-1);

            bool isInFebPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(applicationDate, febStart, febEnd);


            bool isInPreClosedPeriod =
                applicationDate >= preStart && applicationDate <= preEnd;

            if (!isInFebPeriod && !isInPreClosedPeriod)
                return;

            DateTime startDate = isInFebPeriod ? febStart.Value : preStart;
            DateTime endDate = isInFebPeriod ? febEnd.Value : preEnd;

            var (totalApplicationRate, _) =
                await _organicManureLogic.FetchTotalApplicationRateByDateRange(
                    cropId,
                    startDate.ToString(_dateStringLiteral),
                    endDate.ToString(_dateStringLiteral),
                    organicManureId,
                    isPoultry);

            totalApplicationRate = model.ApplicationRate + totalApplicationRate;

            ApplyWarningsRanAndPoultryTotalRateLimit(
                model,
                farm,
                warningList,
                isRanExceptPoultry,
                totalApplicationRate,
                isPoultry, isInFebPeriod);
        }
#pragma warning restore S107
        private async Task<(ManureType?, Error?)> GetManureType(OrganicManureViewModel model)
        {
            if (model?.FarmRB209CountryID == null || model.ManureGroupIdForFilter == null)
                return (null, null);

            var (list, error) = await FetchManureTypeList(
                model.ManureGroupIdForFilter.Value,
                model.FarmRB209CountryID.Value);

            if (error != null || list.Count == 0)
                return (null, error);

            return (list.FirstOrDefault(x => x.Id == model.ManureTypeId), null);
        }
        private static bool IsSlurry(int? manureTypeId)
        {
            if (!manureTypeId.HasValue) return false;

            var slurryTypes = new[]
            {
                (int)NMP.Commons.Enums.ManureTypes.PigSlurry,
                (int)NMP.Commons.Enums.ManureTypes.CattleSlurry,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryStrainerBox,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryWeepingWall,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryMechanicalSeparator,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedPigSlurryLiquidPortion
            };

            return slurryTypes.Contains(manureTypeId.Value);
        }
        private static bool IsPoultryManure(int? manureTypeId)
        {
            return manureTypeId == (int)NMP.Commons.Enums.ManureTypes.PoultryManure;
        }
        private static bool IsWithinClosedPeriodAndFeb(OrganicManureViewModel model, string? closedPeriod)
        {
            if (!model.ApplicationDate.HasValue)
                return false;

            return WarningWithinPeriod.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, closedPeriod) == true;
        }

        private void ApplyWarningsRanAndPoultryTotalRateLimit(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, bool isRanExceptPoultry, decimal? totalApplicationRate, bool isPoultry, bool isInFebPeriod)
        {
            if (isRanExceptPoultry && totalApplicationRate > 30)
            {
                var warning = warningList.FirstOrDefault(x => x.CountryID == farm.CountryID && string.Equals(x.WarningKey?.Trim(),
                        isInFebPeriod ? NMP.Commons.Enums.WarningKey.SlurryMaxRate.ToString() : NMP.Commons.Enums.WarningKey.Slurry4WeekPriorToClosedPeriodStart.ToString(), StringComparison.OrdinalIgnoreCase));
                SetWarning(model, warning);
            }

            if (isPoultry && totalApplicationRate > 5)
            {
                var warning = _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, isInFebPeriod ? NMP.Commons.Enums.WarningKey.PoultryManureMaxApplicationRate.ToString() : NMP.Commons.Enums.WarningKey.Poultry4WeekPriorToClosedPeriodStart.ToString()).Result;

                SetWarning(model, warning);
            }
        }

        private void ApplyWarnings(OrganicManureViewModel model, Farm farm, List<WarningResponse> warningList, bool isSlurry, bool isPoultry)
        {
            if (isSlurry && model.ApplicationRate > 30)
            {
                var warning = warningList.FirstOrDefault(x =>
                    x.CountryID == farm.CountryID &&
                    string.Equals(x.WarningKey?.Trim(),
                        NMP.Commons.Enums.WarningKey.SlurryMaxRate.ToString(),
                        StringComparison.OrdinalIgnoreCase));

                SetWarning(model, warning);
            }

            if (isPoultry && model.ApplicationRate > 8)
            {
                var warning = _warningLogic
                    .FetchWarningByCountryIdAndWarningKeyAsync(
                        model.FarmCountryId ?? 0,
                        NMP.Commons.Enums.WarningKey.PoultryManureMaxApplicationRate.ToString())
                    .Result;

                SetWarning(model, warning);
            }
        }
        private static void SetWarning(OrganicManureViewModel model, WarningResponse? warning)
        {
            if (warning == null) return;

            model.IsEndClosedPeriodFebruaryWarning = true;
            model.EndClosedPeriodEndFebWarningHeader = warning.Header;
            model.EndClosedPeriodEndFebWarningCodeID = warning.WarningCodeID;
            model.EndClosedPeriodEndFebWarningLevelID = warning.WarningLevelID;
            model.EndClosedPeriodEndFebWarningPara1 = warning.Para1;
            model.EndClosedPeriodEndFebWarningPara2 = warning.Para2;
            model.EndClosedPeriodEndFebWarningPara3 = warning.Para3;
        }
        private async Task<(OrganicManureViewModel, Error?)> IsClosedPeriodWarningMessage(
    OrganicManureViewModel model,
    bool isWithinNVZ,
    bool registeredOrganicProducer,
    int fieldId,
    Farm farm,
    DateTime? sowingDate,
    int cropId)
        {
            var (manureTypeList, error) = await GetManureTypes(model);
            if (error != null) return (model, error);

            SetHighReadilyAvailableNitrogen(model, manureTypeList);

            var (updatedModel, closedError, closedPeriod, isWithinClosedPeriod) =
                await HandleClosedPeriodWarningLogic(
                    model,
                    isWithinNVZ,
                    registeredOrganicProducer,
                    model.HighReadilyAvailableNitrogen ?? false,
                    farm);

            if (closedError != null) return (model, closedError);

            model = updatedModel;

            (model, error) = await HandleTwentyDayRule(model, fieldId, closedPeriod);
            if (error != null) return (model, error);

            if (IsScotland(model))
            {
                (model, error) = await HandleScotlandRules(
                    model,
                    manureTypeList,
                    isWithinNVZ,
                    fieldId,
                    sowingDate,
                    cropId);

                if (error != null) return (model, error);
            }

            model.ClosedPeriod = closedPeriod;
            model.IsWithinClosedPeriod = isWithinClosedPeriod;

            return (model, null);
        }
        private async Task<(List<ManureType>, Error?)> GetManureTypes(OrganicManureViewModel model)
        {
            if (!model.FarmRB209CountryID.HasValue || !model.ManureGroupIdForFilter.HasValue)
                return (new List<ManureType>(), null);

            return await FetchManureTypeList(
                model.ManureGroupIdForFilter.Value,
                model.FarmRB209CountryID.Value);
        }
        private static void SetHighReadilyAvailableNitrogen(
    OrganicManureViewModel model,
    List<ManureType> manureTypeList)
        {
            var manureType = manureTypeList
                .FirstOrDefault(x => x.Id == model.ManureTypeId);

            model.HighReadilyAvailableNitrogen =
                manureType?.HighReadilyAvailableNitrogen;
        }
        private static bool IsScotland(OrganicManureViewModel model)
        {
            return model.FarmCountryId ==
                (int)NMP.Commons.Enums.FarmCountry.Scotland;
        }
        private async Task<(OrganicManureViewModel, Error?)> HandleScotlandRules(
    OrganicManureViewModel model,
    List<ManureType> manureTypeList,
    bool isWithinNVZ,
    int fieldId,
    DateTime? sowingDate,
    int cropId)
        {
            if (model.HighReadilyAvailableNitrogen == true && isWithinNVZ)
            {
                var (updatedModel, error) =
                    await HandleScotlandHighN(model, fieldId, sowingDate);

                if (error != null) return (model, error);
                model = updatedModel;
            }

            if (IsLivestockCondition(model, manureTypeList, isWithinNVZ))
            {
                var (updatedModel, error) =
                    await HandleLivestockManureRule(model, fieldId, cropId);

                if (error != null) return (model, error);
                model = updatedModel;
            }

            return (model, null);
        }
        private async Task<(OrganicManureViewModel, Error?)> HandleScotlandHighN(
    OrganicManureViewModel model,
    int fieldId,
    DateTime? sowingDate)
        {
            var (cropTypeResponse, error) =
                await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(
                    fieldId, model.HarvestYear ?? 0, false);

            if (error != null) return (model, error);

            if (cropTypeResponse.CropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                return (model, null);
            }

            if (IsWithinRange(model.HarvestYear ?? 0, model.ApplicationDate.Value, 7, 1, 7, 31))
            {
                (HarvestYearResponseHeader? harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansDetailsByFarmId((model.HarvestYear ?? 0) + 1, model.FarmId ?? 0);
                DateTime? nextHarvestYearEarliestPlan = harvestYearPlanResponse?.CropDetails?.Where(x => x.FieldID == fieldId).Min(x => x.PlantingDate);
                if ((nextHarvestYearEarliestPlan == null || (nextHarvestYearEarliestPlan.Value - model.ApplicationDate.Value).TotalDays >= 43))
                {
                    return await ScotlandJulyHighNWarning(model);
                }
            }

            if (IsWithinRange((model.HarvestYear ?? 0) - 1, model.ApplicationDate.Value, 8, 1, 9, 30) &&
                (sowingDate == null ||
                 (sowingDate.Value - model.ApplicationDate.Value).TotalDays >= 43))
            {
                return await ScotlandJulyHighNWarning(model);
            }

            return (model, null);
        }
        private static bool IsWithinRange(int harvestYear, DateTime applicationDate, int startMonth, int startDay, int endMonth, int endDay)
        {

            DateTime start = new DateTime(harvestYear, startMonth, startDay, 0, 0, 0, DateTimeKind.Utc);

            DateTime end = new DateTime(harvestYear, endMonth, endDay, 0, 0, 0, DateTimeKind.Utc);

            return WarningWithinPeriod.IsApplicationDateWithinDateRange(applicationDate, start, end);
        }
        private static bool IsLivestockCondition(OrganicManureViewModel model, List<ManureType> manureTypeList, bool isWithinNVZ)
        {
            return model.ManureGroupIdForFilter.HasValue &&
                   manureTypeList.Any(x =>
                       x.ManureGroupId ==
                       (int)NMP.Commons.Enums.ManureGroup.LivestockManure) &&
                   isWithinNVZ;
        }
        private async Task<(OrganicManureViewModel, Error?)> HandleLivestockManureRule(OrganicManureViewModel model, int fieldId, int cropId)
        {
            var (organicManureId, error) =
                await GetOrganicManureId(model, fieldId);

            if (error != null) return (model, error);

            var (exists, existsError) =
                await _organicManureLogic.FetchLivestockManureExistanceByDateRange(
                    cropId,
                    model.ApplicationDate.Value.AddDays(-20).ToString(_dateStringLiteral),
                    model.ApplicationDate.Value.ToString(_dateStringLiteral),
                    organicManureId);

            if (existsError != null || !exists)
                return (model, existsError);

            await ApplyLivestockWarning(model);

            return (model, null);
        }
        private async Task ApplyLivestockWarning(OrganicManureViewModel model)
        {
            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;

            var warning =
                await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0,
                    NMP.Commons.Enums.WarningKey
                        .AllowWeeksBetweenSlurryPoultryApplications.ToString());

            model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader = warning.Header;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID = warning.WarningCodeID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID = warning.WarningLevelID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = warning.Para1;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = warning.Para2;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3 = warning.Para3;
        }


        private async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupIdForFilter, int FarmRB209CountryId)
        {
            if (IsOtherManureType(manureGroupIdForFilter))
            {
                return await _mannerLogic.FetchManureTypeList((int)NMP.Commons.Enums.ManureGroup.AnotherTypeOfOrganicMaterial, FarmRB209CountryId);
            }
            else
            {
                return await _mannerLogic.FetchManureTypeList(manureGroupIdForFilter, FarmRB209CountryId);
            }
        }

        private async Task<(OrganicManureViewModel, Error?, string?, bool)> HandleClosedPeriodWarningLogic(
            OrganicManureViewModel model, bool isWithinNVZ, bool registeredOrganicProducer, bool isHighReadilyAvailableNitrogen, Farm farm)
        {
            Error? error = null;
            string? closedPeriod = string.Empty;
            bool isWithinClosedPeriod = false;
            // Non-organic farm, high N, NVZ
            if (!registeredOrganicProducer && isHighReadilyAvailableNitrogen && isWithinNVZ)
            {
                closedPeriod = await GetClosedPeriod(model, farm, isHighReadilyAvailableNitrogen);

                (model, error) = await HandleNonOrganicHighNWarning(model);
                return (model, error, closedPeriod, isWithinClosedPeriod);
            }

            // Organic farm, high N, NVZ
            if (registeredOrganicProducer && isHighReadilyAvailableNitrogen && isWithinNVZ)
            {
                (model, error, closedPeriod, isWithinClosedPeriod) = await HandleOrganicHighNWarning(model, farm);
                return (model, error, closedPeriod, isWithinClosedPeriod);
            }

            return (model, null, closedPeriod, isWithinClosedPeriod);
        }


        private async Task<(OrganicManureViewModel, Error?)> HandleNonOrganicHighNWarning(
            OrganicManureViewModel model)
        {
            bool isWithinClosedPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(
                model.ApplicationDate, model.ClosedPeriodStartDate, model.ClosedPeriodEndDate);

            if (isWithinClosedPeriod)
            {
                //warning excel sheet row no. 10
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureClosedPeriod.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara2 = warning.Para2;
                model.ClosedPeriodWarningPara3 = warning.Para3;
                model.IsClosedPeriodWarning = true;
            }
            return (model, null);
        }

        private async Task<(OrganicManureViewModel, Error?)> ScotlandJulyHighNWarning(OrganicManureViewModel model)
        {
            //scotland warning excel sheet row no. 26
            WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.RanManureJulyToSep.ToString());
            model.ApplicationJulyToSeptHeader = warning.Header;
            model.ApplicationJulyToSeptCodeID = warning.WarningCodeID;
            model.ApplicationJulyToSeptLevelID = warning.WarningLevelID;
            model.ApplicationJulyToSeptPara1 = warning.Para1;
            model.ApplicationJulyToSeptPara2 = warning.Para2;
            model.ApplicationJulyToSeptPara3 = warning.Para3;
            model.IsApplicationJulyToSeptWarning = true;

            return (model, null);
        }


        private async Task<(OrganicManureViewModel, Error?, string, bool)> HandleOrganicHighNWarning(
            OrganicManureViewModel model, Farm farm)
        {
            Error? error = null;
            string? closedPeriod = string.Empty;
            bool isWithinClosedPeriod = false;
            (CropTypeResponse cropTypeResponse, error) = await _organicManureLogic.FetchCropTypeByFieldIdAndHarvestYear(Convert.ToInt32(model.FieldList[0]), model.HarvestYear ?? 0, false);
            if (error != null) return (model, error, closedPeriod, isWithinClosedPeriod);

            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;

            closedPeriod = await GetClosedPeriod(model, farm, null);

            isWithinClosedPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(
                model.ApplicationDate, model.ClosedPeriodStartDate, model.ClosedPeriodEndDate);
            HashSet<int> cropTypeIdsForTrigger = WarningWithinPeriod.FilteredCropForWarning();

            if (isWithinClosedPeriod && !cropTypeIdsForTrigger.Contains(cropTypeResponse.CropTypeId))
            {
                //warning excel sheet row no. 12
                model.IsClosedPeriodWarning = true;
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureClosedPeriodOrganicFarm.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara3 = warning.Para3;
            }

            // England-specific warning for Winter Oilseed Rape or Grass
            DateTime endOfOctober = new DateTime((model.HarvestYear ?? 0) - 1, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            if ((cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape ||
                 cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass) &&
                WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, endOfOctober, model.ClosedPeriodEndDate) &&
                (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.England))
            {
                //warning excel sheet row no. 17
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureDateOnly.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.IsClosedPeriodWarning = true;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara3 = warning.Para3;
            }

            return (model, null, closedPeriod, isWithinClosedPeriod);
        }

        private async Task<(OrganicManureViewModel, Error?)> HandleTwentyDayRule(
            OrganicManureViewModel model, int fieldId, string closedPeriod)
        {
            Error? error = null;

            bool? isWithinClosedPeriodAndFebruary =
                WarningWithinPeriod.CheckEndClosedPeriodAndFebruary(
                    model.ApplicationDate.Value,
                    closedPeriod);

            if (isWithinClosedPeriodAndFebruary != true)
            {
                return (model, null);
            }

            (List<int> managementIds, error) =
                await _organicManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(
                    model.HarvestYear.Value,
                    fieldId.ToString(),
                    null,
                    null);

            if (error != null)
            {
                return (model, error);
            }

            int managementPeriodId = model.OrganicManures[0].ManagementPeriodID;
            int? organicManureId = null;

            if (model.UpdatedOrganicIds?.Count > 0)
            {
                int targetManagementId =
                    managementIds.Count > 1 ? managementPeriodId : managementIds[0];

                organicManureId = model.UpdatedOrganicIds
                    .Where(x => x.ManagementPeriodId == targetManagementId)
                    .Select(x => x.OrganicManureId)
                    .FirstOrDefault();
            }

            (bool isOrganicManureExist, error) =
                await _organicManureLogic.FetchOrganicManureExistanceByDateRange(
                    managementPeriodId,
                    model.ApplicationDate.Value.AddDays(-20).ToString(_dateStringLiteral),
                    model.ApplicationDate.Value.ToString(_dateStringLiteral),
                    false,
                    organicManureId, true);

            if (error != null || !isOrganicManureExist)
            {
                return (model, error);
            }

            bool isSlurry = IsSlurryType(model.ManureTypeId);
            bool isPoultryManure =
                model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.PoultryManure;

            if (!isSlurry && !isPoultryManure)
            {
                return (model, null);
            }

            // warning excel sheet row no. 21
            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;

            WarningResponse warning =
                await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.FarmCountryId ?? 0,
                    NMP.Commons.Enums.WarningKey
                        .AllowWeeksBetweenSlurryPoultryApplications.ToString());

            model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader = warning.Header;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID = warning.WarningCodeID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID = warning.WarningLevelID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = warning.Para1;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = warning.Para2;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3 = warning.Para3;

            return (model, null);
        }

        private static bool IsSlurryType(int? manureTypeId)
        {
            return manureTypeId == (int)NMP.Commons.Enums.ManureTypes.PigSlurry ||
                   manureTypeId == (int)NMP.Commons.Enums.ManureTypes.CattleSlurry ||
                   manureTypeId == (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryStrainerBox ||
                   manureTypeId == (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryWeepingWall ||
                   manureTypeId == (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryMechanicalSeparator ||
                   manureTypeId == (int)NMP.Commons.Enums.ManureTypes.SeparatedPigSlurryLiquidPortion;
        }


        private async Task<(bool, string, Error?)> IsClosedPeriodStartAndEndFebExceedNRateException(OrganicManureViewModel model, int fieldId, Farm farm, int managementPeriodId)
        {
            Error? error = null;
            string warningMsg = string.Empty;

            (ManagementPeriod managementPeriod, error) = await _cropLogic.FetchManagementperiodById(managementPeriodId);
            int cropId = managementPeriod.CropID ?? 0;

            if (farm == null)
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }

            List<ManureType> manureTypeList;
            (manureTypeList, error) = await GetManureTypeList(model);


            if (error != null)
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }

            bool isHighReadilyAvailableNitrogen = false;
            if (manureTypeList.Count > 0)
            {
                var manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
                isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
            }

            FieldDetailResponse fieldDetail = new FieldDetailResponse();
            if (model.HarvestYear != null)
            {
                (fieldDetail, error) = await _fieldLogic.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
            }

            if (error != null)
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }

            Field field = await _fieldLogic.FetchFieldByFieldId(fieldId);
            bool isFieldIsInNVZ = field.IsWithinNVZ != null && field.IsWithinNVZ.Value;

            if (!(farm.RegisteredOrganicProducer.Value && isHighReadilyAvailableNitrogen && isFieldIsInNVZ))
            {
                return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
            }

            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(fieldId));
            if (cropsResponse.Count > 0)
            {
                await ApplyClosedPeriodEndFebWarningsAsync(model, fieldId, managementPeriodId, cropId, cropsResponse);
            }

            return (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150, warningMsg, error);
        }

        // Resolves the organic-manure id argument used by the various N-rate fetch calls below.
        private static int? GetOrganicManureIdForManagementId(OrganicManureViewModel model, int managementPeriodId)
        {
            if (model.UpdatedOrganicIds != null && model.UpdatedOrganicIds.Count > 0)
            {
                return model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId == managementPeriodId).Select(x => x.OrganicManureId).FirstOrDefault();
            }

            return null;
        }

        // Dispatches to each of the independent, crop-type/country-specific warning checks.
        private async Task ApplyClosedPeriodEndFebWarningsAsync(
            OrganicManureViewModel model, int fieldId, int managementPeriodId, int cropId, List<Crop> cropsResponse)
        {
            HashSet<int> cropTypeIdsForTrigger = WarningWithinPeriod.FilteredCropForWarning();
            HashSet<int> brassicaCrops = WarningWithinPeriod.BrassicaCrops();

            int cropTypeId = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.CropTypeID).FirstOrDefault() ?? 0;

            DateTime endDateFebruary = new DateTime((model.HarvestYear ?? 0), 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
            DateTime endOfOctober = new DateTime((model.HarvestYear ?? 0) - 1, 10, 31, 0, 0, 0, DateTimeKind.Utc);

            decimal totalNitrogen = model.OrganicManures?.FirstOrDefault()?.N ?? 0;

            (List<int> managementIds, _) = await _organicManureLogic.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, fieldId.ToString(), null, null);

            // warning excel sheet row no. 15
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass && model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyGrassWarningAsync(model, cropId, managementPeriodId, endOfOctober, totalNitrogen);
            }

            // warning excel sheet row no. 13
            if ((cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Commons.Enums.CropTypes.SaladOnions)
                && model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyAllumWarningAsync(model, cropId, managementPeriodId, endDateFebruary, totalNitrogen);
            }

            // wales warning
            if (cropTypeIdsForTrigger.Contains(cropTypeId) && model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Wales)
            {
                await ApplyAllumWarningAsync(model, cropId, managementPeriodId, endDateFebruary, totalNitrogen);
            }

            // warning excel sheet row no. 14
            if (brassicaCrops.Contains(cropTypeId) && model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyBrassicaWarningAsync(model, cropId, managementPeriodId, endDateFebruary, totalNitrogen, managementIds);
            }

            // warning excel sheet row no. 16
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape && model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyWinterOilseedRapeWarningAsync(model, cropId, managementPeriodId, endOfOctober, totalNitrogen);
            }
        }

        // Row 15: Grass, England.
        private async Task ApplyGrassWarningAsync(OrganicManureViewModel model, int cropId, int managementPeriodId, DateTime endOfOctober, decimal totalNitrogen)
        {
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endOfOctober);
            if (!isWithinDateRange)
            {
                return;
            }

            int? organicManureId = GetOrganicManureIdForManagementId(model, managementPeriodId);
            (decimal totalN, _) = await _organicManureLogic.FetchTotalNBasedOnCropIdAndAppDate(cropId, model.ClosedPeriodStartDate.Value, endOfOctober, false, organicManureId);

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            if (currentNitrogen != null && (currentNitrogen > 40 || currentNitrogen + totalN > 150))
            {
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateGrass.ToString());
                SetStartClosedPeriodEndFebWarning(model, warning, warning.Para2);
            }
        }

        // Row 13: Asparagus / Bulb Onions / Salad Onions, England.
        private async Task ApplyAllumWarningAsync(OrganicManureViewModel model, int cropId, int managementPeriodId, DateTime endDateFebruary, decimal totalNitrogen)
        {
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endDateFebruary);
            if (!isWithinDateRange)
            {
                return;
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            int? organicManureId = GetOrganicManureIdForManagementId(model, managementPeriodId);
            (decimal totalN, _) = await _organicManureLogic.FetchTotalNBasedOnCropIdAndAppDate(cropId, model.ClosedPeriodStartDate.Value, endDateFebruary, false, organicManureId);

            if (currentNitrogen + totalN > 150)
            {
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRate.ToString());
                SetStartClosedPeriodEndFebWarning(model, warning, warning.Para2);
            }
        }

        // Wales warning.


        // Row 14: Brassica crops, England.
        private async Task ApplyBrassicaWarningAsync(
            OrganicManureViewModel model, int cropId, int managementPeriodId, DateTime endDateFebruary, decimal totalNitrogen, List<int> managementIds)
        {
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endDateFebruary);
            if (!isWithinDateRange || managementIds.Count == 0)
            {
                return;
            }

            int? organicManureId = GetOrganicManureIdForManagementId(model, managementPeriodId);
            (decimal totalN, Error? error) = await _organicManureLogic.FetchTotalNBasedOnCropIdAndAppDate(cropId, model.ClosedPeriodStartDate.Value, endDateFebruary, false, organicManureId);

            bool isOrganicManureExistWithin4Weeks;
            if (model.UpdatedOrganicIds != null && model.UpdatedOrganicIds.Count > 0)
            {
                int? previousManureId = model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId == managementIds[0]).Select(x => x.OrganicManureId).FirstOrDefault();
                (isOrganicManureExistWithin4Weeks, error) = await _organicManureLogic.FetchOrganicManureExistanceByDateRange(
                    managementPeriodId, model.ApplicationDate.Value.AddDays(-27).ToString(_dateStringLiteral), model.ApplicationDate.Value.ToString(_dateStringLiteral), false, previousManureId, false);
            }
            else
            {
                (isOrganicManureExistWithin4Weeks, error) = await _organicManureLogic.FetchOrganicManureExistanceByDateRange(
                    managementPeriodId, model.ApplicationDate.Value.AddDays(-27).ToString(_dateStringLiteral), model.ApplicationDate.Value.ToString(_dateStringLiteral), false, null, false);
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            if (currentNitrogen != null && (currentNitrogen > 50 || currentNitrogen + totalN > 150 || isOrganicManureExistWithin4Weeks))
            {
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateWeeks.ToString());
                SetStartClosedPeriodEndFebWarning(model, warning, warning.Para2);
            }
        }

        // Row 16: Winter Oilseed Rape, England.
        private async Task ApplyWinterOilseedRapeWarningAsync(OrganicManureViewModel model, int cropId, int managementPeriodId, DateTime endOfOctober, decimal totalNitrogen)
        {
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, model.ClosedPeriodStartDate, endOfOctober);
            if (!isWithinDateRange)
            {
                return;
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            int? organicManureId = GetOrganicManureIdForManagementId(model, managementPeriodId);
            (decimal totalN, _) = await _organicManureLogic.FetchTotalNBasedOnCropIdAndAppDate(cropId, model.ClosedPeriodStartDate.Value, endOfOctober, false, organicManureId);

            if (currentNitrogen + totalN > 150)
            {
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateOSR.ToString());
                SetStartClosedPeriodEndFebWarning(model, warning, warning.Para2);
            }
        }

        private static void SetStartClosedPeriodEndFebWarning(OrganicManureViewModel model, WarningResponse warningResponse, string para2 = null)
        {
            model.StartClosedPeriodEndFebWarningHeader = warningResponse.Header;
            model.StartClosedPeriodEndFebWarningCodeID = warningResponse.WarningCodeID; //81a
            model.StartClosedPeriodEndFebWarningLevelID = warningResponse.WarningLevelID;
            model.StartClosedPeriodEndFebWarningPara1 = warningResponse.Para1;
            model.StartClosedPeriodEndFebWarningPara2 = para2;
            model.StartClosedPeriodEndFebWarningPara3 = warningResponse.Para3;
            model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = true;
        }




        private async Task<(decimal?, Error?)> GetAvailableNFromMannerOutput(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            Error error = new Error();
            decimal? availableNfromManner = null;


            SetOrganicManureValues(model);

            //logic for AvailableNForNMax column that will be used to get sum of previous manure applications


            (FarmResponse farmData, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (farmData == null && (!string.IsNullOrWhiteSpace(error?.Message)))
            {
                return (availableNfromManner, error);
            }

            (string? mannerJsonString, Error? mannerOutputError) = await BindManureOutput(farmData, organicManure, model);
            if (string.IsNullOrWhiteSpace(mannerJsonString))
            {
                return (availableNfromManner, mannerOutputError);
            }
            (MannerCalculateNutrientResponse mannerCalculateNutrientResponse, error) = await _organicManureLogic.FetchMannerCalculateNutrient(mannerJsonString);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return (availableNfromManner, error);
            }

            availableNfromManner = mannerCalculateNutrientResponse.CurrentCropAvailableN;
            return (availableNfromManner, error);
        }
        private async Task<(string?, Error?)> BindManureOutput(FarmResponse farmData, OrganicManureDataViewModel organic, OrganicManureViewModel model)
        {
            Error? error = null;
            (Crop? crop, Field? fieldData, List<Country> countryList, error) = await FetchDataForMannerOutput(organic);
            if (crop == null && !string.IsNullOrWhiteSpace(error?.Message))
            {
                return (null, error);
            }
            bool isLateSownCropType = false;
            if (crop.SowingDate is DateTime sowingDate)
            {
                DateTime cutoff = new DateTime(
                    sowingDate.Year, 9, 15, 0, 0, 0,
                    DateTimeKind.Unspecified);

                isLateSownCropType = sowingDate.Date > cutoff;
            }

            int topSoilID = 0;
            int subSoilID = 0;
            (SoilTypeSoilTextureResponse soilTexture, error) = await _organicManureLogic.FetchSoilTypeSoilTextureBySoilTypeId(fieldData.SoilTypeID ?? 0);
            if (soilTexture != null)
            {
                topSoilID = soilTexture.TopSoilID;
                subSoilID = soilTexture.SubSoilID;
            }
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(organic.ManureTypeID);
            if (error != null && string.IsNullOrWhiteSpace(error.Message))
            {
                return (null, error);
            }
            (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
            if (error != null && string.IsNullOrWhiteSpace(error.Message))
            {
                return (null, error);
            }
            var mannerOutput = new
            {
                runType = farmData.EnglishRules ? (int)NMP.Commons.Enums.RunType.PlanetEngland : (int)NMP.Commons.Enums.RunType.PlanetScotland,
                postcode = farmData.ClimateDataPostCode.Split(" ")[0],
                countryID = countryList.Where(x => x.ID == farmData.CountryID).Select(x => x.RB209CountryID).First(),
                field = new
                {
                    fieldID = fieldData.ID,
                    fieldName = fieldData.Name,
                    MannerCropTypeID = isLateSownCropType ? cropTypeLinkingResponse.LateSownMannerCropTypeID.Value : cropTypeLinkingResponse.MannerCropTypeID,
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
                                                    applicationDate = organic.ApplicationDate?.ToString(_dateStringLiteral),
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
                                                    endOfDrainageDate = organic.EndOfDrain.ToString(_dateStringLiteral),
                                                    rainfallPostApplication = organic.Rainfall,
                                                    windspeedID = organic.WindspeedID,
                                                    rainTypeID = organic.RainfallWithinSixHoursID,
                                                    topsoilMoistureID = organic.MoistureID
                                                }
                                            }
            };
            return (JsonConvert.SerializeObject(mannerOutput), error);


        }

        private async Task<(Crop?, Field?, List<Country>, Error?)> FetchDataForMannerOutput(OrganicManureDataViewModel organic)
        {
            Error? error = null;
            Field? fieldData = null;
            Crop? crop = null;
            List<Country> countryList = new List<Country>();
            (ManagementPeriod? managementPeriod, error) = await _cropLogic.FetchManagementperiodById(organic.ManagementPeriodID);
            if (managementPeriod != null && managementPeriod.CropID != null)
            {
                (crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
                if (crop != null && crop.FieldID != null)
                {
                    fieldData = await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value);
                    if (fieldData != null)
                    {
                        countryList = await _farmLogic.FetchCountryAsync();

                        return (crop, fieldData, countryList, error);
                    }
                }
            }
            return (crop, fieldData, countryList, error);
        }
        [HttpGet]
        public async Task<IActionResult> AutumnCropNitrogenUptakeDetail()
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptakeDetail() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AutumnCropNitrogenUptakeDetail(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : AutumnCropNitrogenUptakeDetail() post action called");

            if (!ModelState.IsValid)
            {
                return View("AutumnCropNitrogenUptakeDetail", model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }


        [HttpGet]
        public IActionResult OtherMaterialName()
        {
            _logger.LogTrace("Organic Manure Controller : OtherMaterialName() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            try
            {
                if (model == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in OtherMaterialName() get action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                    ModelState.AddModelError(_otherMaterialName, Resource.MsgEnterNameOfTheMaterial);
                }
                else
                {
                    (bool farmManureExist, Error error) =
                        await _organicManureLogic.FetchFarmManureTypeCheckByFarmIdAndManureTypeId(
                            model.FarmId.Value,
                            model.ManureTypeId.Value,
                            model.OtherMaterialName
                        );

                    if (string.IsNullOrWhiteSpace(error?.Message) && farmManureExist)
                        ModelState.AddModelError(_otherMaterialName, Resource.MsgThisManureTypeNameAreadyExist);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                foreach (var manure in model.OrganicManures)
                {
                    manure.ManureTypeName = model.OtherMaterialName;
                }
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in OtherMaterialName() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["ErrorOnVariety"] = ex.Message;
                return View(model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            if (model.IsDoubleCropAvailable)
            {
                return RedirectToAction(_doubleCropAction);
            }
            else
            {
                model.DoubleCrop = null;
            }

            if (model.IsAnyCropIsGrass == true)
            {
                return HandleGrassCrop(model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_manureApplyingDateAction);
        }


        private void BindDataForRemoveOrganic(string q, string r, string s, string? t, string? u, OrganicManureViewModel? model)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                model.EncryptedOrgManureId = q;
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.EncryptedFieldId = r;
                model.FieldList = new List<string>();
                model.FieldList.Add(_fieldDataProtector.Unprotect(r));
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
        }

        private async Task BindViewBegForRemoveOrganicManure(OrganicManureViewModel? model)
        {
            if (model != null && model.FieldList != null && model.FieldList.Count > 0)
            {
                (List<CommonResponse> fieldList, _) = await _fertiliserManureLogic.FetchFieldByFarmIdAndHarvestYearAndCropGroupName(model.HarvestYear.Value, model.FarmId.Value, null);
                if (fieldList.Count > 0)
                {
                    var fieldNames = fieldList
                                     .Where(field => model.FieldList.Contains(field.Id.ToString())).OrderBy(field => field.Name)
                                     .Select(field => field.Name)
                                     .ToList();

                    if (fieldNames.Count == 1)
                    {
                        model.FieldName = fieldNames[0];
                    }
                    else
                    {
                        model.FieldName = string.Empty;
                        ViewBag.SelectedFields = fieldNames.OrderBy(name => name).ToList();
                    }
                    ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());

                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> RemoveOrganicManure(string q, string r, string s, string? t, string? u, string? v)
        {
            _logger.LogTrace($"Organic  Manure Controller : RemoveOrganicManure() action called");
            OrganicManureViewModel? model = new OrganicManureViewModel();

            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    model = GetOrganicManureFromSession();
                    if (model == null)
                    {
                        return RedirectToAction(_farmList, "Farm");
                    }

                    await BindViewBegForRemoveOrganicManure(model);

                }
                else
                {
                    model.IsComingFromRecommendation = true;
                    BindDataForRemoveOrganic(q, r, s, t, u, model);
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "OrganicManure Controller : Exception in RemoveOrganicManure() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                if (model != null && model.IsComingFromRecommendation)
                {
                    TempData[_nutrientRecommendationsError] = ex.Message;
                    return RedirectToAction(_recommendations, "Crop", new { q = model.EncryptedFarmId, r = r, s = model.EncryptedHarvestYear });
                }

                TempData[_addOrganicManureError] = ex.Message;
                return RedirectToAction(_checkAnswer);
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOrganicManure(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : RemoveOrganicManure() post action called");
            Error? error = null;
            AddErrorIfNull(model.IsDeleteOrganic, "IsDeleteOrganic", Resource.MsgSelectAnOptionBeforeContinuing);
            if (!ModelState.IsValid)
            {
                await BindViewBegForRemoveOrganicManure(model);
                return View(model);
            }
            try
            {
                if (!model.IsDeleteOrganic.Value)
                {
                    return RedirectToAction(_checkAnswer);
                }
                else
                {
                    List<int> organicManureIds = await BindOrganicManureIds(model);

                    if (organicManureIds.Count > 0)
                    {
                        var result = new
                        {
                            organicManureIds
                        };

                        string jsonString = JsonConvert.SerializeObject(result);
                        (_, error) = await _organicManureLogic.DeleteOrganicManureByIdAsync(jsonString);
                        if (!string.IsNullOrWhiteSpace(error?.Message))
                        {
                            await BindViewBegForRemoveOrganicManure(model);
                            TempData["RemoveOrganicManureError"] = error.Message;
                            return View(model);
                        }
                        (bool flowControl, IActionResult? value) = RedirectForRemove(model);
                        if (!flowControl && value != null)
                        {
                            return value;
                        }
                    }
                }
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "OrganicManure Controller : Exception in RemoveOrganicManure() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["RemoveOrganicManureError"] = ex.Message;
                return View(model);
            }
            return View(model);


        }

        private async Task<List<int>> BindOrganicManureIds(OrganicManureViewModel model)
        {
            List<int> organicManureIds = new List<int>();
            if (model.IsComingFromRecommendation && (!string.IsNullOrWhiteSpace(model.EncryptedOrgManureId)))
            {
                ViewBag.EncryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                organicManureIds.Add(Convert.ToInt32(_cropDataProtector.Unprotect(model.EncryptedOrgManureId)));
            }
            else if (model.UpdatedOrganicIds?.Count > 0 && model.OrganicManures?.Count > 0)
            {
                foreach (string fieldId in model.FieldList)
                {
                    string fieldName = (await _fieldLogic.FetchFieldByFieldId(Convert.ToInt32(fieldId))).Name;

                    organicManureIds.AddRange(model.UpdatedOrganicIds.Where(organicManure => fieldName.Equals(organicManure.Name)).Select(organicManure => organicManure.OrganicManureId.Value));
                }
            }

            return organicManureIds;
        }

        private (bool flowControl, IActionResult? value) RedirectForRemove(OrganicManureViewModel model)
        {
            HttpContext.Session.Remove(_organicManureSessionKey);
            if (model.IsComingFromRecommendation)
            {
                if (model.FieldList != null && model.FieldList.Count > 0)
                {
                    string encryptedFieldId = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault());
                    if (!string.IsNullOrWhiteSpace(encryptedFieldId))
                    {
                        return (flowControl: false, value: RedirectToAction(_recommendations, "Crop", new { q = model.EncryptedFarmId, r = encryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationRemoved), u = _cropDataProtector.Protect(Resource.lblSelectFieldToSeeItsUpdatedNutrientRecommendations) }));
                    }
                }
            }
            else
            {
                return (flowControl: false, value: Redirect(Url.Action(_harvestYearOverview, "Crop", new { Id = model.EncryptedFarmId, year = model.EncryptedHarvestYear, q = Resource.lblTrue, r = _cropDataProtector.Protect(Resource.lblOrganicMaterialApplicationRemoved), v = _cropDataProtector.Protect(Resource.lblSelectFieldToSeeItsUpdatedNutrientRecommendations) }) + Resource.lblOrganicMaterialApplicationsForSorting));
            }

            return (flowControl: true, value: null);
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Organic Manure Controller : Cancel() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            try
            {
                if (model == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_addOrganicManureError] = ex.Message;
                return RedirectToAction(_checkAnswer);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(OrganicManureViewModel model)
        {
            _logger.LogTrace("Organic Manure Controller : Cancel() post action called");
            AddErrorIfNull(model.IsCancel, "IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
            if (!ModelState.IsValid)
            {
                return View("Cancel", model);
            }
            if (!model.IsCancel.Value)
            {
                return RedirectToAction(_checkAnswer);
            }
            else
            {
                HttpContext.Session.Remove(_organicManureSessionKey);
                if (!model.IsComingFromRecommendation)
                {
                    return RedirectToAction(_harvestYearOverview, "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear
                    });
                }
                else
                {
                    return RedirectToRecommendation(model);
                }
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrganicManureUpdate(OrganicManureViewModel model)
        {
            _logger.LogTrace("Organic Manure Controller : OrganicManureUpdate() post action called");

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(_checkAnswer, model);
                }
                IActionResult? earlyResult = await ValidateOrganicManureUpdateAsync(model);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                if (!string.IsNullOrWhiteSpace(model.EncryptedOrgManureId) && model.OrganicManures != null && model.OrganicManures.Count > 0)
                {
                    earlyResult = await ProcessOrganicManureUpdateAsync(model);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData[_updateOrganicManureError] = ex.Message;
                return RedirectToAction(_checkAnswer);
            }

            return RedirectToAction(_checkAnswer);
        }

        // ===================== Validation =====================

        private async Task<IActionResult?> ValidateOrganicManureUpdateAsync(OrganicManureViewModel model)
        {
            ValidateManureModel(model);

            Error? error = await ValidateDoubleCropSelectionAsync(model);
            if (error != null)
            {
                TempData[_checkYourAnswerError] = error.Message;
                return View(model);
            }

            ValidateGrassDefoliation(model);

            if (!ModelState.IsValid)
            {
                return RedirectToAction(_checkAnswer);
            }

            return null;
        }

        // ===================== Main update flow =====================

        private async Task<IActionResult?> ProcessOrganicManureUpdateAsync(OrganicManureViewModel model)
        {
            SetOrganicManureValues(model);

            // logic for AvailableNForNMax column that will be used to get sum of previous manure applications
            decimal? currentApplicationNitrogen = await CalculateCurrentApplicationNitrogenAsync(model);

            (FarmResponse farmData, Error? error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (farmData == null || !string.IsNullOrWhiteSpace(error?.Message))
            {
                return BuildUpdateFailureRedirect();
            }

            if (model.UpdatedOrganicIds == null || model.UpdatedOrganicIds.Count == 0)
            {
                return null;
            }

            (List<OrganicManureUpdateData> organicManureList, IActionResult? earlyResult) =
                await BuildOrganicManureUpdateListAsync(model, farmData, currentApplicationNitrogen);
            if (earlyResult != null)
            {
                return earlyResult;
            }

            if (organicManureList == null || organicManureList.Count == 0)
            {
                return null;
            }

            List<object> organicManuresPayload = await BuildOrganicManurePayloadAsync(model, organicManureList);

            return await SaveAndRedirectAsync(model, organicManuresPayload);
        }

        // Shared by every failure path that redirects to CheckAnswer with the same error message.
        private IActionResult BuildUpdateFailureRedirect()
        {
            TempData[_updateOrganicManureError] = Resource.MsgWeCouldNotUpdateOrganicManure;
            return RedirectToAction(_checkAnswer);
        }

        // ===================== Building the per-organic-manure update list =====================

        private async Task<(List<OrganicManureUpdateData> List, IActionResult? EarlyResult)> BuildOrganicManureUpdateListAsync(
            OrganicManureViewModel model, FarmResponse farmData, decimal? currentApplicationNitrogen)
        {
            List<OrganicManureUpdateData> organicManureList = new List<OrganicManureUpdateData>();

            foreach (OrganicManureDataViewModel organic in model.OrganicManures)
            {
                (OrganicManureUpdateData? organicManure, Error? orgError) = await FetchManureOutput(model, farmData, organic, currentApplicationNitrogen);
                if (orgError != null && !string.IsNullOrWhiteSpace(orgError.Message))
                {
                    return (organicManureList, BuildUpdateFailureRedirect());
                }

                organicManureList.Add(organicManure);
            }

            return (organicManureList, null);
        }



        private async Task<(int FieldTypeId, int? FieldId)> ResolveFieldTypeAndIdAsync(OrganicManureUpdateData orgManure)
        {
            int? fieldID = null;
            int fieldTypeId = (int)NMP.Commons.Enums.FieldType.Arable;

            (ManagementPeriod manData, Error? error) = await _cropLogic.FetchManagementperiodById(orgManure.ManagementPeriodID);
            if (manData != null)
            {
                (Crop crop, error) = await _cropLogic.FetchCropById(manData.CropID.Value);
                if (crop != null)
                {
                    fieldTypeId = (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                        ? (int)NMP.Commons.Enums.FieldType.Grass
                        : (int)NMP.Commons.Enums.FieldType.Arable;
                    fieldID = crop.FieldID;
                }
            }

            return (fieldTypeId, fieldID);
        }

        // ===================== Save + redirect =====================

        private async Task<IActionResult> SaveAndRedirectAsync(OrganicManureViewModel model, List<object> organicManuresPayload)
        {
            var jsonData = new { OrganicManures = organicManuresPayload };
            string jsonString = JsonConvert.SerializeObject(jsonData);

            (List<OrganicManure> organicManures, Error? error) = await _organicManureLogic.UpdateOrganicManure(jsonString);
            if (error != null || organicManures.Count == 0)
            {
                return BuildUpdateFailureRedirect();
            }

            bool success = true;
            HttpContext.Session.Remove(_organicManureSessionKey);

            return BuildUpdateSuccessRedirect(model, success);
        }

        private IActionResult BuildUpdateSuccessRedirect(OrganicManureViewModel model, bool success)
        {
            if (model.FieldList != null && model.FieldList.Count == 1)
            {
                if (model.IsComingFromRecommendation)
                {
                    string fieldId = model.FieldList[0];
                    return RedirectToAction(_recommendations, "Crop", new
                    {
                        q = model.EncryptedFarmId,
                        r = _fieldDataProtector.Protect(fieldId),
                        s = model.EncryptedHarvestYear,
                        t = _cropDataProtector.Protect(Resource.MsgOrganicMaterialApplicationUpdated),
                        u = _cropDataProtector.Protect(Resource.MsgNutrientRecommendationsMayBeUpdated)
                    });
                }
                else
                {
                    return Redirect(Url.Action(_harvestYearOverview, "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear,
                        q = _farmDataProtector.Protect(success.ToString()),
                        r = _cropDataProtector.Protect(Resource.MsgOrganicMaterialApplicationUpdated),
                        w = _fieldDataProtector.Protect(model.FieldList.FirstOrDefault())
                    }) + Resource.lblOrganicMaterialApplicationsForSorting);
                }
            }
            else if (!model.IsComingFromRecommendation)
            {
                return Redirect(Url.Action(_harvestYearOverview, "Crop", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear,
                    q = _farmDataProtector.Protect(success.ToString()),
                    r = _cropDataProtector.Protect(Resource.MsgOrganicMaterialApplicationUpdated),
                    v = _cropDataProtector.Protect(Resource.lblSelectAFieldToSeeItsUpdatedRecommendations)
                }) + Resource.lblOrganicMaterialApplicationsForSorting);
            }

            // Matches original: FieldList.Count != 1 && IsComingFromRecommendation falls through with no redirect,
            // eventually reaching the final RedirectToAction(_checkAnswer) at the bottom of the main action.
            return null;
        }

        private static void SetOrganicManureValues(OrganicManureViewModel model)
        {
            model.OrganicManures.ForEach(x => x.EndOfDrain = x.SoilDrainageEndDate);

            if (IsOtherManureType(model.ManureTypeId))
            {
                model.OrganicManures.ForEach(x => x.ManureTypeName = model.OtherMaterialName);

                if (IsOtherManureType(model.ManureGroupIdForFilter))
                {
                    model.OrganicManures.ForEach(x =>
                        x.ManureTypeID = model.ManureGroupIdForFilter ?? 0);
                }
            }
            else
            {
                model.OrganicManures.ForEach(x =>
                    x.ManureTypeName = model.ManureTypeName);
            }
        }
        private async Task<decimal?> CalculateCurrentApplicationNitrogenAsync(OrganicManureViewModel model)
        {
            (ManureType manure, _) =
                await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);

            if (manure?.PercentOfTotalNForUseInNmaxCalculation == null)
            {
                return null;
            }

            if (model.OrganicManures == null || !model.OrganicManures.Any())
            {
                return null;
            }

            if (!model.ApplicationRate.HasValue)
            {
                return null;
            }

            decimal totalNitrogen =
                model.OrganicManures.FirstOrDefault()?.N ?? 0;

            decimal percentage =
                Convert.ToDecimal(
                    manure.PercentOfTotalNForUseInNmaxCalculation / 100.0);

            return totalNitrogen *
                   model.ApplicationRate.Value *
                   percentage;
        }
        private async Task<(OrganicManureUpdateData?, Error?)> FetchManureOutput(OrganicManureViewModel model, FarmResponse farmData, OrganicManureDataViewModel organic, decimal? currentApplicationNitrogen)
        {
            Error? error = null;
            OrganicManureUpdateData? organicManure = null;

            (string? mannerJsonString, Error? mannerOutputError) = await BindManureOutput(farmData, organic, model);
            if (!string.IsNullOrWhiteSpace(mannerJsonString))
            {
                (MannerCalculateNutrientResponse mannerCalculateNutrientResponse, error) = await _organicManureLogic.FetchMannerCalculateNutrient(mannerJsonString);
                if (error == null && mannerCalculateNutrientResponse != null)
                {
                    organicManure = new OrganicManureUpdateData
                    {
                        ID = model.UpdatedOrganicIds != null ? (model.UpdatedOrganicIds.Where(x => x.ManagementPeriodId.Value == organic.ManagementPeriodID).Select(x => x.OrganicManureId.Value).FirstOrDefault()) : 0,
                        ManagementPeriodID = organic.ManagementPeriodID,
                        ManureTypeID = organic.ManureTypeID,
                        ManureTypeName = model.ManureTypeName,
                        ApplicationDate = organic.ApplicationDate.Value,
                        Confirm = organic.Confirm,
                        N = organic.N,
                        P2O5 = organic.P2O5,
                        K2O = organic.K2O,
                        MgO = organic.MgO,
                        SO3 = organic.SO3,
                        AvailableN = mannerCalculateNutrientResponse.CurrentCropAvailableN,
                        ApplicationRate = organic.ApplicationRate,
                        DryMatterPercent = organic.DryMatterPercent,
                        UricAcid = organic.UricAcid,
                        EndOfDrain = organic.SoilDrainageEndDate,
                        Rainfall = organic.Rainfall,
                        AreaSpread = organic.AreaSpread,
                        ManureQuantity = organic.ManureQuantity,
                        ApplicationMethodID = organic.ApplicationMethodID,
                        IncorporationMethodID = organic.IncorporationMethodID,
                        IncorporationDelayID = organic.IncorporationDelayID,
                        NH4N = organic.NH4N,
                        NO3N = organic.NO3N,
                        AvailableP2O5 = mannerCalculateNutrientResponse.CropAvailableP2O5,
                        AvailableK2O = mannerCalculateNutrientResponse.CropAvailableK2O,
                        AvailableSO3 = mannerCalculateNutrientResponse.CropAvailableSO3,
                        WindspeedID = organic.WindspeedID,
                        RainfallWithinSixHoursID = organic.RainfallWithinSixHoursID,
                        MoistureID = organic.MoistureID,
                        AutumnCropNitrogenUptake = organic.AutumnCropNitrogenUptake,
                        AvailableNForNMax = currentApplicationNitrogen != null ? currentApplicationNitrogen : mannerCalculateNutrientResponse.CurrentCropAvailableN,
                        AvailableNForNextYear = mannerCalculateNutrientResponse.FollowingCropYear2AvailableN,
                        AvailableNForNextDefoliation = mannerCalculateNutrientResponse.NextGrassNCropCurrentYear

                    };

                    return (organicManure, error);

                }
            }
            else
            {
                return (organicManure, mannerOutputError);
            }


            return (organicManure, error);
        }
        [HttpGet]
        public async Task<IActionResult> DoubleCrop(string q)
        {
            _logger.LogTrace("Organic Manure Controller : DoubleCrop() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            try
            {
                IActionResult? earlyResult;
                (model, earlyResult) = await HandleQueryParamAsync(model, q);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                if (model.FieldList != null && model.FieldList.Count > 0)
                {
                    (model, earlyResult) = await ProcessFieldListAsync(model);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                TempData[_manureTypeError] = ex.Message;
                return RedirectToAction(_manureTypeAction);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoubleCrop(OrganicManureViewModel model)
        {
            _logger.LogTrace("Organic Manure Controller : DoubleCrop() post action called");

            if (model.DoubleCrop[model.DoubleCropCurrentCounter].CropID == 0)
            {
                ModelState.AddModelError("DoubleCrop[" + model.DoubleCropCurrentCounter + "].CropID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return await BuildInvalidModelViewAsync(model);
                }

                OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();

                await ApplyCropOrderAndNameAsync(model);
                await ApplyManagementPeriodToOrganicManureAsync(model);
                await RemoveDefoliationIfNotGrassAsync(model);
                await AdvanceDoubleCropCounterAsync(model);

                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                if (model.IsCheckAnswer || model.DoubleCrop.Count == model.DoubleCropCurrentCounter)
                {
                    await ProcessGrassFlagsAsync(model, organicManureViewModel);
                }

                if (model.DoubleCropCurrentCounter == model.DoubleCrop.Count || (!model.IsAnyChangeInField && model.IsCheckAnswer))
                {
                    return BuildDoubleCropFinalRedirect(model);
                }
                else
                {
                    return await BuildNextDoubleCropViewAsync(model);
                }
            }
            catch (Exception ex)
            {
                TempData[_doubleCropError] = ex.Message;
                return View(model);
            }
        }

        // ===================== Query param ("q") handling =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> HandleQueryParamAsync(OrganicManureViewModel model, string q)
        {
            if (string.IsNullOrWhiteSpace(q) && model.OrganicManures != null && model.OrganicManures.Count > 0
                && (model.IsManureTypeChange || model.IsAnyChangeInField || model.IsFieldGroupChange))
            {
                model.DoubleCropCurrentCounter = 0;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            else if (!string.IsNullOrWhiteSpace(q) && model.DoubleCrop != null && model.DoubleCrop.Count > 0)
            {
                int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
                int index = itemCount - 1;

                if (itemCount == 0)
                {
                    return (model, await HandleDoubleCropResetAsync(model));
                }

                model.FieldID = model.DoubleCrop[index].FieldID;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[index].FieldID)).Name;
                model.DoubleCropCurrentCounter = index;
                model.DoubleCropEncryptedCounter = _fieldDataProtector.Protect(model.DoubleCropCurrentCounter.ToString());
            }

            return (model, null);
        }

        // Handles the itemCount == 0 branch: resets counter and decides the redirect.
        private async Task<IActionResult> HandleDoubleCropResetAsync(OrganicManureViewModel model)
        {
            model.DoubleCropCurrentCounter = 0;
            model.DoubleCropEncryptedCounter = string.Empty;
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            if (model.IsCheckAnswer && (!model.IsManureTypeChange) && (!model.IsFieldGroupChange) && (!model.IsAnyChangeInSameDefoliationFlag))
            {
                return RedirectToAction(_checkAnswer);
            }

            if (IsOtherMaterialGroup(model))
            {
                return RedirectToAction(_manureGroup);
            }
            else if (IsOtherManure(model))
            {
                return RedirectToAction(_otherMaterialName);
            }
            else
            {
                return RedirectToAction(_manureTypeAction);
            }
        }

        // ===================== FieldList processing =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessFieldListAsync(OrganicManureViewModel model)
        {
            if (model.DoubleCrop != null && model.DoubleCrop.Count > 0 && model.DoubleCropCurrentCounter < model.DoubleCrop.Count)
            {
                model.FieldID = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID;
                model.FieldName = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldName;
            }

            IActionResult? earlyResult;

            if (model.DoubleCrop == null || model.IsAnyChangeInField)
            {
                (model, earlyResult) = await BuildDoubleCropListAsync(model);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            if (model.DoubleCrop != null && model.DoubleCrop.Count > 0 &&
                model.DoubleCrop.Any(dc => !model.FieldList.Contains(dc.FieldID.ToString())))
            {
                model.DoubleCrop?.RemoveAll(dc => !model.FieldList.Contains(dc.FieldID.ToString()));
            }

            (model, earlyResult) = await FetchCropOptionsForCurrentFieldAsync(model);
            if (earlyResult != null)
            {
                return (model, earlyResult);
            }

            if (model.DoubleCropCurrentCounter == 0)
            {
                model.FieldID = model.DoubleCrop[0].FieldID;
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DoubleCrop[0].FieldID)).Name;
            }

            return (model, null);
        }

        // Builds up model.DoubleCrop entries for fields with a double crop, when DoubleCrop is null or fields changed.
        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> BuildDoubleCropListAsync(OrganicManureViewModel model)
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

                (List<Crop> cropList, Error? error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId, model.HarvestYear.Value);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_manureTypeError] = error.Message;
                    return (model, RedirectToAction(_manureTypeAction));
                }

                if (cropList != null && cropList.Count == 2)
                {
                    var cropTypeId = cropList[0]?.CropTypeID;
                    if (cropTypeId.HasValue)
                    {
                        string cropTypeName = await _fieldLogic.FetchCropTypeById(cropTypeId.Value);
                        var field = await _fieldLogic.FetchFieldByFieldId(fieldId);

                        var doubleCrop = new DoubleCrop
                        {
                            CropName = cropTypeName,
                            CropOrder = cropList[0].CropOrder ?? 1,
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

            return (model, null);
        }

        // Fetches the crop plan for the current double-crop field and populates ViewBag.DoubleCropOptions when applicable.
        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> FetchCropOptionsForCurrentFieldAsync(OrganicManureViewModel model)
        {
            (List<Crop> cropList, Error? error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(
                Convert.ToInt32(model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID), model.HarvestYear.Value);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_manureTypeError] = error.Message;
                return (model, RedirectToAction(_manureTypeAction));
            }

            if (cropList != null && cropList.Count == 2)
            {
                var cropOptions = new List<SelectListItem>();
                foreach (var crop in cropList.OrderBy(x => x.CropOrder))
                {
                    string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
                    cropOptions.Add(new SelectListItem
                    {
                        Text = $"{Resource.lblCrop} {crop.CropOrder}: {cropTypeName}",
                        Value = crop.ID.ToString()
                    });
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                ViewBag.DoubleCropOptions = cropOptions;
            }

            return (model, null);
        }



        // ===================== Invalid model view =====================

        private async Task<IActionResult> BuildInvalidModelViewAsync(OrganicManureViewModel model)
        {
            if (model.FieldList != null && model.FieldList.Count > 0)
            {
                (List<Crop> cropList, Error? error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(
                    Convert.ToInt32(model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID), model.HarvestYear.Value);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_doubleCropError] = error.Message;
                }

                if (model.DoubleCrop == null)
                {
                    model.DoubleCrop = new List<DoubleCrop>();
                }

                if (cropList != null && cropList.Count == 2)
                {
                    var cropOptions = await BuildCropOptionsAsync(cropList);
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                    ViewBag.DoubleCropOptions = cropOptions;
                }
            }

            return View(model);
        }

        // Shared crop-option builder used by both the invalid-model view and the "not yet done" view below.
        private async Task<List<SelectListItem>> BuildCropOptionsAsync(List<Crop> cropList)
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

            return cropOptions;
        }

        // ===================== Update crop order/name on the matching double-crop entry =====================

        private async Task ApplyCropOrderAndNameAsync(OrganicManureViewModel model)
        {
            if (!model.DoubleCrop.Any(x => x.FieldID == model.FieldID))
            {
                return;
            }

            List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(model.FieldID.Value);
            cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();

            if (cropList.Count != 2)
            {
                return;
            }

            cropList = cropList.Where(x => x.ID == model.DoubleCrop[model.DoubleCropCurrentCounter].CropID).ToList();
            if (cropList.Count > 0)
            {
                model.DoubleCrop[model.DoubleCropCurrentCounter].CropOrder = cropList.Select(x => x.CropOrder.Value).First();
                model.DoubleCrop[model.DoubleCropCurrentCounter].CropName = await _fieldLogic.FetchCropTypeById(Convert.ToInt32(cropList.Select(x => x.CropTypeID.Value).First()));
            }
        }

        // ===================== Update the organic manure's management period for the matching field =====================

        private async Task ApplyManagementPeriodToOrganicManureAsync(OrganicManureViewModel model)
        {
            if (model.DoubleCrop.Count == 0)
            {
                return;
            }

            var currentDoubleCrop = model.DoubleCrop[model.DoubleCropCurrentCounter];
            (List<ManagementPeriod> managementPeriods, Error? error) = await _cropLogic.FetchManagementperiodByCropId(currentDoubleCrop.CropID, true);

            if (!IsValidManagementPeriods(managementPeriods, error))
            {
                return;
            }

            var organicManure = model.OrganicManures.FirstOrDefault(x => x.FieldID == currentDoubleCrop.FieldID);
            if (organicManure == null)
            {
                return;
            }

            int managementPeriodId = managementPeriods.Select(x => x.ID.Value).First();

            if (ShouldUpdateEncryptedOrganicIds(model))
            {
                UpdateMatchingOrganicId(model, organicManure, managementPeriodId);
            }

            organicManure.ManagementPeriodID = managementPeriodId;
        }

        private static bool IsValidManagementPeriods(List<ManagementPeriod> managementPeriods, Error? error)
        {
            return string.IsNullOrWhiteSpace(error?.Message) && managementPeriods != null && managementPeriods.Count > 0;
        }

        private static bool ShouldUpdateEncryptedOrganicIds(OrganicManureViewModel model)
        {
            return model.IsCheckAnswer && !string.IsNullOrWhiteSpace(model.EncryptedOrgManureId) && model.UpdatedOrganicIds != null;
        }

        private static void UpdateMatchingOrganicId(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, int managementPeriodId)
        {
            var matchingUpdatedId = model.UpdatedOrganicIds.FirstOrDefault(x => organicManure.FieldName.Equals(x.Name));
            if (matchingUpdatedId != null)
            {
                matchingUpdatedId.ManagementPeriodId = managementPeriodId;
            }
        }

        // ===================== Remove stale defoliation entries when the crop is no longer grass =====================

        private async Task RemoveDefoliationIfNotGrassAsync(OrganicManureViewModel model)
        {
            (Crop cropData, Error? error) = await _cropLogic.FetchCropById(model.DoubleCrop[model.DoubleCropCurrentCounter].CropID);

            if (string.IsNullOrWhiteSpace(error?.Message) && cropData != null && cropData.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass &&
                model.DefoliationList != null && model.DefoliationList.Any(x => x.FieldID == model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID))
            {
                int fieldIdToRemove = model.DoubleCrop[model.DoubleCropCurrentCounter].FieldID;
                model.DefoliationList.RemoveAll(x => x.FieldID == fieldIdToRemove);
            }
        }

        // ===================== Advance the counter to the next double-crop field =====================

        private async Task AdvanceDoubleCropCounterAsync(OrganicManureViewModel model)
        {
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
        }

        // ===================== Grass-flag processing (only runs when IsCheckAnswer or all double crops processed) =====================

        private async Task ProcessGrassFlagsAsync(OrganicManureViewModel model, OrganicManureViewModel? organicManureViewModel)
        {
            int counter = await ApplyGrassFlagsToOrganicManuresAsync(model);

            if (model.OrganicManures != null && !model.OrganicManures.Any(x => x.IsGrass))
            {
                model.IsAnyCropIsGrass = false;
            }

            model.GrassCropCount = model.OrganicManures != null ? model.OrganicManures.Count(x => x.IsGrass) : counter;

            if (model.IsCheckAnswer && organicManureViewModel != null && organicManureViewModel.DoubleCrop != null && model.DoubleCrop != null)
            {
                ApplyDoubleCropChangeFlag(model, organicManureViewModel);
            }
        }

        private async Task<int> ApplyGrassFlagsToOrganicManuresAsync(OrganicManureViewModel model)
        {
            int counter = 0;

            foreach (var cropId in model.DoubleCrop.Select(doubleCrop => doubleCrop.CropID).Where(cropId => cropId > 0))
            {
                (Crop crop, Error? error) = await _cropLogic.FetchCropById(cropId);

                if (string.IsNullOrWhiteSpace(error?.Message) &&
                    crop != null &&
                    model.OrganicManures != null &&
                    model.OrganicManures.Count > 0)
                {
                    int index = model.OrganicManures.FindIndex(f => f.FieldID == crop.FieldID);

                    if (crop.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && index >= 0)
                    {
                        model.OrganicManures[index].IsGrass = true;
                        counter++;
                        model.IsAnyCropIsGrass = true;
                    }
                    else if (model.OrganicManures.Any(f => f.IsGrass && f.FieldID == crop.FieldID))
                    {
                        model.OrganicManures[index].IsGrass = false;
                        model.OrganicManures[index].Defoliation = null;
                        model.OrganicManures[index].DefoliationName = null;
                    }
                }
            }

            return counter;
        }

        private static void ApplyDoubleCropChangeFlag(OrganicManureViewModel model, OrganicManureViewModel organicManureViewModel)
        {
            int grassCount = model.OrganicManures.Count(x => x.IsGrass);

            if (model.DoubleCropCurrentCounter - 1 < model.DoubleCrop.Count && model.DefoliationList != null && grassCount != model.DefoliationList.Count)
            {
                model.FieldID = model.DoubleCrop[model.DoubleCropCurrentCounter - 1].FieldID;
                model.FieldName = model.DoubleCrop[model.DoubleCropCurrentCounter - 1].FieldName;
            }

            var newItem = model.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID.Value);
            var oldItem = organicManureViewModel.DoubleCrop.FirstOrDefault(x => x.FieldID == model.FieldID.Value);
            if (newItem != null && newItem.CropOrder != oldItem?.CropOrder)
            {
                model.IsDoubleCropValueChange = true;
            }
        }

        // ===================== Final redirect decision (all double crops processed) =====================

        // Used by the DoubleCrop POST action (the one you pasted)
        private IActionResult BuildDoubleCropFinalRedirect(OrganicManureViewModel model)
        {
            if (IsCheckAnswerWithoutGrassOrChanges(model))
            {
                return SaveSessionAndRedirect(model, _checkAnswer);
            }

            if (IsCheckAnswerWithAllGrassFieldsDefoliated(model))
            {
                model.IsAnyChangeInSameDefoliationFlag = false;
                return SaveSessionAndRedirect(model, _checkAnswer);
            }

            if (!IsAnyCropGrass(model))
            {
                return SaveSessionAndRedirect(model, _manureApplyingDateAction);
            }

            if (model.GrassCropCount != null && model.GrassCropCount.Value > 1)
            {
                if (model.OrganicManures.Any(z => z.IsGrass && z.Defoliation == null))
                {
                    model.IsSameDefoliationForAll = null;
                }
                return SaveSessionAndRedirect(model, _isSameDefoliationForAll);
            }

            model.IsSameDefoliationForAll = true;
            return SaveSessionAndRedirect(model, _defoliationAction);
        }

        private static bool IsAnyCropGrass(OrganicManureViewModel model)
        {
            return model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value;
        }

        private static bool IsCheckAnswerWithoutGrassOrChanges(OrganicManureViewModel model)
        {
            return model.IsCheckAnswer
                && model.IsAnyCropIsGrass.HasValue
                && !model.IsAnyCropIsGrass.Value
                && !model.IsAnyChangeInField
                && !model.IsManureTypeChange;
        }

        private static bool IsCheckAnswerWithAllGrassFieldsDefoliated(OrganicManureViewModel model)
        {
            return model.IsCheckAnswer
                && !model.IsManureTypeChange
                && !model.IsAnyChangeInField
                && AllGrassFieldsHaveDefoliation(model);
        }

        private static bool AllGrassFieldsHaveDefoliation(OrganicManureViewModel model)
        {
            if (model.DefoliationList == null)
            {
                return false;
            }

            var defoliatedFieldIds = model.DefoliationList.Select(d => d.FieldID).ToList();

            return model.OrganicManures
                .Where(x => x.IsGrass)
                .Select(x => x.FieldID)
                .All(fieldId => defoliatedFieldIds.Contains(fieldId.Value));
        }

        private IActionResult SaveSessionAndRedirect(OrganicManureViewModel model, string actionName)
        {
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(actionName);
        }

        // ===================== "Not yet done" view (more double crops to process) =====================

        private async Task<IActionResult> BuildNextDoubleCropViewAsync(OrganicManureViewModel model)
        {
            List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(model.FieldID.Value);
            cropList = cropList.Where(x => x.Year == model.HarvestYear).ToList();

            if (cropList.Count == 2)
            {
                var cropOptions = await BuildCropOptionsAsync(cropList);
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                ViewBag.DoubleCropOptions = cropOptions;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ManureType()
        {
            _logger.LogTrace($"Organic Manure Controller : ManureType() action called");
            Error? error = null;
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }
            try
            {
                List<ManureType> manureTypeList = new List<ManureType>();
                if (model.FarmRB209CountryID.HasValue && model.ManureGroupIdForFilter.HasValue)
                {
                    (manureTypeList, error) = await FetchManureTypeList(model.ManureGroupIdForFilter.Value, model.FarmRB209CountryID.Value);
                }
                if (error == null)
                {
                    if (manureTypeList.Count > 0)
                    {
                        var manures = manureTypeList.OrderBy(m => m.SortOrder).ToList();
                        var SelectListItem = ToSelectList(manures, f => f.Id.ToString(), f => f.Name);
                        ViewBag.ManureTypeList = SelectListItem.ToList();
                    }
                    return View(model);
                }
                else
                {
                    TempData[_manureGroupError] = error.Message;
                    return RedirectToAction(_manureGroup, model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureType() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_manureGroupError] = ex.Message;
                return RedirectToAction(_manureGroup, model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureType(OrganicManureViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : ManureType() post action called");

            if (model.ManureTypeId == null)
            {
                ModelState.AddModelError("ManureTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                OrganicManureViewModel? orgManureViewModel = GetOrganicManureFromSession();
                if (orgManureViewModel == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }

                (List<ManureType> manureTypeList, Error? error) = await GetManureTypeList(model);

                if (error == null)
                {
                    IActionResult? earlyResult;
                    (model, earlyResult) = await ProcessManureTypeSelectionAsync(model, orgManureViewModel, manureTypeList);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

                return BuildManureTypeFinalRedirect(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Organic Manure Controller : Exception in ManureType() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_manureTypeError] = ex.Message;
                return View(model);
            }
        }

        // ===================== Manure type selection processing (only when the type-list fetch succeeded) =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessManureTypeSelectionAsync(
            OrganicManureViewModel model, OrganicManureViewModel orgManureViewModel, List<ManureType> manureTypeList)
        {
            if (manureTypeList.Count > 0)
            {
                PopulateManureTypeViewBag(manureTypeList);
            }

            if (!ModelState.IsValid)
            {
                return (model, View(model));
            }

            ApplySelectedManureTypeToOrganicManures(model, manureTypeList);
            ResetNutrientValuesIfManureTypeChanged(model, orgManureViewModel);

            if (model.ManureGroupIdForFilter.HasValue)
            {
                model.ManureGroupId = model.ManureGroupIdForFilter;
            }

            // if manure type change
            if (model.IsCheckAnswer && orgManureViewModel?.ManureTypeId != model.ManureTypeId)
            {
                IActionResult? earlyResult;
                (model, earlyResult) = await HandleManureTypeChangeAsync(model, orgManureViewModel);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            return (model, null);
        }

        private void PopulateManureTypeViewBag(List<ManureType> manureTypeList)
        {
            var manures = manureTypeList.OrderBy(m => m.SortOrder).ToList();
            var selectListItem = ToSelectList(manures, f => f.Id.ToString(), f => f.Name);
            ViewBag.ManureTypeList = selectListItem.OrderBy(x => x.Text).ToList();
        }

        private static void ApplySelectedManureTypeToOrganicManures(OrganicManureViewModel model, List<ManureType> manureTypeList)
        {
            ManureType manureType = manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId);
            if (manureType == null)
            {
                return;
            }

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

        private static void ResetNutrientValuesIfManureTypeChanged(OrganicManureViewModel model, OrganicManureViewModel orgManureViewModel)
        {
            if (orgManureViewModel != null && orgManureViewModel.ManureTypeId != model.ManureTypeId)
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
                model.IsDefaultValueChange = true;
            }
        }

        // ===================== "Manure type changed" handling =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> HandleManureTypeChangeAsync(
            OrganicManureViewModel model, OrganicManureViewModel orgManureViewModel)
        {
            model.IsManureTypeChange = true;

            if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
            {
                model.ApplicationRate = null;
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.ApplicationRate = null;
                }
            }

            // if manure type is changed liquid to solid or solid to liquid then ApplicationMethod, IncorporationMethod, IncorporationDelay need to be set null
            if (orgManureViewModel?.IsManureTypeLiquid != model.IsManureTypeLiquid)
            {
                ResetApplicationAndIncorporationFields(model);
            }

            // if manure type is changed then we need to bind default values
            (ManureType manureTypeData, Error? error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (error != null)
            {
                TempData[_manureTypeError] = error.Message;
                return (model, View(model));
            }

            ApplyManureTypeDataToModel(model, manureTypeData);

            // if manure type is solid then need to set application method value.
            if (!model.IsManureTypeLiquid.Value)
            {
                IActionResult? earlyResult = await ApplyDefaultApplicationMethodAsync(model);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            return (model, null);
        }

        private static void ResetApplicationAndIncorporationFields(OrganicManureViewModel model)
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

        private static void ApplyManureTypeDataToModel(OrganicManureViewModel model, ManureType manureTypeData)
        {
            model.ManureType = manureTypeData;

            if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
            {
                BindNutrientsFromManureType(model);
            }

            model.DryMatterPercent = manureTypeData.DryMatter;
            model.N = manureTypeData.TotalN;
            model.NH4N = manureTypeData.NH4N;
            model.NO3N = manureTypeData.NO3N;
            model.K2O = manureTypeData.K2O;
            model.SO3 = manureTypeData.SO3;
            model.MgO = manureTypeData.MgO;
            model.P2O5 = manureTypeData.P2O5;
            model.UricAcid = manureTypeData.Uric;

            UpdateOrganicManuresFromModel(model, manureTypeData);
        }

        private async Task<IActionResult?> ApplyDefaultApplicationMethodAsync(OrganicManureViewModel model)
        {
            List<Crop> cropsResponse = await _cropLogic.FetchCropsByFieldId(Convert.ToInt32(model.FieldList[0]));
            var fieldType = cropsResponse.Where(x => x.Year == model.HarvestYear).Select(x => x.FieldType).FirstOrDefault();

            (List<ApplicationMethodResponse> applicationMethodList, Error? error) = await _mannerLogic.FetchApplicationMethodList(fieldType ?? 0, model.IsManureTypeLiquid.Value);

            if (error != null)
            {
                TempData[_manureTypeError] = error.Message;
                return View(model);
            }

            if (applicationMethodList.Count > 0)
            {
                model.ApplicationMethod = applicationMethodList[0].ID;
                foreach (var orgManure in model.OrganicManures)
                {
                    orgManure.ApplicationMethodID = model.ApplicationMethod.Value;
                }

                (model.ApplicationMethodName, error) = await _mannerLogic.FetchApplicationMethodById(model.ApplicationMethod.Value);
                if (error != null)
                {
                    TempData[_manureTypeError] = error.Message;
                    return View(model);
                }
            }

            return null;
        }

        // ===================== Final redirect decision =====================

        // Used by the ManureType POST action
        private IActionResult BuildManureTypeFinalRedirect(OrganicManureViewModel model)
        {
            if (IsOtherManure(model))
            {
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_otherMaterialName);
            }

            if (model.IsCheckAnswer && !model.IsAnyChangeInField && !model.IsManureTypeChange)
            {
                if (model.IsAnyCropIsGrass.HasValue && !model.IsAnyCropIsGrass.Value)
                {
                    model.GrassCropCount = null;
                    model.IsSameDefoliationForAll = null;
                    model.IsAnyChangeInSameDefoliationFlag = false;
                    HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                }

                return RedirectToAction(_checkAnswer);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            if (model.IsDoubleCropAvailable)
            {
                return RedirectToAction(_doubleCropAction);
            }
            else
            {
                model.DoubleCrop = null;
            }

            if (model.IsAnyCropIsGrass == true)
            {
                return HandleGrassCrop(model);
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            return RedirectToAction(_manureApplyingDateAction);
        }

        private IActionResult HandleGrassCrop(OrganicManureViewModel model)
        {
            var grassCrop = model.OrganicManures.First(x => x.IsGrass);

            model.FieldID = grassCrop.FieldID;
            model.FieldName = grassCrop.FieldName;

            if (model.GrassCropCount > 1)
            {
                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return RedirectToAction(_isSameDefoliationForAll);
            }

            model.IsSameDefoliationForAll = true;

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            return RedirectToAction(_defoliationAction);
        }

        [HttpGet]
        public async Task<IActionResult> IsSameDefoliationForAll()
        {
            _logger.LogTrace($"OrganicManure Controller : IsSameDefoliationForAll() action called");

            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (model.IsAnyChangeInSameDefoliationFlag)
            {
                model.IsAnyChangeInSameDefoliationFlag = false;
            }

            try
            {
                List<List<SelectListItem>> allDefoliations = await BuildAllDefoliationsAsync(model);

                if (allDefoliations.Count > 0)
                {
                    IActionResult? earlyResult;
                    (model, earlyResult) = await ProcessCommonDefoliationsAsync(model, allDefoliations);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
            }
            catch (Exception ex)
            {
                return HandleIsSameDefoliationException(model, ex);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IsSameDefoliationForAll(OrganicManureViewModel model)
        {
            _logger.LogTrace($"OrganicManure Controller : IsSameDefoliationForAll() post action called");
            AddErrorIfNull(model.IsSameDefoliationForAll, _isSameDefoliationForAll, Resource.MsgSelectAnOptionBeforeContinuing);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.DefoliationCurrentCounter = 0;
                model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
                if (HttpContext.Session.Keys.Contains(_organicManureSessionKey))
                {
                    OrganicManureViewModel? organicManureViewModel = GetOrganicManureFromSession();

                    if (model.IsSameDefoliationForAll != organicManureViewModel.IsSameDefoliationForAll)
                    {
                        model.IsAnyChangeInSameDefoliationFlag = true;
                    }
                    else
                    {
                        model.IsAnyChangeInSameDefoliationFlag = false;
                    }
                }
                else
                {
                    return RedirectToAction(_farmList, "Farm");
                }
                if (model.IsAnyChangeInSameDefoliationFlag)
                {
                    foreach (var organic in model.OrganicManures)
                    {
                        organic.Defoliation = null;
                        organic.DefoliationName = null;
                    }
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                if (!model.IsAnyChangeInSameDefoliationFlag && model.IsCheckAnswer && (!model.IsAnyChangeInField) && (!model.IsManureTypeChange))
                {
                    return RedirectToAction(_checkAnswer);
                }
            }
            catch (Exception ex)
            {
                TempData["IsSameDefoliationForAllError"] = ex.Message;
                return View(model);
            }
            return RedirectToAction(_defoliationAction);
        }

        // ===================== Exception handling =====================

        private IActionResult HandleIsSameDefoliationException(OrganicManureViewModel model, Exception ex)
        {
            if (model.IsDoubleCropAvailable)
            {
                TempData[_doubleCropError] = ex.Message;
                return RedirectToAction(_doubleCropAction, new { q = model.EncryptedCounter });
            }
            else
            {
                TempData[_manureTypeError] = ex.Message;
                return RedirectToAction(_manureTypeAction);
            }
        }

        // ===================== Gathering the defoliation-name lists for each grass organic manure =====================

        private async Task<List<List<SelectListItem>>> BuildAllDefoliationsAsync(OrganicManureViewModel model)
        {
            List<List<SelectListItem>> allDefoliations = new List<List<SelectListItem>>();

            foreach (var organic in model.OrganicManures.Where(x => x.IsGrass))
            {
                List<SelectListItem>? defoliationWithName = await BuildDefoliationForOrganicAsync(model, organic);
                if (defoliationWithName != null)
                {
                    allDefoliations.Add(defoliationWithName);
                }
            }

            return allDefoliations;
        }

        private async Task<List<SelectListItem>?> BuildDefoliationForOrganicAsync(OrganicManureViewModel model, OrganicManureDataViewModel organic)
        {
            (List<Crop> cropList, Error? error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(Convert.ToInt32(organic.FieldID), model.HarvestYear.Value);

            if (cropList.Count == 0 || !cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null))
            {
                return null;
            }

            var cropId = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.ID.Value).FirstOrDefault();
            int? defoliationSequenceID = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.DefoliationSequenceID).FirstOrDefault();

            (List<ManagementPeriod> managementPeriod, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);
            if (managementPeriod == null)
            {
                return null;
            }

            List<int> defoliationList = managementPeriod.Select(x => x.Defoliation.Value).ToList();
            (Crop crop, error) = await _cropLogic.FetchCropById(cropId);

            if (string.IsNullOrWhiteSpace(error?.Message) && defoliationSequenceID != null)
            {
                (DefoliationSequenceResponse defoliationSequence, error) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
                if (error == null && defoliationSequence != null)
                {
                    return CommonHelpers.BindAllDefoliationWithName(defoliationList, defoliationSequence);
                }
            }

            return null;
        }

        // ===================== Deciding whether all grass crops share a common defoliation =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> ProcessCommonDefoliationsAsync(
            OrganicManureViewModel model, List<List<SelectListItem>> allDefoliations)
        {
            List<List<string>> defoliationSequenceList = allDefoliations
                .Select(list => list.Select(item => item.Text).ToList())
                .ToList();

            if (defoliationSequenceList.Count == 0)
            {
                return (model, null);
            }

            List<string> commonDefoliations = defoliationSequenceList.Aggregate((prev, next) => prev.Intersect(next).ToList());

            if (commonDefoliations.Count > 0)
            {
                List<SelectListItem> flattenedList = allDefoliations.SelectMany(list => list).ToList();
                if (flattenedList.Count > 0)
                {
                    model.NeedToShowSameDefoliationForAll = true;
                }

                return (model, null);
            }

            model = await ApplyNoCommonDefoliationFieldAsync(model);
            model.IsSameDefoliationForAll = false;
            model.NeedToShowSameDefoliationForAll = false;
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            return (model, RedirectToAction(_defoliationAction));
        }

        // Sets the current field/counter to the first grass field missing a defoliation entry, when applicable.
        private async Task<OrganicManureViewModel> ApplyNoCommonDefoliationFieldAsync(OrganicManureViewModel model)
        {
            bool shouldUpdateField = model.IsCheckAnswer && model.IsDoubleCropValueChange &&
                model.DefoliationList != null && model.OrganicManures
                    .Where(x => x.IsGrass).Select(x => x.FieldID).Any(fieldId => !model.DefoliationList.Select(d => d.FieldID).Contains(fieldId.Value));

            if (!shouldUpdateField)
            {
                return model;
            }

            var defoIds = model.DefoliationList.Select(d => d.FieldID).ToList();

            model.FieldID = model.OrganicManures
                .Where(x => x.IsGrass)
                .Select(x => x.FieldID)
                .FirstOrDefault(fid => !defoIds.Contains(fid.Value));
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;

            model.DefoliationCurrentCounter = model.DefoliationList.Count;
            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());

            return model;
        }



        [HttpGet]
        public async Task<IActionResult> Defoliation(string q)
        {
            _logger.LogTrace("OrganicManure Controller : Defoliation({Q}) action called", q);
            OrganicManureViewModel? model = GetOrganicManureFromSession();

            try
            {
                if (model == null)
                {
                    return RedirectToAction(_farmList, "Farm");
                }

                IActionResult? earlyResult;
                (model, earlyResult) = await HandleDefoliationQueryParamAsync(model, q);
                if (earlyResult != null)
                {
                    return earlyResult;
                }

                if (model.OrganicManures != null && model.OrganicManures.Count > 0)
                {
                    (model, earlyResult) = await BuildDefoliationListAsync(model);
                    if (earlyResult != null)
                    {
                        return earlyResult;
                    }
                }

                (List<SelectListItem> defoliationsList, Error? error) = await GetDefoliationList(model);
                if (error == null && defoliationsList.Count > 0)
                {
                    ViewBag.DefoliationList = ToSelectList(defoliationsList, f => f.Value, f => f.Text);
                }

                HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "OrganicManure Controller : Exception in Defoliation() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                return RedirectBasedOnCondition(model, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Defoliation(OrganicManureViewModel model)
        {
            _logger.LogTrace($"OrganicManure Controller : Defoliation() post action called");

            try
            {
                ValidateDefoliationSelection(model);

                if (!ModelState.IsValid)
                {
                    return await ReturnDefoliationView(model);
                }

                bool isDifferentDefoliationForFields = !model.NeedToShowSameDefoliationForAll ||
                    (model.IsSameDefoliationForAll.HasValue && !model.IsSameDefoliationForAll.Value);

                if (isDifferentDefoliationForFields)
                {
                    IActionResult? redirectResult = await ProcessDifferentDefoliationForFields(model);
                    if (redirectResult != null)
                    {
                        return redirectResult;
                    }
                }
                else if (model.IsSameDefoliationForAll.HasValue && model.IsSameDefoliationForAll.Value)
                {
                    return await ProcessSameDefoliationForAllFields(model);
                }

                return await FinalizeDefoliationStep(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "OrganicManure Controller : Exception in Defoliation() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["DefoliationError"] = ex.Message;
                return View(model);
            }
        }

        // ===================== Query param ("q") handling =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> HandleDefoliationQueryParamAsync(OrganicManureViewModel model, string q)
        {
            bool shouldReset = string.IsNullOrWhiteSpace(q) && model != null &&
                (model.DefoliationList == null ||
                 (model.DefoliationList != null && model.DefoliationList.Count == 0) ||
                 (model.IsAnyChangeInSameDefoliationFlag && model.DefoliationCurrentCounter == 0) ||
                 (model.IsManureTypeChange || model.IsAnyChangeInField || model.IsFieldGroupChange));

            if (shouldReset)
            {
                await ResetDefoliationCounterAsync(model);
                return (model, null);
            }

            if (model != null && !string.IsNullOrWhiteSpace(q) && model.OrganicManures != null && model.OrganicManures.Count > 0)
            {
                return await HandleDefoliationIndexAsync(model, q);
            }

            return (model, null);
        }

        private async Task ResetDefoliationCounterAsync(OrganicManureViewModel model)
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
                model.FieldID = model.OrganicManures.Where(x => x.IsGrass && x.FieldID.HasValue).Select(x => x.FieldID.Value).First();
                model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
            }

            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> HandleDefoliationIndexAsync(OrganicManureViewModel model, string q)
        {
            int itemCount = Convert.ToInt32(_fieldDataProtector.Unprotect(q));
            int index = itemCount - 1;

            if (itemCount == 0)
            {
                return (model, HandleDefoliationZeroIndex(model));
            }

            if (model.IsCheckAnswer && model.IsDoubleCropAvailable && model.IsDoubleCropValueChange && !model.NeedToShowSameDefoliationForAll)
            {
                return (model, RedirectToAction(_doubleCropAction, new { q = model.DoubleCropEncryptedCounter }));
            }

            model.FieldID = model.DefoliationList[index].FieldID;
            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.DefoliationList[index].FieldID)).Name;
            model.DefoliationCurrentCounter = index;
            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            return (model, null);
        }

        private IActionResult HandleDefoliationZeroIndex(OrganicManureViewModel model)
        {
            model.DefoliationCurrentCounter = 0;
            model.DefoliationEncryptedCounter = string.Empty;
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            if (model.GrassCropCount != null && model.GrassCropCount.Value > 1 && model.NeedToShowSameDefoliationForAll)
            {
                return RedirectToAction(_isSameDefoliationForAll);
            }
            if (model.IsDoubleCropAvailable || model.IsDoubleCropValueChange)
            {
                return RedirectToAction(_doubleCropAction, new { q = model.DoubleCropEncryptedCounter });
            }
            if (IsOtherMaterialGroup(model))
            {
                return RedirectToAction(_manureGroup);
            }
            if (IsOtherManure(model))
            {
                return RedirectToAction(_otherMaterialName);
            }

            return RedirectToAction(_manureTypeAction);
        }

        // ===================== Building the defoliation list for grass fields =====================

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> BuildDefoliationListAsync(OrganicManureViewModel model)
        {
            if (model.DefoliationList != null && model.DefoliationList.Count > 0 && model.DefoliationCurrentCounter < model.DefoliationList.Count)
            {
                model.FieldID = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;
                model.FieldName = model.DefoliationList[model.DefoliationCurrentCounter].FieldName;
            }

            bool needsRebuild = model.DefoliationList == null || model.IsAnyChangeInField ||
                (model.DefoliationList != null && model.OrganicManures
                    .Where(x => x.IsGrass)
                    .Select(x => x.FieldID)
                    .Any(fieldId => !model.DefoliationList.Select(d => d.FieldID).Contains(fieldId.Value)));

            if (needsRebuild)
            {
                IActionResult? earlyResult;
                (model, earlyResult) = await PopulateMissingDefoliationEntriesAsync(model);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }
            }

            return (model, null);
        }

        private async Task<(OrganicManureViewModel Model, IActionResult? EarlyResult)> PopulateMissingDefoliationEntriesAsync(OrganicManureViewModel model)
        {
            if (model.DefoliationList == null)
            {
                model.DefoliationList = new List<DefoliationList>();
            }

            int counter = model.DefoliationList.Count + 1;

            foreach (int fieldId in model.OrganicManures.Where(x => x.IsGrass && x.FieldID.HasValue).Select(x => x.FieldID.Value))
            {
                bool isFieldAlreadyPresent = model.DefoliationList.Any(dc => dc.FieldID == fieldId);
                if (isFieldAlreadyPresent)
                {
                    continue;
                }

                (DefoliationList? newEntry, IActionResult? earlyResult) = await BuildDefoliationEntryAsync(model, fieldId, counter);
                if (earlyResult != null)
                {
                    return (model, earlyResult);
                }

                if (newEntry != null)
                {
                    model.DefoliationList.Add(newEntry);
                    counter++;
                }
            }

            return (model, null);
        }

        private async Task<(DefoliationList? Entry, IActionResult? EarlyResult)> BuildDefoliationEntryAsync(OrganicManureViewModel model, int fieldId, int counter)
        {
            (List<Crop> cropList, Error? error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId, model.HarvestYear.Value);
            if (HasError(error))
            {
                return (null, RedirectBasedOnCondition(model, error.Message));
            }

            if (cropList.Count == 0)
            {
                return (null, null);
            }

            int cropId = cropList.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).Select(x => x.ID.Value).First();

            (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(cropId, false);
            if (HasError(error))
            {
                return (null, RedirectBasedOnCondition(model, error.Message));
            }

            if (managementPeriodList.Count == 0)
            {
                return (null, null);
            }

            var firstManagement = managementPeriodList.FirstOrDefault();
            if (firstManagement?.ID == null)
            {
                _logger.LogError("Organic Manure Controller : Management period not found in Defoliation() action");
                return (null, Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            var field = await _fieldLogic.FetchFieldByFieldId(fieldId);

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
                FieldID = fieldId,
                FieldName = field?.Name,
                EncryptedCounter = _fieldDataProtector.Protect(counter.ToString()),
                Counter = counter,
            };

            return (defoliationList, null);
        }


        private void ValidateDefoliationSelection(OrganicManureViewModel model)
        {
            if (model.DefoliationList[model.DefoliationCurrentCounter].Defoliation == null)
            {
                ModelState.AddModelError(
                    "DefoliationList[" + model.DefoliationCurrentCounter + "].Defoliation",
                    Resource.MsgSelectAnOptionBeforeContinuing);
            }
        }

        private async Task<IActionResult> ReturnDefoliationView(OrganicManureViewModel model)
        {
            (List<SelectListItem> defoliationList, Error? error) = await GetDefoliationList(model);
            if (error == null && defoliationList.Count > 0)
            {
                ViewBag.DefoliationList = ToSelectList(defoliationList, f => f.Value, f => f.Text);
            }
            else
            {
                TempData["DefoliationError"] = error?.Message;
            }

            return View(model);
        }

        private async Task<IActionResult?> FinalizeDefoliationStep(OrganicManureViewModel model)
        {
            if (model.DefoliationCurrentCounter == model.DefoliationList.Count)
            {
                if (model.IsCheckAnswer && !model.IsAnyChangeInField && !model.IsManureTypeChange)
                {
                    return RedirectToAction(_checkAnswer);
                }

                return RedirectToAction(_manureApplyingDateAction);
            }

            return await ReturnDefoliationView(model);
        }

        // ---------- "Different defoliation per field" branch ----------

        private async Task<IActionResult?> ProcessDifferentDefoliationForFields(OrganicManureViewModel model)
        {
            for (int i = 0; i < model.DefoliationList.Count; i++)
            {
                if (model.FieldID != model.DefoliationList[i].FieldID)
                {
                    continue;
                }

                (Crop crop, Error? error) = await _cropLogic.FetchCropById(model.DefoliationList[i].CropID);
                if (error == null && crop != null && crop.DefoliationSequenceID != null)
                {
                    await ProcessFieldDefoliation(model, crop, i);
                }

                model.DefoliationCurrentCounter++;

                if (i + 1 < model.DefoliationList.Count)
                {
                    model.FieldID = model.DefoliationList[i + 1].FieldID;
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID.Value)).Name;
                }

                break;
            }

            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            if (model.IsCheckAnswer && !model.IsAnyChangeInSameDefoliationFlag && !model.IsAnyChangeInField && !model.IsManureTypeChange)
            {
                return RedirectToAction(_checkAnswer);
            }

            return null;
        }

        private async Task ProcessFieldDefoliation(OrganicManureViewModel model, Crop crop, int i)
        {
            if (crop.DefoliationSequenceID != null && model.DefoliationList[i].Defoliation != null)
            {
                (string selectedDefoliation, Error? nameError) = await GetDefoliationName(
                    model.DefoliationList[i].Defoliation.Value, crop.DefoliationSequenceID.Value);

                if (nameError == null && !string.IsNullOrWhiteSpace(selectedDefoliation))
                {
                    model.DefoliationList[i].DefoliationName = selectedDefoliation;

                    if (model.OrganicManures != null && model.OrganicManures.Count > 0)
                    {
                        int index = model.OrganicManures.FindIndex(f => f.IsGrass && f.FieldID == crop.FieldID);
                        if (index >= 0)
                        {
                            // Note: original code intentionally used DefoliationCurrentCounter here, not i
                            model.OrganicManures[index].Defoliation = model.DefoliationList[model.DefoliationCurrentCounter].Defoliation;
                            model.OrganicManures[index].DefoliationName = selectedDefoliation;
                        }
                    }
                }
            }

            (List<ManagementPeriod> managementPeriodList, _) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
            if (managementPeriodList != null)
            {
                UpdateManagementPeriodForCheckAnswer(model, managementPeriodList, model.DefoliationList[i].Defoliation);
                UpdateOrganicManureManagementPeriod(model, managementPeriodList, crop, model.DefoliationList[i].Defoliation);
            }
        }

        // ---------- "Same defoliation for all fields" branch ----------

        private async Task<IActionResult> ProcessSameDefoliationForAllFields(OrganicManureViewModel model)
        {
            model.DefoliationCurrentCounter = 1;

            for (int i = 0; i < model.DefoliationList.Count; i++)
            {
                await ProcessSameDefoliationForField(model, i);
            }

            model.DefoliationEncryptedCounter = _fieldDataProtector.Protect(model.DefoliationCurrentCounter.ToString());
            HttpContext.Session.SetObjectAsJson(_organicManureSessionKey, model);

            if (model.IsCheckAnswer && !model.IsAnyChangeInField && !model.IsManureTypeChange)
            {
                return RedirectToAction(_checkAnswer);
            }

            return RedirectToAction(_manureApplyingDateAction);
        }

        private async Task ProcessSameDefoliationForField(OrganicManureViewModel model, int i)
        {
            (ManagementPeriod managementPeriod, Error? error) = await _cropLogic.FetchManagementperiodById(model.DefoliationList[i].ManagementPeriodID);
            if (error != null || managementPeriod == null)
            {
                return;
            }

            (Crop crop, error) = await _cropLogic.FetchCropById(managementPeriod.CropID.Value);
            if (error != null || crop == null || crop.DefoliationSequenceID == null)
            {
                return;
            }

            (List<ManagementPeriod> managementPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(managementPeriod.CropID.Value, false);
            int? currentDefoliation = model.DefoliationList[0].Defoliation;

            if (managementPeriodList.Count > 0)
            {
                UpdateManagementPeriodForCheckAnswer(model, managementPeriodList, currentDefoliation);
                UpdateOrganicManureManagementPeriod(model, managementPeriodList, crop, currentDefoliation);
            }

            if (currentDefoliation == null)
            {
                return;
            }

            (string selectedDefoliation, Error? nameError) = await GetDefoliationName(currentDefoliation.Value, crop.DefoliationSequenceID.Value);
            if (nameError != null || string.IsNullOrWhiteSpace(selectedDefoliation))
            {
                return;
            }

            model.DefoliationList[i].DefoliationName = selectedDefoliation;
            model.DefoliationList[i].Defoliation = currentDefoliation;

            UpdateMatchingOrganicManureDefoliation(model, crop, currentDefoliation, selectedDefoliation);
        }

        private static void UpdateMatchingOrganicManureDefoliation(OrganicManureViewModel model, Crop crop, int? defoliation, string defoliationName)
        {
            if (model.OrganicManures == null || model.OrganicManures.Count == 0)
            {
                return;
            }

            int index = model.OrganicManures.FindIndex(f => f.IsGrass && f.FieldID == crop.FieldID);
            if (index < 0)
            {
                return;
            }

            model.OrganicManures[index].Defoliation = defoliation;
            model.OrganicManures[index].DefoliationName = defoliationName;
        }

        // ---------- Shared helpers ----------

        private static void UpdateManagementPeriodForCheckAnswer(OrganicManureViewModel model, List<ManagementPeriod> managementPeriodList, int? defoliationValueForFilter)
        {
            if (!model.IsCheckAnswer || string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                return;
            }

            int filteredManId = managementPeriodList
                .Where(fm => model.UpdatedOrganicIds.Any(mp => mp.ManagementPeriodId == fm.ID))
                .Select(x => x.ID.Value)
                .FirstOrDefault();

            if (model.UpdatedOrganicIds == null || model.UpdatedOrganicIds.Count == 0)
            {
                return;
            }

            foreach (var item in model.UpdatedOrganicIds)
            {
                if (item.ManagementPeriodId == filteredManId)
                {
                    item.ManagementPeriodId = managementPeriodList
                        .Where(x => x.Defoliation == defoliationValueForFilter)
                        .Select(x => x.ID.Value)
                        .First();
                    break;
                }
            }
        }

        private static void UpdateOrganicManureManagementPeriod(OrganicManureViewModel model, List<ManagementPeriod> managementPeriodList, Crop crop, int? defoliationValueForFilter)
        {
            if (model.OrganicManures == null || model.OrganicManures.Count == 0)
            {
                return;
            }

            int index = model.OrganicManures.FindIndex(f => f.IsGrass && f.FieldID == crop.FieldID);
            if (index >= 0)
            {
                model.OrganicManures[index].ManagementPeriodID = managementPeriodList
                    .Where(x => x.Defoliation == defoliationValueForFilter)
                    .Select(x => x.ID.Value)
                    .First();
            }
        }

        private IActionResult RedirectBasedOnCondition(OrganicManureViewModel model, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(model.EncryptedOrgManureId))
            {
                if (model.IsDoubleCropAvailable)
                {
                    TempData[_doubleCropError] = errorMessage;
                    return RedirectToAction(_doubleCropAction,
                        new { q = model.DoubleCropEncryptedCounter });
                }

                TempData[_manureGroupError] = errorMessage;
                return RedirectToAction(_manureGroup);
            }

            TempData[_checkYourAnswerError] = errorMessage;
            return RedirectToAction(_checkAnswer);
        }


        [HttpGet]
        public IActionResult backFromManureApplyingDate()
        {
            _logger.LogTrace($"OrganicManure Controller : backFromManureApplyingDate() action called");
            OrganicManureViewModel? model = GetOrganicManureFromSession();
            if (model == null)
            {
                return RedirectToAction(_farmList, "Farm");
            }

            if (model.IsAnyCropIsGrass.HasValue && model.IsAnyCropIsGrass.Value)
            {
                return RedirectToAction(_defoliationAction, new { q = model.DefoliationEncryptedCounter });
            }

            if (model.IsDoubleCropAvailable)
            {
                return RedirectToAction(_doubleCropAction, new { q = model.DoubleCropEncryptedCounter });
            }

            if (IsOtherMaterialGroup(model))
            {
                return RedirectToAction(_manureGroup);
            }

            if (IsOtherManure(model))
            {
                return RedirectToAction(_otherMaterialName);
            }
            else
            {
                return RedirectToAction(_manureTypeAction);
            }
        }

        private async Task<List<WarningMessage>> GetWarningMessages(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            List<WarningMessage> warningMessages = new List<WarningMessage>();
            try
            {
                if (model != null && model.OrganicManures != null && model.OrganicManures.Count > 0)
                {
                    (ManagementPeriod managementPeriod, _) = await _cropLogic.FetchManagementperiodById(organicManure.ManagementPeriodID);
                    if (model.IsOrgManureNfieldLimitWarning || model.IsNMaxLimitWarning || model.IsClosedPeriodWarning || model.IsApplicationJulyToSeptWarning || model.IsEndClosedPeriodFebruaryWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks || model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150)
                    {
                        AddOrganicManureNfieldLimitWarning(model, warningMessages, organicManure, managementPeriod);
                        AddNMaxLimitWarning(model, warningMessages, organicManure, managementPeriod);
                        AddClosedPeriodWarning(model, warningMessages, organicManure, managementPeriod);

                        AddEndClosedPeriodFebruaryWarning(model, warningMessages, organicManure, managementPeriod);
                        AddEndClosedPeriodFebruaryExistWithinThreeWeeks(model, warningMessages, organicManure, managementPeriod);
                        AddStartPeriodEndFebOrganicAppRateExceedMaxN150(model, warningMessages, organicManure, managementPeriod);
                        if (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland)
                        {
                            AddApplicationJulyToSeptWarning(model, warningMessages, organicManure, managementPeriod);
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

        private static void AddStartPeriodEndFebOrganicAppRateExceedMaxN150(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.StartClosedPeriodEndFebWarningLevelID;
                warningMessage.WarningCodeID = model.StartClosedPeriodEndFebWarningCodeID;
                warningMessage.Header = model.StartClosedPeriodEndFebWarningHeader;
                warningMessage.Para1 = model.StartClosedPeriodEndFebWarningPara1;
                warningMessage.Para2 = model.StartClosedPeriodEndFebWarningPara2;
                warningMessage.Para3 = model.StartClosedPeriodEndFebWarningPara3;
                warningMessages.Add(warningMessage);
            }
        }

        private static void AddEndClosedPeriodFebruaryExistWithinThreeWeeks(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID;
                warningMessage.WarningCodeID = model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID;
                warningMessage.Header = model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader;
                warningMessage.Para1 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1;
                warningMessage.Para2 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2;
                warningMessage.Para3 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3;
                warningMessages.Add(warningMessage);
            }
        }

        private static void AddEndClosedPeriodFebruaryWarning(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsEndClosedPeriodFebruaryWarning)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.EndClosedPeriodEndFebWarningLevelID;
                warningMessage.WarningCodeID = model.EndClosedPeriodEndFebWarningCodeID;
                warningMessage.Header = model.EndClosedPeriodEndFebWarningHeader;
                warningMessage.Para1 = model.EndClosedPeriodEndFebWarningPara1;
                warningMessage.Para2 = model.EndClosedPeriodEndFebWarningPara2;
                warningMessage.Para3 = model.EndClosedPeriodEndFebWarningPara3;
                warningMessages.Add(warningMessage);
            }
        }

        private static void AddNMaxLimitWarning(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsNMaxLimitWarning)
            {
                WarningMessage warningMessage = new WarningMessage();

                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
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

        private static void AddClosedPeriodWarning(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsClosedPeriodWarning)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.ClosedPeriodWarningLevelID;
                warningMessage.WarningCodeID = model.ClosedPeriodWarningCodeID;
                warningMessage.Header = model.ClosedPeriodWarningHeader;
                warningMessage.Para1 = model.ClosedPeriodWarningPara1;
                warningMessage.Para2 = model.ClosedPeriodWarningPara2;
                warningMessage.Para3 = model.ClosedPeriodWarningPara3;
                warningMessages.Add(warningMessage);
            }
        }
        private static void AddApplicationJulyToSeptWarning(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsApplicationJulyToSeptWarning)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.ApplicationJulyToSeptLevelID;
                warningMessage.WarningCodeID = model.ApplicationJulyToSeptCodeID;
                warningMessage.Header = model.ApplicationJulyToSeptHeader;
                warningMessage.Para1 = model.ApplicationJulyToSeptPara1;
                warningMessage.Para2 = model.ApplicationJulyToSeptPara2;
                warningMessage.Para3 = model.ApplicationJulyToSeptPara3;
                warningMessages.Add(warningMessage);
            }
        }

        private static void AddOrganicManureNfieldLimitWarning(OrganicManureViewModel model, List<WarningMessage> warningMessages, OrganicManureDataViewModel organicManure, ManagementPeriod managementPeriod)
        {
            if (model.IsOrgManureNfieldLimitWarning)
            {
                WarningMessage warningMessage = new WarningMessage();
                warningMessage.FieldID = organicManure.FieldID ?? 0;
                warningMessage.CropID = managementPeriod.CropID ?? 0;
                warningMessage.JoiningID = null;
                warningMessage.WarningLevelID = model.NmaxWarningLevelID;
                warningMessage.WarningCodeID = model.NmaxWarningCodeID;
                warningMessage.Header = model.NmaxWarningHeader;
                warningMessage.Para1 = model.NmaxWarningPara1;
                warningMessage.Para2 = model.NmaxWarningPara2;
                warningMessage.Para3 = model.NmaxWarningPara3;
                warningMessages.Add(warningMessage);
            }
        }

        private async Task<(List<SelectListItem>, Error?)> GetDefoliationList(OrganicManureViewModel model)
        {
            if (model.IsSameDefoliationForAll == true)
            {
                return await GetDefoliationListForAll(model);
            }

            return await GetDefoliationListSingleMode(model);
        }

        private async Task<(List<SelectListItem>, Error?)> GetDefoliationListForAll(OrganicManureViewModel model)
        {
            var defoliationGroups = new List<List<SelectListItem>>();
            var grassFields = model.OrganicManures.Where(x => x.IsGrass).ToList();

            foreach (var manure in grassFields)
            {
                var (list, error) = await GetFieldDefoliationList(model.HarvestYear.Value, manure.FieldID);
                if (error != null) return (new List<SelectListItem>(), error);
                if (list.Any()) defoliationGroups.Add(list);
            }

            if (!defoliationGroups.Any())
            {
                return (new List<SelectListItem>(), null);
            }

            var common = Functions.GetCommonDefoliations(defoliationGroups);
            var result = Functions.NormalizeDefoliationText(common);
            ViewBag.DefoliationList = result;
            return (result, null);
        }

        private async Task<(List<SelectListItem>, Error?)> GetDefoliationListSingleMode(OrganicManureViewModel model)
        {
            if (model.DefoliationCurrentCounter < 0)
            {
                return (new List<SelectListItem>(), null);
            }

            int fieldId = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;

            var (list, error) = await GetFieldDefoliationList(model.HarvestYear.Value, fieldId);
            if (error != null)
            {
                return (new List<SelectListItem>(), error);
            }

            var normalized = Functions.NormalizeDefoliationText(list);
            ViewBag.DefoliationList = normalized;

            return (normalized, null);
        }

        //common helper methods
        private async Task<(List<SelectListItem>, Error?)> GetFieldDefoliationList(int harvestYear, int? fieldId)
        {
            var empty = new List<SelectListItem>();
            if (!fieldId.HasValue) return (empty, null);

            var (cropList, error) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId.Value, harvestYear);

            if (HasErrorOrNoGrass(cropList, error))
                return (empty, error);

            var crop = cropList.First(x => x.CropTypeID == (int)CropTypes.Grass);
            if (!crop.DefoliationSequenceID.HasValue)
                return (empty, null);

            return await BuildDefoliationSelectList(crop);
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

            var (periods, err) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
            if (periods == null) return (empty, err);

            var defoNumbers = periods.Select(x => x.Defoliation.Value).ToList();

            var (seq, err2) = await _cropLogic.FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
            if (seq == null) return (empty, err2);

            var names = seq.DefoliationSequenceDescription.Split(',').Select(p => p.Trim()).ToArray();

            var list = defoNumbers
    .OrderBy(num => num).Select(num => new SelectListItem
    {
        Text = Functions.FormatDefoliationLabel(num, names),
        Value = num.ToString()
    }).ToList();

            return (list, null);
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

        private static async Task<OrganicManureViewModel> GetDatesFromClosedPeriod(OrganicManureViewModel model, string closedPeriod)
        {
            var (startDate, endDate) = GetClosedPeriodDates(closedPeriod, model.HarvestYear ?? 0);

            model.ClosedPeriodStartDate = startDate;
            model.ClosedPeriodEndDate = endDate;
            return await Task.FromResult(model);
        }
        private static (DateTime? startDate, DateTime? endDate) GetClosedPeriodDates(string closedPeriod, int harvestYear)
        {
            if (string.IsNullOrWhiteSpace(closedPeriod))
                return (null, null);

            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

            Match match = regex.Match(closedPeriod);
            if (!match.Success)
                return (null, null);

            int startDay = int.Parse(match.Groups[1].Value);
            string startMonthStr = match.Groups[2].Value;

            int endDay = int.Parse(match.Groups[3].Value);
            string endMonthStr = match.Groups[4].Value;

            Dictionary<int, string> dtfi = new Dictionary<int, string>()
            {
                {0, Resource.lblJanuary},
                {1, Resource.lblFebruary},
                {2, Resource.lblMarch},
                {3, Resource.lblApril},
                {4, Resource.lblMay},
                {5, Resource.lblJune},
                {6, Resource.lblJuly},
                {7, Resource.lblAugust},
                {8, Resource.lblSeptember},
                {9, Resource.lblOctober},
                {10, Resource.lblNovember},
                {11, Resource.lblDecember}
            };

            int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1;
            int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;


            DateTime startDate;
            DateTime endDate;

            if (startMonth <= endMonth)
            {
                startDate = new DateTime(harvestYear - 1, startMonth, startDay, 0, 0, 0, DateTimeKind.Utc);
                endDate = new DateTime(harvestYear - 1, endMonth, endDay, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                startDate = new DateTime(harvestYear - 1, startMonth, startDay, 0, 0, 0, DateTimeKind.Utc);
                endDate = new DateTime(harvestYear, endMonth, endDay, 0, 0, 0, DateTimeKind.Utc);
            }

            return (startDate, endDate);
        }
        private async Task<(List<SelectListItem>, Error?)> FetchManureGroup()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            (List<CommonResponse> manureGroupList, Error? error) = await _mannerLogic.FetchManureGroupList();
            if (error == null && manureGroupList.Count > 0)
            {
                selectListItems = ToSelectList(manureGroupList.OrderBy(x => x.SortOrder), f => f.Id.ToString(), f => f.Name);
            }
            return (selectListItems, error);
        }

        private async Task<(List<FarmManureTypeResponse>, Error?)> FetchFarmManureGroup(int farmId)
        {
            (List<FarmManureTypeResponse> farmManureTypeList, Error? error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(farmId);
            if (error == null && farmManureTypeList.Count > 0)
            {
                farmManureTypeList = farmManureTypeList
                .Where(farmManureType => IsOtherManureType(farmManureType.ManureTypeID))
                .ToList();
            }
            return (farmManureTypeList, error);
        }
        private OrganicClosedPeriodRequest BuildOrganicClosedPeriodRequest(FieldDetailResponse fieldDetail, OrganicManureViewModel model,
            Farm farm, CropTypeResponse cropTypeResponse, int cropTypeId, bool isPerennial)
        {
            return new OrganicClosedPeriodRequest
            {
                SoilTypeId = fieldDetail.SoilTypeID ?? 0,
                FieldType = fieldDetail.FieldType ?? 0,
                HarvestYear = model.HarvestYear ?? 0,
                SowingDate = fieldDetail.SowingDate?.ToString(_dateStringLiteral),
                CountryId = farm.CountryID ?? 0,
                CropGroupId = cropTypeResponse.CropGroupId,
                CropTypeId = cropTypeId,
                IsPerennial = isPerennial
            };
        }

        private static void ApplyCommonManureProperties(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, int index = 0)
        {
            SetIfHasValue(model.ApplicationMethod, v => organicManure.ApplicationMethodID = v);
            SetIfHasValue(model.ApplicationRate, v => organicManure.ApplicationRate = v);
            SetIfHasValue(model.Area, v => organicManure.AreaSpread = v);
            SetIfHasValue(model.Quantity, v => organicManure.ManureQuantity = v);
            SetIfHasValue(model.TotalRainfall, v => organicManure.Rainfall = v);

            organicManure.ManureTypeID = model.ManureTypeId.Value;
            organicManure.ManureTypeName = model.OtherMaterialName;

            ApplyNutrientValues(model, organicManure);

            SetIfHasValue(model.IncorporationDelay, v => organicManure.IncorporationDelayID = v);
            SetIfHasValue(model.IncorporationMethod, v => organicManure.IncorporationMethodID = v);
            SetIfHasValue(model.SoilDrainageEndDate, v => organicManure.EndOfDrain = v);
            SetIfHasValue(model.WindspeedID, v => organicManure.WindspeedID = v);
            SetIfHasValue(model.MoistureTypeId, v => organicManure.MoistureID = v);
            SetIfHasValue(model.RainfallWithinSixHoursID, v => organicManure.RainfallWithinSixHoursID = v);

            ApplyAutumnCropNitrogen(model, organicManure, index);
        }

        private static void SetIfHasValue<T>(T? value, Action<T> setter) where T : struct
        {
            if (value.HasValue)
            {
                setter(value.Value);
            }
        }

        private static void ApplyNutrientValues(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            if (IsManualNutrientEntry(model))
            {
                SetManualNutrients(model, organicManure);
                return;
            }

            if (model.ManureType != null)
            {
                SetDefaultNutrients(model.ManureType, organicManure);
            }
        }

        private static bool IsManualNutrientEntry(OrganicManureViewModel model)
        {
            return !string.IsNullOrWhiteSpace(model.DefaultNutrientValue) &&
                   model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
        }

        private static void SetManualNutrients(OrganicManureViewModel model, OrganicManureDataViewModel organicManure)
        {
            organicManure.DryMatterPercent = model.DryMatterPercent;
            organicManure.K2O = model.K2O;
            organicManure.MgO = model.MgO;
            organicManure.N = model.N;
            organicManure.NH4N = model.NH4N;
            organicManure.NO3N = model.NO3N;
            organicManure.P2O5 = model.P2O5;
            organicManure.SO3 = model.SO3;
            organicManure.UricAcid = model.UricAcid;
        }

        private static void SetDefaultNutrients(ManureType manureType, OrganicManureDataViewModel organicManure)
        {
            organicManure.DryMatterPercent = manureType.DryMatter;
            organicManure.K2O = manureType.K2O;
            organicManure.MgO = manureType.MgO;
            organicManure.N = manureType.TotalN;
            organicManure.NH4N = manureType.NH4N;
            organicManure.NO3N = manureType.NO3N;
            organicManure.P2O5 = manureType.P2O5;
            organicManure.SO3 = manureType.SO3;
            organicManure.UricAcid = manureType.Uric;
        }

        private static void ApplyAutumnCropNitrogen(OrganicManureViewModel model, OrganicManureDataViewModel organicManure, int index)
        {
            if (model.AutumnCropNitrogenUptakes != null &&
                index < model.AutumnCropNitrogenUptakes.Count)
            {
                organicManure.AutumnCropNitrogenUptake =
                    model.AutumnCropNitrogenUptakes[index].AutumnCropNitrogenUptake;
            }
        }


        private async Task<List<AutumnCropNitrogenUptakeDetail>> BuildAutumnCropNitrogenUptakeAsync(OrganicManureViewModel model)
        {
            var result = new List<AutumnCropNitrogenUptakeDetail>();

            foreach (var field in model.FieldList)
            {
                int fieldId = Convert.ToInt32(field);
                var fieldData = await _fieldLogic.FetchFieldByFieldId(fieldId);
                if (fieldData == null) continue;

                var (crop, _) = await _organicManureLogic
                     .FetchCropTypeByFieldIdAndHarvestYear(fieldId, model.HarvestYear.Value, false);
                (bool isLateSownCropType, Crop? cropData) = await BindIsLateSownCropType(model, fieldId);
                (CropTypeLinkingResponse cropTypeLinkingResponse, Error error) = await _organicManureLogic
                    .FetchCropTypeLinkingByCropTypeId(cropData.CropTypeID.Value);

                if (error != null) continue;

                var payload = new
                {
                    cropTypeId = isLateSownCropType ? cropTypeLinkingResponse.LateSownMannerCropTypeID.Value : cropTypeLinkingResponse.MannerCropTypeID,
                    applicationMonth = model.ApplicationDate.Value.Month
                };

                string json = JsonConvert.SerializeObject(payload);

                var (uptake, err2) = await _organicManureLogic.FetchAutumnCropNitrogenUptake(json);

                if (err2 != null) continue;

                result.Add(new AutumnCropNitrogenUptakeDetail
                {
                    EncryptedFieldId = _organicManureProtector.Protect(fieldId.ToString()),
                    FieldName = fieldData.Name ?? "",
                    CropTypeId = cropData.CropTypeID.Value,
                    CropTypeName = crop.CropType,
                    AutumnCropNitrogenUptake = uptake.value
                });
            }

            return result;
        }

        private async Task<(bool isLateSownCropType, Crop? cropData)> BindIsLateSownCropType(OrganicManureViewModel model, int fieldId)
        {
            bool isLateSownCropType = false;
            (List<Crop> cropList, _) = await _cropLogic.FetchCropPlanByFieldIdAndYear(fieldId, model.HarvestYear.Value);
            Crop? cropData = null;
            if (cropList.Count > 1)
            {
                int cropid = model.DoubleCrop?
                    .FirstOrDefault(x => x.FieldID == cropList[0].FieldID)?
                    .CropID ?? cropList[0].ID.Value;

                cropData = cropList.FirstOrDefault(x => x.ID == cropid);
            }

            else
            {
                cropData = cropList[0];
            }

            if (cropData != null && cropData.SowingDate != null)
            {
                DateTime cutoff = new DateTime(cropData.SowingDate.Value.Year, 9, 15, 0, 0, 0, DateTimeKind.Unspecified);

                isLateSownCropType = cropData.SowingDate.Value.Date > cutoff;
            }

            return (isLateSownCropType, cropData);
        }

        private static void ResetOrganicManures(OrganicManureViewModel model)
        {
            model.OrganicManures ??= new List<OrganicManureDataViewModel>();
            model.OrganicManures.Clear();
        }

        private async Task PopulateOrganicManuresAsync(
            OrganicManureViewModel model,
            List<int> managementIds,
            List<HarvestYearPlanResponse> cropPlans,
            OrganicManureViewModel? previousModel = null)
        {
            ResetOrganicManures(model);

            foreach (var manId in managementIds)
            {
                var organic = new OrganicManureDataViewModel
                {
                    ManagementPeriodID = manId
                };

                if (model.IsCheckAnswer && model.IsAnyCropIsGrass == true && previousModel?.OrganicManures != null)
                {
                    for (int i = 0; i < previousModel.OrganicManures.Count; i++)
                    {
                        if (previousModel.OrganicManures[i].ManagementPeriodID == manId)
                        {
                            organic = await BindDefoliationName(organic, previousModel, manId, cropPlans, i);
                        }
                    }
                }

                model.OrganicManures.Add(organic);
            }
        }


        private static bool IsOtherMaterialGroup(OrganicManureViewModel model)
        {
            return model.ManureGroupIdForFilter == (int)ManureTypes.OtherLiquidMaterials
                || model.ManureGroupIdForFilter == (int)ManureTypes.OtherSolidMaterials;
        }

        private static bool HasError(Error? error)
        {
            return error != null && !string.IsNullOrWhiteSpace(error.Message);
        }
        private IActionResult RedirectToRecommendation(OrganicManureViewModel model)
        {
            string fieldId = model.FieldList[0];
            return RedirectToAction(_recommendations, "Crop", new
            {
                q = model.EncryptedFarmId,
                r = _fieldDataProtector.Protect(fieldId),
                s = model.EncryptedHarvestYear
            });
        }
        private static bool IsDeepAndShallowInjection(OrganicManureViewModel model)
        {
            return model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.DeepInjection2530cm || model.ApplicationMethod == (int)NMP.Commons.Enums.ApplicationMethod.ShallowInjection57cm;
        }
        private static bool IsGrassAndHasDefoliation(List<HarvestYearPlanResponse> cropList)
        {
            return cropList.Count > 0 && cropList.Any(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass && x.DefoliationSequenceID != null);
        }
        private static void ApplyManureTypeName(OrganicManureViewModel model, ManureType manureType)
        {
            model.ManureTypeName = IsOtherMaterialGroup(model)
                ? model.OtherMaterialName
                : manureType?.Name ?? string.Empty;
        }

        private static void ResetWarnings(OrganicManureViewModel model, bool isWarningMsgNeedToShowReset)
        {
            if (isWarningMsgNeedToShowReset)
            {
                model.IsWarningMsgNeedToShow = false;
            }
            model.IsOrgManureNfieldLimitWarning = false;
            model.IsNMaxLimitWarning = false;
            model.IsEndClosedPeriodFebruaryWarning = false;
            model.IsStartPeriodEndFebOrganicAppRateExceedMaxN150 = false;
        }

        private bool TryGetSessionModel(string actionName, out OrganicManureViewModel model, out IActionResult? redirect)
        {
            model = GetOrganicManureFromSession();
            if (model == null)
            {
                _logger.LogTrace("Organic Manure Controller : {Action}() action - Session expired",
                    actionName);

                redirect = Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);

                return false;
            }

            redirect = null;
            return true;
        }
        private static List<SelectListItem> ToSelectList<T>(IEnumerable<T> source, Func<T, string> value, Func<T, string> text)
        {
            return source
                .Select(x => new SelectListItem
                {
                    Value = value(x),
                    Text = text(x)
                })
                .ToList();
        }
        private static void CopyFarmManureToManureNutrientValues(ManureType target, FarmManureTypeResponse? source)
        {
            if (target == null || source == null)
            {
                return;
            }
            target.DryMatter = source.DryMatter;
            target.TotalN = source.TotalN;
            target.NH4N = source.NH4N;
            target.Uric = source.Uric;
            target.NO3N = source.NO3N;
            target.P2O5 = source.P2O5;
            target.K2O = source.K2O;
            target.SO3 = source.SO3;
            target.MgO = source.MgO;
        }
        private void ValidateManureModel(OrganicManureViewModel model)
        {
            AddErrorIfNull(model.ManureTypeId, "ManureTypeId", Resource.MsgManureTypeNotSet);

            AddErrorIfNull(model.ApplicationMethod, "ApplicationMethod",
                string.Format(Resource.MsgApplicationMethodNotSet, model.ManureTypeName));

            AddErrorIfNull(model.ApplicationDate, _applicationDateKey,
                string.Format(Resource.MsgApplyingDateNotSet, model.ManureTypeName));

            AddErrorIfNull(model.DefaultNutrientValue, "DefaultNutrientValue",
                string.Format(Resource.MsgDefaultNutrientValuesNotSet, model.ManureTypeName));

            AddErrorIfNull(model.ApplicationRateMethod, _applicationRateMethodAction,
                string.Format(Resource.MsgApplicationRateMethodNotSet, model.ManureTypeName));

            AddErrorIfNull(model.ApplicationRate, _applicationRateKey, Resource.MsgApplicationRateNotSet);

            if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
            {
                AddErrorIfNull(model.Area, "Area", Resource.MsgAreaNotSet);
                AddErrorIfNull(model.Quantity, "Quantity", Resource.MsgQuantityNotSet);
            }

            AddErrorIfNull(model.IncorporationMethod, _incorporationMethodAction,
                string.Format(Resource.MsgIncorporationMethodNotSet, model.ManureTypeName));

            AddErrorIfNull(model.IncorporationDelay, _incorporationDelayAction,
                string.Format(Resource.MsgIncorporationDelayNotSet, model.ManureTypeName));

            AddErrorIfNull(model.SoilDrainageEndDate, _soilDrainageEndDateKey,
                Resource.MsgEndOfSoilDrainageNotSet);

            AddErrorIfNull(model.RainfallWithinSixHoursID, "RainfallWithinSixHoursID",
                Resource.MsgRainfallWithinSixHoursOfApplicationNotSet);

            AddErrorIfNull(model.TotalRainfall, _totalRainfallKey,
                Resource.MsgTotalRainfallSinceApplicationNotSet);

            AddErrorIfNull(model.WindspeedID, "WindspeedID",
                Resource.MsgWindspeedAtApplicationNotSet);

            AddErrorIfNull(model.MoistureTypeId, "MoistureTypeId",
                Resource.MsgTopsoilMoistureNotSet);
        }
        private void AddErrorIfNull(object? value, string key, string errorMessage)
        {
            if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                ModelState.AddModelError(key, errorMessage);
            }
        }
        private void ValidateApplicationRate(OrganicManureViewModel model)
        {
            if (model.ApplicationRate == null)
                ModelState.AddModelError(_applicationRateKey, Resource.MsgEnterAnapplicationRateBeforeContinuing);

            if (model.ApplicationRate < 0)
                ModelState.AddModelError(_applicationRateKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);

            if (model.ApplicationRate > 250)
                ModelState.AddModelError(_applicationRateKey, Resource.MsgForApplicationRate);
        }


        private void ValidateAreaQuantity(OrganicManureViewModel model)
        {
            ValidateRequired(model);

            ValidateArea(model);
            ValidateQuantity();

            ValidateAreaRules(model);
            ValidateQuantityRules(model);

            CalculateApplicationRate(model);
        }


        private void ValidateRequired(OrganicManureViewModel model)
        {
            if (model.Area == null)
                ModelState.AddModelError(_areaKey, Resource.MsgEnterAValidArea);

            if (model.Quantity == null)
                ModelState.AddModelError(_quantityKey, Resource.MsgEnterAValidQuantity);
        }


        private void ValidateArea(OrganicManureViewModel model)
        {
            if (!ModelState.TryGetValue(_areaKey, out var state))
                return;

            var rawValue = state.RawValue?.ToString();
            var firstError = state.Errors.FirstOrDefault()?.ErrorMessage;

            if (string.IsNullOrEmpty(rawValue))
                return;

            // Max 10 digits (integer part)
            if (rawValue.Split('.')[0].Length > 10)
            {

                ModelState.AddModelError(_areaKey,
                    string.Format(Resource.lblValueMustNotExeedXDigit, Resource.lblArea, 10));
                return;
            }

            // Max 2 decimal places
            if (model.Area.HasValue && Math.Round(model.Area.Value, 2) != model.Area.Value)
            {
                ModelState.AddModelError(_areaKey,
                     string.Format(Resource.lblFarmAreaCanHaveOnlyTwoDecimalPlace, Resource.lblArea.ToLower()));
                return;
            }

            ReplaceNumericError(state, firstError, rawValue, Resource.lblAreas, Resource.lblArea);
        }


        private void ValidateQuantity()
        {
            if (!ModelState.TryGetValue(_quantityKey, out var state))
                return;

            var rawValue = state.RawValue?.ToString();
            var firstError = state.Errors.FirstOrDefault()?.ErrorMessage;

            if (string.IsNullOrEmpty(rawValue))
                return;

            // No decimal allowed
            if (rawValue.Contains("."))
            {
                ModelState.AddModelError(_quantityKey,
                    string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.MsgQuantity));
                return;
            }

            // Max 10 digits
            if (rawValue.Length > 10)
            {
                ModelState.AddModelError(_quantityKey,
                 string.Format(Resource.lblValueMustNotExeedXDigit, Resource.lblQuantity, 10));
                return;
            }

            ReplaceNumericError(state, firstError, rawValue, Resource.lblQuantity, Resource.MsgQuantity);
        }


        private void ValidateAreaRules(OrganicManureViewModel model)
        {
            if (!model.Area.HasValue)
                return;

            if (model.Area == 0)
                ModelState.AddModelError(_areaKey, Resource.MsgAreaMustBeGreaterThanZero);

            if (model.Area < 0)
                ModelState.AddModelError(_areaKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);
        }


        private void ValidateQuantityRules(OrganicManureViewModel model)
        {
            if (!model.Quantity.HasValue)
                return;

            if (model.Quantity < 0)
                ModelState.AddModelError(_quantityKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);
        }


        private void CalculateApplicationRate(OrganicManureViewModel model)
        {
            if (model.Area > 0 && model.Quantity > 0)
            {
                model.ApplicationRate = model.Quantity.Value / model.Area.Value;

                if (model.ApplicationRate > 250)
                {
                    ModelState.AddModelError(_quantityKey, Resource.MsgForApplicationRate);
                }
            }
        }


        private static void ReplaceError(ModelStateEntry state, string message)
        {
            state.Errors.Clear();
            state.Errors.Add(message);
        }

        private static void ReplaceNumericError(
            ModelStateEntry state,
            string firstError,
            string rawValue,
            string pluralLabel,
            string singularLabel)
        {
            var expectedError = string.Format(Resource.lblEnterNumericValue, rawValue, pluralLabel);

            if (!string.IsNullOrEmpty(firstError) && firstError.Equals(expectedError))
            {
                ReplaceError(state,
                    string.Format(Resource.MsgEnterDataOnlyInNumber, singularLabel));
            }
        }
        private async Task<(List<ManureType>, Error?)> GetManureTypeList(OrganicManureViewModel? model)
        {
            if (model != null && model.FarmRB209CountryID.HasValue && model.ManureGroupIdForFilter.HasValue)
            {
                var (manureTypeList, error) = await FetchManureTypeList(model.ManureGroupIdForFilter.Value, model.FarmRB209CountryID.Value);
                model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;
                return (manureTypeList, error);
            }
            return (new List<ManureType>(), null);
        }
        private static void UpdateOrganicManuresFromModel(OrganicManureViewModel model, ManureType? manure)
        {
            if (model?.OrganicManures == null)
                return;

            foreach (var org in model.OrganicManures)
            {
                org.DryMatterPercent = manure?.DryMatter
                                       ?? model.DryMatterPercent
                                       ?? model.ManureType?.DryMatter;

                org.N = manure?.TotalN ?? model.N;
                org.NH4N = manure?.NH4N ?? model.NH4N;
                org.UricAcid = manure?.Uric ?? model.UricAcid;
                org.NO3N = manure?.NO3N ?? model.NO3N;
                org.P2O5 = manure?.P2O5 ?? model.P2O5;
                org.K2O = manure?.K2O ?? model.K2O;
                org.SO3 = manure?.SO3 ?? model.SO3;
                org.MgO = manure?.MgO ?? model.MgO;
            }
        }

        private static void BindNutrientsFromManureType(OrganicManureViewModel model)
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

        private async Task<OrganicManureViewModel> PrepareManureApplyingDateViewModelAsync(OrganicManureViewModel model)
        {
            List<ManureType> manureTypeList = new List<ManureType>();
            Error? error = null;

            (manureTypeList, error) = await GetManureTypeList(model);
            model.ManureTypeName = (error == null && manureTypeList.Count > 0) ? manureTypeList.FirstOrDefault(x => x.Id == model.ManureTypeId)?.Name : string.Empty;
            var manureType = GetAndApplyManureType(model, manureTypeList, error);
            bool isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
            model.HighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen;
            (List<CommonResponse> manureGroupList, Error error1) = await _mannerLogic.FetchManureGroupList();
            model.ManureGroupName = (error1 == null && manureGroupList.Count > 0) ? manureGroupList.FirstOrDefault(x => x.Id == model.ManureGroupId)?.Name : string.Empty;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));

            (FarmResponse? farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);
            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
            {
                TempData["Error"] = error.Message;
            }
            if (farm != null)
            {
                string? closedPeriod = await GetClosedPeriod(model, farm, isHighReadilyAvailableNitrogen);

                model.ClosedPeriod = closedPeriod;
                if (!string.IsNullOrWhiteSpace(closedPeriod))
                {
                    model = await GetDatesFromClosedPeriod(model, closedPeriod);
                    await SetClosedPeriodUIAsync(model);
                }
                model.IsWithinNVZ = await IsAnyFieldWithinNVZ(model.FieldList);


            }
            return model;
        }


    }
}