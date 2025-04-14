using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Globalization;
using System.Reflection;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class SoilAnalysisController : Controller
    {
        private readonly ILogger<FarmController> _logger;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _soilAnalysisDataProtector;
        private readonly IDataProtector _fieldDataProtector;
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISoilAnalysisService _soilAnalysisService;
        private readonly ISoilService _soilService;
        private readonly IPKBalanceService _pKBalanceService;
        public SoilAnalysisController(ILogger<FarmController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor,
             IUserFarmService userFarmService, IFarmService farmService, ISoilService soilService,
            IFieldService fieldService, ISoilAnalysisService soilAnalysisService, IPKBalanceService pKBalanceService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
            _soilAnalysisDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.SoilAnalysisController");
            _userFarmService = userFarmService;
            _farmService = farmService;
            _soilService = soilService;
            _fieldService = fieldService;
            _soilAnalysisService = soilAnalysisService;
            _pKBalanceService = pKBalanceService;
        }

        //[HttpGet]
        //public async Task<IActionResult> SoilAnalysisDetail(string i, string j, string l, string k)//i=EncryptedFieldId,j=EncryptedFarmId,l=EncryptedSoilAnalysisId,k=success
        //{
        //    _logger.LogTrace($"Soil Analysis Controller: SoilAnalysisDetail({i}, {j},{k}) action called.");
        //    SoilAnalysisViewModel model = new SoilAnalysisViewModel();
        //    try
        //    {
        //        if (!string.IsNullOrWhiteSpace(i))
        //        {

        //            (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(j)));
        //            if (string.IsNullOrWhiteSpace(error.Message))
        //            {
        //                int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(i));
        //                var field = await _fieldService.FetchFieldByFieldId(fieldId);
        //                model.FieldName = field.Name;
        //                model.EncryptedFieldId = i;
        //                model.EncryptedFarmId = j;
        //                model.FarmName = farm.Name;
        //                _logger.LogTrace($"SoilAnalysisController: soil-analyses/fields/{fieldId}?shortSummary={Resource.lblFalse} called.");
        //                if (!string.IsNullOrWhiteSpace(l))
        //                {
        //                    string soilAnalysisId = _fieldDataProtector.Unprotect(l);
        //                    (SoilAnalysis soilAnalysis, error) = await _soilAnalysisService.FetchSoilAnalysisById(Convert.ToInt32(soilAnalysisId));
        //                    if (soilAnalysis != null && error == null)
        //                    {

        //                        model.PhosphorusMethodology = Enum.GetName(
        //                           typeof(PhosphorusMethodology), soilAnalysis.PhosphorusMethodologyID);
        //                        //soilAnalysis. = Enum.GetName(
        //                        //    typeof(PhosphorusMethodology), soilAnalysis.PhosphorusMethodologyID);
        //                        //soilAnalysis.EncryptedSoilAnalysisId = _soilAnalysisDataProtector.Protect(soilAnalysis.ID.ToString());
        //                        if (soilAnalysis.PotassiumIndex != null)
        //                        {
        //                            model.PotassiumIndexValue = soilAnalysis.PotassiumIndex.ToString() == Resource.lblMinusTwo ? Resource.lblTwoMinus : (soilAnalysis.PotassiumIndex.ToString() == Resource.lblPlusTwo ? Resource.lblTwoPlus : soilAnalysis.PotassiumIndex.ToString());
        //                        }
        //                        model.Date = soilAnalysis.Date;
        //                        model.Phosphorus=soilAnalysis.Phosphorus;
        //                        model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
        //                        model.PhosphorusIndex = soilAnalysis.PhosphorusIndex;
        //                        model.Potassium = soilAnalysis.Potassium;
        //                        model.PotassiumIndex = soilAnalysis.PotassiumIndex;
        //                        model.Magnesium= soilAnalysis.Magnesium;
        //                        model.MagnesiumIndex= soilAnalysis.MagnesiumIndex;
        //                        model.FieldID= soilAnalysis.FieldID;
        //                        model.SulphurDeficient = soilAnalysis.SulphurDeficient;
        //                        model.PH=soilAnalysis.PH;
        //                        model.EncryptedSoilAnalysisId = l;
        //                        //ViewBag.soilAnalysisList = soilAnalysisResponseList;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                ViewBag.Error = error.Message;
        //                return View(model);
        //            }
        //        }

        //        if (!string.IsNullOrWhiteSpace(k))
        //        {
        //            ViewBag.Success = _soilAnalysisDataProtector.Unprotect(k);
        //            _httpContextAccessor.HttpContext?.Session.Remove("SoilAnalysisData");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogTrace($"Soil Analysis Controller : Exception in SoilAnalysisDetail() action : {ex.Message}, {ex.StackTrace}");
        //        ViewBag.Error = ex.Message;
        //        return View(model);
        //    }
        //    return View(model);
        //}
        [HttpGet]
        public async Task<IActionResult> ChangeSoilAnalysis(string i, string j, string k, string l)//i= soilAnalysisId,j=EncryptedFieldId,k=EncryptedFarmId,l=IsSoilDataChanged
        {
            _logger.LogTrace($"Soil Analysis Controller: ChangeSoilAnalysis({i}, {j},{k}, {l}) action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();

            try
            {
                if (!string.IsNullOrWhiteSpace(l))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(i))
                {
                    _logger.LogTrace($"SoilAnalysisController: farms/{j} called.");
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(k)));
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(j));
                        _logger.LogTrace($"SoilAnalysisController: fields/{fieldId} called.");
                        var field = await _fieldService.FetchFieldByFieldId(fieldId);
                        model.FieldName = field.Name;
                        model.FarmName = farm.Name;
                        model.FieldID = fieldId;
                        int decryptedSoilId = Convert.ToInt32(_fieldDataProtector.Unprotect(i));
                        _logger.LogTrace($"SoilAnalysisController: soil-analyses/{decryptedSoilId} called.");
                        (SoilAnalysis soilAnalysis, error) = await _soilAnalysisService.FetchSoilAnalysisById(decryptedSoilId);
                        if (error == null)
                        {
                            model.Phosphorus = soilAnalysis.Phosphorus;
                            model.PH = soilAnalysis.PH;
                            model.Potassium = soilAnalysis.Potassium;
                            model.Magnesium = soilAnalysis.Magnesium;
                            model.PhosphorusMethodologyID = soilAnalysis.PhosphorusMethodologyID;
                            model.PhosphorusIndex = soilAnalysis.PhosphorusIndex;
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

                        if (model.Phosphorus == null &&
                             model.Potassium == null && model.Magnesium == null)
                        {
                            model.IsSoilNutrientValueTypeIndex = true;// @Resource.lblMiligramValues
                        }
                        else
                        {
                            model.IsSoilNutrientValueTypeIndex = false;// @Resource.lblMiligramValues
                        }
                    }
                    else
                    {
                        TempData["ChangeSoilAnalysisError"] = error.Message;
                        return View(model);
                    }
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
                }
                if (model != null)
                {
                    model.IsCheckAnswer = true;
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Soil Analysis Controller : Exception in ChangeSoilAnalysis() action : {ex.Message}, {ex.StackTrace}");
                TempData["ChangeSoilAnalysisError"] = ex.Message;
                return View(model);
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Date()
        {
            _logger.LogTrace($"Soil Analysis Controller: Date() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            //if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            //{
            //    ViewBag.Inprocess = Resource.lblTrue;
            //    //return RedirectToAction("SoilNutrientValueType")
            //}
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Date(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: Date() post action called.");

            if ((!ModelState.IsValid) && ModelState.ContainsKey("Date"))
            {
                var dateError = ModelState["Date"].Errors.Count > 0 ?
                                ModelState["Date"].Errors[0].ErrorMessage.ToString() : null;

                //if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDateSampleTaken)))
                //{
                //    ModelState["Date"].Errors.Clear();
                //    ModelState["Date"].Errors.Add(Resource.MsgEnterTheDateInNumber);
                //}
                if (dateError != null && (dateError.Equals(Resource.MsgDateMustBeARealDate) ||
                    dateError.Equals(Resource.MsgDateMustIncludeAMonth) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAMonthAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeAYear) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADay) ||
                     dateError.Equals(Resource.MsgDateMustIncludeADayAndMonth)))
                {
                    ModelState["Date"].Errors.Clear();
                    ModelState["Date"].Errors.Add(Resource.MsgTheDateMustInclude);
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction("SoilNutrientValueType");
            }
            return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpGet]
        public async Task<IActionResult> SoilNutrientValueType()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValueType() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoilNutrientValueType(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValueType() post action called.");
            if (model.IsSoilNutrientValueTypeIndex == null)
            {
                ModelState.AddModelError("IsSoilNutrientValueTypeIndex", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            SoilAnalysisViewModel soilAnalysisViewModel = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                soilAnalysisViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model.IsSoilNutrientValueTypeIndex.Value)
            {
                if (soilAnalysisViewModel != null && soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.HasValue && (!soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.Value))
                {
                    model.Magnesium = null;
                    model.Potassium = null;
                    model.Phosphorus = null;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            if (soilAnalysisViewModel != null)
            {
                if (soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.HasValue && model.IsSoilNutrientValueTypeIndex.Value != soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.Value)
                {
                    return RedirectToAction("SoilNutrientValue");
                }
            }
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction("SoilNutrientValue");
            }
            return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpGet]
        public async Task<IActionResult> SoilNutrientValue()
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValue() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoilNutrientValue(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilNutrientValue() post action called.");
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
                            if (value == 2)
                            {
                                ModelState.AddModelError("PotassiumIndexValue", string.Format(Resource.MsgValueIsNotAValidValueForPotassium, value));
                            }
                        }
                        else
                        {
                            if ((model.PotassiumIndexValue.ToString() != Resource.lblTwoMinus) &&
                                                   (model.PotassiumIndexValue.ToString() != Resource.lblTwoPlus))
                            {
                                ModelState.AddModelError("PotassiumIndexValue", Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                            }
                        }


                    }
                    if (model.PH == null && string.IsNullOrWhiteSpace(model.PotassiumIndexValue) &&
                        model.PhosphorusIndex == null && model.MagnesiumIndex == null)
                    {
                        ModelState.AddModelError("Date", Resource.MsgEnterAtLeastOneValue);
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("PhosphorusIndex"))
                    {
                        var InvalidFormatError = ModelState["PhosphorusIndex"].Errors.Count > 0 ?
                                        ModelState["PhosphorusIndex"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["PhosphorusIndex"].AttemptedValue, Resource.lblPhosphorusIndex)))
                        {
                            ModelState["PhosphorusIndex"].Errors.Clear();
                            ModelState["PhosphorusIndex"].Errors.Add(Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                        }
                    }



                    if ((!ModelState.IsValid) && ModelState.ContainsKey("MagnesiumIndex"))
                    {
                        var InvalidFormatError = ModelState["MagnesiumIndex"].Errors.Count > 0 ?
                                        ModelState["MagnesiumIndex"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["MagnesiumIndex"].AttemptedValue, Resource.lblMagnesiumIndex)))
                        {
                            ModelState["MagnesiumIndex"].Errors.Clear();
                            ModelState["MagnesiumIndex"].Errors.Add(Resource.MsgTheValueMustBeAnIntegerValueBetweenZeroAndNine);
                        }
                    }
                }
                else
                {
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Potassium"))
                    {
                        var InvalidFormatError = ModelState["Potassium"].Errors.Count > 0 ?
                                        ModelState["Potassium"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Potassium"].AttemptedValue, Resource.lblPotassiumPerLitreOfSoil)))
                        {
                            ModelState["Potassium"].Errors.Clear();
                            ModelState["Potassium"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Phosphorus"))
                    {
                        var InvalidFormatError = ModelState["Phosphorus"].Errors.Count > 0 ?
                                        ModelState["Phosphorus"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Phosphorus"].AttemptedValue, Resource.lblPhosphorusPerLitreOfSoil)))
                        {
                            ModelState["Phosphorus"].Errors.Clear();
                            ModelState["Phosphorus"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("Magnesium"))
                    {
                        var InvalidFormatError = ModelState["Magnesium"].Errors.Count > 0 ?
                                        ModelState["Magnesium"].Errors[0].ErrorMessage.ToString() : null;

                        if (InvalidFormatError != null && InvalidFormatError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["Magnesium"].AttemptedValue, Resource.lblMagnesiumPerLitreOfSoil)))
                        {
                            ModelState["Magnesium"].Errors.Clear();
                            ModelState["Magnesium"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmount));
                        }
                    }
                    if (model.PH == null && model.Potassium == null &&
                        model.Phosphorus == null && model.Magnesium == null)
                    {
                        ModelState.AddModelError("CropType", Resource.MsgEnterAtLeastOneValue);
                    }

                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.PhosphorusMethodologyID = (int)PhosphorusMethodology.Olsens;


                if (model.IsSoilNutrientValueTypeIndex != null && (!model.IsSoilNutrientValueTypeIndex.Value))
                {
                    if (model.Phosphorus != null || model.Potassium != null ||
                   model.Magnesium != null)
                    {
                        _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Field/Nutrients called.");
                        (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
                        if (error == null && nutrients.Count > 0)
                        {
                            int phosphorusId = 1;
                            int potassiumId = 2;
                            int magnesiumId = 3;

                            _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{phosphorusId}/{model.Phosphorus}/{(int)PhosphorusMethodology.Olsens} called.");
                            _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{magnesiumId}/{model.Magnesium}/{(int)MagnesiumMethodology.None} called.");
                            _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{potassiumId}/{model.Potassium}/{(int)PotassiumMethodology.None} called.");
                            if (model.Phosphorus != null)
                            {
                                var phosphorusNutrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                                if (phosphorusNutrient != null)
                                {
                                    phosphorusId = phosphorusNutrient.nutrientId;
                                }
                                (string PhosphorusIndexValue, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.Phosphorus, (int)PhosphorusMethodology.Olsens);
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
                                (string MagnesiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.Magnesium, (int)MagnesiumMethodology.None);
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
                                (string PotassiumIndexValue, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.Potassium, (int)PotassiumMethodology.None);
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
                _logger.LogTrace($"Soil Analysis Controller : Exception in SoilNutrientValue() post action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = string.Concat(error, ex.Message);
                return View(model);
            }
            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            //if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            //{
            //    return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = string.Empty });
            //}
            return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpGet]
        public IActionResult SulphurDeficient()
        {
            _logger.LogTrace($"Soil Analysis Controller: SulphurDeficient() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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
                return View(model);
            }
            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
            if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
            {
                return RedirectToAction("Date");
            }
            return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSoil(SoilAnalysisViewModel model)
        {
            _logger.LogTrace($"Soil Analysis Controller: UpdateSoil() post action called.");
            if (model.IsSoilNutrientValueTypeIndex.HasValue)
            {

                if (!model.IsSoilNutrientValueTypeIndex.Value)
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
                return View("ChangeSoilAnalysis", model);
            }

            if (model.Potassium != null || model.Phosphorus != null ||
               (!string.IsNullOrWhiteSpace(model.PotassiumIndexValue)) || model.PhosphorusIndex != null)
            {
                PKBalance pKBalance = await _pKBalanceService.FetchPKBalanceByYearAndFieldId(model.Date.Value.Year, model.FieldID.Value);
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
                _logger.LogTrace($"SoilAnalysisController: soil-analyses/{soilAnalysisId}/{jsonData} called.");
                (soilAnalysis, error) = await _soilAnalysisService.UpdateSoilAnalysisAsync(soilAnalysisId, jsonData);
            }
            else
            {
                jsonData = JsonConvert.SerializeObject(soilData);
                _logger.LogTrace($"SoilAnalysisController: soil-analyses/{jsonData} called.");
                (soilAnalysis, error) = await _soilAnalysisService.AddSoilAnalysisAsync(jsonData);
            }



            string success = string.Empty;
            if (error.Message == null && soilAnalysis != null)
            {
                success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                //if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
                //{
                //  return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis) });
                //}
                //return RedirectToAction("SoilAnalysisDetail", new { i = model.EncryptedFieldId, j = model.EncryptedFarmId, k = success, l = model.EncryptedSoilAnalysisId });
            }
            else
            {
                success = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                if (model.isSoilAnalysisAdded == null)
                {
                    TempData["ChangeSoilAnalysisError"] = Resource.MsgSoilAnalysisChangesCouldNotSaved;
                    return View("ChangeSoilAnalysis", model);
                }
            }

            return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value) ? _soilAnalysisDataProtector.Protect(Resource.lblAdd) : _soilAnalysisDataProtector.Protect(Resource.lblUpdate) });
            //return RedirectToAction("SoilAnalysisDetail", new { i = model.EncryptedFieldId, j = model.EncryptedFarmId, k = success ,l=model.EncryptedSoilAnalysisId});
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
                int fieldId = Convert.ToInt32(_fieldDataProtector.Unprotect(i));
                var field = await _fieldService.FetchFieldByFieldId(fieldId);
                if (field != null)
                {
                    model.EncryptedFieldId = i;
                    model.FieldName = field.Name;
                    model.FieldID = fieldId;
                }

            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            return RedirectToAction("SulphurDeficient", model);
        }
        [HttpGet]
        public IActionResult RemoveSoilAnalysis()
        {
            _logger.LogTrace($"Soil Analysis Controller: RemoveSoilAnalysis() action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("SoilAnalysisData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
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

            if (model.SoilAnalysisRemove.Value)
            {
                int soilAnalysisId = Convert.ToInt32(_fieldDataProtector.Unprotect(model.EncryptedSoilAnalysisId));

                _logger.LogTrace($"SoilAnalysisController: soil-analyses/{soilAnalysisId} called.");
                (string success, Error error) = await _soilAnalysisService.DeleteSoilAnalysisByIdAsync(soilAnalysisId);

                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    success = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
                    TempData["RemoveSoilAnalysisError"] = error.Message;
                    return View(model);
                }
                success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId, q = success, r = _fieldDataProtector.Protect(Resource.lblSoilAnalysis), s = _soilAnalysisDataProtector.Protect(Resource.lblRemove) });
            }
            else
            {
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }
            //return RedirectToAction("SoilAnalysisDetail", new { i = model.EncryptedFieldId, j = model.EncryptedFarmId, k = success ,l=model.EncryptedSoilAnalysisId});
        }
        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("SoilAnalysis Controller : Cancel() action called");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            try
            {
                if (HttpContext.Session.Keys.Contains("SoilAnalysisData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SoilAnalysis Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["ChangeSoilAnalysisError"] = ex.Message;
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
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
            if (!model.IsCancel.Value)
            {
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });

            }
            else
            {
                HttpContext?.Session.Remove("SoilAnalysisData");
                return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId });
            }

        }
        [HttpGet]
        public IActionResult BackActionForCheckAnswer()
        {
            _logger.LogTrace("SoilAnalysis Controller : BackActionForCheckAnswer() action called");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            try
            {
                if (HttpContext.Session.Keys.Contains("SoilAnalysisData"))
                {
                    model = HttpContext.Session.GetObjectFromJson<SoilAnalysisViewModel>("SoilAnalysisData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsCheckAnswer = false;
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
                if (model.isSoilAnalysisAdded != null && model.isSoilAnalysisAdded.Value)
                {
                    return RedirectToAction("SoilNutrientValue");
                }
                else
                {
                    return RedirectToAction("FieldSoilAnalysisDetail", "Field", new { id = model.EncryptedFieldId, farmId = model.EncryptedFarmId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"SoilAnalysis Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["ChangeSoilAnalysisError"] = ex.Message;
                return RedirectToAction("ChangeSoilAnalysis", new { i = model.EncryptedSoilAnalysisId, j = model.EncryptedFieldId, k = model.EncryptedFarmId, l = model.IsSoilDataChanged });
            }

            return View(model);
        }
    }
}