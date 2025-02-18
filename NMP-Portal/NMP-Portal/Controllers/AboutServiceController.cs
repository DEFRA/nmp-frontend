using Microsoft.AspNetCore.Mvc;
using NMP.Portal.ViewModels;
using System.Diagnostics.Eventing.Reader;

namespace NMP.Portal.Controllers
{
    public class AboutServiceController : Controller
    {
        public IActionResult Index()
        {
            AboutServiceViewModel model = new AboutServiceViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(AboutServiceViewModel model)
        {
            if (ModelState.IsValid)
            {
                if(model.DoNotShowThisInformationAgain)
                {
                    // Save to Database
                }
            }

            return RedirectToAction("FarmList", "Farm");
        }
    }
}
