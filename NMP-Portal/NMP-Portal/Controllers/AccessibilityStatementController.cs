using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace NMP.Portal.Controllers;
public class AccessibilityStatementController(ILogger<AccessibilityStatementController> logger) : Controller
{
    private readonly ILogger<AccessibilityStatementController> _logger = logger;

    [AllowAnonymous]
    public IActionResult Index()
    {
        _logger.LogTrace("Accessed Accessibility Statement page.");
        return View();
    }
}
