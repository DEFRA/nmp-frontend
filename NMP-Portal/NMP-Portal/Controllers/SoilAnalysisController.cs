using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Portal.Helpers;
using System.Globalization;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class SoilAnalysisController(ILogger<SoilAnalysisController> logger, IDataProtectionProvider dataProtectionProvider, IFarmLogic farmLogic, ISoilLogic soilLogic,
        IFieldLogic fieldLogic, ISoilAnalysisLogic soilAnalysisLogic, IPKBalanceLogic pKBalanceLogic) : Controller
    {
        private readonly ILogger<SoilAnalysisController> _logger = logger;
        private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        private readonly IDataProtector _soilAnalysisDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.SoilAnalysisController");
        private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
        private readonly IFarmLogic _farmLogic = farmLogic;
        private readonly IFieldLogic _fieldLogic = fieldLogic;
        private readonly ISoilAnalysisLogic _soilAnalysisLogic = soilAnalysisLogic;
        private readonly ISoilLogic _soilLogic = soilLogic;
        private readonly IPKBalanceLogic _pKBalanceLogic = pKBalanceLogic;

        private const string _changeSoilAnalysisError = "ChangeSoilAnalysisError";
        private const string _changeSoilAnalysisActionName = "ChangeSoilAnalysis";
        private const string _soilNutrientValueTypeActionName = "SoilNutrientValueType";
        private const string _soilNutrientValueActionName = "SoilNutrientValue";
        private const string _potassiumIndexValue = "PotassiumIndexValue";
        private const string _magnesiumIndex = "MagnesiumIndex";
        private const string _phosphorusIndex = "PhosphorusIndex";  //Field
        private const string _soilAnalysisDataKey = "SoilAnalysisData";
        private const string _fieldSoilAnalysisDetailAction = "FieldSoilAnalysisDetail";
        private const string _fieldController = "Field";
        private const string _phosphorusKey = "Phosphorus";
        private SoilAnalysisViewModel? GetSoilAnalysisFromSession()
        {
            if (HttpContext.Session.Exists(_soilAnalysisDataKey))
            {
                return HttpContext.Session.GetObjectFromJson<SoilAnalysisViewModel>(_soilAnalysisDataKey);
            }
            return null;
        }

        private void SetSoilAnalysisDataToSession(SoilAnalysisViewModel plan)
        {
            HttpContext.Session.SetObjectAsJson(_soilAnalysisDataKey, plan);
        }

        private void RemoveSoilAnalysisDataFromSession()
        {
            if (HttpContext.Session.Exists(_soilAnalysisDataKey))
            {
                HttpContext.Session.Remove(_soilAnalysisDataKey);
            }
        }
        private void ValidateSoilAnalysisIndexValues(SoilAnalysisViewModel model)
        {
            if (!model.PH.HasValue)
            {
                ModelState.AddModelError("PH", Resource.MsgPhNotSet);
            }
            if (string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
            {
                ModelState.AddModelError(_potassiumIndexValue, Resource.MsgPotassiumIndexNotSet);
            }
            if (!model.PhosphorusIndex.HasValue)
            {
                ModelState.AddModelError(_phosphorusIndex, Resource.MsgPhosphorusIndexNotSet);
            }
            if (!model.MagnesiumIndex.HasValue)
            {
                ModelState.AddModelError(_magnesiumIndex, Resource.MsgMagnesiumIndexNotSet);
            }
        }
        private void ValidateSoilAnalysisMgValues(SoilAnalysis model)
        {
            if (!model.PH.HasValue)
            {
                ModelState.AddModelError("SoilAnalyses.PH", Resource.MsgPhNotSet);
            }
            if (!model.Potassium.HasValue)
            {
                ModelState.AddModelError("SoilAnalyses.Potassium", Resource.MsgPotassiumNotSet);
            }
            if (!model.Phosphorus.HasValue)
            {
                ModelState.AddModelError("SoilAnalyses.Phosphorus", Resource.MsgPhosphorusNotSet);
            }
            if (!model.Magnesium.HasValue)
            {
                ModelState.AddModelError("SoilAnalyses.Magnesium", Resource.MsgMagnesiumNotSet);
            }
        }

        private void ValidateSoilAnalysis(SoilAnalysisViewModel model)
        {
            if (!model.Date.HasValue)
            {
                ModelState.AddModelError("Date", string.Format(Resource.lblDateSampleTaken, model.FieldName));
            }
            if (!model.SulphurDeficient.HasValue)
            {
                ModelState.AddModelError("SulphurDeficient", Resource.lblSoilDeficientInSulpurForCheckAnswerNotset);
            }

            if (model.SoilNutrientValueType.HasValue)
            {
                if (model.SoilNutrientValueType.Value == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram &&
                    (!model.PH.HasValue && !model.Potassium.HasValue &&
                        !model.Phosphorus.HasValue && !model.Magnesium.HasValue))
                {
                    ValidateSoilAnalysisMgValues(model);
                }
                else if (model.SoilNutrientValueType.Value == (int)NMP.Commons.Enums.SoilNutrientValueType.Index &&
                    !model.PH.HasValue && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                        !model.MagnesiumIndex.HasValue && !model.PhosphorusIndex.HasValue)
                {
                    ValidateSoilAnalysisIndexValues(model);
                }
                else if (model.SoilNutrientValueType.Value == (int)NMP.Commons.Enums.SoilNutrientValueType.Status &&
                   !model.PH.HasValue && string.IsNullOrWhiteSpace(model.PotassiumStatus) &&
                       string.IsNullOrWhiteSpace(model.PhosphorusStatus) && string.IsNullOrWhiteSpace(model.MagnesiumStatus))
                {
                    ValidateSoilAnalysisIndexValues(model);
                }
            }
            else
            {
                ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgNutrientValueTypeForCheckAnswereNotSet);
            }
        }


        [HttpGet]
        public async Task<IActionResult> ChangeSoilAnalysis(string i, string j, string k, string l)//i= soilAnalysisId,j=EncryptedFieldId,k=EncryptedFarmId,l=IsSoilDataChanged
        {
            _logger.LogTrace("Soil Analysis Controller: ChangeSoilAnalysis({I}, {J},{K}, {L}) action called.", i, j, k, l);
            SoilAnalysisViewModel? model = new SoilAnalysisViewModel();

            try
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    model = GetSoilAnalysisFromSession();
                    if (model == null)
                    {
                        _logger.LogTrace("SoilAnalysisController: Session expired in ChangeSoilAnalysis() action.");
                        return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(i))
                {
                    Error? error;
                    _logger.LogTrace("SoilAnalysisController: farms/{J} called.", j);
                    (FarmResponse? farm, error) = await _farmLogic.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(k)));

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                    {
                        TempData[_changeSoilAnalysisError] = error.Message;
                        return View(model);
                    }

                    model.FarmRB209CountryID = farm?.RB209CountryID;
                    int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(j));
                    _logger.LogTrace("SoilAnalysisController: fields/{FieldId} called.", fieldId);
                    var field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                    model.FieldName = field.Name;
                    model.FarmName = farm?.Name;
                    model.FieldID = fieldId;
                    int decryptedSoilId = Convert.ToInt32(_fieldDataProtector.Unprotect(i));
                    _logger.LogTrace("SoilAnalysisController: soil-analyses/{DecryptedSoilId} called", decryptedSoilId);

                    (SoilAnalysis? soilAnalysis, error) = await _soilAnalysisLogic.FetchSoilAnalysisById(decryptedSoilId);

                    if (soilAnalysis != null)
                    {
                        model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                        model.Phosphorus = soilAnalysis.Phosphorus;
                        model.PH = soilAnalysis.PH;
                        model.Potassium = soilAnalysis.Potassium;
                        model.Magnesium = soilAnalysis.Magnesium;
                        model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
                        model.PhosphorusIndex = soilAnalysis.PhosphorusIndex;
                        model.OrganicMatterPercentage = soilAnalysis.OrganicMatterPercentage;
                        BindPotassiumIndexValueForChangeSoilAnalysis(model, soilAnalysis);

                        model.PotassiumIndex = soilAnalysis.PotassiumIndex;
                        model.MagnesiumIndex = soilAnalysis.MagnesiumIndex;
                        model.Date = soilAnalysis.Date;
                        model.SulphurDeficient = soilAnalysis.SulphurDeficient;
                        if (!string.IsNullOrWhiteSpace(j))
                        {
                            model.EncryptedFieldId = j;
                        }

                        if (!string.IsNullOrWhiteSpace(k))
                        {
                            model.EncryptedFarmId = k;
                        }

                        model.EncryptedSoilAnalysisId = i;
                        model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
                        model.PotassiumMethodologyID = soilAnalysis.PotassiumMethodologyID;
                        model.MagnesiumMethodologyID = soilAnalysis.MagnesiumMethodologyID;

                        model.MagnesiumStatus = soilAnalysis.MagnesiumStatus;
                        model.PhosphorusStatus = soilAnalysis.PhosphorusStatus;
                        model.PotassiumStatus = soilAnalysis.PotassiumStatus;
                    }

                    BindSoilNutrientValueTypeForChangeSoilAnalysis(model);

                    SetSoilAnalysisDataToSession(model);
                }

                BindDataForCheckAnswer(i, l, model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Soil Analysis Controller : Exception in ChangeSoilAnalysis() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_changeSoilAnalysisError] = ex.Message;
                return View(model);
            }
            await FetchMethodologyName(model);
            return View(model);
        }

        private void BindDataForCheckAnswer(string i, string l, SoilAnalysisViewModel? model)
        {
            if (model != null)
            {
                model.IsCheckAnswer = true;
                model.IsSoilAnalysesMethodChange = false;
                model.IsSoilNutrientValueTypeChange = false;
                SetSoilAnalysisDataToSession(model);

                if (!string.IsNullOrWhiteSpace(i) && string.IsNullOrWhiteSpace(l))
                {
                    HttpContext.Session.SetObjectAsJson("SoilAnalysisDataBeforeUpdate", model);
                }

                BindViewBegForIsDataChange(model);
            }
        }

        private void BindViewBegForIsDataChange(SoilAnalysisViewModel? model)
        {
            var previousModel = HttpContext.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisDataBeforeUpdate");
            bool isDataChanged = false;

            if (previousModel != null)
            {
                string oldJson = JsonConvert.SerializeObject(previousModel);
                string newJson = JsonConvert.SerializeObject(model);

                isDataChanged = !string.Equals(oldJson, newJson, StringComparison.Ordinal);
            }
            ViewBag.IsDataChange = isDataChanged;
        }

        private static void BindSoilNutrientValueTypeForChangeSoilAnalysis(SoilAnalysisViewModel? model)
        {
            if (model.Phosphorus != null ||
             model.Potassium != null || model.Magnesium != null)
            {
                model.SoilNutrientValueType = (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram;
                model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
            }
            else if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.MagnesiumStatus != null || model.PotassiumStatus != null || model.PhosphorusStatus != null))
            {
                model.SoilNutrientValueType = (int)NMP.Commons.Enums.SoilNutrientValueType.Status;
                model.SoilNutrientValueTypeName = Resource.lblAsAStatus;
            }
            else
            {
                model.SoilNutrientValueType = (int)NMP.Commons.Enums.SoilNutrientValueType.Index;
                model.SoilNutrientValueTypeName = Resource.lblIndexValues;
            }
        }

        private static void BindPotassiumIndexValueForChangeSoilAnalysis(SoilAnalysisViewModel model, SoilAnalysis soilAnalysis)
        {
            if (soilAnalysis.PotassiumIndex != null)
            {
                string potassiumIndex = soilAnalysis.PotassiumIndex.ToString();
                if (potassiumIndex == Resource.lblMinusTwo)
                {
                    model.PotassiumIndexValue = Resource.lblTwoMinus;
                }
                else if (potassiumIndex == Resource.lblPlusTwo)
                {
                    model.PotassiumIndexValue = Resource.lblTwoPlus;
                }
                else
                {
                    model.PotassiumIndexValue = potassiumIndex;
                }
            }
        }

        [HttpGet]
        public IActionResult Date()
        {
            _logger.LogTrace("Soil Analysis Controller: Date() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();

            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in Date() action.");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Date(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: Date() post action called.");

            ValidateDate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            SetSoilAnalysisDataToSession(model);

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                if (model.FarmRB209CountryID != (int)NMP.Commons.Enums.RB209Country.Scotland)
                {
                    return RedirectToAction(_soilNutrientValueTypeActionName);
                }
                else
                {
                    return RedirectToAction("SoilAnalysesMethod");
                }
            }

            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        private void ValidateDate(SoilAnalysisViewModel model)
        {
            if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
            {
                var dateError = ModelState["Date"]?.Errors.Count > 0 ?
                                ModelState["Date"]?.Errors[0].ErrorMessage.ToString() : null;


                if (dateError != null && (dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblTheDate)) ||
                    dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, Resource.lblTheDate))))
                {
                    ModelState["Date"]?.Errors.Clear();
                    ModelState["Date"]?.Errors.Add(Resource.MsgTheDateMustInclude);
                }
            }

            if (model.Date == null)
            {
                ModelState.AddModelError("Date", Resource.MsgEnterADateBeforeContinuing);
            }

            if (DateTime.TryParseExact(model.Date.ToString(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                ModelState.AddModelError("Date", Resource.MsgEnterTheDateInNumber);
            }

            ValidateMinMaxDate(model);
        }

        private void ValidateMinMaxDate(SoilAnalysisViewModel model)
        {
            if (model.Date != null)
            {
                if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
                {
                    if (model.Date.Value.Date.Year < 1601 || model.Date.Value.Date >= DateTime.Now.AddDays(1).Date)
                    {
                        ModelState.AddModelError("Date", Resource.lblTheDateCannotBeInTheFuture);
                    }
                }
                else
                {
                    if (model.Date.Value.Date.Year < 1601 || model.Date.Value.Date.Year > DateTime.Now.AddYears(1).Year)
                    {
                        ModelState.AddModelError("Date", Resource.MsgEnterTheDateInNumber);
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> SoilNutrientValueType()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValueType() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SoilNutrientValueType() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }

            return await Task.FromResult(View(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNutrientValueType(SoilAnalysisViewModel model)
        {
            _logger.LogTrace("Soil Analysis Controller: SoilNutrientValueType() post action called.");
            if (model.SoilNutrientValueType == null)
            {
                ModelState.AddModelError("SoilNutrientValueType", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            SoilAnalysisViewModel? soilAnalysisViewModel = GetSoilAnalysisFromSession();

            if (soilAnalysisViewModel == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SoilNutrientValueType() post action.");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }

            if (model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.Value == (int)NMP.Commons.Enums.SoilNutrientValueType.Index &&
                (soilAnalysisViewModel.SoilNutrientValueType.HasValue && soilAnalysisViewModel.SoilNutrientValueType.Value != (int)NMP.Commons.Enums.SoilNutrientValueType.Index))
            {
                model.Magnesium = null;
                model.Potassium = null;
                model.Phosphorus = null;
            }

            if (model.SoilNutrientValueType != soilAnalysisViewModel.SoilNutrientValueType)
            {
                model.IsSoilNutrientValueTypeChange = true;
            }


            model = SoilAnalysisNutrientValuesLogic.BindSoilNutrientValueType(model);

            SetSoilAnalysisDataToSession(model);
            if (soilAnalysisViewModel.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.Value != soilAnalysisViewModel.SoilNutrientValueType.Value)
            {
                return RedirectToAction(_soilNutrientValueActionName);
            }
            if (model.IsCheckAnswer && !model.IsSoilAnalysesMethodChange)
            {
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction(_soilNutrientValueActionName);
            }


            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }



        [HttpGet]
        public async Task<IActionResult> SoilNutrientValue()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValue() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SoilNutrientValue() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }

            if (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
            {
                if (model.PotassiumIndexValue == Resource.lblMinusTwo)
                {
                    model.PotassiumIndexValue = Resource.lblTwoMinus;
                }
                else if (model.PotassiumIndexValue.ToString() == Resource.lblPlusTwo)
                {
                    model.PotassiumIndexValue = Resource.lblTwoPlus;
                }
            }

            if (model.PhosphorusMethodologyID == (int)NMP.Commons.Enums.PhosphorusMethodology.Sac)
            {
                await BindViewbegForSoilNutrientValue(model);
            }

            SetSoilAnalysisDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNutrientValue(SoilAnalysisViewModel model)
        {
            _logger.LogTrace("Soil Analysis Controller: SoilNutrientValue() post action called.");
            Error? error = null;
            try
            {

                ValidateSoilNutrientValueProperties(model);

                if (!ModelState.IsValid)
                {
                    await BindViewbegForSoilNutrientValue(model);

                    return View(model);
                }

                BindMethodologyIds(model);

                if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram)
                {
                    if (model.Phosphorus != null || model.Potassium != null ||
                   model.Magnesium != null)
                    {
                        _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Field/Nutrients called.");
                        (bool flowControl, IActionResult? value) = await BindIndexOrStatus(model, error);
                        if (!flowControl && value != null)
                        {
                            return value;
                        }

                    }
                }
                else if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                {
                    ClearNutrientValues(model);
                }
                else if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Status)
                {
                    ClearNutrientValues(model);
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Soil Analysis Controller : Exception in SoilNutrientValue() post action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                ViewBag.Error = string.Concat(error, ex.Message);
                return View(model);
            }
            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);

            SetSoilAnalysisDataToSession(model);

            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        private async Task<(bool flowControl, IActionResult? value)> BindIndexOrStatus(SoilAnalysisViewModel model, Error? error)
        {
            (List<NutrientResponseWrapper> nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                ViewBag.Error = error.Message;
                return (flowControl: false, value: View(model));
            }

            int phosphorusId = 1;
            int potassiumId = 2;
            int magnesiumId = 3;
            bool flowControl = true;
            IActionResult? value = null;
            if (model.Phosphorus != null)
            {
                (flowControl, value) = await BindPhosphorusStatusAndIndex(model, error, nutrients, phosphorusId);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: View(model));
                }
            }
            if (model.Magnesium != null)
            {
                (flowControl, value) = await BindMagnesiumStatusAndIndex(model, error, nutrients, magnesiumId);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: View(model));
                }
            }
            if (model.Potassium != null)
            {
                (flowControl, value) = await BindPotassiumStatusAndIndex(model, error, nutrients, potassiumId);
                if (!flowControl && value != null)
                {
                    return (flowControl: false, value: View(model));
                }
            }

            return (flowControl: true, value: null);
        }

        private async Task<(bool flowControl, IActionResult? value)> BindMagnesiumStatusAndIndex(SoilAnalysisViewModel model, Error? error, List<NutrientResponseWrapper> nutrients, int magnesiumId)
        {
            var magnesiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
            if (magnesiumNutrient != null)
            {
                magnesiumId = magnesiumNutrient.nutrientId;
            }
            (string MagnesiumIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(magnesiumId, model.Magnesium, model.MagnesiumMethodologyID.Value, model.FarmRB209CountryID.Value);

            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                ViewBag.Error = error.Message;
                return (flowControl: false, value: View(model));
            }
            if (model.PhosphorusMethodologyID == (int)NMP.Commons.Enums.PhosphorusMethodology.Sac)
            {
                model.MagnesiumStatus = MagnesiumIndexValue;
            }
            else
            {
                model.MagnesiumIndex = Convert.ToInt32(MagnesiumIndexValue.Trim());
            }

            return (flowControl: true, value: default);
        }

        private async Task<(bool flowControl, IActionResult? value)> BindPhosphorusStatusAndIndex(SoilAnalysisViewModel model, Error? error, List<NutrientResponseWrapper> nutrients, int phosphorusId)
        {
            var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
            if (phosphorusNutrient != null)
            {
                phosphorusId = phosphorusNutrient.nutrientId;
            }

            (string PhosphorusIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(phosphorusId, model.Phosphorus.Value, model.PhosphorusMethodologyID.Value, model.FarmRB209CountryID.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                ViewBag.Error = error.Message;
                return (flowControl: false, value: View(model));
            }
            if (model.PhosphorusMethodologyID == (int)NMP.Commons.Enums.PhosphorusMethodology.Sac)
            {
                model.PhosphorusStatus = PhosphorusIndexValue;
            }
            else
            {
                model.PhosphorusIndex = Convert.ToInt32(PhosphorusIndexValue.Trim());
            }

            return (flowControl: true, value: default);
        }

        private async Task<(bool flowControl, IActionResult? value)> BindPotassiumStatusAndIndex(SoilAnalysisViewModel model, Error? error, List<NutrientResponseWrapper> nutrients, int potassiumId)
        {
            var potassiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
            if (potassiumNutrient != null)
            {
                potassiumId = potassiumNutrient.nutrientId;
            }

            (string PotassiumIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(potassiumId, model.Potassium, model.PotassiumMethodologyID.Value, model.FarmRB209CountryID.Value);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                ViewBag.Error = error.Message;
                return (flowControl: false, value: View(model));
            }
            if (model.PhosphorusMethodologyID == (int)NMP.Commons.Enums.PhosphorusMethodology.Sac)
            {
                model.PotassiumStatus = PotassiumIndexValue.Trim();
            }
            else
            {
                model.PotassiumIndexValue = PotassiumIndexValue.Trim();
            }

            return (flowControl: true, value: default);
        }

        private static void BindMethodologyIds(SoilAnalysisViewModel model)
        {
            if (model.FarmRB209CountryID != (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                model.PhosphorusMethodologyID = (int)NMP.Commons.Enums.PhosphorusMethodology.Olsens;
            }
            model.PotassiumMethodologyID = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland ? model.PhosphorusMethodologyID : (int)PotassiumMethodology.None;
            model.MagnesiumMethodologyID = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland ? model.PhosphorusMethodologyID : (int)MagnesiumMethodology.None;
        }

        private void ValidateSoilNutrientValueProperties(SoilAnalysisViewModel model)
        {

            if (model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
            {
                ValidateModelErrorForSoilNutrientValueIndex(model);
            }
            else if (model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram)
            {
                ValidateModelErrorForSoilNutrientValueMilligram(model);
            }
            else if (IsAllNutrientValuesEmpty(model))
            {
                ViewData["IsPostRequest"] = true;
                ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
            }
            ValidateOrganicMatter(model);
        }

        private bool IsAllNutrientValuesEmpty(SoilAnalysisViewModel model)
        {
            return (model.FarmRB209CountryID.HasValue && model.FarmRB209CountryID.Value == (int)NMP.Commons.Enums.RB209Country.Scotland
                                && (ModelState.IsValid && model.PhosphorusStatus == null && model.PotassiumStatus == null &&
                                   model.MagnesiumStatus == null && model.PH == null));



        }

        private void ValidateOrganicMatter(SoilAnalysisViewModel model)
        {
            if (model.OrganicMatterPercentage != null)
            {
                var value = model.OrganicMatterPercentage.Value;

                if (value < 0 || value > 100 || decimal.Round(value, 1) != value)
                {
                    ModelState.AddModelError("OrganicMatterPercentage", string.Format(Resource.MsgEnterANumberFrom0To100With1Decimal, 0, 100));
                }
            }
        }

        private void ValidateModelErrorForSoilNutrientValueMilligram(SoilAnalysisViewModel model)
        {
            ValidatePotassium();
            ValidatePhosphorus();
            ValidateMagnesium();

            if (model.Phosphorus != null)
            {
                if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland
                && (ModelState.ContainsKey(_phosphorusKey) && Math.Round(model.Phosphorus.Value, 1) != model.Phosphorus))
                {
                    ModelState.AddModelError(_phosphorusKey, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithOneDecimalPlaces, 0, 999));
                }
                else if (model.FarmRB209CountryID != (int)NMP.Commons.Enums.RB209Country.Scotland && ModelState.ContainsKey(_phosphorusKey) &&
        model.Phosphorus.HasValue &&
        model.Phosphorus.Value % 1 != 0)
                {
                    ModelState.AddModelError(_phosphorusKey, string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 999));
                }
            }
            if (ModelState.IsValid && model.PH == null && model.Potassium == null &&
                model.Phosphorus == null && model.Magnesium == null)
            {
                ViewData["IsPostRequest"] = true;
                ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
            }
        }

        private void ValidateMagnesium()
        {
            if (!ModelState.IsValid)
            {
                var magnesiumkey = "Magnesium";

                if (ModelState.TryGetValue(magnesiumkey, out var entry) && entry.Errors.Count > 0)
                {
                    var errorMessage = entry.Errors[0].ErrorMessage;

                    if (errorMessage == string.Format(Resource.lblEnterNumericValue, entry.AttemptedValue, Resource.lblMagnesiumPerLitreOfSoil))
                    {
                        entry.Errors.Clear();
                        entry.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblMagnesiumMg, 0, 9998));
                    }
                }
            }
        }

        private void ValidatePhosphorus()
        {
            if (!ModelState.IsValid)
            {
                var phosphoruskey = _phosphorusKey;

                if (ModelState.TryGetValue(phosphoruskey, out var entry) && entry.Errors.Count > 0)
                {
                    var errorMessage = entry.Errors[0].ErrorMessage;

                    if (errorMessage == string.Format(Resource.lblEnterNumericValue, entry.AttemptedValue, Resource.lblPhosphorusPerLitreOfSoil))
                    {
                        entry.Errors.Clear();
                        entry.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPhosphorusP, 0, 999));
                    }
                }
            }
        }

        private void ValidatePotassium()
        {
            if (!ModelState.IsValid)
            {
                var potassiumkey = "Potassium";

                if (ModelState.TryGetValue(potassiumkey, out var entry) && entry.Errors.Count > 0)
                {
                    var errorMessage = entry.Errors[0].ErrorMessage;

                    if (errorMessage == string.Format(Resource.lblEnterNumericValue, entry.AttemptedValue, Resource.lblPotassiumPerLitreOfSoil))
                    {
                        entry.Errors.Clear();
                        entry.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPotassium, 0, 9998));
                    }
                }
            }
        }

        private void ValidateModelErrorForSoilNutrientValueIndex(SoilAnalysisViewModel model)
        {
            int potassiumIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
            if (!string.IsNullOrEmpty(model.PotassiumIndexValue))
            {
                ValidatePotassiumIndexValue(model, potassiumIndexMaxValue);
            }

            int phosphorusIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
            if (model.PhosphorusIndex.HasValue && (model.PhosphorusIndex > phosphorusIndexMaxValue || model.PhosphorusIndex < 0))
            {
                ModelState.AddModelError(_phosphorusIndex, string.Format(Resource.MsgEnterValidValueForNutrientIndex, phosphorusIndexMaxValue));
            }
            int magnesiumIndexIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
            if (model.MagnesiumIndex.HasValue && (model.MagnesiumIndex > phosphorusIndexMaxValue || model.PhosphorusIndex < 0))
            {
                ModelState.AddModelError(_magnesiumIndex, string.Format(Resource.MsgEnterValidValueForNutrientIndex, magnesiumIndexIndexMaxValue));
            }

            if (ModelState.IsValid && model.PH == null && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                model.PhosphorusIndex == null && model.MagnesiumIndex == null)
            {
                ViewData["IsPostRequest"] = true;
                ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
            }
            BindErrorForPhosphorusIndex();
            BindErrorForMagnissium();
        }

        private void BindErrorForMagnissium()
        {
            if (!ModelState.IsValid)
            {
                var key = _magnesiumIndex;

                if (ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0)
                {
                    var errorMessage = entry.Errors[0].ErrorMessage;

                    if (errorMessage == string.Format(Resource.lblEnterNumericValue, entry.AttemptedValue, Resource.lblMagnesiumIndex))
                    {
                        entry.Errors.Clear();
                        entry.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblMagnesiumMg, 0, 9));
                    }
                }
            }
        }

        private void BindErrorForPhosphorusIndex()
        {
            if (!ModelState.IsValid)
            {
                var phosphorusIndexkey = _phosphorusIndex;

                if (ModelState.TryGetValue(phosphorusIndexkey, out var entry) && entry.Errors.Count > 0)
                {
                    var errorMessage = entry.Errors[0].ErrorMessage;

                    if (errorMessage == string.Format(Resource.lblEnterNumericValue, entry.AttemptedValue, Resource.lblPhosphorusIndex))
                    {
                        entry.Errors.Clear();
                        entry.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPhosphorusP, 0, 9));
                    }
                }
            }
        }

        private void ValidatePotassiumIndexValue(SoilAnalysisViewModel model, int potassiumIndexMaxValue)
        {
            string potassiumIndex = model.PotassiumIndexValue.Replace(" ", "");
            if (int.TryParse(potassiumIndex, out int value))
            {
                if (value > potassiumIndexMaxValue || value < 0)
                {
                    ModelState.AddModelError(_potassiumIndexValue, string.Format(Resource.MsgEnterValidValueForNutrientIndex, potassiumIndexMaxValue));
                }
                if (value == 2)
                {
                    ModelState.AddModelError(_potassiumIndexValue, string.Format(Resource.MsgValueIsNotAValidValueForPotassium, value));
                }
            }
            else
            {
                if ((potassiumIndex.ToString() != Resource.lblTwoMinus) &&
                                       (potassiumIndex.ToString() != Resource.lblTwoPlus))
                {
                    ModelState.AddModelError(_potassiumIndexValue, Resource.MsgValidationForPotasium);
                }
            }
        }

        private async Task BindViewbegForSoilNutrientValue(SoilAnalysisViewModel model)
        {
            if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland)
            {
                var (nutrients, _) = await _fieldLogic.FetchNutrientsAsync();

                var (statusList, _) = await _soilLogic
                    .FetchSoilNutrientStatusList(model.PhosphorusMethodologyID.Value);
                if (statusList != null)
                {
                    SoilAnalysisNutrientValuesLogic soilAnalysisNutrientValuesLogic = new SoilAnalysisNutrientValuesLogic();
                    ViewBag.PhosphorusSelectList = soilAnalysisNutrientValuesLogic.BindViewBagForScotlandNutrient(statusList, nutrients, Resource.lblPhosphate, 1);
                    ViewBag.PotassiumSelectList = soilAnalysisNutrientValuesLogic.BindViewBagForScotlandNutrient(statusList, nutrients, Resource.lblPotash, 2);
                    ViewBag.MagnesiumSelectList = soilAnalysisNutrientValuesLogic.BindViewBagForScotlandNutrient(statusList, nutrients, Resource.lblMagnesium, 3);
                }
            }
        }
        private static void ClearNutrientValues(SoilAnalysisViewModel model)
        {
            if (model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
            {
                model.Phosphorus = null;
                model.Magnesium = null;
                model.Potassium = null;
                model.MagnesiumStatus = null;
                model.PhosphorusStatus = null;
                model.PotassiumStatus = null;
            }
            else if (model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Status)
            {
                model.Phosphorus = null;
                model.Magnesium = null;
                model.Potassium = null;
                model.PhosphorusIndex = null;
                model.PhosphorusIndex = null;
                model.PhosphorusIndex = null;
            }
            else if (model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram)
            {
                model.PhosphorusStatus = null;
                model.PotassiumStatus = null;
                model.MagnesiumStatus = null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> SulphurDeficient()
        {
            _logger.LogTrace($"Soil Analysis Controller: SulphurDeficient() action called.");
            SoilAnalysisViewModel model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SulphurDeficient() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SulphurDeficient(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SulphurDeficient() post action called.");

            if (model.SulphurDeficient == null)
            {
                ModelState.AddModelError("SulphurDeficient", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return await Task.FromResult(View(model));
            }

            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            SetSoilAnalysisDataToSession(model);

            if (model.IsCheckAnswer)
            {
                return await Task.FromResult(RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged }));
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return await Task.FromResult(RedirectToAction("Date"));
            }

            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSoil(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: UpdateSoil() post action called.");
            try
            {
                ValidateSoilAnalysis(model);

                await FetchMethodologyName(model);
                if (!ModelState.IsValid)
                {
                    ViewData["ModelStateErrors"] = ModelState;
                    return View(_changeSoilAnalysisActionName, model);
                }

                await BindPkBalanceData(model);

                BindPotassiumIndexForUpdateSoil(model);


                model.Year = model.Date.Value.Month >= 8 ? model.Date.Value.Year + 1 : model.Date.Value.Year;

                var soilData = new
                {
                    SoilAnalysis = new SoilAnalysis
                    {
                        Year = model.Year,
                        SulphurDeficient = model.SulphurDeficient,
                        Date = model.Date,
                        PH = model.PH,
                        PhosphorusMethodologyID = model.PhosphorusMethodologyID,
                        Phosphorus = model.Phosphorus,
                        PhosphorusIndex = model.PhosphorusIndex,
                        Potassium = model.Potassium,
                        PotassiumIndex = model.PotassiumIndex,
                        Magnesium = model.Magnesium,
                        MagnesiumIndex = model.MagnesiumIndex,
                        SoilNitrogenSupply = model.SoilNitrogenSupply,
                        SoilNitrogenSupplyIndex = model.SoilNitrogenSupplyIndex,
                        SoilNitrogenSampleDate = null,
                        Sodium = model.Sodium,
                        Lime = model.Lime,
                        PhosphorusStatus = model.PhosphorusStatus,
                        PotassiumMethodologyID = model.PotassiumMethodologyID,
                        PotassiumStatus = model.PotassiumStatus,
                        MagnesiumMethodologyID = model.MagnesiumMethodologyID,
                        MagnesiumStatus = model.MagnesiumStatus,
                        NitrogenResidueGroup = model.NitrogenResidueGroup,
                        OrganicMatterPercentage = model.OrganicMatterPercentage,
                        Comments = model.Comments,
                        PreviousID = model.PreviousID,
                        FieldID = model.FieldID
                    },
                    PKBalance = model.PKBalance != null ? model.PKBalance : null
                };
                string jsonData = string.Empty;
                Error? error = null;
                SoilAnalysis? soilAnalysis = null;
                if (model.isSoilAnalysisAdded == null)
                {
                    int soilAnalysisId = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedSoilAnalysisId));
                    jsonData = JsonConvert.SerializeObject(soilData);
                    (soilAnalysis, error) = await _soilAnalysisLogic.UpdateSoilAnalysisAsync(soilAnalysisId, jsonData);
                }
                else
                {
                    jsonData = JsonConvert.SerializeObject(soilData);
                    (soilAnalysis, error) = await _soilAnalysisLogic.AddSoilAnalysisAsync(jsonData);
                }

                string success = string.Empty;
                if (string.IsNullOrWhiteSpace(error?.Message) && soilAnalysis != null)
                {
                    success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                }
                else
                {
                    success = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                    if (model.isSoilAnalysisAdded == null)
                    {
                        TempData[_changeSoilAnalysisError] = Resource.MsgSoilAnalysisChangesCouldNotSaved;
                        return View(_changeSoilAnalysisActionName, model);
                    }
                }

                return RedirectToAction(_fieldSoilAnalysisDetailAction, _fieldController, new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value) ? _soilAnalysisDataProtector.Protect(Resource.lblAdd) : _soilAnalysisDataProtector.Protect(Resource.lblUpdate) });
            }
            catch (Exception ex)
            {
                TempData[_changeSoilAnalysisError] = ex.Message;
                return View(_changeSoilAnalysisActionName, model);
            }
        }

        private async Task BindPkBalanceData(SoilAnalysisViewModel model)
        {
            if (model.Potassium != null || model.Phosphorus != null ||
               (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) || model.PhosphorusIndex != null)
            {
                PKBalance pKBalance = await _pKBalanceLogic.FetchPKBalanceByYearAndFieldId(model.Date.Value.Year, model.FieldID.Value);
                if (pKBalance == null)
                {
                    model.PKBalance = new PKBalance();
                    model.PKBalance.PBalance = 0;
                    model.PKBalance.KBalance = 0;
                    model.PKBalance.Year = model.Date.Value.Year;
                    model.PKBalance.FieldID = model.FieldID;
                }
            }
        }

        private static void BindPotassiumIndexForUpdateSoil(SoilAnalysisViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
            {
                model.PotassiumIndexValue = model.PotassiumIndexValue.Replace(" ", "");
                if (model.PotassiumIndexValue == Resource.lblTwoMinus)
                {
                    model.PotassiumIndex = Convert.ToInt32(Resource.lblMinusTwo);
                }
                else if (model.PotassiumIndexValue == Resource.lblTwoPlus)
                {
                    model.PotassiumIndex = Convert.ToInt32(Resource.lblPlusTwo);
                }
                else
                {
                    model.PotassiumIndex = Convert.ToInt32(model.PotassiumIndexValue.Trim());
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsSoilAnalysisAdded(string i, string j)
        {
            _logger.LogTrace($"Soil Analysis Controller: IsSoilAnalysisAdded() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            model.isSoilAnalysisAdded = true;
            if (!string.IsNullOrWhiteSpace(i) && !string.IsNullOrWhiteSpace(j))
            {
                model.EncryptedFarmId = j;
                (FarmResponse? farm, Error? error) = await _farmLogic.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(j)));
                if (string.IsNullOrWhiteSpace(error?.Message) && farm != null)
                {
                    model.FarmRB209CountryID = farm.RB209CountryID;
                }
                int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(i));
                var field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                if (field != null)
                {
                    model.EncryptedFieldId = i;
                    model.FieldName = field.Name;
                    model.FieldID = fieldId;
                }
            }

            SetSoilAnalysisDataToSession(model);
            return RedirectToAction("SulphurDeficient");
        }

        [HttpGet]
        public async Task<IActionResult> RemoveSoilAnalysis()
        {
            _logger.LogTrace($"Soil Analysis Controller: RemoveSoilAnalysis() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in RemoveSoilAnalysis() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSoilAnalysis(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: RemoveSoilAnalysis() post action called.");
            if (model.SoilAnalysisRemove == null)
            {
                ModelState.AddModelError("SoilAnalysisRemove", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("RemoveSoilAnalysis", model);
            }

            if (model.SoilAnalysisRemove.HasValue && model.SoilAnalysisRemove.Value)
            {
                int soilAnalysisId = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedSoilAnalysisId));
                (string success, Error? error) = await _soilAnalysisLogic.DeleteSoilAnalysisByIdAsync(soilAnalysisId);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    _ = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                    TempData["RemoveSoilAnalysisError"] = error.Message;
                    return View(model);
                }
                success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                return RedirectToAction(_fieldSoilAnalysisDetailAction, _fieldController, new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = _soilAnalysisDataProtector.Protect(Resource.lblRemove) });
            }
            else
            {
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancel()
        {
            _logger.LogTrace("SoilAnalysis Controller : Cancel() action called");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in Cancel() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }
            try
            {
                model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                SetSoilAnalysisDataToSession(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SoilAnalysis Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_changeSoilAnalysisError] = ex.Message;
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(SoilAnalysisViewModel model)
        {
            _logger.LogTrace("SoilAnalysis Controller : Cancel() post action called");

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
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
            else
            {
                RemoveSoilAnalysisDataFromSession();
                return RedirectToAction(_fieldSoilAnalysisDetailAction, _fieldController, new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId });
            }
        }

        [HttpGet]
        public IActionResult BackActionForCheckAnswer()
        {
            _logger.LogTrace("SoilAnalysis Controller : BackActionForCheckAnswer() action called");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("Soil Analysis Controller: Session expired in BackActionForCheckAnswer action.");
                return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
            }
            try
            {
                model.IsCheckAnswer = false;
                SetSoilAnalysisDataToSession(model);

                if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
                {
                    return RedirectToAction(_soilNutrientValueActionName);
                }
                else
                {
                    return RedirectToAction(_fieldSoilAnalysisDetailAction, _fieldController, new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SoilAnalysis Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_changeSoilAnalysisError] = ex.Message;
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
        }

        private async Task<Error?> FetchMethologies(SoilAnalysisViewModel model)
        {
            var (nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
            if (nutrients != null && error == null)
            {
                var nutrientId = nutrients.FirstOrDefault(n => n.nutrient.Equals(Resource.lblPhosphate))?.nutrientId ?? 0;
                (List<SoilMethologiesResponse>? soilMethologiesList, _) = await _soilLogic.FetchSoilMethodologies(nutrientId, model.FarmRB209CountryID.Value);
                if (soilMethologiesList != null && soilMethologiesList.Count > 0)
                {
                    var selectListItems = soilMethologiesList.OrderBy(x => x.methodology).Select(f => new SelectListItem
                    {
                        Value = f.methodologyId.ToString(),
                        Text = f.methodology
                    }).ToList();
                    ViewBag.SoilMethologiesList = selectListItems;

                }
            }
            return error;
        }
        private void ValidateSoilAnalysisMethod(int? methodId, string key)
        {
            if (methodId == null)
            {
                ModelState.AddModelError(key, Resource.MsgSelectAnOptionBeforeContinuing);
            }
        }
        private async Task<IActionResult> ReturnViewWithMethods(SoilAnalysisViewModel model)
        {
            await FetchMethologies(model);
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> SoilAnalysesMethod()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilAnalysesMethod() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SoilAnalysesMethod() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }
            Error? error = await FetchMethologies(model);
            if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["SoilDateError"] = error.Message;
                return RedirectToAction("SoilDate");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilAnalysesMethod(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilAnalysesMethod() post action called.");

            ValidateSoilAnalysisMethod(model.PhosphorusMethodologyID, "PhosphorusMethodologyID");

            if (!ModelState.IsValid)
            {
                await ReturnViewWithMethods(model);
            }
            SoilAnalysisViewModel? soilAnalysisViewModel = GetSoilAnalysisFromSession();
            if (soilAnalysisViewModel != null && soilAnalysisViewModel.PhosphorusMethodologyID == model.PhosphorusMethodologyID)
            {
                model.IsSoilAnalysesMethodChange = false;
                SetSoilAnalysisDataToSession(model);
                return await Task.FromResult(RedirectToAction("ChangeSoilAnalysis"));
            }
            else
            {
                model.IsSoilAnalysesMethodChange = true;
                model.SoilNutrientValueTypeName = null;
                ClearNutrientValues(model);
                model.SoilNutrientValueType = null;
                SetSoilAnalysisDataToSession(model);
            }
            return HandleSoilAnalysisRedirect(model, _soilNutrientValueTypeActionName);
        }

        private IActionResult HandleSoilAnalysisRedirect(SoilAnalysisViewModel model, string nextAction)
        {
            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            SetSoilAnalysisDataToSession(model);

            if (model.IsSoilAnalysesMethodChange)
            {
                return RedirectToAction(nextAction);
            }

            return RedirectToAction(_changeSoilAnalysisActionName,
                new
                {
                    i = model.EncryptedSoilAnalysisId,
                    j = model.EncryptedFieldId,
                    k = model.EncryptedFarmId,
                    l = model.IsSoilDataChanged
                });


        }
        private async Task FetchMethodologyName(SoilAnalysisViewModel model)
        {
            int phosphorusId = 1;
            (List<NutrientResponseWrapper> nutrients, Error? error) = await _fieldLogic.FetchNutrientsAsync();
            if (nutrients != null && nutrients.Count > 0)
            {
                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                if (phosphorusNutrient != null)
                {
                    phosphorusId = phosphorusNutrient.nutrientId;
                }
                (SoilMethologiesResponse? soilMethology, error) = await _soilLogic.FetchSoilMethodologyNameByNutrientIdAndMethodologyId(phosphorusId, model.PhosphorusMethodologyID ?? 0);
                if (soilMethology != null && error == null)
                {
                    ViewBag.MethodologyName = soilMethology.methodology;
                }
            }
        }
    }
}