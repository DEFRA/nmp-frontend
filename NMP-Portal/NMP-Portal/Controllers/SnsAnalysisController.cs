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
         IFieldLogic fieldLogic, ICropLogic cropLogic, ISnsAnalysisLogic snsAnalysisLogic, IFarmLogic farmLogic) : Controller
    {
        private readonly ILogger<SnsAnalysisController> _logger = logger;
        private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
        private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        private readonly IFieldLogic _fieldLogic = fieldLogic;
        private readonly ICropLogic _cropLogic = cropLogic;
        private readonly ISnsAnalysisLogic _snsAnalysisLogic = snsAnalysisLogic;
        private readonly string _soilMineralNitrogen = Resource.lblSoilMineralNitrogenWithSpace;
        private readonly IFarmLogic _farmLogic = farmLogic;
        private const string _snsDataKey = "SnsData";  //GreenAreaIndex
        private const string _recommendationsAction = "Recommendations";
        private const string _sampleDateKey = "SampleDate";
        private const string _checkAnswerAction = "CheckAnswer";
        private const string _farmListAction = "FarmList";
        private const string _sampleDepthAction = "SampleDepth";
        private const string _soilMineralNitrogenAnalysisResultsAction = "SoilMineralNitrogenAnalysisResults";
        private const string _errorTempDataKey = "Error";
        private const string _soilMineralNitrogenAt030CMProp = "SoilMineralNitrogenAt030CM";
        private const string _soilMineralNitrogenAt3060CMProp = "SoilMineralNitrogenAt3060CM";
        private const string _soilMineralNitrogenAt6090CMProp = "SoilMineralNitrogenAt6090CM";
        private const string _calculateNitrogenInCurrentCropQuestionAction = "CalculateNitrogenInCurrentCropQuestion";
        private const string _estimateOfNitrogenMineralisationQuestionAction = "EstimateOfNitrogenMineralisationQuestion";
        private const string _isBasedOnSoilOrganicMatterAction = "IsBasedOnSoilOrganicMatter";
        private const string _soilNitrogenSupplyIndexAction = "SoilNitrogenSupplyIndex";
        private const string _soilOrganicMatterAction = "SoilOrganicMatter";
        private const string _adjustmentValueAction = "AdjustmentValue";
        private const string _numberOfShootsAction = "NumberOfShoots";
        private const string _cropHeightAction = "CropHeight";
        private const string _greenAreaIndexAction = "GreenAreaIndex";

        public IActionResult Index()
        {
            _logger.LogTrace($"Sns Controller : Index() action called");
            return View();
        }
        public IActionResult SnsAnalysisCancel(string q, string r, string? s)
        {
            _logger.LogTrace("SnsAnalysis Controller : SnsAnalysisCancel action called");
            HttpContext.Session.Remove(_snsDataKey);
            return RedirectToAction(_recommendationsAction, "Crop", new { q = q, r = r, s = s });
        }

        [HttpGet]
        public async Task<IActionResult> SoilSampleDate(string? q, string? r, string? s, string? c, string? f)   //q=farmId,r=fieldId,s=harvestYear, c=cropId (ID from crop table),f=fieldName
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilSampleDate() action called");
            SnsAnalysisViewModel model = new SnsAnalysisViewModel();
            if (HasSnsDataInSession())
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
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
                (Crop crop, _) = await _cropLogic.FetchCropById(model.CropId);
                model.CropTypeId = crop.CropTypeID;
            }
            if (!string.IsNullOrWhiteSpace(model.EncryptedFarmId))
            {
                int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                (FarmResponse farm, _) = await _farmLogic.FetchFarmByIdAsync(farmId);
                if (farm != null)
                {
                    model.FarmRB209CountryId = farm.RB209CountryID;
                }
            }
            if (!string.IsNullOrWhiteSpace(f))
            {
                model.EncryptedFieldName = f;
                model.FieldName = _cropDataProtector.Unprotect(f);
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilSampleDate(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilSampleDate() post action called");
            ValidateSoilSampleDateProperties(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel snsViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (snsViewModel.SampleDate == model.SampleDate)
                    {
                        return RedirectToAction(_checkAnswerAction);
                    }
                }
                else
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            model.SnsCategoryId = snsCategoryId;
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables || model.FarmRB209CountryId == (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                return RedirectToAction(_sampleDepthAction);
            }

            return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
        }

        private void ValidateSoilSampleDateProperties(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_sampleDateKey))
            {
                var dateError = ModelState[_sampleDateKey].Errors.Count > 0 ?
                                ModelState[_sampleDateKey].Errors[0].ErrorMessage.ToString() : null;

                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState[_sampleDateKey].Errors.Clear();
                    ModelState[_sampleDateKey].Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }

            if (model.SampleDate == null)
            {
                ModelState.AddModelError(_sampleDateKey, Resource.MsgdateMustBeFilledBeforeProceeding);
            }
            if (DateTime.TryParseExact(model.SampleDate.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError(_sampleDateKey, Resource.MsgDateEnteredIsNotValid);
            }

            if (model.SampleDate != null)
            {
                if (model.SampleDate.Value.Date > DateTime.Now)
                {
                    ModelState.AddModelError(_sampleDateKey, Resource.MsgDateShouldNotBeInTheFuture);
                }
                if (model.SampleDate.Value.Date.Year < 1601)
                {
                    ModelState.AddModelError(_sampleDateKey, Resource.MsgDateEnteredIsNotValid);
                }
            }
        }

        [HttpGet]
        public IActionResult SoilMineralNitrogenAnalysisResults()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilMineralNitrogenAnalysisResults() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }

                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SoilMineralNitrogenAnalysisResults() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction("SoilSampleDate");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilMineralNitrogenAnalysisResults(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilMineralNitrogenAnalysisResults() post action called");
            ValidateSoilMineralNitrogenAnalysisProperties(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.SoilMineralNitrogenAt030CM == model.SoilMineralNitrogenAt030CM && fieldViewModel.SoilMineralNitrogenAt3060CM == model.SoilMineralNitrogenAt3060CM && fieldViewModel.SoilMineralNitrogenAt6090CM == model.SoilMineralNitrogenAt6090CM)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            model.SnsCategoryId = snsCategoryId;
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
            {
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
            }

            return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
        }

        private void ValidateSoilMineralNitrogenAnalysisProperties(SnsAnalysisViewModel model)
        {
            ValidateSoilMineralNitrogenAnalysis();
            if (model.SoilMineralNitrogenAt030CM == null)
            {
                ModelState.AddModelError(_soilMineralNitrogenAt030CMProp, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblKilogramsOfSoilMineralNitrogenAt030CM));
            }
            if (model.SoilMineralNitrogenAt3060CM == null)
            {
                ModelState.AddModelError(_soilMineralNitrogenAt3060CMProp, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblKilogramsOfSoilMineralNitrogenAt3060CM));
            }
            ValidateMinMaxValueForSoilMineralNitrogenAnalysis(model);
        }
        private void ValidateSoilMineralNitrogenField(
    string modelStateKey,
    string displayLabel,
    string validationLabel,
    int minValue,
    int maxValue)
{
    if (!ModelState.IsValid && ModelState.ContainsKey(modelStateKey))
    {
        var invalidFormatError = ModelState[modelStateKey]?.Errors.Count > 0
            ? ModelState[modelStateKey]?.Errors[0].ErrorMessage
            : null;

        if (invalidFormatError != null &&
            invalidFormatError.Equals(
                string.Format(
                    Resource.lblEnterNumericValue,
                    ModelState[modelStateKey].AttemptedValue,
                    displayLabel)))
        {
            ModelState[modelStateKey]?.Errors.Clear();

            ModelState[modelStateKey]?.Errors.Add(
                string.Format(
                    Resource.MsgValidateSoilMineralNitrogenMinMax,
                    validationLabel,
                    minValue,
                    maxValue));
        }
    }
}

        private void ValidateSoilMineralNitrogenAnalysis()
        {
            ValidateSoilMineralNitrogenField(
                _soilMineralNitrogenAt030CMProp,
                Resource.lblSoilMineralNitrogenAt030CM,
                Resource.lblSoilMineralNitrogenAt030CMInLowerCase,
                0,
                999);

            ValidateSoilMineralNitrogenField(
                _soilMineralNitrogenAt3060CMProp,
                Resource.lblSoilMineralNitrogenAt3060CM,
                Resource.lblSoilMineralNitrogenAt3060LowerCase,
                0,
                999);

            ValidateSoilMineralNitrogenField(
                _soilMineralNitrogenAt6090CMProp,
                Resource.lblSoilMineralNitrogenAt6090CM,
                Resource.lblSoilMineralNitrogenAt6090AtLowerCase,
                0,
                999);
        }

        private void ValidateMinMaxValueForSoilMineralNitrogenAnalysis(SnsAnalysisViewModel model)
        {
            if (model.SoilMineralNitrogenAt030CM != null && (model.SoilMineralNitrogenAt030CM < 0 || model.SoilMineralNitrogenAt030CM > 999))
            {
                ModelState.AddModelError(_soilMineralNitrogenAt030CMProp, Resource.MsgEnterAValueBetween0And999);
            }
            if (model.SoilMineralNitrogenAt3060CM != null && (model.SoilMineralNitrogenAt3060CM < 0 || model.SoilMineralNitrogenAt3060CM > 999))
            {
                ModelState.AddModelError(_soilMineralNitrogenAt3060CMProp, Resource.MsgEnterAValueBetween0And999);
            }
            if (model.SoilMineralNitrogenAt6090CM != null && (model.SoilMineralNitrogenAt6090CM < 0 || model.SoilMineralNitrogenAt6090CM > 999))
            {
                ModelState.AddModelError(_soilMineralNitrogenAt6090CMProp, Resource.MsgEnterAValueBetween0And999);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EstimateOfNitrogenMineralisationQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : EstimateOfNitrogenMineralisationQuestion() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");

                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in EstimateOfNitrogenMineralisationQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
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
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.IsEstimateOfNitrogenMineralisation == model.IsEstimateOfNitrogenMineralisation)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            if (model.IsEstimateOfNitrogenMineralisation == true)
            {
                return RedirectToAction(_isBasedOnSoilOrganicMatterAction);
            }
            else
            {
                model.AdjustmentValue = null;
                model.SoilOrganicMatter = null;
                model.IsBasedOnSoilOrganicMatter = null;
                HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
                return RedirectToAction(_soilNitrogenSupplyIndexAction);
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter()
        {
            _logger.LogTrace($"SnsAnalysis Controller : IsBasedOnSoilOrganicMatter() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View("CalculateSoilNitrogenMineralisation", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
            }
        }

        [HttpPost]
        public async Task<IActionResult> IsBasedOnSoilOrganicMatter(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : IsBasedOnSoilOrganicMatter() post action called");
            try
            {
                if (model.IsBasedOnSoilOrganicMatter == null)
                {
                    ModelState.AddModelError(_isBasedOnSoilOrganicMatterAction, Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View("CalculateSoilNitrogenMineralisation", model);
                }
                if (model.IsCheckAnswer)
                {
                    if (HasSnsDataInSession())
                    {
                        SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                        if (fieldViewModel.IsBasedOnSoilOrganicMatter == model.IsBasedOnSoilOrganicMatter)
                        {
                            return RedirectToAction(_checkAnswerAction);
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
                        return RedirectToAction(_farmListAction, "Farm");
                    }
                }

                HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
                if (model.IsBasedOnSoilOrganicMatter.Value)
                {
                    return RedirectToAction(_soilOrganicMatterAction);
                }
                else
                {
                    return RedirectToAction(_adjustmentValueAction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in IsBasedOnSoilOrganicMatter() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_isBasedOnSoilOrganicMatterAction);
            }
        }


        private static MeasurementData BindMesaurmentDataForOtherArableAndPotatoes(SnsAnalysisViewModel model)
        {
            MeasurementData postMeasurementData;
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
            return postMeasurementData;
        }

        private static MeasurementData BindMesaurmentDataForWinterOilseedRape(SnsAnalysisViewModel model)
        {
            MeasurementData postMeasurementData;
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
            return postMeasurementData;
        }

        private static MeasurementData BindMesaurmentDataForWinterCereal(SnsAnalysisViewModel model)
        {
            MeasurementData postMeasurementData;
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
            return postMeasurementData;
        }

        private static void BindMeasurementDataForVegetable(SnsAnalysisViewModel model, ref MeasurementData postMeasurementData, ref MeasurementDataForScotland postMeasurementDataForScotland)
        {
            if (model.FarmRB209CountryId != (int)NMP.Commons.Enums.RB209Country.Scotland)
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
                postMeasurementDataForScotland = new MeasurementDataForScotland
                {
                    smnDepth = model.SampleDepth.Value,
                    measuredSmn = model.SoilMineralNitrogen.Value
                };
            }
        }


        private async Task BindSnsIndex(SnsAnalysisViewModel model, MeasurementData postMeasurementData, MeasurementDataForScotland postMeasurementDataForScotland)
        {
            if (model.FarmRB209CountryId != (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                (SnsResponse snsResponse, Error error) = await _fieldLogic.FetchSNSIndexByMeasurementMethodAsync(postMeasurementData);
                if (string.IsNullOrWhiteSpace(error?.Message))
                {
                    model.SnsIndex = snsResponse.SnsIndex;
                    model.SnsValue = snsResponse.SnsValue;
                    HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
                }
            }
            else
            {
                (SnsResponseForScotland snsResponse, Error error) = await _fieldLogic.FetchSNSIndexByMeasurementMethodForScotlandAsync(postMeasurementDataForScotland);
                if (string.IsNullOrWhiteSpace(error?.Message))
                {
                    model.SnsIndex = snsResponse.ResidueGroupId;
                    model.NitrogenResidueGroup = snsResponse.ResidueGroup;
                    HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
                }
            }
        }

        private static MeasurementData BindMesaurmentDataForWinterOilseedRapeOnly(SnsAnalysisViewModel model)
        {
            MeasurementData postMeasurementData;
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
            return postMeasurementData;
        }

        [HttpGet]
        public async Task<IActionResult> SoilNitrogenSupplyIndex()
        {
            _logger.LogTrace($"Field Controller : SoilNitrogenSupplyIndex() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }

                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);


                //sns logic
                var postMeasurementData = new MeasurementData();
                var postMeasurementDataForScotland = new MeasurementDataForScotland();
                int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables || model.FarmRB209CountryId == (int)NMP.Commons.Enums.RB209Country.Scotland)
                {
                    BindMeasurementDataForVegetable(model, ref postMeasurementData, ref postMeasurementDataForScotland);

                }
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals)
                {
                    postMeasurementData = BindMesaurmentDataForWinterCereal(model);

                }
                else if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.CropHeight && snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
                {
                    postMeasurementData = BindMesaurmentDataForWinterOilseedRape(model);
                }
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
                {
                    postMeasurementData = BindMesaurmentDataForWinterOilseedRapeOnly(model);
                }
                else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
                {
                    postMeasurementData = BindMesaurmentDataForOtherArableAndPotatoes(model);
                }
                else
                {
                    return RedirectToAction(_checkAnswerAction);
                }

                await BindSnsIndex(model, postMeasurementData, postMeasurementDataForScotland);

                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SoilNitrogenSupplyIndex() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuppressMessage("SonarAnalyzer.CSharp", "S6967:ModelState.IsValid should be called in controller actions", Justification = "No validation is needed as data is not saving in database.")]
        public IActionResult SoilNitrogenSupplyIndex(SnsAnalysisViewModel model)
        {
            _logger.LogTrace("SnsAnalysis Controller : SoilNitrogenSupplyIndex() post action called");

            return RedirectToAction(_checkAnswerAction);
        }

        [HttpGet]
        public async Task<IActionResult> SoilOrganicMatter()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SoilOrganicMatter() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in SoilOrganicMatter() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_isBasedOnSoilOrganicMatterAction);
            }
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
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.SoilOrganicMatter == model.SoilOrganicMatter)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_soilNitrogenSupplyIndexAction);
        }

        private void ValidateSoilOrganicMatterProperties(SnsAnalysisViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey(_soilOrganicMatterAction) && ModelState[_soilOrganicMatterAction] != null)
            {
                var InvalidFormatError = ModelState[_soilOrganicMatterAction]?.Errors.Count > 0 ?
                               ModelState[_soilOrganicMatterAction]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_soilOrganicMatterAction].AttemptedValue, Resource.lblSoilOrganicMatterForErrorNotValidValue)))
                {
                    ModelState[_soilOrganicMatterAction]?.Errors.Clear();
                    ModelState[_soilOrganicMatterAction]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilOrganicMatter, 0, 100));
                }

            }


            if (model.SoilOrganicMatter == null)
            {
                ModelState.AddModelError(_soilOrganicMatterAction, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPercentageValue));
            }
            if (model.SoilOrganicMatter != null)
            {
                if (decimal.Round(model.SoilOrganicMatter.Value, 1) != model.SoilOrganicMatter)
                {
                    ModelState.AddModelError(_soilOrganicMatterAction, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithOneDecimalPlaces, 0, 100));
                }
                if (model.SoilOrganicMatter < 0 || model.SoilOrganicMatter > 100)
                {
                    ModelState.AddModelError(_soilOrganicMatterAction, string.Format(Resource.MsgEnterAValueBetweenValue, 0, 100));
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> AdjustmentValue()
        {
            _logger.LogTrace($"SnsAnalysis Controller : AdjustmentValue() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in AdjustmentValue() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_isBasedOnSoilOrganicMatterAction);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustmentValue(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : AdjustmentValue() post action called");
            ValidateAdjustmentValues(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.SoilOrganicMatter = null;
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.AdjustmentValue == model.AdjustmentValue)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_soilNitrogenSupplyIndexAction);
        }

        private void ValidateAdjustmentValues(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_adjustmentValueAction))
            {
                var InvalidFormatError = ModelState[_adjustmentValueAction].Errors.Count > 0 ?
                                ModelState[_adjustmentValueAction].Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_adjustmentValueAction].AttemptedValue, Resource.lblAdjustmentValueForError)))
                {
                    ModelState[_adjustmentValueAction].Errors.Clear();
                    ModelState[_adjustmentValueAction].Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblAdjustmentValue, 0, 60));
                }
            }
            if (model.AdjustmentValue == null)
            {
                ModelState.AddModelError(_adjustmentValueAction, string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAdjustmentValue.ToLower()));
            }
            if (model.AdjustmentValue != null)
            {
                if (model.AdjustmentValue.Value % 1 != 0)
                {
                    ModelState.AddModelError(_adjustmentValueAction, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 60));
                }
                if (model.AdjustmentValue < 0 || model.AdjustmentValue > 60)
                {
                    ModelState.AddModelError(_adjustmentValueAction, string.Format(Resource.MsgEnterAValueBetweenValue, 0, 60));
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculateNitrogenInCurrentCropQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CalculateNitrogenInCurrentCropQuestion() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in CalculateNitrogenInCurrentCropQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
            }
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
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.IsCalculateNitrogen == model.IsCalculateNitrogen)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }
            return await RedirectForCalculateNitrogenUnCurrentCrop(model);
        }

        private async Task<IActionResult> RedirectForCalculateNitrogenUnCurrentCrop(SnsAnalysisViewModel model)
        {
            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);

            if (model.IsCalculateNitrogen == true)
            {
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals)
                {
                    return RedirectToAction(_numberOfShootsAction);
                }
                if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape)
                {
                    return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
                }
            }
            else
            {
                model.IsCalculateNitrogenNo = true;
                HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
                return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
            }

            return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
        }

        [HttpGet]
        public async Task<IActionResult> NumberOfShoots()
        {
            _logger.LogTrace($"SnsAnalysis Controller : NumberOfShoots() action called");

            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                List<SeasonResponse> seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in NumberOfShoots() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NumberOfShoots(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : NumberOfShoots() post action called");
            ValidateNumberOfShootsProperties(model);
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsNumberOfShoots = true;
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.NumberOfShoots == model.NumberOfShoots && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
        }

        private void ValidateNumberOfShootsProperties(SnsAnalysisViewModel model)
        {
            if (!ModelState.IsValid && ModelState.ContainsKey(_numberOfShootsAction) && ModelState[_numberOfShootsAction] != null)
            {
                var value = ModelState[_numberOfShootsAction]?.AttemptedValue;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    ModelState[_numberOfShootsAction]?.Errors.Clear();

                    if (!decimal.TryParse(value, out decimal num))
                    {
                        ModelState[_numberOfShootsAction]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblSoilOrganicMatter, 0, 1500));
                    }
                    else if (num % 1 != 0)
                    {
                        ModelState[_numberOfShootsAction]?.Errors.Add(string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 1500));
                    }
                }
            }
            if (model.NumberOfShoots == null)
            {
                ModelState.AddModelError(_numberOfShootsAction, Resource.lblEnterAValidNumber);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.NumberOfShoots != null && (model.NumberOfShoots < 0 || model.NumberOfShoots > 1500))
            {
                ModelState.AddModelError(_numberOfShootsAction, Resource.MsgEnterShootNumberBetween0To1500);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GreenAreaIndexOrCropHeightQuestion()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndexOrCropHeightQuestion() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }

                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                List<CropGroupResponse> cropGroups = await _fieldLogic.FetchCropGroups();
                ViewBag.CropGroupList = cropGroups;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in GreenAreaIndexOrCropHeightQuestion() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
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
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.GreenAreaIndexOrCropHeight == model.GreenAreaIndexOrCropHeight)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);

            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.CropHeight)
            {
                return RedirectToAction(_cropHeightAction);
            }
            if (model.GreenAreaIndexOrCropHeight == (int)NMP.Commons.Enums.GreenAreaIndexOrCropHeight.GAI)
            {
                return RedirectToAction(_greenAreaIndexAction);
            }
            return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
        }


        [HttpGet]
        public async Task<IActionResult> CropHeight()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CropHeight() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                ViewBag.SeasonList = await _fieldLogic.FetchSeasons();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Field Controller : Exception in CropHeight() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CropHeight(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : CropHeight() post action called");

            ValidateCropHeightProperties(model);
            if (!ModelState.IsValid)
            {
                List<SeasonResponse> seasons = await _fieldLogic.FetchSeasons();
                ViewBag.SeasonList = seasons;
                return View(model);
            }
            model.IsCropHeight = true;
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.CropHeight == model.CropHeight && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
        }

        private void ValidateCropHeightProperties(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_cropHeightAction))
            {
                var InvalidFormatError = ModelState[_cropHeightAction]?.Errors.Count > 0 ?
                                ModelState[_cropHeightAction]?.Errors[0].ErrorMessage.ToString() : null;

                if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_cropHeightAction].AttemptedValue, Resource.lblCropHeight)))
                {
                    ModelState[_cropHeightAction]?.Errors.Clear();
                    ModelState[_cropHeightAction]?.Errors.Add(string.Format(Resource.MsgValidateSoilMineralNitrogenMinMax, Resource.lblNumberOfShoots, 0, 30));
                }
            }
            if (model.CropHeight == null)
            {
                ModelState.AddModelError(_cropHeightAction, Resource.lblEnterACropHeightBeforeContinue);
            }
            if (model.SeasonId == 0)
            {
                ModelState.AddModelError("SeasonId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.CropHeight != null)
            {
                if (model.CropHeight.Value % 1 != 0)
                {
                    ModelState.AddModelError(_cropHeightAction, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 30));
                }
                if (model.CropHeight < 0 || model.CropHeight > 30)
                {
                    ModelState.AddModelError(_cropHeightAction, string.Format(Resource.MsgEnterAValueBetweenValue, 0, 30));
                }
            }
        }
        [HttpGet]
        public IActionResult GreenAreaIndex()
        {
            _logger.LogTrace($"SnsAnalysis Controller : GreenAreaIndex() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in GreenAreaIndex() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction("GreenAreaIndexOrCropHeightQuestion");
            }
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
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.GreenAreaIndex == model.GreenAreaIndex && fieldViewModel.SeasonId == model.SeasonId)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_estimateOfNitrogenMineralisationQuestionAction);
        }

        private void ValidateGrenAreaIndexProperties(SnsAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey(_greenAreaIndexAction))
            {
                var greenAreaIndexError = ModelState[_greenAreaIndexAction]?.Errors.Count > 0 ?
                                ModelState[_greenAreaIndexAction]?.Errors[0].ErrorMessage.ToString() : null;

                if (greenAreaIndexError != null && greenAreaIndexError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[_greenAreaIndexAction]?.RawValue, Resource.lblGreenAreaIndexForError)))
                {
                    ModelState[_greenAreaIndexAction]?.Errors.Clear();
                    ModelState[_greenAreaIndexAction]?.Errors.Add(Resource.MsgForGreenAreaIndex);
                }
            }
            if (model.GreenAreaIndex == null)
            {
                ModelState.AddModelError(_greenAreaIndexAction, Resource.MsgIfGreenAreaIndexIsNull);
            }

            if (model.GreenAreaIndex != null && (model.GreenAreaIndex < 0 || model.GreenAreaIndex > 3))
            {
                ModelState.AddModelError(_greenAreaIndexAction, string.Format(Resource.MsgEnterAValueBetweenValue, 0, 3));
            }
        }

        [HttpGet]
        public async Task<IActionResult> BackActionForCalculateNitrogenCropQuestion()
        {
            _logger.LogTrace("Field Controller : BackActionForCalculateNitrogenCropQuestion() action called");
            if (!HasSnsDataInSession())
            {
                return RedirectToAction(_farmListAction, "Farm");
            }
            SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerAction);
            }
            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
            {
                return RedirectToAction(_sampleDepthAction);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BackActionForEstimateOfNitrogenMineralisationQuestion()
        {
            _logger.LogTrace($"Field Controller : BackActionForEstimateOfNitrogenMineralisationQuestion() action called");
            if (!HasSnsDataInSession())
            {
                return RedirectToAction(_farmListAction, "Farm");
            }
            SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_checkAnswerAction);
            }
            else if (model.IsCalculateNitrogenNo)
            {
                return RedirectToAction(_calculateNitrogenInCurrentCropQuestionAction);
            }
            else if (model.IsNumberOfShoots)
            {
                return RedirectToAction(_numberOfShootsAction);
            }
            else if (model.IsCropHeight)
            {
                return RedirectToAction(_cropHeightAction);
            }
            else if (model.IsGreenAreaIndex)
            {
                return RedirectToAction(_greenAreaIndexAction);
            }
            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

            if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterCereals || snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.WinterOilseedRape ||
                snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.OtherArableAndPotatoes)
            {
                return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
            }
            else if (snsCategoryId == (int)NMP.Commons.Enums.SnsCategories.Vegetables)
            {
                return RedirectToAction(_sampleDepthAction);
            }
            return RedirectToAction(_soilMineralNitrogenAnalysisResultsAction);
        }

        [HttpGet]
        public async Task<IActionResult> SampleDepth()
        {
            _logger.LogTrace($"SnsAnalysis Controller : SampleDepth() action called");
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in SampleDepth() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction("SoilSampleDate");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SampleDepth(SnsAnalysisViewModel model)
        {
            _logger.LogTrace($"SnsAnalysis Controller : SampleDepth() post action called");

            ValidateSampleDepthProperties(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.IsCheckAnswer)
            {
                if (HasSnsDataInSession())
                {
                    SnsAnalysisViewModel fieldViewModel = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                    if (fieldViewModel.SampleDepth == model.SampleDepth && fieldViewModel.SoilMineralNitrogen == model.SoilMineralNitrogen)
                    {
                        return RedirectToAction(_checkAnswerAction);
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
                    return RedirectToAction(_farmListAction, "Farm");
                }
            }

            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            return RedirectToAction(_soilNitrogenSupplyIndexAction);
        }

        private void ValidateSampleDepthProperties(SnsAnalysisViewModel model)
        {
            ValidateSoilMeasurementInput();

            if (model.SampleDepth == null)
            {
                ModelState.AddModelError(_sampleDepthAction, Resource.MsgEnterAValueBeforeContinue);
            }
            if (model.SoilMineralNitrogen == null)
            {
                ModelState.AddModelError(_soilMineralNitrogen, Resource.MsgEnterAValueBeforeContinue);
            }
            ValidateMinMaxSampleDepth(model);
        }

        private void ValidateSoilMeasurementInput()
        {
            if (!ModelState.IsValid && ModelState.ContainsKey(_sampleDepthAction) && ModelState[_sampleDepthAction] != null)
            {
                var value = ModelState[_sampleDepthAction]?.AttemptedValue;


                ValidateWholeNumber(value, _sampleDepthAction, Resource.lblSampleDepth, 1, 90);

            }

            if (!ModelState.IsValid && ModelState.ContainsKey(_soilMineralNitrogen) && ModelState[_soilMineralNitrogen] != null)
            {
                var value = ModelState[_soilMineralNitrogen]?.AttemptedValue;

                ValidateWholeNumber(value, _soilMineralNitrogen, Resource.lblSoilMineralNitrogen, 0, 999);
            }
        }
        private void ValidateWholeNumber(string? value, string modelStateKey, string fieldLabel, int minValue, int maxValue)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ModelState[modelStateKey]?.Errors.Clear();

                if (!decimal.TryParse(value, out decimal num))
                {
                    ModelState[modelStateKey]?.Errors.Add(
                        string.Format(
                            Resource.MsgValidateSoilMineralNitrogenMinMax,
                            fieldLabel,
                            minValue,
                            maxValue));
                }
                else if (num % 1 != 0)
                {
                    ModelState[modelStateKey]?.Errors.Add(
                        string.Format(
                            Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces,
                            minValue,
                            maxValue));
                }
            }
        }
        private void ValidateMinMaxSampleDepth(SnsAnalysisViewModel model)
        {
            if (model.SampleDepth != null)
            {
                if (model.SampleDepth.Value % 1 != 0)
                {
                    ModelState.AddModelError(_sampleDepthAction, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 1, 90));
                }
                if (model.SampleDepth < 1 || model.SampleDepth > 90)
                {
                    ModelState.AddModelError(_sampleDepthAction, string.Format(Resource.MsgEnterAValueBetweenValue, 1, 90));
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
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : CheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            try
            {
                if (HasSnsDataInSession())
                {
                    model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                }
                else
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }

                if (model == null)
                {
                    model = new SnsAnalysisViewModel();
                }
                model.IsRecentSoilAnalysisQuestionChange = false;
                model.IsCheckAnswer = true;

                HttpContext.Session.SetObjectAsJson(_snsDataKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in CheckAnswer() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_errorTempDataKey] = ex.Message;
                return RedirectToAction("CropTypes");
            }
            return View(model);

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
                    return View(_checkAnswerAction, model);
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
                    NitrogenResidueGroup = model.NitrogenResidueGroup,
                    CreatedOn = DateTime.Now,
                    CreatedByID = userId,
                    ModifiedOn = model.ModifiedOn,
                    ModifiedByID = model.ModifiedByID

                };

                (SnsAnalysis snsResponse, error) = await _snsAnalysisLogic.AddSnsAnalysisAsync(sns);
                if (string.IsNullOrWhiteSpace(error?.Message) && snsResponse != null)
                {
                    string success = _cropDataProtector.Protect("true");
                    HttpContext.Session.Remove(_snsDataKey);
                    return RedirectToAction(_recommendationsAction, "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, sns = success });
                }
                else
                {
                    TempData["CheckAnswerError"] = Resource.MsgWeCouldNotAddYourSnsPleaseTryAgainLater;
                    return RedirectToAction(_checkAnswerAction);
                }
            }
            catch (Exception ex)
            {
                TempData["CheckAnswerError"] = ex.Message;
                return RedirectToAction(_checkAnswerAction);
            }
        }

        public async Task<IActionResult> BackCheckAnswer()
        {
            _logger.LogTrace($"SnsAnalysis Controller : BackCheckAnswer() action called");
            SnsAnalysisViewModel? model = null;
            if (HasSnsDataInSession())
            {
                model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
            }
            else
            {
                return RedirectToAction(_farmListAction, "Farm");
            }

            model.IsCheckAnswer = false;
            HttpContext.Session.SetObjectAsJson(_snsDataKey, model);

            int snsCategoryId = await _fieldLogic.FetchSNSCategoryIdByCropTypeId(model.CropTypeId ?? 0);

            if (snsCategoryId > 0)
            {
                return RedirectToAction(_soilNitrogenSupplyIndexAction);
            }
            else
            {
                return RedirectToAction(_checkAnswerAction);
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
                HttpContext.Session.SetObjectAsJson(_snsDataKey, model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in RemoveSnsAnalysis() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["NutrientRecommendationsError"] = ex.Message;
                return RedirectToAction(_recommendationsAction, "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
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
                return RedirectToAction(_recommendationsAction, "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }
            else
            {
                SnsAnalysis snsAnalysis = await _snsAnalysisLogic.FetchSnsAnalysisByCropIdAsync(model.CropId);
                if (snsAnalysis != null)
                {
                    (string message, Error error) = await _snsAnalysisLogic.RemoveSnsAnalysisAsync(snsAnalysis.ID.Value);
                    if (string.IsNullOrWhiteSpace(error?.Message) && (!string.IsNullOrWhiteSpace(message)))
                    {
                        return RedirectToAction(_recommendationsAction, "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear, t = _cropDataProtector.Protect(string.Format(Resource.MsgYourDataSuccessfullyRemoved, Resource.lblSoilNitrogenSupplyAnalysis)) });
                    }
                    else
                    {
                        TempData["RemoveSNSError"] = error?.Message;
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
            try
            {
                if (!HasSnsDataInSession())
                {
                    return RedirectToAction(_farmListAction, "Farm");
                }
                SnsAnalysisViewModel model = HttpContext.Session.GetObjectFromJson<SnsAnalysisViewModel>(_snsDataKey);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SnsAnalysis Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData["CheckAnswerError"] = ex.Message;
                return RedirectToAction(_checkAnswerAction);
            }
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
                return RedirectToAction(_checkAnswerAction);
            }
            else
            {
                HttpContext.Session.Remove(_snsDataKey);
                return RedirectToAction(_recommendationsAction, "Crop", new { q = model.EncryptedFarmId, r = model.EncryptedFieldId, s = model.EncryptedHarvestYear });
            }
        }
        private bool HasSnsDataInSession()
        {
            return HttpContext.Session.Keys.Contains(_snsDataKey);
        }
    }
}
