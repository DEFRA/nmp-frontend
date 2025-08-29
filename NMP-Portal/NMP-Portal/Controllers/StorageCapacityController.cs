using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
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
        private readonly IStorageCapacityService _storageCapacityService;
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
            IStorageCapacityService storageCapacityService,
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
            _storageCapacityService = storageCapacityService;
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

                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                    }

                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                    List<HarvestYear> harvestYearList = new List<HarvestYear>();
                    (List<StoreCapacity> storeCapacityList, error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(decryptedFarmId, model.Year ?? 0);

                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (storeCapacityList != null && storeCapacityList.Count > 0)
                        {
                            (List<CommonResponse> materialStateList, error) = await _storageCapacityService.FetchMaterialStates();
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
        public IActionResult OrganicMaterialStorageNotAvailable()
        {
            _logger.LogTrace("Report Controller : OrganicMaterialStorageNotAvailable() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
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
                return RedirectToAction("Year", "Report");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MaterialStates()
        {
            _logger.LogTrace("Report Controller : MaterialStates() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (List<CommonResponse> materialStateList, Error error) = await _storageCapacityService.FetchMaterialStates();
                if (error == null)
                {
                    ViewBag.MaterialStateList = materialStateList;
                }
                else
                {
                    TempData["ErrorOnOrganicMaterialStorageNotAvailable"] = error.Message;
                    return RedirectToAction("OrganicMaterialStorageNotAvailable");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in MaterialStates() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnOrganicMaterialStorageNotAvailable"] = ex.Message;
                return RedirectToAction("OrganicMaterialStorageNotAvailable");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MaterialStates(StorageCapacityViewModel model)
        {
            _logger.LogTrace("Report Controller : MaterialStates() post action called");
            try
            {
                if (model.MaterialStateId == null)
                {
                    ModelState.AddModelError("MaterialStateId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                Error error = null;
                if (!ModelState.IsValid)
                {
                    (List<CommonResponse> materialStateList, error) = await _storageCapacityService.FetchMaterialStates();
                    if (error == null)
                    {
                        ViewBag.MaterialStateList = materialStateList;
                    }
                    return View(model);
                }

                (CommonResponse materialState, error) = await _storageCapacityService.FetchMaterialStateById(model.MaterialStateId.Value);
                if (error == null)
                {
                    model.MaterialStateName = materialState.Name;
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                return RedirectToAction("StoreName");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in MaterialStates() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult StoreName()
        {
            _logger.LogTrace("Report Controller : StoreName() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in StoreName() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return RedirectToAction("MaterialStates");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StoreName(StorageCapacityViewModel model)
        {
            _logger.LogTrace("Report Controller : StoreName() post action called");
            try
            {
                if (string.IsNullOrWhiteSpace(model.StoreName))
                {
                    ModelState.AddModelError("StoreName", Resource.lblEnterANameForYourOrganicMaterialStore);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                return RedirectToAction("StorageTypes");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in StoreName() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStoreName"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> StorageTypes()
        {
            _logger.LogTrace("Report Controller : StorageTypes() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                    model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    (List<StorageTypeResponse> storageTypes, Error error) = await _storageCapacityService.FetchStorageTypes();
                    if (error == null)
                    {
                        ViewBag.StorageTypes = storageTypes;
                    }
                    else
                    {
                        TempData["ErrorOnStoreName"] = error.Message;
                        return RedirectToAction("StoreName");
                    }
                }
                else
                {
                    (List<SolidManureTypeResponse> solidManureTypeList, Error error) = await _storageCapacityService.FetchSolidManureType();
                    if (error == null)
                    {
                        ViewBag.SolidManureTypeList = solidManureTypeList;
                    }
                    else
                    {
                        TempData["ErrorOnStoreName"] = error.Message;
                        return RedirectToAction("StoreName");
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in StorageTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStoreName"] = ex.Message;
                return RedirectToAction("StoreName");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StorageTypes(StorageCapacityViewModel model)
        {
            _logger.LogTrace("Report Controller : StorageTypes() post action called");
            try
            {
                if (model.StorageTypeId == null)
                {
                    ModelState.AddModelError("StorageTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                Error error = null;
                if (!ModelState.IsValid)
                {
                    if (model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                    {
                        (List<StorageTypeResponse> storageTypes,  error) = await _storageCapacityService.FetchStorageTypes();
                        if (error == null)
                        {
                            ViewBag.StorageTypes = storageTypes;
                        }
                    }
                    else
                    {
                        (List<SolidManureTypeResponse> solidManureTypeList,  error) = await _storageCapacityService.FetchSolidManureType();
                        if (error == null)
                        {
                            ViewBag.SolidManureTypeList = solidManureTypeList;
                        }

                    }
                    return View(model);
                }

                if (model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    (StorageTypeResponse storageTypeResponse, error) = await _storageCapacityService.FetchStorageTypeById(model.StorageTypeId.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = storageTypeResponse.Name;
                    }
                }
                else
                {
                    (SolidManureTypeResponse solidManureTypeResponse, error) = await _storageCapacityService.FetchSolidManureTypeById(model.StorageTypeId.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = solidManureTypeResponse.Name;
                    }
                }
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                return RedirectToAction("Dimension");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in StorageTypes() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageTypes"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult Dimension()
        {
            _logger.LogTrace("Report Controller : Dimension() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in Dimension() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageTypes"] = ex.Message;
                return RedirectToAction("StorageTypes");
            }
            return View(model);
        }


    }
}
