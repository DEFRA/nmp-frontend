using Microsoft.AspNetCore.Mvc;
using NMP.Commons.Models;
using NMP.Portal.Services;
using NMP.Commons.ViewModels;

namespace NMP.Portal.Controllers
{
    public class AboutServiceController : Controller
    {
        
        private readonly IUserExtensionService _userExtensionService;
        public AboutServiceController(IUserExtensionService userExtensionService)
        {            
            _userExtensionService = userExtensionService;
        }
        public async Task<IActionResult> Index()
        {
            AboutServiceViewModel model = new AboutServiceViewModel();
            (UserExtension userExtension, _) = await _userExtensionService.FetchUserExtensionAsync();
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
                {
                    // Save to Database
                    AboutService aboutService = model;
                    (UserExtension userExtension, _) = await _userExtensionService.UpdateShowAboutServiceAsync(aboutService);
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
