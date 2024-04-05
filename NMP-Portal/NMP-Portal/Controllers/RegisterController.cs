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
        private readonly IDataProtector _dataProtector;
        
        private IHttpContextAccessor _httpContextAccessor;
        public RegisterController(ILogger<RegisterController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;            
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.RegisterController");
            _httpContextAccessor = contextAccessor;
        }
        public IActionResult Index()
        {            
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms
            if (model.Farms.Count > 0)
            {
                ViewBag.IsUserHaveAnyFarms = true;
                return View("~/Views/Farm/FarmList.cshtml", model);
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = false;
                return RedirectToAction("Name", "Farm");
            }

        }        
    }
}
