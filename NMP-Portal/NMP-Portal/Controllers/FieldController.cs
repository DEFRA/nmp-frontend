using GovUk.Frontend.AspNetCore.TagHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
using System.Security.Claims;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
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

        public async Task<IActionResult> BackActionForAddField(string id)
        {
            FieldViewModel model = new FieldViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                int fieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmID));
                _httpContextAccessor.HttpContext?.Session.Remove("FieldData");
                if (fieldCount > 0)
                {
                    return RedirectToAction("ManageFarmFields", "Field", new { id = id });
                }
                else
                {
                    return RedirectToAction("FarmSummary", "Farm", new { id = id });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorOnBackButton"] = ex.Message;
                return View("AddField", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddField(string q)//EncryptedfarmId
        {
            FieldViewModel model = new FieldViewModel();
            Error error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                else if (string.IsNullOrWhiteSpace(q))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrEmpty(q))
                {
                    model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    model.EncryptedFarmId = q;

                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmID);
                    model.isEnglishRules = farm.EnglishRules;
                    model.FarmName = farm.Name;
                    model.LastHarvestYear = farm.LastHarvestYear;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = string.Concat(error.Message == null ? "" : error.Message, ex.Message);
                return RedirectToAction("FarmSummary", "Farm", new { id = q });
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(FieldViewModel field)
        {

            if (string.IsNullOrWhiteSpace(field.Name))
            {
                ModelState.AddModelError("Name", Resource.MsgEnterTheFieldName);
            }

            bool isFieldAlreadyexist = await _fieldService.IsFieldExistAsync(field.FarmID, field.Name);
            if (isFieldAlreadyexist)
            {
                ModelState.AddModelError("Name", Resource.MsgFieldAlreadyExist);
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
        public async Task<IActionResult> FieldMeasurements()
        {
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldMeasurements(FieldViewModel field)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("TotalArea"))
            {
                var InvalidFormatError = ModelState["TotalArea"].Errors.Count > 0 ?
                                ModelState["TotalArea"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["TotalArea"].AttemptedValue, Resource.lblTotalFieldArea)))
                {
                    ModelState["TotalArea"].Errors.Clear();
                    ModelState["TotalArea"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("CroppedArea"))
            {
                var InvalidFormatError = ModelState["CroppedArea"].Errors.Count > 0 ?
                                ModelState["CroppedArea"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["CroppedArea"].AttemptedValue, Resource.lblCroppedArea)))
                {
                    ModelState["CroppedArea"].Errors.Clear();
                    ModelState["CroppedArea"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("ManureNonSpreadingArea"))
            {
                var InvalidFormatError = ModelState["ManureNonSpreadingArea"].Errors.Count > 0 ?
                                ModelState["ManureNonSpreadingArea"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["ManureNonSpreadingArea"].AttemptedValue, Resource.lblManureNonSpreadingArea)))
                {
                    ModelState["ManureNonSpreadingArea"].Errors.Clear();
                    ModelState["ManureNonSpreadingArea"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblArea));
                }
            }
            if (field.TotalArea == null || field.TotalArea == 0)
            {
                ModelState.AddModelError("TotalArea", Resource.MsgEnterTotalFieldArea);
            }
            if (field.TotalArea != null)
            {
                if (field.TotalArea < 0)
                {
                    ModelState.AddModelError("TotalArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
            }

            if (field.CroppedArea > field.TotalArea)
            {
                ModelState.AddModelError("CroppedArea", Resource.MsgCroppedAreaIsGreaterThanTotalArea);
            }
            if (field.CroppedArea != null)
            {
                if (field.CroppedArea < 0)
                {
                    ModelState.AddModelError("CroppedArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
                }
            }

            if (field.ManureNonSpreadingArea > field.TotalArea)
            {
                ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgManureNonSpreadingAreaIsGreaterThanTotalArea);
            }
            if (field.ManureNonSpreadingArea != null)
            {
                if (field.ManureNonSpreadingArea < 0)
                {
                    ModelState.AddModelError("ManureNonSpreadingArea", Resource.MsgEnterANumberWhichIsGreaterThanZero);
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

            string farmId = _farmDataProtector.Unprotect(field.EncryptedFarmId);
            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

            field.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Portal.Enums.NVZFields.SomeFieldsInNVZ ? true : false;
            field.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.NVZFields.SomeFieldsInNVZ ? true : false;

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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (model.IsWithinNVZForFarm.HasValue && model.IsWithinNVZForFarm.Value)
            {
                return View(model);
            }
            model.IsWithinNVZ = Convert.ToBoolean(farm.NVZFields);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("ElevationField");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            string farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
            if (model.IsAbove300SeaLevelForFarm.HasValue && model.IsAbove300SeaLevelForFarm.Value)
            {
                return View(model);
            }
            model.IsAbove300SeaLevel = Convert.ToBoolean(farm.FieldsAbove300SeaLevel);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("SoilType");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }


                soilTypes = await _fieldService.FetchSoilTypes();
                if (soilTypes.Count > 0 && soilTypes.Any())
                {
                    var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                    ViewBag.SoilTypesList = soilTypesList;
                }
                else
                {
                    ViewBag.SoilTypeError = Resource.MsgServiceNotAvailable;
                    RedirectToAction("ElevationField");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilType(FieldViewModel field)
        {
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            try
            {
                if (field.SoilTypeID == null)
                {
                    ModelState.AddModelError("SoilTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblSoilType.ToLower()));
                }

                soilTypes = await _fieldService.FetchSoilTypes();
                if (soilTypes.Count > 0 && soilTypes.Any())
                {
                    var country = field.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                    var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                    soilTypes = soilTypesList;
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.SoilTypesList = soilTypes;
                    return View(field);
                }
                field.SoilType = await _soilService.FetchSoilTypeById(field.SoilTypeID.Value);
                SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                if (soilType != null && soilType.KReleasingClay)
                {
                    field.IsSoilReleasingClay = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                    return RedirectToAction("SoilReleasingClay");
                }
                else if (field.IsCheckAnswer)
                {
                    field.IsSoilReleasingClay = false;
                    field.SoilReleasingClay = null;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                    return RedirectToAction("CheckAnswer");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ElevationField");
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilReleasingClay(FieldViewModel field)
        {
            if (field.SoilReleasingClay == null)
            {
                ModelState.AddModelError("SoilReleasingClay", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (field.SoilAnalyses.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            field.IsSoilReleasingClay = true;
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
            //if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilTypes"))
            //{
            //    soilTypes = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<List<SoilTypesResponse>>("SoilTypes");
            //}
            soilTypes = await _fieldService.FetchSoilTypes();
            if (soilTypes.Count > 0 && soilTypes.Any())
            {
                var country = field.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                soilTypes = soilTypesList;
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsSoilReleasingClay = false;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SulphurDeficient(FieldViewModel field)
        {

            if (field.SoilAnalyses.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilDateAndPHLevel(FieldViewModel model)
        {

            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Date"))
            {
                var dateError = ModelState["SoilAnalyses.Date"].Errors.Count > 0 ?
                                ModelState["SoilAnalyses.Date"].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDateSampleTaken)))
                {
                    ModelState["SoilAnalyses.Date"].Errors.Clear();
                    ModelState["SoilAnalyses.Date"].Errors.Add(Resource.MsgEnterTheDateInNumber);
                }
            }

            if (model.SoilAnalyses.Date == null)
            {
                ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgEnterADateBeforeContinuing);
            }
            if (model.SoilAnalyses.PH == null)
            {
                ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgEnterAPHBeforeContinuing);
            }
            if (DateTime.TryParseExact(model.SoilAnalyses.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgEnterTheDateInNumber);
            }

            if (model.SoilAnalyses.Date != null)
            {
                if (model.SoilAnalyses.Date.Value.Date.Year < 1601 || model.SoilAnalyses.Date.Value.Date.Year > DateTime.Now.AddYears(1).Year)
                {
                    ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgEnterTheDateInNumber);
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNutrientValue(FieldViewModel model)
        {
            Error error = null;
            try
            {
                if (model.IsSoilNutrientValueTypeIndex != null && model.IsSoilNutrientValueTypeIndex.Value)
                {
                    if (model.SoilAnalyses.PotassiumIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.PotassiumIndex", Resource.MsgPotassiumIndex);
                    }
                    if (model.SoilAnalyses.PhosphorusIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.PhosphorusIndex", Resource.MsgPhosphorusIndex);
                    }
                    if (model.SoilAnalyses.MagnesiumIndex == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.MagnesiumIndex", Resource.MsgMagnesiumIndex);
                    }
                }
                else
                {
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Potassium"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Potassium"].Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Potassium"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Potassium"].AttemptedValue, Resource.lblPotassiumPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Potassium"].Errors.Clear();
                            ModelState["SoilAnalyses.Potassium"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Phosphorus"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Phosphorus"].Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Phosphorus"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Phosphorus"].AttemptedValue, Resource.lblPhosphorusPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Phosphorus"].Errors.Clear();
                            ModelState["SoilAnalyses.Phosphorus"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Magnesium"))
                    {
                        var InvalidFormatError = ModelState["SoilAnalyses.Magnesium"].Errors.Count > 0 ?
                                        ModelState["SoilAnalyses.Magnesium"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilAnalyses.Magnesium"].AttemptedValue, Resource.lblMagnesiumPerLitreOfSoil)))
                        {
                            ModelState["SoilAnalyses.Magnesium"].Errors.Clear();
                            ModelState["SoilAnalyses.Magnesium"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if (model.SoilAnalyses.Potassium == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.Potassium", Resource.MsgPotassiumPerLitreOfSoil);
                    }
                    if (model.SoilAnalyses.Phosphorus == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.Phosphorus", Resource.MsgPhosphorusPerLitreOfSoil);
                    }
                    if (model.SoilAnalyses.Magnesium == null)
                    {
                        ModelState.AddModelError("SoilAnalyses.Magnesium", Resource.MsgMagnesiumPerLitreOfSoil);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.SoilAnalyses.PhosphorusMethodologyID = (int)PhosphorusMethodology.Olsens;

                if (model.IsSoilNutrientValueTypeIndex != null && (!model.IsSoilNutrientValueTypeIndex.Value))
                {
                    (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
                    if (error == null && nutrients.Count > 0)
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

                        (model.SoilAnalyses.PhosphorusIndex, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.SoilAnalyses.Phosphorus, (int)PhosphorusMethodology.Olsens);
                        (model.SoilAnalyses.MagnesiumIndex, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.SoilAnalyses.Magnesium, (int)MagnesiumMethodology.None);
                        (model.SoilAnalyses.PotassiumIndex, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.SoilAnalyses.Potassium, (int)PotassiumMethodology.None);
                        //if (error != null && error.Message != null)
                        //{
                        //    ViewBag.Error = error.Message;
                        //    return View(model);
                        //}
                    }
                    if (error != null && error.Message != null)
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                }
                else
                {
                    model.SoilAnalyses.Phosphorus = null;
                    model.SoilAnalyses.Magnesium = null;
                    model.SoilAnalyses.Potassium = null;
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
                return View(model);
            }
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
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                cropGroups = await _fieldService.FetchCropGroups();
                //_httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropGroupList", cropGroups);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(FieldViewModel field)
        {
            if (field.CropGroupId == null)
            {
                ModelState.AddModelError("CropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropGroupResponse> cropTypes = new List<CropGroupResponse>();
                ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
                return View(field);
            }

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                if (fieldData.CropGroupId != field.CropGroupId)
                {
                    field.CropType = string.Empty;
                    field.CropTypeID = null;
                }
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            field.CropGroup = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
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
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                cropTypes = await _fieldService.FetchCropTypes(model.CropGroupId ?? 0);
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(FieldViewModel field)
        {
            if (field.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(field.CropGroupId ?? 0);
                var country = field.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
                return View(field);
            }
            field.CropType = await _fieldService.FetchCropTypeById(field.CropTypeID.Value);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("SNSAppliedQuestion");
        }
        [HttpGet]
        public IActionResult SNSAppliedQuestion()
        {
            FieldViewModel? model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SNSAppliedQuestion(FieldViewModel field)
        {
            if (field.WantToApplySns == null)
            {
                ModelState.AddModelError("WantToApplySns", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
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
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model == null)
                {
                    model = new FieldViewModel();
                }
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
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return RedirectToAction("SNSAppliedQuestion");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FieldViewModel model)
        {
            if (!model.CropTypeID.HasValue)
            {
                ModelState.AddModelError("CropTypeID", Resource.MsgPreviousCropTypeNotSet);
            }
            if (model.IsSoilReleasingClay && (!model.SoilReleasingClay.HasValue))
            {
                ModelState.AddModelError("SoilReleasingClay", Resource.MsgSoilReleasingClayNotSet);
            }
            if (!model.IsSoilNutrientValueTypeIndex.Value)
            {
                if (!model.SoilAnalyses.Potassium.HasValue)
                {
                    ModelState.AddModelError("SoilAnalyses.Potassium", Resource.MsgPotassiumNotSet);
                }
                if (!model.SoilAnalyses.Phosphorus.HasValue)
                {
                    ModelState.AddModelError("SoilAnalyses.Phosphorus", Resource.MsgPhosphorusNotSet);
                }
                if (!model.SoilAnalyses.Magnesium.HasValue)
                {
                    ModelState.AddModelError("SoilAnalyses.Magnesium", Resource.MsgMagnesiumNotSet);
                }
            }

            if (!ModelState.IsValid)
            {
                return View("CheckAnswer", model);
            }
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);  // Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.Sid)?.Value);
            var farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            //int farmId = model.FarmID;
            //if (model.WantToApplySns.Value)
            //{
            model.SoilAnalyses.SoilNitrogenSupply = 0;
            model.SoilAnalyses.SoilNitrogenSupplyIndex = 0;
            //}

            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

            FieldData fieldData = new FieldData
            {
                Field = new Field
                {
                    SoilTypeID = model.SoilTypeID,
                    NVZProgrammeID = model.IsWithinNVZ == true ? (int)NMP.Portal.Enums.NVZProgram.CurrentNVZRule : (int)NMP.Portal.Enums.NVZProgram.NotInNVZ,
                    Name = model.Name,
                    LPIDNumber = model.LPIDNumber,
                    NationalGridReference = model.NationalGridReference,
                    OtherReference = model.OtherReference,
                    TotalArea = model.TotalArea,
                    CroppedArea = model.CroppedArea,
                    ManureNonSpreadingArea = model.ManureNonSpreadingArea,
                    SoilReleasingClay = model.SoilReleasingClay,
                    IsWithinNVZ = model.IsWithinNVZ,
                    IsAbove300SeaLevel = model.IsAbove300SeaLevel,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId,
                    ModifiedOn = model.ModifiedOn,
                    ModifiedByID = model.ModifiedByID
                },
                SoilAnalysis = new SoilAnalysis
                {
                    Year = model.SoilAnalyses.Date.Value.Year,
                    SulphurDeficient = model.SoilAnalyses.SulphurDeficient,
                    Date = model.SoilAnalyses.Date,
                    PH = model.SoilAnalyses.PH,
                    PhosphorusMethodologyID = model.SoilAnalyses.PhosphorusMethodologyID,
                    Phosphorus = model.SoilAnalyses.Phosphorus,
                    PhosphorusIndex = model.SoilAnalyses.PhosphorusIndex,
                    Potassium = model.SoilAnalyses.Potassium,
                    PotassiumIndex = model.SoilAnalyses.PotassiumIndex,
                    Magnesium = model.SoilAnalyses.Magnesium,
                    MagnesiumIndex = model.SoilAnalyses.MagnesiumIndex,
                    SoilNitrogenSupply = model.SoilAnalyses.SoilNitrogenSupply,
                    SoilNitrogenSupplyIndex = model.SoilAnalyses.SoilNitrogenSupplyIndex,
                    Sodium = model.SoilAnalyses.Sodium,
                    Lime = model.SoilAnalyses.Lime,
                    PhosphorusStatus = model.SoilAnalyses.PhosphorusStatus,
                    PotassiumAnalysis = model.SoilAnalyses.PotassiumAnalysis,
                    PotassiumStatus = model.SoilAnalyses.PotassiumStatus,
                    MagnesiumAnalysis = model.SoilAnalyses.MagnesiumAnalysis,
                    MagnesiumStatus = model.SoilAnalyses.MagnesiumStatus,
                    NitrogenResidueGroup = model.SoilAnalyses.NitrogenResidueGroup,
                    Comments = model.SoilAnalyses.Comments,
                    PreviousID = model.SoilAnalyses.PreviousID,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId,
                    ModifiedOn = model.SoilAnalyses.ModifiedOn,
                    ModifiedByID = model.SoilAnalyses.ModifiedByID
                },
                Crops = new List<CropData>
                {
                    new CropData
                    {
                        Crop = new Crop
                        {
                            Year=DateTime.Now.Year-1,
                            Confirm=true,
                            CropTypeID=model.CropTypeID,
                            FieldType = model.CropGroupId == (int)NMP.Portal.Enums.CropGroup.Grass ? (int)NMP.Portal.Enums.FieldType.Grass : (int)NMP.Portal.Enums.FieldType.Arable,
                            CreatedOn =DateTime.Now,
                            CreatedByID=userId
                        },
                        ManagementPeriods = new List<ManagementPeriod>
                        {
                            new ManagementPeriod
                            {
                                DefoliationID=1,
                                Utilisation1ID=2,
                                CreatedOn=DateTime.Now,
                                CreatedByID=userId
                            }

                        }
                    },

                }


            };

            (Field fieldResponse, Error error1) = await _fieldService.AddFieldAsync(fieldData, farm.ID, farm.Name);
            if (error1.Message == null && fieldResponse != null)
            {
                string success = _farmDataProtector.Protect("true");
                string fieldName = _farmDataProtector.Protect(fieldResponse.Name);
                _httpContextAccessor.HttpContext?.Session.Remove("FieldData");

                return RedirectToAction("ManageFarmFields", new { id = model.EncryptedFarmId, q = success, name = fieldName });
            }
            else
            {
                TempData["AddFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                return RedirectToAction("CheckAnswer");
            }

        }

        [HttpGet]
        public async Task<IActionResult> ManageFarmFields(string id, string? q, string? name)
        {
            FarmFieldsViewModel model = new FarmFieldsViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Success = false;
            }
            if (!string.IsNullOrWhiteSpace(id))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                model.Fields = await _fieldService.FetchFieldsByFarmId(farmId);

                if (model.Fields != null && model.Fields.Count > 0)
                {
                    model.Fields.ForEach(x => x.EncryptedFieldId = _farmDataProtector.Protect(x.ID.ToString()));
                }
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(farmId);
                model.FarmName = farm.Name;
                if (name != null)
                {
                    model.FieldName = _farmDataProtector.Unprotect(name);
                }
                ViewBag.FieldsList = model.Fields;
                model.EncryptedFarmId = id;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManageFarmFields(FieldViewModel field)
        {

            return RedirectToAction("ManageFarmFields");
        }

        [HttpGet]
        public async Task<IActionResult> FieldSoilAnalysisDetail(string id, string farmId)
        {
            FieldViewModel model = new FieldViewModel();

            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(farmId)));
            int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
            var field = await _fieldService.FetchFieldByFieldId(fieldId);
            model.Name = field.Name;
            model.TotalArea = field.TotalArea ?? 0;
            model.CroppedArea = field.CroppedArea ?? 0;
            model.ManureNonSpreadingArea = field.ManureNonSpreadingArea ?? 0;
            //model.SoilType = await _fieldService.FetchSoilTypeById(field.SoilTypeID.Value); 
            model.SoilReleasingClay = field.SoilReleasingClay ?? false;
            model.IsWithinNVZ = field.IsWithinNVZ ?? false;
            model.IsAbove300SeaLevel = field.IsAbove300SeaLevel ?? false;

            var soilType = await _fieldService.FetchSoilTypeById(field.SoilTypeID.Value);
            model.SoilType = !string.IsNullOrWhiteSpace(soilType) ? soilType : string.Empty;

            model.EncryptedFarmId = farmId;
            model.FarmName = farm.Name;
            List<SoilAnalysisResponse> soilAnalysisResponse = await _fieldService.FetchSoilAnalysisByFieldId(fieldId);
            ViewBag.SampleDate = soilAnalysisResponse;

            return View(model);
        }
    }
}
