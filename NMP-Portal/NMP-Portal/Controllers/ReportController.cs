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
        public async Task<IActionResult> ExportFields(string i, string j)
        {
            _logger.LogTrace("Crop Controller : ExportFields() action called");
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
                        //int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                        (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(model.FarmId.Value);
                        if (farm != null)
                        {
                            model.FarmName = farm.Name;
                        }
                    }
                }
                if (model != null && model.FarmId != null)
                {
                    (List<HarvestYearPlanResponse> fieldList, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        var SelectListItem = fieldList.Select(f => new SelectListItem
                        {
                            Value = f.FieldID.ToString(),
                            Text = f.FieldName
                        }).ToList();
                        ViewBag.fieldList = SelectListItem;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ExportFields() action : {ex.Message}, {ex.StackTrace}");
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
        public async Task<IActionResult> ExportFields(ReportViewModel model)
        {
            _logger.LogTrace("Report Controller : ExportFields() post action called");
            try
            {
                int farmID = Convert.ToInt32(_farmDataProtector.Unprotect(model.EncryptedFarmId));
                (List<HarvestYearPlanResponse> fieldList, Error error) = await _cropService.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
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
                        ViewBag.fieldList = selectListItem;
                        return View("ExportFields", model);
                    }
                    if (model.FieldList.Count == 1 && model.FieldList[0] == Resource.lblSelectAll)
                    {
                        model.FieldList = selectListItem.Select(item => item.Value).ToList();
                    }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);

                    return RedirectToAction("CropAndFieldManagement");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in ExportFields() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSelectField"] = ex.Message;
                return View(model);
            }
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
            if (model.CropAndFieldReport != null && model.CropAndFieldReport.Farm != null)
            {
                if (model.CropAndFieldReport.Farm.Fields != null && model.CropAndFieldReport.Farm.Fields.Count > 0)
                {

                    decimal totalFarmArea = 0;

                    int totalGrassArea = 0;
                    int totalArableArea = 0;
                    foreach (var fieldData in model.CropAndFieldReport.Farm.Fields)
                    {
                        if (fieldData.Crops != null && fieldData.Crops.Count > 0)
                        {
                            totalFarmArea += fieldData.TotalArea.Value * fieldData.Crops.Count;
                            foreach (var cropData in fieldData.Crops)
                            {
                                if (cropData.CropTypeID == (int)NMP.Portal.Enums.CropTypes.Grass)
                                {
                                    totalGrassArea += (int)Math.Round(fieldData.TotalArea.Value);
                                }
                                else
                                {
                                    totalArableArea += (int)Math.Round(fieldData.TotalArea.Value);
                                }
                                
                            }                            
                        }
                        
                    }
                    model.CropAndFieldReport.Farm.GrassArea = totalGrassArea;
                    model.CropAndFieldReport.Farm.ArableArea = totalArableArea;
                    model.CropAndFieldReport.Farm.TotalFarmArea = totalFarmArea;
                }
            }
            _logger.LogTrace("Report Controller : CropAndFieldManagement() post action called");
            return View(model);
        }
    }
}
