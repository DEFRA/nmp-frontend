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
using System.Diagnostics.Eventing.Reader;
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
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _soilAnalysisDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ISoilService _soilService;
        private readonly IOrganicManureService _organicManureService;
        private readonly ISoilAnalysisService _soilAnalysisService;
        private readonly IPKBalanceService _pKBalanceService;
        private readonly ICropService _iCropService;

        public FieldController(ILogger<FieldController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, ISoilService soilService,
             IFieldService fieldService, IOrganicManureService organicManureService, ISoilAnalysisService soilAnalysisService, IPKBalanceService pKBalanceService, ICropService cropService)
        {
            _logger = logger;
            _farmService = farmService;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _soilAnalysisDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.SoilAnalysisController");
            _fieldService = fieldService;
            _soilService = soilService;
            _organicManureService = organicManureService;
            _soilAnalysisService = soilAnalysisService;
            _pKBalanceService = pKBalanceService;
            _iCropService = cropService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace($"Field Controller : Index() action called");
            return View();
        }

        public IActionResult CreateFieldCancel(string id)
        {
            _logger.LogTrace($"Field Controller : CreateFieldCancel({id}) action called");
            _httpContextAccessor.HttpContext?.Session.Remove("FieldData");
            return RedirectToAction("FarmSummary", "Farm", new { Id = id });
        }

        public async Task<IActionResult> BackActionForAddField(string id)
        {
            _logger.LogTrace($"Field Controller : BackActionForAddField({id}) action called");
            FieldViewModel model = new FieldViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                }
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                int fieldCount = await _fieldService.FetchFieldCountByFarmIdAsync(Convert.ToInt32(farmID));

                if (fieldCount > 0)
                {
                    if (model != null && model.CopyExistingField != null && model.CopyExistingField.Value)
                    {
                        return RedirectToAction("CopyFields", "Field");
                    }
                    else
                    {
                        return RedirectToAction("CopyExistingField", "Field", new { q = id });
                    }
                    //return RedirectToAction("ManageFarmFields", "Field", new { id = id });
                }
                else
                {
                    _httpContextAccessor.HttpContext?.Session.Remove("FieldData");
                    return RedirectToAction("FarmSummary", "Farm", new { id = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in BackActionForAddField() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnBackButton"] = ex.Message;
                return View("AddField", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddField(string q)//EncryptedfarmId
        {
            _logger.LogTrace($"Field Controller : AddField({q}) action called");
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
                _logger.LogTrace($"Field Controller : Exception in BackActionForAddField() action : {ex.Message}, {ex.StackTrace}");
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
            _logger.LogTrace($"Field Controller : AddField() action called");
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
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }

            if (field.CopyExistingField != null && (field.CopyExistingField.Value))
            {
                (FieldResponse fieldResponse, Error error) = await _fieldService.FetchFieldSoilAnalysisAndSnsById(field.ID.Value);
                if (fieldResponse != null && string.IsNullOrWhiteSpace(error.Message))
                {
                    //field.Name = fieldData.Name;
                    field.NationalGridReference = fieldResponse.Field.NationalGridReference;
                    field.OtherReference = fieldResponse.Field.OtherReference;
                    field.TotalArea = fieldResponse.Field.TotalArea;
                    field.CroppedArea = fieldResponse.Field.CroppedArea;
                    field.LPIDNumber = fieldResponse.Field.LPIDNumber;
                    field.ManureNonSpreadingArea = fieldResponse.Field.ManureNonSpreadingArea;
                    field.NVZProgrammeID = fieldResponse.Field.NVZProgrammeID;
                    field.IsWithinNVZ = fieldResponse.Field.IsWithinNVZ;
                    field.IsAbove300SeaLevel = fieldResponse.Field.IsAbove300SeaLevel;
                    field.SoilReleasingClay = fieldResponse.Field.SoilReleasingClay;
                    field.SoilOverChalk = fieldResponse.Field.SoilOverChalk;
                    field.SoilTypeID = fieldResponse.Field.SoilTypeID;
                    //field.SoilType = Enum.GetName(typeof(NMP.Portal.Enums.SoilTypeEngland), field.SoilTypeID);
                    List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypes();
                    SoilTypesResponse? soilType = soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);
                    if (soilType != null && soilType.KReleasingClay)
                    {
                        field.IsSoilReleasingClay = true;
                    }
                    else
                    {
                        field.IsSoilReleasingClay = false;
                    }
                    field.SoilType = await _soilService.FetchSoilTypeById(field.SoilTypeID.Value);

                    if (fieldResponse.SoilAnalysis != null)
                    {
                        field.SoilAnalyses.PH = fieldResponse.SoilAnalysis.PH;
                        field.SoilAnalyses.Phosphorus = fieldResponse.SoilAnalysis.Phosphorus;
                        field.SoilAnalyses.PhosphorusIndex = fieldResponse.SoilAnalysis.PhosphorusIndex;
                        field.SoilAnalyses.Potassium = fieldResponse.SoilAnalysis.Potassium;
                        field.SoilAnalyses.PotassiumIndex = fieldResponse.SoilAnalysis.PotassiumIndex;
                        field.SoilAnalyses.Magnesium = fieldResponse.SoilAnalysis.Magnesium;
                        field.SoilAnalyses.MagnesiumIndex = fieldResponse.SoilAnalysis.MagnesiumIndex;
                        field.SoilAnalyses.PhosphorusMethodologyID = fieldResponse.SoilAnalysis.PhosphorusMethodologyID;
                        field.SoilAnalyses.SulphurDeficient = fieldResponse.SoilAnalysis.SulphurDeficient;
                        field.SoilAnalyses.Date = fieldResponse.SoilAnalysis.Date;
                        field.RecentSoilAnalysisQuestion = true;
                        if (fieldResponse.SoilAnalysis.PotassiumIndex != null)
                        {
                            field.PotassiumIndexValue = fieldResponse.SoilAnalysis.PotassiumIndex.ToString() == Resource.lblMinusTwo ? Resource.lblTwoMinus : (fieldResponse.SoilAnalysis.PotassiumIndex.ToString() == Resource.lblPlusTwo ? Resource.lblTwoPlus : fieldResponse.SoilAnalysis.PotassiumIndex.ToString());
                        }
                        if (field.SoilAnalyses.Potassium != null || field.SoilAnalyses.Phosphorus != null
                            || field.SoilAnalyses.Magnesium != null)
                        {
                            field.IsSoilNutrientValueTypeIndex = false;
                        }
                        else
                        {
                            field.IsSoilNutrientValueTypeIndex = true;
                        }
                    }
                    else
                    {
                        field.RecentSoilAnalysisQuestion = false;
                    }

                    if (fieldResponse.PreviousGrasses != null && fieldResponse.PreviousGrasses.Count > 0)
                    {
                        List<int> PreviousGrassYears = new List<int>();
                        foreach (var year in fieldResponse.PreviousGrasses)
                        {
                            PreviousGrassYears.Add(year.HarvestYear.Value);
                        }
                        field.PreviousGrasses.GrassManagementOptionID = fieldResponse.PreviousGrasses[0].GrassManagementOptionID;
                        field.PreviousGrasses.GrassTypicalCutID = fieldResponse.PreviousGrasses[0].GrassTypicalCutID;
                        field.PreviousGrasses.HasGreaterThan30PercentClover = fieldResponse.PreviousGrasses[0].HasGreaterThan30PercentClover;
                        field.PreviousGrasses.SoilNitrogenSupplyItemID = fieldResponse.PreviousGrasses[0].SoilNitrogenSupplyItemID;
                        field.PreviousGrasses.HasGrassInLastThreeYear = fieldResponse.PreviousGrasses[0].HasGrassInLastThreeYear;
                        field.PreviousGrassYears = PreviousGrassYears;

                    }
                    else
                    {
                        field.PreviousGrasses.HasGrassInLastThreeYear = false;
                        List<CropTypeResponse> cropTypeResponses = await _fieldService.FetchAllCropTypes();
                        if (fieldResponse.Crop != null)
                        {
                            field.CropTypeID = fieldResponse.Crop.CropTypeID;

                            field.CropType = await _fieldService.FetchCropTypeById(field.CropTypeID ?? 0);
                            if (cropTypeResponses.Count > 0)
                            {
                                var cropType = cropTypeResponses.FirstOrDefault(x => x.CropTypeId == field.CropTypeID);
                                if (cropType != null)
                                {
                                    field.CropGroupId = cropType.CropGroupId;
                                    field.CropGroup = await _fieldService.FetchCropGroupById(field.CropGroupId.Value);
                                }
                            }
                        }
                        if (fieldResponse.SnsAnalyses != null)
                        {
                            field.CurrentCropTypeId = fieldResponse.SnsAnalyses.CurrentCropTypeID;
                            field.SoilMineralNitrogen = fieldResponse.SnsAnalyses.SoilMineralNitrogen;
                            field.SnsValue = fieldResponse.SnsAnalyses.SoilNitrogenSupplyValue.Value;
                            field.SnsIndex = fieldResponse.SnsAnalyses.SoilNitrogenSupplyIndex.Value;
                            field.SampleForSoilMineralNitrogen = fieldResponse.SnsAnalyses.SampleDate.Value;
                            field.SoilMineralNitrogenAt030CM = fieldResponse.SnsAnalyses.SnsAt0to30cm;
                            field.SoilMineralNitrogenAt3060CM = fieldResponse.SnsAnalyses.SnsAt30to60cm;
                            field.SoilMineralNitrogenAt6090CM = fieldResponse.SnsAnalyses.SnsAt60to90cm;
                            field.SampleDepth = fieldResponse.SnsAnalyses.SampleDepth;
                            field.NumberOfShoots = fieldResponse.SnsAnalyses.NumberOfShoots;
                            field.CropHeight = fieldResponse.SnsAnalyses.CropHeight;
                            field.SeasonId = fieldResponse.SnsAnalyses.SeasonId.Value;
                            field.SoilOrganicMatter = fieldResponse.SnsAnalyses.PercentageOfOrganicMatter;
                            field.AdjustmentValue = fieldResponse.SnsAnalyses.AdjustmentValue;
                            field.WantToApplySns = true;
                            if (field.SoilOrganicMatter != null ||
                                field.AdjustmentValue != null)
                            {
                                field.IsEstimateOfNitrogenMineralisation = true;
                            }
                            else
                            {
                                field.IsEstimateOfNitrogenMineralisation = false;
                            }
                            if (field.SoilOrganicMatter != null)
                            {
                                field.IsBasedOnSoilOrganicMatter = true;
                            }
                            if (field.AdjustmentValue != null)
                            {
                                field.IsBasedOnSoilOrganicMatter = false;
                            }
                            field.CurrentCropType = Enum.GetName(typeof(NMP.Portal.Enums.CropTypes), field.CurrentCropTypeId);

                            if (cropTypeResponses.Count > 0)
                            {
                                var cropType = cropTypeResponses.FirstOrDefault(x => x.CropTypeId == field.CurrentCropTypeId);
                                if (cropType != null)
                                {
                                    field.CurrentCropGroupId = cropType.CropGroupId;
                                    field.CurrentCropGroup = await _fieldService.FetchCropGroupById(field.CurrentCropGroupId.Value);
                                }
                            }
                            (CropTypeLinkingResponse cropTypeLinking, Error cropTypeError) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(field.CurrentCropTypeId.Value);
                            if (cropTypeLinking != null && cropTypeError == null)// && cropTypeLinking.SNSCategoryID != null
                            {
                                field.SnsCategoryId = cropTypeLinking.SNSCategoryID;
                            }
                            if (field.SnsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals ||
                                field.SnsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                            {
                                if (field.CropHeight != null)
                                {
                                    field.GreenAreaIndexOrCropHeight = (int)NMP.Portal.Enums.GreenAreaIndexOrCropHeight.CropHeight;
                                    field.IsCropHeight = true;
                                    field.IsCalculateNitrogen = true;
                                }
                                else if (field.GreenAreaIndex != null)
                                {
                                    field.GreenAreaIndexOrCropHeight = (int)NMP.Portal.Enums.GreenAreaIndexOrCropHeight.GAI;
                                    field.IsGreenAreaIndex = true;
                                    field.IsCalculateNitrogen = true;
                                }
                                else if (field.NumberOfShoots != null)
                                {
                                    field.IsNumberOfShoots = true;
                                    field.IsCalculateNitrogen = true;
                                }
                                if (field.NumberOfShoots != null || field.GreenAreaIndex != null
                                    || field.CropHeight != null)
                                {
                                    field.IsCalculateNitrogen = true;
                                }
                                else
                                {
                                    field.IsCalculateNitrogen = false;
                                    field.IsCalculateNitrogenNo = true;
                                }
                                //if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals)
                                //{
                                //    return RedirectToAction("NumberOfShoots");
                                //}
                                //if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                                //{
                                //    return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
                                //}
                            }
                            field.PreviousGrasses.HasGrassInLastThreeYear = false;
                            string farmId = _farmDataProtector.Unprotect(field.EncryptedFarmId);
                            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                            if (string.IsNullOrWhiteSpace(error.Message))
                            {
                                field.IsWithinNVZForFarm = farm.NVZFields == (int)NMP.Portal.Enums.NVZFields.SomeFieldsInNVZ ? true : false;
                                field.IsAbove300SeaLevelForFarm = farm.FieldsAbove300SeaLevel == (int)NMP.Portal.Enums.NVZFields.SomeFieldsInNVZ ? true : false;
                            }
                        }
                        else
                        {
                            field.WantToApplySns = false;
                        }
                    }

                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                }
                return RedirectToAction("CheckAnswer");
            }


            return RedirectToAction("FieldMeasurements");
        }
        [HttpGet]
        public async Task<IActionResult> FieldMeasurements()
        {
            _logger.LogTrace($"Field Controller : FieldMeasurements() action called");
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
            _logger.LogTrace($"Field Controller : FieldMeasurements() post action called");
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
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }
            return RedirectToAction("NVZField");
        }
        [HttpGet]
        public async Task<IActionResult> NVZField()
        {
            _logger.LogTrace($"Field Controller : NVZField() action called");
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
            _logger.LogTrace($"Field Controller : NVZField() post action called");
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
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }
            return RedirectToAction("ElevationField");
        }

        [HttpGet]
        public async Task<IActionResult> ElevationField()
        {
            _logger.LogTrace($"Field Controller : ElevationField() action called");
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
            _logger.LogTrace($"Field Controller : ElevationField() post action called");
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
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }
            return RedirectToAction("SoilType");
        }
        [HttpGet]
        public async Task<IActionResult> SoilType()
        {
            _logger.LogTrace($"Field Controller : SoilType() action called");
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
                    var country = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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
                _logger.LogTrace($"Field Controller : Exception in SoilType() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("ElevationField");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilType(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : SoilType() post action called");
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
                    var country = field.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
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

                bool isSoilTypeChange = false;
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                {
                    FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                    if (fieldData.SoilTypeID != (int)NMP.Portal.Enums.SoilTypeEngland.DeepClayey &&
                        field.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.DeepClayey)
                    {
                        isSoilTypeChange = true;
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                if (field.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow)
                {
                    return RedirectToAction("SoilOverChalk");
                }
                if (field.IsCheckAnswer && (!isSoilTypeChange))
                {
                    field.IsSoilReleasingClay = false;
                    field.SoilReleasingClay = null;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                    return RedirectToAction("CheckAnswer");
                }

                if (soilType != null && soilType.KReleasingClay)
                {
                    field.IsSoilReleasingClay = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                    return RedirectToAction("SoilReleasingClay");
                }

                field.SoilReleasingClay = null;
                field.IsSoilReleasingClay = false;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
                if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
                {
                    return RedirectToAction("UpdateField");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in SoilType() post action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return View(field);
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
        }

        [HttpGet]
        public IActionResult SoilReleasingClay()
        {
            _logger.LogTrace($"Field Controller : SoilReleasingClay() action called");
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
            _logger.LogTrace($"Field Controller : SoilReleasingClay() post action called");
            if (field.SoilReleasingClay == null)
            {
                ModelState.AddModelError("SoilReleasingClay", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            //if (field.SoilAnalyses.SulphurDeficient == null)
            //{
            //    ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            //}
            field.IsSoilReleasingClay = true;
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();

            soilTypes = await _fieldService.FetchSoilTypes();
            if (soilTypes.Count > 0 && soilTypes.Any())
            {
                var country = field.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var soilTypesList = soilTypes.Where(x => x.CountryId == country).ToList();
                soilTypes = soilTypesList;
            }
            var soilType = soilTypes?.Where(x => x.SoilTypeId == field.SoilTypeID).FirstOrDefault();
            if (!soilType.KReleasingClay)
            {
                field.SoilReleasingClay = false;
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer && (!field.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
        }

        [HttpGet]
        public IActionResult SulphurDeficient()
        {
            _logger.LogTrace($"Field Controller : SulphurDeficient() action called");
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
            _logger.LogTrace($"Field Controller : SulphurDeficient() action called");
            if (field.SoilAnalyses.SulphurDeficient == null)
            {
                ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            //if (field.IsSoilReleasingClay)
            //{
            //    field.IsSoilReleasingClay = false;
            //    field.SoilReleasingClay = null;
            //}
            if (!ModelState.IsValid)
            {
                return View(field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer && (!field.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("SoilDate");
        }

        [HttpGet]
        public async Task<IActionResult> SoilDate()
        {
            _logger.LogTrace($"Field Controller : SoilDateAndPHLevel() action called");
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
        public IActionResult SoilDate(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilDateAndPHLevel() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilAnalyses.Date"))
            {
                var dateError = ModelState["SoilAnalyses.Date"].Errors.Count > 0 ?
                                ModelState["SoilAnalyses.Date"].Errors[0].ErrorMessage.ToString() : null;

                //if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDateSampleTaken)))
                //{
                //    ModelState["SoilAnalyses.Date"].Errors.Clear();
                //    ModelState["SoilAnalyses.Date"].Errors.Add(Resource.MsgEnterTheDateInNumber);
                //}

                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["SoilAnalyses.Date"].Errors.Clear();
                    ModelState["SoilAnalyses.Date"].Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }
            if (model.SoilAnalyses.Date == null)
            {
                ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgEnterADateBeforeContinuing);
            }
            //if (model.SoilAnalyses.PH == null)
            //{
            //    ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgEnterAPHBeforeContinuing);
            //}
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
            if (model.IsCheckAnswer && (!model.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("SoilNutrientValueType");
        }
        [HttpGet]
        public async Task<IActionResult> SoilNutrientValueType()
        {
            _logger.LogTrace($"Field Controller : SoilNutrientValueType() action called");
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
            _logger.LogTrace($"Field Controller : SoilNutrientValueType() post action called");
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
            _logger.LogTrace($"Field Controller : SoilNutrientValue() action called");
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
            _logger.LogTrace($"Field Controller : SoilNutrientValue() post action called");
            Error error = null;
            try
            {
                if (model.IsSoilNutrientValueTypeIndex != null && model.IsSoilNutrientValueTypeIndex.Value)
                {
                    if (!string.IsNullOrEmpty(model.PotassiumIndexValue))
                    {
                        if (int.TryParse(model.PotassiumIndexValue, out int value))
                        {
                            if (value > 9 || value < 0)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgEnterValidValueForNutrientIndex);
                            }
                        }
                        else
                        {
                            if ((model.PotassiumIndexValue.ToString() != Resource.lblTwoMinus) &&
                                                   (model.PotassiumIndexValue.ToString() != Resource.lblTwoPlus))
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgEnterValidValueForNutrientIndex);
                            }
                        }


                    }
                    if (model.SoilAnalyses.PH == null && (string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) &&
                    model.SoilAnalyses.PhosphorusIndex == null && model.SoilAnalyses.MagnesiumIndex == null)
                    {
                        ModelState.AddModelError("CropType", Resource.MsgEnterAtLeastOneValue);
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
                    if (model.SoilAnalyses.PH == null && model.SoilAnalyses.Potassium == null &&
                        model.SoilAnalyses.Phosphorus == null && model.SoilAnalyses.Magnesium == null)
                    {
                        ModelState.AddModelError("CropType", Resource.MsgEnterAtLeastOneValue);
                    }

                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                model.SoilAnalyses.PhosphorusMethodologyID = (int)PhosphorusMethodology.Olsens;

                if (model.SoilAnalyses.Phosphorus != null || model.SoilAnalyses.Potassium != null ||
                    model.SoilAnalyses.Magnesium != null)
                {
                    if (model.IsSoilNutrientValueTypeIndex != null && (!model.IsSoilNutrientValueTypeIndex.Value))
                    {
                        (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
                        if (error == null && nutrients.Count > 0)
                        {
                            int phosphorusId = 1;
                            int potassiumId = 2;
                            int magnesiumId = 3;

                            if (model.SoilAnalyses.Phosphorus != null)
                            {
                                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                                if (phosphorusNutrient != null)
                                {
                                    phosphorusId = phosphorusNutrient.nutrientId;
                                }
                                (string phosphorusIndexValue, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.SoilAnalyses.Phosphorus, (int)PhosphorusMethodology.Olsens);
                                if (!string.IsNullOrWhiteSpace(phosphorusIndexValue) && error == null)
                                {
                                    model.SoilAnalyses.PhosphorusIndex = Convert.ToInt32(phosphorusIndexValue.Trim());

                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                            if (model.SoilAnalyses.Magnesium != null)
                            {
                                var magnesiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                                if (magnesiumNutrient != null)
                                {
                                    magnesiumId = magnesiumNutrient.nutrientId;
                                }
                                (string magnesiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.SoilAnalyses.Magnesium, (int)MagnesiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(magnesiumIndexValue) && error == null)
                                {
                                    model.SoilAnalyses.MagnesiumIndex = Convert.ToInt32(magnesiumIndexValue.Trim());
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                            if (model.SoilAnalyses.Potassium != null)
                            {
                                var potassiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                                if (potassiumNutrient != null)
                                {
                                    potassiumId = potassiumNutrient.nutrientId;
                                }
                                (string potassiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.SoilAnalyses.Potassium, (int)PotassiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(potassiumIndexValue) && error == null)
                                {
                                    model.PotassiumIndexValue = potassiumIndexValue.Trim();
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                        }
                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
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
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("CheckAnswer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in SoilNutrientValue() post action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = string.Concat(error, ex.Message);
                return View(model);
            }
            return RedirectToAction("HasGrassInLastThreeYear");
        }

        [HttpGet]
        public async Task<IActionResult> CropGroups()
        {
            _logger.LogTrace($"Field Controller : CropGroups() action called");
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
                List<CropGroupResponse> cropGroupArables = cropGroups.Where(x => x.CropGroupId != (int)NMP.Portal.Enums.CropGroup.Grass).ToList();
                //_httpContextAccessor.HttpContext.Session.SetObjectAsJson("CropGroupList", cropGroups);
                ViewBag.CropGroupList = cropGroupArables;
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CropGroups() action : {ex.Message}, {ex.StackTrace}");
                //TempData["Error"] = ex.Message;
                if (model.RecentSoilAnalysisQuestion != null && model.RecentSoilAnalysisQuestion.Value == true)
                {
                    ViewBag.Error = ex.Message;
                    return RedirectToAction("SoilNutrientValue");
                }
                else if (model.PreviousGrasses.HasGrassInLastThreeYear != null)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("HasGrassInLastThreeYear");
                }
                else
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("RecentSoilAnalysisQuestion");
                }
                //return RedirectToAction("SNSCalculationMethod");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropGroups(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CropGroups() post action called");
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
            _logger.LogTrace($"Field Controller : CropTypes() action called");
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
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();

                ViewBag.CropTypeList = cropTypeList;
                if (cropTypeList.Count == 1)
                {
                    if (cropTypeList[0].CropTypeId == (int)NMP.Portal.Enums.CropTypes.Other)
                    {
                        model.CropTypeID = cropTypeList[0].CropTypeId;
                        model.CropType = cropTypeList[0].CropType;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                        if (model.IsCheckAnswer)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                        return RedirectToAction("SNSAppliedQuestion");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CropTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropGroups");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropTypes(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CropTypes() post action called");
            if (field.CropTypeID == null)
            {
                ModelState.AddModelError("CropTypeID", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(field.CropGroupId ?? 0);
                var country = field.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
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
            _logger.LogTrace($"Field Controller : SNSAppliedQuestion() action called");
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
            _logger.LogTrace($"Field Controller : SNSAppliedQuestion() action called");
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
            _logger.LogTrace($"Field Controller : CheckAnswer() action called");
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
                model.IsRecentSoilAnalysisQuestionChange = false;
                model.IsCheckAnswer = true;
                if (model.SoilOverChalk != null && model.SoilTypeID != (int)NMP.Portal.Enums.SoilTypeEngland.Shallow)
                {
                    model.SoilOverChalk = null;
                }
                if (model.SoilReleasingClay != null && model.SoilTypeID != (int)NMP.Portal.Enums.SoilTypeEngland.DeepClayey)
                {
                    model.SoilReleasingClay = null;
                    model.IsSoilReleasingClay = false;
                }
                List<CommonResponse> grassManagements = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = grassManagements?.FirstOrDefault(x => x.Id == model.PreviousGrasses.GrassManagementOptionID)?.Name;

                List<CommonResponse> grassTypicalCuts = await _fieldService.GetGrassTypicalCuts();
                ViewBag.GrassTypicalCuts = grassTypicalCuts?.FirstOrDefault(x => x.Id == model.PreviousGrasses.GrassTypicalCutID)?.Name;

                List<CommonResponse> soilNitrogenSupplyItems = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = soilNitrogenSupplyItems?.FirstOrDefault(x => x.Id == model.PreviousGrasses.SoilNitrogenSupplyItemID)?.Name;

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace($"Field Controller : BackCheckAnswer() action called");
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
            if (model.WantToApplySns != null && !model.WantToApplySns.Value)
            {
                return RedirectToAction("SNSAppliedQuestion");
            }
            if (model.PreviousGrasses.HasGrassInLastThreeYear != null && model.PreviousGrasses.HasGrassInLastThreeYear.Value)
            {
                if (model.PreviousGrasses.HasGreaterThan30PercentClover == false)
                {
                    return RedirectToAction("SoilNitrogenSupplyItems");
                }
                else
                {
                    return RedirectToAction("HasGreaterThan30PercentClover");
                }
            }
            return RedirectToAction("SNSAppliedQuestion");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CheckAnswer() post action called");
            if (model.PreviousGrasses.HasGrassInLastThreeYear == false)
            {
                if (!model.CropTypeID.HasValue)
                {
                    ModelState.AddModelError("CropTypeID", Resource.MsgPreviousCropTypeNotSet);
                }
            }

            if (model.RecentSoilAnalysisQuestion.Value)
            {
                if (model.IsSoilReleasingClay && !model.SoilReleasingClay.HasValue)
                {
                    ModelState.AddModelError("SoilReleasingClay", Resource.MsgSoilReleasingClayNotSet);
                }
                if (!model.SoilAnalyses.Date.HasValue)
                {
                    ModelState.AddModelError("SoilAnalyses.Date", Resource.MsgSampleDateNotSet);
                }
                if (!model.SoilAnalyses.SulphurDeficient.HasValue)
                {
                    ModelState.AddModelError("SoilAnalyses.SulphurDeficient", Resource.lblSoilDeficientInSulpurForCheckAnswerNotset);
                }

                if (model.IsSoilNutrientValueTypeIndex.HasValue)
                {

                    if (!model.IsSoilNutrientValueTypeIndex.Value)
                    {
                        if (!model.SoilAnalyses.PH.HasValue && !model.SoilAnalyses.Potassium.HasValue &&
                            !model.SoilAnalyses.Phosphorus.HasValue && !model.SoilAnalyses.Magnesium.HasValue)
                        {
                            if (!model.SoilAnalyses.PH.HasValue)
                            {
                                ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgPhNotSet);
                            }
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
                    else
                    {
                        if (!model.SoilAnalyses.PH.HasValue && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                            !model.SoilAnalyses.MagnesiumIndex.HasValue && !model.SoilAnalyses.PhosphorusIndex.HasValue)
                        {
                            if (!model.SoilAnalyses.PH.HasValue)
                            {
                                ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgPhNotSet);
                            }
                            if (string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgPotassiumIndexNotSet);
                            }
                            if (!model.SoilAnalyses.PhosphorusIndex.HasValue)
                            {
                                ModelState.AddModelError("SoilAnalyses.PhosphorusIndex", Resource.MsgPhosphorusIndexNotSet);
                            }
                            if (!model.SoilAnalyses.MagnesiumIndex.HasValue)
                            {
                                ModelState.AddModelError("SoilAnalyses.MagnesiumIndex", Resource.MsgMagnesiumIndexNotSet);
                            }
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgNutrientValueTypeForCheckAnswereNotSet);
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

            if (model.SoilAnalyses.Potassium != null || model.SoilAnalyses.Phosphorus != null || (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) || model.SoilAnalyses.PhosphorusIndex != null)
            {
                model.PKBalance.PBalance = 0;
                model.PKBalance.KBalance = 0;
                model.PKBalance.Year = model.SoilAnalyses.Date.Value.Year;
            }
            else
            {
                model.PKBalance = null;
            }
            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

            List<PreviousGrass> grass = new List<PreviousGrass>();
            if (model.PreviousGrassYears != null)
            {
                foreach (var year in model.PreviousGrassYears)
                {
                    model.PreviousGrasses.HarvestYear = year;

                    var newGrass = new PreviousGrass
                    {
                        HasGrassInLastThreeYear = model.PreviousGrasses.HasGrassInLastThreeYear,
                        HarvestYear = year,
                        GrassManagementOptionID = model.PreviousGrasses.GrassManagementOptionID,
                        GrassTypicalCutID = model.PreviousGrasses.GrassTypicalCutID,
                        HasGreaterThan30PercentClover = model.PreviousGrasses.HasGreaterThan30PercentClover,
                        SoilNitrogenSupplyItemID = model.PreviousGrasses.SoilNitrogenSupplyItemID,
                        CreatedOn = System.DateTime.Now

                    };

                    grass.Add(newGrass);
                }
                if (model.IsPreviousYearGrass == true)
                {
                    model.CropGroupId = (int)NMP.Portal.Enums.CropGroup.Grass;
                    model.CropTypeID = (int)NMP.Portal.Enums.CropTypes.Grass;
                }

            }
            if (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
            {
                if (model.PotassiumIndexValue == Resource.lblTwoMinus)
                {
                    model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(Resource.lblMinusTwo);
                }
                else if (model.PotassiumIndexValue == Resource.lblTwoPlus)
                {
                    model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(Resource.lblPlusTwo);
                }
                else
                {
                    model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(model.PotassiumIndexValue.Trim());
                }
            }
            //model.SoilAnalyses.PotassiumIndex = Convert.ToInt32(model.PotassiumIndexValue);
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
                    SoilOverChalk = model.SoilOverChalk,
                    IsWithinNVZ = model.IsWithinNVZ,
                    IsAbove300SeaLevel = model.IsAbove300SeaLevel,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId,
                    ModifiedOn = model.ModifiedOn,
                    ModifiedByID = model.ModifiedByID
                },
                SoilAnalysis = (!model.RecentSoilAnalysisQuestion.Value) ? null : new SoilAnalysis
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
                    CurrentCropTypeID = model.CurrentCropTypeId.Value,
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
                            Year=model.LastHarvestYear??0,
                            Confirm=false,
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

                },
                PKBalance = model.PKBalance != null ? model.PKBalance : null,
                PreviousGrasses = model.PreviousGrasses.HasGrassInLastThreeYear == true ? grass : null

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
        public async Task<IActionResult> ManageFarmFields(string id, string? q, string? name, string? isDeleted)
        {
            _logger.LogTrace($"Field Controller : ManageFarmFields() action called");
            FarmFieldsViewModel model = new FarmFieldsViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Success = false;
            }
            if (!string.IsNullOrWhiteSpace(isDeleted))
            {
                ViewBag.FieldName = _fieldDataProtector.Unprotect(name);
                ViewBag.IsDeleted = true;
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
                if (string.IsNullOrWhiteSpace(isDeleted))
                {
                    if (name != null)
                    {
                        model.FieldName = _farmDataProtector.Unprotect(name);
                    }
                }

                ViewBag.FieldsList = model.Fields;
                model.EncryptedFarmId = id;
            }
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                _httpContextAccessor.HttpContext?.Session.Remove("FieldData");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManageFarmFields(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : ManageFarmFields() post action called");
            return RedirectToAction("ManageFarmFields");
        }

        [HttpGet]
        public async Task<IActionResult> FieldSoilAnalysisDetail(string id, string farmId, string? q, string? r, string? s)//id encryptedFieldId,farmID=EncryptedFarmID,q=success,r=FiedlOrSoilAnalysis,s=soilUpdateOrSave
        {
            _logger.LogTrace($"Field Controller : FieldSoilAnalysisDetail() action called");
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
            model.SoilTypeID = field.SoilTypeID;
            model.EncryptedFieldId = id;
            model.ID = fieldId;
            model.isEnglishRules = farm.EnglishRules;
            model.SoilOverChalk = field.SoilOverChalk;

            model.EncryptedFarmId = farmId;
            model.FarmName = farm.Name;
            List<SoilAnalysisResponse> soilAnalysisResponse = (await _fieldService.FetchSoilAnalysisByFieldId(fieldId, Resource.lblFalse)).OrderByDescending(x => x.CreatedOn).ToList();
            if (soilAnalysisResponse != null && soilAnalysisResponse.Count > 0)
            {
                soilAnalysisResponse.ForEach(m => m.EncryptedSoilAnalysisId = _fieldDataProtector.Protect(m.ID.ToString()));
                ViewBag.SoilAnalysisList = soilAnalysisResponse;
            }
            if (!string.IsNullOrWhiteSpace(q))
            {

                if (!string.IsNullOrWhiteSpace(r))
                {
                    string statusFor = _fieldDataProtector.Unprotect(r);
                    if (!string.IsNullOrWhiteSpace(statusFor))
                    {
                        if (statusFor == Resource.lblField)
                        {
                            ViewBag.Success = Resource.lblTrue;
                            ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveUpdated, model.Name);
                        }
                        else if (statusFor == Resource.lblSoilAnalysis)
                        {
                            if (_soilAnalysisDataProtector.Unprotect(q) == Resource.lblFalse)
                            {
                                ViewBag.Success = Resource.lblFalse;
                                ViewBag.Error = Resource.MsgSoilAnalysisCouldNotAdded;                                
                            }
                            else
                            {
                                ViewBag.Success = Resource.lblTrue;
                                if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                {
                                    ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveAddedANewSoilAnalysisForFieldName, model.Name);
                                }
                                else
                                {
                                    ViewBag.SuccessMsgContent = string.Format(Resource.lblYouHaveUpdatedASoilAnalysisForFieldName, model.Name);
                                }
                                if (soilAnalysisResponse.Count > 0)
                                {
                                    List<Crop> crop = (await _iCropService.FetchCropsByFieldId(soilAnalysisResponse.FirstOrDefault().FieldID.Value)).ToList();
                                    if (crop != null && crop.Count > 0)
                                    {
                                        bool anyPlan = crop.Any(x => x.Year >= (soilAnalysisResponse.FirstOrDefault()?.Year ?? 0));
                                        if (anyPlan)
                                        {
                                            int cropYear = crop.FirstOrDefault(x => x.Year >= soilAnalysisResponse.FirstOrDefault().Year).Year;
                                            if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                            {
                                                ViewBag.SuccessMsgAdditionalContent = string.Format(Resource.lblAddSoilAnalysisSuccessMsg, cropYear);
                                            }
                                            else
                                            {
                                                ViewBag.SuccessMsgAdditionalContent = string.Format(Resource.lblThisMayChangeYourNutrientRecommendations, soilAnalysisResponse.FirstOrDefault().Year);
                                            }
                                            
                                            ViewBag.CropYear = _farmDataProtector.Protect(cropYear.ToString());
                                            if (!string.IsNullOrWhiteSpace(s) && _soilAnalysisDataProtector.Unprotect(s) == Resource.lblAdd)
                                            {
                                                ViewBag.SuccessMsgAdditionalContentSecondForAdd = string.Format(Resource.lblYearCropPlan, cropYear);
                                            }
                                            else
                                            {
                                                ViewBag.SuccessMsgAdditionalContentSecondForUpdate = string.Format(Resource.lblCropPlan);
                                            }
                                            ViewBag.SuccessMsgAdditionalContentThird = Resource.lblToSeeItsRecommendations;
                                        }
                                    }

                                }
                            }
                        }

                    }
                }

            }
            else
            {
                ViewBag.Success = null;
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SampleForSoilMineralNitrogen()
        {
            _logger.LogTrace($"Field Controller : SampleForSoilMineralNitrogen() action called");
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
            _logger.LogTrace($"Field Controller : SampleForSoilMineralNitrogen() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SampleForSoilMineralNitrogen"))
            {
                var dateError = ModelState["SampleForSoilMineralNitrogen"].Errors.Count > 0 ?
                                ModelState["SampleForSoilMineralNitrogen"].Errors[0].ErrorMessage.ToString() : null;

                //if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.MsgSampleForSoilMineralNitrogenForError)))
                //{
                //    ModelState["SampleForSoilMineralNitrogen"].Errors.Clear();
                //    ModelState["SampleForSoilMineralNitrogen"].Errors.Add(Resource.MsgDateEnteredIsNotValid);
                //}
                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["SampleForSoilMineralNitrogen"].Errors.Clear();
                    ModelState["SampleForSoilMineralNitrogen"].Errors.Add(Resource.MsgTheDateMustInclude);
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
            _logger.LogTrace($"Field Controller : CurrentCropGroups() post action called");
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
                _logger.LogTrace($"Field Controller : Exception in CurrentCropGroups() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("SampleForSoilMineralNitrogen");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CurrentCropGroups(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CurrentCropGroups() post action called");
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
            _logger.LogTrace($"Field Controller : CurrentCropTypes() action called");
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
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                var cropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();

                ViewBag.CropTypeList = cropTypeList;
                if (cropTypeList.Count == 1)
                {
                    if (cropTypeList[0].CropTypeId == (int)NMP.Portal.Enums.CropTypes.Other)
                    {
                        model.CurrentCropTypeId = cropTypeList[0].CropTypeId;
                        model.CurrentCropType = cropTypeList[0].CropType;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in CurrentCropTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropGroups");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CurrentCropTypes(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CurrentCropTypes() post action called");
            if (model.CurrentCropTypeId == null)
            {
                ModelState.AddModelError("CurrentCropTypeId", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
            }
            if (!ModelState.IsValid)
            {
                List<CropTypeResponse> cropTypes = new List<CropTypeResponse>();
                cropTypes = await _fieldService.FetchCropTypes(model.CurrentCropGroupId ?? 0);
                var country = model.isEnglishRules ? (int)NMP.Portal.Enums.RB209Country.England : (int)NMP.Portal.Enums.RB209Country.Scotland;
                ViewBag.CropTypeList = cropTypes.Where(x => x.CountryId == country || x.CountryId == (int)NMP.Portal.Enums.RB209Country.All).ToList();
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
            _logger.LogTrace($"Field Controller : SoilMineralNitrogenAnalysisResults() action called");
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
                _logger.LogTrace($"Field Controller : Exception in SoilMineralNitrogenAnalysisResults() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropTypes");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilMineralNitrogenAnalysisResults() post action called");
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
                    ModelState.AddModelError("SoilMineralNitrogenAt030CM", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblKilogramsOfSoilMineralNitrogenAt030CM));
                }
            }
            if (model.SoilMineralNitrogenAt3060CM != null)
            {
                if (model.SoilMineralNitrogenAt3060CM < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt3060CM", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblKilogramsOfSoilMineralNitrogenAt3060CM));
                }
            }
            if (model.SoilMineralNitrogenAt6090CM != null)
            {
                if (model.SoilMineralNitrogenAt6090CM < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt6090CM", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblKilogramsOfSoilMineralNitrogenAt6090CM));
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
            _logger.LogTrace($"Field Controller : SampleDepth() action called");
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
                _logger.LogTrace($"Field Controller : Exception in SampleDepth() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropTypes");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleDepth(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SampleDepth() post action called");
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
                    ModelState.AddModelError("SampleDepth", string.Format(Resource.lblValueMustBeGreaterThanZero, Resource.lblSampleDepth));
                }
            }
            if (model.SoilMineralNitrogen != null)
            {
                if (model.SoilMineralNitrogen < 0)
                {
                    ModelState.AddModelError("SoilMineralNitrogen", string.Format(Resource.lblValueMustBeGreaterThanZero, Resource.lblSoilMineralNitrogen));
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
            _logger.LogTrace($"Field Controller : CalculateNitrogenInCurrentCropQuestion() action called");
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
                _logger.LogTrace($"Field Controller : Exception in CalculateNitrogenInCurrentCropQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CalculateNitrogenInCurrentCropQuestion() post action called");
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
            _logger.LogTrace($"Field Controller : NumberOfShoots() action called");
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
                _logger.LogTrace($"Field Controller : Exception in NumberOfShoots() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NumberOfShoots(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : NumberOfShoots() post action called");
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
            _logger.LogTrace($"Field Controller : GreenAreaIndexOrCropHeightQuestion() action called");
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
                _logger.LogTrace($"Field Controller : Exception in GreenAreaIndexOrCropHeightQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GreenAreaIndexOrCropHeightQuestion(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GreenAreaIndexOrCropHeightQuestion() post action called");
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
            _logger.LogTrace($"Field Controller : BackActionForCalculateNitrogenCropQuestion() action called");
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
            _logger.LogTrace($"Field Controller : CropHeight() action called");
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
                _logger.LogTrace($"Field Controller : Exception in CropHeight() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropHeight(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : CropHeight() post action called");
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
            _logger.LogTrace($"Field Controller : GreenAreaIndex() action called");
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
                _logger.LogTrace($"Field Controller : Exception in GreenAreaIndex() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GreenAreaIndex(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GreenAreaIndex() post action called");
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
            _logger.LogTrace($"Field Controller : EstimateOfNitrogenMineralisationQuestion() action called");
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
                _logger.LogTrace($"Field Controller : Exception in EstimateOfNitrogenMineralisationQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : EstimateOfNitrogenMineralisationQuestion() action called");
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
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyIndex() action called");
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
                if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals)
                {
                    if (model.SoilOrganicMatter != null)
                    {
                        model.AdjustmentValue = null;
                    }
                    if (model.SoilOrganicMatter == null && model.AdjustmentValue == null)
                    {
                        model.AdjustmentValue = 0;
                    }
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
                            ShootNumber = model.NumberOfShoots > 0 ? model.NumberOfShoots : 0,
                            GreenAreaIndex = model.GreenAreaIndex > 0 ? model.GreenAreaIndex : null,
                            CropHeight = model.CropHeight > 0 ? model.CropHeight : null
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };

                }
                else if (model.GreenAreaIndexOrCropHeight == (int)NMP.Portal.Enums.GreenAreaIndexOrCropHeight.CropHeight && snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                {
                    model.GreenAreaIndex = null;
                    if (model.SoilOrganicMatter != null)
                    {
                        model.AdjustmentValue = null;
                    }
                    if (model.SoilOrganicMatter == null && model.AdjustmentValue == null)
                    {
                        model.AdjustmentValue = 0;
                    }
                    if (model.CropHeight != null)
                    {
                        model.GreenAreaIndex = null;
                    }
                    if (model.CropHeight == null && model.GreenAreaIndex == null)
                    {
                        model.GreenAreaIndex = 0;
                    }
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
                            GreenAreaIndex = model.GreenAreaIndex,
                            CropHeight = model.CropHeight > 0 ? model.CropHeight : null
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };
                }
                else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
                {
                    model.CropHeight = null;
                    if (model.SoilOrganicMatter != null)
                    {
                        model.AdjustmentValue = null;
                    }
                    if (model.SoilOrganicMatter == null && model.AdjustmentValue == null)
                    {
                        model.AdjustmentValue = 0;
                    }
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        //SeasonId = model.SeasonId == 0 ? 1 : model.SeasonId,
                        Step1ArablePotato = new Step1ArablePotato
                        {
                            Depth0To30Cm = model.SoilMineralNitrogenAt030CM,
                            Depth30To60Cm = model.SoilMineralNitrogenAt3060CM,
                            Depth60To90Cm = model.SoilMineralNitrogenAt6090CM
                        },
                        Step2 = new Step2
                        {
                            ShootNumber = model.NumberOfShoots > 0 ? model.NumberOfShoots : null,
                            GreenAreaIndex = model.GreenAreaIndex > 0 ? model.GreenAreaIndex : 0,
                            CropHeight = model.CropHeight > 0 ? model.CropHeight : null
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };
                }
                else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
                {
                    if (model.SoilOrganicMatter != null)
                    {
                        model.AdjustmentValue = null;
                    }
                    if (model.SoilOrganicMatter == null && model.AdjustmentValue == null)
                    {
                        model.AdjustmentValue = 0;
                    }
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        //SeasonId = 1,
                        Step1ArablePotato = new Step1ArablePotato
                        {
                            Depth0To30Cm = model.SoilMineralNitrogenAt030CM,
                            Depth30To60Cm = model.SoilMineralNitrogenAt3060CM,
                            Depth60To90Cm = model.SoilMineralNitrogenAt6090CM
                        },
                        Step3 = new Step3
                        {
                            Adjustment = model.AdjustmentValue,
                            OrganicMatterPercentage = model.SoilOrganicMatter > 0 ? model.SoilOrganicMatter : null
                        }
                    };

                }
                else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
                {
                    postMeasurementData = new MeasurementData
                    {
                        CropTypeId = model.CurrentCropTypeId ?? 0,
                        //SeasonId = 1,
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
                //if (postMeasurementData.CropTypeId !=null)
                //{
                (SnsResponse snsResponse, Error error) = await _fieldService.FetchSNSIndexByMeasurementMethodAsync(postMeasurementData);
                if (error.Message == null)
                {
                    model.SnsIndex = snsResponse.SnsIndex;
                    model.SnsValue = snsResponse.SnsValue;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                }
                //}

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in SoilNitrogenSupplyIndex() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNitrogenSupplyIndex(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyIndex() post action called");
            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter()
        {
            _logger.LogTrace($"Field Controller : IsBasedOnSoilOrganicMatter() action called");
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
                _logger.LogTrace($"Field Controller : Exception in IsBasedOnSoilOrganicMatter() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }
            return View("CalculateSoilNitrogenMineralisation", model);
        }

        [HttpPost]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : IsBasedOnSoilOrganicMatter() post action called");
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
                _logger.LogTrace($"Field Controller : Exception in IsBasedOnSoilOrganicMatter() post action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
        }
        [HttpGet]
        public async Task<IActionResult> AdjustmentValue()
        {
            _logger.LogTrace($"Field Controller : AdjustmentValue() action called");
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
                _logger.LogTrace($"Field Controller : Exception in AdjustmentValue() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustmentValue(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : AdjustmentValue() post action called");
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
            _logger.LogTrace($"Field Controller : SoilOrganicMatter() action called");
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
                _logger.LogTrace($"Field Controller : Exception in SoilOrganicMatter() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilOrganicMatter(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilOrganicMatter() post action called");
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
            _logger.LogTrace($"Field Controller : BackActionForEstimateOfNitrogenMineralisationQuestion() action called");
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
            _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() action called");
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
                _logger.LogTrace($"Field Controller : Exception in RecentSoilAnalysisQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilType");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecentSoilAnalysisQuestion(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : RecentSoilAnalysisQuestion() post action called");
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
                if (model.IsCheckAnswer)
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
                    {
                        FieldViewModel fieldData = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
                        if (fieldData.RecentSoilAnalysisQuestion != model.RecentSoilAnalysisQuestion)
                        {
                            model.IsRecentSoilAnalysisQuestionChange = true;
                        }
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);

                if (model.RecentSoilAnalysisQuestion.Value)
                {

                    return RedirectToAction("SulphurDeficient");
                }
                else
                {
                    model.SoilAnalyses.SulphurDeficient = null;
                    model.SoilAnalyses.Date = null;
                    model.SoilAnalyses.PH = null;
                    model.SoilAnalyses.Phosphorus = null;
                    model.SoilAnalyses.Magnesium = null;
                    model.SoilAnalyses.Potassium = null;
                    model.SoilAnalyses.PotassiumIndex = null;
                    model.SoilAnalyses.MagnesiumIndex = null;
                    model.SoilAnalyses.PhosphorusIndex = null;
                    model.IsSoilNutrientValueTypeIndex = null;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                    if (model.IsCheckAnswer)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    return RedirectToAction("HasGrassInLastThreeYear");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Field Controller : Exception in RecentSoilAnalysisQuestion() post action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SoilOverChalk()
        {
            _logger.LogTrace($"Field Controller : SoilOverChalk() action called");
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
        public async Task<IActionResult> SoilOverChalk(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : SoilOverChalk() post action called");
            if (field.SoilOverChalk == null)
            {
                ModelState.AddModelError("SoilOverChalk", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(field);
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer && (!field.IsRecentSoilAnalysisQuestionChange))
            {
                return RedirectToAction("CheckAnswer");
            }
            if (!string.IsNullOrWhiteSpace(field.EncryptedIsUpdate))
            {
                return RedirectToAction("UpdateField");
            }
            return RedirectToAction("RecentSoilAnalysisQuestion");
        }



        [HttpGet]
        public async Task<IActionResult> UpdateField(string? id, string? farmId)
        {
            _logger.LogTrace($"Field Controller : UpdateField() action called");
            FieldViewModel model = new FieldViewModel();

            try
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(farmId)));
                    int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(id));
                    var field = await _fieldService.FetchFieldByFieldId(fieldId);
                    model.Name = field.Name;
                    model.TotalArea = field.TotalArea ?? 0;
                    model.CroppedArea = field.CroppedArea ?? 0;
                    model.ManureNonSpreadingArea = field.ManureNonSpreadingArea ?? 0;
                    model.SoilReleasingClay = field.SoilReleasingClay ?? false;
                    model.IsWithinNVZ = field.IsWithinNVZ ?? false;
                    model.IsAbove300SeaLevel = field.IsAbove300SeaLevel ?? false;
                    var soilType = await _fieldService.FetchSoilTypeById(field.SoilTypeID.Value);
                    model.SoilType = !string.IsNullOrWhiteSpace(soilType) ? soilType : string.Empty;
                    model.SoilTypeID = field.SoilTypeID;
                    model.EncryptedFieldId = id;
                    model.ID = fieldId;
                    model.isEnglishRules = farm.EnglishRules;
                    model.SoilOverChalk = field.SoilOverChalk;

                    model.EncryptedFarmId = farmId;
                    model.FarmName = farm.Name;
                    //model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(farmId));

                    bool isUpdateField = true;
                    model.EncryptedIsUpdate = _fieldDataProtector.Protect(isUpdateField.ToString());
                    if (model.SoilOverChalk != null && model.SoilTypeID != (int)NMP.Portal.Enums.SoilTypeEngland.Shallow)
                    {
                        model.SoilOverChalk = null;
                    }
                    if (model.SoilReleasingClay != null && model.SoilTypeID != (int)NMP.Portal.Enums.SoilTypeEngland.DeepClayey)
                    {
                        model.SoilReleasingClay = null;
                        model.IsSoilReleasingClay = false;
                    }
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
                }
                else
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

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
            return View(model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : UpdateField() post action called");
            try
            {
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);

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
                        SoilOverChalk = model.SoilOverChalk,
                        IsWithinNVZ = model.IsWithinNVZ,
                        IsAbove300SeaLevel = model.IsAbove300SeaLevel,
                        IsActive = true,
                        CreatedOn = model.CreatedOn,
                        CreatedByID = model.CreatedByID,
                        ModifiedOn = DateTime.Now,
                        ModifiedByID = userId
                    }
                };
                int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFieldId));
                (Field fieldResponse, Error error1) = await _fieldService.UpdateFieldAsync(fieldData, fieldId);
                if (error1.Message == null && fieldResponse != null)
                {
                    string success = _farmDataProtector.Protect(Resource.lblTrue);
                    string fieldName = _farmDataProtector.Protect(fieldResponse.Name);
                    _httpContextAccessor.HttpContext?.Session.Remove("FieldData");

                    return RedirectToAction("FieldSoilAnalysisDetail", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId, q = success, r = _fieldDataProtector.Protect(Resource.lblField) });
                }
                else
                {
                    TempData["UpdateFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                    return RedirectToAction("UpdateField");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("UpdateField");
            }


        }

        [HttpGet]
        public async Task<IActionResult> FieldRemove()
        {
            _logger.LogTrace($"Field Controller : FieldRemove() action called");
            FieldViewModel? model = new FieldViewModel();
            if (HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = HttpContext.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            else
            {
                return RedirectToAction("ManageFarmFields", "Farm");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FieldRemove(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : FieldRemove() post action called");
            if (field.FieldRemove == null)
            {
                ModelState.AddModelError("FieldRemove", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("FieldRemove", field);
            }
            if (!field.FieldRemove.Value)
            {
                return RedirectToAction("FieldSoilAnalysisDetail", new { id = field.EncryptedFieldId, farmId = field.EncryptedFarmId });
            }
            else
            {
                int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(field.EncryptedFieldId));
                (string message, Error error) = await _fieldService.DeleteFieldByIdAsync(fieldId);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.DeleteFieldError = error.Message;
                    return View(field);
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    string isDeleted = _fieldDataProtector.Protect("true");
                    string name = _fieldDataProtector.Protect(field.Name);
                    //int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(field.EncryptedFarmId));
                    HttpContext.Session.Remove("FieldData");

                    return RedirectToAction("ManageFarmFields", new { id = field.EncryptedFarmId, name = name, isDeleted = isDeleted });
                }
            }
            return View(field);

        }

        [HttpGet]
        public IActionResult CopyExistingField(string q)
        {
            _logger.LogTrace($"Field Controller : CopyExistingField() action called");
            FieldViewModel model = new FieldViewModel();
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
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CopyExistingField(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CopyExistingField() post action called");
            if (field.CopyExistingField == null)
            {
                ModelState.AddModelError("CopyExistingField", Resource.MsgSelectAnOptionBeforeContinuing);
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
            if (field.CopyExistingField != null && !(field.CopyExistingField.Value))
            {
                return RedirectToAction("AddField", new { q = field.EncryptedFarmId });
            }
            return RedirectToAction("CopyFields");
        }
        [HttpGet]
        public async Task<IActionResult> CopyFields()
        {
            _logger.LogTrace($"Field Controller : CopyFields() action called");
            FieldViewModel model = new FieldViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("FieldData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<FieldViewModel>("FieldData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (Error error, List<Field> fieldList) = await _fieldService.FetchFieldByFarmId(model.FarmID, Resource.lblTrue);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                ViewBag.FieldList = fieldList;
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyFields(FieldViewModel field)
        {
            _logger.LogTrace($"Field Controller : CopyFields() post action called");
            if (field.ID == null)
            {
                ModelState.AddModelError("ID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (Error error, List<Field> fieldList) = await _fieldService.FetchFieldByFarmId(field.FarmID, Resource.lblTrue);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    ViewBag.FieldList = fieldList;
                }
                return View("CopyFields", field);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", field);
            if (field.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            return RedirectToAction("AddField", new { q = field.EncryptedFarmId });
        }
        //Grass journey

        [HttpGet]
        public async Task<IActionResult> HasGrassInLastThreeYear()
        {
            _logger.LogTrace($"Field Controller : HasGrassInLastThreeYear() action called");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGrassInLastThreeYear(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : HasGrassInLastThreeYear() post action called");
            if (model.PreviousGrasses.HasGrassInLastThreeYear == null)
            {
                ModelState.AddModelError("PreviousGrasses.HasGrassInLastThreeYear", Resource.MsgSelectAnOptionBeforeContinuing);
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
            if (model.PreviousGrasses.HasGrassInLastThreeYear.Value)
            {

                return RedirectToAction("GrassLastThreeHarvestYear");
            }
            else
            {
                model.PreviousGrasses.HarvestYear = null;
                model.PreviousGrasses.GrassManagementOptionID = null;
                model.PreviousGrasses.GrassTypicalCutID = null;
                model.PreviousGrasses.HasGreaterThan30PercentClover = null;
                model.PreviousGrasses.SoilNitrogenSupplyItemID = null;
                model.PreviousGrassYears = null;
                model.IsPreviousYearGrass = null;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("FieldData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("CropGroups");
            }

        }

        [HttpGet]
        public async Task<IActionResult> GrassLastThreeHarvestYear()
        {
            _logger.LogTrace($"Field Controller : GrassLastThreeHarvestYear() action called");
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

            List<int> previousYears = new List<int>();
            int lastHarvestYear = model.LastHarvestYear ?? 0;
            previousYears.Add(lastHarvestYear);
            previousYears.Add(lastHarvestYear - 1);
            previousYears.Add(lastHarvestYear - 2);
            ViewBag.PreviousGrassesYear = previousYears;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrassLastThreeHarvestYear(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GrassLastThreeHarvestYear() post action called");

            if (model.PreviousGrassYears == null)
            {
                ModelState.AddModelError("PreviousGrassYears", Resource.lblSelectAtLeastOneYearBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<int> previousYears = new List<int>();
                int lastHarvestYear = model.LastHarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                ViewBag.PreviousGrassesYear = previousYears;
                return View(model);
            }
            if (model.PreviousGrassYears?.Count == 1 && model.PreviousGrassYears[0] == 0)
            {
                List<int> previousYears = new List<int>();
                int lastHarvestYear = model.LastHarvestYear ?? 0;
                previousYears.Add(lastHarvestYear);
                previousYears.Add(lastHarvestYear - 1);
                previousYears.Add(lastHarvestYear - 2);
                model.PreviousGrassYears = previousYears;
            }
            model.IsPreviousYearGrass = (model.PreviousGrassYears != null && model.PreviousGrassYears.Contains(System.DateTime.Now.Year - 1)) ? true : false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("GrassManagementOptions");
        }

        [HttpGet]
        public async Task<IActionResult> GrassManagementOptions()
        {
            _logger.LogTrace($"Field Controller : GrassManagementOptions() action called");
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
            List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
            ViewBag.GrassManagementOptions = commonResponses;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassManagementOptions(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GrassManagementOptions() post action called");

            if (model.PreviousGrasses.GrassManagementOptionID == null)
            {
                ModelState.AddModelError("PreviousGrasses.GrassManagementOptionID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetGrassManagementOptions();
                ViewBag.GrassManagementOptions = commonResponses;
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.PreviousGrasses.GrassManagementOptionID == (int)NMP.Portal.Enums.GrassManagementOption.GrazedOnly)
            {
                return RedirectToAction("HasGreaterThan30PercentClover");
            }
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("GrassTypicalCuts");
        }

        [HttpGet]
        public async Task<IActionResult> GrassTypicalCuts()
        {
            _logger.LogTrace($"Field Controller : GrassTypicalCuts() action called");
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
            List<CommonResponse> commonResponses = await _fieldService.GetGrassTypicalCuts();
            ViewBag.GrassTypicalCuts = commonResponses;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrassTypicalCuts(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : GrassTypicalCuts() post action called");

            if (model.PreviousGrasses.GrassTypicalCutID == null)
            {
                ModelState.AddModelError("PreviousGrasses.GrassTypicalCutID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetGrassTypicalCuts();
                ViewBag.GrassTypicalCuts = commonResponses;
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }

            return RedirectToAction("HasGreaterThan30PercentClover");
        }

        [HttpGet]
        public async Task<IActionResult> HasGreaterThan30PercentClover()
        {
            _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() action called");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HasGreaterThan30PercentClover(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : HasGreaterThan30PercentClover() post action called");
            if (model.PreviousGrasses.HasGreaterThan30PercentClover == null)
            {
                ModelState.AddModelError("PreviousGrasses.HasGreaterThan30PercentClover", Resource.MsgSelectAnOptionBeforeContinuing);
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
            if (model.PreviousGrasses.HasGreaterThan30PercentClover.Value)
            {
                if (model.IsPreviousYearGrass == false)
                {
                    return RedirectToAction("CropGroups");
                }
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                return RedirectToAction("SoilNitrogenSupplyItems");
            }

        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyItems()
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() action called");
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
            List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
            ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderByDescending(x => x.Id);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNitrogenSupplyItems(FieldViewModel model)
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyItems() post action called");

            if (model.PreviousGrasses.SoilNitrogenSupplyItemID == null)
            {
                ModelState.AddModelError("PreviousGrasses.SoilNitrogenSupplyItemID", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                List<CommonResponse> commonResponses = await _fieldService.GetSoilNitrogenSupplyItems();
                ViewBag.SoilNitrogenSupplyItems = commonResponses.OrderByDescending(x => x.Id);
                return View(model);
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("FieldData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            if (model.IsPreviousYearGrass == false)
            {
                return RedirectToAction("CropGroups");
            }

            return RedirectToAction("CheckAnswer");
        }
    }
}
