using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    public class StorageCapacityController : Controller
    {
        private readonly ILogger<StorageCapacityController> _logger;
        private readonly IDataProtector _reportDataProtector;
        private readonly IDataProtector _farmDataProtector;
        private readonly IDataProtector _storageCapacityProtector;
        private readonly IAddressLookupService _addressLookupService;
        private readonly IUserFarmService _userFarmService;
        private readonly IFarmService _farmService;
        private readonly IFieldService _fieldService;
        private readonly ICropService _cropService;
        private readonly IOrganicManureService _organicManureService;
        private readonly IFertiliserManureService _fertiliserManureService;
        private readonly IReportService _reportService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public StorageCapacityController(ILogger<StorageCapacityController> logger,
            IDataProtectionProvider dataProtectionProvider,
            IAddressLookupService addressLookupService,
            IUserFarmService userFarmService,
            IFarmService farmService,
            IFieldService fieldService,
            ICropService cropService,
            IOrganicManureService organicManureService,
            IFertiliserManureService fertiliserManureService,
            IReportService reportService,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _reportDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.ReportController");
            _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
            _storageCapacityProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.StorageCapacityProtector");
            _addressLookupService = addressLookupService;
            _userFarmService = userFarmService;
            _farmService = farmService;
            _fieldService = fieldService;
            _cropService = cropService;
            _organicManureService = organicManureService;
            _fertiliserManureService = fertiliserManureService;
            _reportService = reportService;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpGet]
        public async Task<IActionResult> ManageStorageCapacity(string q, string y, string? r, string? s)
        {
            _logger.LogTrace($"Report Controller : ManageStorageCapacity() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            if (!string.IsNullOrWhiteSpace(q))
            {
                int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    //if (!string.IsNullOrWhiteSpace(r))
                    //{
                    //    TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(r);
                    //    if (!string.IsNullOrWhiteSpace(s))
                    //    {
                    //        ViewBag.isComingFromSuccessMsg = _reportDataProtector.Protect(Resource.lblTrue);
                    //        TempData["succesMsgContent2"] = Resource.lblAddMoreLivestock;
                    //        TempData["succesMsgContent3"] = string.Format(Resource.lblCreateALivestockManureNitrogenFarmLimitReport, _farmDataProtector.Unprotect(y));
                    //    }
                    //}
                    model.FarmName = farm.Name;
                    model.FarmId = decryptedFarmId;
                    model.EncryptedFarmId = q;
                    if (!string.IsNullOrWhiteSpace(y))
                    {
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                        model.EncryptedHarvestYear = y;
                    }

                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("ReportData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("ReportData");
                    }

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("ReportData", model);
                    List<HarvestYear> harvestYearList = new List<HarvestYear>();
                    (List<StoreCapacity> storeCapacityList, error) = await _reportService.FetchStoreCapacityByFarmIdAndYear(decryptedFarmId, model.Year ?? 0);

                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (storeCapacityList != null && storeCapacityList.Count > 0)
                        {
                            (List<CommonResponse> materialStateList, error) = await _reportService.FetchMaterialStates();
                            if (materialStateList != null && materialStateList.Count > 0)
                            {
                                if (storeCapacityList
                                .Any(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage))
                                {
                                    ViewBag.DirtyWaterList = storeCapacityList
                                    .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage)
                                    .Select(x => new
                                    {
                                        MaterialStateName = materialStateList.FirstOrDefault(m => m.Id == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage)?.Name,
                                        x.StoreName,
                                        x.Length,
                                        x.Width,
                                        x.Depth,
                                        x.Diameter,
                                        x.CapacityWeight,
                                        x.CapacityVolume,
                                        x.Circumference,
                                        x.BankSlopeAngleID,
                                        x.SurfaceArea,
                                        x.StorageTypeID
                                    })
                                    .ToList();
                                }

                                if (storeCapacityList
                                .Any(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage))
                                {
                                    ViewBag.SlurryStorageList = storeCapacityList
                                .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                                .Select(x => new
                                {
                                    MaterialStateName = materialStateList.FirstOrDefault(m => m.Id == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)?.Name,
                                    x.StoreName,
                                    x.Length,
                                    x.Width,
                                    x.Depth,
                                    x.Diameter,
                                    x.CapacityWeight,
                                    x.CapacityVolume,
                                    x.Circumference,
                                    x.BankSlopeAngleID,
                                    x.SurfaceArea,
                                    x.StorageTypeID
                                })
                                .ToList();
                                }


                                if (storeCapacityList
                              .Any(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage))
                                {
                                    ViewBag.SolidManureStorageList = storeCapacityList
                                .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                                .Select(x => new
                                {
                                    MaterialStateName = materialStateList.FirstOrDefault(m => m.Id == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)?.Name,
                                    x.StoreName,
                                    x.Length,
                                    x.Width,
                                    x.Depth,
                                    x.Diameter,
                                    x.CapacityWeight,
                                    x.CapacityVolume,
                                    x.Circumference,
                                    x.BankSlopeAngleID,
                                    x.SurfaceArea,
                                    x.StorageTypeID
                                })
                                .ToList();
                                }

                            }

                        }
                    }
                    if (storeCapacityList.Count > 0)
                    {
                        return View(model);
                    }
                    return RedirectToAction("OrganicMaterialStorageNotAvailable");
                }
                else
                {
                    TempData["Error"] = error.Message;
                    return RedirectToAction("FarmSummary", "Farm", new { q = q });
                }


            }
            if (!string.IsNullOrWhiteSpace(y))
            {
                model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                model.EncryptedHarvestYear = y;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OrganicMaterialStorageNotAvailable()
        {
            _logger.LogTrace("Report Controller : OrganicMaterialStorageNotAvailable() action called");
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
                _logger.LogTrace($"Report Controller : Exception in OrganicMaterialStorageNotAvailable() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnYear"] = ex.Message;
                return RedirectToAction("Year");
            }
            return View(model);
        }
    }
}
