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
        private void ValidateSoilAnalysisIndexValues(SoilAnalysisViewModel model)
        {
            if (!model.PH.HasValue)
            {
                ModelState.AddModelError("PH", Resource.MsgPhNotSet);
            }
            if (string.IsNullOrWhiteSpace(model.PotassiumIndexValue))
            {
                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgPotassiumIndexNotSet);
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
        private void ValidateSoilAnalysisStatusValues(SoilAnalysisViewModel model)
        {
            if (!model.PH.HasValue)
            {
                ModelState.AddModelError("PH", Resource.MsgPhNotSet);
            }
            if (string.IsNullOrWhiteSpace(model.MagnesiumStatus))
            {
                ModelState.AddModelError("MagnesiumStatus", Resource.MsgPotassiumIndexNotSet);
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
                    //if (model.Phosphorus == null && model.Potassium == null && model.Magnesium == null)
                    //{
                    //    if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.MagnesiumStatus != null || model.PotassiumStatus != null || model.PhosphorusStatus != null))
                    //    {
                    //        model.SoilNutrientValueTypeName = Resource.lblAsAStatus;
                    //    }
                    //    else
                    //    {
                    //        model.SoilNutrientValueTypeName = Resource.lblIndexValues;
                    //    }
                    //}
                    //else
                    //{
                    //    model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
                    //}
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
                            if (soilAnalysis.PotassiumIndex != null)
                            {
                                model.PotassiumIndexValue = soilAnalysis.PotassiumIndex.ToString() == Resource.lblMinusTwo ? Resource.lblTwoMinus : (soilAnalysis.PotassiumIndex.ToString() == Resource.lblPlusTwo ? Resource.lblTwoPlus : soilAnalysis.PotassiumIndex.ToString());
                            }

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
                        }
                        else
                        {
                            ViewBag.Error = error?.Message;
                            return View(model);
                        }


                        model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
                        model.PotassiumMethodologyID = soilAnalysis.PotassiumMethodologyID;
                        model.MagnesiumMethodologyID = soilAnalysis.MagnesiumMethodologyID;

                        model.MagnesiumStatus = soilAnalysis.MagnesiumStatus;
                        model.PhosphorusStatus = soilAnalysis.PhosphorusStatus;
                        model.PotassiumStatus = soilAnalysis.PotassiumStatus;

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
                    model.IsSoilAnalysesMethodChange = false;
                    model.IsSoilNutrientValueTypeChange = false;
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
            await FetchMethodologyName(model);
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

            if (model.SoilNutrientValueType != soilAnalysisViewModel.SoilNutrientValueType)
            {
                model.IsSoilNutrientValueTypeChange = true;
            }

            SetSoilAnalysisDataToSession(model);
            if (soilAnalysisViewModel.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType.Value != soilAnalysisViewModel.SoilNutrientValueType.Value)
            {
                return RedirectToAction(_soilNutrientValueActionName);
            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction(_soilNutrientValueActionName);
            }
            return RedirectToAction(_changeSoilAnalysisActionName, new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        private async Task BindViewBagForScotlandNutrient(SoilAnalysisViewModel model)
        {

            int phosphorusId = 1;
            int potassiumId = 2;
            int magnesiumId = 3;
            Error? error = null;
            (List<NutrientResponseWrapper> nutrients, error) = await _fieldLogic.FetchNutrientsAsync();



            (List<SoilNutrientStatusResponse>? statusList, error) = await _soilLogic.FetchSoilNutrientStatusList(model.PhosphorusMethodologyID.Value);

            List<SelectListItem> phosphorusSelectList = new();
            if (statusList != null && statusList.Any())
            {
                //phosphorus start
                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                if (phosphorusNutrient != null)
                {
                    phosphorusId = phosphorusNutrient.nutrientId;
                }
                phosphorusSelectList = statusList.Where(x => x.nutrientId == phosphorusId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.indexText,
                        Value = x.indexText switch
                        {
                            "Very low (1)" => "VL",
                            "Low (2)" => "L",
                            "Moderate minus (3)" => "-M",
                            "Moderate plus (4)" => "+M",
                            "High (5)" => "H",
                            "Very high (6)" => "VH",
                            _ => x.indexText
                        }
                    })
                    .ToList();
                ViewBag.PhosphorusSelectList = phosphorusSelectList;
                //phosphorus end

                //potassium start
                var potassiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                if (potassiumNutrient != null)
                {
                    potassiumId = potassiumNutrient.nutrientId;
                }

                List<SelectListItem> potassiumSelectList = new();

                potassiumSelectList = statusList.Where(x => x.nutrientId == potassiumId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.indexText,
                        Value = x.indexText switch
                        {
                            "Very low (1)" => "VL",
                            "Low (2)" => "L",
                            "Moderate minus (3)" => "-M",
                            "Moderate plus (4)" => "+M",
                            "High (5)" => "H",
                            "Very high (6)" => "VH",
                            _ => x.indexText
                        }
                    })
                    .ToList();
                ViewBag.PhosphorusSelectList = potassiumSelectList;


                //potassium end

                //magnesium start
                var magnesiumNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                if (magnesiumNutrient != null)
                {
                    magnesiumId = magnesiumNutrient.nutrientId;
                }
                List<SelectListItem> magnesiumSelectList = new();

                magnesiumSelectList = statusList.Where(x => x.nutrientId == magnesiumId)
                .Select(x => new SelectListItem
                {
                    Text = x.indexText,
                    Value = x.indexText switch
                    {
                        "Very low (1)" => "VL",
                        "Low (2)" => "L",
                        "Moderate minus (3)" => "-M",
                        "Moderate plus (4)" => "+M",
                        "High (5)" => "H",
                        "Very high (6)" => "VH",
                        _ => x.indexText
                    }
                })
                .ToList();
                ViewBag.MagnesiumSelectList = magnesiumSelectList;


                //magnesium end
            }


            //magnesium end

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

            if (model.FarmRB209CountryID.HasValue && model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland
                && model.PhosphorusMethodologyID == (int)NMP.Commons.Enums.PhosphorusMethodology.Sac)
            {
                await BindViewBagForScotlandNutrient(model);
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

                if (model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                {
                    int potassiumIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
                    if (!string.IsNullOrEmpty(model.PotassiumIndexValue))
                    {
                        string potassiumIndex = model.PotassiumIndexValue.Replace(" ", "");
                        if (int.TryParse(potassiumIndex, out int value))
                        {
                            if (value > potassiumIndexMaxValue || value < 0)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", string.Format(Resource.MsgEnterValidValueForNutrientIndex, potassiumIndexMaxValue));
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

                    int phosphorusIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
                    if (model.PhosphorusIndex.HasValue && (model.PhosphorusIndex > phosphorusIndexMaxValue || model.PhosphorusIndex < 0))
                    {
                        ModelState.AddModelError("PhosphorusIndex", string.Format(Resource.MsgEnterValidValueForNutrientIndex, phosphorusIndexMaxValue));
                    }
                    int magnesiumIndexIndexMaxValue = model.FarmRB209CountryID.Value != (int)NMP.Commons.Enums.RB209Country.Scotland ? 9 : 4;
                    if (model.MagnesiumIndex.HasValue && (model.MagnesiumIndex > phosphorusIndexMaxValue || model.PhosphorusIndex < 0))
                    {
                        ModelState.AddModelError("MagnesiumIndex", string.Format(Resource.MsgEnterValidValueForNutrientIndex, magnesiumIndexIndexMaxValue));
                    }

                    if (ModelState.IsValid && model.PH == null && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                        model.PhosphorusIndex == null && model.MagnesiumIndex == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }
                    if (!ModelState.IsValid)
                    {
                        var phosphorusIndexkey = "PhosphorusIndex";

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

                    if (!ModelState.IsValid)
                    {
                        var key = "MagnesiumIndex";

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
                else if (model.SoilNutrientValueType.HasValue && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram)
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
                    if (!ModelState.IsValid)
                    {
                        var phosphoruskey = "Phosphorus";

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


                    if (ModelState.IsValid && model.PH == null && model.Potassium == null &&
                        model.Phosphorus == null && model.Magnesium == null)
                    {
                        ViewData["IsPostRequest"] = true;
                        ModelState.AddModelError("FocusFirstEmptyField", Resource.MsgForPhPhosphorusPotassiumMagnesium);
                    }

                }
                else if (model.FarmRB209CountryID.HasValue && model.FarmRB209CountryID.Value == (int)NMP.Commons.Enums.RB209Country.Scotland)
                {
                    if (ModelState.IsValid && model.PhosphorusStatus == null && model.PotassiumStatus == null &&
                       model.MagnesiumStatus == null && model.PH == null)
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
                    if (model.FarmRB209CountryID.HasValue && model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland)
                    {
                        await BindViewBagForScotlandNutrient(model);
                    }
                    return View(model);
                }

                if (model.FarmRB209CountryID != (int)NMP.Commons.Enums.RB209Country.Scotland)
                {
                    model.PhosphorusMethodologyID = (int)NMP.Commons.Enums.PhosphorusMethodology.Olsens;
                }
                model.PotassiumMethodologyID = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland ? model.PhosphorusMethodologyID : (int)PotassiumMethodology.None;
                model.MagnesiumMethodologyID = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland ? model.PhosphorusMethodologyID : (int)MagnesiumMethodology.None;


                if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram)
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
                else if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Index)
                {
                    ClearNutrientValues(model);
                }
                else if (model.SoilNutrientValueType != null && model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Status)
                {
                    ClearNutrientValues(model);
                }
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
                (string success, Error? error) = await _soilAnalysisLogic.DeleteSoilAnalysisByIdAsync(soilAnalysisId);

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
                    return RedirectToAction(_soilNutrientValueActionName);
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

        private async Task FetchMethologies(SoilAnalysisViewModel model)
        {
            var (nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
            if (nutrients != null)
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
            await FetchMethologies(model);
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