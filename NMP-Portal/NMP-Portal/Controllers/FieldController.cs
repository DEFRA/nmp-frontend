using Microsoft.AspNetCore.Components.Forms;
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
using System.Collections.Generic;
using System.Globalization;
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

        public IActionResult CreateFieldCancel(string id)
        {
            _httpContextAccessor.HttpContext?.Session.Remove("FieldData");
            return RedirectToAction("FarmSummary", "Farm", new { Id = id });
        }

        [HttpGet]
        public async Task<IActionResult> AddField(string q)//EncryptedfarmId
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            if (!string.IsNullOrEmpty(q))
            {
                model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                model.EncryptedFarmId = q;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

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

            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

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
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
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
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
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
            return RedirectToAction("SoilType");
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
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("SoilType");
        }
        [HttpGet]
        public async Task<IActionResult> SoilType()
        {
            Error error = new Error();
            FieldViewModel model = new FieldViewModel();
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            FarmViewModel farm = new FarmViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
            {
                farm = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
            }
            if (_httpContextAccessor.HttpContext != null && !_httpContextAccessor.HttpContext.Session.Keys.Contains("SoilTypes"))
            {
                soilTypes = await _fieldService.FetchSoilTypes();
                if (soilTypes.Count > 0 && soilTypes.Any())
                {
                    var isEnglishRule = farm.EnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var soilTypesList = soilTypes.Where(x => x.CountryId == isEnglishRule).ToList();
                    ViewBag.SoilTypesList = soilTypesList;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilTypes", soilTypesList);
                }
                else
                {
                    ViewBag.SoilTypeError = Resource.MsgServiceNotAvailable;
                    RedirectToAction("ElevationField");
                }
            }
            else
            {
                soilTypes = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<SoilTypesResponse>>("SoilTypes");
                ViewBag.SoilTypesList = soilTypes;
            }


            return View(model);
        }

        [HttpPost]
        public IActionResult SoilType(FieldViewModel field)
        {
            if (field.SoilTypeID == null)
            {
                ModelState.AddModelError("SoilTypeID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilTypes"))
            {
                soilTypes = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<SoilTypesResponse>>("SoilTypes");
            }

            var soilType = soilTypes?.Where(x => x.SoilTypeId == field.SoilTypeID).FirstOrDefault();

            if (soilType.KReleasingClay)
            {
                field.SoilReleasingClay = true;
            }
            else
            {
                field.SoilReleasingClay = false;
            }
            
            if (field.IsCheckAnswer)
            {
                FieldViewModel fieldView = JsonConvert.DeserializeObject<FieldViewModel>(_httpContextAccessor.HttpContext.Session.GetString("FieldData"));

                var updatedField = JsonConvert.SerializeObject(field);
                _httpContextAccessor.HttpContext?.Session.SetString("FieldData", updatedField);

                if (fieldView.SoilTypeID == field.SoilTypeID)
                {
                    return RedirectToAction("CheckAnswer");
                }
            }

            if (soilType.KReleasingClay)
            {
                return RedirectToAction("SoilReleasingClay");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);

            return RedirectToAction("SulphurDeficient");
        }

        [HttpGet]
        public IActionResult SoilReleasingClay()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SoilReleasingClay(FieldViewModel field)
        {
            if (field.SoilReleasingClay == null)
            {
                ModelState.AddModelError("SoilReleasingClay", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (field.SoilAnalysis.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalysis.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            field.IsSoilReleasingClay = true;
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("SoilDateAndPHLevel");
        }

        [HttpGet]
        public IActionResult SulphurDeficient()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SulphurDeficient(FieldViewModel field)
        {

            if (field.SoilAnalysis.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalysis.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (field.IsSoilReleasingClay)
            {
                field.IsSoilReleasingClay = false;
            }
            field.SoilReleasingClay = null;
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("SoilDateAndPHLevel");
        }
        [HttpGet]
        public async Task<IActionResult> SoilDateAndPHLevel()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult SoilDateAndPHLevel(FieldViewModel model)
        {
            if (model.SoilAnalysis.Date == null)
            {
                ModelState.AddModelError("SoilAnalysis.Date", Resource.MsgEnterADateBeforeContinuing);
            }
            if (model.SoilAnalysis.PH == null)
            {
                ModelState.AddModelError("SoilAnalysis.PH", Resource.MsgEnterAPHBeforeContinuing);
            }
            //if (model.SoilAnalysis.Date > DateTime.Now.Date)
            //{
            //    ModelState.AddModelError("SoilAnalysis.Date", Resource.MsgEnterTheDateInNumber);
            //}
            if (DateTime.TryParseExact(model.SoilAnalysis.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("SoilAnalysis.Date", Resource.MsgEnterTheDateInNumber);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

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
            return View(model);
        }
        [HttpPost]
        public IActionResult SoilNutrientValueType(FieldViewModel model)
        {
            if (model.IsSoilNutrientValueTypeIndex == null)
            {
                ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
           
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            return RedirectToAction("SoilNutrientValue");
        }
        [HttpGet]
        public async Task<IActionResult> SoilNutrientValue()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SoilNutrientValue(FieldViewModel model)
        {
            Error error = null;
            try
            {
                if (model.IsSoilNutrientValueTypeIndex != null && model.IsSoilNutrientValueTypeIndex == true)
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
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = string.Concat(error, ex.Message);
            }
            return RedirectToAction("CheckAnswer");

        }
        [HttpGet]
        public IActionResult CheckAnswer()
        {
            FieldViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            if (model == null)
            {
                model = new FieldViewModel();
            }

            model.IsCheckAnswer = true;
            //model.OldPostcode = model.Postcode;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);

        }
    }
}
