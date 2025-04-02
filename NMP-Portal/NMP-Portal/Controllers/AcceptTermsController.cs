using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers;
public class AcceptTermsController : Controller
{
    private readonly ILogger<AcceptTermsController> _logger;
    private readonly IUserExtensionService _userExtensionService;
    public AcceptTermsController(ILogger<AcceptTermsController> logger, IUserExtensionService userExtensionService)
    {
        _logger = logger;
        _userExtensionService = userExtensionService;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {   
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Accept()
    {
        TermsOfUseViewModel model = new TermsOfUseViewModel();
        int userId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value);
        (UserExtension userExtension, Error error) = await _userExtensionService.FetchUserExtensionAsync();
        if (userExtension != null && userExtension.IsTermsOfUseAccepted)
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
            TermsOfUse termsOfUse = model;
            (UserExtension userExtension, Error error) = await _userExtensionService.UpdateTermsOfUseAsync(termsOfUse);
            if (userExtension != null && userExtension.IsTermsOfUseAccepted)
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.Error = error.Message;
            }

            return View("Accept", model);
        }
        else
        {
            return View("Accept", model);
        }
    }
}
