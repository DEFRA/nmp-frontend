using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Application;
using NMP.Businesses;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Controllers;
using NMP.Portal.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NMP.Portal.Areas.Manner.Controllers
{
    [Area("Manner")]
    [Authorize]
    public class MannerEstimationController(ILogger<MannerEstimationController> logger, IMannerEstimationLogic mannerEstimationLogic, IDataProtectionProvider dataProtectionProvider, IMannerEstimationLogicDependencies dependencies) : Controller
    {
        private readonly ILogger<MannerEstimationController> _logger = logger;
        private readonly IMannerEstimationLogic _mannerEstimationLogic = mannerEstimationLogic;

        private readonly IOrganicManureLogic _organicManureLogic = dependencies.OrganicManureLogic;
        private readonly IFarmLogic _farmLogic = dependencies.FarmLogic;
        private readonly ICropLogic _cropLogic = dependencies.CropLogic;
        private readonly IFieldLogic _fieldLogic = dependencies.FieldLogic;
        private readonly IMannerLogic _mannerLogic = dependencies.MannerLogic;
        private readonly IWarningLogic _warningLogic = dependencies.WarningLogic;

        private const string _checkAnswerActionName = "CheckAnswer";
        private readonly IDataProtector _mannerEstimationProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.MannerEstimationController");
        private const string _mannerEstimationSessionName = "MannerEstimation";
        private const string _mannerEstimationControllerForLog = "MannerEstimation  Controller : ";
        private const string _organisationId = "organisationId";
        private const string _sowingDate = "SowingDate";
        private const string _applicationDateKey = "ApplicationDate";
        private const string _dryMatterPercentKey = "DryMatterPercent";
        private const string _applicationRateMethodAction = "ApplicationRateMethod";
        private const string _incorporationMethodAction = "IncorporationMethod";
        private const string _applicationRateKey = "ApplicationRate";
        private const string _farmNameKey = "FarmName";
        private const string _areaKey = "AreaSpread";
        private const string _quantityKey = "ManureQuantity";
        private const string _dateStringLiteral = "yyyy-MM-dd";
        private const string _conditionsAffectingNutrients = "ConditionsAffectingNutrients";
        private const string _soilDrainageEndDateKey = "SoilDrainageEndDate";
        private const string _totalRainfallKey = "TotalRainfall";
        private const string _autumnCropNitrogenUptakeKey = "AutumnCropNitrogenUptake";

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> MannerHubPage(string? q)
        {
            RemoveMannerEstimationSession();
            if (!string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }
            Guid organisationId = GetOrganisationId();
            var (mannerEstimations, error) = await _mannerEstimationLogic.FetchMannerEstimationsList(organisationId);

            if (string.IsNullOrWhiteSpace(error?.Message) && mannerEstimations.Count > 0)
            {
                foreach (var estimation in mannerEstimations)
                {
                    estimation.EncryptedId = _mannerEstimationProtector.Protect(estimation.ID.ToString());
                }

                ViewBag.MannerEstimations = mannerEstimations;
                return View();
            }

            return RedirectToAction("Name");
        }

        public IActionResult MannerEstimationCancel()
        {
            _logger.LogTrace("MannerEstimation Controller : MannerEstimationCancel() action called");
            return RedirectToAction("MannerHubPage", new { q = _mannerEstimationProtector.Protect(Resource.lblTrue) });
        }



        private void RemoveMannerEstimationSession()
        {
            if (HttpContext.Session.Exists(_mannerEstimationSessionName))
            {
                HttpContext.Session.Remove(_mannerEstimationSessionName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> FarmName()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmName() action called");
            MannerEstimationStep1ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep1();
            List<SelectListItem> farmsWithFields = await BindAllFarmList();
            if (farmsWithFields.Count > 0)
            {
                model.IsFarmCopied = true;
            }
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmName(MannerEstimationStep1ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmName() post action called");
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);

            if (string.IsNullOrWhiteSpace(model.FarmName))
            {
                ModelState.AddModelError(_farmNameKey, Resource.MsgEnterTheFarmName);
            }
            List<SelectListItem> farmsWithFields = await BindAllFarmList();
            if (farmsWithFields.Count > 0)
            {
                model.IsFarmCopied = true;
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep1();
                return View(model);
            }

            model = _mannerEstimationLogic.SetMannerEstimationStep1(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("Country");
        }

        [HttpGet]
        public async Task<IActionResult> Country()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  Country() action called");


            MannerEstimationStep2ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep2();
            try
            {
                if (model == null)
                {
                    _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in Country() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                ViewBag.CountryList = await _farmLogic.FetchCountryAsync();

                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in Country() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Country(MannerEstimationStep2ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  Country() post action called");

            try
            {
                if (model.CountryID == 0)
                {
                    ModelState.AddModelError("CountryID", Resource.MsgSelectTheCountryTheFarmIsIn);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep2();
                    ViewBag.CountryList = await _farmLogic.FetchCountryAsync();
                    return View("Country", model);
                }

                model = await _mannerEstimationLogic.SetMannerEstimationStep2(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("PostCode");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in Country() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpGet]
        public IActionResult FarmingRules()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmingRules() action called");

            MannerEstimationStep2ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep2();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
        public IActionResult FarmingRules(MannerEstimationStep2ViewModel model)
        {

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            return RedirectToAction("PostCode");
        }

        [HttpGet]
        public IActionResult PostCode()
        {

            _logger.LogTrace($"{_mannerEstimationControllerForLog}  PostCode() action called");
            MannerEstimationStep3ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep3();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog}  Session not found in PostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostCode(MannerEstimationStep3ViewModel model)
        {

            _logger.LogTrace($"{_mannerEstimationControllerForLog} PostCode() post action called");
            try
            {
                if (model.Postcode == null)
                {
                    ModelState.AddModelError("Postcode", Resource.MsgEnterTheFarmPostcode);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep3();
                    return View(model);
                }

                model = await _mannerEstimationLogic.SetMannerEstimationStep3(model);

                if (model.IsCheckAnswer && !model.IsPostCodeChange)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction("AverageAnnualRainfall");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog} HttpRequestException in PostCode()");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog} Exception in PostCode()");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }


        [HttpGet]
        public async Task<IActionResult> AverageAnnualRainfall()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfall() action called");

            try
            {
                MannerEstimationStep4ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep4();

                if (model == null)
                {
                    _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in AverageAnnualRainfall() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in AverageAnnualRainfall() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in AverageAnnualRainfall() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AverageAnnualRainfall(MannerEstimationStep4ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfall() post action called");


            if (!ModelState.IsValid)
            {
                model = await _mannerEstimationLogic.GetMannerEstimationStep4();
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("IsFarmOrganic");
        }

        [HttpGet]
        public async Task<IActionResult> AverageAnnualRainfallManual()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfallManual() action called");
            MannerEstimationStep4ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep4();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in AverageAnnualRainfallManual() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AverageAnnualRainfallManual(MannerEstimationStep4ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfallManual() post action called");
            ValidateRainfall(model);


            if (!ModelState.IsValid)
            {
                model = await _mannerEstimationLogic.GetMannerEstimationStep4();
                return View(model);
            }
            model = await _mannerEstimationLogic.SetMannerEstimationStep4(model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("IsFarmOrganic");
        }

        private void ValidateRainfall(MannerEstimationStep4ViewModel model)
        {
            string key = Resource.lblAverageAnnualRainfallForError;
            if ((!ModelState.IsValid) && ModelState.ContainsKey(key))
            {
                var RainfallError = ModelState[key]?.Errors.Count > 0 ?
                                ModelState[key]?.Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null)
                {
                    ModelState[key]?.Errors.Clear();
                    if (RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[key]?.RawValue, Resource.lblAverageAnnualRainfallForError)))
                    {
                        ModelState[key]?.Errors.Add(Resource.MsgEnterRainfallBetween1And3000);
                    }
                    else if (RainfallError.Equals(Resource.MsgTheValueIsInvalid))
                    {
                        ModelState.AddModelError(key, Resource.MsgEnterTheAverageAnnualRainfall);
                    }
                }
            }

            if (model.AverageAnnualRainfall < 1 || model.AverageAnnualRainfall > 3000)
            {
                ModelState.AddModelError(key, Resource.MsgEnterRainfallBetween1And3000);
            }
        }

        [HttpGet]
        public IActionResult FieldName()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FieldName() action called");
            MannerEstimationStep5ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep5();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in FieldName() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FieldName(MannerEstimationStep5ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FieldName() post action called");


            if (string.IsNullOrWhiteSpace(model.FieldName))
            {
                ModelState.AddModelError("FieldName", Resource.MsgEnterTheFieldName);
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep5();
                return View(model);
            }

            model = _mannerEstimationLogic.SetMannerEstimationStep5(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("NVZField");
        }

        [HttpGet]
        public IActionResult NVZField()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} NVZField() action called");
            MannerEstimationStep6ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep6();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in NVZField() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZField(MannerEstimationStep6ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} NVZField() post action called");


            if (!model.IsWithinNVZ.HasValue)
            {
                ModelState.AddModelError("IsWithinNVZ", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep6();
                return View(model);
            }

            model = _mannerEstimationLogic.SetMannerEstimationStep6(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("TopSoil");
        }

        [HttpGet]
        public async Task<IActionResult> SoilType()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilType() action called");

            MannerEstimationStep7ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep7();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in SoilType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ViewBag.SoilTypesList = await _mannerLogic.FetchSoilTypesByRB209CountryId(model.FarmRB209CountryId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilType(MannerEstimationStep7ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilType() post action called");

            if (model.SoilTypeId == null)
            {
                ModelState.AddModelError("SoilTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep7();
                ViewBag.SoilTypesList = await _mannerLogic.FetchSoilTypesByRB209CountryId(model.FarmRB209CountryId);
                return View(model);
            }

            model = _mannerEstimationLogic.SetMannerEstimationStep7(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropGroup");
        }
        [HttpGet]
        public async Task<IActionResult> CropGroup()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropGroup() action called");
            MannerEstimationStep8ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep8();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in CropGroup() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ViewBag.CropGroupList = await _fieldLogic.FetchCropGroups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroup(MannerEstimationStep8ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropGroup() post action called");


            if (model.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep8();
                ViewBag.CropGroupList = await _fieldLogic.FetchCropGroups();
                return View(model);
            }

            model.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupId ?? 0);
            model = _mannerEstimationLogic.SetMannerEstimationStep8(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropType");
        }

        [HttpGet]
        public async Task<IActionResult> CropType()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropType() action called");

            MannerEstimationStep9ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep9();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in CropType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ViewBag.CropTypeList = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropType(MannerEstimationStep9ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropType() post action called");


            if (model.CropTypeId == null)
            {
                ModelState.AddModelError("CropTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep9();
                ViewBag.CropTypeList = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryId);
                return View(model);
            }

            model.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupId ?? 0);
            model.CropTypeName = await _fieldLogic.FetchCropTypeById(model.CropTypeId ?? 0);
            model = await _mannerEstimationLogic.SetMannerEstimationStep9(model);

            if (model.CropTypeId != null && Enum.IsDefined(typeof(NMP.Commons.Enums.EarlyOrLateSownCropTypes), model.CropTypeId))
            {
                return RedirectToAction("SowingDate");
            }

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ManureGroup");
        }

        [HttpGet]
        public IActionResult IsEarlySown()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IsEarlySown() action called");

            MannerEstimationStep10ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep10();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in IsEarlySown() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IsEarlySown(MannerEstimationStep10ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IsEarlySown() post action called");


            if (model.IsEarlySown == null)
            {
                ModelState.AddModelError("IsEarlySown", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model = _mannerEstimationLogic.SetMannerEstimationStep10(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ManureGroup");
        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureGroup() action called");

            MannerEstimationStep11ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep11();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ManureGroup() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            (List<SelectListItem> manureGroupList, Error? error) = await FetchManureGroup();
            if (error == null)
            {
                ViewBag.ManureGroupList = manureGroupList;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureGroup(MannerEstimationStep11ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureGroup() post action called");


            if (model.ManureGroupId == null)
            {
                ModelState.AddModelError("ManureGroupId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            Error? error = null;
            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep11();
                (List<SelectListItem> manureGroupList, error) = await FetchManureGroup();
                if (error == null)
                {
                    ViewBag.ManureGroupList = manureGroupList;
                }
                return View(model);
            }

            if (model.ManureGroupId.HasValue)
            {
                (CommonResponse manureGroup, error) = await _mannerLogic.FetchManureGroupById(model.ManureGroupId.Value);
                if (error == null && manureGroup != null)
                {
                    model.ManureGroupName = manureGroup.Name;
                }
            }
            model = _mannerEstimationLogic.SetMannerEstimationStep11(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ManureType");
        }
        private async Task<(List<SelectListItem>, Error?)> FetchManureGroup()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            (List<CommonResponse> manureGroupList, Error? error) = await _mannerLogic.FetchManureGroupList();
            if (error == null && manureGroupList.Count > 0)
            {
                selectListItems = manureGroupList.OrderBy(x => x.SortOrder).Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name.ToString()
                }).ToList();

            }
            return (selectListItems, error);
        }

        private async Task<(List<SelectListItem>, Error?)> FetchManureType(MannerEstimationStep12ViewModel model)
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            (List<ManureType> manureTypeList, Error? error) = await _mannerLogic.FetchManureTypeList(model.ManureGroupId, model.FarmRB209CountryId);
            if (error == null && manureTypeList.Count > 0)
            {
                selectListItems = manureTypeList.OrderBy(x => x.Name).Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name
                }).ToList();
            }
            return (selectListItems, error);
        }
        [HttpGet]
        public async Task<IActionResult> ManureType()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureType() action called");

            MannerEstimationStep12ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep12();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ManureType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            (List<SelectListItem> manureTypeList, Error? error) = await FetchManureType(model);
            if (error == null)
            {
                ViewBag.ManureTypeList = manureTypeList;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureType(MannerEstimationStep12ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureType() post action called");


            if (model.ManureTypeId == null)
            {
                ModelState.AddModelError("ManureTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            Error? error = null;
            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep12();
                (List<SelectListItem> manureTypeList, error) = await FetchManureType(model);
                if (error == null)
                {
                    ViewBag.ManureTypeList = manureTypeList;
                }
                return View(model);
            }

            if (model.ManureTypeId.HasValue)
            {
                (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                if (error == null && manureType != null && !string.IsNullOrWhiteSpace(manureType.Name))
                {
                    model.ManureTypeName = manureType.Name;
                }
            }
            model = _mannerEstimationLogic.SetMannerEstimationStep12(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ApplicationDate");
        }
        [HttpGet]
        public async Task<IActionResult> ApplicationDate()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationDate() action called");

            MannerEstimationStep13ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep13();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in SoilType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            bool isPerennial = await _cropLogic.FetchIsPerennialByCropTypeId(model.CropTypeId ?? 0);
            int fieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;

            var (soilTypeId, error) = await _mannerEstimationLogic.FetchSoilTypeSoilTextureByTopSoilSubSoilId(model.TopSoilId ?? 0, model.SubSoilId ?? 0);
            if (string.IsNullOrEmpty(error?.Message))
            {
                string closedPeriod = Functions.GetMannerClosedPeriod(soilTypeId, fieldType, model.SowingDate, model.FarmRB209CountryId, model.CropGroupId ?? 0, model.CropTypeId ?? 0, isPerennial);
                model.ClosedPeriod = closedPeriod;
            }

            model.IsWarningMsgNeedToShow = false;
            model.IsClosedPeriodWarning = false;
            model.IsApplicationJulyToSeptWarning = false;
            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
            model = _mannerEstimationLogic.SetMannerEstimationStep13(model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationDate(MannerEstimationStep13ViewModel formData)
        {
            _logger.LogTrace($"Manner Estimation Controller : ApplicationDate() post action called");
            MannerEstimationStep13ViewModel model = new MannerEstimationStep13ViewModel();
            try
            {
                AddErrorIfNull(formData.ApplicationDate, _applicationDateKey, Resource.MsgEnterADateBeforeContinuing);

                if (!ModelState.IsValid)
                {
                    formData = _mannerEstimationLogic.GetMannerEstimationStep13();
                    return View(formData);
                }
                model = _mannerEstimationLogic.GetMannerEstimationStep13();
                model.ApplicationDate = formData.ApplicationDate;
                var (manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                _mannerEstimationLogic.SetMannerEstimationStep13(model);
                //non organic farm, high N, NVZ
                if (!string.IsNullOrWhiteSpace(model.ClosedPeriod) && string.IsNullOrWhiteSpace(error?.Message))
                {
                    int harvestYear = GetHarvestYearFromApplicationDate(model.ApplicationDate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));

                    error = await CheckApplicationDateWarnings(model, manureType, harvestYear);
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        TempData["ApplicationDateError"] = error.Message;
                    }

                    (bool flowControl, IActionResult? value) = BindPropertiesForManureApplyingDate(model);
                    if (!flowControl && value != null)
                    {
                        return value;
                    }
                }

                return RedirectToAction("ApplicationMethod");
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Manner Estimation Controller  : Exception in ApplicationDate() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        private async Task<Error?> CheckApplicationDateWarnings(MannerEstimationStep13ViewModel model, ManureType? manureType, int harvestYear)
        {
            Error? error = null;
            DateTime endDate = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            (var startDate, endDate) = GetClosedPeriodDates(model.ClosedPeriod, model.ApplicationDate.Value);

            if (model.CountryId != (int)NMP.Commons.Enums.FarmCountry.Scotland)
            {
                if (!(model.IsFarmOrganic ?? false) && manureType.HighReadilyAvailableNitrogen.GetValueOrDefault() && (model.IsWithinNVZ ?? false))
                {
                    await HandleNonOrganicHighNWarning(startDate, endDate, model);
                }

                if ((model.IsFarmOrganic ?? false) && manureType.HighReadilyAvailableNitrogen.GetValueOrDefault() && (model.IsWithinNVZ ?? false))
                {
                    await HandleOrganicHighNWarning(startDate, endDate, model);
                }
            }
            await CheckScotlandClosedPeriodWarning(model, manureType, endDate, startDate);


            // England-specific warning for Winter Oilseed Rape or Grass
            await EndOctoberToEndClosedPeriodWarning(endDate, model, harvestYear);

            error = await EndClosedPeriodEndFebSlurryPoultryTwentyDayWarning(model);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error;
            }

            if (model.FarmRB209CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland)
            {
                //01 July to 30 September scotland warning
                await HandleScotlandHighN(model, harvestYear);

                //There have been 20 days or less since the last application livestock manure
                if (IsLivestockCondition(model))
                {
                    error = await HandleLivestockManureRule(model);
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        return error;
                    }
                }
            }
            _mannerEstimationLogic.SetMannerEstimationStep13(model);
            return error;
        }

        private async Task CheckScotlandClosedPeriodWarning(MannerEstimationStep13ViewModel model, ManureType? manureType, DateTime endDate, DateTime startDate)
        {
            if (model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland && manureType.HighReadilyAvailableNitrogen.GetValueOrDefault() && (model.IsWithinNVZ ?? false))
            {
                await HandleNonOrganicHighNWarning(startDate, endDate, model);
            }
        }

        private async Task<Error?> EndClosedPeriodEndFebSlurryPoultryTwentyDayWarning(MannerEstimationStep13ViewModel model)
        {
            Error? error = null;

            bool? isWithinClosedPeriodAndFebruary = WarningWithinPeriod.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, model.ClosedPeriod);
            if (isWithinClosedPeriodAndFebruary == true)
            {
                (List<MannerEstimationApplication> mannerApplications, error) = await _mannerEstimationLogic.FetchMannerApplicationsByMannerEstimationId(model.MannerEstimationId ?? 0);
                if (mannerApplications.Count > 0)
                {
                    var mannerApplicationWithin21Days = mannerApplications.First(x => (model.ApplicationDate.Value - x.ApplicationDate).TotalDays <= 21);

                    bool isSlurry = IsSlurryType(mannerApplicationWithin21Days.ManureTypeID);
                    bool isPoultryManure =
                        model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.PoultryManure;

                    if (isSlurry || isPoultryManure)
                    {
                        // warning excel sheet row no. 21
                        model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;

                        WarningResponse warning =
                            await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                                model.CountryId,
                                NMP.Commons.Enums.WarningKey
                                    .AllowWeeksBetweenSlurryPoultryApplications.ToString());

                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader = warning.Header;
                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID = warning.WarningCodeID;
                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID = warning.WarningLevelID;
                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = warning.Para1;
                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = warning.Para2;
                        model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3 = warning.Para3;
                    }
                }

            }
            return error;

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

        private async Task<Error?> HandleLivestockManureRule(MannerEstimationStep13ViewModel model)
        {
            (List<MannerEstimationApplication> mannerApplications, Error? error) = await _mannerEstimationLogic.FetchMannerApplicationsByMannerEstimationId(model.MannerEstimationId ?? 0);
            if (mannerApplications.Count > 0)
            {
                var mannerApplicationWithin21Days = mannerApplications.First(x => (model.ApplicationDate.Value - x.ApplicationDate).TotalDays <= 21);
                (ManureType manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(mannerApplicationWithin21Days.ManureTypeID ?? 0);
                if (manureType.ManureGroupId == (int)NMP.Commons.Enums.ManureGroup.LivestockManure)
                {
                    await ApplyLivestockWarning(model);
                }
            }
            return error;
        }
        private async Task HandleScotlandHighN(MannerEstimationStep13ViewModel model, int harvestYear)
        {

            if (model.CropTypeId != (int)NMP.Commons.Enums.CropTypes.Grass)
            {
                if (IsWithinRange(harvestYear, model.ApplicationDate.Value, 7, 1, 7, 31) && (model.SowingDate == null || (model.SowingDate.Value - model.ApplicationDate.Value).TotalDays >= 43))
                {
                    await ScotlandJulyHighNWarning(model);
                }

                if (IsWithinRange(harvestYear - 1, model.ApplicationDate.Value, 8, 1, 9, 30) &&
                    (model.SowingDate == null || (model.SowingDate.Value - model.ApplicationDate.Value).TotalDays >= 43))
                {
                    await ScotlandJulyHighNWarning(model);
                }
            }

        }
        private async Task<(MannerEstimationStep13ViewModel, Error?)> ScotlandJulyHighNWarning(MannerEstimationStep13ViewModel model)
        {
            //scotland warning excel sheet row no. 26
            WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                model.CountryId, NMP.Commons.Enums.WarningKey.RanManureJulyToSep.ToString());
            model.ApplicationJulyToSeptHeader = warning.Header;
            model.ApplicationJulyToSeptCodeID = warning.WarningCodeID;
            model.ApplicationJulyToSeptLevelID = warning.WarningLevelID;
            model.ApplicationJulyToSeptPara1 = warning.Para1;
            model.ApplicationJulyToSeptPara2 = warning.Para2;
            model.ApplicationJulyToSeptPara3 = warning.Para3;
            model.IsApplicationJulyToSeptWarning = true;

            return (model, null);
        }
        private static bool IsWithinRange(int harvestYear, DateTime applicationDate, int startMonth, int startDay, int endMonth, int endDay)
        {

            DateTime start = new DateTime(harvestYear, startMonth, startDay, 0, 0, 0, DateTimeKind.Utc);

            DateTime end = new DateTime(harvestYear, endMonth, endDay, 0, 0, 0, DateTimeKind.Utc);

            return WarningWithinPeriod.IsApplicationDateWithinDateRange(applicationDate, start, end);
        }

        private async Task ApplyLivestockWarning(MannerEstimationStep13ViewModel model)
        {
            model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = true;

            var warning =
                await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.CountryId,
                    NMP.Commons.Enums.WarningKey
                        .AllowWeeksBetweenSlurryPoultryApplications.ToString());

            model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader = warning.Header;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID = warning.WarningCodeID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID = warning.WarningLevelID;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 = warning.Para1;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 = warning.Para2;
            model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3 = warning.Para3;
        }

        private (bool flowControl, IActionResult? value) BindPropertiesForManureApplyingDate(MannerEstimationStep13ViewModel model)
        {
            if (model.IsClosedPeriodWarning || model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks || model.IsApplicationJulyToSeptWarning)
            {
                if (!model.IsWarningMsgNeedToShow)
                {
                    model.IsWarningMsgNeedToShow = true;
                    model = _mannerEstimationLogic.SetMannerEstimationStep13(model);
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
            _mannerEstimationLogic.SetMannerEstimationStep13(model);
            return (flowControl: true, value: null);
        }
        private static bool IsLivestockCondition(MannerEstimationStep13ViewModel model)
        {
            return (model.ManureGroupId ==
                       (int)NMP.Commons.Enums.ManureGroup.LivestockManure) &&
                   model.IsWithinNVZ.Value;
        }
        private static int GetHarvestYearFromApplicationDate(DateTime applicationDate)
        {
            return applicationDate.Month >= 8
                ? applicationDate.Year + 1
                : applicationDate.Year;
        }
        private static (DateTime StartDate, DateTime EndDate) GetClosedPeriodDates(string closedPeriod, DateTime applicationDate)
        {
            int harvestYear = GetHarvestYearFromApplicationDate(applicationDate);

            string[] parts = closedPeriod.Split(" to ");

            string startPart = parts[0]; // e.g. "1 October"
            string endPart = parts[1];   // e.g. "31 January"

            var startTokens = startPart.Split(' ');
            var endTokens = endPart.Split(' ');

            int startDay = int.Parse(startTokens[0]);
            int startMonth = DateTime.ParseExact(
                startTokens[1],
                "MMMM",
                CultureInfo.InvariantCulture).Month;

            int endDay = int.Parse(endTokens[0]);
            int endMonth = DateTime.ParseExact(
                endTokens[1],
                "MMMM",
                CultureInfo.InvariantCulture).Month;

            // Determine years
            int startYear = harvestYear - 1;
            int endYear = endMonth < startMonth
                ? harvestYear          // crosses year boundary
                : harvestYear - 1;     // same calendar year

            DateTime startDate = new(startYear, startMonth, startDay, 00, 00, 00, DateTimeKind.Unspecified);
            DateTime endDate = new(endYear, endMonth, endDay, 00, 00, 00, DateTimeKind.Unspecified);

            return (startDate, endDate);
        }
        private async Task HandleNonOrganicHighNWarning(DateTime startDate, DateTime endDate, MannerEstimationStep13ViewModel model)
        {
            bool isWithinClosedPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(
                model.ApplicationDate, startDate, endDate);

            if (isWithinClosedPeriod)
            {
                //warning excel sheet row no. 10
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.CountryId, NMP.Commons.Enums.WarningKey.HighNOrganicManureClosedPeriod.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara2 = warning.Para2;
                model.ClosedPeriodWarningPara3 = warning.Para3;
                model.IsClosedPeriodWarning = true;
            }
        }
        
        private async Task HandleOrganicHighNWarning(DateTime startDate, DateTime endDate,
            MannerEstimationStep13ViewModel model)
        {
            bool isWithinClosedPeriod = false;

            isWithinClosedPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, startDate, endDate);
            HashSet<int> cropTypeIdsForTrigger = WarningWithinPeriod.FilteredCropForWarning();
            if (isWithinClosedPeriod && !cropTypeIdsForTrigger.Contains(model.CropTypeId ?? 0))
            {
                //warning excel sheet row no. 12
                model.IsClosedPeriodWarning = true;
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.CountryId, NMP.Commons.Enums.WarningKey.HighNOrganicManureClosedPeriodOrganicFarm.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara3 = warning.Para3;
            }



        }

        private async Task EndOctoberToEndClosedPeriodWarning(DateTime endDate, MannerEstimationStep13ViewModel model, int harvestYear)
        {
            DateTime endOfOctober = new DateTime(harvestYear - 1, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            if ((model.CropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape ||
                 model.CropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass) &&
                WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, endOfOctober, endDate) &&
                (model.CountryId == (int)NMP.Commons.Enums.FarmCountry.England))
            {
                //warning excel sheet row no. 17
                WarningResponse warning = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(
                    model.CountryId, NMP.Commons.Enums.WarningKey.HighNOrganicManureDateOnly.ToString());
                model.ClosedPeriodWarningHeader = warning.Header;
                model.ClosedPeriodWarningCodeID = warning.WarningCodeID;
                model.ClosedPeriodWarningLevelID = warning.WarningLevelID;
                model.IsClosedPeriodWarning = true;
                model.ClosedPeriodWarningPara1 = warning.Para1;
                model.ClosedPeriodWarningPara3 = warning.Para3;
            }
        }

        private void AddErrorIfNull(object? value, string key, string errorMessage)
        {
            if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                ModelState.AddModelError(key, errorMessage);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CopyExistingFarmAndFieldDetails()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyExistingFarmAndFieldDetails() action called");
            MannerEstimationStep14ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep14();
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);
            try
            {

                if (model == null)
                {
                    _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in CopyExistingFarmAndFieldDetails() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                List<SelectListItem> farmsWithFields = await BindAllFarmList();
                if (farmsWithFields.Count > 0)
                {
                    return View(model);
                }
                else
                {
                    return RedirectToAction(_farmNameKey);
                }

            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyExistingFarmAndFieldDetails() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyExistingFarmAndFieldDetails() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyExistingFarmAndFieldDetails(MannerEstimationStep14ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyExistingFarmAndFieldDetails() post action called");
            try
            {
                ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);

                if (!model.IsCopyExistingFarmAndFieldDetails.HasValue)
                {
                    ModelState.AddModelError("IsCopyExistingFarmAndFieldDetails", Resource.MsgSelectWheatherYouWantToUseExisting);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep14();
                    await BindAllFarmList();
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep14(model);
                string action = _farmNameKey;
                if (model.IsCopyExistingFarmAndFieldDetails.HasValue && model.IsCopyExistingFarmAndFieldDetails.Value)
                {
                    action = "FarmToCopy";
                }

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction(action);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyExistingFarmAndFieldDetails() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyExistingFarmAndFieldDetails() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        private async Task<List<SelectListItem>> BindAllFarmList()
        {
            Guid organisationId = GetOrganisationId();
            (List<Farm> farmList, _) = await _farmLogic.FetchFarmByOrgIdAsync(organisationId);
            List<SelectListItem> farmsWithFields = new List<SelectListItem>();
            foreach (var farm in farmList)
            {
                (_, var fields) = await _fieldLogic.FetchFieldByFarmId(farm.ID, true.ToString());

                if (fields != null && fields.Any())
                {
                    farmsWithFields.Add(new SelectListItem
                    {
                        Value = farm.ID.ToString(),
                        Text = farm.Name
                    });
                }
            }
            return farmsWithFields;
        }

        [HttpGet]
        public async Task<IActionResult> FarmToCopy()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FarmToCopy() action called");
            MannerEstimationStep15ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep15();
            try
            {
                List<SelectListItem> farmsWithFields = await BindAllFarmList();
                if (farmsWithFields.Count > 0)
                {
                    ViewBag.FarmList = farmsWithFields;
                }
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in FarmToCopy() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in FarmToCopy() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmToCopy(MannerEstimationStep15ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FarmToCopy() post action called");
            try
            {
                if (!model.FarmId.HasValue)
                {
                    ModelState.AddModelError("FarmId", string.Format(Resource.MsgSelectAnJourneyName, Resource.lblFarm));
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep15();
                    List<SelectListItem> farmsWithFields = await BindAllFarmList();
                    if (farmsWithFields.Count > 0)
                    {
                        ViewBag.FarmList = farmsWithFields;
                    }
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep15(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("FieldToCopy");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in FarmToCopy() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in FarmToCopy() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        private async Task<List<SelectListItem>> BindAllFieldList(int farmId)
        {
            List<SelectListItem> fieldList = new List<SelectListItem>();
            (_, var fields) = await _fieldLogic.FetchFieldByFarmId(farmId, true.ToString());

            foreach (var field in fields)
            {
                fieldList.Add(new SelectListItem
                {
                    Value = field.ID.ToString(),
                    Text = field.Name
                });
            }
            return fieldList;
        }
        [HttpGet]
        public async Task<IActionResult> FieldToCopy()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FieldToCopy() action called");
            MannerEstimationStep16ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep16();
            try
            {
                List<SelectListItem> fieldList = await BindAllFieldList(model.FarmId.Value);
                if (fieldList.Count > 0)
                {
                    ViewBag.FieldList = fieldList;
                }
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in FieldToCopy() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in FieldToCopy() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldToCopy(MannerEstimationStep16ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FieldToCopy() post action called");
            try
            {
                if (!model.FieldId.HasValue)
                {
                    ModelState.AddModelError("FieldId", string.Format(Resource.MsgSelectAnJourneyName, Resource.lblField));
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep16();
                    List<SelectListItem> fieldList = await BindAllFieldList(model.FarmId.Value);
                    if (fieldList.Count > 0)
                    {
                        ViewBag.FieldList = fieldList;
                    }
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep16(model);
                MannerEstimationStep15ViewModel mannerEstimationStep15ViewModel = _mannerEstimationLogic.GetMannerEstimationStep15();
                if (mannerEstimationStep15ViewModel.FarmId != null && model.FieldId != null)
                {
                    await _mannerEstimationLogic.CopiedFarmAndFieldData(mannerEstimationStep15ViewModel.FarmId.Value, model.FieldId.Value);
                }
                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropGroup");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in FieldToCopy() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in FieldToCopy() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        [HttpGet]
        public async Task<IActionResult> IsFarmOrganic()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  IsFarmOrganic() action called");
            MannerEstimationStep17ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep17();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in IsFarmOrganic() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsFarmOrganic(MannerEstimationStep17ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  IsFarmOrganic() post action called");
            try
            {
                if (!model.IsFarmOrganic.HasValue)
                {
                    ModelState.AddModelError("IsFarmOrganic", Resource.MsgSelectWhetherYouAreARegisteredOrganicProducer);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep17();
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep17(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("FieldName");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in IsFarmOrganic() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in IsFarmOrganic() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        private async Task BindAllTopsoilList()
        {
            (List<CommonResponse>? topsoilList, _) = await _mannerLogic.FetchTopsoilList();

            ViewBag.TopsoilList = topsoilList?.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            }).ToList();

        }
        [HttpGet]
        public async Task<IActionResult> TopSoil()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  TopSoil() action called");
            MannerEstimationStep18ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep18();
            try
            {
                await BindAllTopsoilList();
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in TopSoil() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in TopSoil() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopSoil(MannerEstimationStep18ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  TopSoil() post action called");
            try
            {
                if (!model.TopSoilId.HasValue)
                {
                    ModelState.AddModelError("TopSoilId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep18();
                    await BindAllTopsoilList();
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep18(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("SubSoil");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in TopSoil() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in TopSoil() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        private async Task BindAllSubsoilList()
        {
            (List<CommonResponse>? subsoilList, _) = await _mannerLogic.FetchSubsoilList();

            ViewBag.SubsoilList = subsoilList?.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            }).ToList();

        }
        [HttpGet]
        public async Task<IActionResult> SubSoil()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  SubSoil() action called");
            MannerEstimationStep19ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep19();
            try
            {
                await BindAllSubsoilList();
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in SubSoil() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in SubSoil() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubSoil(MannerEstimationStep19ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  SubSoil() post action called");
            try
            {
                if (!model.SubSoilId.HasValue)
                {
                    ModelState.AddModelError("SubSoilId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep19();
                    await BindAllSubsoilList();
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep19(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropGroup");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in SubSoil() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in SubSoil() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        [HttpGet]
        public async Task<IActionResult> SowingDate(string q)
        {
            _logger.LogTrace("Crop Controller : SowingDate action called");
            MannerEstimationStep20ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep20();
            try
            {
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in SowingDate() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in SowingDate() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SowingDate(MannerEstimationStep20ViewModel model)
        {
            _logger.LogTrace("Manner estimation Controller : SowingDate() post action called");
            try
            {
                model = await ValidateSowingDatePost(model);
                if (model.SowingDate != null)
                {
                    ValidateCropSpecificRules(model);
                    model = await _mannerEstimationLogic.SetMannerEstimationStep20(model);
                }
                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep20();
                    return View(model);
                }

                return RedirectToAction("ManureGroup");

            }
            catch (Exception ex)
            {
                TempData["SowingDateError"] = ex.Message;
                return View(model);
            }
        }
        private async Task<MannerEstimationStep20ViewModel> ValidateSowingDatePost(MannerEstimationStep20ViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey(_sowingDate))
            {
                var entry = ModelState[_sowingDate];
                var error = entry?.Errors.FirstOrDefault()?.ErrorMessage;

                if (error != null && IsDateFormatError(error))
                {
                    entry.Errors.Clear();
                    entry.Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }
            ValidateRequiredDate(model);
            return model;
        }
        private static bool IsDateFormatError(string error)
        {
            string[] patterns = CommonHelpers.DatePattern();
            return patterns.Any(p => error.Equals(string.Format(p, _sowingDate)));
        }

        private void ValidateCropSpecificRules(MannerEstimationStep20ViewModel model)
        {
            MannerEstimationStep9ViewModel mannerEstimationStep9ViewModel = _mannerEstimationLogic.GetMannerEstimationStep9();
            var cropType = mannerEstimationStep9ViewModel.CropTypeId;

            bool isWinterCrop =
                cropType == (int)NMP.Commons.Enums.CropTypes.WinterWheat ||
                cropType == (int)NMP.Commons.Enums.CropTypes.WinterTriticale ||
                cropType == (int)NMP.Commons.Enums.CropTypes.ForageWinterTriticale ||
                cropType == (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat;

            var date = model.SowingDate;

            if (isWinterCrop && date != null && date.Value.Month is >= 2 and <= 6)
            {
                ModelState.AddModelError("SowingDate",
                    string.Format(Resource.MsgForSowingDate, model.CropTypeName));
            }
        }

        private void ValidateRequiredDate(MannerEstimationStep20ViewModel model)
        {
            if (model.SowingDate == null)
            {
                ModelState.AddModelError(_sowingDate, Resource.MsgEnterADateBeforeContinuing);
            }
        }
        private async Task<List<ApplicationMethodResponse>> BindViewBegForApplicationMethod(MannerEstimationStep23ViewModel model)
        {
            (var manureType, _) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            bool isLiquid = manureType?.IsLiquid ?? false;

            int fieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;
            (List<ApplicationMethodResponse> applicationMethodList, _) = await _mannerLogic.FetchApplicationMethodList(fieldType, isLiquid);
            if (applicationMethodList.Count > 0)
            {
                ViewBag.ApplicationMethodList = applicationMethodList.OrderBy(a => a.SortOrder).ToList();
            }

            return applicationMethodList;
        }
        [HttpGet]
        public async Task<IActionResult> ApplicationMethod()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationMethod() action called");

            MannerEstimationStep23ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep23();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ApplicationMethod() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            List<ApplicationMethodResponse> applicationMethodList = await BindViewBegForApplicationMethod(model);

            model.ApplicationMethodCount = applicationMethodList.Count;
            if (applicationMethodList.Count == 1)
            {
                model.ApplicationMethodId = applicationMethodList[0].ID;
                await _mannerEstimationLogic.SetMannerEstimationStep23(model);
                return RedirectToAction("DefaultNutrientValues");
            }
            await _mannerEstimationLogic.SetMannerEstimationStep23(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationMethod(MannerEstimationStep23ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  ApplicationMethod() post action called");
            try
            {
                if (!model.ApplicationMethodId.HasValue)
                {
                    ModelState.AddModelError("ApplicationMethodId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep23();
                    await BindViewBegForApplicationMethod(model);
                    return View(model);
                }

                await _mannerEstimationLogic.SetMannerEstimationStep23(model);

                return RedirectToAction("DefaultNutrientValues");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in ApplicationMethod() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in ApplicationMethod() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        [HttpGet]
        public async Task<IActionResult> DefaultNutrientValues()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} DefaultNutrientValues() action called");

            MannerEstimationStep24ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep24();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in DefaultNutrientValues() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefaultNutrientValues(MannerEstimationStep24ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  DefaultNutrientValues() post action called");
            try
            {
                if (!model.DefaultNutrientValue.HasValue)
                {
                    ModelState.AddModelError("DefaultNutrientValue", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep24();
                    return View(model);
                }

                model = await _mannerEstimationLogic.SetMannerEstimationStep24(model);

                if (!model.DefaultNutrientValue.Value)
                {
                    return RedirectToAction("ManualNutrientValues");
                }
                return RedirectToAction(_applicationRateMethodAction);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in DefaultNutrientValues() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in DefaultNutrientValues() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        private void ReplaceNumericError(string key, string validationLabel, string displayLabel)
        {
            if (ModelState.ContainsKey(key))
            {
                var errorMessage = ModelState[key].Errors[0].ErrorMessage;
                string expectedMessage = string.Format(Resource.lblEnterNumericValue, ModelState[key].RawValue, validationLabel);
                if (string.Equals(errorMessage, expectedMessage))
                {
                    ModelState[key].Errors.Clear();
                    ModelState[key].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, displayLabel));
                }
            }
        }
        private void ValidateNutrientValues(MannerEstimationStep25ViewModel model)
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
            MinMaxValidationForDryMatterAndTotalN(model.N, "N", Resource.lblTotalNitrogenN.ToLower(), 0, 297);
            ValidateNH4NUricAcidNO3NAndP2O5(model);

        }

        private void ValidateDryMatter(MannerEstimationStep25ViewModel model)
        {
            if (model.DryMatterPercent != null)
            {
                if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.PigSlurry ||
                    model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.CattleSlurry)
                {
                    MinMaxValidationForDryMatterAndTotalN(model.DryMatterPercent, _dryMatterPercentKey, Resource.lblDryMatter.ToLower(), 0, 25);
                }
                else
                {
                    MinMaxValidationForDryMatterAndTotalN(model.DryMatterPercent, _dryMatterPercentKey, Resource.lblDryMatter.ToLower(), 0, 99);
                }
            }

        }

        private void MinMaxValidationForDryMatterAndTotalN(decimal? value,
    string fieldName,
    string displayName, decimal minValue,
    decimal maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                ModelState.AddModelError(fieldName, string.Format(Resource.MsgMinMaxValidation, displayName, maxValue));
            }
        }

        private void ValidateNH4NUricAcidNO3NAndP2O5(MannerEstimationStep25ViewModel model)
        {
            ValidateMaxValue(model.NH4N, "NH4N", Resource.lblAmmonium, 99);
            ValidateMaxValue(model.UricAcid, "UricAcid", Resource.lblUricAcid, 99);
            ValidateMaxValue(model.NO3N, "NO3N", Resource.lblNitrate, 99);
            ValidateMaxValue(model.P2O5, "P2O5", Resource.lblPhosphateP2O5, 99);
            ValidateMaxValue(model.K2O, "K2O", Resource.lblPotashK2O, 99);
            ValidateMaxValue(model.MgO, "MgO", Resource.lblMagnesiumMgO, 99);
            ValidateMaxValue(model.SO3, "SO3", Resource.lblSulphurSO3, 99);
        }
        private void ValidateMaxValue(
    decimal? value,
    string fieldName,
    string displayName,
    decimal max)
        {
            if (value.HasValue && (value.Value < 0 || value.Value > max))
            {
                ModelState.AddModelError(
                    fieldName,
                    string.Format(Resource.MsgMinMaxValidation, displayName, max));
            }
        }
        [HttpGet]
        public async Task<IActionResult> ManualNutrientValues()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManualNutrientValues() action called");

            MannerEstimationStep25ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep25();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ManualNutrientValues() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualNutrientValues(MannerEstimationStep25ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  ManualNutrientValues() post action called");
            try
            {
                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep25();
                    ValidateManualNutrientValues();
                }

                CheckNutrientValuesIfNull(model);
                ValidateNutrientValues(model);

                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep25();
                    return View(model);
                }

                await _mannerEstimationLogic.SetMannerEstimationStep25(model);


                return RedirectToAction(_applicationRateMethodAction);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in ManualNutrientValues() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in ManualNutrientValues() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        private void CheckNutrientValuesIfNull(MannerEstimationStep25ViewModel model)
        {
            AddErrorIfNull(model.DryMatterPercent, _dryMatterPercentKey, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblDryMatter.ToLower()));

            AddErrorIfNull(model.N, "N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblTotalNitrogen.ToLower()));

            AddErrorIfNull(model.NH4N, "NH4N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAmmoniumForError));

            AddErrorIfNull(model.UricAcid, "UricAcid", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.MsgUricAcid));

            AddErrorIfNull(model.NO3N, "NO3N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblNitrateForErrorMsg));

            AddErrorIfNull(model.P2O5, "P2O5", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPhosphate.ToLower()));

            AddErrorIfNull(model.K2O, "K2O", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPotash.ToLower()));

            AddErrorIfNull(model.SO3, "SO3", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblSulphur.ToLower()));

            AddErrorIfNull(model.MgO, "MgO", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblMagnesiumMgO.ToLower()));
        }

        private void ValidateManualNutrientValues()
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

        [HttpGet]
        public async Task<IActionResult> ApplicationRateMethod()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationRateMethod() action called");

            MannerEstimationStep26ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep26();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ApplicationRateMethod() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(MannerEstimationStep26ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : ApplicationRateMethod() post action called");
            try
            {
                AddErrorIfNull(model.ApplicationRateMethod, _applicationRateMethodAction, Resource.MsgSelectAnOptionBeforeContinuing);


                await _mannerEstimationLogic.SetMannerEstimationStep26(model);
                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep26();
                    return View(_applicationRateMethodAction, model);
                }
                if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.EnterAnApplicationRate)
                {
                    return RedirectToAction("ManualApplicationRate");
                }
                if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
                {
                    return RedirectToAction("AreaQuantity");
                }
                if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
                {
                    return RedirectToAction(_incorporationMethodAction);

                }

                return RedirectToAction(_incorporationMethodAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManualApplicationRate() action called");

            MannerEstimationStep27ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep27();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ManualApplicationRate() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ResetWarnings(model, true);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualApplicationRate(MannerEstimationStep27ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : ManualApplicationRate() post action called");
            try
            {
                AddErrorIfNull(model.ApplicationRate, _applicationRateKey, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblApplicationRate));
                if (model.ApplicationRate != null)
                {
                    if (model.ApplicationRate < 0)
                        ModelState.AddModelError(_applicationRateKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);

                    if (model.ApplicationRate > 250)
                        ModelState.AddModelError(_applicationRateKey, Resource.MsgForApplicationRate);
                    if (model.ApplicationRate != Math.Round(model.ApplicationRate.Value, 2))
                    {
                        ModelState.AddModelError(_applicationRateKey, string.Format(Resource.MsgEnterAnPropertyOnlyTwoDecimal, Resource.lblApplicationRate));
                    }
                }
                await _mannerEstimationLogic.SetMannerEstimationStep27(model);
                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep27();
                    return View(model);
                }

                ResetWarnings(model, false);
                bool hasAnyWarning = model.IsOrgManureNfieldLimitWarning;
                if (hasAnyWarning)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        await _mannerEstimationLogic.SetMannerEstimationStep27(model);
                        return View(model);
                    }
                }
                else
                {
                    ResetWarnings(model, true);
                }
                model.IsWarningMsgNeedToShow = false;
                await IsNFieldLimitWarningMessage(model);
                return RedirectToAction(_incorporationMethodAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        private async Task<(MannerEstimationStep27ViewModel, Error?)> IsNFieldLimitWarningMessage(MannerEstimationStep27ViewModel model)
        {

            Error? error = null;
            decimal defaultNitrogen = 0;
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if(string.IsNullOrWhiteSpace(error?.Message))
            {
                defaultNitrogen= manureType?.TotalN ?? 0;
            }

            List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();

            if (model.ApplicationRate.HasValue && model.ApplicationDate.HasValue)
            {
                decimal previousAppliedTotalN = 0;
                decimal totalN = 0;

                //The planned application would result in more than 250 kg/ha of total N from all applications of any Manure type apart from ‘Green compost’ or ‘Green/food compost’, applied or planned to the field in the last 365 days up to and including the application date of the manure
                //warning excel sheet row 2
                if (model.ManureTypeId != (int)NMP.Commons.Enums.ManureTypes.GreenCompost && model.ManureTypeId != (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost)
                {
                    //passing orgId
                    if (model.UpdatedMannerAppId != null)
                    {
                        (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId??0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, model.UpdatedMannerAppId);
                    }
                    else
                    {
                        (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId??0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, null);
                    }
                    if (error == null)
                    {
                        decimal currentApplicationNitrogen = 0;
                        currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                        totalN = previousAppliedTotalN + currentApplicationNitrogen;
                        if (totalN > 250)
                        {
                            model.IsOrgManureNfieldLimitWarning = true;
                            var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimit.ToString();

                            WarningResponse? warning = warningList
                                .FirstOrDefault(x => x.CountryID == model.CountryId &&
                                                     string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));

                            if (warning != null)
                            {
                                model.NFieldLimitWarningHeader = warning.Header;
                                model.NFieldLimitWarningCodeID = warning.WarningCodeID;
                                model.NFieldLimitWarningLevelID = warning.WarningLevelID;

                                model.NFieldLimitWarningPara1 = warning.Para1;
                                model.NFieldLimitWarningPara2 = warning.Para2;
                                model.NFieldLimitWarningPara3 = warning.Para3;
                            }
                        }
                    }
                }

                //warning excel sheet row no. 4
                bool isScotland = model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;

                bool isCompost = model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenCompost || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost;

                if (isScotland || isCompost)
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
                   

                    //The planned application would result in more than 500 of total N from all applications of Green compost & Green/food compost applied or planned to the field in the last 730 days up to and including the application date of the manure.

                    
                    (MannerEstimationResultResponse? mannerEstimationResultResponse, error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(model.MannerEstimationId ?? 0);

                    if (!cropTypeIdsForTrigger.Contains(mannerEstimationResultResponse.MannerEstimation.CropTypeID??0) || isScotland)
                    {
                        //passing orgId
                        if (model.UpdatedMannerAppId != null)
                        {
                            if (!isScotland)
                            {
                                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId??0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, true, model.UpdatedMannerAppId);
                            }
                            else
                            {
                                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, model.UpdatedMannerAppId);
                            }

                        }
                        else
                        {
                            if (!isScotland)
                            {
                                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, true, null);
                            }
                            else
                            {
                                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, null);
                            }

                        }

                        if (error == null)
                        {

                            decimal currentApplicationNitrogen = 0;
                            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                            totalN = previousAppliedTotalN + currentApplicationNitrogen;

                            (bool isGreenCompostExistIn2Year, error) = await _mannerEstimationLogic.CheckMannerGreenCompostExistanceByDateRange(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729).ToString(_dateStringLiteral), model.ApplicationDate.Value.ToString(_dateStringLiteral), model.UpdatedMannerAppId);

                            if ((!isScotland || isGreenCompostExistIn2Year || isCompost) && totalN > 500)
                            {
                                model.IsOrgManureNfieldLimitWarning = true;
                                showPAS100Warning = false;
                                var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompost.ToString();

                                WarningResponse? warning = warningList
                                    .FirstOrDefault(x => x.CountryID == model.CountryId &&
                                                         string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));
                                if (warning != null)
                                {
                                    model.NFieldLimitWarningHeader = warning.Header;
                                    model.NFieldLimitWarningCodeID = warning.WarningCodeID;
                                    model.NFieldLimitWarningLevelID = warning.WarningLevelID;

                                    model.NFieldLimitWarningPara1 = warning.Para1;
                                    model.NFieldLimitWarningPara2 = warning.Para2;
                                    model.NFieldLimitWarningPara3 = warning.Para3;
                                }
                            }

                        }
                    }
                    //warning excel sheet row no. 6
                    if (cropTypeIdsForTrigger.Contains(mannerEstimationResultResponse.MannerEstimation.CropTypeID ?? 0))
                    {
                        //passing orgId
                        if (model.UpdatedMannerAppId != null)
                        {
                            (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-1459), model.ApplicationDate.Value, true, model.UpdatedMannerAppId);
                        }
                        else
                        {
                            (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-1459), model.ApplicationDate.Value, true, null);
                        }

                        if (error == null)
                        {
                            decimal currentApplicationNitrogen = 0;
                            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                            totalN = previousAppliedTotalN + currentApplicationNitrogen;
                            if (totalN > 1000)
                            {
                                model.IsOrgManureNfieldLimitWarning = true;

                                var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostMulch.ToString();

                                WarningResponse? warning = warningList
                                    .FirstOrDefault(x => x.CountryID == model.CountryId &&
                                                         string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));
                                if (warning != null)
                                {
                                    model.NFieldLimitWarningHeader = warning.Header;
                                    model.NFieldLimitWarningCodeID = warning.WarningCodeID;
                                    model.NFieldLimitWarningLevelID = warning.WarningLevelID;

                                    model.NFieldLimitWarningPara1 = warning.Para1;
                                    model.NFieldLimitWarningPara2 = warning.Para2;
                                    model.NFieldLimitWarningPara3 = warning.Para3;
                                }
                            }

                        }
                    }
                    if (model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland && isCompost)
                    {
                        if (model.UpdatedMannerAppId != null)
                        {
                            (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, true, model.UpdatedMannerAppId);
                        }
                        else
                        {
                            (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(model.MannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, true, null);
                        }

                        if (error == null)
                        {

                            decimal currentApplicationNitrogen = 0;
                            currentApplicationNitrogen = (defaultNitrogen * model.ApplicationRate.Value);
                            totalN = previousAppliedTotalN + currentApplicationNitrogen;
                            if (totalN > 250 && showPAS100Warning)
                            {
                                model.IsOrgManureNfieldLimitWarning = true;

                                var warningKey = NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostPAS.ToString();

                                WarningResponse? warning = warningList
                                    .FirstOrDefault(x => x.CountryID == model.CountryId &&
                                                         string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));
                                if (warning != null)
                                {
                                    model.NFieldLimitWarningHeader = warning.Header;
                                    model.NFieldLimitWarningCodeID = warning.WarningCodeID;
                                    model.NFieldLimitWarningLevelID = warning.WarningLevelID;

                                    model.NFieldLimitWarningPara1 = warning.Para1;
                                    model.NFieldLimitWarningPara2 = warning.Para2;
                                    model.NFieldLimitWarningPara3 = warning.Para3;
                                }
                            }

                        }
                    }
                }

            }
            return (model, error);

        }

        private static void ResetWarnings(MannerEstimationStep27ViewModel model, bool isWarningMsgNeedToShowReset)
        {
            if (isWarningMsgNeedToShowReset)
            {
                model.IsWarningMsgNeedToShow = false;
            }
            model.IsOrgManureNfieldLimitWarning = false;
            
        }
        [HttpGet]
        public async Task<IActionResult> AreaQuantity()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AreaQuantity() action called");

            MannerEstimationStep28ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep28();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in AreaQuantity() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AreaQuantity(MannerEstimationStep28ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : AreaQuantity() post action called");
            try
            {
                AddErrorIfNull(model.AreaSpread, "AreaSpread", string.Format(Resource.MsgEnterAValidArea, Resource.lblArea));
                AddErrorIfNull(model.ManureQuantity, "ManureQuantity", string.Format(Resource.MsgEnterAValidQuantity, Resource.lblQuantity));
                ValidateAreaQuantity(model);

                await _mannerEstimationLogic.SetMannerEstimationStep28(model);
                if (!ModelState.IsValid)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep28();
                    return View("AreaQuantity", model);
                }
                model.ApplicationRate = Math.Round((model.ManureQuantity.Value / model.AreaSpread.Value), 2);
                return RedirectToAction(_incorporationMethodAction);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller : Exception in AreaQuantity() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        private void ValidateAreaQuantity(MannerEstimationStep28ViewModel model)
        {
            ValidateArea(model);
            ValidateQuantity();

            ValidateAreaRules(model);
            ValidateQuantityRules(model);
            if (model.AreaSpread > 0 && model.ManureQuantity > 0)
            {
                model.ApplicationRate = Math.Round(model.ManureQuantity.Value / model.AreaSpread.Value, 2);

                if (model.ApplicationRate <= 0 || model.ApplicationRate > 250)
                {
                    ModelState.AddModelError(_quantityKey, Resource.MsgCalculateApplicationRateMustNotBeGreaterThanTwoFifty);
                }
            }
        }



        private void ValidateArea(MannerEstimationStep28ViewModel model)
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
            if (model.AreaSpread.HasValue && Math.Round(model.AreaSpread.Value, 2) != model.AreaSpread.Value)
            {
                ModelState.AddModelError(_areaKey,
                     string.Format(Resource.lblFarmAreaCanHaveOnlyTwoDecimalPlace, Resource.lblArea.ToLower()));
                return;
            }

            var expectedError = string.Format(Resource.lblEnterNumericValue, rawValue, Resource.lblAreas);

            if (!string.IsNullOrEmpty(firstError) && firstError.Equals(expectedError))
            {
                state.Errors.Clear();
                state.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
            }
        }


        private void ValidateQuantity()
        {
            if (!ModelState.TryGetValue(_quantityKey, out var state))
            {
                return;
            }
            var firstError = state.Errors.FirstOrDefault()?.ErrorMessage;
            var rawValue = state.RawValue?.ToString();

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }


            const int maxLength = 10;

            if (rawValue.Length > maxLength)
            {
                ModelState.AddModelError(
                    _quantityKey,
                    string.Format(
                        Resource.lblValueMustNotExeedXDigit,
                        Resource.lblQuantity,
                        maxLength));
            }
            var expectedError = string.Format(Resource.lblEnterNumericValue, rawValue, Resource.lblQuantity);

            if (!string.IsNullOrEmpty(firstError) && firstError.Equals(expectedError))
            {
                state.Errors.Clear();
                state.Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.MsgQuantity));
            }
        }


        private void ValidateAreaRules(MannerEstimationStep28ViewModel model)
        {
            if (!model.AreaSpread.HasValue)
                return;

            if (model.AreaSpread == 0)
                ModelState.AddModelError(_areaKey, Resource.MsgAreaMustBeGreaterThanZero);

            if (model.AreaSpread < 0)
                ModelState.AddModelError(_areaKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);

            if (model.AreaSpread != Math.Round(model.AreaSpread.Value, 2))
            {
                ModelState.AddModelError(_areaKey, string.Format(Resource.MsgEnterAnPropertyOnlyTwoDecimal, Resource.lblAreas));
            }
        }


        private void ValidateQuantityRules(MannerEstimationStep28ViewModel model)
        {
            if (!model.ManureQuantity.HasValue)
                return;

            if (model.ManureQuantity < 0)
                ModelState.AddModelError(_quantityKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);
            if (model.ManureQuantity != Math.Round(model.ManureQuantity.Value, 2))
            {
                ModelState.AddModelError(_quantityKey, string.Format(Resource.MsgEnterAnPropertyOnlyTwoDecimal, Resource.lblQuantity));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CopyEstimate()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyEstimate() action called");


            MannerEstimationStep21ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep21();
            try
            {
                if (model == null)
                {
                    _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in CopyEstimate() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                bool isEstimateExist = false;
                Guid organisationId = GetOrganisationId();
                var (estimations, error) = await _mannerEstimationLogic.FetchMannerEstimationsList(organisationId);

                if (string.IsNullOrWhiteSpace(error?.Message))
                {
                    isEstimateExist = estimations.Count > 0;
                }

                if (isEstimateExist)
                {
                    return View(model);
                }
                else
                {
                    return RedirectToAction("CopyExistingFarmAndFieldDetails");
                }

            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyEstimate() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyEstimate() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CopyEstimate(MannerEstimationStep21ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyEstimate() post action called");

            try
            {
                if (model.IsCopyEstimate == null)
                {
                    ModelState.AddModelError("IsCopyEstimate", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep21();
                    return View("CopyEstimate", model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep21(model);
                if (!model.IsCopyEstimate.Value)
                {
                    return RedirectToAction("Name");
                }

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CopyFromEstimates");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyEstimate() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyEstimate() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpGet]
        public async Task<IActionResult> CopyFromEstimates()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyFromEstimates() action called");


            MannerEstimationStep22ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep22();
            try
            {
                if (model == null)
                {
                    _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in CopyFromEstimates() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }
                await LoadMannerEstimations();
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyFromEstimates() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyFromEstimates() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyFromEstimates(MannerEstimationStep22ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyFromEstimates() post action called");

            try
            {
                if (model.MannerEstimationId == null)
                {
                    ModelState.AddModelError("MannerEstimationId", Resource.MsgSelectAnEstimateToContinue);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep22();
                    await LoadMannerEstimations();
                    return View("CopyFromEstimates", model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep22(model);
                //call copy api to copy the selected estimate to current estimate


                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("Name");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in CopyFromEstimates() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in CopyFromEstimates() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpGet]
        public async Task<IActionResult> MannerEstimationResult(string? q, string? r)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  MannerEstimationResult() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                int estimateId =Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                (MannerEstimationResultResponse? mannerEstimationResultResponse, Error? error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(estimateId);
                if(!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["Error"] = error.Message;
                    return RedirectToAction("MannerHubPage");
                }
                await BindViewBegForMannerEstimationResult(mannerEstimationResultResponse);
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.Success = _mannerEstimationProtector.Unprotect(r);
            }

            return View();
        }

        
        private async Task BindViewBegForMannerEstimationResult(MannerEstimationResultResponse? mannerEstimationResultResponse)
        {
            int nitrogenValue = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.NitrogenValue);
            int p2O5Value = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.PhosphateValue);
            int potashValue = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.PotashValue);
            ViewBag.TotalValue = nitrogenValue + p2O5Value + potashValue;

            ViewBag.LastUpdatedOn = mannerEstimationResultResponse.LastUpdatedOn;
            ViewBag.MannerEstimations = mannerEstimationResultResponse;
            int count = 0;
            foreach (var application in mannerEstimationResultResponse.MannerEstimationApplication)
            {
                count++;
                bool isManureLiquid = await _mannerEstimationLogic.FetchIsManureLiquid(application.ManureTypeID.Value);
                string manureUnit = isManureLiquid ? Resource.lblMeterCubePerHa : Resource.lblTonnesPerHectare;
                TempData[$"ApplicationDefaultValues{count}"] = await _mannerEstimationLogic.FetchDefaultNutrientValue(application.ManureTypeID.Value, application);
                if (application.AreaSpread != null && application.ManureQuantity != null)
                {
                    TempData[$"ApplicationRateOption{count}"] = Resource.lblCalculateBasedOnTheAreaAndQuantity;
                }
                else
                {
                    (bool isDefaultRate, int defaultRate) = await _mannerEstimationLogic.FetchApplicationRateOptionValue(application.ManureTypeID.Value, application, mannerEstimationResultResponse.MannerEstimation);
                    if (isDefaultRate)
                    {
                        TempData[$"ApplicationRateOption{count}"] = string.Format(Resource.lblUseTypicalApplicationRate, defaultRate, manureUnit);
                    }
                    else
                    {
                        TempData[$"ApplicationRateOption{count}"] = string.Format(Resource.lblEnterAnApplicationRate, application.ManureType);
                    }
                }
            }

        }
        private async Task LoadMannerEstimations()
        {
            Guid organisationId = GetOrganisationId();
            var (mannerEstimations, error) = await _mannerEstimationLogic.FetchMannerEstimationsList(organisationId);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return;
            }

            foreach (var estimation in mannerEstimations)
            {
                estimation.FarmName = string.Format(Resource.lblEstimationForFarm, estimation.Name, estimation.FarmName);
            }

            ViewBag.MannerEstimations = mannerEstimations.OrderBy(x => x.Name);
        }
        private async Task<(List<IncorporationMethodResponse>, Error?)> BindViewBegForIncorporationMethod(MannerEstimationStep29ViewModel model)
        {
            string applicableFor = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? Resource.lblG : Resource.lblA;
            if (model.ApplicationMethodId == (int)NMP.Commons.Enums.ApplicationMethod.ShallowInjection57cm ||
                model.ApplicationMethodId == (int)NMP.Commons.Enums.ApplicationMethod.DeepInjection2530cm)
            {
                applicableFor = Resource.lblNull;
            }
            (List<IncorporationMethodResponse> incorporationMethods, Error? error) = await _mannerLogic.FetchIncorporationMethodsByApplicationId(model.ApplicationMethodId.Value, applicableFor);
            if (incorporationMethods != null)
            {
                ViewBag.IncorporationMethod = incorporationMethods.OrderBy(i => i.SortOrder).ToList();
            }
            return (incorporationMethods, error);
        }
        [HttpGet]
        public async Task<IActionResult> IncorporationMethod()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IncorporationMethod() action called");

            MannerEstimationStep29ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep29();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in IncorporationMethod() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            _mannerEstimationLogic.SetMannerEstimationStep29(model);
            (List<IncorporationMethodResponse> incorporationMethods, Error? error) = await BindViewBegForIncorporationMethod(model);
            if (error != null)
            {
                TempData["ApplicationRateMethodError"] = error.Message;
                return RedirectToAction(_applicationRateMethodAction);
            }
            if (incorporationMethods.Count == 1)
            {
                model.IncorporationMethodId = incorporationMethods[0].ID;
                _mannerEstimationLogic.SetMannerEstimationStep29(model);
                return RedirectToAction("IncorporationDelay");
            }
            _mannerEstimationLogic.SetMannerEstimationStep29(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationMethod(MannerEstimationStep29ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  IncorporationMethod() post action called");
            try
            {
                if (!model.IncorporationMethodId.HasValue)
                {
                    ModelState.AddModelError("IncorporationMethodId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep29();
                    (_, Error? error) = await BindViewBegForIncorporationMethod(model);
                    if (error != null)
                    {
                        TempData["IncorporationMethodError"] = error.Message;
                    }
                    return View(model);
                }

                _mannerEstimationLogic.SetMannerEstimationStep29(model);

                return RedirectToAction("IncorporationDelay");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in IncorporationMethod() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in IncorporationMethod() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }
        private async Task<(List<IncorprationDelaysResponse>, Error?)> BindViewBegForIncorporationDelay(MannerEstimationStep30ViewModel model)
        {
            (ManureType? manureType, Error? error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            bool isLiquid = manureType?.IsLiquid ?? false;
            string applicableFor = isLiquid ? Resource.lblL : Resource.lblS;
            if (manureType?.Id == (int)NMP.Commons.Enums.ManureTypes.PoultryManure)
            {
                applicableFor = Resource.lblP;
            }

            if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials ||
                model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
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
            if (model.IncorporationMethodId == (int)NMP.Commons.Enums.IncorporationMethod.NotIncorporated ||
                model.IncorporationMethodId == (int)NMP.Commons.Enums.IncorporationMethod.ShallowInjection ||
                model.IncorporationMethodId == (int)NMP.Commons.Enums.IncorporationMethod.DeepInjection)
            {
                applicableFor = Resource.lblNull;
            }
            (List<IncorprationDelaysResponse> incorporationDelaysList, error) = await _mannerLogic.FetchIncorporationDelaysByMethodIdAndApplicableFor(model.IncorporationMethodId ?? 0, applicableFor);
            if (error == null && incorporationDelaysList.Count > 0)
            {
                ViewBag.IncorporationDelaysList = incorporationDelaysList;
            }
            return (incorporationDelaysList, error);
        }
        [HttpGet]
        public async Task<IActionResult> IncorporationDelay()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IncorporationDelay() action called");

            MannerEstimationStep30ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep30();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in IncorporationDelay() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            _mannerEstimationLogic.SetMannerEstimationStep30(model);
            (List<IncorprationDelaysResponse> incorporationDelaysList, Error? error) = await BindViewBegForIncorporationDelay(model);
            if (error != null)
            {
                TempData["IncorporationMethodError"] = error.Message;
                return RedirectToAction("IncorporationMethod");
            }
            if (incorporationDelaysList.Count == 1)
            {
                model.IncorporationDelayId = incorporationDelaysList[0].ID;
                _mannerEstimationLogic.SetMannerEstimationStep30(model);
                return RedirectToAction("ConditionsAffectingNutrients");
            }
            _mannerEstimationLogic.SetMannerEstimationStep30(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncorporationDelay(MannerEstimationStep30ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  IncorporationDelay() post action called");
            try
            {
                if (!model.IncorporationDelayId.HasValue)
                {
                    ModelState.AddModelError("IncorporationDelayId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep30();
                    (_, Error? error) = await BindViewBegForIncorporationDelay(model);
                    if (error != null)
                    {
                        TempData["IncorporationDelayError"] = error.Message;
                    }
                    model = _mannerEstimationLogic.GetMannerEstimationStep30();
                    return View(model);
                }

                _mannerEstimationLogic.SetMannerEstimationStep30(model);

                return RedirectToAction("ConditionsAffectingNutrients");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in IncorporationDelay() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in IncorporationDelay() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpGet]
        public async Task<IActionResult> Name()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Name() action called");
            MannerEstimationStep31ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep31();
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Name(MannerEstimationStep31ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmName() post action called");
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);
            await ValidationForName(model);
            model = _mannerEstimationLogic.SetMannerEstimationStep31(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model = _mannerEstimationLogic.GetMannerEstimationStep31();

            string action = _farmNameKey;
            if (model.IsCopyEstimate.HasValue && model.IsCopyEstimate.Value)
            {

                (int newEstimationId, Error? error) = await _mannerEstimationLogic.CopyMannerEstimation(model.MannerEstimationId ?? 0, model.Name);
                if (newEstimationId == 0 || error != null)
                {
                    TempData["CopyFromEstimates"] = Resource.MsgWeCounldNotCopyMannerEstimation;
                    return View(model);
                }

                if (newEstimationId > 0)
                {
                    return RedirectToAction("MannerEstimationResult", new
                    {
                        q = _mannerEstimationProtector.Protect(
                         newEstimationId.ToString()),
                        r = _mannerEstimationProtector.Protect(Resource.lblTrue)
                    });
                }
            }
            else 
            {
                List<SelectListItem> farmsWithFields = await BindAllFarmList();
                if (farmsWithFields.Count > 0)
                {
                    action = "CopyExistingFarmAndFieldDetails";
                }
            }
            return RedirectToAction(action);
        }

        private async Task ValidationForName(MannerEstimationStep31ViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheName);
            }

            Claim? claim = HttpContext.User.FindFirst(_organisationId);
            string orgId = claim != null ? claim.Value : Guid.Empty.ToString();
            Guid.TryParse(orgId, out Guid organisationId);
            bool isExist = await _mannerEstimationLogic.FetchIsExistMannerEstimationsByOrgIdAndName(organisationId, model.Name);
            if (isExist)
            {
                ModelState.AddModelError("Name", Resource.MsgNameAlreadyExist);
            }
        }
        private Guid GetOrganisationId()
        {
            Claim? claim = HttpContext.User.FindFirst(_organisationId);
            string orgId = claim != null ? claim.Value : Guid.Empty.ToString();
            Guid.TryParse(orgId, out Guid organisationId);
            return organisationId;
        }
        private async Task<int> BuildAutumnCropNitrogenUptakeAsync(MannerEstimationStep32ViewModel model)
        {

            var (link, _) = await _organicManureLogic
                .FetchCropTypeLinkingByCropTypeId(model.CropTypeId.Value);

            var payload = new
            {
                cropTypeId = link.MannerCropTypeID,
                applicationMonth = model.ApplicationDate.Value.Month
            };

            string json = JsonConvert.SerializeObject(payload);

            var (uptake, _) = await _organicManureLogic.FetchAutumnCropNitrogenUptake(json);

            return uptake.value;
        }
        private IActionResult? HandleError(Error? error, MannerEstimationStep32ViewModel model)
        {
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.Error = error.Message;
                return View(model);
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> AutumnCropNitrogenUptake()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AutumnCropNitrogenUptake() action called");
            MannerEstimationStep32ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep32();

            ViewBag.FieldName = model.FieldName;
            ViewBag.CropTypeName = model.CropTypeName;



            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutumnCropNitrogenUptake(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AutumnCropNitrogenUptake() post action called");
            if (!ModelState.IsValid)
            {
                ReplaceNumericError(_autumnCropNitrogenUptakeKey, _autumnCropNitrogenUptakeKey, Resource.lblAutumnCropNitrogenNUptake);
            }

            if (model.AutumnCropNitrogenUptake == null)
            {
                ModelState.AddModelError(
                    _autumnCropNitrogenUptakeKey,
                    Resource.MsgEnterAValueBeforeContinue);
            }
            else
            {
                var value = model.AutumnCropNitrogenUptake.Value;

                if (value < 0)
                {
                    ModelState.AddModelError(
                        _autumnCropNitrogenUptakeKey,
                        Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }

            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                ViewBag.FieldName = model.FieldName;
                ViewBag.CropTypeName = model.CropTypeName;
                return View(_autumnCropNitrogenUptakeKey, model);
            }


            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);



        }

        [HttpGet]
        public async Task<IActionResult> SoilDrainageEndDate()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilDrainageEndDate() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilDrainageEndDate(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilDrainageEndDate() post action called");
            ValidateSoilDrainageEndDate();

            AddErrorIfNull(model.SoilDrainageEndDate, _soilDrainageEndDateKey, Resource.MsgEnterADateBeforeContinuing);
            ValidateMinMaxSoilDrainageDate(model);
            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return View(_soilDrainageEndDateKey, model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        private void ValidateMinMaxSoilDrainageDate(MannerEstimationStep32ViewModel model)
        {
            if (model.SoilDrainageEndDate == null)
            {
                return;
            }

            var date = model.SoilDrainageEndDate.Value;

            if (DateTime.TryParseExact(
                    date.Date.ToString(),
                    "dd-MM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                ModelState.AddModelError(
                    _soilDrainageEndDateKey,
                    Resource.MsgEnterValidDate);
            }

            if (!(date.Month >= (int)NMP.Commons.Enums.Month.January && date.Month <= (int)NMP.Commons.Enums.Month.April))
            {
                ModelState.AddModelError(
                    _soilDrainageEndDateKey,
                    Resource.MsgSoilDrainageEndDate1stJan30Apr);
            }
        }
        private void ValidateSoilDrainageEndDate()
        {
            if (ModelState.IsValid || !ModelState.ContainsKey(_soilDrainageEndDateKey))
            {
                return;
            }

            var errors = ModelState[_soilDrainageEndDateKey]?.Errors;

            if (errors == null || errors.Count == 0)
            {
                return;
            }

            var dateError = errors[0].ErrorMessage;

            if (dateError == string.Format(Resource.MsgDateMustBeARealDate, _soilDrainageEndDateKey))
            {
                errors.Clear();
                errors.Add(Resource.MsgEnterValidDate);
                return;
            }

            var missingDateMessages = new[]
            {
        string.Format(Resource.MsgDateMustIncludeAMonth, _soilDrainageEndDateKey),
        string.Format(Resource.MsgDateMustIncludeAMonthAndYear, _soilDrainageEndDateKey),
        string.Format(Resource.MsgDateMustIncludeADayAndYear, _soilDrainageEndDateKey),
        string.Format(Resource.MsgDateMustIncludeAYear, _soilDrainageEndDateKey),
        string.Format(Resource.MsgDateMustIncludeADay, _soilDrainageEndDateKey),
        string.Format(Resource.MsgDateMustIncludeADayAndMonth, _soilDrainageEndDateKey)
    };

            if (missingDateMessages.Contains(dateError))
            {
                errors.Clear();
                errors.Add(Resource.MsgTheDateMustInclude);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RainfallWithinSixHour()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} RainfallWithinSixHour() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();

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
        public async Task<IActionResult> RainfallWithinSixHour(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} RainfallWithinSixHour() post action called");
            AddErrorIfNull(model.RainfallWithinSixHoursId, "RainfallWithinSixHoursId", Resource.MsgSelectAnOptionBeforeContinuing);
            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return View("RainfallWithinSixHour", model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfall()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  EffectiveRainfall() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EffectiveRainfall(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  EffectiveRainfall() post action called");
            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return View("EffectiveRainfall", model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfallManual()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} EffectiveRainfallManual() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EffectiveRainfallManual(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} EffectiveRainfallManual() post action called");


            AddErrorIfNull(model.TotalRainfall, _totalRainfallKey, Resource.MsgEnterRainfallAmountBeforeContinuing);
            ValidationEffectiveRainfall();
            if (model.TotalRainfall != null && (model.TotalRainfall < 0 || model.TotalRainfall > 9999))
            {
                ModelState.AddModelError(_totalRainfallKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblEffectiveRainfall, 0, 9999));

            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return View("EffectiveRainfallManual", model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        private void ValidationEffectiveRainfall()
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_totalRainfallKey))
            {
                var RainfallError = ModelState[_totalRainfallKey]?.Errors.Count > 0 ?
                                ModelState[_totalRainfallKey]?.Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_totalRainfallKey].RawValue, _totalRainfallKey)))
                {
                    ModelState[_totalRainfallKey]?.Errors.Clear();
                    if (ModelState[_totalRainfallKey].RawValue.ToString().Contains("."))
                    {
                        ModelState[_totalRainfallKey]?.Errors.Add(Resource.MsgIfUserEnterDecimalValueInRainfall);
                    }
                    else
                    {
                        ModelState[_totalRainfallKey]?.Errors.Add(string.Format(Resource.MsgEnterValueInBetween, Resource.lblEffectiveRainfall, 0, 9999));
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Windspeed()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Windspeed() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();
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
        public async Task<IActionResult> Windspeed(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Windspeed() post action called");
            AddErrorIfNull(model.WindspeedId, "WindspeedID", Resource.MsgSelectAWindConditionBeforeContinuing);

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return await Task.FromResult(View("Windspeed", model));
            }


            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> TopsoilMoisture()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} TopsoilMoisture() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();

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
        public async Task<IActionResult> TopsoilMoisture(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  TopsoilMoisture() post action called");
            AddErrorIfNull(model.MoistureTypeId, "MoistureTypeId", Resource.MsgSelectATopsoilWetnessConditionBeforeContinuing);

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep32();
                return View("TopsoilMoisture", model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep32(model);
            return RedirectToAction(_conditionsAffectingNutrients);
        }


        [HttpGet]
        public async Task<IActionResult> ConditionsAffectingNutrients()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ConditionsAffectingNutrients() action called");
            MannerEstimationStep32ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep32();
            Error error = new Error();
            try
            {
                //Autumn crop Nitrogen uptake
                model.AutumnCropNitrogenUptake ??= await BuildAutumnCropNitrogenUptakeAsync(model);

                //Soil drainage end date
                if (model.SoilDrainageEndDate == null)
                {
                    BindSoilDraingeDate(model);
                }

                // Rainfall within 6 hours
                RainTypeResponse rainType;

                if (model.RainfallWithinSixHoursId.HasValue)
                {
                    (rainType, error) = await _organicManureLogic
                        .FetchRainTypeById(model.RainfallWithinSixHoursId.Value);
                }
                else
                {
                    (rainType, error) = await _organicManureLogic
                        .FetchRainTypeDefault();
                }

                var result = HandleError(error, model);
                if (result != null)
                {
                    return result;
                }
                model.RainfallWithinSixHoursId ??= rainType.ID;
                model.RainfallWithinSixHours = rainType.Name;


                if (model.PostCode != null)
                {
                    // Effective rainfall after application
                    string halfPostCode = model.PostCode[..4].Trim();


                    if (model.ApplicationDate.HasValue &&
                        model.SoilDrainageEndDate.HasValue &&
                        model.TotalRainfall == null)
                    {
                        var rainfallPostCodeApplication = new
                        {
                            applicationDate = model.ApplicationDate.Value.ToString(_dateStringLiteral),
                            endOfSoilDrainageDate = model.SoilDrainageEndDate.Value.ToString(_dateStringLiteral),
                            climateDataPostcode = halfPostCode
                        };

                        model.TotalRainfall = await _organicManureLogic
                            .FetchRainfallByPostcodeAndDateRange(
                                JsonConvert.SerializeObject(rainfallPostCodeApplication));
                    }
                }


                // Windspeed during application
                WindspeedResponse? windspeed;

                (windspeed, Error? errorForWindSpeed) = model.WindspeedId.HasValue
                    ? await _organicManureLogic.FetchWindspeedById(model.WindspeedId.Value)
                    : await _organicManureLogic.FetchWindspeedDataDefault();

                result = HandleError(errorForWindSpeed, model);
                if (result != null)
                {
                    return result;
                }
                model.WindspeedId = windspeed?.ID;
                model.Windspeed = windspeed?.Name;


                // Topsoil moisture
                MoistureTypeResponse moistureType;

                if (model.MoistureTypeId.HasValue)
                {
                    (moistureType, error) = await _organicManureLogic
                        .FetchMoisterTypeById(model.MoistureTypeId.Value);
                }
                else
                {
                    (moistureType, error) = await _organicManureLogic
                        .FetchMoisterTypeDefaultByApplicationDate(
                            model.ApplicationDate!.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
                }

                result = HandleError(error, model);
                if (result != null)
                {
                    return result;
                }
                model.MoistureTypeId ??= moistureType.ID;
                model.MoistureType = moistureType.Name;
                _mannerEstimationLogic.SetMannerEstimationStep32(model);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manner Estimation Controller : Exception in ConditionsAffectingNutrients() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConditionsAffectingNutrients(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ConditionsAffectingNutrients() post action called");
            if (!ModelState.IsValid)
            {
                return View(_conditionsAffectingNutrients, model);
            }
            try
            {
                Guid organisationId = GetOrganisationId();
                (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error)
                   = await _mannerEstimationLogic.AddMannerEstimation(organisationId);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["ConditionsAffectingNutrientsError"] = error.Message;
                    return View(model);
                }

                if (mannerEstimationApplicationResult != null && mannerEstimationApplicationResult.MannerEstimationID != null)
                {
                    return RedirectToAction("MannerEstimationResult", new
                    {
                        q = _mannerEstimationProtector.Protect(
                          mannerEstimationApplicationResult.MannerEstimationID.ToString()),
                        r = _mannerEstimationProtector.Protect(Resource.lblTrue)
                    });
                }
                return View(model);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in ConditionsAffectingNutrients() post action");
                TempData["ConditionsAffectingNutrientsError"] = ex.Message;
                return View(model);
            }

        }
        private static void BindSoilDraingeDate(MannerEstimationStep32ViewModel model)
        {

            var applicationDate = model.ApplicationDate.Value;

            var targetYear = applicationDate.Month >= 8
                ? applicationDate.AddYears(1).Year
                : applicationDate.Year;

            model.SoilDrainageEndDate = new DateTime(
                targetYear,
                (int)NMP.Commons.Enums.Month.March,
                31,
                0,
                0,
                0,
                DateTimeKind.Utc
            );

        }

    }
}
