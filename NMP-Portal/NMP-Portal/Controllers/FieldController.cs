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
        private readonly IOrganicManureService _organicManureService;

        public FieldController(ILogger<FieldController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, ISoilService soilService,
             IFieldService fieldService, IOrganicManureService organicManureService)
        {
            _logger = logger;
            _farmService = farmService;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldService = fieldService;
            _soilService = soilService;
            _organicManureService = organicManureService;
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
                //SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                //if (soilType != null && soilType.KReleasingClay)
                //{
                //    field.IsSoilReleasingClay = true;
                //    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                //    return RedirectToAction("SoilReleasingClay");
                //}
                //else 
                
                if (field.IsCheckAnswer)
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
                return View(field);
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
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
        public IActionResult SNSAppliedQuestion(FieldViewModel model)
        {
            if (model.WantToApplySns == null)
            {
                ModelState.AddModelError("WantToApplySns", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

            if (model.WantToApplySns != null && model.WantToApplySns.Value)
            {
                return RedirectToAction("SampleForSoilMineralNitrogen");
            }

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

        public async Task<IActionResult> BackCheckAnswer()
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
            if (model.WantToApplySns != null && model.WantToApplySns.Value)
            {
                int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);

                if (snsCategoryId > 0)
                {
                    if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Fruit)
                    {
                        return RedirectToAction("SoilMineralNitrogenAnalysisResults");
                    }
                    else
                    {
                        return RedirectToAction("SoilNitrogenSupplyIndex");
                    }
                }
                else
                {
                    return RedirectToAction("CurrentCropTypes");
                }
            }
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
            if (model.RecentSoilAnalysisQuestion.Value)
            {
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
            }
            if (!ModelState.IsValid)
            {
                return View("CheckAnswer", model);
            }
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);  // Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.Sid)?.Value);
            var farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            //int farmId = model.FarmID;
            if (model.WantToApplySns == true)
            {
                model.SoilAnalyses.SoilNitrogenSupply = model.SnsValue;
                model.SoilAnalyses.SoilNitrogenSupplyIndex = model.SnsIndex;

            }
            else
            {
                model.SoilAnalyses.SoilNitrogenSupply = 0;
                model.SoilAnalyses.SoilNitrogenSupplyIndex = 0;
            }

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
                    SoilNitrogenSampleDate = model.SampleForSoilMineralNitrogen,
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
                SnsAnalysis = model.WantToApplySns == true ? new SnsAnalysis
                {
                    SampleDate = model.SampleForSoilMineralNitrogen,
                    SnsAt0to30cm = model.SoilMineralNitrogenAt030CM,
                    SnsAt30to60cm = model.SoilMineralNitrogenAt3060CM,
                    SnsAt60to90cm = model.SoilMineralNitrogenAt6090CM,
                    SampleDepth = model.SampleDepth,
                    SoilMineralNitrogen = model.SoilMineralNitrogen,
                    NumberOfShoots = model.NumberOfShoots,
                    CropHeight = model.CropHeight,
                    SeasonId = model.SeasonId,
                    PercentageOfOrganicMatter = model.SoilOrganicMatter,
                    AdjustmentValue = model.AdjustmentValue,
                    SoilNitrogenSupplyValue = model.SnsValue,
                    SoilNitrogenSupplyIndex = model.SnsIndex,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId,
                    ModifiedOn = model.ModifiedOn,
                    ModifiedByID = model.ModifiedByID

                } : null,
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
                            CropOrder=1,
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

        [HttpGet]
        public async Task<IActionResult> SampleForSoilMineralNitrogen()
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
        public IActionResult SampleForSoilMineralNitrogen(FieldViewModel model)
        {

            if ((!ModelState.IsValid) && ModelState.ContainsKey("SampleForSoilMineralNitrogen"))
            {
                var dateError = ModelState["SampleForSoilMineralNitrogen"].Errors.Count > 0 ?
                                ModelState["SampleForSoilMineralNitrogen"].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.MsgSampleForSoilMineralNitrogenForError)))
                {
                    ModelState["SampleForSoilMineralNitrogen"].Errors.Clear();
                    ModelState["SampleForSoilMineralNitrogen"].Errors.Add(Resource.MsgDateEnteredIsNotValid);
                }
            }

            if (model.SampleForSoilMineralNitrogen == null)
            {
                ModelState.AddModelError("SampleForSoilMineralNitrogen", Resource.MsgdateMustBeFilledBeforeProceeding);
            }
            if (DateTime.TryParseExact(model.SampleForSoilMineralNitrogen.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("SampleForSoilMineralNitrogen", Resource.MsgDateEnteredIsNotValid);
            }

            if (model.SampleForSoilMineralNitrogen != null)
            {
                if (model.SampleForSoilMineralNitrogen.Value.Date > DateTime.Now)
                {
                    ModelState.AddModelError("SampleForSoilMineralNitrogen", Resource.MsgDateShouldNotBeInTheFuture);
                }
                if (model.SampleForSoilMineralNitrogen.Value.Date.Year < 1601)
                {
                    ModelState.AddModelError("SampleForSoilMineralNitrogen", Resource.MsgDateEnteredIsNotValid);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.SampleForSoilMineralNitrogen == model.SampleForSoilMineralNitrogen)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            return RedirectToAction("CurrentCropGroups");
        }
        [HttpGet]
        public async Task<IActionResult> CurrentCropGroups()
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
                ViewBag.CropGroupList = cropGroups;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("SampleForSoilMineralNitrogen");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CurrentCropGroups(FieldViewModel model)
        {
            if (model.CurrentCropGroupId == null)
            {
                ModelState.AddModelError("CurrentCropGroupId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropGroup.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                ViewBag.CropGroupList = await _fieldService.FetchCropGroups();
                return View(model);
            }

            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                if (model.CurrentCropGroupId != fieldData.CurrentCropGroupId)
                {
                    model.CurrentCropType = string.Empty;
                    model.CurrentCropTypeId = null;
                }
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.CurrentCropGroup = await _fieldService.FetchCropGroupById(model.CurrentCropGroupId.Value);
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.CurrentCropGroupId == model.CurrentCropGroupId)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.CurrentCropTypeId = null;
                        model.CurrentCropType = null;
                        model.SoilMineralNitrogenAt030CM = null;
                        model.SoilMineralNitrogenAt3060CM = null;
                        model.SoilMineralNitrogenAt6090CM = null;
                        model.SampleDepth = null;
                        model.SoilMineralNitrogen = null;
                        model.IsCalculateNitrogen = null;
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.NumberOfShoots = null;
                        model.SeasonId = 0;
                        model.GreenAreaIndexOrCropHeight = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;
                        model.SnsCategoryId = null;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }

            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

            return RedirectToAction("CurrentCropTypes");
        }

        [HttpGet]
        public async Task<IActionResult> CurrentCropTypes()
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

                cropTypes = await _fieldService.FetchCropTypes(model.CurrentCropGroupId ?? 0);
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();

                ViewBag.CropTypeList = cropTypeList;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropGroups");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CurrentCropTypes(FieldViewModel model)
        {
            if (model.CurrentCropTypeId == null)
            {
                ModelState.AddModelError("CurrentCropTypeId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(model.CurrentCropGroupId ?? 0);
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.Country.England : (int)NMP.Portal.Enums.Country.Scotland;
                ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.Country.All).ToList();
                return View(model);
            }
            model.CurrentCropType = await _fieldService.FetchCropTypeById(model.CurrentCropTypeId.Value);

            (CropTypeLinkingResponse cropTypeLinking, Error error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(model.CurrentCropTypeId.Value);
            if (cropTypeLinking != null)// && cropTypeLinking.SNSCategoryID != null
            {
                model.SnsCategoryId = cropTypeLinking.SNSCategoryID;
            }


            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                if (fieldViewModel != null && fieldViewModel.CurrentCropTypeId == model.CurrentCropTypeId)
                {
                    if (model.IsCheckAnswer)
                    {
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                        return RedirectToAction("CheckAnswer");
                    }
                }
                else
                {
                    model.SoilMineralNitrogenAt030CM = null;
                    model.SoilMineralNitrogenAt3060CM = null;
                    model.SoilMineralNitrogenAt6090CM = null;
                    model.SampleDepth = null;
                    model.SoilMineralNitrogen = null;
                    model.IsCalculateNitrogen = null;
                    model.IsEstimateOfNitrogenMineralisation = null;
                    model.IsBasedOnSoilOrganicMatter = null;
                    model.NumberOfShoots = null;
                    model.SeasonId = 0;
                    model.GreenAreaIndexOrCropHeight = 0;
                    model.CropHeight = null;
                    model.GreenAreaIndex = null;
                    model.IsCropHeight = false;
                    model.IsGreenAreaIndex = false;
                    model.IsNumberOfShoots = false;
                    model.SoilOrganicMatter = null;
                    model.AdjustmentValue = null;
                    model.SnsIndex = 0;
                    model.SnsValue = 0;

                }
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            if (error == null)
            {
                if (cropTypeLinking != null && cropTypeLinking.SNSCategoryID != null)
                {
                    if (cropTypeLinking.SNSCategoryID == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes ||
                        cropTypeLinking.SNSCategoryID == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape ||
                        cropTypeLinking.SNSCategoryID == (int)NMP.Portal.Enums.SNSCategories.WinterCereals ||
                        cropTypeLinking.SNSCategoryID == (int)NMP.Portal.Enums.SNSCategories.Fruit)
                    {
                        model.SampleDepth = null;
                        model.SoilMineralNitrogen = null;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                        return RedirectToAction("SoilMineralNitrogenAnalysisResults");
                    }
                    else if (cropTypeLinking.SNSCategoryID == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
                    {
                        model.SoilMineralNitrogenAt030CM = null;
                        model.SoilMineralNitrogenAt3060CM = null;
                        model.SoilMineralNitrogenAt6090CM = null;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                        return RedirectToAction("SampleDepth");
                    }
                }
                else if (cropTypeLinking != null && cropTypeLinking.SNSCategoryID == null)
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            else
            {
                TempData["Error"] = error.Message;
                return View(model);
            }
            return RedirectToAction("SoilMineralNitrogenAnalysisResults");
        }
        [HttpGet]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults()
        {
            FieldViewModel model = new FieldViewModel();

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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropTypes");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults(FieldViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogenAt030CM"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogenAt030CM"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogenAt030CM"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt030CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt030CMForError)))
                {
                    ModelState["SoilMineralNitrogenAt030CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt030CM"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblKilogramsOfSoilMineralNitrogenAt030CM));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogenAt3060CM"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogenAt3060CM"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogenAt3060CM"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt3060CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt3060CMForError)))
                {
                    ModelState["SoilMineralNitrogenAt3060CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt3060CM"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblKilogramsOfSoilMineralNitrogenAt3060CM));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogenAt6090CM"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogenAt6090CM"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogenAt6090CM"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt6090CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt6090CMForError)))
                {
                    ModelState["SoilMineralNitrogenAt6090CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt6090CM"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblKilogramsOfSoilMineralNitrogenAt6090CM));
                }
            }
            if (model.SoilMineralNitrogenAt030CM == null)
            {
                ModelState.AddModelError("SoilMineralNitrogenAt030CM", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblKilogramsOfSoilMineralNitrogenAt030CM));
            }
            if (model.SoilMineralNitrogenAt3060CM == null)
            {
                ModelState.AddModelError("SoilMineralNitrogenAt3060CM", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblKilogramsOfSoilMineralNitrogenAt3060CM));
            }
            if (model.SoilMineralNitrogenAt030CM != null)
            {
                if (model.SoilMineralNitrogenAt030CM < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt030CM", string.Format(Resource.lblEnterValidValue, Resource.lblKilogramsOfSoilMineralNitrogenAt030CM));
                }
            }
            if (model.SoilMineralNitrogenAt3060CM != null)
            {
                if (model.SoilMineralNitrogenAt3060CM < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt3060CM", string.Format(Resource.lblEnterValidValue, Resource.lblKilogramsOfSoilMineralNitrogenAt3060CM));
                }
            }
            if (model.SoilMineralNitrogenAt6090CM != null)
            {
                if (model.SoilMineralNitrogenAt6090CM < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt6090CM", string.Format(Resource.lblEnterValidValue, Resource.lblKilogramsOfSoilMineralNitrogenAt6090CM));
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.SoilMineralNitrogenAt030CM == model.SoilMineralNitrogenAt030CM && fieldViewModel.SoilMineralNitrogenAt3060CM == model.SoilMineralNitrogenAt3060CM && fieldViewModel.SoilMineralNitrogenAt6090CM == model.SoilMineralNitrogenAt6090CM)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.SampleDepth = null;
                        model.SoilMineralNitrogen = null;
                        model.IsCalculateNitrogen = null;
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.NumberOfShoots = null;
                        model.SeasonId = 0;
                        model.GreenAreaIndexOrCropHeight = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }

            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);
            if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals || snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
            {
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Fruit)
            {
                return RedirectToAction("CheckAnswer");
            }
            else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }

            return RedirectToAction("SoilMineralNitrogenAnalysisResults");
        }
        [HttpGet]
        public async Task<IActionResult> SampleDepth()
        {
            FieldViewModel model = new FieldViewModel();
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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropTypes");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleDepth(FieldViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SampleDepth"))
            {
                var InvalidFormatError = ModelState["SampleDepth"].Errors.Count > 0 ?
                                ModelState["SampleDepth"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SampleDepth"].AttemptedValue, Resource.lblSamplelDepthForError)))
                {
                    ModelState["SampleDepth"].Errors.Clear();
                    ModelState["SampleDepth"].Errors.Add(Resource.MsgEnterValidNumericValueBeforeContinuing);
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogen"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogen"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogen"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogen"].AttemptedValue, Resource.lblSoilMineralNitrogenForError)))
                {
                    ModelState["SoilMineralNitrogen"].Errors.Clear();
                    ModelState["SoilMineralNitrogen"].Errors.Add(Resource.MsgEnterValidNumericValueBeforeContinuing);
                }
            }
            if (model.SampleDepth == null)
            {
                ModelState.AddModelError("SampleDepth", Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.SoilMineralNitrogen == null)
            {
                ModelState.AddModelError("SoilMineralNitrogen", Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.SampleDepth != null)
            {
                if (model.SampleDepth < 0)
                {
                    ModelState.AddModelError("SampleDepth", Resource.MsgEnterValidNumericValueBeforeContinuing);
                }
            }
            if (model.SoilMineralNitrogen != null)
            {
                if (model.SoilMineralNitrogen < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogen", Resource.MsgEnterValidNumericValueBeforeContinuing);
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.SampleDepth == model.SampleDepth && fieldViewModel.SoilMineralNitrogen == model.SoilMineralNitrogen)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsCalculateNitrogen = null;
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.NumberOfShoots = null;
                        model.SeasonId = 0;
                        model.GreenAreaIndexOrCropHeight = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion()
        {
            FieldViewModel model = new FieldViewModel();
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

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion(FieldViewModel model)
        {
            if (model.IsCalculateNitrogen == null)
            {
                ModelState.AddModelError("IsCalculateNitrogen", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.IsCalculateNitrogen == model.IsCalculateNitrogen)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.NumberOfShoots = null;
                        model.SeasonId = 0;
                        model.GreenAreaIndexOrCropHeight = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

            if (model.IsCalculateNitrogen == true)
            {
                if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals)
                {
                    return RedirectToAction("NumberOfShoots");
                }
                if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                {
                    return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
                }
            }
            else
            {
                model.IsCalculateNitrogenNo = true;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }




            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> NumberOfShoots()
        {
            FieldViewModel model = new FieldViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

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
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NumberOfShoots(FieldViewModel model)
        {
            if (model.NumberOfShoots == null)
            {
                ModelState.AddModelError("NumberOfShoots", Resource.lblEnterAValidNumber);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.NumberOfShoots != null && (model.NumberOfShoots < 0 || model.NumberOfShoots > 1500))
            {
                ModelState.AddModelError("NumberOfShoots", Resource.MsgEnterShootNumberBetween0To1500);
            }
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = new List<SeasonResponse>();
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsNumberOfShoots = true;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.NumberOfShoots == model.NumberOfShoots && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.GreenAreaIndexOrCropHeight = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> GreenAreaIndexOrCropHeightQuestion()
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
                ViewBag.CropGroupList = cropGroups;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GreenAreaIndexOrCropHeightQuestion(FieldViewModel model)
        {
            if (model.GreenAreaIndexOrCropHeight == 0)
            {
                ModelState.AddModelError("GreenAreaIndexOrCropHeight", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.GreenAreaIndexOrCropHeight == model.GreenAreaIndexOrCropHeight)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.NumberOfShoots = null;
                        model.SeasonId = 0;
                        model.CropHeight = null;
                        model.GreenAreaIndex = null;
                        model.IsCropHeight = false;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Portal.Enums.GreenAreaIndexOrCropHeight.CropHeight)
            {
                return RedirectToAction("CropHeight");
            }
            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Portal.Enums.GreenAreaIndexOrCropHeight.GAI)
            {
                return RedirectToAction("GreenAreaIndex");
            }
            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> BackActionForCalculateNitrogenCropQuestion()
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
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals || snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
            {
                return RedirectToAction("SampleDepth");
            }


            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> CropHeight()
        {
            FieldViewModel model = new FieldViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

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
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropHeight(FieldViewModel model)
        {
            if (model.CropHeight == null)
            {
                ModelState.AddModelError("CropHeight", Resource.lblEnterACropHeightBeforeContinue);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.CropHeight != null && (model.CropHeight < 0 || model.CropHeight > 30))
            {
                ModelState.AddModelError("CropHeight", Resource.MSGEnterAValidCropHeight);
            }
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = new List<SeasonResponse>();
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsCropHeight = true;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.CropHeight == model.CropHeight && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.GreenAreaIndex = null;
                        model.IsGreenAreaIndex = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> GreenAreaIndex()
        {
            FieldViewModel model = new FieldViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

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
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GreenAreaIndex(FieldViewModel model)
        {
            if (model.GreenAreaIndex == null)
            {
                ModelState.AddModelError("GreenAreaIndex", Resource.lblEnterGAIValueBeforeContinue);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.GreenAreaIndex != null && (model.GreenAreaIndex < 0 || model.GreenAreaIndex > 3))
            {
                ModelState.AddModelError("GreenAreaIndex", Resource.MsgEnterAValidNumericGAIvalue);
            }
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = new List<SeasonResponse>();
                seasons = await _fieldService.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsGreenAreaIndex = true;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.GreenAreaIndex == model.GreenAreaIndex && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsEstimateOfNitrogenMineralisation = null;
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.CropHeight = null;
                        model.IsCropHeight = false;
                        model.IsNumberOfShoots = false;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }



        [HttpGet]
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion()
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

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion(FieldViewModel model)
        {
            if (model.IsEstimateOfNitrogenMineralisation == null)
            {
                ModelState.AddModelError("IsEstimateOfNitrogenMineralisation", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.IsEstimateOfNitrogenMineralisation == model.IsEstimateOfNitrogenMineralisation)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsBasedOnSoilOrganicMatter = null;
                        model.SoilOrganicMatter = null;
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.IsEstimateOfNitrogenMineralisation == true)
            {
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            else
            {
                model.AdjustmentValue = null;
                model.SoilOrganicMatter = null;
                model.IsBasedOnSoilOrganicMatter = null;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                return RedirectToAction("SoilNitrogenSupplyIndex");
            }

        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyIndex()
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

                //sns logic
                var postMeasurementData = new MeasurementData();
                int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);
                if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals || snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                {
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        SeasonId = model.SeasonId == 0 ? 1 : model.SeasonId,
                        Step1ArablePotato = new Step1ArablePotato
                        {
                            Depth0To30Cm = model.SoilMineralNitrogenAt030CM,
                            Depth30To60Cm = model.SoilMineralNitrogenAt3060CM,
                            Depth60To90Cm = model.SoilMineralNitrogenAt6090CM
                        },
                        Step2 = new Step2
                        {
                            ShootNumber = model.NumberOfShoots > 0 ? model.NumberOfShoots : null,
                            GreenAreaIndex = model.GreenAreaIndex > 0 ? model.GreenAreaIndex : null,
                            CropHeight = model.CropHeight > 0 ? model.CropHeight : null
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue > 0 ? model.AdjustmentValue : null,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };

                }
                else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
                {
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        SeasonId = 1,
                        Step1ArablePotato = new Step1ArablePotato
                        {
                            Depth0To30Cm = model.SoilMineralNitrogenAt030CM,
                            Depth30To60Cm = model.SoilMineralNitrogenAt3060CM,
                            Depth60To90Cm = model.SoilMineralNitrogenAt6090CM
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue > 0 ? model.AdjustmentValue : null,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };

                }
                else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
                {
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        SeasonId = 1,
                        Step1Veg = new Step1Veg
                        {
                            DepthCm = model.SampleDepth,
                            DepthValue = model.SoilMineralNitrogen
                        },
                        Step3 = new Step3
                        {
                            Adjustment = null,
                            OrganicMatterPercentage = null
                        }
                    };

                }
                else
                {
                    return RedirectToAction("CheckAnswer");
                }
                if (postMeasurementData.CropTypeId > 0)
                {
                    (SnsResponse snsResponse, Error error) = await _fieldService.FetchSNSIndexByMeasurementMethodAsync(postMeasurementData);
                    if (error.Message == null)
                    {
                        model.SnsIndex = snsResponse.SnsIndex;
                        model.SnsValue = snsResponse.SnsValue;
                        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                    }
                }

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNitrogenSupplyIndex(FieldViewModel model)
        {
            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter()
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

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }
            return View("CalculateSoilNitrogenMineralisation", model);
        }

        [HttpPost]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter(FieldViewModel model)
        {
            try
            {
                if (model.IsBasedOnSoilOrganicMatter == null)
                {
                    ModelState.AddModelError("IsBasedOnSoilOrganicMatter", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View("CalculateSoilNitrogenMineralisation", model);
                }
                if (model.IsCheckAnswer)
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                    {
                        FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                        if (fieldViewModel.IsBasedOnSoilOrganicMatter == model.IsBasedOnSoilOrganicMatter)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                        else
                        {
                            model.SoilOrganicMatter = null;
                            model.AdjustmentValue = null;
                            model.SnsIndex = 0;
                            model.SnsValue = 0;
                        }
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                if (model.IsBasedOnSoilOrganicMatter.Value)
                {
                    return RedirectToAction("SoilOrganicMatter");
                }
                else
                {
                    return RedirectToAction("AdjustmentValue");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
        }
        [HttpGet]
        public async Task<IActionResult> AdjustmentValue()
        {
            FieldViewModel model = new FieldViewModel();

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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustmentValue(FieldViewModel model)
        {

            if ((!ModelState.IsValid) && ModelState.ContainsKey("AdjustmentValue"))
            {
                var InvalidFormatError = ModelState["AdjustmentValue"].Errors.Count > 0 ?
                                ModelState["AdjustmentValue"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["AdjustmentValue"].AttemptedValue, Resource.lblAdjustmentValueForError)))
                {
                    ModelState["AdjustmentValue"].Errors.Clear();
                    ModelState["AdjustmentValue"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAdjustmentValue));
                }
            }
            if (model.AdjustmentValue == null)
            {
                ModelState.AddModelError("AdjustmentValue", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAdjustmentValue.ToLower()));
            }
            if (model.AdjustmentValue != null && (model.AdjustmentValue < 0 || model.AdjustmentValue > 60))
            {
                ModelState.AddModelError("AdjustmentValue", string.Format(Resource.MsgEnterValueInBetween, Resource.lblValue.ToLower(), 0, 60));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.SoilOrganicMatter = null;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.AdjustmentValue == model.AdjustmentValue)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.SoilOrganicMatter = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);


            return RedirectToAction("SoilNitrogenSupplyIndex");
        }
        [HttpGet]
        public async Task<IActionResult> SoilOrganicMatter()
        {
            FieldViewModel model = new FieldViewModel();

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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilOrganicMatter(FieldViewModel model)
        {

            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilOrganicMatter"))
            {
                var InvalidFormatError = ModelState["SoilOrganicMatter"].Errors.Count > 0 ?
                                ModelState["SoilOrganicMatter"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilOrganicMatter"].AttemptedValue, Resource.lblSoilOrganicMatterForError)))
                {
                    ModelState["SoilOrganicMatter"].Errors.Clear();
                    ModelState["SoilOrganicMatter"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblSoilOrganicMatter));
                }
            }
            if (model.SoilOrganicMatter == null)
            {
                ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPercentageValue));
            }
            if (model.SoilOrganicMatter != null && (model.SoilOrganicMatter < 4 || model.SoilOrganicMatter > 10))
            {
                ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterValueInBetween, Resource.lblPercentageLable.ToLower(), 4, 10));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.AdjustmentValue = null;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldViewModel.SoilOrganicMatter == model.SoilOrganicMatter)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.AdjustmentValue = null;
                        model.SnsIndex = 0;
                        model.SnsValue = 0;

                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);


            return RedirectToAction("SoilNitrogenSupplyIndex");
        }
        [HttpGet]
        public async Task<IActionResult> BackActionForEstimateOfNitrogenMineralisationQuestion()
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
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            else if (model.IsCalculateNitrogenNo == true)
            {
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            else if (model.IsNumberOfShoots == true)
            {
                return RedirectToAction("NumberOfShoots");
            }
            else if (model.IsCropHeight == true)
            {
                return RedirectToAction("CropHeight");
            }
            else if (model.IsGreenAreaIndex == true)
            {
                return RedirectToAction("GreenAreaIndex");
            }
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CurrentCropTypeId ?? 0);

            if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals || snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
            {
                return RedirectToAction("SampleDepth");
            }
            return RedirectToAction("SoilMineralNitrogenAnalysisResults");
        }
        [HttpGet]
        public async Task<IActionResult> RecentSoilAnalysisQuestion()
        {
            FieldViewModel model = new FieldViewModel();

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
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilType");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecentSoilAnalysisQuestion(FieldViewModel model)
        {
            if (model.RecentSoilAnalysisQuestion == null)
            {
                ModelState.AddModelError("RecentSoilAnalysisQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);            


                if (model.RecentSoilAnalysisQuestion.Value)
                {
                    List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
                    if (soilTypes.Count > 0)
                    {
                        SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == model.SoilTypeID);

                        if (soilType != null && soilType.KReleasingClay)
                        {
                            model.IsSoilReleasingClay = true;
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                            return RedirectToAction("SoilReleasingClay");
                        }
                    }
                    return RedirectToAction("SulphurDeficient");
                }
                else
                {
                    model.SoilAnalyses.SulphurDeficient = null;
                    model.SoilReleasingClay = null;
                    model.SoilAnalyses.Date = null;
                    model.SoilAnalyses.PH = null;
                    model.SoilAnalyses.Phosphorus = null;
                    model.SoilAnalyses.Magnesium = null;
                    model.SoilAnalyses.Potassium = null;
                    model.SoilAnalyses.PotassiumIndex = null;
                    model.SoilAnalyses.MagnesiumIndex = null;
                    model.SoilAnalyses.PhosphorusIndex = null;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                    return RedirectToAction("CropGroups");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
    }
}
