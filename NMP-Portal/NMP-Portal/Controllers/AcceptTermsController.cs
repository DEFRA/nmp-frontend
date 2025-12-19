using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Commons.Resources;
using NMP.Application;


namespace NMP.Portal.Controllers;
public class AcceptTermsController(ILogger<AcceptTermsController> logger, IAcceptTermsLogic acceptTermsLogic) : Controller
{
    private readonly ILogger<AcceptTermsController> _logger = logger;
    private readonly IAcceptTermsLogic _acceptTermsLogic = acceptTermsLogic;

    [AllowAnonymous]
    public IActionResult Index()
    {
        _logger.LogTrace("Index action called in AcceptTermsController.");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Accept()
    {
        _logger.LogTrace("AcceptTermsController : Accept() get action called");
        TermsOfUseViewModel model = new TermsOfUseViewModel();
        model.IsTermsOfUseAccepted = await _acceptTermsLogic.IsUserTermsOfUseAccepted();         
        if (model.IsTermsOfUseAccepted)
        {            
            return RedirectToAction("FarmList", "Farm");                   
        }

        return View("Accept", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(TermsOfUseViewModel model)
    {
        if (!model.IsTermsOfUseAccepted)
        {
            ModelState.AddModelError("IsTermsOfUseAccepted", Resource.msgAcceptTermsOfUse);
        }

        if (ModelState.IsValid)
        {            
            await _acceptTermsLogic.UpdateTermsOfUseAsync(model);
            if (model.IsTermsOfUseAccepted)
            {
                return RedirectToAction("FarmList", "Farm");
            }
            
            return View("Accept", model);
        }
        else
        {            
            return View("Accept", model);
        }
    }
}
