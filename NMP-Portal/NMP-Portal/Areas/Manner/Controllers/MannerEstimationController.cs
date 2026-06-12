using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Application;
using NMP.Businesses;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Controllers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NMP.Portal.Areas.Manner.Controllers
{
    [Area("Manner")]
    [Authorize]
    public class MannerEstimationController(ILogger<MannerEstimationController> logger, IFarmLogic farmLogic, IMannerLogic mannerLogic, IDataProtectionProvider dataProtectionProvider, IFieldLogic fieldLogic) : Controller
    {
        private readonly ILogger<MannerEstimationController> _logger = logger;
        private readonly IFarmLogic _farmLogic = farmLogic;
        private readonly IMannerLogic _mannerLogic = mannerLogic;

        private readonly IFieldLogic _fieldLogic = fieldLogic;
        private const string _checkAnswerActionName = "CheckAnswer";
        private readonly IDataProtector _mannerEstimationProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.MannerEstimationController");
        private const string _mannerEstimationSessionName = "MannerEstimation";
        private const string _mannerEstimationControllerForLog = "MannerEstimation  Controller : ";
        private const string _organisationId = "organisationId";

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult MannerHubPage(string? q)
        {
            RemoveMannerEstimationSession();
            if (!string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "" });
            }

            return RedirectToAction("CopyExistingFarmAndFieldDetails");
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
            MannerEstimationStep1ViewModel model = _mannerLogic.GetMannerEstimationStep1();
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
                ModelState.AddModelError("FarmName", Resource.MsgEnterTheFarmName);
            }
            List<SelectListItem> farmsWithFields = await BindAllFarmList();
            if (farmsWithFields.Count > 0)
            {
                model.IsFarmCopied = true;
            }

            if (!ModelState.IsValid)
            {

                return View(model);
            }

            model = _mannerLogic.SetMannerEstimationStep1(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("Country");
        }

        [HttpGet]
        public async Task<IActionResult> Country()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  Country() action called");


            MannerEstimationStep2ViewModel model = _mannerLogic.GetMannerEstimationStep2();
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
                    model = _mannerLogic.GetMannerEstimationStep2();
                    ViewBag.CountryList = await _farmLogic.FetchCountryAsync();
                    return View("Country", model);
                }

                model = await _mannerLogic.SetMannerEstimationStep2(model);

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

            MannerEstimationStep2ViewModel model = _mannerLogic.GetMannerEstimationStep2();
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
            MannerEstimationStep3ViewModel model = _mannerLogic.GetMannerEstimationStep3();

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
                    model = _mannerLogic.GetMannerEstimationStep3();
                    return View(model);
                }

                model = await _mannerLogic.SetMannerEstimationStep3(model);

                if (model.IsCheckAnswer && !model.IsPostCodeChange)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
                return RedirectToAction("IsFarmOrganic");
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
                MannerEstimationStep4ViewModel model = await _mannerLogic.GetMannerEstimationStep4();

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


            model = await _mannerLogic.GetMannerEstimationStep4();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("FieldName");
        }

        [HttpGet]
        public async Task<IActionResult> AverageAnnualRainfallManual()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} AverageAnnualRainfallManual() action called");
            MannerEstimationStep4ViewModel model = await _mannerLogic.GetMannerEstimationStep4();

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
                model = await _mannerLogic.GetMannerEstimationStep4();
                return View(model);
            }
            model = await _mannerLogic.SetMannerEstimationStep4(model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }
            return RedirectToAction("FieldName");
        }

        private void ValidateRainfall(MannerEstimationStep4ViewModel model)
        {
            string key = Resource.lblAverageAnnualRainfallForError;
            if ((!ModelState.IsValid) && ModelState.ContainsKey(key))
            {
                var RainfallError = ModelState[key]?.Errors.Count > 0 ?
                                ModelState[key]?.Errors[0].ErrorMessage.ToString() : null;

                if (RainfallError != null && RainfallError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[key]?.RawValue, Resource.lblAverageAnnualRainfallForError)))
                {
                    ModelState[key]?.Errors.Clear();
                    ModelState[key]?.Errors.Add(Resource.MsgForRainfallManual);
                }
            }


            if (model.AverageAnnualRainfall == 0)
            {
                ModelState.AddModelError(key, Resource.MsgEnterTheAverageAnnualRainfall);
            }

            if (model.AverageAnnualRainfall < 0)
            {
                ModelState.AddModelError(key, Resource.MsgEnterANumberWhichIsGreaterThanZero);
            }
        }

        [HttpGet]
        public IActionResult FieldName()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FieldName() action called");
            MannerEstimationStep5ViewModel model = _mannerLogic.GetMannerEstimationStep5();

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
                return View(model);
            }

            model = _mannerLogic.SetMannerEstimationStep5(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("NVZField");
        }

        [HttpGet]
        public IActionResult NVZField()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} NVZField() action called");
            MannerEstimationStep6ViewModel model = _mannerLogic.GetMannerEstimationStep6();

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
                return View(model);
            }

            model = _mannerLogic.SetMannerEstimationStep6(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("TopSoil");
        }

        [HttpGet]
        public async Task<IActionResult> SoilType()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} SoilType() action called");

            MannerEstimationStep7ViewModel model = _mannerLogic.GetMannerEstimationStep7();
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
                model = _mannerLogic.GetMannerEstimationStep7();
                ViewBag.SoilTypesList = await _mannerLogic.FetchSoilTypesByRB209CountryId(model.FarmRB209CountryId);
                return View(model);
            }

            model = _mannerLogic.SetMannerEstimationStep7(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropGroup");
        }
        [HttpGet]
        public async Task<IActionResult> CropGroup()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropGroup() action called");
            MannerEstimationStep8ViewModel model = _mannerLogic.GetMannerEstimationStep8();

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
                ViewBag.CropGroupList = await _fieldLogic.FetchCropGroups();
                return View(model);
            }

            model.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupId ?? 0);
            model = _mannerLogic.SetMannerEstimationStep8(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("CropType");
        }

        [HttpGet]
        public async Task<IActionResult> CropType()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} CropType() action called");

            MannerEstimationStep9ViewModel model = _mannerLogic.GetMannerEstimationStep9();
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

                ViewBag.CropTypeList = await _fieldLogic.FetchCropTypes(model.CropGroupId ?? 0, model.FarmRB209CountryId);
                return View(model);
            }

            model.CropGroupName = await _fieldLogic.FetchCropGroupById(model.CropGroupId ?? 0);
            model = _mannerLogic.SetMannerEstimationStep9(model);

            if (model.CropTypeId != null && Enum.IsDefined(typeof(NMP.Commons.Enums.EarlyOrLateSownCropTypes), model.CropTypeId))
            {
                return RedirectToAction("IsEarlySown");
            }

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ManureGroup");
        }

        [HttpGet]
        public IActionResult IsEarlySown()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} IsEarlySown() action called");

            MannerEstimationStep10ViewModel model = _mannerLogic.GetMannerEstimationStep10();
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

            model = _mannerLogic.SetMannerEstimationStep10(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ManureGroup");
        }
        [HttpGet]
        public async Task<IActionResult> ManureGroup()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ManureGroup() action called");

            MannerEstimationStep11ViewModel model = _mannerLogic.GetMannerEstimationStep11();
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
                model = _mannerLogic.GetMannerEstimationStep11();
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
            model = _mannerLogic.SetMannerEstimationStep11(model);

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

            MannerEstimationStep12ViewModel model = _mannerLogic.GetMannerEstimationStep12();
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
                model = _mannerLogic.GetMannerEstimationStep12();
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
            model = _mannerLogic.SetMannerEstimationStep12(model);

            return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("ApplicationDate");
        }
        [HttpGet]
        public IActionResult ApplicationDate()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} ApplicationDate() action called");

            MannerEstimationStep13ViewModel model = _mannerLogic.GetMannerEstimationStep13();
            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog} Session not found in SoilType() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> CopyExistingFarmAndFieldDetails()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  CopyExistingFarmAndFieldDetails() action called");
            MannerEstimationStep14ViewModel model = _mannerLogic.GetMannerEstimationStep14();
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
                    return RedirectToAction("FarmName");
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
                    model = _mannerLogic.GetMannerEstimationStep14();
                    await BindAllFarmList();
                    return View(model);
                }

                model = _mannerLogic.SetMannerEstimationStep14(model);
                string action = "FarmName";
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
            Claim? claim = HttpContext.User.FindFirst(_organisationId);
            string orgId = claim != null ? claim.Value : Guid.Empty.ToString();
            Guid.TryParse(orgId, out Guid organisationId);
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
            MannerEstimationStep15ViewModel model = _mannerLogic.GetMannerEstimationStep15();
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
                    model = _mannerLogic.GetMannerEstimationStep15();
                    List<SelectListItem> farmsWithFields = await BindAllFarmList();
                    if (farmsWithFields.Count > 0)
                    {
                        ViewBag.FarmList = farmsWithFields;
                    }
                    return View(model);
                }

                model = _mannerLogic.SetMannerEstimationStep15(model);

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
            MannerEstimationStep16ViewModel model = _mannerLogic.GetMannerEstimationStep16();
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
                    model = _mannerLogic.GetMannerEstimationStep16();
                    List<SelectListItem> fieldList = await BindAllFieldList(model.FarmId.Value);
                    if (fieldList.Count > 0)
                    {
                        ViewBag.FieldList = fieldList;
                    }
                    return View(model);
                }

                model = _mannerLogic.SetMannerEstimationStep16(model);

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
            MannerEstimationStep17ViewModel model = _mannerLogic.GetMannerEstimationStep17();
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
                    return View(model);
                }

                model = _mannerLogic.SetMannerEstimationStep17(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("AverageAnnualRainfall");
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
            MannerEstimationStep18ViewModel model = _mannerLogic.GetMannerEstimationStep18();
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
                    model = _mannerLogic.GetMannerEstimationStep18();
                    await BindAllTopsoilList();
                    return View(model);
                }

                model = _mannerLogic.SetMannerEstimationStep18(model);

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
        [HttpGet]
        public async Task<IActionResult> SubSoil()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  SubSoil() action called");
            MannerEstimationStep19ViewModel model = _mannerLogic.GetMannerEstimationStep19();
            try
            {
                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, $"{_mannerEstimationControllerForLog}  HttpRequestException in SubSoil() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_mannerEstimationControllerForLog}  Exception in TopSSubSoiloil() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
