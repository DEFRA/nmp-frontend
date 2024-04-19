using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    public class FieldController : Controller
    {
        private readonly ILogger<FieldController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FieldController(ILogger<FieldController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _farmService = farmService;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> AddField(string encryptedFarmId)
        {
            FieldViewModel model = new FieldViewModel();
            Error error = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            model.EncryptedFarmId= encryptedFarmId;
            if (!string.IsNullOrEmpty(encryptedFarmId))
            {
                string farmId = _farmDataProtector.Unprotect(encryptedFarmId);
                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                if (farm != null)
                {
                    //model = new FieldViewModel();
                    //model.FarmName = farm.Name;
                    //model.FarmID = farm.ID;
                    //model.EncryptedFarmId = encryptedFarmId;
                    //var fieldModel = JsonConvert.SerializeObject(model);
                    //_httpContextAccessor.HttpContext?.Session.SetString("FieldData", fieldModel);
                }
            }
            
            return View(model);
        }

        [HttpPost]
        public IActionResult AddField(FieldViewModel field)
        {

            if (string.IsNullOrWhiteSpace(field.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFieldName);
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            return RedirectToAction("FieldMeasurements");
        }
        [HttpGet]
        public IActionResult FieldMeasurements()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult FieldMeasurements(FieldViewModel field)
        {
            if (field.TotalArea == null || field.TotalArea == 0)
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTotalFieldArea);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            return RedirectToAction("NVZField");
        }
        [HttpGet]
        public async Task<IActionResult> NVZField()
        {
            Error error = new Error();

            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (farm.NVZFields == 1)
            {
                return View(model);
            }
            model.IsWithinNVZ = Convert.ToBoolean(farm.NVZFields);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("ElevationField");
        }
        [HttpPost]
        public IActionResult NVZField(FieldViewModel field)
        {
            if (field.IsWithinNVZ == null)
            {
                ModelState.AddModelError("IsWithinNVZ", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            return RedirectToAction("NVZField");
        }
        
    }
}
