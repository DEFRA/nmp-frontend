using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Diagnostics.Eventing.Reader;

namespace NMP.Portal.Controllers
{
    public class AboutServiceController : Controller
    {
        private readonly ILogger<AboutServiceController> _logger;
        private readonly IUserExtensionService _userExtensionService;
        public AboutServiceController(ILogger<AboutServiceController> logger, IUserExtensionService userExtensionService)
        {
            _logger = logger;
            _userExtensionService = userExtensionService;
        }
        public async Task<IActionResult> Index()
        {
            AboutServiceViewModel model = new AboutServiceViewModel();
            (UserExtension userExtension, Error error) = await _userExtensionService.FetchUserExtensionAsync();
            if (userExtension != null && userExtension.DoNotShowAboutThisService)
            {
                return RedirectToAction("Accept", "AcceptTerms");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AboutServiceViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.DoNotShowAboutThisService)
                {// Save to Database
                    AboutService aboutService = model;
                    (UserExtension userExtension, Error error) = await _userExtensionService.UpdateShowAboutServiceAsync(aboutService);
                    if (userExtension != null)
                    {
                        //saved in DB
                    }                    
                }
            }

            return RedirectToAction("Accept", "AcceptTerms");
        }
    }
}
