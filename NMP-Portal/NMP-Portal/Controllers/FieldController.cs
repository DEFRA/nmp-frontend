using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Commons.Enums;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Helpers;
using NMP.Portal.Services;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using Error = NMP.Commons.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class FieldController : Controller
    {
        private readonly ILogger<FieldController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _soilAnalysisDataProtector;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly ISoilService _soilService;
        private readonly ICropService _cropService;
        private readonly IPreviousCroppingService _previousCroppingService;
        private const string CheckAnswerActionName = "CheckAnswer";
        private const string UpdateFieldActionName = "UpdateField";

        public FieldController(ILogger<FieldController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, ISoilService soilService, IFieldService fieldService, ICropService cropService, IPreviousCroppingService previousCroppingService)
        {
            _logger = logger;
            _farmService = farmService;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _soilAnalysisDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.SoilAnalysisController");
            _fieldService = fieldService ?? throw new ArgumentNullException(nameof(fieldService));
            _soilService = soilService;
            _cropService = cropService;
            _previousCroppingService = previousCroppingService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace($"Field Controller : Index() action called");
            return View();
        }

        public IActionResult CreateFieldCancel(string id, string? q)
        {
            _logger.LogTrace("Field Controller : CreateFieldCancel({0}) action called", id);
            if (string.IsNullOrWhiteSpace(q))
            {
                RemoveFieldDataFromSession();
                return RedirectToAction("FarmSummary", "Farm", new { Id = id });
            }
            else
            {
                FieldViewModel? model = LoadFieldDataFromSession();

                if (model != null)
                {
                    RemoveFieldDataFromSession();
                    if (!string.IsNullOrWhiteSpace(model.EncryptedFieldId) && !string.IsNullOrWhiteSpace(model.EncryptedFarmId))
                    {
                        return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId });
                    }
                    return RedirectToAction("FarmSummary", "Farm", new { Id = id });
                }
                else
                {
                    RemoveFieldDataFromSession();
                    return RedirectToAction("FarmSummary", "Farm", new { Id = id });
                }
            }
        }

        public async Task<IActionResult> BackActionForAddField(string id)
        {
            _logger.LogTrace("Field Controller : BackActionForAddField({0}) action called", id);
            FieldViewModel model = LoadFieldDataFromSession() ?? new FieldViewModel();
            try
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                int fieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmID));
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction(CheckAnswerActionName);
                }
                else if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) && !string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    return RedirectToAction(UpdateFieldActionName, new
                    {
                        fieldId = model.EncryptedFieldId,
                        farmId = model.EncryptedFarmId
                    });
                }
                else if (HttpContext.Session.Keys.Contains("ReportData"))
                {
                    ReportViewModel? reportViewModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                    if (reportViewModel != null)
                    {
                        RemoveFieldDataFromSession();
                        return RedirectToAction("ExportFieldsOrCropType", "Report");
                    }
                    return RedirectToAction("FarmSummary", "Farm", new { id = id });
                }
                else if (model.HarvestYear != null && (!string.IsNullOrWhiteSpace(model.EncryptedHarvestYear)))
                {
                    RemoveFieldDataFromSession();
                    return RedirectToAction("HarvestYearOverview", "Crop", new
                    {
                        id = model.EncryptedFarmId,
                        year = model.EncryptedHarvestYear
                    });
                }
                else if (fieldCount > 0)
                {
                    if (model != null && model.CopyExistingField != null && model.CopyExistingField.Value)
                    {
                        return RedirectToAction("CopyFields", "Field");
                    }
                    else
                    {
                        return RedirectToAction("CopyExistingField", "Field", new { q = id });
                    }
                }
                else
                {
                    RemoveFieldDataFromSession();
                    return RedirectToAction("FarmSummary", "Farm", new { id = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in BackActionForAddField() action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["AddFieldError"] = ex.Message;
                return View("AddField", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddField(string q, string? r)//EncryptedfarmId EncryptedYear
        {
            _logger.LogTrace("Field Controller : AddField({0}) action called", q);
            FieldViewModel model = LoadFieldDataFromSession() ?? new FieldViewModel();
            Error error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    _logger.LogTrace("Field Controller : AddField() action : Farm Id is null or empty");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.BadRequest);
                }

                if (!string.IsNullOrEmpty(q))
                {
                    model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.EncryptedFarmId = q;

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmID);
                    model.isEnglishRules = farm.EnglishRules;
                    model.FarmName = farm.Name;

                    model.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
                    model.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;

                }

                if (!string.IsNullOrWhiteSpace(r))
                {
                    model.EncryptedHarvestYear = r;
                    model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in BackActionForAddField() action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["Error"] = string.Concat(error.Message == null ? "" : error.Message, ex.Message);
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
            }

            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : AddField() action called");
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFieldName);
            }

            bool isFieldAlreadyexist = await _fieldService.IsFieldExistAsync(field.FarmID, field.Name);
            if (isFieldAlreadyexist)
            {
                ModelState.AddModelError("Name", Resource.MsgFieldAlreadyExist);
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            if (field.CopyExistingField != null && (field.CopyExistingField.Value))
            {
                (FieldResponse fieldResponse, Error error) = await _fieldService.FetchFieldSoilAnalysisAndSnsById(field.ID.Value);
                if (fieldResponse != null && string.IsNullOrWhiteSpace(error.Message))
                {
                    field.NationalGridReference = fieldResponse.Field.NationalGridReference;
                    field.OtherReference = fieldResponse.Field.OtherReference;
                    field.TotalArea = fieldResponse.Field.TotalArea;
                    field.CroppedArea = fieldResponse.Field.CroppedArea;
                    field.LPIDNumber = fieldResponse.Field.LPIDNumber;
                    field.ManureNonSpreadingArea = fieldResponse.Field.ManureNonSpreadingArea;
                    field.NVZProgrammeID = fieldResponse.Field.NVZProgrammeID;
                    field.IsWithinNVZ = fieldResponse.Field.IsWithinNVZ;
                    field.IsAbove300SeaLevel = fieldResponse.Field.IsAbove300SeaLevel;
                    field.SoilReleasingClay = fieldResponse.Field.SoilReleasingClay;
                    field.SoilOverChalk = fieldResponse.Field.SoilOverChalk;
                    field.SoilTypeID = fieldResponse.Field.SoilTypeID;
                    List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
                    SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);
                    if (soilType != null && soilType.KReleasingClay)
                    {
                        field.IsSoilReleasingClay = true;
                    }
                    else
                    {
                        field.IsSoilReleasingClay = false;
                    }
                    field.SoilType = await _soilService.FetchSoilTypeById(field.SoilTypeID.Value);


                    SetFieldDataToSession(field);
                }
                else
                {
                    TempData["AddFieldError"] = error.Message;
                    return View("AddField", field);
                }
                return RedirectToAction("RecentSoilAnalysisQuestion");
            }


            return RedirectToAction("FieldMeasurements");
        }
        [HttpGet]
        public IActionResult FieldMeasurements()
        {
            _logger.LogTrace($"Field Controller : FieldMeasurements() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : FieldMeasurements() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldMeasurements(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : FieldMeasurements() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("TotalArea"))
            {
                var InvalidFormatError = ModelState["TotalArea"]?.Errors.Count > 0 ?
                                ModelState["TotalArea"]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["TotalArea"].AttemptedValue, Resource.lblTotalFieldArea)))
                {
                    ModelState["TotalArea"]?.Errors.Clear();
                    ModelState["TotalArea"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("CroppedArea"))
            {
                var InvalidFormatError = ModelState["CroppedArea"]?.Errors.Count > 0 ?
                                ModelState["CroppedArea"]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["CroppedArea"].AttemptedValue, Resource.lblCroppedArea)))
                {
                    ModelState["CroppedArea"]?.Errors.Clear();
                    ModelState["CroppedArea"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ManureNonSpreadingArea"))
            {
                var InvalidFormatError = ModelState["ManureNonSpreadingArea"]?.Errors.Count > 0 ?
                                ModelState["ManureNonSpreadingArea"]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ManureNonSpreadingArea"].AttemptedValue, Resource.lblManureNonSpreadingArea)))
                {
                    ModelState["ManureNonSpreadingArea"]?.Errors.Clear();
                    ModelState["ManureNonSpreadingArea"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }

            if (field.TotalArea == null || field.TotalArea == 0)
            {
                ModelState.AddModelError("TotalArea", Resource.MsgEnterTotalFieldArea);
            }

            if (field.TotalArea != null && field.TotalArea < 0)
            {
                ModelState.AddModelError("TotalArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }

            if (field.CroppedArea == null || field.CroppedArea == 0)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgEnterTheCroppedArea);
            }

            if (field.CroppedArea != null && field.CroppedArea < 0)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }

            if (field.CroppedArea > field.TotalArea)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgCroppedAreaIsGreaterThanTotalArea);
            }

            if (field.CroppedArea != null && field.CroppedArea < 0)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }

            if (field.ManureNonSpreadingArea > field.TotalArea)
            {
                ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgManureNonSpreadingAreaIsGreaterThanTotalArea);
            }
            if (field.ManureNonSpreadingArea != null && field.ManureNonSpreadingArea < 0)
            {
                ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            if (!(field.CroppedArea.HasValue) && !(field.ManureNonSpreadingArea.HasValue))
            {
                field.CroppedArea = field.TotalArea;
            }

            if ((!field.CroppedArea.HasValue) && (field.ManureNonSpreadingArea.HasValue) && field.ManureNonSpreadingArea > 0)
            {
                field.CroppedArea = field.TotalArea - field.ManureNonSpreadingArea;
            }

            string farmId = _farmDataProtector.Unprotect(field.EncryptedFarmId);
            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

            field.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
            field.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction("NVZField");
        }

        [HttpGet]
        public async Task<IActionResult> NVZField()
        {
            _logger.LogTrace($"Field Controller : NVZField() action called");
            Error error = new Error();

            FieldViewModel? model = LoadFieldDataFromSession();

            if (model == null)
            {
                _logger.LogTrace("Field Controller : NVZField() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (model.IsWithinNVZForFarm.HasValue && model.IsWithinNVZForFarm.Value)
            {
                return View(model);
            }

            model.IsWithinNVZ = Convert.ToBoolean(farm.NVZFields);
            SetFieldDataToSession(model);
            return RedirectToAction("ElevationField");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZField(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : NVZField() post action called");
            if (model.IsWithinNVZ == null)
            {
                ModelState.AddModelError("IsWithinNVZ", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetFieldDataToSession(model);

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction("ElevationField");
        }

        [HttpGet]
        public async Task<IActionResult> ElevationField()
        {
            _logger.LogTrace($"Field Controller : ElevationField() action called");
            Error error = new Error();

            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : ElevationField() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (model.IsAbove300SeaLevelForFarm.HasValue && model.IsAbove300SeaLevelForFarm.Value)
            {
                return View(model);
            }
            model.IsAbove300SeaLevel = Convert.ToBoolean(farm.FieldsAbove300SeaLevel);
            SetFieldDataToSession(model);
            return RedirectToAction("SoilType");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ElevationField(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : ElevationField() post action called");
            if (field.IsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("IsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction("SoilType");
        }

        [HttpGet]
        public async Task<IActionResult> SoilType()
        {
            _logger.LogTrace($"Field Controller : SoilType() action called");
            Error error = new Error();
            FieldViewModel? model = LoadFieldDataFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace("Field Controller : SoilType() action : Field data is not available in session");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }


                List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
                if (soilTypes != null && soilTypes.Any())
                {
                    var country = model.isEnglishRules ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
                    var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                    ViewBag.SoilTypesList = soilTypesList;
                }
                else
                {
                    ViewBag.SoilTypeError = Resource.MsgServiceNotAvailable;
                    RedirectToAction("ElevationField");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in SoilType() action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("ElevationField");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilType(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : SoilType() post action called");

            try
            {
                if (field.SoilTypeID == null)
                {
                    ModelState.AddModelError("SoilTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblSoilType.ToLower()));
                }

                List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
                if (soilTypes.Count > 0 && soilTypes.Any())
                {
                    var country = field.isEnglishRules ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
                    var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                    soilTypes = soilTypesList;
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.SoilTypesList = soilTypes;
                    return View(field);
                }

                field.SoilType = await _soilService.FetchSoilTypeById(field.SoilTypeID.Value);
                SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);

                bool isSoilTypeChange = false;
                FieldViewModel? fieldData = LoadFieldDataFromSession();
                if (fieldData == null)
                {
                    _logger.LogTrace("Field Controller : SoilType() post action : Field data is not available in session");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                if (fieldData.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.DeepClayey &&
                    field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.DeepClayey)
                {
                    isSoilTypeChange = true;
                }

                SetFieldDataToSession(field);

                if (field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                {
                    return RedirectToAction("SoilOverChalk");
                }

                if (field.IsCheckAnswer && (!isSoilTypeChange))
                {
                    field.IsSoilReleasingClay = false;
                    field.SoilReleasingClay = null;
                    SetFieldDataToSession(field);
                    return RedirectToAction(CheckAnswerActionName);
                }

                if (soilType != null && soilType.KReleasingClay)
                {
                    field.IsSoilReleasingClay = true;
                    SetFieldDataToSession(field);
                    return RedirectToAction("SoilReleasingClay");
                }

                field.SoilReleasingClay = null;
                field.IsSoilReleasingClay = false;
                SetFieldDataToSession(field);

                if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
                {
                    return RedirectToAction(UpdateFieldActionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in SoilType() post action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return View(field);
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
        }

        [HttpGet]
        public IActionResult SoilReleasingClay()
        {
            _logger.LogTrace($"Field Controller : SoilReleasingClay() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : SoilReleasingClay() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilReleasingClay(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : SoilReleasingClay() post action called");
            if (field.SoilReleasingClay == null)
            {
                ModelState.AddModelError("SoilReleasingClay", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            field.IsSoilReleasingClay = true;
            if (!ModelState.IsValid)
            {
                return View(field);
            }

            List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
            if (soilTypes.Count > 0 && soilTypes.Any())
            {
                var country = field.isEnglishRules ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
                var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                soilTypes = soilTypesList;
            }

            var soilType = soilTypes?.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);
            if (soilType != null && !soilType.KReleasingClay)
            {
                field.SoilReleasingClay = false;
            }

            SetFieldDataToSession(field);
            if (field.IsCheckAnswer && (!field.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
        }

        [HttpGet]
        public IActionResult SulphurDeficient()
        {
            _logger.LogTrace($"Field Controller : SulphurDeficient() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : SulphurDeficient() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            model.IsSoilReleasingClay = false;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SulphurDeficient(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SulphurDeficient() action called");
            if (model.SoilAnalyses.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetFieldDataToSession(model);

            if (model.IsCheckAnswer && (!model.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            return RedirectToAction("SoilDate");
        }

        [HttpGet]
        public async Task<IActionResult> SoilDate()
        {
            _logger.LogTrace($"Field Controller : SoilDateAndPHLevel() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : SoilDateAndPHLevel() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilDate(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilDateAndPHLevel() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblSoilAnalysesDate))
            { NormalizeDateModelStateErrors(); }
            ValidateSoilAnalysisDate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetFieldDataToSession(model);
            if (model.IsCheckAnswer && (!model.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            return RedirectToAction("SoilNutrientValueType");
        }

        private void NormalizeDateModelStateErrors()
        {

            var error = ModelState[Resource.lblSoilAnalysesDate]?.Errors.FirstOrDefault()?.ErrorMessage;
            if (string.IsNullOrEmpty(error))
                return;

            var invalidDateMessages = new HashSet<string>
    {
        string.Format(Resource.MsgDateMustBeARealDate, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeAMonth, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeAMonthAndYear, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeADayAndYear, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeAYear, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeADay, Resource.lblTheDate),
        string.Format(Resource.MsgDateMustIncludeADayAndMonth, Resource.lblTheDate)
    };

            if (invalidDateMessages.Contains(error))
            {
                ModelState[Resource.lblSoilAnalysesDate]?.Errors.Clear();
                ModelState[Resource.lblSoilAnalysesDate]?.Errors.Add(Resource.MsgTheDateMustInclude);
            }

        }

        private void ValidateSoilAnalysisDate(FieldViewModel model)
        {
            var date = model.SoilAnalyses.Date;

            if (date == null)
            {
                ModelState.AddModelError(Resource.lblSoilAnalysesDate, Resource.MsgEnterADateBeforeContinuing);
                return;
            }

            if (DateTime.TryParseExact(model.SoilAnalyses.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || IsOutOfAllowedRange(date.Value))
            {
                ModelState.AddModelError(Resource.lblSoilAnalysesDate, Resource.MsgEnterTheDateInNumber);
            }
        }

        private static bool IsOutOfAllowedRange(DateTime date)
        {
            var year = date.Year;
            return year < 1601 || year > DateTime.Now.AddYears(1).Year;
        }

        [HttpGet]
        public Task<IActionResult> SoilNutrientValueType()
        {
            _logger.LogTrace($"Field Controller : SoilNutrientValueType() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : SoilNutrientValueType() action : Field data is not available in session");
                return Task.FromResult<IActionResult>(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            return Task.FromResult<IActionResult>(View(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNutrientValueType(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilNutrientValueType() post action called");
            if (model.IsSoilNutrientValueTypeIndex == null)
            {
                ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetFieldDataToSession(model);

            return RedirectToAction("SoilNutrientValue");
        }

        [HttpGet]
        public Task<IActionResult> SoilNutrientValue()
        {
            _logger.LogTrace($"Field Controller : SoilNutrientValue() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : SoilNutrientValue() action : Field data is not available in session");
                return Task.FromResult<IActionResult>(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            return Task.FromResult<IActionResult>(View(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNutrientValue(FieldViewModel model)
        {
            _logger.LogTrace("Field Controller : SoilNutrientValue() post action called");
            Error error = null;
            try
            {
                if (model.IsSoilNutrientValueTypeIndex != null && model.IsSoilNutrientValueTypeIndex.Value)
                {
                    if (!string.IsNullOrEmpty(model.PotassiumIndexValue))
                    {
                        if (int.TryParse(model.PotassiumIndexValue, out int value))
                        {
                            if (value > 9 || value < 0)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgEnterValidValueForNutrientIndex);
                            }
                            if (value == 2)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", string.Format(Resource.MsgValueIsNotAValidValueForPotassium, value));
                            }
                        }
                        else
                        {
                            if ((model.PotassiumIndexValue.ToString() != Resource.lblTwoMinus) &&
                                                   (model.PotassiumIndexValue.ToString() != Resource.lblTwoPlus))
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                            }
                        }


                    }
                    if (model.SoilAnalyses.PH == null && (string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) &&
                    model.SoilAnalyses.PhosphorusIndex == null && model.SoilAnalyses.MagnesiumIndex == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.PhosphorusIndex"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.PhosphorusIndex"]?.Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.PhosphorusIndex"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.PhosphorusIndex"].AttemptedValue, Resource.lblPhosphorusIndex)))
                        {
                            ModelState["SoilAnalyses.PhosphorusIndex"]?.Errors.Clear();
                            ModelState["SoilAnalyses.PhosphorusIndex"]?.Errors.Add(Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                        }
                    }

                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.MagnesiumIndex"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.MagnesiumIndex"]?.Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.MagnesiumIndex"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.MagnesiumIndex"].AttemptedValue, Resource.lblMagnesiumIndex)))
                        {
                            ModelState["SoilAnalyses.MagnesiumIndex"]?.Errors.Clear();
                            ModelState["SoilAnalyses.MagnesiumIndex"]?.Errors.Add(Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                        }
                    }
                }
                else
                {
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Potassium"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Potassium"]?.Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Potassium"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Potassium"].AttemptedValue, Resource.lblPotassiumPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Potassium"]?.Errors.Clear();
                            ModelState["SoilAnalyses.Potassium"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Phosphorus"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Phosphorus"]?.Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Phosphorus"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Phosphorus"].AttemptedValue, Resource.lblPhosphorusPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Phosphorus"]?.Errors.Clear();
                            ModelState["SoilAnalyses.Phosphorus"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Magnesium"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Magnesium"]?.Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Magnesium"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Magnesium"].AttemptedValue, Resource.lblMagnesiumPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Magnesium"]?.Errors.Clear();
                            ModelState["SoilAnalyses.Magnesium"]?.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if (model.SoilAnalyses.PH == null && model.SoilAnalyses.Potassium == null &&
                        model.SoilAnalyses.Phosphorus == null && model.SoilAnalyses.Magnesium == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.SoilAnalyses.PhosphorusMethodologyID = (int)PhosphorusMethodology.Olsens;

                if (model.SoilAnalyses.Phosphorus != null || model.SoilAnalyses.Potassium != null ||
                    model.SoilAnalyses.Magnesium != null)
                {
                    if (model.IsSoilNutrientValueTypeIndex != null && (!model.IsSoilNutrientValueTypeIndex.Value))
                    {
                        (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
                        if (error == null && nutrients.Count > 0)
                        {
                            int phosphorusId = 1;
                            int potassiumId = 2;
                            int magnesiumId = 3;

                            if (model.SoilAnalyses.Phosphorus != null)
                            {
                                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                                if (phosphorusNutrient != null)
                                {
                                    phosphorusId = phosphorusNutrient.nutrientId;
                                }
                                (string phosphorusIndexValue, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.SoilAnalyses.Phosphorus, (int)PhosphorusMethodology.Olsens);
                                if (!string.IsNullOrWhiteSpace(phosphorusIndexValue) && error == null)
                                {
                                    model.SoilAnalyses.PhosphorusIndex = Convert.ToInt32(phosphorusIndexValue.Trim());

                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }

                            if (model.SoilAnalyses.Magnesium != null)
                            {
                                var magnesiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                                if (magnesiumNutrient != null)
                                {
                                    magnesiumId = magnesiumNutrient.nutrientId;
                                }
                                (string magnesiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.SoilAnalyses.Magnesium, (int)MagnesiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(magnesiumIndexValue) && error == null)
                                {
                                    model.SoilAnalyses.MagnesiumIndex = Convert.ToInt32(magnesiumIndexValue.Trim());
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }

                            if (model.SoilAnalyses.Potassium != null)
                            {
                                var potassiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                                if (potassiumNutrient != null)
                                {
                                    potassiumId = potassiumNutrient.nutrientId;
                                }
                                (string potassiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.SoilAnalyses.Potassium, (int)PotassiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(potassiumIndexValue) && error == null)
                                {
                                    model.PotassiumIndexValue = potassiumIndexValue.Trim();
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                        }
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                        {
                            ViewBag.Error = error.Message;
                            return View(model);
                        }
                    }
                    else
                    {
                        model.SoilAnalyses.Phosphorus = null;
                        model.SoilAnalyses.Magnesium = null;
                        model.SoilAnalyses.Potassium = null;
                    }
                }

                SetFieldDataToSession(model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction(CheckAnswerActionName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in SoilNutrientValue() post action : {0}, {1}", ex.Message, ex.StackTrace);
                ViewBag.Error = string.Concat(error, ex.Message);
                return View(model);
            }

            return RedirectToAction("LastHarvestYear");
        }

        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            _logger.LogTrace($"Field Controller : CropGroups() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : CropGroups() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            try
            {
                List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                List<CropGroupResponse> cropGroupArables = cropGroups.Where(x => x.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName).ToList();

                ViewBag.CropGroupList = cropGroupArables;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in CropGroups() action : {0}, {1}", ex.Message, ex.StackTrace);

                if (model.RecentSoilAnalysisQuestion.HasValue && model.RecentSoilAnalysisQuestion.Value)
                {
                    ViewBag.Error = ex.Message;
                    return RedirectToAction("SoilNutrientValue");
                }
                else if (model.PreviousCroppings.HasGrassInLastThreeYear != null)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("HasGrassInLastThreeYear");
                }
                else
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("RecentSoilAnalysisQuestion");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(FieldViewModel field)
        {
            _logger.LogTrace("Field Controller : CropGroups() post action called");
            if (field.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropGroupResponse> cropGroups = await _fieldService.FetchCropGroups();
                if (cropGroups.Count > 0)
                {
                    List<CropGroupResponse> cropGroupArables = cropGroups.Where(x => x.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName).ToList();
                    ViewBag.CropGroupList = cropGroupArables;
                }
                return View(field);
            }

            FieldViewModel? fieldData = LoadFieldDataFromSession();
            if (fieldData == null)
            {
                _logger.LogTrace("Field Controller : CropGroups() post action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            if (fieldData.CropGroupId != field.CropGroupId)
            {
                field.CropType = string.Empty;
                field.CropTypeID = null;
            }

            field.CropGroup = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
            SetFieldDataToSession(field);
            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            _logger.LogTrace($"Field Controller : CropTypes() action called");
            FieldViewModel? model = LoadFieldDataFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace("Field Controller : CropTypes() action : Field data is not available in session");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                var country = model.isEnglishRules ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();

                ViewBag.CropTypeList = cropTypeList;
                if (cropTypeList.Count == 1)
                {
                    if (cropTypeList[0].CropTypeId == (int)NMP.Commons.Enums.CropTypes.Other)
                    {
                        model.CropTypeID = cropTypeList[0].CropTypeId;
                        model.CropType = cropTypeList[0].CropType;
                        SetFieldDataToSession(model);
                        if (model.IsCheckAnswer)
                        {
                            return RedirectToAction(CheckAnswerActionName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CropTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CropTypes() post action called");
            if (field.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(field.CropGroupId ?? 0);
                var country = field.isEnglishRules ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
                ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).OrderBy(c => c.CropType).ToList();
                return View(field);
            }
            field.CropType = await _fieldService.FetchCropTypeById(field.CropTypeID.Value);
            SetFieldDataToSession(field);
            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }
            return RedirectToAction(CheckAnswerActionName);
        }


        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"Field Controller : CheckAnswer() action called");
            FieldViewModel? model = null;
            try
            {
                if (HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model == null)
                {
                    model = new FieldViewModel();
                }
                model.IsRecentSoilAnalysisQuestionChange = false;
                model.IsCheckAnswer = true;
                model.IsLastHarvestYearChange = false;
                if (model.SoilOverChalk != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                {
                    model.SoilOverChalk = null;
                }
                if (model.SoilReleasingClay != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.DeepClayey)
                {
                    model.SoilReleasingClay = null;
                    model.IsSoilReleasingClay = false;
                }
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.PreviousCroppings.GrassManagementOptionID)?.Name;

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.PreviousCroppings.SoilNitrogenSupplyItemID)?.Name;
                model.IsHasGrassInLastThreeYearChange = false;
                SetFieldDataToSession(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace($"Field Controller : BackCheckAnswer() action called");
            FieldViewModel? model = LoadFieldDataFromSession();

            if (model == null)
            {
                _logger.LogTrace("Field Controller : BackCheckAnswer() action : Field data is not available in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            model.IsCheckAnswer = false;
            SetFieldDataToSession(model);

            if (model.PreviousCroppings.HasGrassInLastThreeYear != null && model.PreviousCroppings.HasGrassInLastThreeYear.Value)
            {
                if (!model.PreviousGrassYears.Contains(model.LastHarvestYear ?? 0))
                {
                    return RedirectToAction("CropTypes");
                }
                else
                {
                    if (model.PreviousCroppings.HasGreaterThan30PercentClover == false)
                    {
                        return RedirectToAction("SoilNitrogenSupplyItems");
                    }
                    else
                    {
                        return RedirectToAction("HasGreaterThan30PercentClover");
                    }
                }
            }

            return RedirectToAction("CropTypes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CheckAnswer() post action called");
            try
            {
                if (model.PreviousCroppings != null && model.PreviousCroppings.HasGrassInLastThreeYear == false)
                {
                    if (!model.CropGroupId.HasValue)
                    {
                        ModelState.AddModelError("CropGroupId", string.Format("{0} {1}", string.Format(Resource.lblWhatWasThePreviousCropGroupForCheckAnswere, model.LastHarvestYear), Resource.lblNotSet));
                    }
                    if (!model.CropTypeID.HasValue)
                    {
                        ModelState.AddModelError("CropTypeID", string.Format("{0} {1}", string.Format(Resource.lblWhatWasThePreviousCropTypeForCheckAnswere, model.LastHarvestYear), Resource.lblNotSet));
                    }
                }
                if (model.PreviousCroppings != null && model.PreviousCroppings.HasGrassInLastThreeYear == true)
                {
                    if (!model.PreviousGrassYears.Contains(model.LastHarvestYear.Value))
                    {
                        if (string.IsNullOrWhiteSpace(model.CropGroup))
                        {
                            ModelState.AddModelError("CropGroupId", string.Format("{0} {1}", string.Format(Resource.lblWhatWasThePreviousCropGroupForCheckAnswere, model.LastHarvestYear), Resource.lblNotSet));
                        }
                        if (string.IsNullOrWhiteSpace(model.CropType))
                        {
                            ModelState.AddModelError("CropTypeID", string.Format("{0} {1}", string.Format(Resource.lblWhatWasThePreviousCropTypeForCheckAnswere, model.LastHarvestYear), Resource.lblNotSet));
                        }
                    }

                    if (model.PreviousGrassYears == null)
                    {
                        ModelState.AddModelError("PreviousGrassYears", string.Format("{0} {1}", string.Format(Resource.lblInWhichYearsWasUsedForGrass, model.Name), Resource.lblNotSet));
                    }
                    if (model.PreviousCroppings.GrassManagementOptionID == null)
                    {
                        ModelState.AddModelError("PreviousCroppings.GrassManagementOptionID", string.Format("{0} {1}", Resource.lblHowWasTheGrassTypicallyManagedEachYear, Resource.lblNotSet));
                    }

                    if (model.PreviousCroppings.HasGreaterThan30PercentClover == null)
                    {
                        ModelState.AddModelError("PreviousCroppings.HasGreaterThan30PercentClover", string.Format("{0} {1}", string.Format(Resource.lblDoesFieldTypicallyHaveMoreThan30PercentClover, model.Name), Resource.lblNotSet));
                    }
                    else
                    {
                        if ((!model.PreviousCroppings.HasGreaterThan30PercentClover.Value) && model.PreviousCroppings.SoilNitrogenSupplyItemID == null)
                        {
                            ModelState.AddModelError("PreviousCroppings.SoilNitrogenSupplyItemID", string.Format("{0} {1}", string.Format(Resource.lblHowMuchNitrogenHasBeenAppliedToFieldEachYear, model.Name), Resource.lblNotSet));
                        }
                    }
                }


                if (model.RecentSoilAnalysisQuestion.Value)
                {
                    if (model.IsSoilReleasingClay && !model.SoilReleasingClay.HasValue)
                    {
                        ModelState.AddModelError("SoilReleasingClay", Resource.MsgSoilReleasingClayNotSet);
                    }
                    if (!model.SoilAnalyses.Date.HasValue)
                    {
                        ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgSampleDateNotSet);
                    }
                    if (!model.SoilAnalyses.SulphurDeficient.HasValue)
                    {
                        ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.lblSoilDeficientInSulpurForCheckAnswerNotset);
                    }

                    if (model.IsSoilNutrientValueTypeIndex.HasValue)
                    {

                        if (!model.IsSoilNutrientValueTypeIndex.Value)
                        {
                            if (!model.SoilAnalyses.PH.HasValue && !model.SoilAnalyses.Potassium.HasValue &&
                                !model.SoilAnalyses.Phosphorus.HasValue && !model.SoilAnalyses.Magnesium.HasValue)
                            {
                                if (!model.SoilAnalyses.PH.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgPhNotSet);
                                }
                                if (!model.SoilAnalyses.Potassium.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.Potassium", Resource.MsgPotassiumNotSet);
                                }
                                if (!model.SoilAnalyses.Phosphorus.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.Phosphorus", Resource.MsgPhosphorusNotSet);
                                }
                                if (!model.SoilAnalyses.Magnesium.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.Magnesium", Resource.MsgMagnesiumNotSet);
                                }
                            }
                        }
                        else
                        {
                            if (!model.SoilAnalyses.PH.HasValue && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                                !model.SoilAnalyses.MagnesiumIndex.HasValue && !model.SoilAnalyses.PhosphorusIndex.HasValue)
                            {
                                if (!model.SoilAnalyses.PH.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgPhNotSet);
                                }
                                if (string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
                                {
                                    ModelState.AddModelError("PotassiumIndexValue", Resource.MsgPotassiumIndexNotSet);
                                }
                                if (!model.SoilAnalyses.PhosphorusIndex.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.PhosphorusIndex", Resource.MsgPhosphorusIndexNotSet);
                                }
                                if (!model.SoilAnalyses.MagnesiumIndex.HasValue)
                                {
                                    ModelState.AddModelError("SoilAnalyses.MagnesiumIndex", Resource.MsgMagnesiumIndexNotSet);
                                }
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgNutrientValueTypeForCheckAnswereNotSet);
                    }
                }

                if (model.TotalArea == null || model.TotalArea == 0)
                {
                    ModelState.AddModelError("TotalArea", Resource.MsgEnterTotalFieldArea);
                }
                if (model.TotalArea != null && model.TotalArea < 0)
                {
                    ModelState.AddModelError("TotalArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
                if (model.CroppedArea == null || model.CroppedArea == 0)
                {
                    ModelState.AddModelError("CroppedArea", Resource.MsgEnterTheCroppedArea);
                }
                if (model.CroppedArea != null && model.CroppedArea < 0)
                {
                    ModelState.AddModelError("CroppedArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }

                if (model.CroppedArea > model.TotalArea)
                {
                    ModelState.AddModelError("CroppedArea", Resource.MsgCroppedAreaIsGreaterThanTotalArea);
                }
                if (model.CroppedArea != null && model.CroppedArea < 0)
                {
                    ModelState.AddModelError("CroppedArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }

                if (model.ManureNonSpreadingArea > model.TotalArea)
                {
                    ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgManureNonSpreadingAreaIsGreaterThanTotalArea);
                }
                if (model.ManureNonSpreadingArea != null && model.ManureNonSpreadingArea < 0)
                {
                    ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
                if (!ModelState.IsValid)
                {
                    List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                    ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.PreviousCroppings.GrassManagementOptionID)?.Name;


                    List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                    ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.PreviousCroppings.SoilNitrogenSupplyItemID)?.Name;

                    return View(CheckAnswerActionName, model);
                }

                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                var farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);

                if (model.SoilAnalyses.Potassium != null || model.SoilAnalyses.Phosphorus != null || (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) || model.SoilAnalyses.PhosphorusIndex != null)
                {
                    model.PKBalance.PBalance = 0;
                    model.PKBalance.KBalance = 0;
                    model.PKBalance.Year = model.SoilAnalyses.Date.Value.Year;
                }
                else
                {
                    model.PKBalance = null;
                }

                int? lastGroupNumber = null;
                Error error = new Error();
                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

                if (farm != null && (string.IsNullOrWhiteSpace(error.Message)))
                {
                    (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.LastHarvestYear.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));

                    if (harvestYearPlanResponse != null && harvestYearPlanResponse.Count > 0)
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

                    if (lastGroupNumber != null)
                    {
                        model.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, (lastGroupNumber + 1));
                    }
                    else
                    {
                        model.CropGroupName = string.Format(Resource.lblCropGroupWithCounter, 1);
                    }
                }
                else
                {
                    TempData["AddFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                    return RedirectToAction(CheckAnswerActionName);
                }

                List<PreviousCroppingData> previousCropping = new List<PreviousCroppingData>();

                if (model.IsPreviousYearGrass == true && model.PreviousGrassYears != null)
                {
                    model.CropGroupId = (int)NMP.Commons.Enums.CropGroup.Grass;
                    model.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass;
                    foreach (var year in model.PreviousGrassYears)
                    {
                        model.PreviousCroppings.HarvestYear = year;

                        var newPreviousCropping = new PreviousCroppingData
                        {
                            CropGroupID = model.CropGroupId,
                            CropTypeID = model.CropTypeID,
                            HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear ?? false,
                            HarvestYear = year,
                            LayDuration = model.PreviousCroppings.LayDuration,
                            GrassManagementOptionID = model.PreviousCroppings.GrassManagementOptionID,
                            HasGreaterThan30PercentClover = model.PreviousCroppings.HasGreaterThan30PercentClover,
                            SoilNitrogenSupplyItemID = model.PreviousCroppings.SoilNitrogenSupplyItemID
                        };
                        previousCropping.Add(newPreviousCropping);
                    }

                    if (model.PreviousGrassYears.Count < 3)
                    {
                        if (!model.PreviousGrassYears.Any(x => x == model.LastHarvestYear - 1))
                        {
                            var newPreviousCropping = new PreviousCroppingData
                            {
                                CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                                CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                HarvestYear = model.LastHarvestYear - 1,
                                HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear,
                            };
                            previousCropping.Add(newPreviousCropping);
                        }
                        if (!model.PreviousGrassYears.Any(x => x == model.LastHarvestYear - 2))
                        {
                            var newPreviousCropping = new PreviousCroppingData
                            {
                                CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                                CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                HarvestYear = model.LastHarvestYear - 2,
                                HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear,
                            };
                            previousCropping.Add(newPreviousCropping);
                        }
                    }
                }
                else
                {
                    var newPreviousCropping = new PreviousCroppingData
                    {
                        CropGroupID = model.CropGroupId,
                        CropTypeID = model.CropTypeID,
                        HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear ?? false,
                        HarvestYear = model.LastHarvestYear,
                        LayDuration = null,
                        GrassManagementOptionID = null,
                        HasGreaterThan30PercentClover = null,
                        SoilNitrogenSupplyItemID = null
                    };
                    previousCropping.Add(newPreviousCropping);

                    if (model.PreviousGrassYears != null)
                    {
                        model.CropGroupId = (int)CropGroup.Grass;
                        model.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass;
                        foreach (var year in model.PreviousGrassYears)
                        {
                            model.PreviousCroppings.HarvestYear = year;

                            var newPreviousGass = new PreviousCroppingData
                            {
                                CropGroupID = model.CropGroupId,
                                CropTypeID = model.CropTypeID,
                                HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear ?? false,
                                HarvestYear = year,
                                LayDuration = model.PreviousCroppings.LayDuration,
                                GrassManagementOptionID = model.PreviousCroppings.GrassManagementOptionID,
                                HasGreaterThan30PercentClover = model.PreviousCroppings.HasGreaterThan30PercentClover,
                                SoilNitrogenSupplyItemID = model.PreviousCroppings.SoilNitrogenSupplyItemID
                            };
                            previousCropping.Add(newPreviousGass);
                        }
                        if (model.PreviousGrassYears.Count < 3)
                        {
                            if (!model.PreviousGrassYears.Any(x => x == model.LastHarvestYear - 1))
                            {
                                newPreviousCropping = new PreviousCroppingData
                                {
                                    CropGroupID = (int)CropGroup.Other,
                                    CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                    HarvestYear = model.LastHarvestYear - 1,
                                    HasGrassInLastThreeYear = true
                                };
                                previousCropping.Add(newPreviousCropping);
                            }
                            if (!model.PreviousGrassYears.Any(x => x == model.LastHarvestYear - 2))
                            {
                                newPreviousCropping = new PreviousCroppingData
                                {
                                    CropGroupID = (int)CropGroup.Other,
                                    CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                    HarvestYear = model.LastHarvestYear - 2,
                                    HasGrassInLastThreeYear = true
                                };
                                previousCropping.Add(newPreviousCropping);
                            }
                        }
                    }
                    else
                    {
                        newPreviousCropping = new PreviousCroppingData
                        {
                            CropGroupID = (int)CropGroup.Other,
                            CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                            HarvestYear = model.LastHarvestYear - 1,
                            HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear ?? false,
                        };
                        previousCropping.Add(newPreviousCropping);

                        newPreviousCropping = new PreviousCroppingData
                        {
                            CropGroupID = (int)CropGroup.Other,
                            CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                            HarvestYear = model.LastHarvestYear - 2,
                            HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear ?? false,
                        };
                        previousCropping.Add(newPreviousCropping);
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
                {
                    if (model.PotassiumIndexValue == Resource.lblTwoMinus)
                    {
                        model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(Resource.lblMinusTwo);
                    }
                    else if (model.PotassiumIndexValue == Resource.lblTwoPlus)
                    {
                        model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(Resource.lblPlusTwo);
                    }
                    else
                    {
                        model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(model.PotassiumIndexValue.Trim());
                    }
                }

                SoilAnalysis? soilAnalysis = null;
                if (model.RecentSoilAnalysisQuestion.HasValue && model.RecentSoilAnalysisQuestion.Value)
                {
                    soilAnalysis = new SoilAnalysis()
                    {
                        Year = model.SoilAnalyses.Date.Value.Month >= 8 ? model.SoilAnalyses.Date.Value.Year + 1 : model.SoilAnalyses.Date.Value.Year,
                        SulphurDeficient = model.SoilAnalyses.SulphurDeficient,
                        Date = model.SoilAnalyses.Date,
                        PH = model.SoilAnalyses.PH,
                        PhosphorusMethodologyID = model.SoilAnalyses.PhosphorusMethodologyID,
                        Phosphorus = model.SoilAnalyses.Phosphorus,
                        PhosphorusIndex = model.SoilAnalyses.PhosphorusIndex,
                        Potassium = model.SoilAnalyses.Potassium,
                        PotassiumIndex = model.SoilAnalyses.PotassiumIndex,
                        Magnesium = model.SoilAnalyses.Magnesium,
                        MagnesiumIndex = model.SoilAnalyses.MagnesiumIndex,
                        SoilNitrogenSupply = model.SoilAnalyses.SoilNitrogenSupply,
                        SoilNitrogenSupplyIndex = model.SoilAnalyses.SoilNitrogenSupplyIndex,
                        SoilNitrogenSampleDate = model.SampleForSoilMineralNitrogen,
                        Sodium = model.SoilAnalyses.Sodium,
                        Lime = model.SoilAnalyses.Lime,
                        PhosphorusStatus = model.SoilAnalyses.PhosphorusStatus,
                        PotassiumAnalysis = model.SoilAnalyses.PotassiumAnalysis,
                        PotassiumStatus = model.SoilAnalyses.PotassiumStatus,
                        MagnesiumAnalysis = model.SoilAnalyses.MagnesiumAnalysis,
                        MagnesiumStatus = model.SoilAnalyses.MagnesiumStatus,
                        NitrogenResidueGroup = model.SoilAnalyses.NitrogenResidueGroup,
                        Comments = model.SoilAnalyses.Comments,
                        PreviousID = model.SoilAnalyses.PreviousID,
                        CreatedOn = DateTime.Now,
                        CreatedByID = userId,
                        ModifiedOn = model.SoilAnalyses.ModifiedOn,
                        ModifiedByID = model.SoilAnalyses.ModifiedByID
                    };
                }

                FieldData fieldData = new FieldData
                {
                    Field = new Field
                    {
                        //ID= model.ID,
                        SoilTypeID = model.SoilTypeID,
                        NVZProgrammeID = model.IsWithinNVZ == true ? (int)NMP.Commons.Enums.NvzProgram.CurrentNVZRule : (int)NMP.Commons.Enums.NvzProgram.NotInNVZ,
                        Name = model.Name,
                        LPIDNumber = model.LPIDNumber,
                        NationalGridReference = model.NationalGridReference,
                        OtherReference = model.OtherReference,
                        TotalArea = model.TotalArea,
                        CroppedArea = model.CroppedArea,
                        ManureNonSpreadingArea = model.ManureNonSpreadingArea,
                        SoilReleasingClay = model.SoilReleasingClay,
                        SoilOverChalk = model.SoilOverChalk,
                        IsWithinNVZ = model.IsWithinNVZ,
                        IsAbove300SeaLevel = model.IsAbove300SeaLevel,
                        IsActive = true,
                        CreatedOn = DateTime.Now,
                        CreatedByID = userId,
                        ModifiedOn = model.ModifiedOn,
                        ModifiedByID = model.ModifiedByID
                    },
                    SoilAnalysis = soilAnalysis,
                    PKBalance = model.PKBalance != null ? model.PKBalance : null,
                    PreviousCroppings = previousCropping
                };

                (Field fieldResponse, Error error1) = await _fieldService.AddFieldAsync(fieldData, farm.ID, farm.Name);
                if (error1.Message == null && fieldResponse != null)
                {
                    string success = _farmDataProtector.Protect("true");
                    string fieldName = _farmDataProtector.Protect(fieldResponse.Name);
                    RemoveFieldDataFromSession();
                    return RedirectToAction("ManageFarmFields", new { id = model.EncryptedFarmId, q = success, name = fieldName });
                }
                else
                {
                    TempData["AddFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                    return RedirectToAction(CheckAnswerActionName);
                }
            }
            catch (Exception ex)
            {
                TempData["AddFieldError"] = ex.Message;
                return RedirectToAction(CheckAnswerActionName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageFarmFields(string id, string? q, string? name, string? isDeleted)
        {
            _logger.LogTrace($"Field Controller : ManageFarmFields() action called");
            FarmFieldsViewModel model = new FarmFieldsViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Success = false;
            }
            if (!string.IsNullOrWhiteSpace(isDeleted))
            {
                ViewBag.FieldName = _fieldDataProtector.Unprotect(name);
                ViewBag.IsDeleted = true;
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                model.Fields = await _fieldService.FetchFieldsByFarmId(farmId);

                if (model.Fields != null && model.Fields.Count > 0)
                {
                    model.Fields.ForEach(x => x.EncryptedFieldId = _fieldDataProtector.Protect(x.ID.ToString()));
                }
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                model.FarmName = farm.Name;
                if (string.IsNullOrWhiteSpace(isDeleted) && name != null)
                {
                    model.FieldName = _farmDataProtector.Unprotect(name);
                }

                ViewBag.FieldsList = model.Fields;
                model.EncryptedFarmId = id;
            }

            RemoveFieldDataFromSession();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManageFarmFields(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : ManageFarmFields() post action called");
            return RedirectToAction("ManageFarmFields");
        }



        [HttpGet]
        public async Task<IActionResult> FieldSoilAnalysisDetail(string farmId, string fieldId, string? q, string? r, string? s, string? t)//id encryptedFieldId,farmID=EncryptedFarmID,q=success,r=FiedlOrSoilAnalysis,s=soilUpdateOrSave
        {
            _logger.LogTrace($"Field Controller : FieldSoilAnalysisDetail() action called");
            if (HttpContext.Session.Exists("SoilAnalysisDataBeforeUpdate"))
            {
                HttpContext.Session.Remove("SoilAnalysisDataBeforeUpdate");
            }

            if (HttpContext.Session.Exists("FieldDataBeforeUpdate"))
            {
                HttpContext.Session.Remove("FieldDataBeforeUpdate");
            }

            FieldViewModel model = new FieldViewModel();
            Error error = new Error();
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(farmId)));
            int decryptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(fieldId));
            var field = await _fieldService.FetchFieldByFieldId(decryptedFieldId);
            List<Crop> cropPlans = await _cropService.FetchCropsByFieldId(decryptedFieldId);
            List<PreviousCroppingData> prevCroppings = new List<PreviousCroppingData>();
            if (!cropPlans.Any())
            {
                (prevCroppings, error) = await _previousCroppingService.FetchDataByFieldId(decryptedFieldId, null);
                if (string.IsNullOrWhiteSpace(error.Message) && prevCroppings.Count > 0)
                {
                    model.LastHarvestYear = prevCroppings.Max(p => p.HarvestYear);
                }
            }
            int oldestYearWithPlan = cropPlans.Any() ? cropPlans.Min(cp => cp.Year) : (model.LastHarvestYear ?? 0) + 1;
            model.LastHarvestYear = oldestYearWithPlan - 1;
            (prevCroppings, error) = await _previousCroppingService.FetchDataByFieldId(decryptedFieldId, oldestYearWithPlan);

            if (string.IsNullOrWhiteSpace(error.Message))
            {
                List<int> previousYears = new List<int>();

                List<PreviousCroppingData> grassCroppings = prevCroppings.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).ToList();
                foreach (var item in grassCroppings)
                {
                    previousYears.Add(item.HarvestYear ?? 0);
                }

                model.PreviousGrassYears = previousYears;

                List<PreviousCroppingData> previousCroppingsExcludePlan = prevCroppings.Where(pc => !cropPlans.Any(cp => cp.Year == pc.HarvestYear)).ToList();

                var tasks = previousCroppingsExcludePlan.Select(async pc => new
                {
                    pc.ID,
                    pc.FieldID,
                    pc.CropGroupID,
                    pc.CropTypeID,
                    pc.HasGrassInLastThreeYear,
                    pc.HarvestYear,
                    pc.LayDuration,
                    pc.GrassManagementOptionID,
                    pc.HasGreaterThan30PercentClover,
                    pc.SoilNitrogenSupplyItemID,
                    pc.CreatedOn,
                    pc.CreatedByID,
                    pc.ModifiedOn,
                    pc.ModifiedByID,
                    CropTypeName = await _fieldService.FetchCropTypeById(pc.CropTypeID ?? 0)
                }).ToList();

                ViewBag.PreviousCroppingsList = (await Task.WhenAll(tasks)).OrderByDescending(x => x.HarvestYear).ToList();

                if (tasks != null && tasks.Count > 0)
                {
                    var completedTasks = (await Task.WhenAll(tasks)).OrderByDescending(x => x.HarvestYear).ToList();
                    var hasGrass = completedTasks.Any(t => t.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass);
                    int maxYear = completedTasks.Where(x => x.HarvestYear.HasValue).Max(x => x.HarvestYear.Value);
                    if (hasGrass)
                    {
                        ViewBag.PreviousCroppingsList = completedTasks.Where(x => x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass || x.HarvestYear == maxYear).ToList();
                    }
                    else
                    {

                        ViewBag.PreviousCroppingsList = completedTasks.Where(x => x.HarvestYear.HasValue && x.HarvestYear.Value == maxYear).ToList();
                    }
                }

                bool? hasGrassInLastThreeYear = null;

                if (grassCroppings.Count > 0)
                {
                    //grass
                    model.IsPreviousYearGrass = grassCroppings.Any(x => x.HarvestYear == model.LastHarvestYear);
                    model.PreviousCroppings = grassCroppings[0];
                    hasGrassInLastThreeYear = true;
                }
                else
                {
                    //arable
                    model.IsPreviousYearGrass = false;
                    hasGrassInLastThreeYear = false;
                    if (model.PreviousCroppingsList.Count > 0)
                    {
                        model.PreviousCroppings.HasGrassInLastThreeYear = false;
                    }
                    else
                    {
                        model.PreviousCroppings.HasGrassInLastThreeYear = null;
                    }
                }
                model.CropGroupId = prevCroppings.FirstOrDefault(x => x.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)?.CropGroupID;
                model.CropTypeID = prevCroppings.FirstOrDefault(x => x.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass)?.CropTypeID;

                if (model.CropGroupId != null && model.CropTypeID != null)
                {
                    model.CropGroup = await _fieldService.FetchCropGroupById(model.CropGroupId.Value);
                    model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                }
                ViewBag.HasGrassInLastThreeYear = hasGrassInLastThreeYear;
                if (hasGrassInLastThreeYear == true)
                {
                    List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                    ViewBag.GrassManagementOption = grassManagements?.FirstOrDefault(x => x.Id == prevCroppings
                             .Where(pc => pc.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                             .Select(pc => pc.GrassManagementOptionID)
                             .FirstOrDefault())?.Name;


                    List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                    ViewBag.SoilNitrogenSupplyItem = soilNitrogenSupplyItems?.FirstOrDefault(x =>
                          x.Id == prevCroppings
                            .Where(pc => pc.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                            .Select(pc => pc.SoilNitrogenSupplyItemID)
                            .FirstOrDefault())?.Name;
                }

            }


            model.Name = field.Name;
            model.TotalArea = field.TotalArea ?? 0;
            model.CroppedArea = field.CroppedArea ?? 0;
            model.ManureNonSpreadingArea = field.ManureNonSpreadingArea ?? 0;
            model.SoilReleasingClay = field.SoilReleasingClay ?? false;
            model.IsWithinNVZ = field.IsWithinNVZ ?? false;
            model.IsAbove300SeaLevel = field.IsAbove300SeaLevel ?? false;
            if (!string.IsNullOrWhiteSpace(t))
            {
                model.HarvestYear = Convert.ToInt32(_farmDataProtector.Unprotect(t));
                model.EncryptedHarvestYear = t;
            }
            else
            {
                model.HarvestYear = null;
                model.EncryptedHarvestYear = null;
            }
            model.EncryptedFieldId = fieldId;
            model.ID = decryptedFieldId;
            model.isEnglishRules = farm.EnglishRules;
            model.SoilOverChalk = field.SoilOverChalk;
            if (farm != null)
            {
                model.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
                model.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
            }
            else
            {
                model.IsWithinNVZForFarm = false;
                model.IsAbove300SeaLevelForFarm = false;
            }
            List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
            if (soilTypes != null && soilTypes.Count > 0)
            {
                SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);
                model.SoilType = !string.IsNullOrWhiteSpace(soilType.SoilType) ? soilType.SoilType : string.Empty;
                model.SoilTypeID = field.SoilTypeID;
                if (soilType != null && soilType.KReleasingClay)
                {
                    ViewBag.IsSoilReleasingClay = true;
                }
                else
                {
                    ViewBag.IsSoilReleasingClay = false;
                }
                if (model.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                {
                    ViewBag.IsSoilOverChalk = true;
                }
                else
                {
                    ViewBag.IsSoilOverChalk = false;
                }
            }
            model.EncryptedFarmId = farmId;
            model.FarmName = farm.Name;
            List<SoilAnalysisResponse> soilAnalysisResponse = (await _fieldService.FetchSoilAnalysisByFieldId(decryptedFieldId, Resource.lblFalse)).OrderByDescending(x => x.CreatedOn).ToList();
            if (soilAnalysisResponse != null && soilAnalysisResponse.Count > 0)
            {
                soilAnalysisResponse.ForEach(m => m.EncryptedSoilAnalysisId = _fieldDataProtector.Protect(m.ID.ToString()));
                ViewBag.SoilAnalysisList = soilAnalysisResponse;
            }
            if (!string.IsNullOrWhiteSpace(q))
            {

                if (!string.IsNullOrWhiteSpace(r))
                {
                    string statusFor = _fieldDataProtector.Unprotect(r);
                    if (!string.IsNullOrWhiteSpace(statusFor))
                    {
                        if (statusFor == Resource.lblField)
                        {
                            ViewBag.Success = Resource.lblTrue;
                            ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveUpdated, model.Name);
                            ViewBag.SuccessMsgContentLink = Resource.MsgViewYourFarmDetails;
                        }
                        else if (statusFor == Resource.lblSoilAnalysis)
                        {
                            if (_soilAnalysisDataProtector.Unprotect(q) == Resource.lblFalse)
                            {
                                ViewBag.Success = Resource.lblFalse;
                                ViewBag.Error = Resource.MsgSoilAnalysisCouldNotAdded;
                            }
                            else
                            {
                                ViewBag.Success = Resource.lblTrue;
                                if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                {
                                    ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveAddedANewSoilAnalysisForFieldName, model.Name);
                                }
                                else if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblUpdate)
                                {
                                    ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveUpdatedASoilAnalysisForFieldName, model.Name);
                                }
                                else if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblRemove)
                                {
                                    ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveRemovedASoilAnalysisForFieldName, model.Name);
                                }
                                List<Crop> crop = (await _cropService.FetchCropsByFieldId(model.ID.Value)).ToList();
                                if (crop != null && crop.Count > 0)
                                {
                                    if (soilAnalysisResponse.Count > 0)
                                    {
                                        bool anyPlan = crop.Any(x => x.Year >= (soilAnalysisResponse.FirstOrDefault()?.Year ?? 0));
                                        if (anyPlan)
                                        {
                                            int cropYear = crop.FirstOrDefault(x => x.Year >= soilAnalysisResponse.FirstOrDefault().Year).Year;
                                            if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                            {
                                                ViewBag.SuccessMsgAdditionalContent = Resource.lblNutrientRecommendationsWillBeBasedOnTheLatest;
                                            }
                                            else
                                            {
                                                ViewBag.SuccessMsgAdditionalContent = string.Format(Resource.lblThisMayChangeYourNutrientRecommendations);
                                            }

                                            ViewBag.CropYear = _farmDataProtector.Protect(cropYear.ToString());
                                            if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                            {
                                                ViewBag.SuccessMsgAdditionalContentSecondForAdd = Resource.lblCheckYourCropPlans;
                                                ViewBag.SuccessMsgAdditionalContentThird = Resource.lblToSeeYourRecommendations;
                                            }
                                            else
                                            {
                                                ViewBag.SuccessMsgAdditionalContentSecondForUpdate = string.Format(Resource.lblCropPlan);
                                                ViewBag.SuccessMsgAdditionalContentThird = Resource.lblToSeeItsRecommendations;
                                            }

                                        }
                                    }
                                    else if (!string.IsNullOrWhiteSpace(s) && (_soilAnalysisDataProtector.Unprotect(s) == Resource.lblUpdate || _soilAnalysisDataProtector.Unprotect(s) == Resource.lblRemove))
                                    {
                                        ViewBag.SuccessMsgAdditionalContent = string.Format(Resource.lblThisMayChangeYourNutrientRecommendations);
                                        ViewBag.SuccessMsgAdditionalContentSecondForUpdate = string.Format(Resource.lblCropPlan);
                                        ViewBag.SuccessMsgAdditionalContentThird = Resource.lblToSeeItsRecommendations;
                                    }

                                }
                            }
                        }

                    }
                }

            }
            else
            {
                ViewBag.Success = null;
            }

            SetFieldDataToSession(model);

            return View(model);
        }

        [HttpGet]
        public IActionResult RecentSoilAnalysisQuestion()
        {
            _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() action called");
            FieldViewModel? model = LoadFieldDataFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() action : No field data found in session.");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in RecentSoilAnalysisQuestion() action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilType");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecentSoilAnalysisQuestion(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() post action called");
            if (model.RecentSoilAnalysisQuestion == null)
            {
                ModelState.AddModelError("RecentSoilAnalysisQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.IsCheckAnswer)
                {
                    FieldViewModel? fieldData = LoadFieldDataFromSession();
                    if (fieldData == null)
                    {
                        _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() post action : No field data found in session.");
                        return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                    }

                    if (fieldData.RecentSoilAnalysisQuestion != model.RecentSoilAnalysisQuestion)
                    {
                        model.IsRecentSoilAnalysisQuestionChange = true;
                    }
                }

                SetFieldDataToSession(model);

                if (model.RecentSoilAnalysisQuestion.HasValue && model.RecentSoilAnalysisQuestion.Value)
                {
                    return RedirectToAction("SulphurDeficient");
                }
                else
                {
                    model.SoilAnalyses.SulphurDeficient = null;
                    model.SoilAnalyses.Date = null;
                    model.SoilAnalyses.PH = null;
                    model.SoilAnalyses.Phosphorus = null;
                    model.SoilAnalyses.Magnesium = null;
                    model.SoilAnalyses.Potassium = null;
                    model.SoilAnalyses.PotassiumIndex = null;
                    model.SoilAnalyses.MagnesiumIndex = null;
                    model.SoilAnalyses.PhosphorusIndex = null;
                    model.IsSoilNutrientValueTypeIndex = null;
                    SetFieldDataToSession(model);

                    if (model.IsCheckAnswer)
                    {
                        return RedirectToAction(CheckAnswerActionName);
                    }
                    return RedirectToAction("LastHarvestYear");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex,"Field Controller : Exception in RecentSoilAnalysisQuestion() post action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SoilOverChalk()
        {
            _logger.LogTrace($"Field Controller : SoilOverChalk() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace($"Field Controller : SoilOverChalk() action : No field data found in session.");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilOverChalk(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : SoilOverChalk() post action called");
            if (field.SoilOverChalk == null)
            {
                ModelState.AddModelError("SoilOverChalk", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer && (!field.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction("RecentSoilAnalysisQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateField(string? fieldId, string? farmId)
        {
            _logger.LogTrace($"Field Controller : UpdateField() action called");
            FieldViewModel model = new FieldViewModel();

            try
            {
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();

                if (!string.IsNullOrWhiteSpace(fieldId))
                {
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(farmId)));
                    int decrptedFieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(fieldId));
                    var field = await _fieldService.FetchFieldByFieldId(decrptedFieldId);
                    //get plans of field
                    List<Crop> cropPlans = await _cropService.FetchCropsByFieldId(decrptedFieldId);
                    //get oldest plan
                    List<PreviousCroppingData> prevCroppings = new List<PreviousCroppingData>();
                    if (!cropPlans.Any())
                    {
                        (prevCroppings, error) = await _previousCroppingService.FetchDataByFieldId(decrptedFieldId, null);
                        if (string.IsNullOrWhiteSpace(error.Message) && prevCroppings.Count > 0)
                        {
                            model.LastHarvestYear = prevCroppings.Max(p => p.HarvestYear);
                        }
                    }
                    int oldestYearWithPlan = cropPlans.Any() ? cropPlans.Min(cp => cp.Year) : (model.LastHarvestYear ?? 0) + 1;// farm.LastHarvestYear to model.LastHarvestYear

                    //fetch previous cropping data and extract 3 from this and assing into model.PreviousCroppingsList
                    (prevCroppings, error) = await _previousCroppingService.FetchDataByFieldId(decrptedFieldId, oldestYearWithPlan);

                    prevCroppings = prevCroppings.Where(x => x.HarvestYear < oldestYearWithPlan).ToList();
                    model.PreviousCroppingsList = prevCroppings;
                    //get previous grasses which harvest year is less than oldest plan.
                    List<PreviousCroppingData> grassCroppings = prevCroppings.Where(x => x.HarvestYear < oldestYearWithPlan && x.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass).ToList();
                    model.PreviousGrassYears = new List<int>();
                    foreach (var item in grassCroppings)
                    {
                        model.PreviousGrassYears.Add(item.HarvestYear ?? 0);
                    }

                    //update last harvest year 
                    model.LastHarvestYear = oldestYearWithPlan - 1;

                    bool? hasGrassInLastThreeYear = null;
                    if (grassCroppings.Count > 0)
                    {
                        //grass
                        model.IsPreviousYearGrass = grassCroppings.Any(x => x.HarvestYear == model.LastHarvestYear) ? true : false;

                        model.PreviousCroppings = grassCroppings.FirstOrDefault();

                        hasGrassInLastThreeYear = true;

                    }
                    else
                    {
                        //arable
                        model.IsPreviousYearGrass = false;
                        hasGrassInLastThreeYear = false;
                        if (model.PreviousCroppingsList.Count > 0)
                        {
                            model.PreviousCroppings.HasGrassInLastThreeYear = false;
                        }
                        else
                        {
                            model.PreviousCroppings.HasGrassInLastThreeYear = null;
                        }
                    }

                    model.CropGroupId = prevCroppings.FirstOrDefault(x => x.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass && x.HarvestYear == model.LastHarvestYear)?.CropGroupID;
                    model.CropTypeID = prevCroppings.FirstOrDefault(x => x.CropTypeID != (int)NMP.Commons.Enums.CropTypes.Grass && x.HarvestYear == model.LastHarvestYear)?.CropTypeID;
                    if (model.CropGroupId != null && model.CropTypeID != null)
                    {
                        model.CropGroup = await _fieldService.FetchCropGroupById(model.CropGroupId.Value);
                        model.CropType = await _fieldService.FetchCropTypeById(model.CropTypeID.Value);
                    }

                    if (hasGrassInLastThreeYear == true)
                    {
                        ViewBag.GrassManagementOption = grassManagements?.FirstOrDefault(x => x.Id == prevCroppings
                          .Where(pc => pc.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                          .Select(pc => pc.GrassManagementOptionID)
                          .FirstOrDefault())?.Name;

                        ViewBag.SoilNitrogenSupplyItem = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == prevCroppings
                          .Where(pc => pc.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                          .Select(pc => pc.SoilNitrogenSupplyItemID)
                          .FirstOrDefault())?.Name;
                    }



                    model.Name = field.Name;
                    model.TotalArea = field.TotalArea ?? 0;
                    model.CroppedArea = field.CroppedArea ?? 0;
                    model.ManureNonSpreadingArea = field.ManureNonSpreadingArea ?? 0;
                    model.SoilReleasingClay = field.SoilReleasingClay ?? false;
                    model.IsWithinNVZ = field.IsWithinNVZ ?? false;
                    model.IsAbove300SeaLevel = field.IsAbove300SeaLevel ?? false;
                    var soilType = await _fieldService.FetchSoilTypeById(field.SoilTypeID.Value);
                    model.SoilType = !string.IsNullOrWhiteSpace(soilType) ? soilType : string.Empty;
                    model.SoilTypeID = field.SoilTypeID;
                    model.EncryptedFieldId = fieldId;
                    model.ID = decrptedFieldId;
                    model.isEnglishRules = farm.EnglishRules;
                    model.SoilOverChalk = field.SoilOverChalk;

                    model.EncryptedFarmId = farmId;
                    model.FarmName = farm.Name;
                    if (farm != null)
                    {
                        model.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
                        model.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Commons.Enums.NvzFields.SomeFieldsInNVZ ? true : false;
                    }
                    else
                    {
                        model.IsWithinNVZForFarm = false;
                        model.IsAbove300SeaLevelForFarm = false;
                    }
                    bool isUpdateField = true;
                    model.EncryptedIsUpdate = _fieldDataProtector.Protect(isUpdateField.ToString());
                    if (model.SoilOverChalk != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                    {
                        model.SoilOverChalk = null;
                    }
                    if (model.SoilReleasingClay != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.DeepClayey)
                    {
                        model.SoilReleasingClay = null;
                        model.IsSoilReleasingClay = false;
                    }

                    SetFieldDataToSession(model);
                }
                else
                {
                    model = LoadFieldDataFromSession();

                    if (model == null)
                    {
                        _logger.LogTrace($"Field Controller : UpdateField() post action : No field data found in session.");
                        return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                    }

                    if (model != null)
                    {
                        if (model.PreviousCroppings.GrassManagementOptionID != null)
                        {
                            ViewBag.GrassManagementOption = grassManagements?.FirstOrDefault(x => x.Id == model.PreviousCroppings.GrassManagementOptionID)?.Name;

                        }
                        if (model.PreviousCroppings.SoilNitrogenSupplyItemID != null)
                        {
                            ViewBag.SoilNitrogenSupplyItem = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.PreviousCroppings.SoilNitrogenSupplyItemID)?.Name;
                        }
                        if (model.PreviousGrassYears == null)
                        {
                            model.PreviousGrassYears = new List<int>();
                        }
                        if (model.PreviousCroppingsList != null && model.PreviousGrassYears != null)
                        {
                            //if PreviousGrassYears does not contains last harvest year means last harvest year is arable
                            if (!model.PreviousGrassYears.Contains(model.LastHarvestYear ?? 0))
                            {
                                var existing = model.PreviousCroppingsList.FirstOrDefault(pc => pc.HarvestYear == model.LastHarvestYear);
                                if (existing != null)
                                {
                                    existing.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId));
                                    existing.CropGroupID = model.CropGroupId;
                                    existing.CropTypeID = model.CropTypeID;
                                    existing.HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear;
                                    existing.LayDuration = null;
                                    existing.GrassManagementOptionID = null;
                                    existing.HasGreaterThan30PercentClover = null;
                                    existing.SoilNitrogenSupplyItemID = null;
                                    existing.Action = existing.Action == null ? (int)NMP.Commons.Enums.Action.Update : existing.Action;
                                }
                                else
                                {
                                    // Add new record if not present
                                    model.PreviousCroppingsList.Add(new PreviousCroppingData
                                    {
                                        FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId)),
                                        CropGroupID = model.CropGroupId,
                                        CropTypeID = model.CropTypeID,
                                        HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear,
                                        HarvestYear = model.LastHarvestYear,
                                        LayDuration = null,
                                        GrassManagementOptionID = null,
                                        HasGreaterThan30PercentClover = null,
                                        SoilNitrogenSupplyItemID = null,
                                        Action = (int)NMP.Commons.Enums.Action.Insert,

                                    });
                                }
                            }
                            // 1️. Add or update
                            foreach (var year in model.PreviousGrassYears)
                            {
                                var existing = model.PreviousCroppingsList.FirstOrDefault(pc => pc.HarvestYear == year);
                                if (existing != null)
                                {
                                    existing.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId));
                                    existing.CropGroupID = (int)NMP.Commons.Enums.CropGroup.Grass;
                                    existing.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass;
                                    existing.HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear;
                                    existing.LayDuration = model.PreviousCroppings.LayDuration;
                                    existing.GrassManagementOptionID = model.PreviousCroppings.GrassManagementOptionID;
                                    existing.HasGreaterThan30PercentClover = model.PreviousCroppings.HasGreaterThan30PercentClover;
                                    existing.SoilNitrogenSupplyItemID = model.PreviousCroppings.SoilNitrogenSupplyItemID;
                                    existing.Action = existing.Action == null ? (int)NMP.Commons.Enums.Action.Update : existing.Action;

                                }
                                else
                                {
                                    // Add new record if not present
                                    model.PreviousCroppingsList.Add(new PreviousCroppingData
                                    {
                                        FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId)),
                                        CropGroupID = (int)NMP.Commons.Enums.CropGroup.Grass,
                                        CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass,
                                        HasGrassInLastThreeYear = model.PreviousCroppings.HasGrassInLastThreeYear,
                                        HarvestYear = year,
                                        LayDuration = model.PreviousCroppings.LayDuration,
                                        GrassManagementOptionID = model.PreviousCroppings.GrassManagementOptionID,
                                        HasGreaterThan30PercentClover = model.PreviousCroppings.HasGreaterThan30PercentClover,
                                        SoilNitrogenSupplyItemID = model.PreviousCroppings.SoilNitrogenSupplyItemID,
                                        Action = (int)NMP.Commons.Enums.Action.Insert,

                                    });
                                }
                            }

                            // 2️. Update Arable/grass to other crop if not exist in PreviousGrassYears
                            foreach (var pc in model.PreviousCroppingsList)
                            {
                                if (!model.PreviousGrassYears.Contains(pc.HarvestYear ?? 0) && pc.HarvestYear != model.LastHarvestYear)
                                {
                                    var existing = model.PreviousCroppingsList.FirstOrDefault(pcl => pcl.HarvestYear == pc.HarvestYear);
                                    pc.Action = existing != null ? (int)NMP.Commons.Enums.Action.Update : (int)NMP.Commons.Enums.Action.Insert;
                                    pc.CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other;
                                    pc.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other;
                                    existing.LayDuration = null;
                                    existing.GrassManagementOptionID = null;
                                    existing.HasGreaterThan30PercentClover = null;
                                    existing.SoilNitrogenSupplyItemID = null;
                                }
                            }
                        }


                        if (model.SoilOverChalk != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                        {
                            model.SoilOverChalk = null;
                        }
                        if (model.SoilReleasingClay != null && model.SoilTypeID != (int)NMP.Commons.Enums.SoilTypeEngland.DeepClayey)
                        {
                            model.SoilReleasingClay = null;
                            model.IsSoilReleasingClay = false;
                        }

                        SetFieldDataToSession(model);
                    }
                }

                if (!string.IsNullOrWhiteSpace(fieldId))
                {
                    HttpContext.Session.SetObjectAsJson("FieldDataBeforeUpdate", model);
                }

                var previousModel = HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldDataBeforeUpdate");

                bool isDataChanged = false;
                string action = "Action";

                if (previousModel != null)
                {
                    var oldJson = JObject.FromObject(previousModel);
                    var newJson = JObject.FromObject(model);

                    (oldJson["PreviousCroppings"] as JObject)?
                        .Property(action)?
                        .Remove();

                    (newJson["PreviousCroppings"] as JObject)?
                        .Property(action)?
                        .Remove();

                    oldJson["PreviousCroppingsList"]?.Children<JObject>().Select(x => x.Property(action))
                    .Where(p => p != null).ToList().ForEach(p => p!.Remove());

                    newJson["PreviousCroppingsList"]?.Children<JObject>().Select(x => x.Property(action))
                        .Where(p => p != null).ToList().ForEach(p => p!.Remove());

                    isDataChanged = !JToken.DeepEquals(oldJson, newJson);
                }
                ViewBag.IsDataChange = isDataChanged;


            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
            return View(model);

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : UpdateField() post action called");
            try
            {
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                FieldData fieldData = new FieldData
                {
                    Field = new Field
                    {
                        SoilTypeID = model.SoilTypeID,
                        NVZProgrammeID = model.IsWithinNVZ == true ? (int)NMP.Commons.Enums.NvzProgram.CurrentNVZRule : (int)NMP.Commons.Enums.NvzProgram.NotInNVZ,
                        Name = model.Name,
                        LPIDNumber = model.LPIDNumber,
                        NationalGridReference = model.NationalGridReference,
                        OtherReference = model.OtherReference,
                        TotalArea = model.TotalArea,
                        CroppedArea = model.CroppedArea,
                        ManureNonSpreadingArea = model.ManureNonSpreadingArea,
                        SoilReleasingClay = model.SoilReleasingClay,
                        SoilOverChalk = model.SoilOverChalk,
                        IsWithinNVZ = model.IsWithinNVZ,
                        IsAbove300SeaLevel = model.IsAbove300SeaLevel,
                        IsActive = true,
                        CreatedOn = model.CreatedOn,
                        CreatedByID = model.CreatedByID,
                        ModifiedOn = DateTime.Now,
                        ModifiedByID = userId
                    },
                    PreviousCroppings = model.PreviousCroppingsList,
                };

                int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId));
                (Field fieldResponse, Error error1) = await _fieldService.UpdateFieldAsync(fieldData, fieldId);
                if (error1.Message == null && fieldResponse != null)
                {
                    string success = _farmDataProtector.Protect(Resource.lblTrue);
                    string fieldName = _farmDataProtector.Protect(fieldResponse.Name);
                    RemoveFieldDataFromSession();
                    return RedirectToAction("FieldSoilAnalysisDetail", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId, q = success, r = _fieldDataProtector.Protect(Resource.lblField) });
                }
                else
                {
                    TempData["UpdateFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                    return RedirectToAction(UpdateFieldActionName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(UpdateFieldActionName);
            }
        }

        [HttpGet]
        public IActionResult FieldRemove()
        {
            _logger.LogTrace($"Field Controller : FieldRemove() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace($"Field Controller : FieldRemove() action : No field data found in session.");
                return RedirectToAction("ManageFarmFields", "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldRemove(FieldViewModel field)
        {
            _logger.LogTrace("Field Controller : FieldRemove() post action called");
            if (field.FieldRemove == null)
            {
                ModelState.AddModelError("FieldRemove", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View("FieldRemove", field);
            }

            if (field.FieldRemove.HasValue && !field.FieldRemove.Value)
            {
                return RedirectToAction("FieldSoilAnalysisDetail", new { farmId = field.EncryptedFarmId, fieldId = field.EncryptedFieldId });
            }
            else
            {
                int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(field.EncryptedFieldId));
                (string message, Error error) = await _fieldService.DeleteFieldByIdAsync(fieldId);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.DeleteFieldError = error.Message;
                    return View(field);
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    string isDeleted = _fieldDataProtector.Protect("true");
                    string name = _fieldDataProtector.Protect(field.Name);
                    RemoveFieldDataFromSession();
                    return RedirectToAction("ManageFarmFields", new { id = field.EncryptedFarmId, name = name, isDeleted = isDeleted });
                }
            }

            return View(field);
        }

        [HttpGet]
        public IActionResult CopyExistingField(string q)
        {
            _logger.LogTrace($"Field Controller : CopyExistingField() action called");
            FieldViewModel model = LoadFieldDataFromSession() ?? new FieldViewModel();
            if (string.IsNullOrWhiteSpace(q))
            {
                _logger.LogTrace("Field Controller : farm id not found in query string");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.BadRequest);
            }

            if (!string.IsNullOrEmpty(q))
            {
                model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                model.EncryptedFarmId = q;
            }
            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CopyExistingField(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CopyExistingField() post action called");
            if (field.CopyExistingField == null)
            {
                ModelState.AddModelError("CopyExistingField", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (field.CopyExistingField != null && !(field.CopyExistingField.Value))
            {
                return RedirectToAction("AddField", new { q = field.EncryptedFarmId });
            }

            return RedirectToAction("CopyFields");
        }

        [HttpGet]
        public async Task<IActionResult> CopyFields()
        {
            _logger.LogTrace($"Field Controller : CopyFields() action called");
            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (Error error, List<Field> fieldList) = await _fieldService.FetchFieldByFarmId(model.FarmID, Resource.lblTrue);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.FieldList = fieldList;
            }

            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyFields(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CopyFields() post action called");
            if (field.ID == null)
            {
                ModelState.AddModelError("ID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (Error error, List<Field> fieldList) = await _fieldService.FetchFieldByFarmId(field.FarmID, Resource.lblTrue);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.FieldList = fieldList;
                }
                return View("CopyFields", field);
            }

            SetFieldDataToSession(field);

            if (field.IsCheckAnswer)
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            return RedirectToAction("AddField", new { q = field.EncryptedFarmId });
        }

        [HttpGet]
        public IActionResult HasGrassInLastThreeYear()
        {
            _logger.LogTrace("Field Controller : HasGrassInLastThreeYear() action called");
            Error error = new Error();

            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGrassInLastThreeYear(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : HasGrassInLastThreeYear() post action called");
            if (model.PreviousCroppings.HasGrassInLastThreeYear == null)
            {
                ModelState.AddModelError("PreviousCroppings.HasGrassInLastThreeYear", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            FieldViewModel? fieldData = LoadFieldDataFromSession();
            bool isAnyChangeInHasGrassLastThreeYearFlag = false;
            if (fieldData != null && fieldData.PreviousCroppings != null &&
                   model.PreviousCroppings != null &&
                   fieldData.PreviousCroppings.HasGrassInLastThreeYear != model.PreviousCroppings.HasGrassInLastThreeYear)
            {
                isAnyChangeInHasGrassLastThreeYearFlag = true;
            }
            if (model.IsCheckAnswer && fieldData != null)
            {
                if (isAnyChangeInHasGrassLastThreeYearFlag)
                {
                    model.IsHasGrassInLastThreeYearChange = true;
                    if ((model.PreviousCroppings.HasGrassInLastThreeYear != null && (!model.PreviousCroppings.HasGrassInLastThreeYear.Value)))
                    {
                        model.CropGroupId = null;
                        model.CropGroup = string.Empty;
                        model.CropTypeID = null;
                        model.CropType = string.Empty;
                        model.PreviousCroppings.HarvestYear = null;
                        model.PreviousCroppings.GrassManagementOptionID = null;
                        model.PreviousCroppings.HasGreaterThan30PercentClover = null;
                        model.PreviousCroppings.SoilNitrogenSupplyItemID = null;
                        model.PreviousGrassYears = null;
                        model.IsPreviousYearGrass = null;
                        SetFieldDataToSession(model);
                        return RedirectToAction("CropGroups");
                    }
                    else
                    {
                        if (model.PreviousCroppings.HasGrassInLastThreeYear.HasValue && model.PreviousCroppings.HasGrassInLastThreeYear.Value)
                        {
                            SetFieldDataToSession(model);
                            return RedirectToAction("GrassLastThreeHarvestYear");
                        }
                    }
                }
                else
                {
                    model.IsHasGrassInLastThreeYearChange = false;
                    SetFieldDataToSession(model);
                    if (!model.IsLastHarvestYearChange)
                    {
                        return RedirectToAction(CheckAnswerActionName);
                    }
                }
            }

            SetFieldDataToSession(model);

            if (model.PreviousCroppings.HasGrassInLastThreeYear.HasValue && model.PreviousCroppings.HasGrassInLastThreeYear.Value)
            {
                return RedirectToAction("GrassLastThreeHarvestYear");
            }
            else
            {
                model.PreviousCroppings.HarvestYear = null;
                model.PreviousCroppings.GrassManagementOptionID = null;
                model.PreviousCroppings.HasGreaterThan30PercentClover = null;
                model.PreviousCroppings.SoilNitrogenSupplyItemID = null;
                model.PreviousGrassYears = null;
                model.IsPreviousYearGrass = null;
                SetFieldDataToSession(model);
                if (model.IsCheckAnswer && (!model.IsLastHarvestYearChange))
                {
                    return RedirectToAction(CheckAnswerActionName);
                }
                if (isAnyChangeInHasGrassLastThreeYearFlag)
                {
                    model.CropGroupId = null;
                    model.CropGroup = string.Empty;
                    model.CropTypeID = null;
                    model.CropType = string.Empty;
                    SetFieldDataToSession(model);
                }
                return RedirectToAction("CropGroups");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GrassLastThreeHarvestYear()
        {
            _logger.LogTrace($"Field Controller : GrassLastThreeHarvestYear() action called");
            Error error = new Error();

            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<int> previousYears = new List<int>();
            int lastHarvestYear = model.LastHarvestYear ?? 0;
            previousYears.Add(lastHarvestYear);
            previousYears.Add(lastHarvestYear - 1);
            previousYears.Add(lastHarvestYear - 2);

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate))
            {
                int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedFieldId));
                List<Crop> cropPlans = await _cropService.FetchCropsByFieldId(fieldId);

                if (cropPlans.Any())
                {
                    int oldestYearWithPlan = cropPlans.Min(cp => cp.Year);
                    previousYears = Enumerable.Range(1, 3).Select(i => oldestYearWithPlan - i).ToList();
                }
            }

            ViewBag.PreviousCroppingsYear = previousYears;
            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrassLastThreeHarvestYear(FieldViewModel model)
        {
            _logger.LogTrace("Field Controller : GrassLastThreeHarvestYear() post action called");
            int lastHarvestYear = 0;
            if (model.PreviousGrassYears == null)
            {
                ModelState.AddModelError("PreviousGrassYears", Resource.lblSelectAtLeastOneYearBeforeContinuing);
            }

            SetFieldDataToSession(model);

            if (!ModelState.IsValid)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.LastHarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousCroppingsYear = previousYears;
                return View(model);
            }

            //below condition is for select all
            if (model.PreviousGrassYears?.Count == 1 && model.PreviousGrassYears[0] == 0)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.LastHarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                model.PreviousGrassYears = previousYears;
            }

            lastHarvestYear = model.LastHarvestYear ?? 0;
            model.IsPreviousYearGrass = (model.PreviousGrassYears != null && model.PreviousGrassYears.Contains(lastHarvestYear)) ? true : false;

            SetFieldDataToSession(model);
            if (model.IsPreviousYearGrass.HasValue && model.IsPreviousYearGrass.Value)
            {
                model.CropGroupId = null;
                model.CropGroup = string.Empty;
                model.CropTypeID = null;
                model.CropType = string.Empty;
                SetFieldDataToSession(model);
            }
            if (model.PreviousGrassYears?.Count == 3)
            {
                model.PreviousCroppings.LayDuration = (int)NMP.Commons.Enums.LayDuration.ThreeYearsOrMore;
            }
            else if (model.PreviousGrassYears?.Count <= 2 && model.PreviousGrassYears[0] == model.LastHarvestYear)
            {
                model.PreviousCroppings.LayDuration = (int)NMP.Commons.Enums.LayDuration.OneToTwoYears;
            }
            else
            {
                return RedirectToAction("LayDuration");
            }
            SetFieldDataToSession(model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange) && (!model.IsLastHarvestYearChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            return RedirectToAction("GrassManagementOptions");
        }

        [HttpGet]
        public async Task<IActionResult> GrassManagementOptions()
        {
            _logger.LogTrace($"Field Controller : GrassManagementOptions() action called");
            Error error = new Error();

            FieldViewModel model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
            ViewBag.GrassManagementOptions = commonResponses;
            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassManagementOptions(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GrassManagementOptions() post action called");

            if (model.PreviousCroppings.GrassManagementOptionID == null)
            {
                ModelState.AddModelError("PreviousCroppings.GrassManagementOptionID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = commonResponses;
                return View(model);
            }
            SetFieldDataToSession(model);
            if (model.PreviousCroppings.GrassManagementOptionID == (int)NMP.Commons.Enums.GrassManagementOption.GrazedOnly)
            {
                return RedirectToAction("HasGreaterThan30PercentClover");
            }
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange) && (!model.IsLastHarvestYearChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) && (!model.IsHasGrassInLastThreeYearChange) && model.IsPreviousYearGrass == true)
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction("HasGreaterThan30PercentClover");
        }



        [HttpGet]
        public Task<IActionResult> HasGreaterThan30PercentClover()
        {
            _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() action called");

            FieldViewModel? model = LoadFieldDataFromSession();
            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Task.FromResult<IActionResult>(Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict));
            }

            SetFieldDataToSession(model);
            return Task.FromResult<IActionResult>(View(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGreaterThan30PercentClover(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() post action called");

            if (model.PreviousCroppings.HasGreaterThan30PercentClover == null)
            {
                ModelState.AddModelError("PreviousCroppings.HasGreaterThan30PercentClover", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetFieldDataToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange) && (!model.IsLastHarvestYearChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (model.PreviousCroppings.HasGreaterThan30PercentClover.HasValue && model.PreviousCroppings.HasGreaterThan30PercentClover.Value)
            {
                model.PreviousCroppings.SoilNitrogenSupplyItemID = null;
                SetFieldDataToSession(model);
                if (model.IsPreviousYearGrass == false)
                {
                    return RedirectToAction("CropGroups");
                }

                if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) && (!model.IsHasGrassInLastThreeYearChange))
                {
                    return RedirectToAction(UpdateFieldActionName);
                }

                return RedirectToAction(CheckAnswerActionName);
            }
            else
            {
                return RedirectToAction("SoilNitrogenSupplyItems");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyItems()
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() action called");
            FieldViewModel? model = LoadFieldDataFromSession();

            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
            ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNitrogenSupplyItems(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() post action called");

            if (model.PreviousCroppings.SoilNitrogenSupplyItemID == null)
            {
                ModelState.AddModelError("PreviousCroppings.SoilNitrogenSupplyItemID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderBy(x => x.Id);
                return View(model);
            }

            SetFieldDataToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange) && (!model.IsLastHarvestYearChange))
            {
                return RedirectToAction(CheckAnswerActionName);
            }

            if (model.IsPreviousYearGrass == false && model.CropGroupId == null)
            {
                return RedirectToAction("CropGroups");
            }

            if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction(UpdateFieldActionName);
            }

            return RedirectToAction(CheckAnswerActionName);
        }

        [HttpGet]
        public async Task<IActionResult> LayDuration()
        {
            _logger.LogTrace($"Field Controller : LayDuration() action called");
            FieldViewModel? model = LoadFieldDataFromSession();

            if (model == null)
            {
                _logger.LogTrace("Field Controller : field data not found in session");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            SetFieldDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> LayDuration(FieldViewModel model)
        {
            _logger.LogTrace("Field Controller : LayDuration() post action called");

            if (model.PreviousCroppings.LayDuration == null)
            {
                ModelState.AddModelError("PreviousCroppings.LayDuration", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return Task.FromResult<IActionResult>(View(model));
            }
            SetFieldDataToSession(model);
            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange) && (!model.IsLastHarvestYearChange))
            {
                return Task.FromResult<IActionResult>(RedirectToAction(CheckAnswerActionName));
            }
            if (!string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) && (!model.IsHasGrassInLastThreeYearChange) && model.IsPreviousYearGrass == true)
            {
                return Task.FromResult<IActionResult>(RedirectToAction(UpdateFieldActionName));
            }

            return Task.FromResult<IActionResult>(RedirectToAction("GrassManagementOptions"));
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Field Controller : Cancel() action called");
            FieldViewModel model = LoadFieldDataFromSession();
            try
            {
                if (model == null)
                {
                    _logger.LogTrace("Field Controller : field data not found in session");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in Cancel() action : {0}, {1}", ex.Message, ex.StackTrace);
                TempData["AddFieldError"] = ex.Message;
                return RedirectToAction(CheckAnswerActionName);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(FieldViewModel model)
        {
            _logger.LogTrace("Field Controller : Cancel() post action called");
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
                if (string.IsNullOrWhiteSpace(model.EncryptedFieldId))
                {
                    return RedirectToAction(CheckAnswerActionName);
                }
                else
                {
                    return RedirectToAction(UpdateFieldActionName);
                }
            }
            else
            {
                string isComingFromUpdate = !string.IsNullOrWhiteSpace(model.EncryptedIsUpdate) ? _fieldDataProtector.Protect(true.ToString()) : _fieldDataProtector.Protect(false.ToString());
                return RedirectToAction("CreateFieldCancel", new { id = model.EncryptedFarmId, q = isComingFromUpdate });
            }
        }

        [HttpGet]
        public IActionResult LastHarvestYear()
        {
            _logger.LogTrace($"Field Controller : LastHarvestYear() action called");
            FieldViewModel model;
            if (HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            DateTime currentDate = System.DateTime.Now;
            DateTime startOfCurrentHarvestYear = new DateTime(currentDate.Year, 4, 1);
            DateTime endOfCurrentHarvestYear = new DateTime(currentDate.Year + 1, 3, 31);
            int secondLastHarvestYear = System.DateTime.Now.Year - 1;
            int lastHarvestYear = System.DateTime.Now.Year;
            if (currentDate.Date >= startOfCurrentHarvestYear.Date && currentDate.Date <= endOfCurrentHarvestYear.Date) // Between April and February
            {
                secondLastHarvestYear = currentDate.Year - 1;
                lastHarvestYear = currentDate.Year;
            }
            else if (currentDate.Date < startOfCurrentHarvestYear.Date)
            {
                secondLastHarvestYear = currentDate.Year - 2;
                lastHarvestYear = currentDate.Year - 1;
            }
            else if (currentDate.Date > endOfCurrentHarvestYear.Date)
            {
                secondLastHarvestYear = currentDate.Year;
                lastHarvestYear = currentDate.Year + 1;
            }
            ViewBag.LastHarvestYear = lastHarvestYear;
            ViewBag.SecondLastHarvestYear = secondLastHarvestYear;
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LastHarvestYear(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : LastHarvestYear() post action called");
            if (model.LastHarvestYear == null)
            {
                ModelState.AddModelError("LastHarvestYear", Resource.MsgSelectAHarvestYearBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                DateTime currentDate = System.DateTime.Now;
                DateTime startOfCurrentHarvestYear = new DateTime(currentDate.Year, 4, 1);
                DateTime endOfCurrentHarvestYear = new DateTime(currentDate.Year + 1, 3, 31);
                int secondLastHarvestYear = System.DateTime.Now.Year - 1;
                int lastHarvestYear = System.DateTime.Now.Year;
                if (currentDate.Date >= startOfCurrentHarvestYear.Date && currentDate.Date <= endOfCurrentHarvestYear.Date) // Between April and February
                {
                    secondLastHarvestYear = currentDate.Year - 1;
                    lastHarvestYear = currentDate.Year;
                }
                else if (currentDate.Date < startOfCurrentHarvestYear.Date)
                {
                    secondLastHarvestYear = currentDate.Year - 2;
                    lastHarvestYear = currentDate.Year - 1;
                }
                else if (currentDate.Date > endOfCurrentHarvestYear.Date)
                {
                    secondLastHarvestYear = currentDate.Year;
                    lastHarvestYear = currentDate.Year + 1;
                }
                ViewBag.LastHarvestYear = lastHarvestYear;
                ViewBag.SecondLastHarvestYear = secondLastHarvestYear;
                return View("LastHarvestYear", model);
            }
            FieldViewModel? fieldViewModel = new FieldViewModel();
            if (HttpContext.Session.Keys.Contains("FieldData"))
            {
                fieldViewModel = HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            if (fieldViewModel.LastHarvestYear != model.LastHarvestYear)
            {
                model.IsLastHarvestYearChange = true;
            }
            SetFieldDataToSession(model);
            return RedirectToAction("HasGrassInLastThreeYear");
        }

        private FieldViewModel? LoadFieldDataFromSession()
        {
            if (HttpContext.Session.Exists("FieldData"))
            {
                return HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            return null;
        }

        private void SetFieldDataToSession(FieldViewModel model)
        {
            HttpContext.Session.SetObjectAsJson("FieldData", model);
        }

        private void RemoveFieldDataFromSession()
        {
            if (HttpContext.Session.Exists("FieldData"))
            {
                HttpContext.Session.Remove("FieldData");
            }
        }
    }
}