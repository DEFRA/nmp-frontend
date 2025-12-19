using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    
    public class AccessibilityStatementController : Controller
    {
        private readonly ILogger<AccessibilityStatementController> _logger;

        public AccessibilityStatementController(ILogger<AccessibilityStatementController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            _logger.LogTrace("Accessed Accessibility Statement page.");
            return View();
        }
    }
}
