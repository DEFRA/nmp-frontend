using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class CookieController(ILogger<CookieController> logger) : Controller
    {
        private readonly ILogger<CookieController> _logger = logger;
        public IActionResult Index()
        {
            _logger.LogTrace("Accessed Cookie Information page.");
            return View();
        }
    }
}
