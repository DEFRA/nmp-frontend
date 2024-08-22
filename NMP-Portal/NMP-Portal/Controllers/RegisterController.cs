using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class RegisterController : Controller
    {
        private readonly ILogger<RegisterController> _logger;       
        public RegisterController(ILogger<RegisterController> logger)
        {
            _logger = logger;            
            
        }
        public IActionResult Index()
        {
            //TODO: Need to comment below line of code in production.
            ViewBag.Token = HttpContext?.User.FindFirst("access_token").Value;
            return View();
        }        
    }
}
