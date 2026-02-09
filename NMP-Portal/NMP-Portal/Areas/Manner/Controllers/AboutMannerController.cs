using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Commons.ViewModels;
using NMP.Core.Interfaces;

namespace NMP.Portal.Areas.Manner.Controllers
{
    [Area("Manner")]
    [Authorize]
    public class AboutMannerController(ILogger<AboutMannerController> logger, IAboutMannerLogic aboutMannerLogic) : Controller
    {
        private readonly ILogger<AboutMannerController> _logger = logger;
        private readonly IAboutMannerLogic _aboutMannerLogic = aboutMannerLogic;
        public async Task<IActionResult> Index()
        {
            AboutMannerViewModel model = new();
            model.DoNotShowAboutManner = await _aboutMannerLogic.CheckDoNotShowAboutManner();
            if (model.DoNotShowAboutManner)
            {
                return RedirectToAction("MannerHubPage", "MannerEstimate");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AboutMannerViewModel model)
        {
            _logger.LogTrace("AboutMannerController : Index() post action called");
            if (ModelState.IsValid && model.DoNotShowAboutManner)
            {
                // Save to Database
                await _aboutMannerLogic.UpdateShowAboutMannerAsync(model.DoNotShowAboutManner);
            }

            return RedirectToAction("MannerHubPage", "MannerEstimate");
        }
    }
}
