using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NMP.Portal.Controllers
{
    public class SnsAnalysisController(ILogger<SnsAnalysisController> logger, IDataProtectionProvider dataProtectionProvider,
         IFieldLogic fieldLogic, ICropLogic cropLogic, ISnsAnalysisLogic snsAnalysisLogic) : Controller
    {
        private readonly ILogger<SnsAnalysisController> _logger = logger;
        private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
        private readonly IFieldLogic _fieldLogic = fieldLogic;
        private readonly ICropLogic _cropLogic = cropLogic;
        private readonly ISnsAnalysisLogic _snsAnalysisLogic = snsAnalysisLogic;
        private readonly string _soilMineralNitrogen = Resource.lblSoilMineralNitrogenWithSpace;

        public IActionResult Index()
        {
            _logger.LogTrace($"Sns Controller : Index() action called");
            return View();
        }
        public IActionResult SnsAnalysisCancel(string q, string r, string? s)
        {
            _logger.LogTrace("SnsAnalysis Controller : SnsAnalysisCancel action called");
            HttpContext.Session.Remove("SnsData");
            return RedirectToAction("Recommendations", "Crop", new { q = q, r = r, s = s });
        }

        [HttpGet]
        public async Task<IActionResult> SoilSampleDate(string? q, string? r, string? s, string? c, string? f)   //q=farmId,r=fieldId,s=harvestYear, c=cropId (ID from crop table),f=fieldName
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilSampleDate() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            if (!string.IsNullOrWhiteSpace(c))
            {
                model.CropId = Convert.ToInt32(_cropDataProtector.Unprotect(c));
                (Crop crop, NMP.Commons.ServiceResponses.Error error) = await _cropLogic.FetchCropById(model.CropId);
                model.CropTypeId = crop.CropTypeID;
            }
            if (!string.IsNullOrWhiteSpace(f))
            {
                model.EncryptedFieldName = f ?? string.Empty;
                model.FieldName = _cropDataProtector.Unprotect(f);
            }

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilSampleDate(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilSampleDate() post action called");
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel snsViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            model.SnsCategoryId = snsCategoryId;
            HttpContext.Session.SetObjectAsJson("SnsData", model);
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SoilMineralNitrogenAnalysisResults() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt030CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt030CM)))
                {
                    ModelState["SoilMineralNitrogenAt030CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt030CM"].Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilMineralNitrogenAt030CMInLowerCase, 0, 999));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogenAt3060CM"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogenAt3060CM"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogenAt3060CM"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt3060CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt3060CM)))
                {
                    ModelState["SoilMineralNitrogenAt3060CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt3060CM"].Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilMineralNitrogenAt3060LowerCase, 0, 999));
                }
            }
            if ((!ModelState.IsValid) && ModelState.ContainsKey("SoilMineralNitrogenAt6090CM"))
            {
                var InvalidFormatError = ModelState["SoilMineralNitrogenAt6090CM"].Errors.Count > 0 ?
                                ModelState["SoilMineralNitrogenAt6090CM"].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilMineralNitrogenAt6090CM"].AttemptedValue, Resource.lblSoilMineralNitrogenAt6090CM)))
                {
                    ModelState["SoilMineralNitrogenAt6090CM"].Errors.Clear();
                    ModelState["SoilMineralNitrogenAt6090CM"].Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilMineralNitrogenAt6090AtLowerCase, 0, 999));
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
                if (model.SoilMineralNitrogenAt030CM < 0 || model.SoilMineralNitrogenAt030CM > 999)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt030CM", Resource.MsgEnterAValueBetween0And999);
                }
            }
            if (model.SoilMineralNitrogenAt3060CM != null)
            {
                if (model.SoilMineralNitrogenAt3060CM < 0 || model.SoilMineralNitrogenAt3060CM > 999)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt3060CM", Resource.MsgEnterAValueBetween0And999);
                }
            }
            if (model.SoilMineralNitrogenAt6090CM != null)
            {
                if (model.SoilMineralNitrogenAt6090CM < 0 || model.SoilMineralNitrogenAt6090CM > 999)
                {
                    ModelState.AddModelError("SoilMineralNitrogenAt6090CM", Resource.MsgEnterAValueBetween0And999);
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            model.SnsCategoryId = snsCategoryId;
            HttpContext.Session.SetObjectAsJson("SnsData", model);
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
            {
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in EstimateOfNitrogenMineralisationQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            if (model.IsEstimateOfNitrogenMineralisation == true)
            {
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            else
            {
                model.AdjustmentValue = null;
                model.SoilOrganicMatter = null;
                model.IsBasedOnSoilOrganicMatter = null;
                HttpContext.Session.SetObjectAsJson("SnsData", model);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                    if (HttpContext.Session.Keys.Contains("SnsData"))
                    {
                        SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

                HttpContext.Session.SetObjectAsJson("SnsData", model);
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
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                //sns logic
                var postMeasurementData = new MeasurementData();
                int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals)
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
                else if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.CropHeight && snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
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
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
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
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
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
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
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

                (SnsResponse snsResponse, Error error) = await _fieldLogic.FetchSNSIndexByMeasurementMethodAsync(postMeasurementData);
                if (error.Message == null)
                {
                    model.SnsIndex = snsResponse.SnsIndex;
                    model.SnsValue = snsResponse.SnsValue;
                    HttpContext.Session.SetObjectAsJson("SnsData", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SoilNitrogenSupplyIndex() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("CalculateNitrogenInCurrentCropQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
        public IActionResult SoilNitrogenSupplyIndex(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilNitrogenSupplyIndex() post action called");

            return RedirectToAction("CheckAnswer");
        }

        [HttpGet]
        public async Task<IActionResult> SoilOrganicMatter()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilOrganicMatter() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();

            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in SoilOrganicMatter() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("IsBasedOnSoilOrganicMatter");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilOrganicMatter(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilOrganicMatter() post action called");
            ValidateSoilOrganicMatterProperties(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.AdjustmentValue = null;
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        private void ValidateSoilOrganicMatterProperties(SnsAnalysisViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey("SoilOrganicMatter") && ModelState["SoilOrganicMatter"] != null)
            {
                var InvalidFormatError = ModelState["SoilOrganicMatter"]?.Errors.Count > 0 ?
                               ModelState["SoilOrganicMatter"]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SoilOrganicMatter"].AttemptedValue, Resource.lblSoilOrganicMatterForErrorNotValidValue)))
                {
                    ModelState["SoilOrganicMatter"]?.Errors.Clear();
                    ModelState["SoilOrganicMatter"]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilOrganicMatter, 0, 100));
                }

            }


            if (model.SoilOrganicMatter == null)
            {
                ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPercentageValue));
            }
            if (model.SoilOrganicMatter != null)
            {
                if (decimal.Round(model.SoilOrganicMatter.Value, 1) != model.SoilOrganicMatter)
                {
                    ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithOneDecimalPlaces, 0, 100));
                }
                if (model.SoilOrganicMatter < 0 || model.SoilOrganicMatter > 100)
                {
                    ModelState.AddModelError("SoilOrganicMatter", string.Format(Resource.MsgEnterAValueBetweenValue, 0, 100));
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> AdjustmentValue()
        {
            _logger.LogTrace($"SnsAnalysis Controller : AdjustmentValue() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();

            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in AdjustmentValue() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                    ModelState["AdjustmentValue"].Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblAdjustmentValue, 0, 60));
                }
            }
            if (model.AdjustmentValue == null)
            {
                ModelState.AddModelError("AdjustmentValue", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAdjustmentValue.ToLower()));
            }
            if (model.AdjustmentValue != null)
            {
                if (model.AdjustmentValue.Value % 1 != 0)
                {
                    ModelState.AddModelError("AdjustmentValue", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 60));
                }
                if (model.AdjustmentValue < 0 || model.AdjustmentValue > 60)
                {
                    ModelState.AddModelError("AdjustmentValue", string.Format(Resource.MsgEnterAValueBetweenValue, 0, 60));
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.SoilOrganicMatter = null;
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        [HttpGet]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CalculateNitrogenInCurrentCropQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in CalculateNitrogenInCurrentCropQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            HttpContext.Session.SetObjectAsJson("SnsData", model);

            if (model.IsCalculateNitrogen == true)
            {
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals)
                {
                    return RedirectToAction("NumberOfShoots");
                }
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
                {
                    return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
                }
            }
            else
            {
                model.IsCalculateNitrogenNo = true;
                HttpContext.Session.SetObjectAsJson("SnsData", model);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in NumberOfShoots() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
            ValidateNumberOfShootsProperties(model);
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = new List<SeasonResponse>();
                seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsNumberOfShoots = true;
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        private void ValidateNumberOfShootsProperties(SnsAnalysisViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey("NumberOfShoots") && ModelState["NumberOfShoots"] != null)
            {
                var value = ModelState["NumberOfShoots"]?.AttemptedValue;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    ModelState["NumberOfShoots"]?.Errors.Clear();

                    if (!decimal.TryParse(value, out decimal num))
                    {
                        ModelState["NumberOfShoots"]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilOrganicMatter, 0, 1500));
                    }
                    else if (num % 1 != 0)
                    {
                        ModelState["NumberOfShoots"]?.Errors.Add(string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 1500));
                    }
                }
            }
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
        }
        [HttpGet]
        public async Task<IActionResult> GreenAreaIndexOrCropHeightQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndexOrCropHeightQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<CropGroupResponse> cropGroups = new List<CropGroupResponse>();

            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                cropGroups = await _fieldLogic.FetchCropGroups();
                ViewBag.CropGroupList = cropGroups;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in GreenAreaIndexOrCropHeightQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            HttpContext.Session.SetObjectAsJson("SnsData", model);

            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.CropHeight)
            {
                return RedirectToAction("CropHeight");
            }
            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.GAI)
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
            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                ViewBag.SeasonList = await _fieldLogic.FetchSeasons();
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in CropHeight() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
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

            ValidateCropHeightProperties(model);
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = new List<SeasonResponse>();
                seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsCropHeight = true;
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        private void ValidateCropHeightProperties(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("CropHeight"))
            {
                var InvalidFormatError = ModelState["CropHeight"]?.Errors.Count > 0 ?
                                ModelState["CropHeight"]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["CropHeight"].AttemptedValue, Resource.lblCropHeight)))
                {
                    ModelState["CropHeight"]?.Errors.Clear();
                    ModelState["CropHeight"]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblNumberOfShoots, 0, 30));
                }
            }
            if (model.CropHeight == null)
            {
                ModelState.AddModelError("CropHeight", Resource.lblEnterACropHeightBeforeContinue);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.CropHeight != null)
            {
                if (model.CropHeight.Value % 1 != 0)
                {
                    ModelState.AddModelError("CropHeight", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 30));
                }
                if (model.CropHeight < 0 || model.CropHeight > 30)
                {
                    ModelState.AddModelError("CropHeight", string.Format(Resource.MsgEnterAValueBetweenValue, 0, 30));
                }
            }
        }
        [HttpGet]
        public IActionResult GreenAreaIndex()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndex() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            List<SeasonResponse> seasons = new List<SeasonResponse>();

            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in GreenAreaIndex() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GreenAreaIndex(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndex() post action called");

            ValidateGrenAreaIndexProperties(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.IsGreenAreaIndex = true;
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("EstimateOfNitrogenMineralisationQuestion");
        }

        private void ValidateGrenAreaIndexProperties(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("GreenAreaIndex"))
            {
                var greenAreaIndexError = ModelState["GreenAreaIndex"]?.Errors.Count > 0 ?
                                ModelState["GreenAreaIndex"]?.Errors[0].ErrorMessage.ToString() : null;

                if (greenAreaIndexError != null && greenAreaIndexError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["GreenAreaIndex"]?.RawValue, Resource.lblGreenAreaIndexForError)))
                {
                    ModelState["GreenAreaIndex"]?.Errors.Clear();
                    ModelState["GreenAreaIndex"]?.Errors.Add(Resource.MsgForGreenAreaIndex);
                }
            }
            if (model.GreenAreaIndex == null)
            {
                ModelState.AddModelError("GreenAreaIndex", Resource.MsgIfGreenAreaIndexIsNull);
            }

            if (model.GreenAreaIndex != null && (model.GreenAreaIndex < 0 || model.GreenAreaIndex > 3))
            {
                ModelState.AddModelError("GreenAreaIndex", string.Format(Resource.MsgEnterAValueBetweenValue, 0, 3));
            }
        }

        [HttpGet]
        public async Task<IActionResult> BackActionForCalculateNitrogenCropQuestion()
        {
            _logger.LogTrace("Field Controller : BackActionForCalculateNitrogenCropQuestion() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("CheckAnswer");
            }
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
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
            if (HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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
            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction("SoilMineralNitrogenAnalysisResults");
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
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
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SampleDepth() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("SoilSampleDate");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleDepth(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SampleDepth() post action called");

            if (!ModelState.IsValid && ModelState.ContainsKey("SampleDepth") && ModelState["SampleDepth"] != null)
            {
                var value = ModelState["SampleDepth"]?.AttemptedValue;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    ModelState["SampleDepth"]?.Errors.Clear();

                    if (!decimal.TryParse(value, out decimal num))
                    {
                        ModelState["SampleDepth"]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSampleDepth, 1, 90));
                    }
                    else if (num % 1 != 0)
                    {
                        ModelState["SampleDepth"]?.Errors.Add(string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 1, 90));
                    }
                }
            }

            if (!ModelState.IsValid && ModelState.ContainsKey(_soilMineralNitrogen) && ModelState[_soilMineralNitrogen] != null)
            {
                var value = ModelState[_soilMineralNitrogen]?.AttemptedValue;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    ModelState[_soilMineralNitrogen]?.Errors.Clear();

                    if (!decimal.TryParse(value, out decimal num))
                    {
                        ModelState[_soilMineralNitrogen]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilOrganicMatter, 0, 999));
                    }
                    else if (num % 1 != 0)
                    {
                        ModelState[_soilMineralNitrogen]?.Errors.Add(string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 999));
                    }
                }
            }

            if (model.SampleDepth == null)
            {
                ModelState.AddModelError("SampleDepth", Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.SoilMineralNitrogen == null)
            {
                ModelState.AddModelError(_soilMineralNitrogen, Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.SampleDepth != null)
            {
                if (model.SampleDepth.Value % 1 != 0)
                {
                    ModelState.AddModelError("SampleDepth", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 1, 90));
                }
                if (model.SampleDepth < 1 || model.SampleDepth > 90)
                {
                    ModelState.AddModelError("SampleDepth", string.Format(Resource.MsgEnterAValueBetweenValue, 1, 90));
                }
            }
            if (model.SoilMineralNitrogen != null)
            {
                if (model.SoilMineralNitrogen.Value % 1 != 0)
                {
                    ModelState.AddModelError(_soilMineralNitrogen, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 999));
                }
                if (model.SoilMineralNitrogen < 0 || model.SoilMineralNitrogen > 999)
                {
                    ModelState.AddModelError(_soilMineralNitrogen, string.Format(Resource.MsgEnterAValueBetweenValue, 0, 999));
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

            HttpContext.Session.SetObjectAsJson("SnsData", model);
            return RedirectToAction("SoilNitrogenSupplyIndex");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
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

                HttpContext.Session.SetObjectAsJson("SnsData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in CheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["Error"] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : BackCheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            if (HttpContext.Session.Keys.Contains("SnsData"))
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            model.IsCheckAnswer = false;
            HttpContext.Session.SetObjectAsJson("SnsData", model);

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

            if (snsCategoryId > 0)
            {
                return RedirectToAction("SoilNitrogenSupplyIndex");
            }
            else
            {
                return RedirectToAction("CheckAnswer");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : CheckAnswer() post action called");
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("CheckAnswer", model);
                }
                int userId = Convert.ToInt32(HttpContext.User.FindFirst("UserId")?.Value);
                Error error = new Error();
                SnsAnalysis sns = new SnsAnalysis
                {
                    CropID = model.CropID,
                    CropTypeID = model.CropTypeId ?? 0,
                    SampleDate = model.SampleDate,
                    SnsAt0to30cm = model.SoilMineralNitrogenAt030CM,
                    SnsAt30to60cm = model.SoilMineralNitrogenAt3060CM,
                    SnsAt60to90cm = model.SoilMineralNitrogenAt6090CM,
                    SampleDepth = model.SampleDepth,
                    SoilMineralNitrogen = model.SoilMineralNitrogen,
                    NumberOfShoots = model.NumberOfShoots,
                    GreenAreaIndex = model.GreenAreaIndex,
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

                (SnsAnalysis snsResponse, error) = await _snsAnalysisLogic.AddSnsAnalysisAsync(sns);
                if (error.Message == null && snsResponse != null)
                {
                    string success = _cropDataProtector.Protect("true");
                    HttpContext.Session.Remove("SnsData");
                    return RedirectToAction("Recommendations", "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, sns = success });
                }
                else
                {
                    TempData["CheckAnswerError"] = Resource.MsgWeCouldNotAddYourSnsPleaseTryAgainLater;
                    return RedirectToAction("CheckAnswer");
                }
            }
            catch (Exception ex)
            {
                TempData["CheckAnswerError"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }
        }

        [HttpGet]
        public IActionResult RemoveSnsAnalysis(string? q, string? r, string? s, string? c)
        {
            _logger.LogTrace("SnsAnalysis Controller : RemoveSnsAnalysis() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            try
            {

                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.EncryptedFarmId = q;
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    model.EncryptedFieldId = r;
                }
                if (!string.IsNullOrWhiteSpace(s))
                {
                    model.EncryptedHarvestYear = s;
                }
                if (!string.IsNullOrWhiteSpace(c))
                {
                    model.EncryptedCropId = c;
                    model.CropId = Convert.ToInt32(_cropDataProtector.Unprotect(c));
                }
                HttpContext.Session.SetObjectAsJson("SnsData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in RemoveSnsAnalysis() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["NutrientRecommendationsError"] = ex.Message;
                return RedirectToAction("Recommendations", "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }
            return View(model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSnsAnalysis(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : RemoveSns() post action called");
            if (model.IsSnsRemove == null)
            {
                ModelState.AddModelError("IsSNSRemove", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsSnsRemove == false)
            {
                return RedirectToAction("Recommendations", "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }
            else
            {
                SnsAnalysis snsAnalysis = await _snsAnalysisLogic.FetchSnsAnalysisByCropIdAsync(model.CropId);
                if (snsAnalysis != null)
                {
                    (string message, Error error) = await _snsAnalysisLogic.RemoveSnsAnalysisAsync(snsAnalysis.ID.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && (!string.IsNullOrWhiteSpace(message)))
                    {
                        return RedirectToAction("Recommendations", "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(string.Format(Resource.MsgYourDataSuccessfullyRemoved, Resource.lblSoilNitrogenSupplyAnalysis)) });
                    }
                    else
                    {
                        TempData["RemoveSNSError"] = error.Message;
                        return View(model);
                    }
                }
            }
            return View(model);

        }
        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("SnsAnalysis Controller : Cancel() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            try
            {
                if (HttpContext.Session.Keys.Contains("SnsData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>("SnsData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["CheckAnswerError"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : Cancel() post action called");
            if (model.IsCancel == null)
            {
                ModelState.AddModelError("IsCancel", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("Cancel", model);
            }
            if (model.IsCancel.HasValue && !model.IsCancel.Value)
            {
                return RedirectToAction("CheckAnswer");
            }
            else
            {
                HttpContext.Session.Remove("SnsData");
                return RedirectToAction("Recommendations", "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }
        }
    }
}
