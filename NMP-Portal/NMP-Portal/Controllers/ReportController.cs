using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
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
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IDataProtector _reportDataProtector;
        private readonly IDataProtector _farmDataProtector;
        private readonly IAddressLookupService _addressLookupService;
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IOrganicManureService _organicManureService;
        private readonly IFertiliserManureService _fertiliserManureService;
        private readonly IReportService _reportService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ReportController(ILogger<ReportController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IAddressLookupService addressLookupService,
            IUserFarmService userFarmService, IFarmService farmService,
            IFieldService fieldService, ICropService cropService, IOrganicManureService organicManureService,
            IFertiliserManureService fertiliserManureService, IReportService reportService)
        {
            _logger = logger;
            _reportDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.ReportController");
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _addressLookupService = addressLookupService;
            _userFarmService = userFarmService;
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
            _organicManureService = organicManureService;
            _fertiliserManureService = fertiliserManureService;
            _httpContextAccessor = httpContextAccessor;
            _reportService = reportService;
        }
        public IActionResult Index()
        {
            _logger.LogTrace($"Report Controller : Index() action called");
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ExportFieldsOrCropType()
        {
            _logger.LogTrace("Report Controller : ExportFieldsOrCropType() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                Error error = null;
                ViewBag.EncryptedYear = _farmDataProtector.Protect(model.Year.Value.ToString());
                if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                {
                    (error, List<Field> fields) = await _fieldService.FetchFieldByFarmId(model.FarmId.Value, Resource.lblTrue);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (fields.Count > 0)
                        {
                            int fieldCount = 0;
                            foreach (var field in fields)
                            {
                                List<Crop> cropList = await _cropService.FetchCropsByFieldId(field.ID.Value);
                                if (cropList.Count > 0)
                                {
                                    cropList = cropList.Where(x => x.Year == model.Year).ToList();
                                    if (cropList.Count == 0)
                                    {
                                        fieldCount++;
                                    }
                                }
                            }
                            if (fields.Count == fieldCount)
                            {
                                ViewBag.NoPlan = string.Format(Resource.lblYouHaveNotEnteredAnyCropInformation, model.Year);

                            }
                        }
                        else
                        {
                            ViewBag.NoField = Resource.lblYouHaveNotEnteredAnyField;

                        }

                    }
                }
                if (ViewBag.NoPlan == null && ViewBag.NoField == null)
                {
                    if (model.FieldAndPlanReportOption != null && model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                    {
                        (List<HarvestYearPlanResponse> fieldList, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            var SelectListItem = fieldList.Select(f => new SelectListItem
                            {
                                Value = f.FieldID.ToString(),
                                Text = f.FieldName
                            }).ToList();
                            ViewBag.fieldList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                        }
                    }
                    else if (model.NVZReportOption != null && model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.NmaxReport)
                    {
                        (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                        if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                        {
                            (List<HarvestYearPlanResponse> cropTypeList, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                            if (string.IsNullOrWhiteSpace(error.Message) && cropTypeList != null && cropTypeList.Count > 0)
                            {
                                (List<CropTypeLinkingResponse> cropTypeLinking, error) = await _cropService.FetchCropTypeLinking();
                                if (error == null && cropTypeLinking != null && cropTypeLinking.Count > 0)
                                {
                                    if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                    {
                                        cropTypeLinking = cropTypeLinking.Where(x => x.NMaxLimitEngland != null).ToList();
                                    }
                                    else
                                    {
                                        cropTypeLinking = cropTypeLinking.Where(x => x.NMaxLimitWales != null).ToList();
                                    }
                                    cropTypeList = cropTypeList
                                    .Where(crop => cropTypeLinking
                                    .Any(link => link.CropTypeId == crop.CropTypeID))
                                    .DistinctBy(x => x.CropTypeID).ToList();
                                    if (cropTypeList.Count > 0)
                                    {
                                        var SelectListItem = cropTypeList.Select(f => new SelectListItem
                                        {
                                            Value = f.CropTypeID.ToString(),
                                            Text = f.CropTypeName
                                        }).ToList();
                                        ViewBag.CropTypeList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                                    }
                                    else
                                    {

                                        if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                                        {
                                            TempData["ErrorOnYear"] = Resource.lblNoCropTypesAvailable; ;
                                            return View("Year", model);
                                        }
                                        else
                                        {
                                            if (model.ReportOption == (int)NMP.Portal.Enums.ReportOption.FieldRecordsAndPlan)
                                            {
                                                TempData["ErrorFieldAndPlanReports"] = Resource.lblNoCropTypesAvailable; ;
                                                return RedirectToAction("FieldAndPlanReports");
                                            }
                                            else
                                            {
                                                TempData["ErrorNVZComplianceReports"] = Resource.lblNoCropTypesAvailable; ;
                                                return RedirectToAction("NVZComplianceReports");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ExportFieldsOrCropType() action : {ex.Message}, {ex.StackTrace}");
                if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                {
                    TempData["ErrorOnYear"] = ex.Message;
                    return RedirectToAction("Year");
                }
                else
                {
                    if (model.ReportOption == (int)NMP.Portal.Enums.ReportOption.FieldRecordsAndPlan)
                    {
                        TempData["ErrorFieldAndPlanReports"] = ex.Message;
                        return RedirectToAction("FieldAndPlanReports");
                    }
                    else
                    {
                        TempData["ErrorNVZComplianceReports"] = ex.Message;
                        return RedirectToAction("NVZComplianceReports");
                    }

                }
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportFieldsOrCropType(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ExportFieldsOrCropType() post action called");
            try
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                //fetch field
                Error error = null;
                if (model.FieldAndPlanReportOption != null && model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                {
                    (List<HarvestYearPlanResponse> fieldList, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        var selectListItem = fieldList.Select(f => new SelectListItem
                        {
                            Value = f.FieldID.ToString(),
                            Text = f.FieldName
                        }).ToList();

                        if (model.FieldList == null || model.FieldList.Count == 0)
                        {
                            ModelState.AddModelError("FieldList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblField.ToLower()));
                        }
                        if (!ModelState.IsValid)
                        {
                            ViewBag.fieldList = selectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                            return View(model);
                        }
                        if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblSelectAll)
                        {
                            model.FieldList = selectListItem.Select(item => item.Value).ToList();
                        }
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    }
                    return RedirectToAction("CropAndFieldManagement");

                }
                else if (model.NVZReportOption != null && model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.NmaxReport)
                {
                    //fetch crop type
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        (List<HarvestYearPlanResponse> cropTypeList, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            (List<CropTypeLinkingResponse> cropTypeLinking, error) = await _cropService.FetchCropTypeLinking();
                            if (error == null && cropTypeLinking != null && cropTypeLinking.Count > 0)
                            {
                                if (farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England)
                                {
                                    cropTypeLinking = cropTypeLinking.Where(x => x.NMaxLimitEngland != null).ToList();
                                }
                                else
                                {
                                    cropTypeLinking = cropTypeLinking.Where(x => x.NMaxLimitWales != null).ToList();
                                }
                                cropTypeList = cropTypeList
                                .Where(crop => cropTypeLinking
                                .Any(link => link.CropTypeId == crop.CropTypeID))
                                .DistinctBy(x => x.CropTypeID).ToList();
                                var SelectListItem = cropTypeList.Select(f => new SelectListItem
                                {
                                    Value = f.CropTypeID.ToString(),
                                    Text = f.CropTypeName
                                }).ToList();
                                if (model.CropTypeList == null || model.CropTypeList.Count == 0)
                                {
                                    ModelState.AddModelError("CropTypeList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
                                }
                                if (!ModelState.IsValid)
                                {
                                    ViewBag.CropTypeList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                                    return View(model);
                                }
                                if (model.CropTypeList.Count == 1 && model.CropTypeList[0] == Resource.lblSelectAll)
                                {
                                    model.CropTypeList = SelectListItem.Select(item => item.Value).ToList();
                                }
                                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                                ViewBag.CropTypeList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                            }
                            else
                            {
                                TempData["ErrorOnSelectField"] = error != null ? error.Message : null;
                                return View(model);
                            }
                            return RedirectToAction("NMaxReport");
                        }
                        else
                        {
                            TempData["ErrorOnSelectField"] = error.Message;
                            return View(model);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ExportFieldsOrCropType() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSelectField"] = ex.Message;
                return View(model);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CropAndFieldManagement()
        {
            ReportViewModel model = new ReportViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            string fieldIds = string.Join(",", model.FieldList);
            (CropAndFieldReportResponse cropAndFieldReportResponse, Error error) = await _fieldService.FetchCropAndFieldReportById(fieldIds, model.Year.Value);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                model.CropAndFieldReport = cropAndFieldReportResponse;
            }
            else
            {
                TempData["ErrorOnSelectField"] = error.Message;
                return RedirectToAction("ExportFieldsOrCropType");
            }
            (List<NutrientResponseWrapper> nutrients, error) = await _fieldService.FetchNutrientsAsync();
            if (error == null && nutrients.Count > 0)
            {
                model.Nutrients = new List<NutrientResponseWrapper>();
                model.Nutrients = nutrients;
            }
            if (model.CropAndFieldReport != null && model.CropAndFieldReport.Farm != null)
            {
                if (string.IsNullOrWhiteSpace(model.CropAndFieldReport.Farm.CPH))
                {
                    model.CropAndFieldReport.Farm.CPH = Resource.lblNotEntered;
                }
                if (string.IsNullOrWhiteSpace(model.CropAndFieldReport.Farm.BusinessName))
                {
                    model.CropAndFieldReport.Farm.BusinessName = Resource.lblNotEntered;
                }
                model.CropAndFieldReport.Farm.FullAddress = string.Format("{0}, {1} {2}, {3}, {4}", model.CropAndFieldReport.Farm.Address1, model.CropAndFieldReport.Farm.Address2 != null ? model.CropAndFieldReport.Farm.Address2 + "," : string.Empty, model.CropAndFieldReport.Farm.Address3, model.CropAndFieldReport.Farm.Address4, model.CropAndFieldReport.Farm.Postcode);
                int totalCount = 0;
                if ((!string.IsNullOrWhiteSpace(model.CropAndFieldReport.Farm.FullAddress)) && model.CropAndFieldReport.Farm.CountryID != null)
                {
                    model.CropAndFieldReport.Farm.FullAddress += ", " + Enum.GetName(typeof(NMP.Portal.Enums.FarmCountry), model.CropAndFieldReport.Farm.CountryID);
                }
                if (model.CropAndFieldReport.Farm.Fields != null && model.CropAndFieldReport.Farm.Fields.Count > 0)
                {
                    model.CropAndFieldReport.Farm.Fields = model.CropAndFieldReport.Farm.Fields.OrderBy(a => a.Name).ToList();
                    decimal totalFarmArea = 0;

                    int totalGrassArea = 0;
                    int totalArableArea = 0;
                    foreach (var fieldData in model.CropAndFieldReport.Farm.Fields)
                    {
                        List<int> fieldIdsForGrowthClass = new List<int>();
                        fieldIdsForGrowthClass.Add(fieldData.ID.Value);

                        totalFarmArea += fieldData.TotalArea.Value;
                        if (fieldData.Crops != null && fieldData.Crops.Count > 0)
                        {
                            // * fieldData.Crops.Count;
                            foreach (var cropData in fieldData.Crops)
                            {
                                (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropService.FetchGrassGrowthClass(fieldIdsForGrowthClass);
                                if (string.IsNullOrWhiteSpace(error.Message))
                                {

                                    if (cropData.SwardTypeID == (int)NMP.Portal.Enums.SwardType.Grass)
                                    {
                                        cropData.GrowthClass = grassGrowthClasses.FirstOrDefault().GrassGrowthClassName;
                                    }

                                }
                                else
                                {
                                    TempData["ErrorOnSelectField"] = error.Message;
                                    return RedirectToAction("ExportFieldsOrCropType");
                                }
                                totalCount++;
                                if (cropData.CropOrder == 1)
                                {
                                    cropData.SwardManagementName = cropData.SwardManagementName;
                                    cropData.EstablishmentName = cropData.EstablishmentName;
                                    cropData.SwardTypeName = cropData.SwardTypeName;
                                    if (cropData.Establishment != null)
                                    {
                                        if (cropData.Establishment != (int)NMP.Portal.Enums.Season.Autumn &&
                                        cropData.Establishment != (int)NMP.Portal.Enums.Season.Spring)
                                        {
                                            cropData.EstablishmentName = Resource.lblExistingSwards;
                                        }
                                        else if (cropData.Establishment == (int)NMP.Portal.Enums.Season.Spring)
                                        {
                                            cropData.EstablishmentName = Resource.lblSpringSown;
                                        }
                                        //else if (cropData.Establishment == (int)NMP.Portal.Enums.Season.Spring)
                                        //{
                                        //    cropData.EstablishmentName = Resource.lblautumn;
                                        //}
                                    }

                                    //cropData.DefoliationSequenceName = cropData.DefoliationSequenceName;
                                    if (cropData.CropTypeID == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        totalGrassArea += (int)Math.Round(fieldData.TotalArea.Value);
                                    }
                                    else
                                    {
                                        totalArableArea += (int)Math.Round(fieldData.TotalArea.Value);
                                    }
                                }
                                string defolicationName = string.Empty;
                                if (cropData.SwardTypeID != null && cropData.PotentialCut != null && cropData.DefoliationSequenceID != null)
                                {
                                    if ((string.IsNullOrWhiteSpace(defolicationName)) && cropData.CropTypeID == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        (DefoliationSequenceResponse defResponse, Error grassError) = await _cropService.FetchDefoliationSequencesById(cropData.DefoliationSequenceID.Value);
                                        if (grassError == null && defResponse != null)
                                        {
                                            defolicationName = defResponse.DefoliationSequenceDescription;
                                            if (!string.IsNullOrWhiteSpace(defolicationName))
                                            {
                                                List<string> defoliationList = defolicationName
                                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => s.Trim())
                                                .ToList();
                                                cropData.DefoliationSequenceName = ShorthandDefoliationSequence(defoliationList);
                                            }
                                        }
                                    }
                                }
                                int defIndex = 0;
                                var defolicationParts = (!string.IsNullOrWhiteSpace(defolicationName)) ? defolicationName.Split(',') : null;
                                if (cropData.ManagementPeriods != null)
                                {

                                    foreach (var manData in cropData.ManagementPeriods)
                                    {
                                        string part = (defolicationParts != null && defIndex < defolicationParts.Length) ? defolicationParts[defIndex].Trim() : string.Empty;
                                        string defoliationSequenceName = (!string.IsNullOrWhiteSpace(part)) ? char.ToUpper(part[0]).ToString() + part.Substring(1) : string.Empty;
                                        if (defolicationParts != null)
                                        {
                                            manData.DefoliationSequenceName = defoliationSequenceName;// (defolicationParts != null && defIndex < defolicationParts.Length) ? char.ToUpper(defolicationParts[defIndex][0]) + defolicationParts[defIndex].Substring(1) : string.Empty;
                                        }
                                        if (manData.Recommendation != null)
                                        {
                                            manData.Recommendation.LimeIndex = manData.Recommendation.PH;
                                            manData.Recommendation.CropLime = (manData.Recommendation.PreviousAppliedLime != null && manData.Recommendation.PreviousAppliedLime > 0) ? manData.Recommendation.PreviousAppliedLime : manData.Recommendation.CropLime;
                                            manData.Recommendation.KIndex = manData.Recommendation.KIndex != null ? (manData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (manData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : manData.Recommendation.KIndex)) : null;
                                        }
                                        defIndex++;
                                    }
                                }
                            }
                        }
                        //manData.Recommendation.KIndex != null ? (manData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (manData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : manData.Recommendation.KIndex)) : null;
                        if (fieldData.SoilAnalysis != null)
                        {
                            if (fieldData.SoilAnalysis != null)
                            {
                                fieldData.SoilAnalysis.PotassiumIndex = fieldData.SoilAnalysis.PotassiumIndex != null ? (fieldData.SoilAnalysis.PotassiumIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (fieldData.SoilAnalysis.PotassiumIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : fieldData.SoilAnalysis.PotassiumIndex)) : null;
                            }

                        }
                    }
                    model.CropAndFieldReport.Farm.GrassArea = totalGrassArea;
                    model.CropAndFieldReport.Farm.ArableArea = totalArableArea;
                    model.CropAndFieldReport.Farm.TotalFarmArea = totalFarmArea;
                    ViewBag.TotalCount = totalCount;
                }
            }
            _logger.LogTrace("Report Controller : CropAndFieldManagement() post action called");
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ReportType(string i, string? j)
        {
            _logger.LogTrace("Report Controller : ReportType() action called");
            ReportViewModel model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = new ReportViewModel();
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else if (string.IsNullOrWhiteSpace(i) && string.IsNullOrWhiteSpace(j))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model == null)
                {
                    model = new ReportViewModel();
                    if (!(string.IsNullOrWhiteSpace(i) && string.IsNullOrWhiteSpace(j)))
                    {

                        model.EncryptedFarmId = i;
                        model.EncryptedHarvestYear = j;
                        model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId.ToString()));
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear.ToString()));
                        (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                        if (farm != null)
                        {
                            model.FarmName = farm.Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ReportType() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportType(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ReportType() post action called");
            try
            {
                if (model.ReportType == null)
                {
                    ModelState.AddModelError("ReportType", Resource.MsgSelectTheFarmInformationAndPlanningReportYouWantToCreate);
                }
                if (!ModelState.IsValid)
                {
                    return View("ReportType", model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                //if (model.ReportType != null && model.ReportType == (int)NMP.Portal.Enums.ReportType.CropAndFieldManagementReport)
                //{
                if (model.Year != null)
                {
                    return RedirectToAction("ExportFieldsOrCropType");
                }
                else
                {
                    return RedirectToAction("Year");
                }
                //}
                //else
                //{
                //    return RedirectToAction("ExportCrops");
                //}
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ReportType() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnReportSelection"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> NMaxReport()
        {
            _logger.LogTrace("Report Controller : NMaxReport() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.NMaxLimitReport = new List<NMaxReportResponse>();
                Error error = null;
                (model.Farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                if (model.Farm != null && string.IsNullOrWhiteSpace(error.Message))
                {
                    List<string> nmaxReportCropType = model.CropTypeList;
                    (HarvestYearResponseHeader harvestYearPlanResponse, error) = await _cropService.FetchHarvestYearPlansDetailsByFarmId(model.Year.Value, model.FarmId.Value);
                    if (harvestYearPlanResponse != null && error.Message == null && harvestYearPlanResponse.CropDetails != null && harvestYearPlanResponse.CropDetails.Count > 0)
                    {
                        var vegetableGroup1 = new List<int>
                            {
                                (int)NMP.Portal.Enums.CropTypes.Asparagus,
                                (int)NMP.Portal.Enums.CropTypes.Carrots,
                                (int)NMP.Portal.Enums.CropTypes.Radish,
                                (int)NMP.Portal.Enums.CropTypes.Swedes
                            };
                        var vegetableGroup2 = new List<int>
                            {
                                (int)NMP.Portal.Enums.CropTypes.CelerySelfBlanching,
                                (int)NMP.Portal.Enums.CropTypes.Courgettes,
                                (int)NMP.Portal.Enums.CropTypes.DwarfBeans,
                                (int)NMP.Portal.Enums.CropTypes.Lettuce,
                                (int)NMP.Portal.Enums.CropTypes.BulbOnions,
                                (int)NMP.Portal.Enums.CropTypes.SaladOnions,
                                (int)NMP.Portal.Enums.CropTypes.Parsnips,
                                (int)NMP.Portal.Enums.CropTypes.RunnerBeans,
                                (int)NMP.Portal.Enums.CropTypes.Sweetcorn,
                                (int)NMP.Portal.Enums.CropTypes.Turnips
                            };
                        var vegetableGroup3 = new List<int>
                            {
                                (int)NMP.Portal.Enums.CropTypes.Beetroot,
                                (int)NMP.Portal.Enums.CropTypes.BrusselSprouts,
                                (int)NMP.Portal.Enums.CropTypes.Cabbage,
                                (int)NMP.Portal.Enums.CropTypes.Calabrese,
                                (int)NMP.Portal.Enums.CropTypes.Cauliflower,
                                (int)NMP.Portal.Enums.CropTypes.Leeks
                            };


                        List<string> vegetableGroup1List = new List<string>();
                        List<string> vegetableGroup2List = new List<string>();
                        List<string> vegetableGroup3List = new List<string>();

                        foreach (string cropTypeId in nmaxReportCropType)
                        {
                            if (vegetableGroup1.Contains(Convert.ToInt32(cropTypeId)))
                            {
                                vegetableGroup1List.Add(cropTypeId);
                            }
                            if (vegetableGroup2.Contains(Convert.ToInt32(cropTypeId)))
                            {
                                vegetableGroup2List.Add(cropTypeId);
                            }
                            if (vegetableGroup3.Contains(Convert.ToInt32(cropTypeId)))
                            {
                                vegetableGroup3List.Add(cropTypeId);
                            }
                        }

                        bool isVegetableCropType = false;
                        foreach (string cropType in nmaxReportCropType)
                        {
                            string cropTypeName = string.Empty;
                            int nMaxLimit = 0;
                            string vegetableGroup = string.Empty;
                            List<NitrogenApplicationsForNMaxReportResponse> nitrogenApplicationsForNMaxReportResponse = new List<NitrogenApplicationsForNMaxReportResponse>();
                            List<NMaxLimitReportResponse> nMaxLimitReportResponse = new List<NMaxLimitReportResponse>();
                            if ((!isVegetableCropType) &&
                                (vegetableGroup1List.Contains(cropType) ||
                                vegetableGroup2List.Contains(cropType) ||
                                vegetableGroup3List.Contains(cropType)) && (vegetableGroup1List.Count > 0 ||
                                vegetableGroup2List.Count > 0 || vegetableGroup3List.Count > 0))
                            {
                                isVegetableCropType = true;
                                if (vegetableGroup1List.Count > 0)
                                {
                                    foreach (string cropTypeId in vegetableGroup1List)
                                    {
                                        nitrogenApplicationsForNMaxReportResponse = new List<NitrogenApplicationsForNMaxReportResponse>();
                                        nMaxLimitReportResponse = new List<NMaxLimitReportResponse>();
                                        (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error) = await GetNMaxReportData(harvestYearPlanResponse, Convert.ToInt32(cropTypeId), model,
                                            nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse);
                                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                        {
                                            TempData["ErrorOnSelectField"] = error.Message;
                                            return RedirectToAction("ExportFieldsOrCropType");
                                        }
                                    }
                                    if (nMaxLimitReportResponse != null && nMaxLimitReportResponse.Count > 0)
                                    {
                                        vegetableGroup = Resource.lblVegetableGroupOne;
                                        cropTypeName = string.Format(Resource.lblNitrogenNVegetables, Resource.lblLow);
                                        var fullReport = new NMaxReportResponse
                                        {
                                            CropTypeName = cropTypeName,
                                            NmaxLimit = nMaxLimit,
                                            VegetableGroup = vegetableGroup,
                                            IsComply = (nMaxLimitReportResponse == null && nitrogenApplicationsForNMaxReportResponse == null) ? false : (nMaxLimitReportResponse.Sum(x => x.MaximumLimitForNApplied) > nitrogenApplicationsForNMaxReportResponse.Sum(x => x.NTotal) ? true : false),
                                            NMaxLimitReportResponse = nMaxLimitReportResponse,
                                            NitrogenApplicationsForNMaxReportResponse = (nitrogenApplicationsForNMaxReportResponse != null && nitrogenApplicationsForNMaxReportResponse.Count > 0) ? nitrogenApplicationsForNMaxReportResponse : null
                                        };
                                        model.NMaxLimitReport.Add(fullReport);
                                    }
                                }
                                if (vegetableGroup2List.Count > 0)
                                {
                                    foreach (string cropTypeId in vegetableGroup2List)
                                    {
                                        nitrogenApplicationsForNMaxReportResponse = new List<NitrogenApplicationsForNMaxReportResponse>();
                                        nMaxLimitReportResponse = new List<NMaxLimitReportResponse>();
                                        (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error) = await GetNMaxReportData(harvestYearPlanResponse, Convert.ToInt32(cropTypeId), model,
                                            nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse);
                                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                        {
                                            TempData["ErrorOnSelectField"] = error.Message;
                                            return RedirectToAction("ExportFieldsOrCropType");
                                        }
                                    }
                                    if (nMaxLimitReportResponse != null && nMaxLimitReportResponse.Count > 0)
                                    {
                                        vegetableGroup = Resource.lblVegetableGroupThree;
                                        cropTypeName = string.Format(Resource.lblNitrogenNVegetables, Resource.lblHigh);
                                        var fullReport = new NMaxReportResponse
                                        {
                                            CropTypeName = cropTypeName,
                                            NmaxLimit = nMaxLimit,
                                            VegetableGroup = vegetableGroup,
                                            IsComply = (nMaxLimitReportResponse == null && nitrogenApplicationsForNMaxReportResponse == null) ? false : (nMaxLimitReportResponse.Sum(x => x.MaximumLimitForNApplied) > nitrogenApplicationsForNMaxReportResponse.Sum(x => x.NTotal) ? true : false),
                                            NMaxLimitReportResponse = nMaxLimitReportResponse,
                                            NitrogenApplicationsForNMaxReportResponse = (nitrogenApplicationsForNMaxReportResponse != null && nitrogenApplicationsForNMaxReportResponse.Count > 0) ? nitrogenApplicationsForNMaxReportResponse : null
                                        };
                                        model.NMaxLimitReport.Add(fullReport);
                                    }
                                }
                                if (vegetableGroup3List.Count > 0)
                                {
                                    foreach (string cropTypeId in vegetableGroup3List)
                                    {
                                        nitrogenApplicationsForNMaxReportResponse = new List<NitrogenApplicationsForNMaxReportResponse>();
                                        nMaxLimitReportResponse = new List<NMaxLimitReportResponse>();
                                        (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error) = await GetNMaxReportData(harvestYearPlanResponse, Convert.ToInt32(cropTypeId), model,
                                            nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse);
                                        if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                                        {
                                            TempData["ErrorOnSelectField"] = error.Message;
                                            return RedirectToAction("ExportFieldsOrCropType");
                                        }
                                    }
                                    if (nMaxLimitReportResponse != null && nMaxLimitReportResponse.Count > 0)
                                    {
                                        vegetableGroup = Resource.lblVegetableGroupTwo;
                                        cropTypeName = string.Format(Resource.lblNitrogenNVegetables, Resource.lblMedium);
                                        var fullReport = new NMaxReportResponse
                                        {
                                            CropTypeName = cropTypeName,
                                            NmaxLimit = nMaxLimit,
                                            VegetableGroup = vegetableGroup,
                                            IsComply = (nMaxLimitReportResponse == null && nitrogenApplicationsForNMaxReportResponse == null) ? false : (nMaxLimitReportResponse.Sum(x => x.MaximumLimitForNApplied) > nitrogenApplicationsForNMaxReportResponse.Sum(x => x.NTotal) ? true : false),
                                            NMaxLimitReportResponse = nMaxLimitReportResponse,
                                            NitrogenApplicationsForNMaxReportResponse = (nitrogenApplicationsForNMaxReportResponse != null && nitrogenApplicationsForNMaxReportResponse.Count > 0) ? nitrogenApplicationsForNMaxReportResponse : null
                                        };
                                        model.NMaxLimitReport.Add(fullReport);
                                    }
                                }
                                continue;
                            }
                            if (vegetableGroup1List.Contains(cropType) ||
                                vegetableGroup2List.Contains(cropType) ||
                                vegetableGroup3List.Contains(cropType))
                            {
                                continue;
                            }
                            (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error) = await GetNMaxReportData(harvestYearPlanResponse, Convert.ToInt32(cropType), model,
                                           nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse);
                            cropTypeName = (await _fieldService.FetchCropTypeById(Convert.ToInt32(cropType)));
                            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                            {
                                TempData["ErrorOnSelectField"] = error.Message;
                                return RedirectToAction("ExportFieldsOrCropType");
                            }
                            if (nMaxLimitReportResponse != null && nMaxLimitReportResponse.Count > 0)
                            {
                                var fullReport = new NMaxReportResponse
                                {
                                    CropTypeName = cropTypeName,
                                    NmaxLimit = nMaxLimit,
                                    VegetableGroup = (!string.IsNullOrWhiteSpace(vegetableGroup)) ? vegetableGroup : string.Empty,
                                    IsComply = (nMaxLimitReportResponse == null && nitrogenApplicationsForNMaxReportResponse == null) ? false : (nMaxLimitReportResponse.Sum(x => x.MaximumLimitForNApplied) > nitrogenApplicationsForNMaxReportResponse.Sum(x => x.NTotal) ? true : false),
                                    NMaxLimitReportResponse = nMaxLimitReportResponse,
                                    NitrogenApplicationsForNMaxReportResponse = (nitrogenApplicationsForNMaxReportResponse != null && nitrogenApplicationsForNMaxReportResponse.Count > 0) ? nitrogenApplicationsForNMaxReportResponse : null
                                };
                                model.NMaxLimitReport.Add(fullReport);
                            }

                        }

                    }
                    else
                    {

                        TempData["ErrorOnSelectField"] = error.Message;
                        return RedirectToAction("ExportFieldsOrCropType");
                        //TempData["NMaxReport"] = error.Message;
                        //return View(model);
                    }
                }
                else
                {
                    //TempData["NMaxReport"] = error.Message;
                    //return View(model);

                    TempData["ErrorOnSelectField"] = error.Message;
                    return RedirectToAction("ExportFieldsOrCropType");
                }

            }

            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in NMaxReport() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSelectField"] = ex.Message;
                return RedirectToAction("ExportFieldsOrCropType");
            }
            return View(model);
        }
        private async Task<(List<NitrogenApplicationsForNMaxReportResponse>, List<NMaxLimitReportResponse>, int nMaxLimit, Error?)> GetNMaxReportData(HarvestYearResponseHeader harvestYearPlanResponse, int cropTypeId, ReportViewModel model,
            List<NitrogenApplicationsForNMaxReportResponse> nitrogenApplicationsForNMaxReportResponse, List<NMaxLimitReportResponse> nMaxLimitReportResponse)
        {
            List<CropDetailResponse> cropDetails = harvestYearPlanResponse.CropDetails.Where(x => x.CropTypeID == cropTypeId).ToList();
            Error error = null;
            int nMaxLimit = 0;
            string cropTypeName = string.Empty;
            string vegetableGroup = string.Empty;
            NMaxReportResponse nMaxLimitReport = new NMaxReportResponse();
            foreach (var cropData in cropDetails)
            {
                (Crop crop, error) = await _cropService.FetchCropById(cropData.CropId.Value);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureService.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
                    if (error == null && cropTypeLinkingResponse != null)
                    {
                        nMaxLimit = model.Farm.CountryID == (int)NMP.Portal.Enums.FarmCountry.England ?
                            ((cropTypeLinkingResponse.NMaxLimitEngland != null) ? cropTypeLinkingResponse.NMaxLimitEngland.Value : 0) :
                            ((cropTypeLinkingResponse.NMaxLimitWales != null) ? cropTypeLinkingResponse.NMaxLimitWales.Value : 0);
                        if (nMaxLimit != null && nMaxLimit > 0)
                        {
                            cropTypeName = cropData.CropTypeName;
                            Field field = await _fieldService.FetchFieldByFieldId(crop.FieldID.Value);
                            if (field != null)
                            {
                                (List<int> currentYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(field.ID.Value), model.Year.Value, false);
                                (List<int> previousYearManureTypeIds, error) = await _organicManureService.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(field.ID.Value), model.Year.Value - 1, false);
                                if (error == null)
                                {
                                    bool manureTypeCondition = false;
                                    if (currentYearManureTypeIds.Count > 0)
                                    {
                                        foreach (var Ids in currentYearManureTypeIds)
                                        {
                                            if (Ids == (int)NMP.Portal.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                                                Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                                            {
                                                manureTypeCondition = true;
                                            }
                                        }
                                    }
                                    if (previousYearManureTypeIds.Count > 0)
                                    {
                                        foreach (var Ids in previousYearManureTypeIds)
                                        {
                                            if (Ids == (int)NMP.Portal.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                                                Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                                            {
                                                manureTypeCondition = true;
                                            }
                                        }
                                    }
                                    cropTypeName = (await _fieldService.FetchCropTypeById(crop.CropTypeID.Value));

                                    int soilTypeAdjustment = 0;
                                    int millingWheat = 0;
                                    decimal yieldAdjustment = 0;
                                    int paperCrumbleOrStrawMulch = 0;
                                    decimal grassCut = 0;

                                    if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SugarBeet
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup1 || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup2
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup3 || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup4
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.ForageMaize || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterBeans
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SpringBeans || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Peas
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Asparagus || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Carrots
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Radish || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Swedes
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.CelerySelfBlanching || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Courgettes
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.DwarfBeans || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Lettuce
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.BulbOnions || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SaladOnions
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Parsnips || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.RunnerBeans
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Sweetcorn || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Turnips
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Beetroot || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Cabbage || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Calabrese
                                    || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Cauliflower || crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Leeks)
                                    {
                                        if (manureTypeCondition)
                                        {
                                            paperCrumbleOrStrawMulch = 80;
                                        }

                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.Grass)
                                    {
                                        if (manureTypeCondition)
                                        {
                                            paperCrumbleOrStrawMulch = 80;
                                        }
                                        if (crop.PotentialCut >= 3)
                                        {
                                            grassCut = 40;
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterWheat ||
                                        crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SpringWheat ||
                                        crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterBarley ||
                                        crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SpringBarley ||
                                        crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
                                    {
                                        if (manureTypeCondition)
                                        {
                                            paperCrumbleOrStrawMulch = 80;
                                        }
                                        if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterWheat)
                                        {
                                            if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow)
                                            {
                                                soilTypeAdjustment = 20;
                                            }
                                            if (crop.CropInfo1 != null && crop.CropInfo1 == (int)NMP.Portal.Enums.CropInfoOne.Milling)
                                            {
                                                millingWheat = 40;
                                            }
                                            if (crop.Yield != null && crop.Yield > 8.0m)
                                            {
                                                yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 8.0m) / 0.1m) * 2);
                                            }
                                        }
                                        else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SpringWheat)
                                        {
                                            if (crop.CropInfo1 != null && crop.CropInfo1 == (int)NMP.Portal.Enums.CropInfoOne.Milling)
                                            {
                                                millingWheat = 40;
                                            }
                                            if (crop.Yield != null && crop.Yield > 7.0m)
                                            {
                                                yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 7.0m) / 0.1m) * 2);
                                            }
                                        }
                                        else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterBarley)
                                        {
                                            if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow)
                                            {
                                                soilTypeAdjustment = 20;
                                            }
                                            if (crop.Yield != null && crop.Yield > 6.5m)
                                            {
                                                yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 6.5m) / 0.1m) * 2);
                                            }
                                        }
                                        else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.SpringBarley)
                                        {
                                            if (crop.Yield != null && crop.Yield > 5.5m)
                                            {
                                                yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 5.5m) / 0.1m) * 2);
                                            }
                                        }
                                        else if (crop.CropTypeID.Value == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
                                        {
                                            if (crop.Yield != null && crop.Yield > 3.5m)
                                            {
                                                yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 3.5m) / 0.1m) * 6);
                                            }
                                        }

                                    }

                                    int nMaxLimitForCropType = nMaxLimit;
                                    if (nMaxLimit != null && nMaxLimit > 0)
                                    {
                                        nMaxLimitForCropType = Convert.ToInt32(Math.Round(nMaxLimitForCropType + soilTypeAdjustment + yieldAdjustment + millingWheat + paperCrumbleOrStrawMulch + grassCut, 0));
                                        var nMaxLimitData = new NMaxLimitReportResponse
                                        {
                                            FieldId = field.ID.Value,
                                            FieldName = field.Name,
                                            CropTypeName = cropTypeName,
                                            CropArea = field.CroppedArea.Value,
                                            AdjustmentForThreeOrMoreCuts = grassCut,
                                            CropYield = crop.Yield != null ? crop.Yield.Value : null,
                                            SoilTypeAdjustment = soilTypeAdjustment,
                                            YieldAdjustment = yieldAdjustment,
                                            MillingWheat = millingWheat,
                                            PaperCrumbleOrStrawMulch = paperCrumbleOrStrawMulch,
                                            AdjustedNMaxLimit = nMaxLimitForCropType,
                                            MaximumLimitForNApplied = nMaxLimitForCropType * field.CroppedArea.Value
                                        };
                                        nMaxLimitReportResponse.Add(nMaxLimitData);
                                        decimal? totalFertiliserN = null;
                                        decimal? totalOrganicAvailableN = null;
                                        (List<ManagementPeriod> ManPeriodList, error) = await _cropService.FetchManagementperiodByCropId(crop.ID.Value, false);
                                        if (string.IsNullOrWhiteSpace(error.Message) && ManPeriodList != null && ManPeriodList.Count > 0)
                                        {
                                            foreach (var managementPeriod in ManPeriodList)
                                            {
                                                (decimal? totalNitrogen, error) = await _fertiliserManureService.FetchTotalNByManagementPeriodID(managementPeriod.ID.Value);
                                                if (error == null)
                                                {
                                                    if (totalNitrogen != null)
                                                    {
                                                        if (totalFertiliserN == null)
                                                        {
                                                            totalFertiliserN = 0;
                                                        }
                                                        totalFertiliserN = totalFertiliserN + totalNitrogen;
                                                    }
                                                }
                                            }
                                            foreach (var managementPeriod in ManPeriodList)
                                            {
                                                (decimal? totalNitrogen, error) = await _organicManureService.FetchAvailableNByManagementPeriodID(managementPeriod.ID.Value);
                                                if (error == null)
                                                {
                                                    if (totalNitrogen != null)
                                                    {
                                                        if (totalOrganicAvailableN == null)
                                                        {
                                                            totalOrganicAvailableN = 0;
                                                        }
                                                        totalOrganicAvailableN = totalOrganicAvailableN + totalNitrogen;
                                                    }
                                                }
                                            }
                                        }
                                        var nitrogenResponse = new NitrogenApplicationsForNMaxReportResponse
                                        {
                                            FieldId = field.ID.Value,
                                            FieldName = field.Name,
                                            CropTypeName = cropTypeName,
                                            CropArea = field.CroppedArea.Value,
                                            InorganicNRate = totalFertiliserN != null ? totalFertiliserN : null,
                                            InorganicNTotal = (totalFertiliserN != null ? totalFertiliserN * field.CroppedArea.Value : null),
                                            OrganicCropAvailableNRate = totalOrganicAvailableN != null ? totalOrganicAvailableN : null,
                                            OrganicCropAvailableNTotal = (totalOrganicAvailableN != null ? totalOrganicAvailableN * field.CroppedArea.Value : null),
                                            NRate = (totalFertiliserN == null && totalOrganicAvailableN == null) ? null : (totalFertiliserN ?? 0) + (totalOrganicAvailableN ?? 0),
                                            NTotal = (totalFertiliserN == null && totalOrganicAvailableN == null) ? null : ((totalFertiliserN ?? 0) + (totalOrganicAvailableN ?? 0)) * field.CroppedArea.Value,
                                        };

                                        if (nitrogenResponse != null)
                                        {
                                            nitrogenApplicationsForNMaxReportResponse.Add(nitrogenResponse);
                                        }
                                    }

                                }
                                else
                                {
                                    return (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error);
                                    TempData["ErrorOnSelectField"] = error.Message;
                                    //return RedirectToAction("ExportFieldsOrCropType");
                                }
                            }
                        }
                    }
                    else
                    {
                        return (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error);

                    }
                }
                else
                {

                    TempData["ErrorOnSelectField"] = error.Message;
                    return (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error);
                    // return RedirectToAction("ExportFieldsOrCropType");
                    //TempData["NMaxReport"] = error.Message;
                    //return View(model);
                }

            }
            return (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error);
        }
        private static string ShorthandDefoliationSequence(List<string> data)
        {
            if (data == null && data.Count == 0)
            {
                return "";
            }

            Dictionary<string, int> defoliationSequence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string item in data)
            {
                string name = item.Trim().ToLower();
                if (defoliationSequence.ContainsKey(name))
                {
                    defoliationSequence[name]++;
                }
                else
                {
                    defoliationSequence[name] = 1;
                }
            }

            List<string> result = new List<string>();

            foreach (var entry in defoliationSequence)
            {
                string word = entry.Key;

                if (entry.Value > 1)
                {
                    if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("z") ||
                        word.EndsWith("sh") || word.EndsWith("ch"))
                    {
                        word += "es";
                    }
                    else
                    {
                        word += "s";
                    }
                }


                word = char.ToUpper(word[0]) + word.Substring(1);
                result.Add($"{entry.Value} {word}");
            }

            return string.Join(", ", result);
        }


        [HttpGet]
        public async Task<IActionResult> ReportOptions(string f, string? h)
        {
            _logger.LogTrace("Report Controller : ReportOptions() action called");
            ReportViewModel model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = new ReportViewModel();
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else if (string.IsNullOrWhiteSpace(f) && string.IsNullOrWhiteSpace(h))
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model == null)
                {
                    model = new ReportViewModel();
                    if (!string.IsNullOrWhiteSpace(f))
                    {
                        model.EncryptedFarmId = f;
                        model.FarmId = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId.ToString()));
                        (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                        if (farm != null)
                        {
                            model.FarmName = farm.Name;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(h))
                    {
                        model.IsComingFromPlan = true;
                        model.EncryptedHarvestYear = h;
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedHarvestYear.ToString()));
                    }
                    else
                    {
                        model.IsComingFromPlan = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ReportType() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportOptions(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ReportOptions() post action called");
            try
            {
                if (model.ReportOption == null)
                {
                    ModelState.AddModelError("ReportOption", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);

                if (model.ReportOption == (int)NMP.Portal.Enums.ReportOption.FieldRecordsAndPlan)
                {
                    return RedirectToAction("FieldAndPlanReports", model);
                }
                if (model.ReportOption == (int)NMP.Portal.Enums.ReportOption.FarmAndFieldDetailsForNVZRecord)
                {
                    return RedirectToAction("NVZComplianceReports", model);
                }

                //return RedirectToAction("ReportType", new {i = model.EncryptedFarmId,j = model.EncryptedHarvestYear});
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ReportOptions() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnReportOptions"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult FieldAndPlanReports()
        {
            _logger.LogTrace("Report Controller : FieldAndPlanReports() action called");
            ReportViewModel model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = new ReportViewModel();
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in FieldAndPlanReports() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnReportOptions"] = ex.Message;
                return RedirectToAction("ReportOptions");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FieldAndPlanReports(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : FieldAndPlanReports() post action called");
            try
            {
                if (model.FieldAndPlanReportOption == null)
                {
                    ModelState.AddModelError("FieldAndPlanReportOption", Resource.MsgSelectTheFarmInformationAndPlanningReportYouWantToCreate);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                model.NVZReportOption = null;
                if (model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                {
                    //model.ReportType = (int)NMP.Portal.Enums.ReportType.CropAndFieldManagementReport;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                    {
                        return RedirectToAction("ExportFieldsOrCropType");
                    }
                    else
                    {
                        return RedirectToAction("Year");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in FieldAndPlanReports() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnFieldAndPlanReports"] = ex.Message;
                return View(model);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult NVZComplianceReports()
        {
            _logger.LogTrace("Report Controller : NVZComplianceReports() action called");
            ReportViewModel model = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = new ReportViewModel();
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in NVZComplianceReports() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnReportOptions"] = ex.Message;
                return RedirectToAction("ReportOptions");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NVZComplianceReports(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : NVZComplianceReports() post action called");
            try
            {
                if (model.NVZReportOption == null)
                {
                    ModelState.AddModelError("NVZReportOption", string.Format(Resource.MsgSelectTheReportYouWantToCreate, Resource.lblNVZComplianceReport));
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                model.FieldAndPlanReportOption = null;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.NmaxReport)
                {
                    //model.ReportType = (int)NMP.Portal.Enums.ReportType.NMaxReport;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                    {
                        return RedirectToAction("ExportFieldsOrCropType");
                    }
                    else
                    {
                        return RedirectToAction("Year");
                    }
                }
                if (model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.LivestockManureNFarmLimitReport)
                {
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                    {
                        return RedirectToAction("IsGrasslandDerogation");
                    }
                    else
                    {
                        return RedirectToAction("Year");
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in NVZComplianceReports() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnNVZComplianceReports"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult Year()
        {
            _logger.LogTrace("Report Controller : Year() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model.FieldAndPlanReportOption != null)
                {
                    if (model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                    {
                        model.ReportTypeName = Resource.lblFieldRecordsAndNutrientManagementPlanning;
                    }
                    else if (model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.LivestockNumbersReport)
                    {
                        model.ReportTypeName = Resource.lblLivestockNumbers;
                    }
                    else if (model.FieldAndPlanReportOption == (int)NMP.Portal.Enums.FieldAndPlanReportOption.ImportsAndExportsReport)
                    {
                        model.ReportTypeName = Resource.lblImportsExports;
                    }
                    // = Enum.GetName(typeof(FieldAndPlanReportOption), model.FieldAndPlanReportOption);
                }
                else if (model.NVZReportOption != null)
                {
                    if (model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.NmaxReport)
                    {
                        model.ReportTypeName = Resource.lblNMax;
                    }
                    else if (model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.LivestockManureNFarmLimitReport)
                    {
                        model.ReportTypeName = Resource.lblLivestockManureNitrogenFarmLimit;
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in Year() action : {ex.Message}, {ex.StackTrace}");
                if (model.ReportOption == (int)NMP.Portal.Enums.ReportOption.FieldRecordsAndPlan)
                {
                    TempData["ErrorOnFieldAndPlanReports"] = ex.Message;
                    return RedirectToAction("FieldAndPlanReports");
                }
                else
                {
                    TempData["ErrorOnNVZComplianceReports"] = ex.Message;
                    return RedirectToAction("NVZComplianceReports");
                }
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Year(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : Year() post action called");
            try
            {
                if (model.Year == null)
                {
                    ModelState.AddModelError("Year", string.Format(Resource.lblSelectAOptionBeforeContinuing, Resource.lblYear.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.NVZReportOption == (int)NMP.Portal.Enums.NVZReportOption.LivestockManureNFarmLimitReport)
                {
                    return RedirectToAction("IsGrasslandDerogation");
                }
                return RedirectToAction("ExportFieldsOrCropType");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in Year() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnYear"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsGrasslandDerogation()
        {
            _logger.LogTrace("Report Controller : IsGrasslandDerogation() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!model.IsCheckList)
                {

                    (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails, Error error) = await _reportService.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.FarmId ?? 0, model.Year ?? 0);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["FetchNutrientsLoadingFarmDetailsError"] = error.Message;
                        return View(model);
                    }
                    if (nutrientsLoadingFarmDetails != null)
                    {
                        model.IsGrasslandDerogation = nutrientsLoadingFarmDetails.Derogation;
                        model.TotalFarmArea = nutrientsLoadingFarmDetails.TotalFarmed;
                        model.TotalAreaInNVZ = nutrientsLoadingFarmDetails.LandInNVZ;

                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                        return RedirectToAction("LivestockManureNitrogenReportChecklist", model);
                    }


                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in IsGrasslandDerogation() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnYear"] = ex.Message;
                return RedirectToAction("Year");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsGrasslandDerogation(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : IsGrasslandDerogation() post action called");
            try
            {
                if (model.IsGrasslandDerogation == null)
                {
                    ModelState.AddModelError("IsGrasslandDerogation", string.Format(Resource.lblSelectAOptionBeforeContinuing, Resource.lblYear.ToLower()));
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var NutrientsLoadingFarmDetailsData = new NutrientsLoadingFarmDetail()
                {
                    FarmID = model.FarmId,
                    CalendarYear = model.Year,
                    LandInNVZ = model.TotalAreaInNVZ,
                    LandNotNVZ = model.TotalFarmArea - model.TotalAreaInNVZ,
                    TotalFarmed = model.TotalFarmArea,
                    ManureTotal = null,
                    Derogation = model.IsGrasslandDerogation,
                    GrassPercentage = null,
                    ContingencyPlan = false
                };
                (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData, Error error) = await _reportService.AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["DerogationSaveError"] = error.Message;
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);

                return RedirectToAction("LivestockManureNitrogenReportChecklist");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in IsGrasslandDerogation() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnIsGrasslandDerogation"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> LivestockManureNitrogenReportChecklist()
        {
            _logger.LogTrace("Report Controller : LivestockManureNitrogenReportChecklist() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsCheckList = true;
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                (List<NutrientsLoadingManures> nutrientsLoadingManuresList, Error error) = await _reportService.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManuresList.Count > 0)
                {
                    nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                    if (nutrientsLoadingManuresList.Count > 0)
                    {
                        ViewBag.IsNutrientsLoadingManureshaveData = _reportDataProtector.Protect(Resource.lblTrue);
                    }
                }
                if (model.LivestockImportExportQuestion.HasValue && (!model.LivestockImportExportQuestion.Value))
                {
                    ViewBag.IsNutrientsLoadingManureshaveData = _reportDataProtector.Protect(Resource.lblTrue);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockManureNitrogenReportChecklist() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
                return View(model);

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LivestockManureNitrogenReportChecklist(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : LivestockManureNitrogenReportChecklist() post action called");
            try
            {
                if (model.IsGrasslandDerogation == null)
                {
                    ModelState.AddModelError(string.Empty, string.Format(Resource.MsgDerogationForYearMustBeCompleted, model.Year));
                }
                if (model.TotalFarmArea == null || model.TotalAreaInNVZ == null)
                {
                    ModelState.AddModelError(string.Empty, string.Format(Resource.MsgFarmAreaForYearMustBeCompleted, model.Year));
                }
                if (model.LivestockNumbers == null)
                {
                    ModelState.AddModelError(string.Empty, string.Format(Resource.MsgLivestockNumbersForYearMustBeCompleted, model.Year));
                }
                (List<NutrientsLoadingManures> nutrientsLoadingManuresList, Error error) = await _reportService.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (nutrientsLoadingManuresList.Count > 0)
                    {
                        nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                        if (nutrientsLoadingManuresList.Count == 0 && ((!model.LivestockImportExportQuestion.HasValue)||
                            model.LivestockImportExportQuestion.HasValue && model.LivestockImportExportQuestion.Value))
                        {
                            ModelState.AddModelError(string.Empty, string.Format(Resource.MsgImportsAndExportsOfManureForYearMustBeCompleted, model.Year));
                        }
                        else if (nutrientsLoadingManuresList.Count > 0)
                        {
                            ViewBag.IsNutrientsLoadingManureshaveData = _reportDataProtector.Protect(Resource.lblTrue);
                        }
                    }
                    else if (!model.LivestockImportExportQuestion.HasValue)
                    {
                        ModelState.AddModelError(string.Empty, string.Format(Resource.MsgImportsAndExportsOfManureForYearMustBeCompleted, model.Year));
                    }
                }
                if (model.LivestockImportExportQuestion.HasValue && (!model.LivestockImportExportQuestion.Value))
                {
                    ViewBag.IsNutrientsLoadingManureshaveData = _reportDataProtector.Protect(Resource.lblTrue);
                }
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                if (!ModelState.IsValid)
                {
                    return View("~/Views/Report/LivestockManureNitrogenReportChecklist.cshtml", model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);

                return RedirectToAction("LivestockManureNitrogenReportChecklist");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockManureNitrogenReportChecklist() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult FarmAreaForLivestockManure()
        {
            _logger.LogTrace("Report Controller : FarmAreaForLivestockManure() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in FarmAreaForLivestockManure() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
                return RedirectToAction("LivestockManureNitrogenReportChecklist");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FarmAreaForLivestockManure(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : FarmAreaForLivestockManure() post action called");
            try
            {
                if (model.TotalFarmArea == null)
                {
                    ModelState.AddModelError("TotalFarmArea", Resource.MsgEnterTotalFarmArea);
                }
                if (model.TotalAreaInNVZ == null)
                {
                    ModelState.AddModelError("TotalAreaInNVZ", Resource.MsgEnterTotalAreaInNVZ);
                }
                if (model.TotalFarmArea <= 0)
                {
                    ModelState.AddModelError("TotalFarmArea", Resource.MsgTotalFarmAreaShouldBeGreaterThanZero);
                }
                if (model.TotalAreaInNVZ < 0)
                {
                    ModelState.AddModelError("TotalAreaInNVZ", Resource.MsgTotalAreaInNVZShouldNotBeLessThanZero);
                }
                if (model.TotalAreaInNVZ > model.TotalFarmArea)
                {
                    ModelState.AddModelError("TotalAreaInNVZ", Resource.MsgTotalAreaInNVZShouldNotBeMoreThanTotalFarmArea);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var NutrientsLoadingFarmDetailsData = new NutrientsLoadingFarmDetail()
                {
                    FarmID = model.FarmId,
                    CalendarYear = model.Year,
                    LandInNVZ = model.TotalAreaInNVZ,
                    LandNotNVZ = model.TotalFarmArea - model.TotalAreaInNVZ,
                    TotalFarmed = model.TotalFarmArea,
                    ManureTotal = null,
                    Derogation = model.IsGrasslandDerogation,
                    GrassPercentage = null,
                    ContingencyPlan = false
                };
                (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData, Error error) = await _reportService.UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["FarmDetailsSaveError"] = error.Message;
                    return View(model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);

                return RedirectToAction("LivestockManureNitrogenReportChecklist");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in FarmAreaForLivestockManure() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnFarmAreaForLivestockManure"] = ex.Message;
                return View(model);
            }
        }

        public IActionResult BackCheckList()
        {
            _logger.LogTrace("Report Controller : BackCheckList() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsCheckList = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in BackCheckList() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
                return RedirectToAction("LivestockManureNitrogenReportChecklist");

            }
            //if(model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value))
            //{
            //    return RedirectToAction("Year");
            //}
            return RedirectToAction("IsGrasslandDerogation");
        }

        [HttpGet]
        public IActionResult LivestockImportExportQuestion()
        {
            _logger.LogTrace("Report Controller : LivestockImportExportQuestion() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockImportExportQuestion() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
                return RedirectToAction("LivestockManureNitrogenReportChecklist");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockImportExportQuestion(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : LivestockImportExportQuestion() post action called");
            try
            {
                if (model.LivestockImportExportQuestion == null)
                {
                    ModelState.AddModelError("LivestockImportExportQuestion", Resource.MsgSelectYesIfYouHadAnyImportsOrExportsOfLivestockManure);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (!model.LivestockImportExportQuestion.Value)
                {
                    model.IsCheckAnswer = false;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockManureNitrogenReportChecklist");
                }
                else
                {
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("ImportExportOption");
                }



            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockImportExportQuestion() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockImportExportQuestion"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult ImportExportOption(string? q, string? r)
        {
            _logger.LogTrace("Report Controller : ImportExportOption() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrWhiteSpace(r))
                {
                    int year = Convert.ToInt32(_farmDataProtector.Unprotect(r));
                    if (year != null)
                    {
                        model.Year = year;
                        model.EncryptedHarvestYear = r;
                        model.IsComingFromImportExportOverviewPage = r;
                        model.IsCheckList = false;
                    }
                }
                //ViewBag.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ImportExportOption() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockImportExportQuestion"] = ex.Message;
                return RedirectToAction("LivestockImportExportQuestion");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportExportOption(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ImportExportOption() post action called");
            try
            {
                if (model.ImportExport == null)
                {
                    ModelState.AddModelError("ImportExport", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockImportExportCheckAnswer");
                }                
                return RedirectToAction("ManureType");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ImportExportOption() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnImportExportOption"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> ManureType(string? q)
        {
            _logger.LogTrace("Report Controller : ManureType() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    (List<ManureType> ManureTypes, error) = await _organicManureService.FetchManureTypeList((int)NMP.Portal.Enums.ManureGroup.LivestockManure, farm.CountryID.Value);
                    if (error == null && ManureTypes != null && ManureTypes.Count > 0)
                    {
                        var SelectListItem = ManureTypes.Select(f => new SelectListItem
                        {
                            Value = f.Id.ToString(),
                            Text = f.Name
                        }).ToList();
                        ViewBag.ManureTypeList = SelectListItem.ToList();
                        //ViewBag.ManureTypeList= ManureTypes;
                    }
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    string import = _reportDataProtector.Unprotect(q);
                    if (!string.IsNullOrWhiteSpace(import))
                    {
                        if (import == Resource.lblImport)
                        {
                            model.IsImport = true;
                            model.ImportExport = (int)NMP.Portal.Enums.ImportExport.Import;
                        }
                        else
                        {
                            model.IsImport = false;
                            model.ImportExport = (int)NMP.Portal.Enums.ImportExport.Export;
                        }
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ManureType() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnImportExportOption"] = ex.Message;
                return RedirectToAction("ImportExportOption");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManureType(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ManureType() post action called");
            try
            {
                if (model.ManureTypeId == null)
                {
                    ModelState.AddModelError("ManureTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                Error error = null;
                if (!ModelState.IsValid)
                {
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        (List<ManureType> ManureTypes, error) = await _organicManureService.FetchManureTypeList((int)NMP.Portal.Enums.ManureGroup.LivestockManure, farm.CountryID.Value);
                        if (error == null && ManureTypes != null && ManureTypes.Count > 0)
                        {
                            var SelectListItem = ManureTypes.Select(f => new SelectListItem
                            {
                                Value = f.Id.ToString(),
                                Text = f.Name
                            }).ToList();
                            ViewBag.ManureTypeList = SelectListItem.ToList();
                        }
                    }
                    return View(model);
                }
                (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                if (error == null && manureType != null)
                {
                    model.IsManureTypeLiquid = manureType.IsLiquid.Value;
                    model.ManureTypeName = manureType.Name;
                }

                ReportViewModel reportViewModel = new ReportViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    reportViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                if (reportViewModel != null && reportViewModel.ManureTypeId != model.ManureTypeId)
                {
                    model.IsDefaultValueChange = true;
                    model.IsManureTypeChange = true;
                }

                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                if (error == null && farmManureTypeList.Count > 0)
                {
                    FarmManureTypeResponse previousFarmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == reportViewModel.ManureTypeId);
                    FarmManureTypeResponse currentFarmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                    if (previousFarmManure != null && currentFarmManure == null)
                    {
                        model.DefaultNutrientValue = Resource.lblYes;
                    }

                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsDefaultValueChange && model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockDefaultNutrientValue");
                }
                else if (!model.IsDefaultValueChange && model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockImportExportCheckAnswer");
                }
                return RedirectToAction("LivestockImportExportDate");


            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ManureType() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnManureType"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult LivestockImportExportDate()
        {
            _logger.LogTrace("Report Controller : LivestockImportExportDate() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockImportExportDate() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnManureType"] = ex.Message;
                return RedirectToAction("ManureType");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockImportExportDate(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : LivestockImportExportDate() post action called");
            try
            {
                if (model.LivestockImportExportDate == null)
                {
                    ModelState.AddModelError("LivestockImportExportDate", Resource.MsgEnterADateBeforeContinuing);
                }
                if (model.LivestockImportExportDate != null)
                {
                    if (model.LivestockImportExportDate.Value.Date.Year != model.Year)
                    {
                        ModelState.AddModelError("LivestockImportExportDate", Resource.lblThisDateIsOutsideTheSelectedCalenderYear);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockImportExportCheckAnswer");
                }
                return RedirectToAction("LivestockQuantity");

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockImportExportDate() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockImportExportDate"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult LivestockQuantity()
        {
            _logger.LogTrace("Report Controller : LivestockQuantity() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockQuantity() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockImportExportDate"] = ex.Message;
                return RedirectToAction("LivestockImportExportDate");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockQuantity(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : LivestockQuantity() post action called");
            try
            {
                if (model.LivestockQuantity == null)
                {
                    if ((!ModelState.IsValid) && ModelState.ContainsKey("LivestockQuantity"))
                    {
                        var areaError = ModelState["LivestockQuantity"].Errors.Count > 0 ?
                                        ModelState["LivestockQuantity"].Errors[0].ErrorMessage.ToString() : null;

                        if (areaError != null && areaError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["LivestockQuantity"].RawValue, Resource.lblLivestockQuantityWIthoutSpace)))
                        {
                            ModelState["LivestockQuantity"].Errors.Clear();
                            ModelState["LivestockQuantity"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblQuantity.ToLower()));
                        }
                        else
                        {
                            ModelState.AddModelError("LivestockQuantity", Resource.lblEnterTheAmountYouImportedInTonnes);
                        }
                    }

                }
                if (model.LivestockQuantity != null && model.LivestockQuantity < 0)
                {
                    ModelState.AddModelError("LivestockQuantity", string.Format(Resource.lblEnterAPositiveValueOfPropertyName, Resource.lblQuantity.ToLower()));
                }
                if (model.LivestockQuantity != null && model.LivestockQuantity > 999999)
                {
                    ModelState.AddModelError("LivestockQuantity", string.Format(Resource.MsgEnterValueInBetween, Resource.lblQuantity, 0, 999999));
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockImportExportCheckAnswer");
                }
                return RedirectToAction("LivestockDefaultNutrientValue");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockQuantity() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockQuantity"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> UpdateLivestockImportExport(string q)
        {
            _logger.LogTrace($"Report Controller : UpdateLivestockImportExport() action called");
            ReportViewModel model = new ReportViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    model.FarmName = farm.Name;
                    model.FarmId = decryptedFarmId;
                    model.EncryptedFarmId = q;
                    List<HarvestYear> harvestYearList = new List<HarvestYear>();

                    model.IsComingFromImportExportOverviewPage = _reportDataProtector.Protect(Resource.lblTrue);
                    (List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetailList, error) = await _reportService.FetchNutrientsLoadingFarmDetailsByFarmId(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingFarmDetailList != null && nutrientsLoadingFarmDetailList.Count > 0)
                    {
                        (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportService.FetchNutrientsLoadingManuresByFarmId(decryptedFarmId);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            var uniqueYears = nutrientsLoadingFarmDetailList
                                .Where(x => x.CalendarYear.HasValue)
                                .Select(x => x.CalendarYear.Value)
                                .Distinct();

                            foreach (var year in uniqueYears)
                            {
                                DateTime? lastModifyDate = null;
                                if (nutrientsLoadingManuresList != null && nutrientsLoadingManuresList.Count > 0)
                                {
                                    var matchedManures = nutrientsLoadingManuresList
                                        .Where(m => m.ManureDate.HasValue && m.ManureDate.Value.Year == year)
                                        .ToList();

                                    lastModifyDate = matchedManures
                                       .Select(m => m.ModifiedOn ?? m.CreatedOn)
                                       .OrderByDescending(d => d)
                                       .FirstOrDefault();
                                }
                                harvestYearList.Add(new HarvestYear
                                {
                                    Year = year,
                                    EncryptedYear = _farmDataProtector.Protect(year.ToString()),
                                    LastModifiedOn = lastModifyDate
                                });
                            }
                            if (harvestYearList.Count > 0)
                            {
                                harvestYearList = harvestYearList.OrderBy(x => x.Year).ToList();
                                model.HarvestYear = harvestYearList;
                            }
                            //(List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportService.FetchNutrientsLoadingManuresByFarmId(decryptedFarmId);
                            //if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManuresList != null && nutrientsLoadingManuresList.Count > 0)
                            //{
                            //    HarvestYear harvestYear = new HarvestYear();
                            //    foreach (var nutrientsLoadingManure in nutrientsLoadingManuresList)
                            //    {
                            //        harvestYear.LastModifiedOn = nutrientsLoadingManure.ModifiedOn != null ? nutrientsLoadingManure.ModifiedOn.Value : nutrientsLoadingManure.CreatedOn.Value;
                            //        harvestYear.Year = nutrientsLoadingManure.ManureDate.Value.Year;
                            //        harvestYear.EncryptedYear = _reportDataProtector.Protect(nutrientsLoadingManure.ManureDate.Value.Year.ToString());
                            //        harvestYearList.Add(harvestYear);
                            //    }

                            //    harvestYearList = harvestYearList.OrderBy(x => x.Year).ToList();
                            //    model.HarvestYear = harvestYearList;
                            //}
                            //else
                            //{
                            //    TempData["Error"] = error.Message;
                            //    return RedirectToAction("FarmSummary", "Farm", new { q = q });
                            //}
                        }
                        else
                        {
                            TempData["Error"] = error.Message;
                            return RedirectToAction("FarmSummary", "Farm", new { q = q });
                        }

                    }
                    else
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmSummary", "Farm", new { q = q });
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                }
                else
                {
                    TempData["Error"] = error.Message;
                    return RedirectToAction("FarmSummary", "Farm", new { q = q });
                }
                //ViewBag.IsComingFromOverviewPage = _reportDataProtector.Protect(Resource.lblTrue);
                return View(model);
            }

            return RedirectToAction("FarmSummary", "Farm", new { q = q });
        }
        [HttpGet]
        public async Task<IActionResult> LivestockDefaultNutrientValue()
        {
            _logger.LogTrace("Report Controller : LivestockDefaultNutrientValue() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                Error? error = null;
                FarmManureTypeResponse? farmManure = null;
                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                if (error == null)
                {
                    (ManureType manureType, error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

                    if (error == null && manureType != null && farmManureTypeList.Count > 0)
                    {
                        farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);

                        if (model.IsDefaultValueChange)
                        {
                            model.IsDefaultValueChange = false;
                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                            }
                            else
                            {
                                if (error == null)
                                {
                                    model.ManureType = manureType;
                                }
                            }
                        }
                        else
                        {
                            if (farmManure != null)
                            {
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                                if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseValues) || (model.IsThisDefaultValueOfRB209 != null && (!model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                                }
                                else if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues) || (model.IsThisDefaultValueOfRB209 != null && (model.IsThisDefaultValueOfRB209.Value)))
                                {
                                    ViewBag.FarmManureApiOption = null;
                                    ViewBag.RB209ApiOption = Resource.lblTrue;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (error == null)
                        {
                            model.ManureType = manureType;
                        }
                    }
                }


                model.IsDefaultNutrient = true;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockDefaultNutrientValue() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockQuantity"] = ex.Message;
                return RedirectToAction("LivestockQuantity");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LivestockDefaultNutrientValue(ReportViewModel model)
        {
            _logger.LogTrace($"Livestock Manure Controller : LivestockDefaultNutrientValue() post action called");
            if (model.DefaultNutrientValue == null)
            {
                ModelState.AddModelError("DefaultNutrientValue", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                Error? error = null;
                FarmManureTypeResponse? farmManure = null;

                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);

                (ManureType manureType, Error manureTypeError) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

                if (error == null && farmManureTypeList.Count > 0)
                {
                    farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                    if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue) || (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYes))
                    {
                        if (farmManure != null)
                        {
                            model.ManureType.DryMatter = farmManure.DryMatter;
                            model.ManureType.TotalN = farmManure.TotalN;
                            model.ManureType.NH4N = farmManure.NH4N;
                            model.ManureType.Uric = farmManure.Uric;
                            model.ManureType.NO3N = farmManure.NO3N;
                            model.ManureType.P2O5 = farmManure.P2O5;
                            model.ManureType.K2O = farmManure.K2O;
                            model.ManureType.SO3 = farmManure.SO3;
                            model.ManureType.MgO = farmManure.MgO;
                            ViewBag.FarmManureApiOption = Resource.lblTrue;
                            model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                        }
                        else
                        {
                            if (manureTypeError == null)
                            {
                                model.ManureType = manureType;
                            }
                        }
                    }
                    else
                    {
                        if (farmManure != null)
                        {
                            if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseValues) || (model.IsThisDefaultValueOfRB209 != null && (!model.IsThisDefaultValueOfRB209.Value)))
                            {
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                            }
                            else if ((!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues) || (model.IsThisDefaultValueOfRB209 != null && (model.IsThisDefaultValueOfRB209.Value)))
                            {
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                                ViewBag.RB209ApiOption = Resource.lblTrue;
                            }
                        }
                    }
                }
                else
                {
                    if (manureTypeError == null)
                    {
                        model.ManureType = manureType;
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis)
            {
                if (model.DryMatterPercent == null)
                {
                    model.DryMatterPercent = model.ManureType.DryMatter;
                    model.N = model.ManureType.TotalN;
                    model.P2O5 = model.ManureType.P2O5;
                    model.NH4N = model.ManureType.NH4N;
                    model.UricAcid = model.ManureType.Uric;
                    model.SO3 = model.ManureType.SO3;
                    model.K2O = model.ManureType.K2O;
                    model.MgO = model.ManureType.MgO;
                    model.NO3N = model.ManureType.NO3N;
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockManualNutrientValue");
            }
            else
            {

                model.DryMatterPercent = null;
                model.N = null;
                model.P2O5 = null;
                model.NH4N = null;
                model.UricAcid = null;
                model.SO3 = null;
                model.K2O = null;
                model.MgO = null;
                model.NO3N = null;
                ReportViewModel reportViewModel = new ReportViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    reportViewModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }

                if (reportViewModel != null && (!string.IsNullOrWhiteSpace(reportViewModel.DefaultNutrientValue)))
                {
                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                    {
                        (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error1 == null && farmManureTypeList.Count > 0)
                        {
                            FarmManureTypeResponse farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);

                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;
                            }

                            model.IsThisDefaultValueOfRB209 = false;
                            if (reportViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                            {
                                if (farmManure != null)
                                {
                                    ViewBag.FarmManureApiOption = Resource.lblTrue;
                                }
                                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);
                                if (reportViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (reportViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || reportViewModel.DefaultNutrientValue != Resource.lblYesUseTheseStandardNutrientValues)
                                    && model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                                {
                                    return View(model);
                                }
                            }
                        }
                    }
                    else
                    {
                        (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;

                        model.IsThisDefaultValueOfRB209 = true;
                        if (reportViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);
                            if (reportViewModel.DefaultNutrientValue != model.DefaultNutrientValue && (reportViewModel.DefaultNutrientValue != Resource.lblIwantToEnterARecentOrganicMaterialAnalysis || reportViewModel.DefaultNutrientValue != Resource.lblYesUseTheseValues)
                                  && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                            {
                                return View(model);
                            }

                        }
                        if (reportViewModel.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                    {

                        (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                        if (error1 == null && farmManureTypeList.Count > 0)
                        {
                            FarmManureTypeResponse farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);

                            if (farmManure != null)
                            {
                                model.ManureType.DryMatter = farmManure.DryMatter;
                                model.ManureType.TotalN = farmManure.TotalN;
                                model.ManureType.NH4N = farmManure.NH4N;
                                model.ManureType.Uric = farmManure.Uric;
                                model.ManureType.NO3N = farmManure.NO3N;
                                model.ManureType.P2O5 = farmManure.P2O5;
                                model.ManureType.K2O = farmManure.K2O;
                                model.ManureType.SO3 = farmManure.SO3;
                                model.ManureType.MgO = farmManure.MgO;

                            }
                            if (model.DefaultNutrientValue == Resource.lblYesUseTheseValues)
                            {
                                model.IsThisDefaultValueOfRB209 = false;
                                ViewBag.FarmManureApiOption = Resource.lblTrue;
                            }
                        }
                    }
                    else
                    {
                        (ManureType manureType, Error error) = await _organicManureService.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        model.ManureType = manureType;
                        if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                        {
                            model.IsThisDefaultValueOfRB209 = true;
                            ViewBag.RB209ApiOption = Resource.lblTrue;
                            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);
                            return View(model);
                        }

                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);

            }

            if (model.IsCheckAnswer)
            {
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
            return RedirectToAction("LivestockReceiver");
        }
        [HttpGet]
        public IActionResult LivestockManualNutrientValue()
        {
            _logger.LogTrace($"Organic Manure Controller : LivestockManualNutrientValue() post action called");
            ReportViewModel model = new ReportViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockManualNutrientValue(ReportViewModel model)
        {
            _logger.LogTrace($"Organic Manure Controller : LivestockManualNutrientValue() post action called");
            try
            {
                if ((!ModelState.IsValid) && ModelState.ContainsKey("DryMatterPercent"))
                {
                    var dryMatterPercentError = ModelState["DryMatterPercent"].Errors.Count > 0 ?
                                    ModelState["DryMatterPercent"].Errors[0].ErrorMessage.ToString() : null;

                    if (dryMatterPercentError != null && dryMatterPercentError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["DryMatterPercent"].RawValue, Resource.lblDryMatterPercent)))
                    {
                        ModelState["DryMatterPercent"].Errors.Clear();
                        ModelState["DryMatterPercent"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblDryMatter));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("N"))
                {
                    var totalNitrogenError = ModelState["N"].Errors.Count > 0 ?
                                    ModelState["N"].Errors[0].ErrorMessage.ToString() : null;

                    if (totalNitrogenError != null && totalNitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["N"].RawValue, Resource.lblN)))
                    {
                        ModelState["N"].Errors.Clear();
                        ModelState["N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalNitrogen));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("NH4N"))
                {
                    var ammoniumError = ModelState["NH4N"].Errors.Count > 0 ?
                                    ModelState["NH4N"].Errors[0].ErrorMessage.ToString() : null;

                    if (ammoniumError != null && ammoniumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["NH4N"].RawValue, Resource.lblNH4N)))
                    {
                        ModelState["NH4N"].Errors.Clear();
                        ModelState["NH4N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblAmmonium));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("UricAcid"))
                {
                    var uricAcidError = ModelState["UricAcid"].Errors.Count > 0 ?
                                    ModelState["UricAcid"].Errors[0].ErrorMessage.ToString() : null;

                    if (uricAcidError != null && uricAcidError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["UricAcid"].RawValue, Resource.lblUricAcidForError)))
                    {
                        ModelState["UricAcid"].Errors.Clear();
                        ModelState["UricAcid"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblUricAcid));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("NO3N"))
                {
                    var nitrogenError = ModelState["NO3N"].Errors.Count > 0 ?
                                    ModelState["NO3N"].Errors[0].ErrorMessage.ToString() : null;

                    if (nitrogenError != null && nitrogenError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["NO3N"].RawValue, Resource.lblNO3N)))
                    {
                        ModelState["NO3N"].Errors.Clear();
                        ModelState["NO3N"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblNitrogen));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("P2O5"))
                {
                    var totalPhosphateError = ModelState["P2O5"].Errors.Count > 0 ?
                                    ModelState["P2O5"].Errors[0].ErrorMessage.ToString() : null;

                    if (totalPhosphateError != null && totalPhosphateError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["P2O5"].RawValue, Resource.lblP2O5)))
                    {
                        ModelState["P2O5"].Errors.Clear();
                        ModelState["P2O5"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalPhosphate));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("K2O"))
                {
                    var totalPotassiumError = ModelState["K2O"].Errors.Count > 0 ?
                                    ModelState["K2O"].Errors[0].ErrorMessage.ToString() : null;

                    if (totalPotassiumError != null && totalPotassiumError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["K2O"].RawValue, Resource.lblK2O)))
                    {
                        ModelState["K2O"].Errors.Clear();
                        ModelState["K2O"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalPotassium));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("SO3"))
                {
                    var sulphurSO3Error = ModelState["SO3"].Errors.Count > 0 ?
                                    ModelState["SO3"].Errors[0].ErrorMessage.ToString() : null;

                    if (sulphurSO3Error != null && sulphurSO3Error.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["SO3"].RawValue, Resource.lblSO3)))
                    {
                        ModelState["SO3"].Errors.Clear();
                        ModelState["SO3"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblTotalSulphur));
                    }
                }
                if ((!ModelState.IsValid) && ModelState.ContainsKey("MgO"))
                {
                    var totalMagnesiumOxideError = ModelState["MgO"].Errors.Count > 0 ?
                                    ModelState["MgO"].Errors[0].ErrorMessage.ToString() : null;

                    if (totalMagnesiumOxideError != null && totalMagnesiumOxideError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState["MgO"].RawValue, Resource.lblMgO)))
                    {
                        ModelState["MgO"].Errors.Clear();
                        ModelState["MgO"].Errors.Add(string.Format(Resource.MsgEnterDataOnlyInNumber, Resource.lblMagnesiumMgO));
                    }
                }
                if (model.DryMatterPercent == null)
                {
                    ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblDryMatter.ToLower()));
                }
                if (model.N == null)
                {
                    ModelState.AddModelError("N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblTotalNitrogen.ToLower()));
                }
                if (model.NH4N == null)
                {
                    ModelState.AddModelError("NH4N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblAmmoniumForError));
                }
                if (model.UricAcid == null)
                {
                    ModelState.AddModelError("UricAcid", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.MsgUricAcid));
                }
                if (model.NO3N == null)
                {
                    ModelState.AddModelError("NO3N", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblNitrateForErrorMsg));
                }
                if (model.P2O5 == null)
                {
                    ModelState.AddModelError("P2O5", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPhosphate.ToLower()));
                }
                if (model.K2O == null)
                {
                    ModelState.AddModelError("K2O", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblPotash.ToLower()));
                }
                if (model.SO3 == null)
                {
                    ModelState.AddModelError("SO3", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblSulphur.ToLower()));
                }
                if (model.MgO == null)
                {
                    ModelState.AddModelError("MgO", string.Format(Resource.MsgEnterTheValueBeforeContinuing, Resource.lblMagnesiumMgO.ToLower()));
                }

                if (model.N != null && model.NH4N != null && model.UricAcid != null && model.NO3N != null)
                {
                    decimal totalValue = model.NH4N.Value + model.UricAcid.Value + model.NO3N.Value;
                    if (model.N < totalValue)
                    {
                        ModelState.AddModelError("N", Resource.lblTotalNitrogenMustBeGreaterOrEqualToAmmoniumUricacidNitrate);
                    }
                }

                if (model.DryMatterPercent != null)
                {
                    if (model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.PigSlurry ||
                        model.ManureTypeId == (int)NMP.Portal.Enums.ManureTypes.CattleSlurry)
                    {
                        if (model.DryMatterPercent < 0 || model.DryMatterPercent > 25)
                        {
                            ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter.ToLower(), 25));
                        }
                    }
                    else
                    {
                        if (model.DryMatterPercent < 0 || model.DryMatterPercent > 99)
                        {
                            ModelState.AddModelError("DryMatterPercent", string.Format(Resource.MsgMinMaxValidation, Resource.lblDryMatter, 99));
                        }
                    }
                }

                if (model.N != null)
                {
                    if (model.N < 0 || model.N > 297)
                    {
                        ModelState.AddModelError("N", string.Format(Resource.MsgMinMaxValidation, Resource.lblTotalNitrogenN, 297));
                    }
                }

                if (model.NH4N != null)
                {
                    if (model.NH4N < 0 || model.NH4N > 99)
                    {
                        ModelState.AddModelError("NH4N", string.Format(Resource.MsgMinMaxValidation, Resource.lblAmmonium, 99));
                    }
                }

                if (model.UricAcid != null)
                {
                    if (model.UricAcid < 0 || model.UricAcid > 99)
                    {
                        ModelState.AddModelError("UricAcid", string.Format(Resource.MsgMinMaxValidation, Resource.lblUricAcid, 99));
                    }
                }

                if (model.NO3N != null)
                {
                    if (model.NO3N < 0 || model.NO3N > 99)
                    {
                        ModelState.AddModelError("NO3N", string.Format(Resource.MsgMinMaxValidation, Resource.lblNitrate, 99));
                    }
                }

                if (model.P2O5 != null)
                {
                    if (model.P2O5 < 0 || model.P2O5 > 99)
                    {
                        ModelState.AddModelError("P2O5", string.Format(Resource.MsgMinMaxValidation, Resource.lblPhosphateP2O5, 99));
                    }
                }

                if (model.K2O != null)
                {
                    if (model.K2O < 0 || model.K2O > 99)
                    {
                        ModelState.AddModelError("K2O", string.Format(Resource.MsgMinMaxValidation, Resource.lblPotashK2O, 99));
                    }
                }
                if (model.MgO != null)
                {
                    if (model.MgO < 0 || model.MgO > 99)
                    {
                        ModelState.AddModelError("MgO", string.Format(Resource.MsgMinMaxValidation, Resource.lblMagnesiumMgO, 99));
                    }
                }

                if (model.SO3 != null)
                {
                    if (model.SO3 < 0 || model.SO3 > 99)
                    {
                        ModelState.AddModelError("SO3", string.Format(Resource.MsgMinMaxValidation, Resource.lblSulphurSO3, 99));
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (model.ManureType.DryMatter != model.DryMatterPercent || model.ManureType.TotalN != model.N
               || model.ManureType.NH4N != model.NH4N || model.ManureType.Uric != model.UricAcid
                || model.ManureType.NO3N != model.NO3N || model.ManureType.P2O5 != model.P2O5 ||
                model.ManureType.K2O != model.K2O || model.ManureType.MgO != model.MgO
                || model.ManureType.SO3 != model.SO3)
                {
                    model.IsAnyNeedToStoreNutrientValueForFuture = true;
                }
                else
                {
                    model.IsAnyNeedToStoreNutrientValueForFuture = false;
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("ReportData", model);

                if (model.IsCheckAnswer)
                {
                    return RedirectToAction("LivestockImportExportCheckAnswer");
                }
                return RedirectToAction("LivestockReceiver");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockManualNutrientValue() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnLivestockManualNutrientValue"] = ex.Message;
                return View(model);
            }

        }
        [HttpGet]
        public IActionResult LivestockReceiver()
        {
            _logger.LogTrace("Report Controller : LivestockReceiver() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockReceiver() action : {ex.Message}, {ex.StackTrace}");
                if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis))
                {
                    TempData["ErrorOnLivestockManualNutrientValue"] = ex.Message;
                    return RedirectToAction("LivestockManualNutrientValue");
                }
                else
                {

                    TempData["ErrorOnLivestockDefaultNutrientValue"] = ex.Message;
                    return RedirectToAction("LivestockDefaultNutrientValue");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockReceiver(ReportViewModel model)
        {
            _logger.LogTrace($"Report Controller : LivestockReceiver() post action called");
            if (string.IsNullOrEmpty(model.ReceiverName))
            {
                ModelState.AddModelError("ReceiverName", string.Format(Resource.MsgEnterTheNameOfThePersonOrOrganisationYouAreFrom, model.ImportExport == (int)NMP.Portal.Enums.ImportExport.Import ?
                    Resource.lblImporting : Resource.lblExporting));
            }

            if(!string.IsNullOrWhiteSpace(model.Address1)&&model.Address1.Length>50)
            {
                ModelState.AddModelError("Address1", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine1,50));
            }
            if (!string.IsNullOrWhiteSpace(model.Address2) && model.Address2.Length > 50)
            {
                ModelState.AddModelError("Address2", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine2ForErrorMsg,50));
            }
            if (!string.IsNullOrWhiteSpace(model.Address3) && model.Address3.Length > 50)
            {
                ModelState.AddModelError("Address3", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblTownOrCity,50));
            }
            if (!string.IsNullOrWhiteSpace(model.Address4) && model.Address4.Length > 50)
            {
                ModelState.AddModelError("Address4", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblCountry,50));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsCheckAnswer)
            {
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
            return RedirectToAction("LivestockComment");
        }
        [HttpGet]
        public IActionResult LivestockComment()
        {
            _logger.LogTrace("Report Controller : LivestockComment() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockComment() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnLivestockReceiver"] = ex.Message;
                return RedirectToAction("LivestockReceiver");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LivestockComment(ReportViewModel model)
        {
            _logger.LogTrace($"Report Controller : LivestockComment() post action called");

            if (!string.IsNullOrWhiteSpace(model.Comment) && model.Comment.Length > 255)
            {
                ModelState.AddModelError("Comment", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblComment,255));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            return RedirectToAction("LivestockImportExportCheckAnswer");
        }
        [HttpGet]
        public IActionResult BackLivestockImportExportCheckAnswer()
        {
            _logger.LogTrace("Report Controller : BackLivestockImportExportCheckAnswer() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsCheckAnswer = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in BackLivestockImportExportCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckYourAnswers"] = ex.Message;
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
            return RedirectToAction("LivestockComment");
        }

        [HttpGet]
        public IActionResult LivestockImportExportCheckAnswer()
        {
            _logger.LogTrace("Report Controller : LivestockImportExportCheckAnswer() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                model.IsCheckAnswer = true;
                model.IsManureTypeChange = false;
                model.IsDefaultValueChange = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in LivestockImportExportCheckAnswer() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnLivestockComment"] = ex.Message;
                return RedirectToAction("LivestockComment");

            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LivestockImportExportCheckAnswer(ReportViewModel model)
        {
            _logger.LogTrace($"Report Controller : LivestockImportExportCheckAnswer() post action called");
            Error error = null;
            if (model.IsDefaultNutrient == null && model.ManureTypeId != null)
            {
                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureService.FetchFarmManureTypeByFarmId(model.FarmId.Value);
                if (error == null && farmManureTypeList.Count > 0)
                {
                    farmManureTypeList = farmManureTypeList.Where(x => x.ManureTypeID == model.ManureTypeId).ToList();
                    if (farmManureTypeList.Count > 0)
                    {
                        ModelState.AddModelError("IsDefaultNutrient", string.Format("{0} {1}", string.Format(Resource.lblNutrientValuesForManureTypeNameYouAddedOnDate, model.ManureTypeName, model.DefaultFarmManureValueDate.Value.ToLocalTime().Date.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"))), Resource.lblNotSet));
                    }
                    else
                    {
                        ModelState.AddModelError("IsDefaultNutrient", string.Format("{0} {1}", string.Format(Resource.lblNutrientValuesForManureTypeName, model.ManureTypeName), Resource.lblNotSet));
                    }
                }
                else
                {
                    ModelState.AddModelError("IsDefaultNutrient", string.Format("{0} {1}", string.Format(Resource.lblNutrientValuesForManureTypeName, model.ManureTypeName), Resource.lblNotSet));
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.ImportExport == null && model.IsImport != null)
            {
                if (model.IsImport.Value)
                {
                    model.ImportExport = (int)NMP.Portal.Enums.ImportExport.Import;
                }
                else
                {
                    model.ImportExport = (int)NMP.Portal.Enums.ImportExport.Export;
                }
            }
            decimal totalN = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.N.Value : model.ManureType.TotalN.Value;
            decimal totalP = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.P2O5.Value : model.ManureType.P2O5.Value;
            NutrientsLoadingManures nutrientsLoadingManure = new NutrientsLoadingManures();
            nutrientsLoadingManure.FarmID = model.FarmId.Value;
            nutrientsLoadingManure.ManureLookupType = Enum.GetName(typeof(NMP.Portal.Enums.ImportExport), model.ImportExport);
            nutrientsLoadingManure.ManureTypeID = model.ManureTypeId.Value;
            nutrientsLoadingManure.ManureType = model.ManureTypeName;
            nutrientsLoadingManure.Quantity = model.LivestockQuantity;
            nutrientsLoadingManure.NContent = totalN;
            nutrientsLoadingManure.PContent = totalP;
            nutrientsLoadingManure.NTotal = Math.Round(totalN * model.LivestockQuantity.Value, 0);
            nutrientsLoadingManure.PTotal = Math.Round(totalP * model.LivestockQuantity.Value, 0);
            nutrientsLoadingManure.ManureDate = model.LivestockImportExportDate;
            nutrientsLoadingManure.FarmName = model.ReceiverName;
            nutrientsLoadingManure.Address1 = model.Address1;
            nutrientsLoadingManure.Address2 = model.Address2;
            nutrientsLoadingManure.Address3 = model.Address3;
            nutrientsLoadingManure.Address4 = model.Address4;
            nutrientsLoadingManure.PostCode = model.Postcode;
            nutrientsLoadingManure.Comments = model.Comment;
            nutrientsLoadingManure.DryMatterPercent = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.DryMatterPercent : model.ManureType.DryMatter;
            nutrientsLoadingManure.UricAcid = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.UricAcid : model.ManureType.Uric;
            nutrientsLoadingManure.K2O = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.K2O : model.ManureType.K2O;
            nutrientsLoadingManure.MgO = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.MgO : model.ManureType.MgO;
            nutrientsLoadingManure.SO3 = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.SO3 : model.ManureType.SO3;
            nutrientsLoadingManure.NH4N = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.NH4N : model.ManureType.NH4N;
            nutrientsLoadingManure.NO3N = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.NO3N : model.ManureType.NO3N;

            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            var jsonData = new
            {
                NutrientsLoadingManure = nutrientsLoadingManure,
                SaveDefaultForFarm = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? true : false
            };

            string jsonString = JsonConvert.SerializeObject(jsonData);
            (NutrientsLoadingManures nutrientsLoadingManureData, error) = await _reportService.AddNutrientsLoadingManuresAsync(jsonString);
            if (nutrientsLoadingManureData != null && string.IsNullOrWhiteSpace(error.Message))
            {
                string successMsg = _reportDataProtector.Protect(string.Format(Resource.MsgImportExportSuccessMsgContent1, Resource.lblAdded, model.ImportExport == (int)NMP.Portal.Enums.ImportExport.Import ? Resource.lblImport : Resource.lblExport));
                model.ImportExport = null;
                model.LivestockImportExportDate = null;
                model.ManureTypeId = null;
                model.ManureTypeName = null;
                model.DefaultFarmManureValueDate = null;
                model.DefaultNutrientValue = null;
                model.LivestockQuantity = null;
                model.ReceiverName = null;
                model.Postcode = null;
                model.Address1 = null;
                model.Address3 = null;
                model.Address2 = null;
                model.Address4 = null;
                model.Comment = null;
                model.IsCheckAnswer = false;
                model.IsManureTypeChange = false;
                model.LivestockImportExportQuestion = null;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("ManageImportExport", new
                {
                    q = model.EncryptedFarmId,
                    y = _farmDataProtector.Protect(model.Year.ToString()),
                    r = successMsg,
                    s = Resource.lblTrue
                });
            }
            else
            {
                TempData["ErrorOnCheckYourAnswers"] = error.Message;
            }
            return RedirectToAction("LivestockImportExportCheckAnswer");
        }


        [HttpGet]
        public async Task<IActionResult> ManageImportExport(string q, string y, string r, string s)
        {
            _logger.LogTrace($"Report Controller : ManageImportExport() action called");
            ReportViewModel model = new ReportViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                if (string.IsNullOrWhiteSpace(model.IsComingFromImportExportOverviewPage))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                    }
                }

                int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    if (!string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(s))
                    {

                        //HttpContext?.Session.Remove("ReportData");
                        //model.IsComingFromImportExportOverviewPage = _reportDataProtector.Protect(Resource.lblTrue);
                        TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(r);
                        TempData["succesMsgContent2"] = Resource.MsgImportExportSuccessMsgContent2;
                        TempData["succesMsgContent3"] = string.Format(Resource.MsgImportExportSuccessMsgContent3, _farmDataProtector.Unprotect(y));
                    }
                    model.FarmName = farm.Name;
                    model.FarmId = decryptedFarmId;
                    model.EncryptedFarmId = q;
                    if (!string.IsNullOrWhiteSpace(y))
                    {
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                        model.EncryptedHarvestYear = y;
                    }
                    List<HarvestYear> harvestYearList = new List<HarvestYear>();
                    (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportService.FetchNutrientsLoadingManuresByFarmId(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (nutrientsLoadingManuresList != null && nutrientsLoadingManuresList.Count > 0)
                        {
                            nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year.Value).ToList();
                            if (nutrientsLoadingManuresList.Count > 0)
                            {
                                HarvestYear harvestYear = new HarvestYear();
                                foreach (var nutrientsLoadingManure in nutrientsLoadingManuresList)
                                {
                                    harvestYear.LastModifiedOn = nutrientsLoadingManure.ModifiedOn != null ? nutrientsLoadingManure.ModifiedOn.Value : nutrientsLoadingManure.CreatedOn.Value;
                                    harvestYear.Year = nutrientsLoadingManure.ManureDate.Value.Year;
                                    harvestYearList.Add(harvestYear);
                                }

                                harvestYearList.OrderBy(x => x.Year).ToList();
                                model.HarvestYear = harvestYearList;
                                ViewBag.ImportList = nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper()).ToList();
                                string unit = "";
                                (Farm farmData, error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                                if (string.IsNullOrWhiteSpace(error.Message) && farmData != null)
                                {
                                    (List<ManureType> ManureTypes, error) = await _organicManureService.FetchManureTypeList((int)NMP.Portal.Enums.ManureGroup.LivestockManure, farmData.CountryID.Value);
                                    if (error == null && ManureTypes != null && ManureTypes.Count > 0)
                                    {
                                        var allImportData = nutrientsLoadingManuresList
                                       .Where(x => x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper())
                                       .Select(x => new
                                       {
                                           Manure = x,
                                           Unit = (ManureTypes.FirstOrDefault(mt => mt.Id.HasValue && mt.Id.Value == x.ManureTypeID)?.IsLiquid ?? false)
                                            ? Resource.lblCubicMeters
                                            : Resource.lbltonnes
                                       })
                                       .ToList();
                                        ViewBag.ImportList = allImportData;
                                        var allExportData = nutrientsLoadingManuresList
                                       .Where(x => x.ManureLookupType?.ToUpper() == Resource.lblExport.ToUpper())
                                       .Select(x => new
                                       {
                                           Manure = x,
                                           Unit = (ManureTypes.FirstOrDefault(mt => mt.Id.HasValue && mt.Id.Value == x.ManureTypeID)?.IsLiquid ?? false)
                                            ? Resource.lblCubicMeters
                                            : Resource.lbltonnes
                                       })
                                       .ToList();
                                        ViewBag.ExportList = allExportData;
                                    }
                                }
                                decimal? totalImports = (nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper()).Sum(x => x.Quantity)) * 1000;
                                ViewBag.TotalImportsInKg = totalImports;
                                //ViewBag.ExportList = nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblExport.ToUpper()).ToList();
                                decimal? totalExports = (nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblExport.ToUpper()).Sum(x => x.Quantity)) * 1000;
                                ViewBag.TotalExportsInKg = totalExports;
                                ViewBag.NetTotal = totalImports - totalExports;
                                ViewBag.IsImport = _reportDataProtector.Protect(Resource.lblImport);
                                ViewBag.IsExport = _reportDataProtector.Protect(Resource.lblExport);
                            }
                        }
                    }
                    else
                    {
                        TempData["Error"] = error.Message;
                        return RedirectToAction("FarmSummary", "Farm", new { q = q });
                    }
                    if (nutrientsLoadingManuresList.Count > 0)
                    {
                        nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                        if (nutrientsLoadingManuresList.Count == 0)
                        {
                            model.IsManageImportExport = false;
                            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                            return RedirectToAction("LivestockImportExportQuestion", model);
                        }
                    }
                    else
                    {
                        model.IsManageImportExport = false;
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                        return RedirectToAction("LivestockImportExportQuestion", model);
                    }
                }
                else
                {
                    TempData["Error"] = error.Message;
                    return RedirectToAction("FarmSummary", "Farm", new { q = q });
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            if (!string.IsNullOrWhiteSpace(y))
            {
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                model.EncryptedHarvestYear = y;
            }

            model.IsManageImportExport = true;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
            return View(model);
        }
        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("Report Controller : Cancel() action called");
            ReportViewModel model = new ReportViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckYourAnswers"] = ex.Message;
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : Cancel() post action called");
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
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
            else
            {
                model.ImportExport = null;
                model.LivestockImportExportDate = null;
                model.ManureTypeId = null;
                model.ManureTypeName = null;
                model.DefaultFarmManureValueDate = null;
                model.DefaultNutrientValue = null;
                model.LivestockQuantity = null;
                model.ReceiverName = null;
                model.Postcode = null;
                model.Address1 = null;
                model.Address3 = null;
                model.Address2 = null;
                model.Address4 = null;
                model.Comment = null;
                model.IsCheckAnswer = false;
                model.IsManureTypeChange = false;
                model.LivestockImportExportQuestion = null;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsManageImportExport)
                {
                    return RedirectToAction("ManageImportExport", "Report", new { q = model.EncryptedFarmId, y = _farmDataProtector.Protect(model.Year.Value.ToString()) });

                }
                else if (string.IsNullOrWhiteSpace(model.IsComingFromImportExportOverviewPage))
                {                    
                    if (!model.IsCheckList)
                    {
                        return RedirectToAction("FarmSummary", "Farm", new { Id = model.EncryptedFarmId });

                    }
                    else
                    {
                        return RedirectToAction("LivestockManureNitrogenReportChecklist", "Report");

                    }
                }
                else
                {
                    return RedirectToAction("UpdateLivestockImportExport", "Report", new { q = model.EncryptedFarmId });
                }
            }
        }
    }

}