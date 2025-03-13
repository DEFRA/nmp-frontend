using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Globalization;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NMP.Portal.Controllers
{
    public class SnsAnalysisController : Controller
    {
        private readonly ILogger<SnsAnalysisController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IDataProtector _cropDataProtector;
        private readonly IFarmService _farmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFieldService _fieldService;
        private readonly ISoilService _soilService;
        private readonly IOrganicManureService _organicManureService;
        private readonly ISoilAnalysisService _soilAnalysisService;
        private readonly IPKBalanceService _pKBalanceService;
        private readonly ICropService _cropService;
        private readonly ISnsAnalysisService _snsAnalysisService;

        public SnsAnalysisController(ILogger<SnsAnalysisController> logger, IDataProtectionProvider dataProtectionProvider,
             IFarmService farmService, IHttpContextAccessor httpContextAccessor, ISoilService soilService,
             IFieldService fieldService, IOrganicManureService organicManureService, ISoilAnalysisService soilAnalysisService, IPKBalanceService pKBalanceService, ICropService cropService, ISnsAnalysisService snsAnalysisService)
        {
            _logger = logger;
            _farmService = farmService;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
            _fieldService = fieldService;
            _soilService = soilService;
            _organicManureService = organicManureService;
            _soilAnalysisService = soilAnalysisService;
            _pKBalanceService = pKBalanceService;
            _cropService = cropService;
            _snsAnalysisService = snsAnalysisService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace($"Sns Controller : Index() action called");
            return View();
        }
        public IActionResult SnsAnalysisCancel(string q, string r, string? s)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SnsAnalysisCancel action called");
            _httpContextAccessor.HttpContext?.Session.Remove("SnsData");
            return RedirectToAction("Recommendations", "Crop", new { q = q,r=r,s=s });
        }

        [HttpGet]
        public async Task<IActionResult> SoilSampleDate(string? q, string? r, string? s, string? c, string? f)   //q=farmId,r=fieldId,s=harvestYear, c=cropId (ID from crop table),f=fieldName
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilSampleDate() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedFarmId))
            {
                model.EncryptedFarmId = q ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedFieldId))
            {
                model.EncryptedFieldId = r ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedHarvestYear))
            {
                model.EncryptedHarvestYear = s ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(model.EncryptedCropId))
            {
                model.EncryptedCropId = c ?? string.Empty;
            }
            if(!string.IsNullOrWhiteSpace(c))
            {
                model.CropId = Convert.ToInt32(_cropDataProtector.Unprotect(c));
                (Crop crop, NMP.Portal.ServiceResponses.Error error) = await _cropService.FetchCropById(model.CropId);
                model.CropTypeId = crop.CropTypeID;
            }
            if (!string.IsNullOrWhiteSpace(f))
            {
                model.EncryptedFieldName = f ?? string.Empty;
                model.FieldName = _cropDataProtector.Unprotect(f);
            }
            
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilSampleDate(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilSampleDate() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SampleDate"))
            {
                var dateError = ModelState["SampleDate"].Errors.Count > 0 ?
                                ModelState["SampleDate"].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["SampleDate"].Errors.Clear();
                    ModelState["SampleDate"].Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }

            if (model.SampleDate == null)
            {
                ModelState.AddModelError("SampleDate", Resource.MsgdateMustBeFilledBeforeProceeding);
            }
            if (DateTime.TryParseExact(model.SampleDate.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("SampleDate", Resource.MsgDateEnteredIsNotValid);
            }

            if (model.SampleDate != null)
            {
                if (model.SampleDate.Value.Date > DateTime.Now)
                {
                    ModelState.AddModelError("SampleDate", Resource.MsgDateShouldNotBeInTheFuture);
                }
                if (model.SampleDate.Value.Date.Year < 1601)
                {
                    ModelState.AddModelError("SampleDate", Resource.MsgDateEnteredIsNotValid);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel snsViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                    if (snsViewModel.SampleDate == model.SampleDate)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);

            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            model.SnsCategoryId = snsCategoryId;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);
            if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Vegetables)
            {
                return RedirectToAction("SampleDepth");
            }

            return RedirectToAction("SoilMineralNitrogenAnalysisResults");
        }

        [HttpGet]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilMineralNitrogenAnalysisResults() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in SoilMineralNitrogenAnalysisResults() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilSampleDate");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilMineralNitrogenAnalysisResults() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            

            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId??0);
            model.SnsCategoryId = snsCategoryId;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);
            if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterCereals || snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.WinterOilseedRape)
            {
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            //else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Fruit)
            //{
            //    return RedirectToAction("CheckAnswer");
            //}
            else if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }

            return RedirectToAction("SoilMineralNitrogenAnalysisResults");
        }

        [HttpGet]
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : EstimateOfNitrogenMineralisationQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : EstimateOfNitrogenMineralisationQuestion() action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
            if (model.IsEstimateOfNitrogenMineralisation == true)
            {
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            else
            {
                model.AdjustmentValue = null;
                model.SoilOrganicMatter = null;
                model.IsBasedOnSoilOrganicMatter = null;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
                return RedirectToAction("SoilNitrogenSupplyIndex");
            }

        }

        [HttpGet]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter()
        {
            _logger.LogTrace($"SnsAnalysis Controller : IsBasedOnSoilOrganicMatter() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }
            return View("CalculateSoilNitrogenMineralisation", model);
        }

        [HttpPost]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : IsBasedOnSoilOrganicMatter() post action called");
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
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                    {
                        SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
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
                _logger.LogTrace($"SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() post action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyIndex()
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyIndex() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                //sns logic
                var postMeasurementData = new MeasurementData();
                int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
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
                        CropTypeId = model.CropTypeId ?? 0,
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
                        CropTypeId = model.CropTypeId ?? 0,
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
                        CropTypeId = model.CropTypeId ?? 0,
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
                        CropTypeId = model.CropTypeId ?? 0,
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
                        CropTypeId = model.CropTypeId ?? 0,
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
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
                }
                //}

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in SoilNitrogenSupplyIndex() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNitrogenSupplyIndex(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilNitrogenSupplyIndex() post action called");
            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> SoilOrganicMatter()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilOrganicMatter() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> SoilOrganicMatter(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilOrganicMatter() post action called");
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
            if (model.SoilOrganicMatter != null && (model.SoilOrganicMatter < 0 || model.SoilOrganicMatter > 100))
            {
                ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterValueInBetween, Resource.lblPercentageLable.ToLower(), 0, 100));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.AdjustmentValue = null;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);


            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        [HttpGet]
        public async Task<IActionResult> AdjustmentValue()
        {
            _logger.LogTrace($"SnsAnalysis Controller : AdjustmentValue() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> AdjustmentValue(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : AdjustmentValue() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);


            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        [HttpGet]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CalculateNitrogenInCurrentCropQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : CalculateNitrogenInCurrentCropQuestion() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);

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
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);
                return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
            }

            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> NumberOfShoots()
        {
            _logger.LogTrace($"SnsAnalysis Controller : NumberOfShoots() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> NumberOfShoots(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : NumberOfShoots() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> GreenAreaIndexOrCropHeightQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndexOrCropHeightQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
                _logger.LogTrace($"SnsAnalysis Controller : Exception in GreenAreaIndexOrCropHeightQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GreenAreaIndexOrCropHeightQuestion(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndexOrCropHeightQuestion() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);

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
        public async Task<IActionResult> CropHeight()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CropHeight() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
        public async Task<IActionResult> CropHeight(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : CropHeight() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> GreenAreaIndex()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndex() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                //seasons = await _fieldService.FetchSeasons();
                //ViewBag.SeasonList = seasons;
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in GreenAreaIndex() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GreenAreaIndex(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndex() post action called");
            if ((!ModelState.IsValid) && ModelState.ContainsKey("GreenAreaIndex"))
            {
                var greenAreaIndexError = ModelState["GreenAreaIndex"].Errors.Count > 0 ?
                                ModelState["GreenAreaIndex"].Errors[0].ErrorMessage.ToString() : null;

                if (greenAreaIndexError != null && greenAreaIndexError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["GreenAreaIndex"].RawValue, Resource.lblGreenAreaIndexForError)))
                {
                    ModelState["GreenAreaIndex"].Errors.Clear();
                    ModelState["GreenAreaIndex"].Errors.Add(Resource.MsgForGreenAreaIndex);
                }
            }
            if (model.GreenAreaIndex == null)
            {
                ModelState.AddModelError("GreenAreaIndex", Resource.MsgIfGreenAreaIndexIsNull);
            }
            //if (model.SeasonId == 0)
            //{
            //    ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            //}
            if (model.GreenAreaIndex != null && (model.GreenAreaIndex < 0 || model.GreenAreaIndex > 3))
            {
                ModelState.AddModelError("GreenAreaIndex", Resource.MsgEnterAValidNumericGAIvalue);
            }
            if (!ModelState.IsValid)
            {
                //List<SeasonResponse> seasons = new List<SeasonResponse>();
                //seasons = await _fieldService.FetchSeasons();
                //ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsGreenAreaIndex = true;
            if (model.IsCheckAnswer)
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);


            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        [HttpGet]
        public async Task<IActionResult> BackActionForCalculateNitrogenCropQuestion()
        {
            _logger.LogTrace($"Field Controller : BackActionForCalculateNitrogenCropQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
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
        public async Task<IActionResult> BackActionForEstimateOfNitrogenMineralisationQuestion()
        {
            _logger.LogTrace($"Field Controller : BackActionForEstimateOfNitrogenMineralisationQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

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
        public async Task<IActionResult> SampleDepth()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SampleDepth() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in SampleDepth() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CurrentCropTypes");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleDepth(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SampleDepth() post action called");
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("SnsData", model);

            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                if (model == null)
                {
                    model = new SnsAnalysisViewModel();
                }
                model.IsRecentSoilAnalysisQuestionChange = false;
                model.IsCheckAnswer = true;
                
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SnsAnalysis Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : BackCheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SnsData", model);
            
                int snsCategoryId = await _fieldService.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

                if (snsCategoryId > 0)
                {
                    //if (snsCategoryId == (int)NMP.Portal.Enums.SNSCategories.Fruit)
                    //{
                    //    return RedirectToAction("SoilMineralNitrogenAnalysisResults");
                    //}
                    //else
                    //{
                        return RedirectToAction("SoilNitrogenSupplyIndex");
                    //}
                }
                else
                {
                    return RedirectToAction("CurrentCropTypes");
                }
            
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : CheckAnswer() post action called");
            

            if (!ModelState.IsValid)
            {
                return View("CheckAnswer", model);
            }
            int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);  // Convert.ToInt32(HttpContext.User.FindFirst(ClaimTypes.Sid)?.Value);
            var farmId = _farmDataProtector.Unprotect(model.EncryptedFarmId);
            //int farmId = model.FarmID;
            
            int? lastGroupNumber = null;
            Error error = new Error();
            (Farm farm, error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));

            if (farm != null && (string.IsNullOrWhiteSpace(error.Message)))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansByFarmId(farm.LastHarvestYear.Value, Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId)));

                if (harvestYearPlanResponse != null && harvestYearPlanResponse.Count > 0)
                {
                    var lastGroup = harvestYearPlanResponse.Where(cg => !string.IsNullOrEmpty(cg.CropGroupName) && cg.CropGroupName.StartsWith("Crop group") &&
                                     int.TryParse(cg.CropGroupName.Split(' ')[2], out _))
                                    .OrderByDescending(cg => int.Parse(cg.CropGroupName.Split(' ')[2]))
                                    .FirstOrDefault();
                    if (lastGroup != null)
                    {
                        lastGroupNumber = int.Parse(lastGroup.CropGroupName.Split(' ')[2]);
                    }
                }

                
            }
            else
            {
                TempData["AddFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                return RedirectToAction("CheckAnswer");
            }
            SnsAnalysis sns = new SnsAnalysis
            {
                CropID=model.CropID,
                CropTypeID = model.CropTypeId ?? 0,
                SampleDate = model.SampleDate,
                SnsAt0to30cm = model.SoilMineralNitrogenAt030CM,
                SnsAt30to60cm = model.SoilMineralNitrogenAt3060CM,
                SnsAt60to90cm = model.SoilMineralNitrogenAt6090CM,
                SampleDepth = model.SampleDepth,
                SoilMineralNitrogen = model.SoilMineralNitrogen,
                NumberOfShoots = model.NumberOfShoots,
                GreenAreaIndex=model.GreenAreaIndex,
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

            };

            (SnsAnalysis snsResponse, error) = await _snsAnalysisService.AddSnsAnalysisAsync(sns);
            if (error.Message == null && snsResponse != null)
            {
                string success = _cropDataProtector.Protect("true");
                _httpContextAccessor.HttpContext?.Session.Remove("SnsData");
                return RedirectToAction("Recommendations", "Crop",new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, sns=success });
            }
            else
            {
                TempData["AddFieldError"] = Resource.MsgWeCouldNotAddYourFieldPleaseTryAgainLater;
                return RedirectToAction("CheckAnswer");
            }

            return null;

        }
    }
}
