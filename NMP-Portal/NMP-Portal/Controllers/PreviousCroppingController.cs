using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Application;
using NMP.Commons.Helpers;
using System.Net;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class PreviousCroppingController(ILogger<PreviousCroppingController> logger, IDataProtectionProvider dataProtectionProvider, IFieldLogic fieldLogic, IPreviousCroppingLogic previousCroppingLogic, IFarmLogic farmLogic) : Controller
    {
        private readonly ILogger<PreviousCroppingController> _logger = logger;
        private readonly IFieldLogic _fieldLogic = fieldLogic;
        private readonly IFarmLogic _farmLogic = farmLogic;
        private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
        private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
        private readonly IPreviousCroppingLogic _previousCroppingLogic = previousCroppingLogic;
        private const string _previousCroppingSessionKey = "PreviousCroppingData";
        private const string _checkAnswerActionName = "CheckAnswer";
        private const string _hasGrassInLastThreeYearText = "HasGrassInLastThreeYear";
        private const string _cropGroupsActionName = "CropGroups";
        private const string _sasGreaterThan30PercentCloverActionName = "HasGreaterThan30PercentClover";
        private PreviousCroppingViewModel? GetPreviousCroppingFromSession()
        {
            if (HttpContext.Session.Exists(_previousCroppingSessionKey))
            {
                return HttpContext.Session.GetObjectFromJson<PreviousCroppingViewModel>(_previousCroppingSessionKey);
            }
            return null;
        }

        private void SetPreviousCroppingToSession(PreviousCroppingViewModel previousCroppingViewModel)
        {
            HttpContext.Session.SetObjectAsJson(_previousCroppingSessionKey, previousCroppingViewModel);
        }

        private void RemovePreviousCroppingFromSession()
        {
            if (HttpContext.Session.Exists(_previousCroppingSessionKey))
            {
                HttpContext.Session.Remove(_previousCroppingSessionKey);
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> HasGrassInLastThreeYear(string? q, string? r, string? s)
        {
            _logger.LogTrace($"Previous Croppping Controller: HasGrassInLastThreeYear() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            try
            {
                if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(r) && model == null)
                {
                    _logger.LogTrace($"Previous Croppping Controller : HasGrassInLastThreeYear() action : FarmId and FieldId parameters missing");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.BadRequest);
                }

                if (!string.IsNullOrWhiteSpace(q) && !string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
                {
                    model = new PreviousCroppingViewModel();
                    model.EncryptedFarmID = q;
                    model.EncryptedFieldID = r;
                    model.EncryptedYear = s;
                    model.FieldID = Convert.ToInt32(_fieldDataProtector.Unprotect(r));
                    model.FieldName = (await _fieldLogic.FetchFieldByFieldId(model.FieldID)).Name;
                    int currentYear = Convert.ToInt32(_farmDataProtector.Unprotect(s));
                    model.HarvestYear = currentYear - 1;
                    SetPreviousCroppingToSession(model);
                    int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    (FarmResponse farm, Error error) = await _farmLogic.FetchFarmByIdAsync(farmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmRB209CountryID = farm.RB209CountryID;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Previous Croppping Controller : Exception in HasGrassInLastThreeYear() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HasGrassInLastThreeYear(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGrassInLastThreeYear() post action called");

            if (model.HasGrassInLastThreeYear == null)
            {
                ModelState.AddModelError(_hasGrassInLastThreeYearText, Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return await Task.FromResult(View(model));
            }

            PreviousCroppingViewModel? previousCroppingData = GetPreviousCroppingFromSession();
            if (model.IsCheckAnswer && previousCroppingData != null)
            {
                if (model.HasGrassInLastThreeYear != previousCroppingData.HasGrassInLastThreeYear)
                {
                    model.IsHasGrassInLastThreeYearChange = true;

                    if ((model.HasGrassInLastThreeYear != null && (!model.HasGrassInLastThreeYear.Value)))
                    {
                        model.CropGroupID = null;
                        model.CropTypeID = null;
                        model.CropTypeName = string.Empty;
                        model.GrassManagementOptionID = null;
                        model.HasGreaterThan30PercentClover = null;
                        model.SoilNitrogenSupplyItemID = null;
                        model.PreviousGrassYears = null;
                        model.IsPreviousYearGrass = null;
                        SetPreviousCroppingToSession(model);
                        return RedirectToAction(_cropGroupsActionName);
                    }
                    else
                    {
                        if (model.HasGrassInLastThreeYear.HasValue && model.HasGrassInLastThreeYear.Value)
                        {
                            SetPreviousCroppingToSession(model);
                            return RedirectToAction("GrassLastThreeHarvestYear");
                        }
                    }
                }
                else
                {
                    model.IsHasGrassInLastThreeYearChange = false;
                    SetPreviousCroppingToSession(model);
                    return RedirectToAction(_checkAnswerActionName);
                }
            }

            SetPreviousCroppingToSession(model);

            if (model.HasGrassInLastThreeYear.HasValue && model.HasGrassInLastThreeYear.Value)
            {
                return RedirectToAction("GrassLastThreeHarvestYear");
            }
            else
            {
                model.CropGroupID = null;
                model.CropTypeID = null;
                model.CropTypeName = string.Empty;
                model.GrassManagementOptionID = null;
                model.HasGreaterThan30PercentClover = null;
                model.SoilNitrogenSupplyItemID = null;
                model.PreviousGrassYears = null;
                model.IsPreviousYearGrass = null;
                SetPreviousCroppingToSession(model);

                if (model.IsCheckAnswer)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }

                return RedirectToAction(_cropGroupsActionName);
            }
        }


        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            _logger.LogTrace($"Previous Croppping Controller : CropGroups() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace($"Previous Croppping Controller : CropGroups() action : PreviousCropping data is not available in session");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                }

                ViewBag.CropGroupList = await _fieldLogic.FetchArableCropGroups();
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Previous Croppping Controller : Exception in CropGroups() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["HasGrassInLastThreeYearError"] = ex.Message;
                return RedirectToAction(_hasGrassInLastThreeYearText);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CropGroups() post action called");
            if (model.CropGroupID == null)
            {
                ModelState.AddModelError("CropGroupID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                ViewBag.CropGroupList = await _fieldLogic.FetchArableCropGroups();
                return View(model);
            }

            PreviousCroppingViewModel? previousCroppingData = GetPreviousCroppingFromSession();
            if (previousCroppingData != null)
            {
                if (previousCroppingData.CropGroupID != model.CropGroupID)
                {
                    model.CropTypeName = string.Empty;
                    model.CropTypeID = null;
                }
            }
            else
            {
                _logger.LogTrace($"Previous Croppping Controller : CropGroups() post action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            SetPreviousCroppingToSession(model);

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            _logger.LogTrace($"Previous Croppping Controller : CropTypes() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace($"Previous Croppping Controller : CropTypes() action : PreviousCropping data is not available in session");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                }

                List<CropTypeResponse> cropTypeList = await _fieldLogic.FetchCropTypes(model.CropGroupID.Value, model.FarmRB209CountryID);

                ViewBag.CropTypeList = cropTypeList;
                if (cropTypeList.Count == 1 && cropTypeList[0].CropTypeId == (int)NMP.Commons.Enums.CropTypes.Other)
                {
                    model.CropTypeID = cropTypeList[0].CropTypeId;
                    model.CropTypeName = cropTypeList[0].CropType;
                    SetPreviousCroppingToSession(model);

                    if (model.IsCheckAnswer)
                    {
                        return RedirectToAction(_checkAnswerActionName);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Previous Croppping Controller : Exception in CropTypes() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction(_cropGroupsActionName);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CropTypes() post action called");
            if (model.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = await _fieldLogic.FetchCropTypes(model.CropGroupID.Value, model.FarmRB209CountryID);
                ViewBag.CropTypeList = cropTypes;

                return View(model);
            }

            model.CropTypeName = await _fieldLogic.FetchCropTypeById(model.CropTypeID.Value);
            SetPreviousCroppingToSession(model);

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction(_checkAnswerActionName);
        }

        [HttpGet]
        public IActionResult GrassLastThreeHarvestYear()
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassLastThreeHarvestYear() action called");

            try
            {
                PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();
                if (model == null)
                {
                    _logger.LogTrace($"Previous Croppping Controller : GrassLastThreeHarvestYear() action : PreviousCropping data is not available in session");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                }
                List<int> previousYears = new List<int>();
                int lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousGrassesYear = previousYears;
                SetPreviousCroppingToSession(model);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["HasGrassInLastThreeYearError"] = ex.Message;
                return RedirectToAction(_hasGrassInLastThreeYearText);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrassLastThreeHarvestYear(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassLastThreeHarvestYear() post action called");
            int lastHarvestYear = 0;
            if (model.PreviousGrassYears == null)
            {
                ModelState.AddModelError("PreviousGrassYears", Resource.lblSelectAtLeastOneYearBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousGrassesYear = previousYears;
                return View(model);
            }

            //below condition is for select all
            if (model.PreviousGrassYears?.Count == 1 && model.PreviousGrassYears[0] == 0)
            {
                List<int> previousYears = new List<int>();
                lastHarvestYear = model.HarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                model.PreviousGrassYears = previousYears;
            }

            lastHarvestYear = model.HarvestYear ?? 0;
            model.IsPreviousYearGrass = (model.PreviousGrassYears != null && model.PreviousGrassYears.Contains(lastHarvestYear)) ? true : false;

            SetPreviousCroppingToSession(model);

            if (model.PreviousGrassYears?.Count == 3)
            {
                model.LayDuration = (int)NMP.Commons.Enums.LayDuration.ThreeYearsOrMore;
            }
            else if (model.PreviousGrassYears?.Count <= 2 && model.PreviousGrassYears[0] == model.HarvestYear)
            {
                model.LayDuration = (int)NMP.Commons.Enums.LayDuration.OneToTwoYears;
            }
            else
            {
                return RedirectToAction("LayDuration");
            }

            SetPreviousCroppingToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("GrassManagementOptions");
        }

        [HttpGet]
        public async Task<IActionResult> GrassManagementOptions()
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassManagementOptions() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            if (model == null)
            {
                _logger.LogTrace($"Previous Croppping Controller : GrassManagementOptions() action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            ViewBag.GrassManagementOptions = await _fieldLogic.GetGrassManagementOptions();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassManagementOptions(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : GrassManagementOptions() post action called");

            if (model.GrassManagementOptionID == null)
            {
                ModelState.AddModelError("GrassManagementOptionID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.GrassManagementOptions = await _fieldLogic.GetGrassManagementOptions();
                return View(model);
            }

            SetPreviousCroppingToSession(model);

            if (model.GrassManagementOptionID == (int)NMP.Commons.Enums.GrassManagementOption.GrazedOnly)
            {
                return RedirectToAction(_sasGreaterThan30PercentCloverActionName);
            }

            if (model.IsCheckAnswer && !model.IsHasGrassInLastThreeYearChange)
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            return RedirectToAction(_sasGreaterThan30PercentCloverActionName);
        }

        [HttpGet]
        public async Task<IActionResult> HasGreaterThan30PercentClover()
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGreaterThan30PercentClover() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            if (model == null)
            {
                _logger.LogTrace($"Previous Croppping Controller : HasGreaterThan30PercentClover() action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            return await Task.FromResult(View(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGreaterThan30PercentClover(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : HasGreaterThan30PercentClover() post action called");

            if (model.HasGreaterThan30PercentClover == null)
            {
                ModelState.AddModelError("HasGreaterThan30PercentClover", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetPreviousCroppingToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            if (model.HasGreaterThan30PercentClover.HasValue && model.HasGreaterThan30PercentClover.Value)
            {
                if (model.IsPreviousYearGrass == false)
                {
                    return RedirectToAction(_cropGroupsActionName);
                }
                return RedirectToAction(_checkAnswerActionName);
            }
            else
            {
                return RedirectToAction("SoilNitrogenSupplyItems");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyItems()
        {
            _logger.LogTrace($"Previous Croppping Controller : SoilNitrogenSupplyItems() action called");

            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();
            if (model == null)
            {
                _logger.LogTrace($"Previous Croppping Controller : SoilNitrogenSupplyItems() action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            ViewBag.SoilNitrogenSupplyItems = (await _fieldLogic.GetSoilNitrogenSupplyItems()).OrderBy(x => x.Id);
            SetPreviousCroppingToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNitrogenSupplyItems(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : SoilNitrogenSupplyItems() post action called");

            if (model.SoilNitrogenSupplyItemID == null)
            {
                ModelState.AddModelError("SoilNitrogenSupplyItemID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.SoilNitrogenSupplyItems = (await _fieldLogic.GetSoilNitrogenSupplyItems()).OrderBy(x => x.Id);
                return View(model);
            }

            SetPreviousCroppingToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            if (model.IsPreviousYearGrass == false)
            {
                return RedirectToAction(_cropGroupsActionName);
            }

            return RedirectToAction(_checkAnswerActionName);
        }

        [HttpGet]
        public IActionResult LayDuration()
        {
            _logger.LogTrace($"Previous Croppping Controller : LayDuration() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();
            if (model == null)
            {
                _logger.LogTrace($"Previous Croppping Controller : LayDuration() action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LayDuration(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : LayDuration() post action called");

            if (model.LayDuration == null)
            {
                ModelState.AddModelError("LayDuration", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            SetPreviousCroppingToSession(model);

            if (model.IsCheckAnswer && (!model.IsHasGrassInLastThreeYearChange))
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            return RedirectToAction("GrassManagementOptions");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"Previous Croppping Controller : CheckAnswer() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogTrace($"Previous Croppping Controller : CheckAnswer() action : PreviousCropping data is not available in session");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                }

                List<CommonResponse> grassManagements = await _fieldLogic.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldLogic.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
                model.IsCheckAnswer = true;
                model.IsHasGrassInLastThreeYearChange = false;

                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupID.Value);
                }

                SetPreviousCroppingToSession(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Previous Croppping Controller : Exception in CheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(PreviousCroppingViewModel model)
        {
            _logger.LogTrace($"Previous Croppping Controller : CheckAnswer() post action called");
            if (!model.HasGrassInLastThreeYear.HasValue)
            {
                ModelState.AddModelError(_hasGrassInLastThreeYearText, $"{string.Format(Resource.lblHasFieldNameBeenUsedForGrassInAnyOfTheLastThreeYear, model.FieldName)} {Resource.lblNotSet}");
            }
            else
            {
                if (model.HasGrassInLastThreeYear.Value)
                {
                    if (model.PreviousGrassYears == null)
                    {
                        ModelState.AddModelError("GrassLastThreeHarvestYear", $"{string.Format(Resource.lblInWhichYearsWasUsedForGrass, model.FieldName)} {Resource.lblNotSet}");
                    }

                    if (model.PreviousGrassYears != null && model.PreviousGrassYears.Count == 3 && model.LayDuration == null)
                    {
                        ModelState.AddModelError("LayDuration", $"{string.Format(Resource.lblWhatWasTheLengthOfTheLayInYear, model.PreviousGrassYears?[0])} {Resource.lblNotSet}");
                    }

                    if (model.GrassManagementOptionID == null)
                    {
                        ModelState.AddModelError("GrassManagementOptionID", $"{Resource.lblHowWasTheGrassTypicallyManagedEachYear} {Resource.lblNotSet}");
                    }

                    if (model.HasGreaterThan30PercentClover == null)
                    {
                        ModelState.AddModelError("HasGreaterThan30PercentClover", $"{string.Format(Resource.lblDoesFieldTypicallyHaveMoreThan30PercentClover, model.FieldName)} {Resource.lblNotSet}");
                    }
                    if (model.HasGreaterThan30PercentClover == false && model.SoilNitrogenSupplyItemID == null)
                    {
                        ModelState.AddModelError("SoilNitrogenSupplyItemID", $"{string.Format(Resource.lblHowMuchNitrogenHasBeenAppliedToFieldEachYear, model.FieldName)} {Resource.lblNotSet}");
                    }

                    if (model.CropTypeID == null && (model.HasGrassInLastThreeYear.HasValue && (!model.HasGrassInLastThreeYear.Value)
                        || (model.IsPreviousYearGrass.HasValue && !model.IsPreviousYearGrass.Value)))
                    {
                        ModelState.AddModelError("CropTypeID", $"{string.Format(Resource.lblWhatWasTheCropType, model.HarvestYear)} {Resource.lblNotSet}");
                    }
                }
                else
                {

                    if (model.CropTypeID == null)
                    {
                        ModelState.AddModelError("CropTypeID", $"{string.Format(Resource.lblWhatWasTheCropType, model.HarvestYear)} {Resource.lblNotSet}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                List<CommonResponse> grassManagements = await _fieldLogic.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;
                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupID.Value);
                }

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldLogic.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
                return View(model);
            }


            List<PreviousCropping> previousCropping = new List<PreviousCropping>();
            Error? error = null;
            int? id = null;
            (List<PreviousCroppingData> previousCropList, error) = await _previousCroppingLogic.FetchDataByFieldId(model.FieldID, null);
            if (model.IsPreviousYearGrass == true && model.PreviousGrassYears != null)
            {
                model.CropGroupID = (int)NMP.Commons.Enums.CropGroup.Grass;
                model.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass;
                foreach (var year in model.PreviousGrassYears)
                {
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == year).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    var newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        FieldID = model.FieldID,
                        CropGroupID = model.CropGroupID,
                        CropTypeID = model.CropTypeID,
                        HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                        HarvestYear = year,
                        LayDuration = model.LayDuration,
                        GrassManagementOptionID = model.GrassManagementOptionID,
                        HasGreaterThan30PercentClover = model.HasGreaterThan30PercentClover,
                        SoilNitrogenSupplyItemID = model.SoilNitrogenSupplyItemID
                    };
                    previousCropping.Add(newPreviousCropping);
                }
                if (model.PreviousGrassYears.Count < 3)
                {
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 1))
                    {
                        var newPreviousCropping = new PreviousCropping
                        {
                            ID = id,
                            CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                            CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                            HarvestYear = model.HarvestYear - 1,
                            HasGrassInLastThreeYear = true,
                            FieldID = model.FieldID,
                        };
                        previousCropping.Add(newPreviousCropping);
                    }

                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 2))
                    {
                        var newPreviousCropping = new PreviousCropping
                        {
                            ID = id,
                            CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                            CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                            HarvestYear = model.HarvestYear - 2,
                            HasGrassInLastThreeYear = true,
                            FieldID = model.FieldID,
                        };
                        previousCropping.Add(newPreviousCropping);
                    }
                }
            }
            else
            {
                id = null;
                if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                {
                    id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear.Value).Select(x => x.ID).FirstOrDefault(); ;
                }
                var newPreviousCropping = new PreviousCropping
                {
                    ID = id,
                    FieldID = model.FieldID,
                    CropGroupID = model.CropGroupID,
                    CropTypeID = model.CropTypeID,
                    HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                    HarvestYear = model.HarvestYear,
                    LayDuration = null,
                    GrassManagementOptionID = null,
                    HasGreaterThan30PercentClover = null,
                    SoilNitrogenSupplyItemID = null
                };
                previousCropping.Add(newPreviousCropping);

                if (model.PreviousGrassYears != null)
                {
                    model.CropGroupID = (int)NMP.Commons.Enums.CropGroup.Grass;
                    model.CropTypeID = (int)NMP.Commons.Enums.CropTypes.Grass;
                    foreach (var year in model.PreviousGrassYears)
                    {
                        id = null;
                        if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                        {
                            var firstPreviousCrop = previousCropList.FirstOrDefault(x => x.HarvestYear == year);
                            id = firstPreviousCrop?.ID;
                        }

                        var newPreviousGrass = new PreviousCropping
                        {
                            ID = id,
                            FieldID = model.FieldID,
                            CropGroupID = model.CropGroupID,
                            CropTypeID = model.CropTypeID,
                            HasGrassInLastThreeYear = model.HasGrassInLastThreeYear ?? false,
                            HarvestYear = year,
                            LayDuration = model.LayDuration,
                            GrassManagementOptionID = model.GrassManagementOptionID,
                            HasGreaterThan30PercentClover = model.HasGreaterThan30PercentClover,
                            SoilNitrogenSupplyItemID = model.SoilNitrogenSupplyItemID
                        };

                        previousCropping.Add(newPreviousGrass);
                    }

                    if (model.PreviousGrassYears.Count < 3)
                    {
                        if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 1))
                        {
                            id = null;
                            if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                            {
                                id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault();
                            }
                            newPreviousCropping = new PreviousCropping
                            {
                                ID = id,
                                CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                                FieldID = model.FieldID,
                                CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                HarvestYear = model.HarvestYear - 1,
                                HasGrassInLastThreeYear = true
                            };

                            previousCropping.Add(newPreviousCropping);
                        }

                        if (!model.PreviousGrassYears.Any(x => x == model.HarvestYear - 2))
                        {
                            id = null;
                            if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                            {
                                id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault();
                            }

                            newPreviousCropping = new PreviousCropping
                            {
                                ID = id,
                                CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                                FieldID = model.FieldID,
                                CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                                HarvestYear = model.HarvestYear - 2,
                                HasGrassInLastThreeYear = true
                            };
                            previousCropping.Add(newPreviousCropping);
                        }
                    }
                }
                else
                {
                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 1).Select(x => x.ID).FirstOrDefault();
                    }

                    newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                        CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                        FieldID = model.FieldID,
                        HarvestYear = model.HarvestYear - 1,
                        HasGrassInLastThreeYear = false,
                    };

                    previousCropping.Add(newPreviousCropping);

                    id = null;
                    if (string.IsNullOrWhiteSpace(error.Message) && previousCropList.Count > 0)
                    {
                        id = previousCropList.Where(x => x.HarvestYear == model.HarvestYear - 2).Select(x => x.ID).FirstOrDefault(); ;
                    }
                    newPreviousCropping = new PreviousCropping
                    {
                        ID = id,
                        CropGroupID = (int)NMP.Commons.Enums.CropGroup.Other,
                        CropTypeID = (int)NMP.Commons.Enums.CropTypes.Other,
                        FieldID = model.FieldID,
                        HarvestYear = model.HarvestYear - 2,
                        HasGrassInLastThreeYear = false,
                    };
                    previousCropping.Add(newPreviousCropping);
                }

            }

            var previousDataWrapper = new
            {
                PreviousCroppings = previousCropping
            };

            string jsonData = JsonConvert.SerializeObject(previousDataWrapper);
            (bool success, error) = await _previousCroppingLogic.MergePreviousCropping(jsonData);

            if (string.IsNullOrWhiteSpace(error.Message) && success)
            {
                RemovePreviousCroppingFromSession();
                return RedirectToAction("Recommendations", "Crop", new
                {
                    q = model.EncryptedFarmID,
                    r = model.EncryptedFieldID,
                    s = model.EncryptedYear,
                    t = _cropDataProtector.Protect(Resource.MsgRecommendationsUpdated)
                });
            }
            else
            {
                TempData["Error"] = error.Message;
                List<CommonResponse> grassManagements = await _fieldLogic.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.GrassManagementOptionID)?.Name;

                if (model.CropGroupID != null)
                {
                    ViewBag.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupID.Value);
                }

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldLogic.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.SoilNitrogenSupplyItemID)?.Name;
            }

            return View(model);
        }

        public IActionResult BackCheckAnswer()
        {
            _logger.LogTrace($"Previous cropping Controller : BackCheckAnswer() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();
            if (model == null)
            {
                _logger.LogTrace($"Previous cropping Controller : BackCheckAnswer() action : PreviousCropping data is not available in session");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            model.IsCheckAnswer = false;
            SetPreviousCroppingToSession(model);

            if (model.HasGrassInLastThreeYear != null && model.HasGrassInLastThreeYear.Value)
            {
                if (model.PreviousGrassYears != null && !model.PreviousGrassYears.Contains(model.HarvestYear ?? 0))
                {
                    return RedirectToAction("CropTypes");
                }
                else
                {
                    if (model.HasGreaterThan30PercentClover == false)
                    {
                        return RedirectToAction("SoilNitrogenSupplyItems");
                    }
                    else
                    {
                        return RedirectToAction(_sasGreaterThan30PercentCloverActionName);
                    }
                }
            }
            return RedirectToAction("CropTypes");
        }


        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Previous cropping Controller : Cancel() action called");
            PreviousCroppingViewModel? model = GetPreviousCroppingFromSession();
            try
            {
                if (model == null)
                {
                    _logger.LogTrace("Previous cropping Controller : Cancel() action - PreviousCroppingData not found in session");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Previous cropping  Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction(_checkAnswerActionName);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(PreviousCroppingViewModel model)
        {
            _logger.LogTrace("Previous cropping Controller : Cancel() post action called");

            if (model.IsCancel == null)
            {
                ModelState.AddModelError("IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return await Task.FromResult(View("Cancel", model));
            }

            if (model.IsCancel.HasValue && !model.IsCancel.Value)
            {
                return await Task.FromResult(RedirectToAction(_checkAnswerActionName));
            }
            else
            {
                return await Task.FromResult(RedirectToAction("Recommendations", "Crop", new
                {
                    q = model.EncryptedFarmID,
                    r = model.EncryptedFieldID,
                    s = model.EncryptedYear,
                }));
            }
        }
    }
}
