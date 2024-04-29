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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
            _httpContextAccessor.HttpContext?.Session.Remove("CropGroupList");
            _httpContextAccessor.HttpContext?.Session.Remove("SoilTypes");
            _httpContextAccessor.HttpContext?.Session.Remove("CropTypeList");
            return RedirectToAction("FarmSummary", "Farm", new { Id = id });
        }

        [HttpGet]
        public async Task<IActionResult> AddField(string q)//EncryptedfarmId
        {
            FieldViewModel model = new FieldViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                if (!string.IsNullOrEmpty(q))
                {
                    model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmID);
                    model.isEnglishRules = farm.EnglishRules;
                    model.EncryptedFarmId = q;
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
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

            if (field.CroppedArea > field.TotalArea)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgCroppedAreaIsGreaterThanTotalArea);
            }
            if (field.ManureNonSpreadingArea > field.TotalArea)
            {
                ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgManureNonSpreadingAreaIsGreaterThanTotalArea);
            }

            if (field.CroppedArea.HasValue && field.ManureNonSpreadingArea.HasValue)
            {
                decimal totalArea = field.CroppedArea.Value + field.ManureNonSpreadingArea.Value;
                if (totalArea > field.TotalArea)
                {
                    ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgIfCroppedAreaAndNonSpreadingArea);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(field);
            }

            if (!(field.CroppedArea.HasValue) && !(field.ManureNonSpreadingArea.HasValue))
            {
                field.CroppedArea = field.TotalArea;
            }

            if ((!field.CroppedArea.HasValue) && (field.ManureNonSpreadingArea.HasValue) && field.ManureNonSpreadingArea > 0)
            {
                field.CroppedArea = field.TotalArea - field.ManureNonSpreadingArea;
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
            try
            {
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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ElevationField");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult SoilType(FieldViewModel field)
        {
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            if (field.SoilTypeID == null)
            {
                ModelState.AddModelError("SoilTypeID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilTypes"))
            {
                soilTypes = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<SoilTypesResponse>>("SoilTypes");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SoilTypesList = soilTypes;
                return View(field);
            }

            SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (soilType != null && soilType.KReleasingClay)
            {
                return RedirectToAction("SoilReleasingClay");
            }
            else if (field.IsCheckAnswer)
            {
                field.IsSoilReleasingClay = false;
                field.SoilReleasingClay = null;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                return RedirectToAction("CheckAnswer");
            }


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
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilTypes"))
            {
                soilTypes = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<SoilTypesResponse>>("SoilTypes");
            }

            var soilType = soilTypes?.Where(x => x.SoilTypeId == field.SoilTypeID).FirstOrDefault();
            if (!soilType.KReleasingClay)
            {
                field.SoilReleasingClay = false;
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
                field.SoilReleasingClay = null;
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
            if (DateTime.TryParseExact(model.SoilAnalysis.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("SoilAnalysis.Date", Resource.MsgEnterTheDateInNumber);
            }

            if (model.SoilAnalysis.Date != null)
            {
                if (model.SoilAnalysis.Date.Value.Date.Year < 1960 || model.SoilAnalysis.Date.Value.Date > DateTime.Now.Date)
                {
                    ModelState.AddModelError("SoilAnalysis.Date", Resource.MsgEnterADateAfter);
                }
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
                if (model.IsSoilNutrientValueTypeIndex != null && model.IsSoilNutrientValueTypeIndex.Value)
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

                model.SoilAnalysis.PhosphorusMethodologyId = (int)PhosphorusMethodology.Olsens;

                if (model.IsSoilNutrientValueTypeIndex != null && (!model.IsSoilNutrientValueTypeIndex.Value))
                {
                    List<NutrientResponseWrapper> nutrients = await _fieldService.FetchNutrientsAsync();
                    if (nutrients.Count > 0)
                    {
                        int phosphorusId = 1;
                        int potassiumId = 2;
                        int magnesiumId = 3;

                        var phosphorusNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                        if (phosphorusNuetrient != null)
                        {
                            phosphorusId = phosphorusNuetrient.nutrientId;
                        }

                        var magnesiumNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                        if (magnesiumNuetrient != null)
                        {
                            magnesiumId = magnesiumNuetrient.nutrientId;
                        }

                        var potassiumNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                        if (potassiumNuetrient != null)
                        {
                            potassiumId = potassiumNuetrient.nutrientId;
                        }

                        (model.SoilAnalysis.PhosphorusIndex, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.SoilAnalysis.Phosphorus, (int)PhosphorusMethodology.Olsens);
                        (model.SoilAnalysis.MagnesiumIndex, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.SoilAnalysis.Magnesium, (int)MagnesiumMethodology.None);
                        (model.SoilAnalysis.PotassiumIndex, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.SoilAnalysis.Potassium, (int)PotassiumMethodology.None);

                    }
                }
                else
                {
                    model.SoilAnalysis.Phosphorus = null;
                    model.SoilAnalysis.Magnesium = null;
                    model.SoilAnalysis.Potassium = null;
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
            return RedirectToAction("SNSCalculationMethod");
        }

        [HttpGet]
        public IActionResult SNSCalculationMethod()
        {
            FieldViewModel? model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }
        [HttpPost]
        public IActionResult SNSCalculationMethod(FieldViewModel field)
        {
            if (field.IsSnsBasedOnPreviousCrop == null)
            {
                ModelState.AddModelError("IsSnsBasedOnPreviousCrop", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);

            return RedirectToAction("CropGroups");
        }

        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            FieldViewModel model = new FieldViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                cropGroups = await _fieldService.FetchCropGroups();
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropGroupList", cropGroups);
                ViewBag.CropGroupList = cropGroups;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("SNSCalculationMethod");
            }
            return View(model);
        }
        [HttpPost]
        public IActionResult CropGroups(FieldViewModel field)
        {
            if (field.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CropGroupResponse> cropTypes = new List<CropGroupResponse>();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
                {
                    ViewBag.CropGroupList = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<CropGroupResponse>>("CropGroupList");
                }
                return View(field);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);

            return RedirectToAction("CropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CropTypes()
        {
            FieldViewModel model = new FieldViewModel();
            List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                FarmViewModel farm = new FarmViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
                {
                    farm = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FarmViewModel>("FarmData");
                }

                cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                var isEnglishRule = farm.EnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == isEnglishRule || x.CountryId == (int)NMP.Portal.Enums.Country.Both).ToList();

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropTypeList", cropTypeList);
                ViewBag.CropTypeList = cropTypeList;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult CropTypes(FieldViewModel field)
        {
            if (field.Crop.CropTypeId == null)
            {
                ModelState.AddModelError("Crop.CropTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FarmData"))
                {
                    ViewBag.CropTypeList = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<CropTypeResponse>>("CropTypeList");
                }
                return View(field);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);

            return RedirectToAction("CheckAnswer");
        }
        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            FieldViewModel? model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                if (model == null)
                {
                    model = new FieldViewModel();
                }

                model.SoilType = await _soilService.FetchSoilTypeById(model.SoilTypeID ?? 0);
                model.CropGroup = await _fieldService.FetchCropGroupById(model.CropGroupId ?? 0);
                model.CropType = await _fieldService.FetchCropTypeById(model.Crop.CropTypeId ?? 0);
                model.IsCheckAnswer = true;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

        }

        public IActionResult BackCheckAnswer()
        {
            FieldViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("CropTypes");
        }
    }
}
