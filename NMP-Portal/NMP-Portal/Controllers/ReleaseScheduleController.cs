using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers;

[AllowAnonymous]
public class ReleaseScheduleController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
