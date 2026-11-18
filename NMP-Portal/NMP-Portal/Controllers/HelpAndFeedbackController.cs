using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers;

[AllowAnonymous]
public class HelpAndFeedbackController : BaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
