using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
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
        private SoilAnalysisViewModel? GetSoilAnalysisFromSession()
        {
            if (HttpContext.Session.Exists("SoilAnalysisData"))
            {
                return HttpContext.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            return null;
        }

        private void SetSoilAnalysisDataToSession(SoilAnalysisViewModel plan)
        {
            HttpContext.Session.SetObjectAsJson("SoilAnalysisData", plan);
        }

        private void RemoveSoilAnalysisDataFromSession()
        {
            if (HttpContext.Session.Exists("SoilAnalysisData"))
            {
                HttpContext.Session.Remove("SoilAnalysisData");
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
                    if (model.Phosphorus == null && model.Potassium == null && model.Magnesium == null)
                    {
                        if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.MagnesiumStatus != null || model.PotassiumStatus != null || model.PhosphorusStatus != null))
                        {
                            model.SoilNutrientValueTypeName = Resource.lblAsAStatus;
                        }
                        else
                        {
                            model.SoilNutrientValueTypeName = Resource.lblIndexValues;
                        }
                    }
                    else
                    {
                        model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(i))
                {
                    Error? error;
                    _logger.LogTrace("SoilAnalysisController: farms/{J} called.", j);
                    (FarmResponse farm, error) = await _farmLogic.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(k)));

                    if (string.IsNullOrWhiteSpace(error?.Message))
                    {
                        model.FarmRB209CountryID = farm?.RB209CountryID;
                        int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(j));
                        _logger.LogTrace("SoilAnalysisController: fields/{FieldId} called.", fieldId);
                        var field = await _fieldLogic.FetchFieldByFieldId(fieldId);
                        model.FieldName = field.Name;
                        model.FarmName = farm?.Name;
                        model.FieldID = fieldId;
                        int decryptedSoilId = Convert.ToInt32(_fieldDataProtector.Unprotect(i));
                        _logger.LogTrace("SoilAnalysisController: soil-analyses/{DecryptedSoilId} called", decryptedSoilId);

                        (SoilAnalysis soilAnalysis, error) = await _soilAnalysisLogic.FetchSoilAnalysisById(decryptedSoilId);

                        if (error == null)
                        {
                            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                            model.Phosphorus = soilAnalysis.Phosphorus;
                            model.PH = soilAnalysis.PH;
                            model.Potassium = soilAnalysis.Potassium;
                            model.Magnesium = soilAnalysis.Magnesium;
                            model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
                            model.PhosphorusIndex = soilAnalysis.PhosphorusIndex;
                            model.OrganicMatterPercentage = soilAnalysis.OrganicMatterPercentage;
                            if (soilAnalysis.PotassiumIndex != null)
                            {
                                model.PotassiumIndexValue = soilAnalysis.PotassiumIndex.ToString() == Resource.lblMinusTwo ? Resource.lblTwoMinus : (soilAnalysis.PotassiumIndex.ToString() == Resource.lblPlusTwo ? Resource.lblTwoPlus : soilAnalysis.PotassiumIndex.ToString());
                            }

                            model.PotassiumIndex = soilAnalysis.PotassiumIndex;
                            model.MagnesiumIndex = soilAnalysis.MagnesiumIndex;
                            model.Date = soilAnalysis.Date.Value.ToLocalTime();
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
                        }
                        else
                        {
                            ViewBag.Error = error.Message;
                            return View(model);
                        }

                        model.SoilAnalysesMethodID = soilAnalysis.SoilAnalysesMethodID;

                        if (model.Phosphorus == null &&
                             model.Potassium == null && model.Magnesium == null)
                        {
                            if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.MagnesiumStatus != null || model.PotassiumStatus != null || model.PhosphorusStatus != null))
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
                        else
                        {
                            model.SoilNutrientValueType = (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram;// @Resource.lblMiligramValues
                            model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
                        }
                    }
                    else
                    {
                        TempData[_changeSoilAnalysisError] = error.Message;
                        return View(model);
                    }

                    SetSoilAnalysisDataToSession(model);
                }

                if (model != null)
                {
                    model.IsCheckAnswer = true;
                    SetSoilAnalysisDataToSession(model);

                    if (!string.IsNullOrWhiteSpace(i) && string.IsNullOrWhiteSpace(l))
                    {
                        HttpContext.Session.SetObjectAsJson("SoilAnalysisDataBeforeUpdate", model);
                    }

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
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Soil Analysis Controller : Exception in ChangeSoilAnalysis() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_changeSoilAnalysisError] = ex.Message;
                return View(model);
            }
            return View(model);
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

            if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
            {
                var dateError = ModelState["Date"]?.Errors.Count > 0 ?
                                ModelState["Date"]?.Errors[0].ErrorMessage.ToString() : null;


                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblTheDate)) ||
                    dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonth, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAMonthAndYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeAYear, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADay, Resource.lblTheDate)) ||
                     dateError.Equals(string.Format(Resource.MsgDateMustIncludeADayAndMonth, Resource.lblTheDate)))
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
            

            SetSoilAnalysisDataToSession(model);
            if (soilAnalysisViewModel.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.Value != soilAnalysisViewModel.SoilNutrientValueType.Value)
            {
                return RedirectToAction("SoilNutrientValue");
            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction("SoilNutrientValue");
            }
            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpGet]
        public async Task<IActionResult> SoilNutrientValue()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValue() action called.");
            SoilAnalysisViewModel model = GetSoilAnalysisFromSession();
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

            SetSoilAnalysisDataToSession(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNutrientValue(SoilAnalysisViewModel model)
        {
            _logger.LogTrace("Soil Analysis Controller: SoilNutrientValue() post action called.");
            Error error = null;
            try
            {
                if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                {
                    if (!string.IsNullOrEmpty(model.PotassiumIndexValue))
                    {
                        string potassiumIndex = model.PotassiumIndexValue.Replace(" ", "");
                        if (int.TryParse(potassiumIndex, out int value))
                        {
                            if (value > 9 || value < 0)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgEnterValidValueForNutrientIndex);
                            }
                            if (value == 2)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", string.Format(Resource.MsgValueIsNotAValidValueForPotassium, value));
                            }
                        }
                        else
                        {
                            if ((potassiumIndex.ToString() != Resource.lblTwoMinus) &&
                                                   (potassiumIndex.ToString() != Resource.lblTwoPlus))
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgValidationForPotasium);
                            }
                        }


                    }
                    if (ModelState.IsValid && model.PH == null && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                        model.PhosphorusIndex == null && model.MagnesiumIndex == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("PhosphorusIndex"))
                    {
                        var InvalidFormatError = ModelState["PhosphorusIndex"]?.Errors.Count > 0 ?
                                        ModelState["PhosphorusIndex"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["PhosphorusIndex"]?.AttemptedValue, Resource.lblPhosphorusIndex)))
                        {
                            ModelState["PhosphorusIndex"]?.Errors.Clear();
                            ModelState["PhosphorusIndex"]?.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPhosphorusP, 0, 9));
                        }
                    }



                    if ((!ModelState.IsValid) && ModelState.ContainsKey("MagnesiumIndex"))
                    {
                        var InvalidFormatError = ModelState["MagnesiumIndex"]?.Errors.Count > 0 ?
                                        ModelState["MagnesiumIndex"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["MagnesiumIndex"].AttemptedValue, Resource.lblMagnesiumIndex)))
                        {
                            ModelState["MagnesiumIndex"]?.Errors.Clear();
                            ModelState["MagnesiumIndex"]?.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblMagnesiumMg, 0, 9));
                        }
                    }
                }
                else
                {
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Potassium"))
                    {
                        var InvalidFormatError = ModelState["Potassium"]?.Errors.Count > 0 ?
                                        ModelState["Potassium"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Potassium"].AttemptedValue, Resource.lblPotassiumPerLitreOfSoil)))
                        {
                            ModelState["Potassium"]?.Errors.Clear();
                            ModelState["Potassium"]?.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPotassium, 0, 9998));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Phosphorus"))
                    {
                        var InvalidFormatError = ModelState["Phosphorus"]?.Errors.Count > 0 ?
                                        ModelState["Phosphorus"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Phosphorus"].AttemptedValue, Resource.lblPhosphorusPerLitreOfSoil)))
                        {
                            ModelState["Phosphorus"]?.Errors.Clear();
                            ModelState["Phosphorus"]?.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblPhosphorusP, 0, 999));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Magnesium"))
                    {
                        var InvalidFormatError = ModelState["Magnesium"]?.Errors.Count > 0 ?
                                        ModelState["Magnesium"]?.Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Magnesium"]?.AttemptedValue, Resource.lblMagnesiumPerLitreOfSoil)))
                        {
                            ModelState["Magnesium"]?.Errors.Clear();
                            ModelState["Magnesium"]?.Errors.Add(string.Format(Resource.MsgForNotValidValueForNutrient, Resource.lblMagnesiumMg, 0, 9998));
                        }
                    }
                    if (ModelState.IsValid && model.PH == null && model.Potassium == null &&
                        model.Phosphorus == null && model.Magnesium == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }

                }
                if (model.OrganicMatterPercentage != null)
                {
                    if (model.OrganicMatterPercentage < 0 || model.OrganicMatterPercentage > 100)
                    {
                        ModelState.AddModelError("OrganicMatterPercentage", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 100));
                    }

                    if (model.OrganicMatterPercentage.Value % 1 != 0)
                    {
                        ModelState.AddModelError("OrganicMatterPercentage", string.Format(Resource.MsgEnterAnAmountBetweenXAndYWithNoDecimalPlaces, 0, 100));
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.PhosphorusMethodologyID = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland ? (int)PhosphorusMethodology.Resin : (int)PhosphorusMethodology.Olsens;


                if (model.SoilNutrientValueType != null && model.SoilNutrientValueType != (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                {
                    if (model.Phosphorus != null || model.Potassium != null ||
                   model.Magnesium != null)
                    {
                        _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Field/Nutrients called.");
                        (List<NutrientResponseWrapper> nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
                        if (error == null && nutrients.Count > 0)
                        {
                            int phosphorusId = 1;
                            int potassiumId = 2;
                            int magnesiumId = 3;

                            if (model.Phosphorus != null)
                            {
                                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                                if (phosphorusNutrient != null)
                                {
                                    phosphorusId = phosphorusNutrient.nutrientId;
                                }

                                (string PhosphorusIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(phosphorusId, model.Phosphorus, model.PhosphorusMethodologyID.Value);
                                if (!string.IsNullOrWhiteSpace(PhosphorusIndexValue) && error == null)
                                {
                                    model.PhosphorusIndex = Convert.ToInt32(PhosphorusIndexValue.Trim());

                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                            if (model.Magnesium != null)
                            {
                                var magnesiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                                if (magnesiumNutrient != null)
                                {
                                    magnesiumId = magnesiumNutrient.nutrientId;
                                }
                                (string MagnesiumIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(magnesiumId, model.Magnesium, (int)MagnesiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(MagnesiumIndexValue) && error == null)
                                {
                                    model.MagnesiumIndex = Convert.ToInt32(MagnesiumIndexValue.Trim());
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                            if (model.Potassium != null)
                            {
                                var potassiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                                if (potassiumNutrient != null)
                                {
                                    potassiumId = potassiumNutrient.nutrientId;
                                }
                                (string PotassiumIndexValue, error) = await _soilLogic.FetchSoilNutrientIndex(potassiumId, model.Potassium, (int)PotassiumMethodology.None);
                                if (!string.IsNullOrWhiteSpace(PotassiumIndexValue) && error == null)
                                {
                                    model.PotassiumIndexValue = PotassiumIndexValue.Trim();
                                }
                                else if (error != null)
                                {
                                    ViewBag.Error = error.Message;
                                    return View(model);
                                }
                            }
                        }
                        if (error != null && error.Message != null)
                        {
                            ViewBag.Error = error.Message;
                            return View(model);
                        }
                    }
                }
                else
                {
                    model.Phosphorus = null;
                    model.Magnesium = null;
                    model.Potassium = null;
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
                if (model.SoilNutrientValueType.HasValue)
                {
                    if (model.SoilNutrientValueType != (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                    {
                        if (!model.PH.HasValue && !model.Potassium.HasValue &&
                            !model.Phosphorus.HasValue && !model.Magnesium.HasValue)
                        {
                            if (!model.PH.HasValue)
                            {
                                ModelState.AddModelError("PH", Resource.MsgPhNotSet);
                            }
                            if (!model.Potassium.HasValue)
                            {
                                ModelState.AddModelError("Potassium", Resource.MsgPotassiumNotSet);
                            }
                            if (!model.Phosphorus.HasValue)
                            {
                                ModelState.AddModelError("Phosphorus", Resource.MsgPhosphorusNotSet);
                            }
                            if (!model.Magnesium.HasValue)
                            {
                                ModelState.AddModelError("Magnesium", Resource.MsgMagnesiumNotSet);
                            }
                        }
                    }
                    else
                    {
                        if (!model.PH.HasValue && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                            !model.MagnesiumIndex.HasValue && !model.PhosphorusIndex.HasValue)
                        {
                            if (!model.PH.HasValue)
                            {
                                ModelState.AddModelError("PH", Resource.MsgPhNotSet);
                            }

                            if (string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
                            {
                                ModelState.AddModelError("PotassiumIndex", Resource.MsgPotassiumIndexNotSet);
                            }

                            if (!model.PhosphorusIndex.HasValue)
                            {
                                ModelState.AddModelError("PhosphorusIndex", Resource.MsgPhosphorusIndexNotSet);
                            }

                            if (!model.MagnesiumIndex.HasValue)
                            {
                                ModelState.AddModelError("MagnesiumIndex", Resource.MsgMagnesiumIndexNotSet);
                            }
                        }
                    }
                }
                if (!ModelState.IsValid)
                {
                    return View(_changeSoilAnalysisActionName, model);
                }

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
                        PotassiumAnalysis = model.PotassiumAnalysis,
                        PotassiumStatus = model.PotassiumStatus,
                        MagnesiumAnalysis = model.MagnesiumAnalysis,
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

                return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value) ? _soilAnalysisDataProtector.Protect(Resource.lblAdd) : _soilAnalysisDataProtector.Protect(Resource.lblUpdate) });
            }
            catch (Exception ex)
            {
                TempData[_changeSoilAnalysisError] = ex.Message;
                return View(_changeSoilAnalysisActionName, model);
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
                (string success, Error error) = await _soilAnalysisLogic.DeleteSoilAnalysisByIdAsync(soilAnalysisId);

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    _ = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                    TempData["RemoveSoilAnalysisError"] = error.Message;
                    return View(model);
                }
                success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = _soilAnalysisDataProtector.Protect(Resource.lblRemove) });
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
            try
            {
                if (model == null)
                {
                    _logger.LogTrace("SoilAnalysisController: Session expired in Cancel() action.");
                    return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
                }

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
                return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId });
            }
        }

        [HttpGet]
        public IActionResult BackActionForCheckAnswer()
        {
            _logger.LogTrace("SoilAnalysis Controller : BackActionForCheckAnswer() action called");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            try
            {
                if (model == null)
                {
                    _logger.LogTrace("Soil Analysis Controller: Session expired in BackActionForCheckAnswer action.");
                    return Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict);
                }

                model.IsCheckAnswer = false;
                SetSoilAnalysisDataToSession(model);

                if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
                {
                    return RedirectToAction("SoilNutrientValue");
                }
                else
                {
                    return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { farmId = model.EncryptedFarmId, fieldId = model.EncryptedFieldId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "SoilAnalysis Controller : Exception in Cancel() action : {Message}, {StackTrace}", ex.Message, ex.StackTrace);
                TempData[_changeSoilAnalysisError] = ex.Message;
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
        }

        private async Task FetchAllSoilAnalysesMethod()
        {
            (List<CommonResponse>? SoilAnalysesMethodList, Error? error) = await _soilLogic.FetchAllSoilAnalysesMethod();
            if (error == null && SoilAnalysesMethodList != null && SoilAnalysesMethodList.Count > 0)
            {
                var selectListItems = SoilAnalysesMethodList.OrderBy(x => x.Name).Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name
                }).ToList();
                ViewBag.SoilAnalysesMethodList = selectListItems;

            }
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
            await FetchAllSoilAnalysesMethod();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilAnalysesMethod(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilAnalysesMethod() post action called.");

            if (model.SoilAnalysesMethodID == null)
            {
                ModelState.AddModelError("SoilAnalysesMethodID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                await FetchAllSoilAnalysesMethod();
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
                return await Task.FromResult(RedirectToAction(_soilNutrientValueTypeActionName));
            }

            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }
        [HttpGet]
        public async Task<IActionResult> SacMethod()
        {
            _logger.LogTrace($"Soil Analysis Controller: SacMethod() action called.");
            SoilAnalysisViewModel? model = GetSoilAnalysisFromSession();
            if (model == null)
            {
                _logger.LogTrace("SoilAnalysisController: Session expired in SacMethod() action.");
                return await Task.FromResult(Functions.RedirectToErrorHandler((int)System.Net.HttpStatusCode.Conflict));
            }
            await FetchAllSoilAnalysesMethod();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SacMethod(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SacMethod() post action called.");

            if (model.SoilAnalysesMethodID == null)
            {
                ModelState.AddModelError("SoilAnalysesMethodID", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                await FetchAllSoilAnalysesMethod();
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
                return await Task.FromResult(RedirectToAction(_soilNutrientValueTypeActionName));
            }

            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }
    }
}