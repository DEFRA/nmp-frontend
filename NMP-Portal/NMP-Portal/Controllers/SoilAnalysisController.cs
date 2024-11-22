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
            _soilAnalysisDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.SoilAnalysisController");
            _userFarmService = userFarmService;
            _farmService = farmService;
            _soilService = soilService;
            _fieldService = fieldService;
            _soilAnalysisService = soilAnalysisService;
            _pKBalanceService = pKBalanceService;
        }

        [HttpGet]
        public async Task<IActionResult> SoilAnalysisDetail(string i, string j, string k)//i=EncryptedFieldId,j=EncryptedFarmId,k=success
        {
            _logger.LogTrace($"Soil Analysis Controller: SoilAnalysisDetail({i}, {j},{k}) action called.");
            SoilAnalysisViewModel model = new SoilAnalysisViewModel();
            try
            {
                if (!string.IsNullOrWhiteSpace(i))
                {

                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(_farmDataProtector.Unprotect(j)));
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(i));
                        var field = await _fieldService.FetchFieldByFieldId(fieldId);
                        model.FieldName = field.Name;
                        model.EncryptedFieldId = i;
                        model.EncryptedFarmId = j;
                        model.FarmName = farm.Name;
                        _logger.LogTrace($"SoilAnalysisController: soil-analyses/fields/{fieldId}?shortSummary={Resource.lblFalse} called.");
                        List<SoilAnalysisResponse> soilAnalysisResponseList = await _fieldService.FetchSoilAnalysisByFieldId(fieldId, Resource.lblFalse);
                        if (soilAnalysisResponseList.Count > 0)
                        {
                            foreach (var soilAnalysis in soilAnalysisResponseList)
                            {
                                soilAnalysis.PhosphorusMethodology = Enum.GetName(
                                    typeof(PhosphorusMethodology), soilAnalysis.PhosphorusMethodologyID);
                                soilAnalysis.EncryptedSoilAnalysisId = _soilAnalysisDataProtector.Protect(soilAnalysis.ID.ToString());
                            }
                            ViewBag.soilAnalysisList = soilAnalysisResponseList;
                        }
                    }
                    else
                    {
                        ViewBag.Error = error.Message;
                        return View(model);
                    }
                }

                if (!string.IsNullOrWhiteSpace(k))
                {
                    ViewBag.Success = _soilAnalysisDataProtector.Unprotect(k);
                    _httpContextAccessor.HttpContext?.Session.Remove("SoilAnalysisData");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Soil Analysis Controller : Exception in SoilAnalysisDetail() action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View(model);
            }
            return View(model);
        }
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
                        int fieldId = Convert.ToInt32(_farmDataProtector.Unprotect(j));
                        _logger.LogTrace($"SoilAnalysisController: fields/{fieldId} called.");
                        var field = await _fieldService.FetchFieldByFieldId(fieldId);
                        model.FieldName = field.Name;
                        model.FarmName = farm.Name;
                        model.FieldID = fieldId;
                        int decryptedSoilId = Convert.ToInt32(_soilAnalysisDataProtector.Unprotect(i));
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
        public async Task<IActionResult> Date()
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

                if (dateError != null && dateError.Equals(string.Format(Resource.MsgDateMustBeARealDate, Resource.lblDateSampleTaken)))
                {
                    ModelState["Date"].Errors.Clear();
                    ModelState["Date"].Errors.Add(Resource.MsgEnterTheDateInNumber);
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
                if (model.Date.Value.Date.Year < 1601 || model.Date.Value.Date.Year > DateTime.Now.AddYears(1).Year)
                {
                    ModelState.AddModelError("Date", Resource.MsgEnterTheDateInNumber);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);

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
                if (soilAnalysisViewModel != null && (!soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.Value))
                {
                    model.Magnesium = null;
                    model.Potassium = null;
                    model.Phosphorus = null;
                }
            }
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);
            if (soilAnalysisViewModel != null)
            {
                if (model.IsSoilNutrientValueTypeIndex.Value != soilAnalysisViewModel.IsSoilNutrientValueTypeIndex.Value)
                {
                    return RedirectToAction("SoilNutrientValue");
                }
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
                    if (model.PH == null && model.PotassiumIndex == null &&
                        model.PhosphorusIndex == null && model.MagnesiumIndex == null)
                    {
                        ModelState.AddModelError("Date", Resource.MsgEnterAtLeastOneValue);
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

                            if (model.Phosphorus != null)
                            {
                                var phosphorusNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPhosphate));
                                if (phosphorusNuetrient != null)
                                {
                                    phosphorusId = phosphorusNuetrient.nutrientId;
                                }
                                _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{phosphorusId}/{model.Phosphorus}/{(int)PhosphorusMethodology.Olsens} called.");
                                (model.PhosphorusIndex, error) = await _soilService.FetchSoilNutrientIndex(phosphorusId, model.Phosphorus, (int)PhosphorusMethodology.Olsens);
                            }
                            if (model.Magnesium != null)
                            {
                                var magnesiumNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblMagnesium));
                                if (magnesiumNuetrient != null)
                                {
                                    magnesiumId = magnesiumNuetrient.nutrientId;
                                }
                                _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{magnesiumId}/{model.Magnesium}/{(int)MagnesiumMethodology.None} called.");
                                (model.MagnesiumIndex, error) = await _soilService.FetchSoilNutrientIndex(magnesiumId, model.Magnesium, (int)MagnesiumMethodology.None);
                            }
                            if (model.Potassium != null)
                            {
                                var potassiumNuetrient = nutrients.FirstOrDefault(a => a.nutrient.Equals(Resource.lblPotash));
                                if (potassiumNuetrient != null)
                                {
                                    potassiumId = potassiumNuetrient.nutrientId;
                                }
                                _logger.LogTrace($"SoilAnalysisController: vendors/rb209/Soil/NutrientIndex/{potassiumId}/{model.Potassium}/{(int)PotassiumMethodology.None} called.");
                                (model.PotassiumIndex, error) = await _soilService.FetchSoilNutrientIndex(potassiumId, model.Potassium, (int)PotassiumMethodology.None);
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



                // _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Soil Analysis Controller : Exception in SoilNutrientValue() post action : {ex.Message}, {ex.StackTrace}");
                ViewBag.Error = string.Concat(error, ex.Message);
                return View(model);
            }
            model.IsSoilDataChanged = _soilAnalysisDataProtector.Protect(Resource.lblTrue);

            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("SoilAnalysisData", model);

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
                    if (!model.PH.HasValue && !model.PotassiumIndex.HasValue &&
                        !model.MagnesiumIndex.HasValue && !model.PhosphorusIndex.HasValue)
                    {
                        if (!model.PH.HasValue)
                        {
                            ModelState.AddModelError("PH", Resource.MsgPhNotSet);
                        }
                        if (!model.PotassiumIndex.HasValue)
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

            if (model.Potassium!=null||model.Phosphorus!=null)
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

            var soilData = new
            {
                SoilAnalysis = new SoilAnalysis
                {
                    Year = model.Date.Value.Year,
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
            int soilAnalysisId = Convert.ToInt32(_soilAnalysisDataProtector.Unprotect(model.EncryptedSoilAnalysisId));
            string jsonData = JsonConvert.SerializeObject(soilData);
            _logger.LogTrace($"SoilAnalysisController: soil-analyses/{soilAnalysisId}/{jsonData} called.");
            (SoilAnalysis soilAnalysis, Error error) = await _soilAnalysisService.UpdateSoilAnalysisAsync(soilAnalysisId, jsonData);
            string success = string.Empty;
            if (error.Message == null && soilAnalysis != null)
            {
                success = _soilAnalysisDataProtector.Protect(Resource.lblTrue);
            }
            else
            {
                success = _soilAnalysisDataProtector.Protect(Resource.lblFalse);
            }
            return RedirectToAction("SoilAnalysisDetail", new { i = model.EncryptedFieldId, j = model.EncryptedFarmId, k = success });
        }
    }
}
