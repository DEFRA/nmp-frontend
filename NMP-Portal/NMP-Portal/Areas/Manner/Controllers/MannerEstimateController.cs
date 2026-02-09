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
    public class MannerEstimateController(ILogger<CropController> logger, IFarmLogic farmLogic, IDataProtectionProvider dataProtectionProvider) : Controller
    {
        private readonly ILogger<CropController> _logger = logger;
        private readonly IFarmLogic _farmLogic = farmLogic;
        private const string _checkAnswerActionName = "CheckAnswer";
        private readonly IDataProtector _mannerEstimateProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.MannerEstimateController");
        private const string _mannerEstimateActionName = "MannerEstimate";

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult MannerHubPage(string? q)
        {
            RemoveMannerEstimateSession();
            if (!string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("FarmList", "Farm", new { area = "" });
            }
            return RedirectToAction("FarmName");
        }

        public IActionResult MannerEstimateCancel()
        {
            _logger.LogTrace("MannerEstimate Controller : MannerEstimateCancel() action called");
            return RedirectToAction("MannerHubPage", new { q = _mannerEstimateProtector.Protect(Resource.lblTrue) });
        }

        private MannerEstimateViewModel? GetMannerEstimateFromSession()
        {
            if (HttpContext.Session.Exists(_mannerEstimateActionName))
            {
                return HttpContext.Session.GetObjectFromJson<MannerEstimateViewModel>(_mannerEstimateActionName);
            }
            return null;
        }

        private void SetMannerEstimateToSession(MannerEstimateViewModel farm)
        {
            HttpContext.Session.SetObjectAsJson(_mannerEstimateActionName, farm);
        }

        private void RemoveMannerEstimateSession()
        {
            if (HttpContext.Session.Exists(_mannerEstimateActionName))
            {
                HttpContext.Session.Remove(_mannerEstimateActionName);
            }
        }

        [HttpGet]
        public IActionResult FarmName()
        {
            _logger.LogTrace("MannerEstimate  Controller : FarmName() action called");
            MannerEstimateViewModel? model = GetMannerEstimateFromSession();
            if (model == null)
            {
                model = new MannerEstimateViewModel();
            }
            ViewBag.IsBack = _mannerEstimateProtector.Protect(Resource.lblTrue);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FarmName(MannerEstimateViewModel farm)
        {
            _logger.LogTrace("MannerEstimate Controller : Name() post action called");

            if (string.IsNullOrWhiteSpace(farm.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFarmName);
            }

            if (!ModelState.IsValid)
            {
                return View(farm);
            }

            SetMannerEstimateToSession(farm);

            return farm.IsCheckAnswer ? RedirectToAction(_checkAnswerActionName) : RedirectToAction("Country");
        }

        [HttpGet]
        public async Task<IActionResult> Country()
        {
            _logger.LogTrace("MannerEstimate Controller : Country() action called");
            MannerEstimateViewModel? model = GetMannerEstimateFromSession();

            try
            {
                if (model == null)
                {
                    _logger.LogError("MannerEstimate Controller : Session not found in Country() action");
                    return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
                }

                ViewBag.CountryList = await _farmLogic.FetchCountryAsync();

                return View(model);
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "MannerEstimate Controller : HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimate Controller : Exception in Country() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Country(MannerEstimateViewModel model)
        {
            _logger.LogTrace("MannerEstimate Controller : Country() post action called");
            try
            {
                if (model.CountryID == null)
                {
                    ModelState.AddModelError("CountryID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCountry.ToLower()));
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.CountryList = await _farmLogic.FetchCountryAsync();
                    return View("Country", model);
                }

                model.EnglishRues = model.CountryID != (int)NMP.Commons.Enums.FarmCountry.Scotland;

                if (model.CountryID.HasValue && Enum.IsDefined(typeof(NMP.Commons.Enums.FarmCountry), model.CountryID.Value))
                {
                    model.Country = Enum.GetName(typeof(NMP.Commons.Enums.FarmCountry), model.CountryID);
                }

                SetMannerEstimateToSession(model);

                if (model.IsCheckAnswer)
                {
                    return RedirectToAction(_checkAnswerActionName);
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "MannerEstimate Controller : HttpRequestException in Country() action");
                return Functions.RedirectToErrorHandler((int)(hre.StatusCode ?? HttpStatusCode.InternalServerError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MannerEstimate Controller : Exception in Country() post action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.InternalServerError);
            }

            return RedirectToAction("FarmingRules");
        }
        [HttpGet]
        public IActionResult FarmingRules()
        {
            _logger.LogTrace("MannerEstimate Controller : FarmingRules() action called");
            MannerEstimateViewModel? model = GetMannerEstimateFromSession();

            if (model == null)
            {
                _logger.LogError("MannerEstimate Controller : Session not found in FarmingRules() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
        public IActionResult FarmingRules(MannerEstimateViewModel model)
        {
            SetMannerEstimateToSession(model);

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerActionName);
            }

            return RedirectToAction("PostCode");
        }
        [HttpGet]
        public IActionResult PostCode()
        {
            _logger.LogTrace("MannerEstimate Controller : PostCode() action called");
            MannerEstimateViewModel? model = GetMannerEstimateFromSession();

            if (model == null)
            {
                _logger.LogError("MannerEstimate Controller : Session not found in PostCode() action");
                return Functions.RedirectToErrorHandler((int)HttpStatusCode.Conflict);
            }

            return View(model);
        }
    }
}
