using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;

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

        private const string _updateFieldOrCropDataActionName = "UpdateFieldOrCropData";
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
        private const string _nutrientProductErrorKey = "NutrientProductError";
        private const string _mannerEstimationResultKey = "MannerEstimationResult";
        private const string _mannerEstimationResultErrorKey = "MannerEstimationResultError";
        private const string _nitrogenKey = "N";
        private const string _ammoniaKey = "NH4N";
        private const string _uricAcidKey = "UricAcid";
        private const string _nO3NKey = "NO3N";
        private const string _p2O5Key = "P2O5";
        private const string _k2OKey = "K2O";
        private const string _sO3Key = "SO3";
        private const string _mgOKey = "MgO";
        private const string _updateApplicationDataActionName = "UpdateApplicationData";
        private const string _mannerHubPageAction = "MannerHubPage";
        private const string _dateFormat = "d MMMM yyyy";

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> MannerHubPage(string? q, string? r, string? s)
        {
            RemoveMannerEstimationSession();
            if (!string.IsNullOrWhiteSpace(q))
            {
                int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                HttpContext.Session.SetString("current_manner_estimate_manner_farm_id", q);
                (MannerFarmViewModel? mannerFarm, _) = await _mannerEstimationLogic.FetchMannerFarmById(mannerFarmId);
                if (mannerFarm != null)
                {
                    ViewBag.FarmName = mannerFarm.Name;
                }
                (List<MannerEstimationSummaryViewModel>? mannerEstimateList, Error? error) = await _mannerEstimationLogic.FetchMannerEstimateByFarmId(mannerFarmId);
                if (string.IsNullOrWhiteSpace(error?.Message) && mannerEstimateList?.Count > 0)
                {
                    BindEncryptedIdForMannerHubPage(mannerEstimateList);
                    ViewBag.EncryptedMannerFarmId = _mannerEstimationProtector.Protect(mannerFarmId.ToString());
                    ViewBag.MannerEstimations = mannerEstimateList.OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn).ToList();
                }

                await BindMannerEstimationSessionForHubPage(q, mannerFarmId);
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.Success = _mannerEstimationProtector.Unprotect(r);
            }
            if (!string.IsNullOrWhiteSpace(s))
            {
                ViewBag.SuccessForCopyEstimate = _mannerEstimationProtector.Unprotect(s);
            }

            if (!string.IsNullOrWhiteSpace(q) || !string.IsNullOrWhiteSpace(r))
            {
                return View();
            }
            return RedirectToAction("Name");
        }

        private async Task BindMannerEstimationSessionForHubPage(string? q, int mannerFarmId)
        {
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimationViewModel == null)
            {
                mannerEstimationViewModel = new MannerEstimationViewModel();
                mannerEstimationViewModel.IsNewEstimate = true;
                mannerEstimationViewModel.MannerFarmId = mannerFarmId;
                mannerEstimationViewModel.EncryptedMannerFarmId = q;
                _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);

                await _mannerEstimationLogic.BindFarmDataForMannerEstimateUpdateOrCreate(mannerFarmId);
            }
        }

        private void BindEncryptedIdForMannerHubPage(List<MannerEstimationSummaryViewModel> mannerEstimateList)
        {
            foreach (var estimation in mannerEstimateList)
            {
                estimation.EncryptedId = _mannerEstimationProtector.Protect(estimation.ID.ToString());
            }
        }


        public async Task<IActionResult> MannerEstimationCancel()
        {
            _logger.LogTrace("MannerEstimation Controller : MannerEstimationCancel() action called");

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimationViewModel != null && !string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerFarmId))
            {
                return RedirectToAction(_mannerHubPageAction, new { q = mannerEstimationViewModel.EncryptedMannerFarmId });
            }
            return RedirectToAction("MannerFarmList");
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
            (_, bool isAnyFarmExists) = await BindAllFarmList();
            if (isAnyFarmExists)
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

            await ValidationForFarmName(model);
            (_, bool isAnyFarmExists) = await BindAllFarmList();
            if (isAnyFarmExists)
            {
                model.IsFarmCopied = true;
            }

            if (!ModelState.IsValid)
            {
                model = _mannerEstimationLogic.GetMannerEstimationStep1();
                model.IsFarmCopied = isAnyFarmExists;
                return View(model);
            }

            _mannerEstimationLogic.SetMannerEstimationStep1(model);

            return RedirectToAction("Country");
        }

        private async Task ValidationForFarmName(MannerEstimationStep1ViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FarmName))
            {
                ModelState.AddModelError(_farmNameKey, Resource.MsgEnterTheFarmName);
            }
            Guid organisationId = GetOrganisationId();
            bool isExist = await _mannerEstimationLogic.FetchIsExistMannerFarmByOrgIdAndName(organisationId, model.FarmName);
            if (isExist)
            {
                ModelState.AddModelError(_farmNameKey, Resource.MsgFarmNameAlreadyExist);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Country(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  Country() action called");

            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

                await _mannerEstimationLogic.SetMannerEstimationStep2(model);
                return RedirectToAction("PostCode");
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
        public async Task<IActionResult> PostCode(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  PostCode() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

                await _mannerEstimationLogic.SetMannerEstimationStep3(model);
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
        public async Task<IActionResult> AverageAnnualRainfall(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfall() action called");

            try
            {
                if (!string.IsNullOrWhiteSpace(q))
                {
                    await BindFarmFieldOrCropDataUpdate(q);
                }
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
            await _mannerEstimationLogic.SetMannerEstimationStep4(model);

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
        public async Task<IActionResult> FieldName(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FieldName() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

            _mannerEstimationLogic.SetMannerEstimationStep5(model);

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("NVZField");
        }

        [HttpGet]
        public async Task<IActionResult> NVZField(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} NVZField() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

            _mannerEstimationLogic.SetMannerEstimationStep6(model);

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("TopSoil");
        }

        [HttpGet]
        public async Task<IActionResult> SoilType(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilType() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }

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

            return model.IsCheckAnswer ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("CropGroup");
        }
        [HttpGet]
        public async Task<IActionResult> CropGroup(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropGroup() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsCropGroupChange) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("CropType");
        }

        [HttpGet]
        public async Task<IActionResult> CropType(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropType() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }

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

            if (model.CropTypeId != null && Enum.IsDefined(typeof(NMP.Commons.Enums.EarlyOrLateSownCropTypes), model.CropTypeId) && model.IsCropTypeChange)
            {
                return RedirectToAction("SowingDate");
            }

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("ManureGroup");
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

            return model.IsCheckAnswer ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("ManureGroup");
        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup(string? q, string? r, string? s)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureGroup() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

            if (!string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
            {
                model.EncryptedMannerEstimationId = r;
                model.IsComingForAddNewApplication = true;
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (mannerEstimationViewModel != null)
                {
                    mannerEstimationViewModel.EncryptedMannerEstimationId = r;
                    mannerEstimationViewModel.IsComingForAddNewApplication = true;
                    _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);
                }
                (MannerEstimation? estimate, error) = await _mannerEstimationLogic.FetchMannerEstimateById(Convert.ToInt32(_mannerEstimationProtector.Unprotect(r)));
                if (error == null && estimate != null)
                {
                    (MannerFarmViewModel? mannerFarm, error) = await _mannerEstimationLogic.FetchMannerFarmById(estimate.MannerFarmID.Value);
                    if (error == null && mannerFarm != null)
                    {
                        model.CountryId = mannerFarm.CountryID ?? 0;
                        model.CropTypeId = estimate.CropTypeID ?? 0;
                        model.IsFarmOrganic = mannerFarm.RegisteredOrganicProducer;
                        model.IsWithinNVZ = estimate.IsWithinNVZ;
                    }
                }



                await _mannerEstimationLogic.SetMannerEstimationStep11(model);
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

            model = await _mannerEstimationLogic.SetMannerEstimationStep11(model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedMannerEstimationId) && !model.IsComingForAddNewApplication && !model.IsManureGroupIdChange)
            {
                return RedirectToAction(_updateApplicationDataActionName);
            }
            return RedirectToAction("ManureType");
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
                selectListItems = manureTypeList.OrderBy(m => m.SortOrder).Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name
                }).ToList();
            }
            return (selectListItems, error);
        }
        [HttpGet]
        public async Task<IActionResult> ManureType(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureType() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange && !model.IsComingForAddNewApplication) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction("ApplicationDate");
        }
        public static (DateTime StartDate, DateTime EndDate) GetHarvestYear(DateTime date)
        {
            if (date.Month >= 8) // August to December
            {
                return (
                    new DateTime(date.Year, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(date.Year + 1, 7, 31, 0, 0, 0, DateTimeKind.Utc)
                );
            }
            else // January to July
            {
                return (
                    new DateTime(date.Year - 1, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(date.Year, 7, 31, 0, 0, 0, DateTimeKind.Utc)
                );
            }
        }

        public static (bool, int?, int?) ValidateApplicationDate(DateTime applicationDate, DateTime? sowingDate, DateTime? otherApplication)
        {
            // If sowing date or other application date is available, application date must be in the same harvest year
            if (sowingDate.HasValue)
            {
                var harvestYear = GetHarvestYear(sowingDate.Value);
                return (applicationDate >= harvestYear.StartDate &&
                       applicationDate <= harvestYear.EndDate, harvestYear.StartDate.Year, harvestYear.EndDate.Year);
            }
            if (otherApplication.HasValue)
            {
                var harvestYear = GetHarvestYear(otherApplication.Value);

                return (applicationDate >= harvestYear.StartDate &&
                       applicationDate <= harvestYear.EndDate, harvestYear.StartDate.Year, harvestYear.EndDate.Year);
            }

            return (true, null, null);
        }
        [HttpGet]
        public async Task<IActionResult> ApplicationDate(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationDate() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

            MannerEstimationStep13ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep13();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in SoilType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            MannerEstimationViewModel mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimationViewModel != null && !string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerEstimationId))
            {
                await BindFarmFieldOrCropDataUpdate(mannerEstimationViewModel.EncryptedMannerEstimationId);
                model = _mannerEstimationLogic.GetMannerEstimationStep13();
            }

            var (manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
            if (manureType?.HighReadilyAvailableNitrogen == true && model.IsWithinNVZ == true)
            {
                bool isPerennial = await _cropLogic.FetchIsPerennialByCropTypeId(model.CropTypeId ?? 0);
                int fieldType = model.CropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;

                bool isSandyShallowSoil = _mannerEstimationLogic.CheckSandyShallowByTopSoilSubSoilId(model.TopSoilId ?? 0, model.SubSoilId ?? 0, model.CountryId);
                if (string.IsNullOrEmpty(error?.Message))
                {
                    string closedPeriod = Functions.GetMannerClosedPeriod(isSandyShallowSoil, fieldType, model.SowingDate, model.CountryId, model.CropGroupId ?? 0, model.CropTypeId ?? 0, isPerennial);
                    model.ClosedPeriod = closedPeriod;
                }

                model.IsWarningMsgNeedToShow = false;
                model.IsClosedPeriodWarning = false;
                model.IsApplicationJulyToSeptWarning = false;
                model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false;
            }

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
                await ValidateApplicationDate(formData);

                if (!ModelState.IsValid)
                {
                    model = _mannerEstimationLogic.GetMannerEstimationStep13();
                    model.ApplicationDate = formData.ApplicationDate;
                    return View(model);
                }
                model = _mannerEstimationLogic.GetMannerEstimationStep13();
                if (model.ApplicationDate != formData.ApplicationDate)
                {
                    model.IsWarningMsgNeedToShow = false;
                }
                model.ApplicationDate = formData.ApplicationDate;
                var (manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
                model = _mannerEstimationLogic.SetMannerEstimationStep13(model);
                //non organic farm, high N, NVZ
                if (!string.IsNullOrWhiteSpace(model.ClosedPeriod) && string.IsNullOrWhiteSpace(error?.Message))
                {
                    int harvestYear = GetHarvestYearFromApplicationDate(model.ApplicationDate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));

                    error = await CheckApplicationDateWarnings(model, manureType, harvestYear, true);
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
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange && model.IsApplicationDateChange)
                {
                    return RedirectToAction(_conditionsAffectingNutrients);
                }

                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction("ApplicationMethod");
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Manner Estimation Controller  : Exception in ApplicationDate() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }

        }

        private async Task ValidateApplicationDate(MannerEstimationStep13ViewModel formData)
        {
            if (formData.ApplicationDate != null)
            {
                MannerEstimationStep13ViewModel mannerEstimationStep13View = _mannerEstimationLogic.GetMannerEstimationStep13();
                if (mannerEstimationStep13View.SowingDate != null)
                {
                    var (isValid, startDate, endDate) = ValidateApplicationDate(formData.ApplicationDate.Value, mannerEstimationStep13View.SowingDate, null);
                    if (!isValid)
                    {
                        ModelState.AddModelError(_applicationDateKey, string.Format(Resource.MsgApplicationDateHarvestYearValidation, startDate, endDate));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(mannerEstimationStep13View.EncryptedMannerEstimateId))
                {
                    int mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep13View.EncryptedMannerEstimateId));
                    (MannerEstimationResultResponse? mannerEstimationResultResponse, _) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimationId);
                    if (mannerEstimationResultResponse?.MannerEstimationApplication.Count >= 1)
                    {
                        var (isValid, startDate, endDate) = ValidateApplicationDate(formData.ApplicationDate.Value, null, mannerEstimationResultResponse.MannerEstimationApplication[0]?.ApplicationDate);
                        if (!isValid)
                        {
                            ModelState.AddModelError(_applicationDateKey, string.Format(Resource.MsgApplicationDateHarvestYearValidation, startDate, endDate));

                        }
                    }
                }
            }
        }

        private async Task<Error?> CheckApplicationDateWarnings(
    MannerEstimationStep13ViewModel model,
    ManureType? manureType,
    int harvestYear,
    bool persistToSession = true)
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

            if (model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland)
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

            if (persistToSession)
            {
                _mannerEstimationLogic.SetMannerEstimationStep13(model);
            }

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
            int? mannerEstimationId = null;
            if (model.MannerEstimationId == null && !string.IsNullOrWhiteSpace(model.EncryptedMannerEstimateId))
            {
                mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimateId));
            }
            bool? isWithinClosedPeriodAndFebruary = WarningWithinPeriod.CheckEndClosedPeriodAndFebruary(model.ApplicationDate.Value, model.ClosedPeriod);
            if (isWithinClosedPeriodAndFebruary == true)
            {
                (List<MannerEstimationApplication> mannerApplications, error) = await _mannerEstimationLogic.FetchMannerApplicationsByMannerEstimationId(mannerEstimationId ?? 0);
                if (mannerApplications.Count > 0)
                {
                    var mannerApplicationWithin21Days = mannerApplications.First(x => (model.ApplicationDate.Value - x.ApplicationDate).TotalDays <= 21);

                    bool isSlurry = CommonHelpers.IsSlurryType(mannerApplicationWithin21Days.ManureTypeID);
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


        private async Task<Error?> HandleLivestockManureRule(MannerEstimationStep13ViewModel model)
        {
            int? mannerEstimationId = null;
            if (model.MannerEstimationId == null && !string.IsNullOrWhiteSpace(model.EncryptedMannerEstimateId))
            {
                mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimateId));
            }
            (List<MannerEstimationApplication> mannerApplications, Error? error) = await _mannerEstimationLogic.FetchMannerApplicationsByMannerEstimationId(mannerEstimationId ?? 0);
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

                (_, bool isAnyFarmExists) = await BindAllFarmList(Resource.lblTrue);
                if (isAnyFarmExists)
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
                    await BindAllFarmList(Resource.lblTrue);
                    return View(model);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep14(model);
                string action = _farmNameKey;
                if (model.IsCopyExistingFarmAndFieldDetails.HasValue && model.IsCopyExistingFarmAndFieldDetails.Value)
                {
                    action = "FarmToCopy";
                }

                return model.IsCheckAnswer ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(action);
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

        private async Task<(List<SelectListItem>, bool)> BindAllFarmList(string? q = null)
        {
            Guid organisationId = GetOrganisationId();
            List<SelectListItem> farmsWithFields = new List<SelectListItem>();
            (List<Farm> farmList, _) = await _farmLogic.FetchFarmByOrgIdAsync(organisationId);
            bool isAnyFarmExists = farmList.Any();
            if (!string.IsNullOrWhiteSpace(q))
            {
                return (farmsWithFields, isAnyFarmExists);
            }
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
            return (farmsWithFields, isAnyFarmExists);
        }

        [HttpGet]
        public async Task<IActionResult> FarmToCopy()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FarmToCopy() action called");
            MannerEstimationStep15ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep15();
            try
            {
                (List<SelectListItem> farmsWithFields, bool isAnyFarmExists) = await BindAllFarmList();
                if (isAnyFarmExists)
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
                (List<SelectListItem> farmsWithFields, bool isAnyFarmExists) = await BindAllFarmList();
                if (!model.FarmId.HasValue)
                {
                    ModelState.AddModelError("FarmId", string.Format(Resource.MsgSelectAnJourneyName, Resource.lblFarm));
                }
                else
                {
                    string farmName = farmsWithFields?.FirstOrDefault(x => x.Value == model.FarmId.ToString())?.Text;
                    Guid organisationId = GetOrganisationId();
                    bool isExist = await _mannerEstimationLogic.FetchIsExistMannerFarmByOrgIdAndName(organisationId, farmName);
                    if (isExist)
                    {
                        ModelState.AddModelError("FarmId", Resource.MsgFarmNameAlreadyExist);
                    }
                }
                if (!ModelState.IsValid)
                {
                    MannerEstimationStep15ViewModel mannerEstimationStep15ViewModel = _mannerEstimationLogic.GetMannerEstimationStep15();
                    if (mannerEstimationStep15ViewModel != null)
                    {
                        mannerEstimationStep15ViewModel.FarmId = model.FarmId;
                    }
                    if (isAnyFarmExists)
                    {
                        ViewBag.FarmList = farmsWithFields;
                    }
                    return View(mannerEstimationStep15ViewModel);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep15(model);

                return model.IsCheckAnswer ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("FieldToCopy");
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "MannerEstimation  Controller :  HttpRequestException in FarmToCopy() action : {Message} {StackTrace}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller :   Exception in FarmToCopy() post action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
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
                return model.IsCheckAnswer ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("CropGroup");
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
        public async Task<IActionResult> IsFarmOrganic(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  IsFarmOrganic() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

                _mannerEstimationLogic.SetMannerEstimationStep17(model);

                return RedirectToAction("FieldName");
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
        public async Task<IActionResult> TopSoil(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  TopSoil() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

                _mannerEstimationLogic.SetMannerEstimationStep18(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("SubSoil");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "TopSoil");
            }

        }

        private IActionResult HandleException(Exception ex, string actionName)
        {
            if (ex is HttpRequestException hre)
            {
                _logger.LogError(
                    hre,
                    "MannerEstimation Controller : HttpRequestException in {Action} action : {Message} {StackTrace}",
                    actionName,
                    hre.Message,
                    hre.StackTrace);

                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }

            _logger.LogError(
                ex,
                "MannerEstimation Controller : Exception in {Action} post action : {Message} {StackTrace}",
                actionName,
                ex.Message,
                ex.StackTrace);

            return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
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
        public async Task<IActionResult> SubSoil(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  SubSoil() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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
                _mannerEstimationLogic.SetMannerEstimationStep19(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("CropGroup");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "TopSoil");
            }

        }
        [HttpGet]
        public async Task<IActionResult> SowingDate(string? q)
        {
            _logger.LogTrace("Crop Controller : SowingDate action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }
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

                model = await _mannerEstimationLogic.SetMannerEstimationStep20(model);
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId)) ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("ManureGroup");

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
        public async Task<IActionResult> ApplicationMethod(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationMethod() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

                model = await _mannerEstimationLogic.SetMannerEstimationStep23(model);
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange && model.IsApplicationMethodChange)
                {
                    return RedirectToAction("IncorporationMethod");
                }

                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction("DefaultNutrientValues");
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
        public async Task<IActionResult> DefaultNutrientValues(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} DefaultNutrientValues() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_applicationRateMethodAction);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "MannerEstimation  Controller :   HttpRequestException in DefaultNutrientValues() action : {Message} {StackTrace}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller :   Exception in DefaultNutrientValues() post action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
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
        private void ReplaceNumericRangeError(string key, string validationLabel, string displayLabel)
        {
            if (ModelState.ContainsKey(key))
            {
                var errorMessage = ModelState[key].Errors[0].ErrorMessage;
                string expectedMessage = string.Format(Resource.lblEnterNumericValue, ModelState[key].RawValue, validationLabel);
                if (string.Equals(errorMessage, expectedMessage))
                {
                    ModelState[key].Errors.Clear();
                    ModelState[key].Errors.Add(string.Format(Resource.MsgEnterAValueBetween0And9999, displayLabel));
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
        public async Task<IActionResult> ManualNutrientValues(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManualNutrientValues() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

                model = await _mannerEstimationLogic.SetMannerEstimationStep25(model);



                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_applicationRateMethodAction);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ManualNutrientValues");
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
            ReplaceNumericError(_nitrogenKey, Resource.lblN, Resource.lblTotalNitrogen);
            ReplaceNumericError(_ammoniaKey, Resource.lblNH4N, Resource.lblAmmonium);
            ReplaceNumericError(_uricAcidKey, Resource.lblUricAcidForError, Resource.lblUricAcid);
            ReplaceNumericError(_nO3NKey, Resource.lblNO3N, Resource.lblNitrogen);
            ReplaceNumericError(_p2O5Key, Resource.lblP2O5, Resource.lblTotalPhosphate);
            ReplaceNumericError(_k2OKey, Resource.lblK2O, Resource.lblTotalPotassium);
            ReplaceNumericError(_sO3Key, Resource.lblSO3, Resource.lblTotalSulphur);
            ReplaceNumericError(_mgOKey, Resource.lblMgO, Resource.lblMagnesiumMgO);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationRateMethod(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationRateMethod() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

            MannerEstimationStep26ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep26();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ApplicationRateMethod() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ResetWarnings(model, true);
            model = await _mannerEstimationLogic.SetMannerEstimationStep26(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationRateMethod(MannerEstimationStep26ViewModel formData)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : ApplicationRateMethod() post action called");
            MannerEstimationStep26ViewModel model = new MannerEstimationStep26ViewModel();
            try
            {
                AddErrorIfNull(formData.ApplicationRateMethod, _applicationRateMethodAction, Resource.MsgSelectAnOptionBeforeContinuing);
                if (!ModelState.IsValid)
                {
                    formData = await _mannerEstimationLogic.GetMannerEstimationStep26();
                    return View(_applicationRateMethodAction, formData);
                }
                (bool flowControl, IActionResult value) = RedirectForApplicationRateMethod(formData);
                if (!flowControl)
                {
                    await _mannerEstimationLogic.SetMannerEstimationStep26(formData);
                    return value;
                }
                Error? error = null;
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (formData.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
                {
                    model = await _mannerEstimationLogic.GetMannerEstimationStep26();
                    model.ApplicationRateMethod = formData.ApplicationRateMethod;
                    error = await GetDefaultNitrogenRate(model, error);
                    ResetWarnings(model, false);
                    var (updatingEstimateId, updatingApplicationId) = await GetUpdatingEstimationAndApplicationId(model.EncryptedMannerEstimateId, model.EncryptedMannerApplicationsId);
                    (model, error) = await NitrogenApplicationLimitWarningMessage(model, updatingEstimateId, updatingApplicationId);
                    bool hasAnyWarning = model.IsOrgManureNfieldLimitWarning || model.IsEndClosedPeriodFebruaryWarning || model.IsStartClosedPeriodEndFebWarning;
                    if (hasAnyWarning)
                    {
                        if (!model.IsWarningMsgNeedToShow)
                        {
                            model.IsWarningMsgNeedToShow = true;
                            model = await _mannerEstimationLogic.SetMannerEstimationStep26(model);
                            return View(model);
                        }
                    }
                    else
                    {
                        ResetWarnings(model, true);
                    }
                    model.IsWarningMsgNeedToShow = false;
                    model = await _mannerEstimationLogic.SetMannerEstimationStep26(model);
                    return RedirectAfterApplicationRateMethod(mannerEstimationViewModel, model);
                }
                model = await _mannerEstimationLogic.SetMannerEstimationStep26(model);
                return RedirectAfterApplicationRateMethod(mannerEstimationViewModel, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        private IActionResult RedirectAfterApplicationRateMethod(MannerEstimationViewModel? mannerEstimationViewModel, MannerEstimationStep26ViewModel model)
        {
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange)
                ? RedirectToAction(_updateApplicationDataActionName)
                : RedirectToAction(_incorporationMethodAction);
        }

        private (bool flowControl, IActionResult value) RedirectForApplicationRateMethod(MannerEstimationStep26ViewModel model)
        {
            if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.EnterAnApplicationRate)
            {
                return (flowControl: false, value: RedirectToAction("ManualApplicationRate"));
            }
            if (model.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
            {
                return (flowControl: false, value: RedirectToAction("AreaQuantity"));
            }

            return (flowControl: true, value: null);
        }

        private async Task<Error?> GetDefaultNitrogenRate(MannerEstimationStep26ViewModel model, Error? error)
        {
            if (!IsOtherManureType(model.ManureTypeId))
            {
                (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

                if (error == null)
                {
                    model.ApplicationRate = manureType?.ApplicationRateArable;
                }
                else
                {
                    ViewBag.Error = error.Message;
                }
            }

            return error;
        }

        private static bool IsOtherManureType(int? manureId)
        {
            return manureId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials
                || manureId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials;
        }
        [HttpGet]
        public async Task<IActionResult> ManualApplicationRate(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManualApplicationRate() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

            MannerEstimationStep27ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep27();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in ManualApplicationRate() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ResetWarnings(model, true);
            model = await _mannerEstimationLogic.SetMannerEstimationStep27(model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualApplicationRate(MannerEstimationStep27ViewModel formData)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : ManualApplicationRate() post action called");
            MannerEstimationStep27ViewModel model = new MannerEstimationStep27ViewModel();
            try
            {
                AddErrorIfNull(formData.ApplicationRate, _applicationRateKey, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblApplicationRate));
                ValidateManualApplicationRate(formData);
                if (!ModelState.IsValid)
                {
                    formData = await _mannerEstimationLogic.GetMannerEstimationStep27();
                    return View(formData);
                }
                model = await _mannerEstimationLogic.GetMannerEstimationStep27();
                if (model.ApplicationRate != formData.ApplicationRate)
                {
                    model.IsWarningMsgNeedToShow = false;
                }

                model.ApplicationRate = formData.ApplicationRate;
                ResetWarnings(model, false);

                var (updatingEstimateId, updatingApplicationId) = await GetUpdatingEstimationAndApplicationId(model.EncryptedMannerEstimateId, model.EncryptedMannerApplicationsId);

                (model, Error? error) = await NitrogenApplicationLimitWarningMessage(model, updatingEstimateId, updatingApplicationId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    ViewBag.Error = error.Message;
                    return View(model);
                }

                bool hasAnyWarning = model.IsOrgManureNfieldLimitWarning || model.IsEndClosedPeriodFebruaryWarning || model.IsStartClosedPeriodEndFebWarning;
                if (hasAnyWarning)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        model = await _mannerEstimationLogic.SetMannerEstimationStep27(model);
                        return View(model);
                    }
                }
                else
                {
                    ResetWarnings(model, true);
                }
                model.IsWarningMsgNeedToShow = false;
                model = await _mannerEstimationLogic.SetMannerEstimationStep27(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_incorporationMethodAction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller : Exception in ApplicationRateMethod() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        private void ValidateManualApplicationRate(MannerEstimationStep27ViewModel formData)
        {
            if (formData.ApplicationRate != null)
            {
                if (formData.ApplicationRate < 0)
                    ModelState.AddModelError(_applicationRateKey, Resource.MsgEnterANumberWhichIsGreaterThanZero);

                if (formData.ApplicationRate > 250)
                    ModelState.AddModelError(_applicationRateKey, Resource.MsgForApplicationRate);
                if (formData.ApplicationRate != Math.Round(formData.ApplicationRate.Value, 2))
                {
                    ModelState.AddModelError(_applicationRateKey, string.Format(Resource.MsgEnterAnPropertyOnlyTwoDecimal, Resource.lblApplicationRate));
                }
            }
        }

        private static void ResetWarnings(MannerEstimationNWarningViewModel model, bool isWarningMsgNeedToShowReset)
        {
            if (isWarningMsgNeedToShowReset)
            {
                model.IsWarningMsgNeedToShow = false;
            }

            model.IsOrgManureNfieldLimitWarning = false;
            model.IsEndClosedPeriodFebruaryWarning = false;
            model.IsStartClosedPeriodEndFebWarning = false;
        }
        [HttpGet]
        public async Task<IActionResult> AreaQuantity(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AreaQuantity() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

            MannerEstimationStep28ViewModel model = await _mannerEstimationLogic.GetMannerEstimationStep28();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in AreaQuantity() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            ResetWarnings(model, true);
            model = await _mannerEstimationLogic.SetMannerEstimationStep28(model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AreaQuantity(MannerEstimationStep28ViewModel formData)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} : AreaQuantity() post action called");
            MannerEstimationStep28ViewModel model = new MannerEstimationStep28ViewModel();
            try
            {
                AddErrorIfNull(formData.AreaSpread, "AreaSpread", string.Format(Resource.MsgEnterAValidArea, Resource.lblArea));
                AddErrorIfNull(formData.ManureQuantity, "ManureQuantity", string.Format(Resource.MsgEnterAValidQuantity, Resource.lblQuantity));
                ValidateAreaQuantity(formData);

                if (!ModelState.IsValid)
                {
                    formData = await _mannerEstimationLogic.GetMannerEstimationStep28();
                    return View("AreaQuantity", formData);
                }
                formData.ApplicationRate = Math.Round((formData.ManureQuantity.Value / formData.AreaSpread.Value), 2);


                model = await _mannerEstimationLogic.GetMannerEstimationStep28();
                model.AreaSpread = formData.AreaSpread;
                model.ManureQuantity = formData.ManureQuantity;
                if (model.ManureQuantity != formData.ManureQuantity || model.AreaSpread != formData.AreaSpread)
                {
                    model.IsWarningMsgNeedToShow = false;
                }

                model.ApplicationRate = formData.ApplicationRate;
                ResetWarnings(model, false);

                var (updatingEstimateId, updatingApplicationId) = await GetUpdatingEstimationAndApplicationId(model.EncryptedMannerEstimateId, model.EncryptedMannerApplicationsId);

                (model, Error? error) = await NitrogenApplicationLimitWarningMessage(model, updatingEstimateId, updatingApplicationId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    ViewBag.Error = error.Message;
                    return View(model);
                }

                bool hasAnyWarning = model.IsOrgManureNfieldLimitWarning || model.IsEndClosedPeriodFebruaryWarning || model.IsStartClosedPeriodEndFebWarning;
                if (hasAnyWarning)
                {
                    if (!model.IsWarningMsgNeedToShow)
                    {
                        model.IsWarningMsgNeedToShow = true;
                        model = await _mannerEstimationLogic.SetMannerEstimationStep28(model);
                        return View(model);
                    }
                }
                else
                {
                    ResetWarnings(model, true);
                }
                model.IsWarningMsgNeedToShow = false;
                model = await _mannerEstimationLogic.SetMannerEstimationStep28(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_incorporationMethodAction);

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

                return RedirectToAction("CopyFromEstimates");
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


                return model.IsCheckAnswer ? RedirectToAction(_updateFieldOrCropDataActionName) : RedirectToAction("Name");
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
        public async Task<IActionResult> MannerEstimationResult(string? q, string? r, string? s)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  MannerEstimationResult() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.EncryptedEstimateId = q;
                ViewBag.EncryptedIsAddOtherApplication = _mannerEstimationProtector.Protect(Resource.lblTrue);
                int estimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                (MannerEstimationResultResponse? mannerEstimationResultResponse, Error? error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(estimateId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_mannerEstimationResultErrorKey] = error.Message;
                    return RedirectToAction(_mannerHubPageAction);
                }
                await BindViewBegForMannerEstimationResult(mannerEstimationResultResponse);
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                ViewBag.Success = _mannerEstimationProtector.Unprotect(r);
            }
            if (!string.IsNullOrWhiteSpace(s))
            {
                ViewBag.SuccessForTab = _mannerEstimationProtector.Unprotect(s);
            }

            return View();
        }


        private async Task BindViewBegForMannerEstimationResult(MannerEstimationResultResponse? mannerEstimationResultResponse)
        {
            RemoveMannerEstimationSession();
            int nitrogenValue = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.NitrogenValue);
            int p2O5Value = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.PhosphateValue);
            int potashValue = mannerEstimationResultResponse.MannerEstimationApplication.Sum(x => x.PotashValue);
            ViewBag.TotalValue = nitrogenValue + p2O5Value + potashValue;

            ViewBag.LastUpdatedOn = mannerEstimationResultResponse.LastUpdatedOn;
            mannerEstimationResultResponse.MannerEstimationApplication.ForEach(x => x.EncryptedApplicationId = _mannerEstimationProtector.Protect(x.ID.ToString()));
            ViewBag.MannerEstimations = mannerEstimationResultResponse;
            List<CropTypeResponse> cropTypeList = await _fieldLogic.FetchAllCropTypes();
            int cropGroupId = cropTypeList.FirstOrDefault(x => x.CropTypeId == mannerEstimationResultResponse.MannerEstimation.CropTypeID.Value)?.CropGroupId ?? 0;
            ViewBag.CropGroup = await _fieldLogic.FetchCropGroupById(cropGroupId);
            int count = 0;
            foreach (var application in mannerEstimationResultResponse.MannerEstimationApplication)
            {
                count++;
                (ManureType? manure, _) = await _mannerLogic.FetchManureTypeByManureTypeId(application.ManureTypeID.Value);
                int manureGroupId = manure?.ManureGroupId ?? 0;
                application.ManureGroup = (await _mannerLogic.FetchManureGroupById(manureGroupId)).Item1.Name;
                bool isManureLiquid = await _mannerEstimationLogic.FetchIsManureLiquid(application.ManureTypeID.Value);
                application.IsManureTypeLiquid = isManureLiquid;
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
                        TempData[$"ApplicationRateOption{count}"] = string.Format(Resource.lblEnterAnApplicationRate, manureUnit);
                    }
                }
            }
            Country? country = await _mannerLogic.FetchCountryById(mannerEstimationResultResponse.MannerFarm?.CountryID ?? 0);
            if (country != null)
            {
                ViewBag.CountryName = country.Name;
            }
            string encryptedMannerFarmId = _mannerEstimationProtector.Protect(mannerEstimationResultResponse.MannerFarm.ID.ToString());
            ViewBag.EncryptedMannerFarmId = encryptedMannerFarmId;
            ViewBag.EncryptedNitrogenId =
    _mannerEstimationProtector.Protect(((int)NMP.Commons.Enums.MannerNutrients.Nitrogen).ToString());

            ViewBag.EncryptedPhosphateId =
                _mannerEstimationProtector.Protect(((int)NMP.Commons.Enums.MannerNutrients.Phosphorus).ToString());

            ViewBag.EncryptedPotashId =
                _mannerEstimationProtector.Protect(((int)NMP.Commons.Enums.MannerNutrients.Potassium).ToString());
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimationViewModel == null)
            {
                mannerEstimationViewModel = new MannerEstimationViewModel();
                mannerEstimationViewModel.EncryptedMannerFarmId = encryptedMannerFarmId;
                _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);
            }
            await _mannerEstimationLogic.BindFarmDataForMannerEstimateUpdateOrCreate(mannerEstimationResultResponse.MannerFarm.ID ?? 0);
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
                estimation.MannerFarm.Name = string.Format(Resource.lblEstimationForFarm, estimation.MannerEstimation.Name, estimation.MannerFarm.Name);
            }

            ViewBag.MannerEstimations = mannerEstimations.OrderBy(x => x.MannerEstimation.Name);
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
        public async Task<IActionResult> IncorporationMethod(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IncorporationMethod() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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

                model = _mannerEstimationLogic.SetMannerEstimationStep29(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange && !model.IsIncorporationMethodChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction("IncorporationDelay");
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
        public async Task<IActionResult> IncorporationDelay(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IncorporationDelay() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }

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
                model = _mannerEstimationLogic.SetMannerEstimationStep30(model);
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
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

                model = _mannerEstimationLogic.SetMannerEstimationStep30(model);

                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !model.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "MannerEstimation  Controller :   HttpRequestException in IncorporationDelay() action : {Message} {StackTrace}", hre.Message, hre.StackTrace);
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimation  Controller :   Exception in IncorporationDelay() post action : {Message} {StackTrace}", ex.Message, ex.StackTrace);
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

        }

        [HttpGet]
        public async Task<IActionResult> Name(string? q, string? r, string? s)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Name() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindFarmFieldOrCropDataUpdate(q);
            }

            MannerEstimationStep31ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep31();
            if (!string.IsNullOrWhiteSpace(s))
            {
                model.IsAnyFarmExist = Convert.ToBoolean(_mannerEstimationProtector.Unprotect(s));
                model = _mannerEstimationLogic.SetMannerEstimationStep31(model);
            }
            if (!string.IsNullOrWhiteSpace(r))
            {
                model.EncryptedMannerEstimationId = r;
                model.MannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(r));
                model.IsCopyEstimate = true;
                model = _mannerEstimationLogic.SetMannerEstimationStep31(model);
            }
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

            MannerEstimationStep31ViewModel mannerEstimationStep31ViewModel = _mannerEstimationLogic.GetMannerEstimationStep31();
            if (mannerEstimationStep31ViewModel != null)
            {
                model.IsAnyFarmExist = mannerEstimationStep31ViewModel.IsAnyFarmExist;
            }
            model = _mannerEstimationLogic.SetMannerEstimationStep31(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model = _mannerEstimationLogic.GetMannerEstimationStep31();

            string action = _farmNameKey;

            (bool flowControl, IActionResult? value) = RedirectForName(model);
            if (!flowControl && value != null)
            {
                return value;
            }
            if (model.IsCopyEstimate.HasValue && model.IsCopyEstimate.Value)
            {
                MannerEstimationViewModel mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                int mannerEstimationId = model.MannerEstimationId ?? Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimationId));
                (int newEstimationId, Error? error) = await _mannerEstimationLogic.CopyMannerEstimation(mannerEstimationId, model.Name);
                if (newEstimationId == 0 || error != null)
                {
                    TempData["CopyFromEstimates"] = Resource.MsgWeCounldNotCopyMannerEstimation;
                    return View(model);
                }

                if (newEstimationId > 0)
                {
                    return RedirectToAction(_mannerHubPageAction, new
                    {
                        q = mannerEstimationViewModel.EncryptedMannerFarmId,
                        r = _mannerEstimationProtector.Protect(Resource.lblTrue),
                        s = _mannerEstimationProtector.Protect(Resource.lblTrue),

                    });
                }
            }
            else
            {
                action = "CopyExistingFarmAndFieldDetails";
            }


            return RedirectToAction(action);
        }

        private (bool flowControl, IActionResult? value) RedirectForName(MannerEstimationStep31ViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.EncryptedMannerEstimationId) && (model.IsCopyEstimate == false || model.IsCopyEstimate == null))
            {
                return (flowControl: false, value: RedirectToAction(_updateFieldOrCropDataActionName));
            }
            MannerEstimationViewModel? mannerEstimation = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimation != null && mannerEstimation.MannerFarmId != null)
            {
                return (flowControl: false, value: RedirectToAction("FieldName"));
            }

            return (flowControl: true, value: null);
        }

        private async Task ValidationForName(MannerEstimationStep31ViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheName);
            }
            MannerEstimationViewModel mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            if (mannerEstimationViewModel != null && !string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerFarmId))
            {
                int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationViewModel.EncryptedMannerFarmId));
                bool isExist = await _mannerEstimationLogic.FetchIsExistMannerEstimationsByMannerFarmIdAndName(mannerFarmId, model.Name);
                if (isExist)
                {
                    ModelState.AddModelError("Name", Resource.MsgNameAlreadyExist);
                }
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

            var payload = new
            {
                cropTypeId = model.MannerCropTypeId,
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
        public async Task<IActionResult> AutumnCropNitrogenUptake(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AutumnCropNitrogenUptake() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel model =await _mannerEstimationLogic.GetMannerEstimationStep32();
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutumnCropNitrogenUptake(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AutumnCropNitrogenUptake() post action called");
            if (!ModelState.IsValid)
            {
                ReplaceNumericRangeError(_autumnCropNitrogenUptakeKey, _autumnCropNitrogenUptakeKey, Resource.lblAutumnCropNitrogenNUptake);
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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.AutumnCropNitrogenUptake = model.AutumnCropNitrogenUptake;
            if (!ModelState.IsValid)
            {
                ViewBag.FieldName = mannerEstimationStep32ViewModel.FieldName;
                ViewBag.CropTypeName = mannerEstimationStep32ViewModel.CropTypeName;
                return View(_autumnCropNitrogenUptakeKey, mannerEstimationStep32ViewModel);
            }


            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);

        }

        [HttpGet]
        public async Task<IActionResult> SoilDrainageEndDate(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilDrainageEndDate() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();


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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.SoilDrainageEndDate = model.SoilDrainageEndDate;
            if (!ModelState.IsValid)
            {
                return View(_soilDrainageEndDateKey, mannerEstimationStep32ViewModel);
            }

            model = await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            if (model.IsSoilDrainageEndDateChange)
            {
                return RedirectToAction("EffectiveRainfall");
            }
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
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
        public async Task<IActionResult> RainfallWithinSixHour(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} RainfallWithinSixHour() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();

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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.RainfallWithinSixHoursId = model.RainfallWithinSixHoursId;
            if (!ModelState.IsValid)
            {
                return View("RainfallWithinSixHour", mannerEstimationStep32ViewModel);
            }

            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
        }

        private async Task FetchDefaultTotalRainfall(MannerEstimationStep32ViewModel model)
        {
            string halfPostCode = model.PostCode[..4].Trim();

            if (model.ApplicationDate.HasValue &&
                model.SoilDrainageEndDate.HasValue)
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
        [HttpGet]
        public async Task<IActionResult> EffectiveRainfall(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  EffectiveRainfall() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();
            if (model.IsSoilDrainageEndDateChange && model.PostCode != null)
            {
                // Effective rainfall after application
                await FetchDefaultTotalRainfall(model);

            }
            return View(model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EffectiveRainfall(MannerEstimationStep32ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  EffectiveRainfall() post action called");
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();

            if (!ModelState.IsValid)
            {
                return View("EffectiveRainfall", mannerEstimationStep32ViewModel);
            }

            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> EffectiveRainfallManual()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} EffectiveRainfallManual() action called");
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();

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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.TotalRainfall = model.TotalRainfall;
            if (!ModelState.IsValid)
            {
                return View("EffectiveRainfallManual", mannerEstimationStep32ViewModel);
            }

            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
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
        public async Task<IActionResult> Windspeed(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Windspeed() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();
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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.WindspeedId = model.WindspeedId;
            if (!ModelState.IsValid)
            {
                return await Task.FromResult(View("Windspeed", mannerEstimationStep32ViewModel));
            }


            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
        }

        [HttpGet]
        public async Task<IActionResult> TopsoilMoisture(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} TopsoilMoisture() action called");
            if (!string.IsNullOrWhiteSpace(q))
            {
                await BindApplicationDetailForUpdate(q);
            }
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();

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
            MannerEstimationStep32ViewModel mannerEstimationStep32ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep32();
            mannerEstimationStep32ViewModel.MoistureTypeId = model.MoistureTypeId;
            if (!ModelState.IsValid)
            {
                return View("TopsoilMoisture", mannerEstimationStep32ViewModel);
            }

            await _mannerEstimationLogic.SetMannerEstimationStep32(mannerEstimationStep32ViewModel);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            return (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerEstimationId) && !mannerEstimationStep32ViewModel.IsManureTypeChange) ? RedirectToAction(_updateApplicationDataActionName) : RedirectToAction(_conditionsAffectingNutrients);
        }




        private async Task BindPostCodeAndCropTypeDataForAddNewApplication(MannerEstimationStep32ViewModel model)
        {
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();

            if (mannerEstimationViewModel?.IsComingForAddNewApplication == true || (!string.IsNullOrWhiteSpace(mannerEstimationViewModel?.EncryptedMannerFarmId)))
            {
                if (!string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerEstimationId))
                {
                    int mannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationViewModel.EncryptedMannerEstimationId));
                    (MannerEstimation? mannerEstimate, _) = await _mannerEstimationLogic.FetchMannerEstimateById(mannerEstimateId);
                    if (mannerEstimate != null)
                    {
                        (MannerFarm? mannerFarm, _) = await _mannerEstimationLogic.FetchMannerFarmById(mannerEstimate.MannerFarmID.Value);
                        if (mannerFarm != null)
                        {
                            model.PostCode = mannerFarm.Postcode;
                            model.CropTypeId = mannerEstimate.CropTypeID;
                            model.MannerCropTypeId = mannerEstimate.MannerCropTypeID;
                        }
                    }
                }
                else
                {
                    int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationViewModel.EncryptedMannerFarmId));
                    (MannerFarm? mannerFarm, _) = await _mannerEstimationLogic.FetchMannerFarmById(mannerFarmId);
                    if (mannerFarm != null)
                    {
                        model.PostCode = mannerFarm.Postcode;
                        model.CropTypeId = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;
                        model.MannerCropTypeId = mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId;
                    }
                }

                _mannerEstimationLogic.SetMannerEstimationStep32(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> ConditionsAffectingNutrients()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ConditionsAffectingNutrients() action called");
            MannerEstimationStep32ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep32();
            Error error = new Error();
            try
            {
                await BindPostCodeAndCropTypeDataForAddNewApplication(model);
                //Autumn crop Nitrogen uptake
                model.AutumnCropNitrogenUptake = await BuildAutumnCropNitrogenUptakeAsync(model);

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
                    await FetchDefaultTotalRainfall(model);
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
                            model.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
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
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (mannerEstimationViewModel != null && (mannerEstimationViewModel.IsNewEstimate && mannerEstimationViewModel.MannerFarmId != null))
                {
                    return RedirectToAction("AddNewMannerEstimate");
                }
                if (mannerEstimationViewModel != null && (!mannerEstimationViewModel.IsComingForAddNewApplication && !string.IsNullOrWhiteSpace(mannerEstimationViewModel.EncryptedMannerEstimationId) && !model.IsManureTypeChange))
                {
                    return RedirectToAction(_updateApplicationDataActionName);
                }
                if (mannerEstimationViewModel?.IsComingForAddNewApplication == true)
                {
                    return RedirectToAction("AddApplicationData");
                }
                Guid organisationId = GetOrganisationId();

                (MannerFarmEstimationApplicationResponse? mannerFarmEstimationApplicationResult, Error? error)
                   = await _mannerEstimationLogic.AddMannerFarmEstimation(organisationId);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["ConditionsAffectingNutrientsError"] = error.Message;
                    return View(model);
                }

                if (mannerFarmEstimationApplicationResult != null && mannerFarmEstimationApplicationResult.MannerEstimation.ID != null)
                {
                    return RedirectToAction(_mannerEstimationResultKey, new
                    {
                        q = _mannerEstimationProtector.Protect(
                          mannerFarmEstimationApplicationResult.MannerEstimation.ID.ToString()),
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
        [HttpGet]
        public async Task<IActionResult> UpdateNitrogenPriceQuestion()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdateNitrogenPriceQuestion() action called");
            MannerEstimationStep33ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep33();
            int mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimateId));
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.FetchMannerEstimateById(mannerEstimationId);
            if (!string.IsNullOrWhiteSpace(error?.Message) && mannerEstimation == null)
            {
                TempData[_nutrientProductErrorKey] = error.Message;
                return RedirectToAction("NutrientProduct", new { q = model.EncryptedMannerEstimateId });
            }
            if (!model.UpdateNitrogenPriceQuestion.HasValue)
            {
                model.UpdateNitrogenPriceQuestion = mannerEstimation.IsNitrogenPriceBasedOnNutrientPrice ? (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice : (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice;
                _mannerEstimationLogic.SetMannerEstimationStep33(model);
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNitrogenPriceQuestion(MannerEstimationStep33ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdateNitrogenPriceQuestion() post action called");
            AddErrorIfNull(model.UpdateNitrogenPriceQuestion, "UpdateNitrogenPriceQuestion", Resource.MsgSelectAnOptionBeforeContinuing);

            MannerEstimationStep33ViewModel mannerEstimationStep33ViewModelmodel = _mannerEstimationLogic.GetMannerEstimationStep33();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep33ViewModelmodel.EncryptedMannerEstimateId));
            _mannerEstimationLogic.SetMannerEstimationStep33(model);
            return RedirectToAction("UpdateNitrogenPrice");
        }

        [HttpGet]
        public async Task<IActionResult> NutrientProduct(string q, string r)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} NutrientProduct() action called");
            MannerEstimationStep35ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep35();
            int nutrientId = 1;
            if (!string.IsNullOrWhiteSpace(r))
            {
                nutrientId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(r));
            }
            (List<NutrientProductResponse> nutrientProducts, Error? error) = await _mannerEstimationLogic.FetchNutrientProductByNutrientId(nutrientId);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new { q = q });
            }

            var productList = nutrientProducts.Select(x => new
            {
                Value = x.id.ToString(),
                Text = x.name,
                Hint = string.Format("{0:0.##} {1} {2}", x.nutrientPercentage, Resource.lblPercent, Resource.lblNitrogenLowercase)
            }).ToList();

            model.EncryptedMannerEstimateId = q;
            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
            (MannerEstimation? mannerEstimation, error) = await _mannerEstimationLogic.FetchMannerEstimateById(model.MannerEstimateId ?? 0);
            if (!string.IsNullOrWhiteSpace(error?.Message) && mannerEstimation == null)
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new { q = q });
            }

            int defaultNitrogenProductId = BindDefaultNutrientProductId(nutrientId, mannerEstimation);

            if (!model.NutrientProductId.HasValue)
            {
                model.NutrientProductId = nutrientProducts
                    .Where(x => x.id == defaultNitrogenProductId)
                    .Select(x => (int?)x.id)
                    .FirstOrDefault();
            }

            ViewBag.NutrientProductList = productList;


            model.NutrientId = nutrientId;

            _mannerEstimationLogic.SetMannerEstimationStep35(model);

            if (productList.Count == 1)
            {
                if (model.NutrientId == (int)NMP.Commons.Enums.MannerNutrients.Phosphorus)
                {
                    return RedirectToAction("UpdatePhosphorusPriceQuestion");
                }
                else
                {
                    return RedirectToAction("UpdatePotashPriceQuestion");
                }
            }



            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NutrientProduct(MannerEstimationStep35ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  NutrientProduct() post action called");
            AddErrorIfNull(model.NutrientProductId, "NutrientProductId", Resource.MsgSelectAnOptionBeforeContinuing);
            MannerEstimationStep35ViewModel? mannerEstimationStep35ViewModel = _mannerEstimationLogic.GetMannerEstimationStep35();
            if (!ModelState.IsValid)
            {
                (List<NutrientProductResponse> nutrientProducts, Error? error) = await _mannerEstimationLogic.FetchNutrientProductByNutrientId((int)NMP.Commons.Enums.MannerNutrients.Nitrogen);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_nutrientProductErrorKey] = error.Message;
                    return View(model);
                }

                var productList = nutrientProducts.Select(x => new
                {
                    Value = x.id.ToString(),
                    Text = x.name,
                    Hint = string.Format("{0:0.##} {1}", x.nutrientPercentage, Resource.lblNitrogenLowercase)
                }).ToList();

                (MannerEstimation? mannerEstimation, error) = await _mannerEstimationLogic.FetchMannerEstimateById(mannerEstimationStep35ViewModel.MannerEstimateId ?? 0);
                if (!string.IsNullOrWhiteSpace(error?.Message) && mannerEstimation == null)
                {
                    TempData[_nutrientProductErrorKey] = error.Message;
                    return View(model);
                }
                if (!model.NutrientProductId.HasValue)
                {
                    model.NutrientProductId = nutrientProducts
                        .Where(x => x.id == mannerEstimation.NitrogenProductId)
                        .Select(x => (int?)x.id)
                        .FirstOrDefault();
                }
                if (!model.NutrientProductId.HasValue)
                {
                    model.NutrientProductId = nutrientProducts
                        .Where(x => x.isNutrientDefaultProduct)
                        .Select(x => (int?)x.id)
                        .FirstOrDefault();
                }
                ViewBag.NutrientProductList = productList;

                model = _mannerEstimationLogic.GetMannerEstimationStep35();
                return View(model);
            }
            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep35ViewModel.EncryptedMannerEstimateId));

            _mannerEstimationLogic.SetMannerEstimationStep35(model);
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();

            if (mannerEstimationViewModel != null)
            {
                await RecalculateNutrientPrices(mannerEstimationStep35ViewModel, mannerEstimationViewModel);
            }
            return RedirectToAction("UpdateNitrogenPriceQuestion");
        }

        private static int BindDefaultNutrientProductId(int nutrientId, MannerEstimation? mannerEstimation)
        {
            int defaultNitrogenProductId = 0;
            if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Nitrogen)
            {
                defaultNitrogenProductId = mannerEstimation.NitrogenProductId;
            }
            else if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Phosphorus)
            {
                defaultNitrogenProductId = mannerEstimation.PhosphateProductId;
            }
            else if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Potassium)
            {
                defaultNitrogenProductId = mannerEstimation.PotashProductId;
            }

            return defaultNitrogenProductId;
        }


        private async Task RecalculateNutrientPrices(MannerEstimationStep35ViewModel mannerEstimationStep35ViewModel, MannerEstimationViewModel mannerEstimationViewModel)
        {
            if (mannerEstimationStep35ViewModel.NutrientId == (int)NMP.Commons.Enums.MannerNutrients.Nitrogen && mannerEstimationViewModel.MannerEstimationStep34 != null && mannerEstimationViewModel.MannerEstimationStep34.NitrogenProductPrice != null)
            {
                mannerEstimationViewModel.MannerEstimationStep34.UpdateNitrogenPriceQuestion = (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
                await _mannerEstimationLogic.SetMannerEstimationStep34(mannerEstimationViewModel.MannerEstimationStep34);
            }
            else if (mannerEstimationStep35ViewModel.NutrientId == (int)NMP.Commons.Enums.MannerNutrients.Phosphorus && mannerEstimationViewModel.MannerEstimationStep37 != null && mannerEstimationViewModel.MannerEstimationStep37.PhosphorusProductPrice != null)
            {
                mannerEstimationViewModel.MannerEstimationStep37.UpdatePhosphorusPriceQuestion = (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
                await _mannerEstimationLogic.SetMannerEstimationStep37(mannerEstimationViewModel.MannerEstimationStep37);
            }
            else if (mannerEstimationStep35ViewModel.NutrientId == (int)NMP.Commons.Enums.MannerNutrients.Potassium && mannerEstimationViewModel.MannerEstimationStep39 != null && mannerEstimationViewModel.MannerEstimationStep39.PotashProductPrice != null)
            {
                mannerEstimationViewModel.MannerEstimationStep39.UpdatePotashPriceQuestion = (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
                await _mannerEstimationLogic.SetMannerEstimationStep39(mannerEstimationViewModel.MannerEstimationStep39);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateNitrogenPrice()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdateNutrientPrice() action called");
            MannerEstimationStep34ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep34();

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNitrogenPrice(MannerEstimationStep34ViewModel model)
        {
            string nitrogenProductPriceKey = "NitrogenProductPrice";
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdateNutrientPrice() post action called");

            MannerEstimationStep34ViewModel mannerEstimationStep34ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep34();
            ValidateNitrogenPrice(model, nitrogenProductPriceKey, mannerEstimationStep34ViewModel);

            if (!ModelState.IsValid)
            {
                if (mannerEstimationStep34ViewModel.UpdateNitrogenPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
                {
                    mannerEstimationStep34ViewModel.NitrogenProductPrice = model.NitrogenProductPrice;
                }
                else
                {
                    mannerEstimationStep34ViewModel.NitrogenPrice = model.NitrogenPrice;
                }
                return View(mannerEstimationStep34ViewModel);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep34ViewModel.EncryptedMannerEstimateId));
            await _mannerEstimationLogic.SetMannerEstimationStep34(model);
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.UpdateMannerEstimation(model.MannerEstimateId);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["NitrogenPriceError"] = error.Message;
                return View(model);
            }

            string successMsg = Resource.lblNutrientPricesAndFinancialValueUpdated;

            return RedirectToResultWithSuccessValues(mannerEstimation.ID.Value, successMsg, "FinancialValue");
        }

        private void ValidateNitrogenPrice(MannerEstimationStep34ViewModel model, string nitrogenProductPriceKey, MannerEstimationStep34ViewModel mannerEstimationStep34ViewModel)
        {
            if (mannerEstimationStep34ViewModel.UpdateNitrogenPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
            {
                AddErrorIfNull(model.NitrogenProductPrice, nitrogenProductPriceKey, Resource.lblEnterAValidNumber);
                if (model.NitrogenProductPrice.HasValue && (model.NitrogenProductPrice < 0 || model.NitrogenProductPrice > 99999999))
                {
                    ModelState.AddModelError(nitrogenProductPriceKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenProductPrice.ToLower(), 0, 99999999));
                }
                ReplaceModelStateError(
   ModelState,
   nitrogenProductPriceKey,
   string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenProductPrice.ToLower(), 0, 99999999));
            }
            else
            {
                string nitrogenPriceKey = "NitrogenPrice";
                AddErrorIfNull(model.NitrogenPrice, nitrogenPriceKey, Resource.lblEnterAValidNumber);
                if (model.NitrogenPrice.HasValue && (model.NitrogenPrice < 0 || model.NitrogenPrice > 999999))
                {
                    ModelState.AddModelError(nitrogenPriceKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenPrice.ToLower(), 0, 999999));
                }
                ReplaceModelStateError(
   ModelState,
   nitrogenPriceKey,
   string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenPrice.ToLower(), 0, 999999));
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdatePhosphorusPriceQuestion()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdatePhosphorusPriceQuestion() action called");
            MannerEstimationStep36ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep36();
            int mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimateId));
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.FetchMannerEstimateById(mannerEstimationId);
            if (!string.IsNullOrWhiteSpace(error?.Message) && mannerEstimation == null)
            {
                TempData[_nutrientProductErrorKey] = error.Message;
                return RedirectToAction("NutrientProduct", new { q = model.EncryptedMannerEstimateId });
            }
            if (!model.UpdatePhosphorusPriceQuestion.HasValue)
            {
                model.UpdatePhosphorusPriceQuestion = mannerEstimation.IsPhosphatePriceBasedOnNutrientPrice ? (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice : (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice;

                _mannerEstimationLogic.SetMannerEstimationStep36(model);
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhosphorusPriceQuestion(MannerEstimationStep36ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdatePhosphorusPriceQuestion() post action called");
            AddErrorIfNull(model.UpdatePhosphorusPriceQuestion, "UpdatePhosphorusPriceQuestion", Resource.MsgSelectAnOptionBeforeContinuing);

            MannerEstimationStep36ViewModel mannerEstimationStep36ViewModelmodel = _mannerEstimationLogic.GetMannerEstimationStep36();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep36ViewModelmodel.EncryptedMannerEstimateId));
            _mannerEstimationLogic.SetMannerEstimationStep36(model);
            return RedirectToAction("UpdatePhosphorusPrice");
        }
        [HttpGet]
        public async Task<IActionResult> UpdatePhosphorusPrice()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdatePhosphorusPrice() action called");
            MannerEstimationStep37ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep37();

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhosphorusPrice(MannerEstimationStep37ViewModel model)
        {
            string phosphorusProductPriceKey = "PhosphorusProductPrice";
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdatePhosphorusPrice() post action called");

            MannerEstimationStep37ViewModel mannerEstimationStep37ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep37();
            ValidatePhosphorusPrice(model, phosphorusProductPriceKey, mannerEstimationStep37ViewModel);

            if (!ModelState.IsValid)
            {
                if (mannerEstimationStep37ViewModel.UpdatePhosphorusPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
                {
                    mannerEstimationStep37ViewModel.PhosphorusProductPrice = model.PhosphorusProductPrice;
                }
                else
                {
                    mannerEstimationStep37ViewModel.PhosphorusPrice = model.PhosphorusPrice;
                }
                return View(mannerEstimationStep37ViewModel);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep37ViewModel.EncryptedMannerEstimateId));
            await _mannerEstimationLogic.SetMannerEstimationStep37(model);
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.UpdateMannerEstimation(model.MannerEstimateId);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["PhosphorusPriceError"] = error.Message;
                return View(model);
            }
            string successMsg = Resource.lblNutrientPricesAndFinancialValueUpdated;

            return RedirectToResultWithSuccessValues(mannerEstimation.ID.Value, successMsg, "FinancialValue");
        }

        private void ValidatePhosphorusPrice(MannerEstimationStep37ViewModel model, string phosphorusProductPriceKey, MannerEstimationStep37ViewModel mannerEstimationStep37ViewModel)
        {
            if (mannerEstimationStep37ViewModel.UpdatePhosphorusPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
            {
                AddErrorIfNull(model.PhosphorusProductPrice, "PhosphorusProductPrice", Resource.lblEnterAValidNumber);
                if (model.PhosphorusProductPrice.HasValue && (model.PhosphorusProductPrice < 0 || model.PhosphorusProductPrice > 99999999))
                {
                    ModelState.AddModelError("NitrogenProductPrice", string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenProductPrice.ToLower(), 0, 99999999));
                }
                ReplaceModelStateError(
                ModelState,
                phosphorusProductPriceKey,
                string.Format(Resource.MsgEnterValueInBetween,
                    Resource.lblNitrogenProductPrice.ToLower(), 0, 99999999));

            }
            else
            {
                string phosphorusPriceKey = "PhosphorusPrice";
                AddErrorIfNull(model.PhosphorusPrice, phosphorusPriceKey, Resource.lblEnterAValidNumber);
                if (model.PhosphorusPrice.HasValue && (model.PhosphorusPrice < 0 || model.PhosphorusPrice > 999999))
                {
                    ModelState.AddModelError(phosphorusPriceKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenPrice.ToLower(), 0, 999999));
                }
                ReplaceModelStateError(
    ModelState,
    phosphorusPriceKey,
    string.Format(Resource.MsgEnterValueInBetween, Resource.lblPhosphorusPrice.ToLower(), 0, 999999));
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdatePotashPriceQuestion()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdatePhosphorusPriceQuestion() action called");
            MannerEstimationStep38ViewModel? model = _mannerEstimationLogic.GetMannerEstimationStep38();
            int mannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(model.EncryptedMannerEstimateId));
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.FetchMannerEstimateById(mannerEstimationId);
            if (!string.IsNullOrWhiteSpace(error?.Message) && mannerEstimation == null)
            {
                TempData[_nutrientProductErrorKey] = error.Message;
                return RedirectToAction("NutrientProduct", new { q = model.EncryptedMannerEstimateId });
            }
            if (!model.UpdatePotashPriceQuestion.HasValue)
            {
                model.UpdatePotashPriceQuestion = mannerEstimation.IsPotashPriceBasedOnNutrientPrice ? (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice : (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice;
                _mannerEstimationLogic.SetMannerEstimationStep38(model);
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePotashPriceQuestion(MannerEstimationStep38ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdatePotashPriceQuestion() post action called");
            AddErrorIfNull(model.UpdatePotashPriceQuestion, "UpdatePotashPriceQuestion", Resource.MsgSelectAnOptionBeforeContinuing);

            MannerEstimationStep38ViewModel mannerEstimationStep38ViewModelmodel = _mannerEstimationLogic.GetMannerEstimationStep38();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep38ViewModelmodel.EncryptedMannerEstimateId));
            _mannerEstimationLogic.SetMannerEstimationStep38(model);
            return RedirectToAction("UpdatePotashPrice");
        }
        [HttpGet]
        public async Task<IActionResult> UpdatePotashPrice()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} UpdatePhosphorusPrice() action called");
            MannerEstimationStep39ViewModel? model = await _mannerEstimationLogic.GetMannerEstimationStep39();

            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePotashPrice(MannerEstimationStep39ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdatePotashPrice() post action called");

            MannerEstimationStep39ViewModel mannerEstimationStep39ViewModel = await _mannerEstimationLogic.GetMannerEstimationStep39();
            ValidatePotashPrice(model, mannerEstimationStep39ViewModel);

            if (!ModelState.IsValid)
            {
                if (mannerEstimationStep39ViewModel.UpdatePotashPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
                {
                    mannerEstimationStep39ViewModel.PotashProductPrice = model.PotashProductPrice;
                }
                else
                {
                    mannerEstimationStep39ViewModel.PotashPrice = model.PotashPrice;
                }
                return View(mannerEstimationStep39ViewModel);
            }

            model.MannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep39ViewModel.EncryptedMannerEstimateId));
            await _mannerEstimationLogic.SetMannerEstimationStep39(model);
            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.UpdateMannerEstimation(model.MannerEstimateId);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["PotashPriceError"] = error.Message;
                return View(model);
            }

            string successMsg = Resource.lblNutrientPricesAndFinancialValueUpdated;

            return RedirectToResultWithSuccessValues(mannerEstimation.ID.Value, successMsg, "FinancialValue");

        }

        private IActionResult RedirectToResultWithSuccessValues(int mannerEstimateId, string succesMsg, string tabId)
        {
            return RedirectToAction(actionName: _mannerEstimationResultKey,
                       controllerName: "MannerEstimation", routeValues: new
                       {
                           q = _mannerEstimationProtector.Protect(mannerEstimateId.ToString()),
                           r = _mannerEstimationProtector.Protect(Resource.lblTrue),
                           s = _mannerEstimationProtector.Protect(succesMsg)
                       }, fragment: tabId);
        }

        private void ValidatePotashPrice(MannerEstimationStep39ViewModel model, MannerEstimationStep39ViewModel mannerEstimationStep39ViewModel)
        {
            string potashProductPriceKey = "PotashProductPrice";
            if (mannerEstimationStep39ViewModel.UpdatePotashPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByProductPrice)
            {
                AddErrorIfNull(model.PotashProductPrice, potashProductPriceKey, Resource.lblEnterAValidNumber);
                if (model.PotashProductPrice.HasValue && (model.PotashProductPrice < 0 || model.PotashProductPrice > 99999999))
                {
                    ModelState.AddModelError(potashProductPriceKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenProductPrice.ToLower(), 0, 99999999));
                }
                ReplaceModelStateError(
                ModelState,
                potashProductPriceKey,
                string.Format(Resource.MsgEnterValueInBetween,
                Resource.lblPotashProductPrice.ToLower(), 0, 99999999));

            }
            else
            {
                string potashPriceKey = "PotashPrice";
                AddErrorIfNull(model.PotashPrice, potashPriceKey, Resource.lblEnterAValidNumber);
                if (model.PotashPrice.HasValue && (model.PotashPrice < 0 || model.PotashPrice > 999999))
                {
                    ModelState.AddModelError(potashPriceKey, string.Format(Resource.MsgEnterValueInBetween, Resource.lblNitrogenPrice.ToLower(), 0, 999999));
                }

                ReplaceModelStateError(
                ModelState,
                potashPriceKey,
                string.Format(Resource.MsgEnterValueInBetween, Resource.lblPotashPrice.ToLower(), 0, 999999));

            }
        }
        private void ReplaceModelStateError(ModelStateDictionary modelState, string key, string numericErrorMessage)
        {
            if (!modelState.IsValid && modelState.ContainsKey(key))
            {
                var error = modelState[key]?.Errors.FirstOrDefault()?.ErrorMessage;

                if (string.IsNullOrEmpty(error))
                    return;


                if (error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[key].RawValue, key)))
                {
                    modelState[key].Errors.Clear();
                    modelState.AddModelError(key, numericErrorMessage);
                }

            }
        }
        private async Task<(int?, int?)> GetUpdatingEstimationAndApplicationId(string? encryptedEstimateId, string? encryptedApplicationId)
        {
            int? updatingEstimateId = !string.IsNullOrWhiteSpace(encryptedEstimateId) ? Convert.ToInt32(_mannerEstimationProtector.Unprotect(encryptedEstimateId)) : null;

            int? updatingApplicationId = !string.IsNullOrWhiteSpace(encryptedApplicationId) ? Convert.ToInt32(_mannerEstimationProtector.Unprotect(encryptedApplicationId)) : null;

            return (updatingEstimateId, updatingApplicationId);
        }

        private async Task<(TModel, Error?)> NitrogenApplicationLimitWarningMessage<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId)
    where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;

            (model, error) = await NFieldLimitWarningMessage(model, mannerEstimationId, mannerAppId);
            (model, error) = await NitrogenLimitWarningMessage(model, mannerEstimationId, mannerAppId);
            return (model, error);
        }

        private async Task<(TModel, Error?)> NFieldLimitWarningMessage<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId) //mannerEstimationId will be null for new application and will have value for updated application and add another application
    where TModel : MannerEstimationNWarningViewModel
        {
            if (model.IsWithinNVZ == false || model.IsWithinNVZ == null)
            {
                return (model, null);
            }
            Error? error = null;
            decimal defaultNitrogen = 0;
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (string.IsNullOrWhiteSpace(error?.Message))
            {
                defaultNitrogen = manureType?.TotalN ?? 0;
            }

            List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();

            if (!model.ApplicationRate.HasValue || !model.ApplicationDate.HasValue)
            {
                return (model, error);
            }

            decimal currentApplicationNitrogen = defaultNitrogen * model.ApplicationRate.Value;

            // Warning excel sheet row 2: >250 kg/ha total N in last 365 days (non green-compost manures)
            if (model.ManureTypeId != (int)NMP.Commons.Enums.ManureTypes.GreenCompost &&
                model.ManureTypeId != (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost)
            {
                error = await CheckNFieldLimit250(model, warningList, currentApplicationNitrogen, mannerEstimationId, mannerAppId);
            }

            bool isScotland = model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;
            bool isCompost = model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenCompost ||
                              model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.GreenFoodCompost;

            if (isScotland || isCompost)
            {
                error = await CheckCompostAndScotlandLimits(model, warningList, currentApplicationNitrogen, isScotland, isCompost, mannerEstimationId, mannerAppId);
            }

            return (model, error);
        }

        private async Task<(TModel, Error?)> NitrogenLimitWarningMessage<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId) //mannerEstimationId will be null for new application and will have value for updated application and add another application
    where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            if (!(IsOtherManureType(model.ManureTypeId)))
            {
                (model, error) = await IsEndClosedPeriodFebruaryWarningMessage(model, mannerEstimationId, mannerAppId);

            }
            if (!(IsOtherManureType(model.ManureTypeId)))
            {
                (model, error) = await IsClosedPeriodStartAndEndFebExceedNRateException(model, mannerEstimationId, mannerAppId);

            }
            return (model, error);
        }
        private async Task<(TModel, Error?)> IsClosedPeriodStartAndEndFebExceedNRateException<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId) where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);
            bool isHighReadilyAvailableNitrogen = false;
            if (error == null)
            {
                isHighReadilyAvailableNitrogen = manureType?.HighReadilyAvailableNitrogen ?? false;
            }

            bool? isFieldIsInNVZ = model.IsWithinNVZ;

            if (!(model.IsFarmOrganic.Value && isHighReadilyAvailableNitrogen && isFieldIsInNVZ.Value))
            {
                return (model, error);
            }

            await ApplyClosedPeriodEndFebWarningsAsync(model, mannerEstimationId, mannerAppId);

            return (model, error);
        }

        private async Task<(TModel, Error?)> ApplyClosedPeriodEndFebWarningsAsync<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId) where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            HashSet<int> cropTypeIdsForTrigger = WarningWithinPeriod.FilteredCropForWarning();
            HashSet<int> brassicaCrops = WarningWithinPeriod.BrassicaCrops();

            int cropTypeId = model.CropTypeId ?? 0;
            int harvestYear = GetHarvestYearFromApplicationDate(model.ApplicationDate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            DateTime endDateFebruary = new DateTime((harvestYear), 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
            DateTime endOfOctober = new DateTime((harvestYear) - 1, 10, 31, 0, 0, 0, DateTimeKind.Utc);

            decimal totalNitrogen = 0;
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (string.IsNullOrWhiteSpace(error?.Message))
            {
                totalNitrogen = manureType?.TotalN ?? 0;
            }


            // warning excel sheet row no. 15
            if (model.CropTypeId == (int)NMP.Commons.Enums.CropTypes.Grass && model.CountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyGrassWarningAsync(model, mannerEstimationId, mannerAppId, endOfOctober, totalNitrogen);
            }

            // warning excel sheet row no. 13
            if ((cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Commons.Enums.CropTypes.SaladOnions)
                && model.CountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyAllumWarningAsync(model, mannerEstimationId, mannerAppId, endDateFebruary, totalNitrogen);
            }

            // wales warning
            if (cropTypeIdsForTrigger.Contains(cropTypeId) && model.CountryId == (int)NMP.Commons.Enums.FarmCountry.Wales)
            {
                await ApplyAllumWarningAsync(model, mannerEstimationId, mannerAppId, endDateFebruary, totalNitrogen);
            }

            // warning excel sheet row no. 14
            if (brassicaCrops.Contains(cropTypeId) && model.CountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyBrassicaWarningAsync(model, mannerEstimationId, mannerAppId, endDateFebruary, totalNitrogen);
            }

            // warning excel sheet row no. 16
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape && model.CountryId == (int)NMP.Commons.Enums.FarmCountry.England)
            {
                await ApplyWinterOilseedRapeWarningAsync(model, mannerEstimationId, mannerAppId, endOfOctober, totalNitrogen);
            }
            return (model, error);
        }

        private async Task ApplyWinterOilseedRapeWarningAsync<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId, DateTime endOfOctober, decimal totalNitrogen) where TModel : MannerEstimationNWarningViewModel
        {
            var (startDate, _) = GetClosedPeriodDates(model.ClosedPeriod, model.ApplicationDate.Value);
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, startDate, endOfOctober);
            if (!isWithinDateRange)
            {
                return;
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            (decimal totalN, _) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(mannerEstimationId ?? 0, startDate, endOfOctober, mannerAppId);

            if (currentNitrogen + totalN > 150)
            {
                List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateOSR.ToString(), Resource.lblHighNOrganicManureMaxRateOSR);
            }
        }

        private async Task ApplyBrassicaWarningAsync<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId, DateTime endDateFebruary, decimal totalNitrogen) where TModel : MannerEstimationNWarningViewModel
        {
            var (startDate, _) = GetClosedPeriodDates(model.ClosedPeriod, model.ApplicationDate.Value);
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, startDate, endDateFebruary);
            if (!isWithinDateRange)
            {
                return;
            }

            (decimal totalN, Error? error) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(mannerEstimationId ?? 0, startDate, endDateFebruary, mannerAppId);

            decimal nitrogenWithin4Weeks = 0;
            if (!string.IsNullOrWhiteSpace(model.EncryptedMannerApplicationsId))
            {

                (nitrogenWithin4Weeks, error) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(
                    mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-27), model.ApplicationDate.Value, mannerAppId);
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            if (currentNitrogen != null && (currentNitrogen > 50 || currentNitrogen + totalN > 150 || (nitrogenWithin4Weeks > 0)))
            {
                List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateWeeks.ToString(), Resource.lblHighNOrganicManureMaxRateWeeks);
            }
        }



        private async Task ApplyAllumWarningAsync<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId, DateTime endDateFebruary, decimal totalNitrogen) where TModel : MannerEstimationNWarningViewModel
        {
            var (startDate, _) = GetClosedPeriodDates(model.ClosedPeriod, model.ApplicationDate.Value);
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, startDate, endDateFebruary);
            if (!isWithinDateRange)
            {
                return;
            }

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            (decimal totalN, _) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(mannerEstimationId ?? 0, startDate, endDateFebruary, mannerAppId);

            if (currentNitrogen + totalN > 150)
            {
                List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRate.ToString(), Resource.lblHighNOrganicManureMaxRate);
            }
        }

        private async Task ApplyGrassWarningAsync<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId, DateTime endOfOctober, decimal totalNitrogen) where TModel : MannerEstimationNWarningViewModel
        {
            var (startDate, _) = GetClosedPeriodDates(model.ClosedPeriod, model.ApplicationDate.Value);
            bool isWithinDateRange = WarningWithinPeriod.IsApplicationDateWithinDateRange(model.ApplicationDate, startDate, endOfOctober);
            if (!isWithinDateRange)
            {
                return;
            }

            (decimal totalN, _) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(mannerEstimationId ?? 0, startDate, endOfOctober, mannerAppId);

            decimal? currentNitrogen = totalNitrogen * model.ApplicationRate;
            if (currentNitrogen != null && (currentNitrogen > 40 || currentNitrogen + totalN > 150))
            {
                List<WarningResponse> warningList = await _warningLogic.FetchAllWarningAsync();
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.HighNOrganicManureMaxRateGrass.ToString(), Resource.lblHighNOrganicManureMaxRateGrass);
            }
        }



        private async Task<(TModel, Error?)> IsEndClosedPeriodFebruaryWarningMessage<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId) where TModel : MannerEstimationNWarningViewModel
        {

            var warningList = await _warningLogic.FetchAllWarningAsync();

            bool isSlurry = Functions.IsSlurry(model.ManureTypeId);
            bool isPoultry = Functions.IsPoultryManure(model.ManureTypeId);


            if (model.CountryId != (int)NMP.Commons.Enums.FarmCountry.Scotland)
            {
                return await HandleNonScotland(model, model.ClosedPeriod, isSlurry, isPoultry, warningList);
            }

            return await HandleScotland(model, mannerEstimationId, mannerAppId, model.ClosedPeriod, isPoultry, warningList);
        }
        private static async Task<(TModel, Error?)> HandleNonScotland<TModel>(TModel model, string? closedPeriod, bool isSlurry, bool isPoultry, List<WarningResponse> warningList) where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            if (!IsWithinClosedPeriodAndFeb(model.ApplicationDate, closedPeriod))
                return (model, error);

            if (isSlurry && model.ApplicationRate > 30)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.SlurryMaxRate.ToString(), Resource.lblEndClosedPeriodEndFeb);
            }

            if (isPoultry && model.ApplicationRate > 8)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.PoultryManureMaxApplicationRate.ToString(), Resource.lblEndClosedPeriodEndFeb);
            }

            return (model, error);
        }
        private async Task<(TModel, Error?)> HandleScotland<TModel>(TModel model, int? mannerEstimationId, int? mannerAppId, string closedPeriod, bool isPoultry, List<WarningResponse> warningList) where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(model.ManureTypeId ?? 0);

            bool isRanExceptPoultry =
                (manureType.HighReadilyAvailableNitrogen ?? false) && !isPoultry;

            if (!model.ApplicationDate.HasValue ||
                string.IsNullOrWhiteSpace(closedPeriod) ||
                !closedPeriod.Contains("to"))
                return (model, error);

            var parts = closedPeriod.Split(" to ", StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return (model, error);

            DateTime applicationDate = model.ApplicationDate.Value;
            int year = applicationDate.Year;

            DateTime closedStartDate = DateTime.ParseExact($"{parts[0]} {year}", "d MMMM yyyy", CultureInfo.InvariantCulture);

            // Feb window
            string period = $"{parts[1]} to 14 February";
            var (febStart, febEnd) =
                GetClosedPeriodDates(period, applicationDate);

            // 28-day pre-closed window
            DateTime preStart = closedStartDate.AddDays(-28);
            DateTime preEnd = closedStartDate.AddDays(-1);

            bool isInFebPeriod = WarningWithinPeriod.IsApplicationDateWithinDateRange(applicationDate, febStart, febEnd);


            bool isInPreClosedPeriod =
                applicationDate >= preStart && applicationDate <= preEnd;

            if (!isInFebPeriod && !isInPreClosedPeriod)
                return (model, error);

            DateTime startDate = isInFebPeriod ? febStart : preStart;
            DateTime endDate = isInFebPeriod ? febEnd : preEnd;

            var (totalApplicationRate, _) =
                await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(
                    mannerEstimationId ?? 0,
                    startDate,
                    endDate,
                    mannerAppId);

            totalApplicationRate = model.ApplicationRate + totalApplicationRate ?? 0;

            await ApplyWarningsRanAndPoultryTotalRateLimit(
                model,
                warningList,
                isRanExceptPoultry,
                totalApplicationRate,
                isPoultry, isInFebPeriod);
            return (model, error);

        }


        private static async Task<(TModel, Error?)> ApplyWarningsRanAndPoultryTotalRateLimit<TModel>(TModel model, List<WarningResponse> warningList, bool isRanExceptPoultry, decimal? totalApplicationRate, bool isPoultry, bool isInFebPeriod) where TModel : MannerEstimationNWarningViewModel
        {
            Error? error = null;
            if (isRanExceptPoultry && totalApplicationRate > 30)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.Slurry4WeekPriorToClosedPeriodStart.ToString(), Resource.lblEndClosedPeriodEndFeb);
            }

            if (isPoultry && totalApplicationRate > 5)
            {
                string warningKey = isInFebPeriod ? NMP.Commons.Enums.WarningKey.PoultryManureMaxApplicationRate.ToString() : NMP.Commons.Enums.WarningKey.Poultry4WeekPriorToClosedPeriodStart.ToString();
                ApplyWarning(model, warningList, warningKey, Resource.lblEndClosedPeriodEndFeb);
            }
            return (model, error);
        }

        private static bool IsWithinClosedPeriodAndFeb(DateTime? applicationDate, string? closedPeriod)
        {
            if (!applicationDate.HasValue)
                return false;

            return WarningWithinPeriod.CheckEndClosedPeriodAndFebruary(applicationDate.Value, closedPeriod) == true;
        }

        private async Task<Error?> CheckCompostAndScotlandLimits<TModel>(
            TModel model,
            List<WarningResponse> warningList,
            decimal currentApplicationNitrogen,
            bool isScotland,
            bool isCompost, int? mannerEstimationId, int? mannerAppId)
            where TModel : MannerEstimationNWarningViewModel
        {
            Error? error;

            var cropTypeIdsForTrigger = new HashSet<int>
            {
                (int)NMP.Commons.Enums.CropTypes.CiderApples,
                (int)NMP.Commons.Enums.CropTypes.CulinaryApples,
                (int)NMP.Commons.Enums.CropTypes.DessertApples,
                (int)NMP.Commons.Enums.CropTypes.Cherries,
                (int)NMP.Commons.Enums.CropTypes.Pears,
                (int)NMP.Commons.Enums.CropTypes.Plums
            };

            int? cropTypeId;
            if (mannerAppId != null)
            {
                MannerEstimationResultResponse? result;
                (result, error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimationId ?? 0);
                cropTypeId = result?.MannerEstimation?.CropTypeID;
            }
            else
            {
                error = null;
                cropTypeId = model.CropTypeId;
            }

            bool isTriggerCrop = cropTypeIdsForTrigger.Contains(cropTypeId ?? 0);

            //pas100 warning for england/wales and scotland
            error = await CheckNFieldLimitPAS100Compost(model, warningList, currentApplicationNitrogen, isCompost, mannerEstimationId, mannerAppId);

            // Warning excel sheet row 4: >500 total N in last 730 days (compost/Scotland, non-trigger crops or Scotland)
            if (!isTriggerCrop || isScotland)
            {
                error = await CheckNFieldLimit500Compost(model, warningList, currentApplicationNitrogen, isScotland, isCompost, mannerEstimationId, mannerAppId);
            }

            // Warning excel sheet row 6: >1000 total N in last 1460 days (trigger crops)
            if (isTriggerCrop)
            {
                error = await CheckNFieldLimit1000CompostMulch(model, warningList, currentApplicationNitrogen, mannerEstimationId, mannerAppId);
            }

            return error;
        }

        private async Task<Error?> CheckNFieldLimit250<TModel>(
            TModel model, List<WarningResponse> warningList, decimal currentApplicationNitrogen, int? mannerEstimationId, int? mannerAppId)
            where TModel : MannerEstimationNWarningViewModel
        {

            var (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(
                mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, false, mannerAppId);

            if (error == null && (previousAppliedTotalN + currentApplicationNitrogen) > 250)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimit.ToString(), Resource.lblNFieldLimit);
            }

            return error;
        }

        private async Task<Error?> CheckNFieldLimitPAS100Compost<TModel>(
            TModel model, List<WarningResponse> warningList, decimal currentApplicationNitrogen, bool isCompost, int? mannerEstimationId, int? mannerAppId)
            where TModel : MannerEstimationNWarningViewModel
        {
            decimal previousAppliedTotalN = 0;
            Error? error = null;

            (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(
                    mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-364), model.ApplicationDate.Value, true, mannerAppId);

            if (error != null)
            {
                return error;
            }

            decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

            if (isCompost && totalN > 250)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostPAS.ToString(), Resource.lblNFieldLimit);
            }

            return error;
        }

        private async Task<Error?> CheckNFieldLimit500Compost<TModel>(
            TModel model, List<WarningResponse> warningList, decimal currentApplicationNitrogen, bool isScotland, bool isCompost, int? mannerEstimationId, int? mannerAppId)
            where TModel : MannerEstimationNWarningViewModel
        {
            decimal previousAppliedTotalN;
            Error? error;

            if (!isScotland)
            {
                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(
                    mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, true, mannerAppId);
            }
            else
            {
                (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNByMannerEstimationIdAppDate(
                    mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-729), model.ApplicationDate.Value, mannerAppId);
            }

            if (error != null)
            {
                return error;
            }

            decimal totalN = previousAppliedTotalN + currentApplicationNitrogen;

            bool isGreenCompostExistIn2Year;
            (isGreenCompostExistIn2Year, error) = await _mannerEstimationLogic.CheckMannerGreenCompostExistanceByDateRange(
                mannerEstimationId ?? 0,
                model.ApplicationDate.Value.AddDays(-729).ToString(_dateStringLiteral),
                model.ApplicationDate.Value.ToString(_dateStringLiteral),
                mannerAppId);

            if ((!isScotland || isGreenCompostExistIn2Year || isCompost) && totalN > 500)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompost.ToString(), Resource.lblNFieldLimit);
            }

            return error;
        }

        private async Task<Error?> CheckNFieldLimit1000CompostMulch<TModel>(
            TModel model, List<WarningResponse> warningList, decimal currentApplicationNitrogen, int? mannerEstimationId, int? mannerAppId)
            where TModel : MannerEstimationNWarningViewModel
        {
            var (previousAppliedTotalN, error) = await _mannerEstimationLogic.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(
                mannerEstimationId ?? 0, model.ApplicationDate.Value.AddDays(-1459), model.ApplicationDate.Value, true, mannerAppId);

            if (error == null && (previousAppliedTotalN + currentApplicationNitrogen) > 1000)
            {
                ApplyWarning(model, warningList, NMP.Commons.Enums.WarningKey.OrganicManureNFieldLimitCompostMulch.ToString(), Resource.lblNFieldLimit);
            }

            return error;
        }


        private static void ApplyWarning<TModel>(TModel model, List<WarningResponse> warningList, string warningKey, string warningType)
            where TModel : MannerEstimationNWarningViewModel
        {

            WarningResponse? warning = warningList.FirstOrDefault(x =>
                x.CountryID == model.CountryId &&
                string.Equals(x.WarningKey?.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));

            if (warning != null)
            {
                if (warningType.Equals(Resource.lblNFieldLimit))
                {
                    model.IsOrgManureNfieldLimitWarning = true;
                    model.NFieldLimitWarningHeader = warning.Header;
                    model.NFieldLimitWarningCodeID = warning.WarningCodeID;
                    model.NFieldLimitWarningLevelID = warning.WarningLevelID;
                    model.NFieldLimitWarningPara1 = warning.Para1;
                    model.NFieldLimitWarningPara2 = warning.Para2;
                    model.NFieldLimitWarningPara3 = warning.Para3;
                }
                if (warningType.Equals(Resource.lblEndClosedPeriodEndFeb))
                {
                    model.IsEndClosedPeriodFebruaryWarning = true;
                    model.EndClosedPeriodEndFebWarningHeader = warning.Header;
                    model.EndClosedPeriodEndFebWarningCodeID = warning.WarningCodeID;
                    model.EndClosedPeriodEndFebWarningLevelID = warning.WarningLevelID;
                    model.EndClosedPeriodEndFebWarningPara1 = warning.Para1;
                    model.EndClosedPeriodEndFebWarningPara2 = warning.Para2;
                    model.EndClosedPeriodEndFebWarningPara3 = warning.Para3;
                }
                if (warningType.Equals(Resource.lblHighNOrganicManureMaxRateGrass) || warningType.Equals(Resource.lblHighNOrganicManureMaxRate) || warningType.Equals(Resource.lblHighNOrganicManureMaxRateWeeks) || warningType.Equals(Resource.lblHighNOrganicManureMaxRateOSR))
                {
                    model.IsStartClosedPeriodEndFebWarning = true;
                    model.StartClosedPeriodEndFebWarningHeader = warning.Header;
                    model.StartClosedPeriodEndFebFebWarningCodeID = warning.WarningCodeID;
                    model.StartClosedPeriodEndFebWarningLevelID = warning.WarningLevelID;
                    model.StartClosedPeriodEndFebWarningPara1 = warning.Para1;
                    model.StartClosedPeriodEndFebWarningPara2 = warning.Para2;
                    model.StartClosedPeriodEndFebWarningPara3 = warning.Para3;
                }

            }
        }

        public async Task<IActionResult?> BindFarmFieldOrCropDataUpdate(string? q)//, string? r
        {
            if (!string.IsNullOrWhiteSpace(q))// && !string.IsNullOrWhiteSpace(r)
            {
                int mannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                Error? error = await _mannerEstimationLogic.BindMannerEstimationDataForUpdate(mannerEstimateId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_mannerEstimationResultErrorKey] = error.Message;
                    return RedirectToAction(_mannerEstimationResultKey, new
                    {
                        q = q

                    });
                }
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (mannerEstimationViewModel != null)
                {
                    mannerEstimationViewModel.EncryptedMannerEstimationId = q;
                    _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);
                }


            }
            return null;
        }

        public async Task<IActionResult> UpdateFieldOrCropData()
        {
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();

            (MannerEstimation? mannerEstimation, Error? error) = await _mannerEstimationLogic.UpdateFarmFieldAndCropData(mannerEstimationViewModel.MannerEstimationId.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new
                {
                    q = mannerEstimationViewModel.EncryptedMannerEstimationId

                });
            }
            string succesMsg = Resource.lblFarmFieldCropDataUpdated;
            return RedirectToResultWithSuccessValues(mannerEstimation.ID.Value, succesMsg, "FarmFieldAndCrop");
        }

        public async Task<IActionResult?> BindApplicationDetailForUpdate(string? q)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                int mannerEstimateApplicationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));

                Error? error = await _mannerEstimationLogic.BindApplicationDetailForUpdate(mannerEstimateApplicationId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_mannerEstimationResultErrorKey] = error.Message;
                    return RedirectToAction(_mannerEstimationResultKey, new
                    {
                        q = q

                    });
                }
                MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
                if (mannerEstimationViewModel != null)
                {
                    mannerEstimationViewModel.EncryptedMannerEstimationApplicationId = q;
                    mannerEstimationViewModel.EncryptedMannerEstimationId = _mannerEstimationProtector.Protect(mannerEstimationViewModel.MannerEstimationId.ToString());
                    _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);
                }
            }
            return null;
        }
        public async Task<IActionResult> UpdateApplicationData()
        {
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();

            (MannerEstimationApplication? mannerEstimationApplication, Error? error) = await _mannerEstimationLogic.UpdateMannerEstimationApplicationData();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new
                {
                    q = mannerEstimationViewModel.EncryptedMannerEstimationId
                });
            }
            (MannerEstimationResultResponse? mannerEstimationResultResponse, error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimationApplication.MannerEstimationID.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["Error"] = error.Message;
                return RedirectToAction(_mannerHubPageAction);
            }

            return RedirectToResult(mannerEstimationApplication, mannerEstimationResultResponse, true);
        }

        public async Task<IActionResult> Report(string? q)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} Report() action called");
            MannerEstimationReportViewModel model = new MannerEstimationReportViewModel();

            if (!string.IsNullOrWhiteSpace(q))
            {
                int estimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                (MannerEstimationResultResponse? mannerEstimationResultResponse, Error? error) =
                    await _mannerEstimationLogic.FetchMannerApplicationResultById(estimateId);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["Error"] = error.Message;
                    return RedirectToAction("MannerEstimationResult", new { q = q });
                }

                var mannerFarm = mannerEstimationResultResponse?.MannerFarm;
                var estimation = mannerEstimationResultResponse?.MannerEstimation;
                var applications = mannerEstimationResultResponse?.MannerEstimationApplication;
                if (estimation != null)
                {
                    int nitrogenValue = applications?.Sum(x => x.NitrogenValue) ?? 0;
                    int p2O5Value = applications?.Sum(x => x.PhosphateValue) ?? 0;
                    int potashValue = applications?.Sum(x => x.PotashValue) ?? 0;
                    ViewBag.TotalValue = nitrogenValue + p2O5Value + potashValue;
                    ViewBag.FarmName = mannerFarm?.Name;
                    ViewBag.PostCode = mannerFarm?.Postcode;
                    ViewBag.CountryId = mannerFarm?.CountryID;
                    Country? country = await _mannerLogic.FetchCountryById(mannerFarm?.CountryID ?? 0);
                    if (country != null)
                    {
                        ViewBag.CountryName = country.Name;
                    }

                    model.EncryptedMannerEstimateId = q;
                    model.FarmRB209CountryID = mannerFarm?.CountryID;

                    // Field and crop details
                    model.MannerFieldAndCropDetails.MannerEstimation = estimation;
                    model.MannerFieldAndCropDetails.MannerFarm = mannerFarm;
                }

                if (applications != null && applications.Any())
                {
                    foreach (var application in applications)
                    {
                        //warnings
                        await BindWarnings(model.MannerFieldAndCropDetails, application, model);

                        bool isLiquid = await _mannerEstimationLogic.FetchIsManureLiquid(application.ManureTypeID??0);

                        // Application details
                        model.MannerEstimationApplicationDetails.Add(new MannerEstimationApplicationDetailsViewModel
                        {
                            ID = application.ID,
                            MannerEstimationID = application.MannerEstimationID,
                            ManureTypeID = application.ManureTypeID,
                            ApplicationDate = application.ApplicationDate,
                            N = application.N,
                            P2O5 = application.P2O5,
                            K2O = application.K2O,
                            MgO = application.MgO,
                            SO3 = application.SO3,
                            DryMatterPercent = application.DryMatterPercent,
                            NH4N = application.NH4N,
                            NO3N = application.NO3N,
                            UricAcid = application.UricAcid,
                            ApplicationRate = application.ApplicationRate,
                            AreaSpread = application.AreaSpread,
                            ManureQuantity = application.ManureQuantity,
                            ApplicationMethodID = application.ApplicationMethodID,
                            IncorporationMethodID = application.IncorporationMethodID,
                            IncorporationDelayID = application.IncorporationDelayID,
                            WindspeedID = application.WindspeedID,
                            RainfallWithinSixHoursID = application.RainfallWithinSixHoursID,
                            MoistureID = application.MoistureID,
                            AutumnCropNitrogenUptake = application.AutumnCropNitrogenUptake,
                            EndOfDrainageDate = application.EndOfDrainageDate,
                            RainfallPostApplication = application.RainfallPostApplication,
                            TotalN = application.TotalN,
                            CropAvailableNCurrentCrop = application.CropAvailableNCurrentCrop,
                            CropAvailableNitrogenFollowingCropYearTwo = application.CropAvailableNitrogenFollowingCropYearTwo,
                            TotalP2O5 = application.TotalP2O5,
                            CropAvailableP2O5 = application.CropAvailableP2O5,
                            TotalSO3 = application.TotalSO3,
                            TotalMgO = application.TotalMgO,
                            TotalK2O = application.TotalK2O,
                            CropAvailableK2O = application.CropAvailableK2O,
                            CropAvailableSO3 = application.CropAvailableSO3,
                            NitrogenUseEfficiency = application.NitrogenUseEfficiency,
                            MineralisedNitrogenLosses = application.MineralisedNitrogenLosses,
                            LostNitrateLosses = application.LostNitrateLosses,
                            LostAmmonia = application.LostAmmonia,
                            LostDenitrified = application.LostDenitrified,
                            NitrogenValue = application.NitrogenValue,
                            PhosphateValue = application.PhosphateValue,
                            PotashValue = application.PotashValue,
                            CreatedOn = application.CreatedOn,
                            CreatedByID = application.CreatedByID,
                            ModifiedOn = application.ModifiedOn,
                            ModifiedByID = application.ModifiedByID,

                            // Display/lookup text fields
                            ManureType = application.ManureType,
                            Windspeed = application.Windspeed,
                            RainType = application.RainType,
                            MoistureType = application.MoistureType,
                            ApplicationMethod = application.ApplicationMethod,
                            IncorporationMethod = application.IncorporationMethod,
                            IncorporationDelay = application.IncorporationDelay,
                            IsManureTypeLiquid = isLiquid
                        });

                        // Manure analysis (one per application)
                        model.ManureAnalyses.Add(new MannerManureAnalysisViewModel
                        {
                            DryMatterContent = application.DryMatterPercent,
                            TotalNitrogen = application.N,
                            AmmoniumNitrogen = application.NH4N,
                            UricAcidNitrogen = application.UricAcid,
                            NitrateNitrogen = application.NO3N,
                            TotalPhosphate = application.P2O5,
                            TotalPotash = application.K2O,
                            TotalSulphur = application.SO3,
                            TotalMagnesium = application.MgO
                        });

                        // NPK results (one per application)
                        model.MannerNpkResults.Add(new MannerNpkResultViewModel
                        {
                            TotalNitrogen = application.TotalN,
                            CropAvailableNitrogenCurrentCrop = application.CropAvailableNCurrentCrop,
                            CropAvailableNitrogenFollowingCropYear2 = application.CropAvailableNitrogenFollowingCropYearTwo,
                            NitrogenUseEfficiency = application.NitrogenUseEfficiency,
                            MineralisedNitrogen = application.MineralisedNitrogenLosses,
                            LostNitrateNitrogen = application.LostNitrateLosses,
                            LostAmmoniaNitrogen = application.LostAmmonia,
                            LostDenitrifiedNitrogen = application.LostDenitrified,
                            TotalPhosphate = application.TotalP2O5,
                            CropAvailablePhosphate = application.CropAvailableP2O5,
                            TotalPotash = application.TotalK2O,
                            CropAvailablePotash = application.CropAvailableK2O,
                            TotalSulphur = application.TotalSO3,
                            CropAvailableSulphur = application.CropAvailableSO3,
                            TotalMagnesium = application.TotalMgO,
                            NitrogenValue = application.NitrogenValue,
                            PhosphateValue = application.PhosphateValue,
                            PotashValue = application.PotashValue,
                            TotalValue = application.NitrogenValue + application.PhosphateValue + application.PotashValue
                        });

                        // Conditions/Step32 (one per application)
                        model.MannerEstimationConditions.Add(new MannerEstimationStep32ViewModel
                        {
                            ApplicationMethodId = application.ApplicationMethodID,
                            SoilDrainageEndDate = application.EndOfDrainageDate,
                            RainfallWithinSixHoursId = application.RainfallWithinSixHoursID,
                            RainfallWithinSixHours = application.RainType,
                            WindspeedId = application.WindspeedID,
                            Windspeed = application.Windspeed,
                            MoistureTypeId = application.MoistureID,
                            MoistureType = application.MoistureType,
                            IncorporationMethodId = application.IncorporationMethodID,
                            AutumnCropNitrogenUptake = application.AutumnCropNitrogenUptake,
                            ApplicationDate = application.ApplicationDate,
                            // Fields not on MannerEstimationApplication — pulled from estimation if needed
                            PostCode = mannerFarm?.Postcode,
                            CropTypeId = estimation?.CropTypeID,
                            FieldName = estimation?.FieldName,
                            CropTypeName = estimation?.CropTypeName,
                            TotalRainfall = mannerFarm?.AverageAnuualRainfall
                        });
                    }
                }
            }

            return View(model);
        }
        private async Task BindWarnings(MannerEstimationDetailsViewModel estimation, MannerEstimationApplicationDetailsViewModel application, MannerEstimationReportViewModel model)
        {
            Error? error = null;

            string? closedPeriod = string.Empty;
            var dateWarningViewModel = new MannerEstimationStep13ViewModel();
            (ManureType? manureType, error) = await _mannerLogic.FetchManureTypeByManureTypeId(application.ManureTypeID ?? 0);
            if (manureType?.HighReadilyAvailableNitrogen == true && estimation.MannerEstimation.IsWithinNVZ == true)
            {
                int? cropGroupId = await _mannerEstimationLogic.GetCropGroupByCropTypeId(estimation.MannerEstimation.CropTypeID);
                bool isPerennial = await _cropLogic.FetchIsPerennialByCropTypeId(estimation.MannerEstimation.CropTypeID ?? 0);
                int fieldType = cropGroupId == (int)NMP.Commons.Enums.CropGroup.Grass ? (int)NMP.Commons.Enums.FieldType.Grass : (int)NMP.Commons.Enums.FieldType.Arable;

                bool isSandyShallowSoil = _mannerEstimationLogic.CheckSandyShallowByTopSoilSubSoilId(estimation.MannerEstimation.TopSoilID ?? 0, estimation.MannerEstimation.SubSoilID ?? 0, estimation.MannerFarm?.CountryID ?? 0);
                if (string.IsNullOrEmpty(error?.Message))
                {
                    closedPeriod = Functions.GetMannerClosedPeriod(isSandyShallowSoil, fieldType, estimation.MannerEstimation.SowingDate, estimation.MannerFarm.CountryID ?? 0, cropGroupId, estimation.MannerEstimation.CropTypeID ?? 0, isPerennial);
                }
                int harvestYear = GetHarvestYearFromApplicationDate(application.ApplicationDate);

                // --- date-based warnings ---
                dateWarningViewModel = new MannerEstimationStep13ViewModel
                {
                    ApplicationDate = application.ApplicationDate,
                    FieldName = estimation.MannerEstimation.FieldName ?? string.Empty,
                    ManureTypeName = application.ManureType ?? string.Empty,
                    CountryId = estimation.MannerFarm.CountryID ?? 0,
                    FarmRB209CountryId = estimation.MannerFarm.CountryID ?? 0,
                    CropTypeId = estimation.MannerEstimation.CropTypeID,
                    CropGroupId = cropGroupId,
                    TopSoilId = estimation.MannerEstimation.TopSoilID,
                    SubSoilId = estimation.MannerEstimation.SubSoilID,
                    SowingDate = estimation.MannerEstimation.SowingDate,
                    IsWithinNVZ = estimation.MannerEstimation.IsWithinNVZ,
                    IsFarmOrganic = estimation.MannerFarm.RegisteredOrganicProducer,
                    ManureTypeId = application.ManureTypeID,
                    ClosedPeriod = closedPeriod,
                    MannerEstimationId = estimation.MannerEstimation.ID,
                    MannerEstimationApplicationsId = application.ID,
                    IsWarningMsgNeedToShow = false,
                    IsClosedPeriodWarning = false,
                    IsApplicationJulyToSeptWarning = false,
                    IsEndClosedPeriodFebruaryExistWithinThreeWeeks = false
                };

                error = await CheckApplicationDateWarnings(dateWarningViewModel, manureType, harvestYear, persistToSession: false);
            }

            // --- N-field-limit warnings ---
            MannerEstimationNWarningViewModel nWarningViewModel = new MannerEstimationNWarningViewModel
            {
                ManureTypeId = application.ManureTypeID,
                ApplicationRate = application.ApplicationRate,
                ApplicationDate = application.ApplicationDate,
                CountryId = estimation.MannerFarm.CountryID ?? 0,
                MannerEstimationId = estimation.MannerEstimation.ID,
                CropTypeId = estimation.MannerEstimation.CropTypeID,
                UpdatedMannerAppId = application.ID,
                IsWithinNVZ = estimation.MannerEstimation.IsWithinNVZ,
                IsFarmOrganic = estimation.MannerFarm.RegisteredOrganicProducer,
                ClosedPeriod = closedPeriod,
                IsOrgManureNfieldLimitWarning = false,
                IsEndClosedPeriodFebruaryWarning = false,
                IsStartClosedPeriodEndFebWarning = false
            };

            (nWarningViewModel, error) = await NitrogenApplicationLimitWarningMessage(nWarningViewModel, estimation.MannerEstimation.ID, application.ID);

            // --- combine and store against this application ---
            var combinedWarnings = new List<WarningItemViewModel>();
            combinedWarnings.AddRange(BuildApplicationDateWarnings(dateWarningViewModel));
            combinedWarnings.AddRange(BuildNitrogenLimitWarnings(nWarningViewModel));

            model.ApplicationWarnings.Add(new MannerEstimationApplicationWarningViewModel
            {
                ApplicationId = application.ID,
                Warnings = combinedWarnings
            });
        }
        public async Task<IActionResult> AddApplicationData()
        {
            MannerEstimationViewModel? mannerEstimationViewModel = _mannerEstimationLogic.GetMannerEstimationFromSession();
            mannerEstimationViewModel.MannerEstimationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationViewModel.EncryptedMannerEstimationId));
            _mannerEstimationLogic.SetMannerEstimationToSession(mannerEstimationViewModel);
            (MannerEstimationApplication? mannerEstimationApplication, Error? error) = await _mannerEstimationLogic.AddMannerEstimationApplication();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new
                {
                    q = mannerEstimationViewModel.EncryptedMannerEstimationId
                });
            }
            (MannerEstimationResultResponse? mannerEstimationResultResponse, error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimationApplication.MannerEstimationID.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData[_mannerEstimationResultErrorKey] = error.Message;
                return RedirectToAction(_mannerEstimationResultKey, new
                {
                    q = mannerEstimationViewModel.EncryptedMannerEstimationId
                });
            }

            return RedirectToResult(mannerEstimationApplication, mannerEstimationResultResponse, false);
        }

        private IActionResult RedirectToResult(MannerEstimationApplication mannerEstimationApplication, MannerEstimationResultResponse? mannerEstimationResultResponse, bool isUpdate)
        {
            string succesMsg = isUpdate ? Resource.lblApplicationDetailUpdated : Resource.lblOrganicMaterialApplicationAdded;

            string tabName = "ApplicationDetails";
            if (mannerEstimationResultResponse?.MannerEstimationApplication != null)
            {
                int applicationNumber = mannerEstimationResultResponse.MannerEstimationApplication
            .FindIndex(x => x.ID == mannerEstimationApplication.ID) + 1;
                if (mannerEstimationResultResponse.MannerEstimationApplication.Count != 3)
                {
                    tabName = string.Format("{0}{1}", "ApplicationDetail", applicationNumber);
                }
                if (mannerEstimationResultResponse.MannerEstimationApplication.Count != 1)
                {
                    succesMsg = isUpdate ? string.Format(Resource.lblMannerApplicationDetailCountUpdated, applicationNumber) : succesMsg;
                    return RedirectToResultWithSuccessValues(mannerEstimationApplication.MannerEstimationID.Value, succesMsg, tabName);
                }
            }
            return RedirectToResultWithSuccessValues(mannerEstimationApplication.MannerEstimationID.Value, succesMsg, tabName);
        }

        private List<WarningItemViewModel> BuildApplicationDateWarnings(MannerEstimationStep13ViewModel model)
        {
            var warnings = new List<WarningItemViewModel>();

            if (model.IsClosedPeriodWarning)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.ClosedPeriodWarningHeader,
                    Para1 = model.ClosedPeriodWarningPara1,
                    Para2 = model.ClosedPeriodWarningPara2,
                    Para3 = model.ClosedPeriodWarningPara3,
                    CodeID = model.ClosedPeriodWarningCodeID,
                    LevelID = model.ClosedPeriodWarningLevelID
                });
            }

            if (model.IsApplicationJulyToSeptWarning)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.ApplicationJulyToSeptHeader,
                    Para1 = model.ApplicationJulyToSeptPara1,
                    Para2 = model.ApplicationJulyToSeptPara2,
                    Para3 = model.ApplicationJulyToSeptPara3,
                    CodeID = model.ApplicationJulyToSeptCodeID,
                    LevelID = model.ApplicationJulyToSeptLevelID
                });
            }

            if (model.IsEndClosedPeriodFebruaryExistWithinThreeWeeks)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.EndClosedPeriodFebruaryExistWithinThreeWeeksHeader,
                    Para1 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara1,
                    Para2 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara2,
                    Para3 = model.EndClosedPeriodFebruaryExistWithinThreeWeeksPara3,
                    CodeID = model.EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID,
                    LevelID = model.EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID
                });
            }

            return warnings;
        }
        private List<WarningItemViewModel> BuildNitrogenLimitWarnings(MannerEstimationNWarningViewModel model)
        {
            var warnings = new List<WarningItemViewModel>();

            if (model.IsOrgManureNfieldLimitWarning)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.NFieldLimitWarningHeader,
                    Para1 = model.NFieldLimitWarningPara1,
                    Para2 = model.NFieldLimitWarningPara2,
                    Para3 = model.NFieldLimitWarningPara3,
                    CodeID = model.NFieldLimitWarningCodeID,
                    LevelID = model.NFieldLimitWarningLevelID
                });
            }
            if (model.IsEndClosedPeriodFebruaryWarning)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.EndClosedPeriodEndFebWarningHeader,
                    Para1 = model.EndClosedPeriodEndFebWarningPara1,
                    Para2 = model.EndClosedPeriodEndFebWarningPara2,
                    Para3 = model.EndClosedPeriodEndFebWarningPara3,
                    CodeID = model.EndClosedPeriodEndFebWarningCodeID,
                    LevelID = model.EndClosedPeriodEndFebWarningLevelID
                });
            }
            if (model.IsStartClosedPeriodEndFebWarning)
            {
                warnings.Add(new WarningItemViewModel
                {
                    Header = model.StartClosedPeriodEndFebWarningHeader,
                    Para1 = model.StartClosedPeriodEndFebWarningPara1,
                    Para2 = model.StartClosedPeriodEndFebWarningPara2,
                    Para3 = model.StartClosedPeriodEndFebWarningPara3,
                    CodeID = model.StartClosedPeriodEndFebFebWarningCodeID,
                    LevelID = model.StartClosedPeriodEndFebWarningLevelID
                });
            }

            return warnings;
        }


        [HttpGet]
        public async Task<IActionResult> RemoveEstimations(string? q)
        {
            MannerEstimationStep40ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep40();
            model.EncryptedMannerFarmId = q;
            _mannerEstimationLogic.SetMannerEstimationStep40(model);
            int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
            await FetchMannerEstimationSelectList(mannerFarmId);
            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> RemoveEstimations(MannerEstimationStep40ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  RemoveEstimations() post action called");
            try
            {
                if (model.MannerEstimationIdList == null)
                {
                    ModelState.AddModelError("MannerEstimationIdList", Resource.MsgSelectAtLeastOneNutrientSupplyEstimateToRemove);
                }

                MannerEstimationStep40ViewModel mannerEstimationStep40ViewModel = _mannerEstimationLogic.GetMannerEstimationStep40();
                if (!ModelState.IsValid)
                {
                    int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep40ViewModel.EncryptedMannerFarmId));
                    await FetchMannerEstimationSelectList(mannerFarmId);
                    return View(mannerEstimationStep40ViewModel);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep40(model);

                if (model.MannerEstimationIdList.Contains(Resource.lblSelectAll))
                {
                    int mannerFarmId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep40ViewModel.EncryptedMannerFarmId));
                    await FetchMannerEstimationSelectList(mannerFarmId);
                    SelectAllLogic(model, ViewBag.MannerEstimationIdList);
                }
                List<int> mannerEstimationIds = new List<int>();

                foreach (string estimationId in model.MannerEstimationIdList)
                {
                    mannerEstimationIds.Add(Convert.ToInt32(estimationId));
                }
                var result = new
                {
                    mannerEstimationIds
                };

                string jsonString = JsonConvert.SerializeObject(result);
                Error? error = await _mannerEstimationLogic.RemoveMannerEstimations(jsonString);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["MannerEstimationRemoveError"] = error.Message;
                    return View(model);
                }
                else
                {
                    return RedirectToAction(_mannerHubPageAction, new
                    {
                        q = mannerEstimationStep40ViewModel.EncryptedMannerFarmId,
                        r = _mannerEstimationProtector.Protect(Resource.lblTrue)
                    });
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in RemoveEstimations() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in RemoveEstimations() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        private async Task FetchMannerEstimationSelectList(int mannerFarmId)
        {
            (List<MannerEstimationSummaryViewModel> mannerEstimations, Error? error) = await _mannerEstimationLogic.FetchMannerEstimateByFarmId(mannerFarmId);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["Error"] = error.Message;
            }
            if (string.IsNullOrWhiteSpace(error?.Message) && mannerEstimations.Count > 0)
            {
                foreach (var estimation in mannerEstimations)
                {
                    estimation.EncryptedId = _mannerEstimationProtector.Protect(estimation.ID.ToString());
                }
                var selectList = ToSelectList(mannerEstimations, f => f.ID.ToString(), f => string.Format(Resource.lblRemoveEstimationNames, f.Name, f.FarmName, f.ModifiedOn != null ? f.ModifiedOn.Value.ToString(_dateFormat) : f.CreatedOn.Value.ToString(_dateFormat)))
                                .OrderBy(x => x.Text)
                                .ToList();
                ViewBag.MannerEstimationIdList = selectList;
            }
        }
        private static void SelectAllLogic(MannerEstimationStep40ViewModel model, List<SelectListItem> fieldSelectList)
        {
            if (model.MannerEstimationIdList.Contains(Resource.lblSelectAll))
                model.MannerEstimationIdList = fieldSelectList
                    .Where(x => x.Value != Resource.lblSelectAll)
                    .Select(x => x.Value)
                    .ToList();
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
        public async Task<IActionResult?> RemoveMannerEstimateApplication(string? q)
        {
            MannerEstimationStep41ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep41();
            if (!string.IsNullOrWhiteSpace(q))
            {
                int mannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(q));
                model.EncryptedMannerEstimateId = q;
                _mannerEstimationLogic.SetMannerEstimationStep41(model);
                (MannerEstimationResultResponse? mannerEstimationResult, Error? error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimateId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_mannerEstimationResultErrorKey] = error.Message;
                    return RedirectToAction(_mannerEstimationResultKey, new
                    {
                        q = q

                    });
                }
                ViewBag.ApplicationList = BindApplicationList(mannerEstimationResult);
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMannerEstimateApplication(MannerEstimationStep41ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  UpdatePotashPrice() post action called");
            MannerEstimationStep41ViewModel mannerEstimationStep41ViewModel = _mannerEstimationLogic.GetMannerEstimationStep41();
            Error? error = null;
            if (model.MannerEstimateApplicationId == null)
            {
                ModelState.AddModelError("MannerEstimateApplicationId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                int mannerEstimateId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep41ViewModel.EncryptedMannerEstimateId));
                (MannerEstimationResultResponse? mannerEstimationResult, error) = await _mannerEstimationLogic.FetchMannerApplicationResultById(mannerEstimateId);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData[_mannerEstimationResultErrorKey] = error.Message;
                    return RedirectToAction(_mannerEstimationResultKey, new
                    {
                        q = mannerEstimationStep41ViewModel.EncryptedMannerEstimateId

                    });
                }
                ViewBag.ApplicationList = BindApplicationList(mannerEstimationResult);
                return View(mannerEstimationStep41ViewModel);
            }

            int mannerApplicationId = Convert.ToInt32(_mannerEstimationProtector.Unprotect(mannerEstimationStep41ViewModel.EncryptedMannerEstimateId));
            (_, error) = await _mannerEstimationLogic.DeleteMannerEstimateApplicationById(model.MannerEstimateApplicationId.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["RemoveMannerEstimateApplicationError"] = error.Message;
                return View(model);
            }

            string successMsg = Resource.lblOrganicMaterialApplicationRemoved;

            return RedirectToResultWithSuccessValues(mannerApplicationId, successMsg, "Nutrients");

        }

        private static List<SelectListItem> BindApplicationList(MannerEstimationResultResponse mannerEstimationResult)
        {
            List<MannerEstimationApplicationDetailsViewModel>? mannerEstimationApplication = mannerEstimationResult.MannerEstimationApplication.ToList();
            List<SelectListItem> selectListItem = mannerEstimationApplication
     .Select((x, index) => new SelectListItem
     {
         Value = x.ID.ToString(),
         Text = $"Application {index + 1}",
         Group = new SelectListGroup
         {
             Name = $"{x.ManureType}, {(x.ApplicationDate.ToLocalTime()):dd MMM yyyy}"
         }
     })
     .ToList();
            return selectListItem;

        }
        public async Task<IActionResult> MannerFarmList(string? q, string? r)
        {
            RemoveMannerEstimationSession();
            ViewBag.IsNewFarm = _mannerEstimationProtector.Protect(Resource.lblFalse);
            Guid organisationId = GetOrganisationId();
            (List<MannerFarmViewModel> mannerFarmList, _) = await _mannerEstimationLogic.FetchMannerFarmListByOrgId(organisationId);
            if (mannerFarmList.Count > 0)
            {
                foreach (var mannerfarm in mannerFarmList)
                {
                    mannerfarm.EncryptedId = _mannerEstimationProtector.Protect(mannerfarm.ID.ToString());
                }
            }

            HttpContext.Session.SetString("is_current_manner_estimate", Resource.lblTrue);
            HttpContext.Session.SetString("is_manner_estimate_section", Resource.lblTrue);
            HttpContext.Session.Remove("current_manner_estimate_farm_name");
            HttpContext.Session.Remove("current_manner_estimate_manner_farm_id");
            ViewBag.MannerFarmList = mannerFarmList.OrderBy(x => x.Name).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Success = Resource.lblTrue;
            }
            else if (mannerFarmList.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(r))
                {
                    return RedirectToAction("Index", "DashBoard", new { area = "" });
                }
                return RedirectToAction("Name", new { s = _mannerEstimationProtector.Protect(Resource.lblFalse) });
            }
            return View();
        }

        public async Task<IActionResult> AddNewMannerEstimate()
        {
            (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error)
                    = await _mannerEstimationLogic.AddNewMannerEstimation();

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                TempData["ConditionsAffectingNutrientsError"] = error.Message;
                return RedirectToAction(_conditionsAffectingNutrients);
            }

            if (mannerEstimationApplicationResult != null && mannerEstimationApplicationResult.MannerEstimationID != null)
            {
                return RedirectToAction(_mannerEstimationResultKey, new
                {
                    q = _mannerEstimationProtector.Protect(
                      mannerEstimationApplicationResult.MannerEstimationID.ToString()),
                    r = _mannerEstimationProtector.Protect(Resource.lblTrue)
                });
            }
            return RedirectToAction(_conditionsAffectingNutrients);
        }
        [HttpGet]
        public async Task<IActionResult> RemoveMannerFarm(string? q)
        {
            MannerEstimationStep42ViewModel model = _mannerEstimationLogic.GetMannerEstimationStep42();
            await FetchRemoveMannerFarmSelectList();
            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> RemoveMannerFarm(MannerEstimationStep42ViewModel model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  RemoveEstimations() post action called");
            try
            {
                if (model.MannerFarmIdList == null)
                {
                    ModelState.AddModelError("MannerFarmIdList", Resource.MsgSelectAtLeastOneNutrientSupplyEstimateToRemove);
                }

                MannerEstimationStep42ViewModel mannerEstimationStep42ViewModel = _mannerEstimationLogic.GetMannerEstimationStep42();
                if (!ModelState.IsValid)
                {
                    await FetchRemoveMannerFarmSelectList();
                    return View(mannerEstimationStep42ViewModel);
                }

                model = _mannerEstimationLogic.SetMannerEstimationStep42(model);

                if (model.MannerFarmIdList.Contains(Resource.lblSelectAll))
                {
                    List<SelectListItem> mannerFarmList = await FetchRemoveMannerFarmSelectList();
                    if (model.MannerFarmIdList.Contains(Resource.lblSelectAll) && mannerFarmList != null)
                    {
                        model.MannerFarmIdList = mannerFarmList
                            .Where(x => x.Value != Resource.lblSelectAll)
                            .Select(x => x.Value)
                            .ToList();
                    }
                }
                List<int> mannerFarmsIds = new List<int>();

                foreach (string farmId in model.MannerFarmIdList)
                {
                    mannerFarmsIds.Add(Convert.ToInt32(farmId));
                }
                var result = new
                {
                    mannerFarmsIds
                };

                string jsonString = JsonConvert.SerializeObject(result);
                Error? error = await _mannerEstimationLogic.RemoveMannerFarms(jsonString);
                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    TempData["MannerFarmRemoveError"] = error.Message;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("MannerFarmList", new
                    {
                        q = _mannerEstimationProtector.Protect(Resource.lblTrue)
                    });
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in RemoveEstimations() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in RemoveEstimations() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }
        private async Task<List<SelectListItem>> FetchRemoveMannerFarmSelectList()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            Guid organisationId = GetOrganisationId();
            (List<MannerFarmViewModel> mannerFarmList, _) = await _mannerEstimationLogic.FetchMannerFarmListByOrgId(organisationId);
            if (mannerFarmList.Count > 0)
            {
                foreach (var mannerfarm in mannerFarmList)
                {
                    mannerfarm.EncryptedId = _mannerEstimationProtector.Protect(mannerfarm.ID.ToString());
                }
                var selectList = ToSelectList(mannerFarmList, f => f.ID.ToString(), f => string.Format(Resource.lblRemoveMannerFarm, f.Name, f.LastUpdatedDate != null ? f.LastUpdatedDate.Value.ToString(_dateFormat) : f.CreatedOn.Value.ToString(_dateFormat)))
                            .OrderBy(x => x.Text)
                            .ToList();
                selectListItems = selectList;
                ViewBag.MannerFarmIdList = selectList;
            }
            return selectListItems;



        }
    }
}