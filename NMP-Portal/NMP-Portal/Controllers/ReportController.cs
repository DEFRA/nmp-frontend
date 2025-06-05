using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Diagnostics.Metrics;
using Error = NMP.Portal.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IDataProtector _dataProtector;
        private readonly IDataProtector _farmDataProtector;
        private readonly IAddressLookupService _addressLookupService;
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ReportController(ILogger<ReportController> logger, IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor, IAddressLookupService addressLookupService,
            IUserFarmService userFarmService, IFarmService farmService,
            IFieldService fieldService, ICropService cropService)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.ReportController");
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _addressLookupService = addressLookupService;
            _userFarmService = userFarmService;
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
            _httpContextAccessor = httpContextAccessor;
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
                if (model.ReportType != null && model.ReportType == (int)NMP.Portal.Enums.ReportType.CropAndFieldManagementReport)
                {
                    (List<HarvestYearPlanResponse> fieldList, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
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
                else
                {
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message)&& farm!=null)
                    {
                        (List<HarvestYearPlanResponse> cropTypeList, error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                        if (string.IsNullOrWhiteSpace(error.Message) && cropTypeList != null && cropTypeList.Count > 0)
                        {
                            (List<CropTypeLinkingResponse> cropTypeLinking, error) = await _cropService.FetchCropTypeLinking();
                            if (error==null&& cropTypeLinking!=null&& cropTypeLinking.Count>0)
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
                                .ToList();
                                var SelectListItem = cropTypeList.Select(f => new SelectListItem
                                {
                                    Value = f.CropTypeID.ToString(),
                                    Text = f.CropTypeName
                                }).ToList();
                                ViewBag.CropTypeList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ExportFieldsOrCropType() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnReportSelection"] = ex.Message;
                return RedirectToAction("ReportType");
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
                if (model.ReportType != null && model.ReportType == (int)NMP.Portal.Enums.ReportType.CropAndFieldManagementReport)
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
                            return View("ExportFields", model);
                        }
                        if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblSelectAll)
                        {
                            model.FieldList = selectListItem.Select(item => item.Value).ToList();
                        }
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    }
                    return RedirectToAction("CropAndFieldManagement");

                }
                else
                {
                    //fetch crop type
                    (Farm farm,  error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
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
                                .ToList();
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
                                    return View("ExportFieldsOrCropType", model);
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

                        totalFarmArea += fieldData.TotalArea.Value;
                        if (fieldData.Crops != null && fieldData.Crops.Count > 0)
                        {

                            // * fieldData.Crops.Count;
                            foreach (var cropData in fieldData.Crops)
                            {
                                totalCount++;
                                if (cropData.CropOrder == 1)
                                {
                                    cropData.SwardManagementName = cropData.SwardManagementName;
                                    cropData.EstablishmentName = cropData.EstablishmentName;
                                    cropData.SwardTypeName = cropData.SwardTypeName;
                                    cropData.DefoliationSequenceName = cropData.DefoliationSequenceName;
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
                                        (List<DefoliationSequenceResponse> defResponse, Error grassError) = await _cropService.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(cropData.SwardManagementID.Value, cropData.PotentialCut.Value,cropData.Establishment>0?true:false);
                                        if (grassError == null && defResponse.Count > 0)
                                        {
                                            defolicationName = defResponse.Where(x => x.DefoliationSequenceId == cropData.DefoliationSequenceID).Select(x => x.DefoliationSequenceDescription).FirstOrDefault();
                                        }
                                    }
                                }
                                int defIndex = 0;
                                foreach (var manData in cropData.ManagementPeriods)
                                {
                                    var defolicationParts = (!string.IsNullOrWhiteSpace(defolicationName)) ? defolicationName.Split(',') : null;
                                    if (manData != null)
                                    {
                                        manData.DefoliationSequenceName = (defolicationParts != null && defIndex < defolicationParts.Length) ? defolicationParts[defIndex] : string.Empty;
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
        public async Task<IActionResult> ReportType(string i, string j)
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
                    ModelState.AddModelError("ReportType",Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View("ReportType", model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                //if (model.ReportType != null && model.ReportType == (int)NMP.Portal.Enums.ReportType.CropAndFieldManagementReport)
                //{
                return RedirectToAction("ExportFieldsOrCropType");
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

                if (model.CropTypeList != null)
                {
                    (model.Farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                    if (model.Farm != null && string.IsNullOrWhiteSpace(error.Message))
                    {
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                        //(error, List<Field> fieldList) = await _fieldService.FetchFieldByFarmId(model.Farm.ID, Resource.lblFalse);
                        //if (fieldList != null && fieldList.Count > 0 && string.IsNullOrWhiteSpace(error.Message))
                        //{
                        //    foreach (var field in fieldList)
                        //    {
                        //        List<Crop> cropList = await _cropService.FetchCropsByFieldId(field.ID.Value);
                        //        if (cropList.Count > 0)
                        //        {
                        //            cropList = cropList.Where(x => x.Year == model.Year).ToList();
                        //            if (cropList.Any(c => model.CropTypeList.Contains(c.CropTypeID.Value.ToString())))
                        //            {
                        //                foreach (var crop in cropList)
                        //                {

                        //                }
                        //            }
                        //        }
                        //        //(Crop crop,Error error)=await _cropService.fetchcropty
                        //    }
                        //}
                    }
                }

            }

            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in NMaxReport() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                return RedirectToAction("HarvestYearOverview", new
                {
                    id = model.EncryptedFarmId,
                    year = model.EncryptedHarvestYear
                });
            }
            return View(model);
        }
    }
}
