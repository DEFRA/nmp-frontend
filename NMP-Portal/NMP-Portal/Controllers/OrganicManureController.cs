using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using System.Reflection;
using NMP.Portal.Resources;
using NMP.Portal.Helpers;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class OrganicManureController : Controller
    {
        private readonly ILogger<OrganicManureController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrganicManureService _organicManureService;

        public OrganicManureController(ILogger<OrganicManureController> logger, IDataProtectionProvider dataProtectionProvider,
              IHttpContextAccessor httpContextAccessor, IOrganicManureService organicManureService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _organicManureService = organicManureService;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult CreateManureCancel(string q, string r)
        {
            //_httpContextAccessor.HttpContext?.Session.Remove("CropData");
            return RedirectToAction("HarvestYearOverview", "Crop", new { Id = q, year = r });
        }
        
        [HttpGet]
        public IActionResult ManureApplyingDate()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManureApplyingDate(OrganicManureViewModel model)
        {

            if (model.ApplicationDate != null)
            {
                if (model.ApplicationDate.Date.Year < 1601 || model.ApplicationDate.Date.Year > DateTime.Now.AddYears(1).Year)
                {
                    ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgEnterTheDateInNumber);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);
           

            return RedirectToAction("DefaultNutrientValues");
        }

        [HttpGet]
        public IActionResult DefaultNutrientValues()
        {
            OrganicManureViewModel model = new OrganicManureViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("OrganicManure"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<OrganicManureViewModel>("OrganicManure");
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DefaultNutrientValues(OrganicManureViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("OrganicManure", model);


            return RedirectToAction("DefaultNutrientValues");
        }
    }
}