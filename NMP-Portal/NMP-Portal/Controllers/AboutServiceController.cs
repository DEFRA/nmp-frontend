using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Commons.ViewModels;

namespace NMP.Portal.Controllers
{
    public class AboutServiceController(ILogger<AboutServiceController> logger ,IAboutServiceLogic aboutServiceLogic) : Controller
    {
        private readonly ILogger<AboutServiceController> _logger = logger;
        private readonly IAboutServiceLogic _aboutServiceLogic = aboutServiceLogic;
        public async Task<IActionResult> Index()
        {
            _logger.LogTrace("Index action called in AboutServiceController.");
            AboutServiceViewModel model = new();
            model.DoNotShowAboutThisService = await _aboutServiceLogic.CheckDoNotShowAboutThisService();
            if (model.DoNotShowAboutThisService)
            {
                return RedirectToAction("Accept", "AcceptTerms");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AboutServiceViewModel model)
        {
            _logger.LogTrace("AboutServiceController : Index() post action called");
            if (ModelState.IsValid && model.DoNotShowAboutThisService)
            {
                // Save to Database
                await _aboutServiceLogic.UpdateShowAboutServiceAsync(model.DoNotShowAboutThisService);                                                  
            }

            return RedirectToAction("Accept", "AcceptTerms");
        }
    }
}
