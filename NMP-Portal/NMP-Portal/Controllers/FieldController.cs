using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Reflection;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    public class FieldController : Controller
    {
        private readonly ILogger<FieldController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ISoilService _soilService;

        public FieldController(ILogger<FieldController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, ISoilService soilService,
             IFieldService fieldService)
        {
            _logger = logger;
            _farmService = farmService;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldService = fieldService;
            _soilService = soilService;
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
            model.EncryptedFarmId = encryptedFarmId;
            if (!string.IsNullOrEmpty(encryptedFarmId))
            {
                string farmId = _farmDataProtector.Unprotect(encryptedFarmId);
                (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
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
                ModelState.AddModelError("TotalArea", Resource.MsgEnterTotalFieldArea);
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
            return RedirectToAction("ElevationField");
        }

        [HttpGet]
        public async Task<IActionResult> ElevationField()
        {
            Error error = new Error();

            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (farm.FieldsAbove300SeaLevel == 1)
            {
                return View(model);
            }
            model.IsAbove300SeaLevel = Convert.ToBoolean(farm.FieldsAbove300SeaLevel);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("Soil");
        }

        [HttpPost]
        public IActionResult ElevationField(FieldViewModel field)
        {
            if (field.IsAbove300SeaLevel == null)
            {
                ModelState.AddModelError("IsAbove300SeaLevel", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            return RedirectToAction("SoilNutrientValueType");
        }
        [HttpGet]
        public async Task<IActionResult> SoilNutrientValueType()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }
        [HttpPost]
        public IActionResult SoilNutrientValueType(FieldViewModel model)
        {
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            return RedirectToAction("AddSoilAnalysis");
        }
        [HttpGet]
        public async Task<IActionResult> AddSoilAnalysis()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddSoilAnalysis(FieldViewModel model)
        {
            Error error = null;
            try
            {
                if (model.IsSoilNutrientValueTypeIndex!=null&&model.IsSoilNutrientValueTypeIndex==true)
                {
                    if (model.SoilAnalysis.PotassiumIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.PotassiumIndex", Resource.MsgPotassiumIndex);
                    }
                    if (model.SoilAnalysis.PhosphorusIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.PhosphorusIndex", Resource.MsgPhosphorusIndex);
                    }
                    if (model.SoilAnalysis.MagnesiumIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.MagnesiumIndex", Resource.MsgMagnesiumIndex);
                    }
                }
                else
                {
                    if (model.SoilAnalysis.Potassium == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.Potassium", Resource.MsgPotassiumPerLitreOfSoil);
                    }
                    if (model.SoilAnalysis.Phosphorus == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.Phosphorus", Resource.MsgPhosphorusPerLitreOfSoil);
                    }
                    if (model.SoilAnalysis.Magnesium == null)
                    {
                        ModelState.AddModelError("SoilAnalysis.Magnesium", Resource.MsgMagnesiumPerLitreOfSoil);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.SoilAnalysis.PhosphorusMethodologyId = (int?)PhosphorusMethodology.Olsens;

                if (model.SoilAnalysis.Magnesium != null && model.SoilAnalysis.Potassium != null &&
                    model.SoilAnalysis.Phosphorus != null)
                {
                    List<FieldResponseWapper> nutrients = await _fieldService.FetchNutrientsAsync();
                    if (nutrients.Count > 0)
                    {
                        int phosphorusId = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate)).nutrientId;
                        int magnesiumId = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium)).nutrientId;
                        int potassiumId = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash)).nutrientId;

                        (model.SoilAnalysis.PhosphorusIndex, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.SoilAnalysis.Phosphorus, (int)PhosphorusMethodology.Olsens);
                        (model.SoilAnalysis.MagnesiumIndex, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.SoilAnalysis.Magnesium, (int)MagnesiumMethodology.None);
                        (model.SoilAnalysis.PotassiumIndex, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.SoilAnalysis.Potassium, (int)PotassiumMethodology.None);
                        
                    }
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            }
            catch (Exception ex)
            {
                ViewBag.Error =string.Concat(error,ex.Message);
            }
            return View(model);
        }
    }
}
