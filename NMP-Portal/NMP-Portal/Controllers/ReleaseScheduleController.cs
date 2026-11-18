using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers;

[AllowAnonymous]
public class ReleaseScheduleController : BaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
