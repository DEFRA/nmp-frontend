using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Resources;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers;
public class AcceptTermsController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Accept()
    {
        TermsOfUseViewModel model = new TermsOfUseViewModel();        
        return View("Accept", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Accept(TermsOfUseViewModel model)
    {
        if(!model.IsAccepted)
        {
            ModelState.AddModelError("IsAccepted", Resource.msgAcceptTermsOfUse);
        }

        if (ModelState.IsValid)
        {
            return RedirectToAction("Index", "AboutService");
        }
        else
        {
            return View("Accept", model);
        }
    }
}
