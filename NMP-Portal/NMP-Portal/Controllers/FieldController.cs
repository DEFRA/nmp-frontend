using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;

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
            FieldViewModel? model = null;
            if (!string.IsNullOrEmpty(encryptedFarmId))
            {
                string farmId = _farmDataProtector.Unprotect(encryptedFarmId);
                Farm farm = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                if (farm != null)
                {
                    model = new FieldViewModel();
                    model.FarmName = farm.Name;
                    model.FarmID = farm.ID;
                    model.EncryptedFarmId = encryptedFarmId;
                    var fieldModel = JsonConvert.SerializeObject(model);
                    _httpContextAccessor.HttpContext?.Session.SetString("FieldData", fieldModel);
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddField(FieldViewModel fieldViewModel)
        {
            return RedirectToAction("FieldMeasurements");
        }

        public IActionResult FieldMeasurements()
        {
            return View();
        }
    }
}
