using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Businesses;
using NMP.Commons.Helpers;
using NMP.Commons.Resources;
using NMP.Commons.ViewModels;
using NMP.Portal.Controllers;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace NMP.Portal.Areas.Manner.Controllers
{
    [Area("Manner")]
    [Authorize]
    public class MannerEstimationController(ILogger<CropController> logger, IFarmLogic farmLogic, IMannerLogic mannerLogic, IDataProtectionProvider dataProtectionProvider) : Controller
    {
        private readonly ILogger<CropController> _logger = logger;
        private readonly IFarmLogic _farmLogic = farmLogic;
        private readonly IMannerLogic _mannerLogic = mannerLogic;
        private const string _checkAnswerActionName = "CheckAnswer";
        private readonly IDataProtector _mannerEstimationProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.MannerEstimationController");
        private const string _mannerEstimationSessionName = "MannerEstimation";
        private const string _mannerEstimationControllerForLog = "MannerEstimation  Controller : ";

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult MannerHubPage(string? q)
        {
            RemoveMannerEstimationSession();
            if (!string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("FarmList", "Farm", new { area = "" });
            }
            return RedirectToAction("FarmName");
        }

        public IActionResult MannerEstimationCancel()
        {
            _logger.LogTrace("MannerEstimation Controller : MannerEstimationCancel() action called");
            return RedirectToAction("MannerHubPage", new { q = _mannerEstimationProtector.Protect(Resource.lblTrue) });
        }

        private MannerEstimationViewModel? GetMannerEstimationFromSession()
        {
            if (HttpContext.Session.Exists(_mannerEstimationSessionName))
            {
                return HttpContext.Session.GetObjectFromJson<MannerEstimationViewModel>(_mannerEstimationSessionName);
            }
            return null;
        }


        private void RemoveMannerEstimationSession()
        {
            if (HttpContext.Session.Exists(_mannerEstimationSessionName))
            {
                HttpContext.Session.Remove(_mannerEstimationSessionName);
            }
        }

        [HttpGet]
        public IActionResult FarmName()
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmName() action called");
            MannerEstimationStep1 model = _mannerLogic.GetMannerEstimationStep1();
            ViewBag.IsBack = _mannerEstimationProtector.Protect(Resource.lblTrue);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FarmName(MannerEstimationStep1 model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog} FarmName() post action called");

            if (string.IsNullOrWhiteSpace(model.FarmName))
            {
                ModelState.AddModelError("FarmName", Resource.MsgEnterTheFarmName);
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
            MannerEstimationStep2 model = _mannerLogic.GetMannerEstimationStep2();
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
        public async Task<IActionResult> Country(MannerEstimationStep2 model)
        {
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  Country() post action called");
            try
            {
                if (model.CountryID == 0)
                {
                    ModelState.AddModelError("CountryID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCountry.ToLower()));
                }

                if (!ModelState.IsValid)
                {
                    model = _mannerLogic.GetMannerEstimationStep2();
                    ViewBag.CountryList = await _farmLogic.FetchCountryAsync();
                    return View("Country", model);
                }

                model = _mannerLogic.SetMannerEstimationStep2(model);

                return model.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("FarmingRules");
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
            _logger.LogTrace($"{_mannerEstimationControllerForLog}  FarmingRules() action called");
            MannerEstimationStep2 model = _mannerLogic.GetMannerEstimationStep2();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
        public IActionResult FarmingRules(MannerEstimationStep2 model)
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
            MannerEstimationStep3 model = _mannerLogic.GetMannerEstimationStep3();

            if (model == null)
            {
                _logger.LogError($"{_mannerEstimationControllerForLog}  Session not found in PostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
    }
}
