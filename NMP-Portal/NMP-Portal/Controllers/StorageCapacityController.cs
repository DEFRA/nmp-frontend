using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using System.Diagnostics.Metrics;
using System.Reflection;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Error = NMP.Portal.ServiceResponses.Error;
using System.Linq;

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
        public async Task<IActionResult> ManageStorageCapacity(string q, string y, string? r, string? s, string isPlan)
        {
            _logger.LogTrace($"StorageCapacity Controller : ManageStorageCapacity() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
            {
                HttpContext?.Session.Remove("StorageCapacityData");
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    model.FarmName = farm.Name;
                    model.FarmID = decryptedFarmId;
                    model.EncryptedFarmID = q;
                    if (!string.IsNullOrWhiteSpace(y))
                    {
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                        model.EncryptedHarvestYear = y;
                    }
                    if (!string.IsNullOrWhiteSpace(isPlan))
                    {
                        model.IsComingFromPlan = Convert.ToBoolean(_reportDataProtector.Unprotect(isPlan));
                        ViewBag.IsPlan = isPlan;
                    }

                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(r);
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            ViewBag.isComingFromSuccessMsg = _reportDataProtector.Protect(Resource.lblTrue);
                            TempData["succesMsgContent1Detail"] = string.Format(Resource.MsgToCreateAnExistingManureStorageCapacityReportEnterAllThe, model.FarmName, model.Year);
                            TempData["succesMsgContent2"] = Resource.lblAddMoreManureStorage;
                            TempData["succesMsgContent3"] = Resource.lblCreateAnExistingManureStorageCapacityReport;
                        }
                    }


                    //_httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                    List<HarvestYear> harvestYearList = new List<HarvestYear>();
                    (List<StoreCapacityResponse> storeCapacityList, error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(decryptedFarmId, model.Year ?? 0);

                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        if (storeCapacityList != null && storeCapacityList.Count > 0)
                        {
                            model.IsStoreCapacityExist = true;
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
                                        x.StorageTypeID,
                                        x.StorageTypeName,
                                        x.SolidManureTypeName
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
                                    x.StorageTypeID,
                                    x.StorageTypeName,
                                    x.SolidManureTypeName
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
                                    x.StorageTypeID,
                                    x.StorageTypeName,
                                    x.SolidManureTypeName
                                })
                                .ToList();
                                }

                                ViewBag.TotalLiquidCapacity = storeCapacityList
                                    .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage || x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage).Sum(x => x.CapacityVolume);

                                ViewBag.TotalSolidCapacity = storeCapacityList
                                    .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage).Sum(x => x.CapacityVolume);

                                ViewBag.TotalSolidWeightCapacity = storeCapacityList
                                    .Where(x => x.Year == model.Year && x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage).Sum(x => x.CapacityWeight);

                                ViewBag.TotalSurfaceCapacity = storeCapacityList
                                    .Where(x => x.Year == model.Year).Sum(x => x.SurfaceArea);

                                ViewBag.EncryptedSolidStateId = _storageCapacityProtector.Protect(Convert.ToString((int)NMP.Portal.Enums.MaterialState.SolidManureStorage));
                                ViewBag.EncryptedSlurryStateId = _storageCapacityProtector.Protect(Convert.ToString((int)NMP.Portal.Enums.MaterialState.SlurryStorage));
                                ViewBag.EncryptedDirtyWaterStateId = _storageCapacityProtector.Protect(Convert.ToString((int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage));

                            }

                        }
                    }
                    //_httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                    if (storeCapacityList.Count > 0)
                    {
                        return View(model);
                    }
                    return RedirectToAction("OrganicMaterialStorageNotAvailable", "StorageCapacity", new { f = q, y = y, isPlan = isPlan });
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
        public async Task<IActionResult> OrganicMaterialStorageNotAvailable(string? f, string? y, string? isPlan)
        {
            _logger.LogTrace("StorageCapacity Controller : OrganicMaterialStorageNotAvailable() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (!string.IsNullOrWhiteSpace(f))
                {
                    int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(f));
                    (Farm farm, Error error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.FarmID = decryptedFarmId;
                        model.EncryptedFarmID = f;
                        if (!string.IsNullOrWhiteSpace(y))
                        {
                            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                            model.EncryptedHarvestYear = y;
                        }
                        if (!string.IsNullOrWhiteSpace(isPlan))
                        {
                            model.IsComingFromPlan = Convert.ToBoolean(_reportDataProtector.Unprotect(isPlan));
                            ViewBag.IsPlan = isPlan;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in OrganicMaterialStorageNotAvailable() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnYear"] = ex.Message;
                return RedirectToAction("Year", "Report");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MaterialStates(string? f, string? y, string? isPlan)
        {
            _logger.LogTrace("StorageCapacity Controller : MaterialStates() action called");
            StorageCapacityViewModel? model = new StorageCapacityViewModel();
            Error? error =null;
            int? decryptedFarmId = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("StorageCapacityData", model);
                }

                //if (!string.IsNullOrWhiteSpace(q))
                //{
                //    model.EncryptedFarmID = q;
                //    model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                //}
                //if (!string.IsNullOrWhiteSpace(y))
                //{
                //    model.EncryptedHarvestYear = y;
                //    model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                //}

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

                if (!string.IsNullOrWhiteSpace(f))
                {
                    decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(f));
                    (Farm farm,  error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId??0);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.FarmID = decryptedFarmId;
                        model.EncryptedFarmID = f;
                        if (!string.IsNullOrWhiteSpace(y))
                        {
                            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                            model.EncryptedHarvestYear = y;
                        }
                        if (!string.IsNullOrWhiteSpace(isPlan))
                        {
                            model.IsComingFromPlan = Convert.ToBoolean(_reportDataProtector.Unprotect(isPlan));
                        }
                        
                    }
                }
                int farmId= decryptedFarmId ?? model.FarmID ?? 0;
                (List<StoreCapacityResponse> storeCapacityList, error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(farmId, model.Year ?? 0);

                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (storeCapacityList.Count > 0)
                    {
                        ViewBag.StoreCapacityList = storeCapacityList;
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("StorageCapacityData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in MaterialStates() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnOrganicMaterialStorageNotAvailable"] = ex.Message;
                return RedirectToAction("OrganicMaterialStorageNotAvailable");

            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MaterialStates(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : MaterialStates() post action called");
            try
            {
                if (model.MaterialStateID == null)
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

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.MaterialStateID == storageModel.MaterialStateID)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }

                (CommonResponse materialState, error) = await _storageCapacityService.FetchMaterialStateById(model.MaterialStateID.Value);
                if (error == null)
                {
                    model.MaterialStateName = materialState.Name;
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                return RedirectToAction("StoreName");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in MaterialStates() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StoreName(string? f, string? y, string? isPlan, string? q)
        {
            _logger.LogTrace("StorageCapacity Controller : StoreName() action called");
            StorageCapacityViewModel? model = new StorageCapacityViewModel();
            Error? error = null;
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    //return RedirectToAction("FarmList", "Farm");
                }
                if (!string.IsNullOrWhiteSpace(f))
                {
                    int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(f));
                    (Farm farm, error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.FarmID = decryptedFarmId;
                        model.EncryptedFarmID = f;
                        if (!string.IsNullOrWhiteSpace(y))
                        {
                            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                            model.EncryptedHarvestYear = y;
                        }
                        if (!string.IsNullOrWhiteSpace(isPlan))
                        {
                            model.IsComingFromPlan = Convert.ToBoolean(_reportDataProtector.Unprotect(isPlan));
                        }

                        (List<StoreCapacityResponse> storeCapacityList, error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(decryptedFarmId, model.Year ?? 0);

                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            if (storeCapacityList.Count > 0)
                            {
                                ViewBag.StoreCapacityList = storeCapacityList;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    model.EncryptedMaterialStateID = q;
                    model.MaterialStateID = Convert.ToInt32(_storageCapacityProtector.Unprotect(q));
                    (CommonResponse materialState, error) = await _storageCapacityService.FetchMaterialStateById(model.MaterialStateID.Value);
                    if (error == null)
                    {
                        model.MaterialStateName = materialState.Name;
                    }
                }
                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("StorageCapacityData", model ?? new StorageCapacityViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StoreName() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return RedirectToAction("MaterialStates");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StoreName(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : StoreName() post action called");
            try
            {
                if (string.IsNullOrWhiteSpace(model.StoreName))
                {
                    ModelState.AddModelError("StoreName", Resource.lblEnterANameForYourOrganicMaterialStore);
                }

                (bool isStoreNameExists, Error error) = await _storageCapacityService.IsStoreNameExistAsync(model.FarmID ?? 0, model.Year ?? 0, model.StoreName);

                if (error == null)
                {
                    if (isStoreNameExists)
                    {
                        ModelState.AddModelError("StoreName", Resource.MsgStoreAlreadyExists);
                    }
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.StoreName == storageModel.StoreName)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                return RedirectToAction("StorageTypes");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StoreName() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStoreName"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> StorageTypes()
        {
            _logger.LogTrace("StorageCapacity Controller : StorageTypes() action called");
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
                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                    model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStoreName"] = ex.Message;
                return RedirectToAction("StoreName");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StorageTypes(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : StorageTypes() post action called");
            try
            {
                if (model.StorageTypeID == null)
                {
                    ModelState.AddModelError("StorageTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                Error error = null;
                if (!ModelState.IsValid)
                {
                    if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                    {
                        (List<StorageTypeResponse> storageTypes, error) = await _storageCapacityService.FetchStorageTypes();
                        if (error == null)
                        {
                            ViewBag.StorageTypes = storageTypes;
                        }
                    }
                    else
                    {
                        (List<SolidManureTypeResponse> solidManureTypeList, error) = await _storageCapacityService.FetchSolidManureType();
                        if (error == null)
                        {
                            ViewBag.SolidManureTypeList = solidManureTypeList;
                        }

                    }
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.StorageTypeID == storageModel.StorageTypeID)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }

                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    (StorageTypeResponse storageTypeResponse, error) = await _storageCapacityService.FetchStorageTypeById(model.StorageTypeID.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = storageTypeResponse.Name;
                        model.FreeBoardHeight = storageTypeResponse.FreeBoardHeight;
                    }
                }
                else
                {
                    (SolidManureTypeResponse solidManureTypeResponse, error) = await _storageCapacityService.FetchSolidManureTypeById(model.StorageTypeID.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = solidManureTypeResponse.Name;
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.StorageBag)
                {
                    model.Length = null;
                    model.Width = null;
                    model.Depth = null;
                    model.IsCovered = null;
                    model.Circumference = null;
                    model.Diameter = null;
                    model.IsCircumference = null;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                    return RedirectToAction("StorageBagCapacity");
                }
                else
                {
                    return RedirectToAction("Dimensions");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageTypes() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageTypes"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult Dimensions()
        {
            _logger.LogTrace("StorageCapacity Controller : Dimension() action called");
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in Dimension() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageTypes"] = ex.Message;
                return RedirectToAction("StorageTypes");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Dimensions(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : Dimension() post action called");
            try
            {
                if (model.StorageTypeID != (int)NMP.Portal.Enums.StorageTypes.StorageBag)
                {
                    if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                    {
                        if (model.Length == null)
                        {
                            ModelState.AddModelError("Length", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblLength.ToLower()));
                        }
                        if (model.Width == null)
                        {
                            ModelState.AddModelError("Width", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblWidth.ToLower()));
                        }
                        if (model.Depth == null)
                        {
                            ModelState.AddModelError("Depth", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDepth.ToLower()));
                        }
                    }
                    else
                    {
                        if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.SquareOrRectangularTank || model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
                        {
                            if (model.Length == null)
                            {
                                ModelState.AddModelError("Length", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblLength.ToLower()));
                            }
                            if (model.Width == null)
                            {
                                ModelState.AddModelError("Width", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblWidth.ToLower()));
                            }
                            if (model.Depth == null)
                            {
                                ModelState.AddModelError("Depth", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDepth.ToLower()));
                            }
                            if (model.IsCovered == null)
                            {
                                ModelState.AddModelError("IsCovered", string.Format(Resource.MsgSelectIfYourStorageIsCovered, model.StoreName));
                            }
                        }
                        if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.CircularTank)
                        {
                            if (model.IsCircumference == null)
                            {
                                ModelState.AddModelError("CircumferenceOrDiameter", Resource.MsgSelectCircumferenceOrDiameterBeforeContinuing);
                            }
                            else
                            {
                                if (model.IsCircumference == true)
                                {
                                    if (model.Circumference == null)
                                    {
                                        ModelState.AddModelError("Circumference", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblCircumference.ToLower()));
                                    }
                                    model.Diameter = null;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                                }
                                else
                                {
                                    if (model.Diameter == null)
                                    {
                                        ModelState.AddModelError("Diameter", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDiameter.ToLower()));
                                    }
                                    model.Circumference = null;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                                }
                            }
                            if (model.Depth == null)
                            {
                                ModelState.AddModelError("Depth", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDepth.ToLower()));
                            }
                            if (model.IsCovered == null)
                            {
                                ModelState.AddModelError("IsCovered", string.Format(Resource.MsgSelectIfYourStorageIsCovered, model.StoreName));
                            }
                        }
                    }

                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }


                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    if (model.IsCheckAnswer)
                    {
                        if (model.Length == storageModel.Length && model.Width == storageModel.Width && model.Depth == storageModel.Depth)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                    }
                    return RedirectToAction("WeightCapacity");
                }
                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage || model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
                    {
                        if (model.IsCheckAnswer)
                        {
                            if (model.Length == storageModel.Length && model.Width == storageModel.Width && model.Depth == storageModel.Depth && model.IsCovered == storageModel.IsCovered)
                            {
                                return RedirectToAction("CheckAnswer");
                            }
                        }
                        return RedirectToAction("SlopeQuestion");
                    }
                    else
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in Dimension() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnDimension"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> WeightCapacity()
        {
            _logger.LogTrace("StorageCapacity Controller : WeightCapacity() action called");
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
                (SolidManureTypeResponse solidManureTypeResponse, Error error) = await _storageCapacityService.FetchSolidManureTypeById(model.StorageTypeID.Value);
                if (error == null)
                {
                    model.SolidManureDensity = solidManureTypeResponse.Density;
                    model.CapacityWeight = Math.Round((model.Length * model.Width * model.Depth) * (solidManureTypeResponse.Density) ?? 0);  //solid manure weight capacity calculation
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in WeightCapacity() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnDimension"] = ex.Message;
                return RedirectToAction("Dimensions");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WeightCapacity(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : Dimension() post action called");
            try
            {
                if (model.CapacityWeight == null)
                {
                    ModelState.AddModelError("WeightCapacity", Resource.MsgEnterTheWeightCapacityBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (model.CapacityWeight != Math.Round((model.Length * model.Width * model.Depth) * (model.SolidManureDensity) ?? 0))
                {
                    model.Length = null;
                    model.Width = null;
                    model.Depth = null;
                }
                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.CapacityWeight == storageModel.CapacityWeight)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in WeightCapacity() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnWeightCapacity"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StorageBagCapacity()
        {
            _logger.LogTrace("StorageCapacity Controller : StorageBagCapacity() action called");
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageBagCapacity() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageTypes"] = ex.Message;
                return RedirectToAction("StorageTypes");
            }
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StorageBagCapacity(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : StorageBagCapacity() post action called");
            try
            {
                if (model.StorageBagCapacity == null)
                {
                    ModelState.AddModelError("StorageBagCapacity", Resource.MsgEnterTheTotalCapacityOfYourStorage);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.StorageBagCapacity == storageModel.StorageBagCapacity)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageBagCapacity() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageBagCapacity"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult SlopeQuestion()
        {
            _logger.LogTrace($"StorageCapacity Controller : SlopeQuestion() action called");
            StorageCapacityViewModel? model = new StorageCapacityViewModel();
            if (HttpContext.Session.Keys.Contains("StorageCapacityData"))
            {
                model = HttpContext.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SlopeQuestion(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : SlopeQuestion() post action called");
            try
            {
                if (model.IsSlopeEdge == null)
                {
                    ModelState.AddModelError("IsSlopeEdge", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                if (model.IsCheckAnswer)
                {
                    if (model.IsSlopeEdge == storageModel.IsSlopeEdge)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if (model.IsSlopeEdge == false)
                {
                    return RedirectToAction("CheckAnswer");
                }
                return RedirectToAction("BankSlopeAngle");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in SlopeQuestion() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSlopeQuestion"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> BankSlopeAngle()
        {
            _logger.LogTrace("StorageCapacity Controller : BankSlopeAngle() action called");
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

                (List<BankSlopeAnglesResponse> bankSlopeAngles, Error error) = await _storageCapacityService.FetchBankSlopeAngles();
                if (error == null)
                {
                    ViewBag.BankSlopeAngles = bankSlopeAngles;
                }
                else
                {
                    TempData["ErrorOnSlopeQuestion"] = error.Message;
                    return RedirectToAction("SlopeQuestion");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageTypes() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnSlopeQuestion"] = ex.Message;
                return RedirectToAction("SlopeQuestion");

            }
            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BankSlopeAngle(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : BankSlopeAngle() post action called");
            try
            {
                Error error = null;
                if (model.BankSlopeAngleID == null)
                {
                    ModelState.AddModelError("BankSlopeAngleId", Resource.MsgSelectAnOptionBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    (List<BankSlopeAnglesResponse> bankSlopeAngles, error) = await _storageCapacityService.FetchBankSlopeAngles();
                    if (error == null)
                    {
                        ViewBag.BankSlopeAngles = bankSlopeAngles;
                    }
                    return View(model);
                }
                (BankSlopeAnglesResponse bankSlopeAngle, error) = await _storageCapacityService.FetchBankSlopeAngleById(model.BankSlopeAngleID ?? 0);
                if (error == null)
                {
                    model.BankSlopeAngleName = bankSlopeAngle.Name;
                    model.Slope = bankSlopeAngle.Slope;
                }
                else
                {
                    TempData["ErrorOnBankSlopeAngle"] = error.Message;
                    return RedirectToAction("BankSlopeAngle");
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in BankSlopeAngle() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnBankSlopeAngle"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckAnswer()
        {
            _logger.LogTrace("StorageCapacity Controller : CheckAnswer() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                Error error = null;
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    model.CapacityVolume = model.Length * model.Width * model.Depth;
                    model.SurfaceArea = model.Length * model.Width;
                }
                else
                {
                    (decimal CapacityVolume, decimal SurfaceArea) = CalculateCapacityAndArea(model);
                    model.CapacityVolume = Math.Round(CapacityVolume);
                    model.SurfaceArea = Math.Round(SurfaceArea);

                }

                model.IsCheckAnswer = true;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in CheckAnswer() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckAnswer"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAnswer(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : CheckAnswer() post action called");
            try
            {
                Error error = null;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    model.SolidManureTypeID = model.StorageTypeID;
                    model.StorageTypeID = null;
                }

                var storeCapacityData = new StoreCapacity()
                {
                    FarmID = model.FarmID,
                    Year = model.Year,
                    StoreName = model.StoreName,
                    MaterialStateID = model.MaterialStateID,
                    StorageTypeID = model.StorageTypeID,
                    SolidManureTypeID = model.SolidManureTypeID,
                    Length = model.Length,
                    Width = model.Width,
                    Depth = model.Depth,
                    Circumference = model.Circumference,
                    Diameter = model.Diameter,
                    BankSlopeAngleID = model.BankSlopeAngleID,
                    IsCovered = model.IsCovered,
                    CapacityVolume = model.CapacityVolume,
                    CapacityWeight = model.CapacityWeight,
                    SurfaceArea = model.SurfaceArea

                };
                (StoreCapacity StoreCapacityData, error) = await _storageCapacityService.AddStoreCapacityAsync(storeCapacityData);
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["ErrorOnCheckAnswer"] = error.Message;
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    HttpContext?.Session.Remove("StorageCapacityData");
                    bool success = true;
                    string successMsg = Resource.lblYouHaveAddedManureStorage;
                    return RedirectToAction("ManageStorageCapacity", "StorageCapacity", new
                    {
                        q = model.EncryptedFarmID,
                        y = model.EncryptedHarvestYear,
                        r = _reportDataProtector.Protect(successMsg),
                        s = _reportDataProtector.Protect(success.ToString()),
                        isPlan=_reportDataProtector.Protect(model.IsComingFromPlan.ToString())
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in CheckAnswer() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckAnswer"] = ex.Message;
                return View(model);
            }
        }
        public IActionResult BackStoreCapacityCheckAnswer()
        {
            _logger.LogTrace($"Farm Controller : BackLivestockCheckAnswer() action called");
            StorageCapacityViewModel? model = null;
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
            if (model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
            {
                return RedirectToAction("CapacityWeight");
            }
            else
            {
                if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.StorageBag)
                {
                    return RedirectToAction("StorageBagCapacity");
                }
                else if (model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
                {
                    if (model.IsSlopeEdge == false)
                    {
                        return RedirectToAction("SlopeQuestion");
                    }
                    else
                    {
                        return RedirectToAction("BankSlopeAngle");
                    }

                }
                else
                {
                    return RedirectToAction("Dimensions");
                }
            }

        }

        public static (decimal CapacityVolume, decimal SurfaceArea) CalculateCapacityAndArea(StorageCapacityViewModel model)
        {
            int typeId = model.StorageTypeID ?? 0;
            bool covered = model.IsCovered ?? false;
            decimal l = model.Length ?? 0m;
            decimal w = model.Width ?? 0m;
            decimal d = model.Depth ?? 0m;
            decimal diameter = model.Diameter ?? 0m;
            decimal circumference = model.Circumference ?? 0m;
            decimal slope = model.Slope ?? 0m;
            decimal freeboardDefault = model.FreeBoardHeight ?? 0m;

            decimal freeboardToUse = covered ? 0m : freeboardDefault;
            decimal effDepth = d - freeboardToUse;
            if (effDepth < 0m) effDepth = 0m;

            decimal capacity = 0m;
            decimal surfaceArea = 0m;

            switch (typeId)
            {
                case (int)NMP.Portal.Enums.StorageTypes.SquareOrRectangularTank:
                    capacity = l * w * effDepth;
                    surfaceArea = l * w;
                    break;
                case (int)NMP.Portal.Enums.StorageTypes.CircularTank:
                    if (model.IsCircumference == true)
                    {
                        diameter = circumference / (decimal)Math.PI;
                    }
                    decimal r = diameter / 2m;
                    capacity = (decimal)Math.PI * r * r * effDepth;
                    surfaceArea = (decimal)Math.PI * r * r;
                    break;
                case (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon:
                    if (model.IsSlopeEdge == false)
                    {
                        capacity = l * w * effDepth;
                        surfaceArea = l * w;
                    }
                    else
                    {
                        decimal areaBottom = l * w;
                        decimal lengthTop = l + 2m * slope * effDepth;
                        decimal widthTop = w + 2m * slope * effDepth;
                        decimal areaTop = lengthTop * widthTop;
                        capacity = (effDepth / 3m) * (areaBottom + areaTop + (decimal)Math.Sqrt((double)(areaBottom * areaTop)));
                        surfaceArea = areaTop;
                    }

                    break;
                case (int)NMP.Portal.Enums.StorageTypes.StorageBag:
                    capacity = model.StorageBagCapacity ?? 0m;
                    surfaceArea = 0m;
                    break;
            }

            return (capacity, surfaceArea);
        }

        [HttpGet]
        public async Task<IActionResult> StorageCapacityReport()
        {
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            try
            {
                (List<StoreCapacityResponse> storeCapacities, Error error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(model.FarmID.Value, model.Year.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && storeCapacities.Count > 0)
                {
                    (model.Farm, error) = await _farmService.FetchFarmByIdAsync(model.FarmID.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && model.Farm != null)
                    {
                        model.FarmName = model.Farm.Name;
                        List<StoreCapacityResponse> solidStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage).ToList();
                        if (solidStoreCapacities.Count > 0)
                        {
                            ViewBag.SolidStoreCapacities = solidStoreCapacities;
                        }
                        List<StoreCapacityResponse> dirtyWaterStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage).ToList();
                        if (dirtyWaterStoreCapacities.Count > 0)
                        {
                            ViewBag.DirtyWaterStoreCapacities = dirtyWaterStoreCapacities;
                        }
                        List<StoreCapacityResponse> slurryStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage).ToList();
                        if (slurryStoreCapacities.Count > 0)
                        {
                            ViewBag.SlurryStoreCapacities = slurryStoreCapacities;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorOnManageStorageCapacity"] = ex.Message;
                _logger.LogTrace("StorageCapacity Controller : StorageCapacityReport() get action called");
                return RedirectToAction("ManageStorageCapacity", new
                {
                    q = model.EncryptedFarmID,
                    y = model.EncryptedHarvestYear
                });
            }
            _logger.LogTrace("StorageCapacity Controller : StorageCapacityReport() get action called");
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> StorageCapacityManagement(string q)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                try
                {
                    ViewBag.EncryptedFarmId = q;
                    int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    List<int> fixedYearList = GetReportYearsList();

                    (List<StoreCapacity> storeCapacities, Error error) = await _storageCapacityService.FetchStoreCapacityByFarmIdAndYear(decryptedFarmId, null);

                    if (string.IsNullOrWhiteSpace(error.Message) && storeCapacities.Count > 0)
                    {
                        (Farm farm, error) = await _farmService.FetchFarmByIdAsync(decryptedFarmId);
                        if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                        {
                            ViewBag.FarmName = farm.Name;
                            var storeYears = storeCapacities
                            .Select(sc => sc.Year)
                            .Where(y => y.HasValue)
                            .Select(y => y.Value)
                            .Distinct()
                            .ToList();

                            var finalYearList = fixedYearList.Select(year =>
                            {
                                var entries = storeCapacities
                                    .Where(x => x.Year == year)
                                    .OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn)
                                    .ToList();

                                var latestEntry = entries.FirstOrDefault();

                                var lastModifyDate = latestEntry != null
                                    ? (latestEntry.ModifiedOn ?? latestEntry.CreatedOn)?.ToString("yyyy-MMMM-dd")
                                    : Resource.lblHyphen;

                                return new
                                {
                                    Year = year,
                                    EncryptedYear = _farmDataProtector.Protect(year.ToString()),
                                    Label = storeYears.Contains(year) ? Resource.lblUpdate : Resource.lblAdd,
                                    LastModifyDate = lastModifyDate
                                };
                            })
                            .Concat(
                                storeYears
                                    .Where(year => !fixedYearList.Contains(year))
                                    .Select(year =>
                                    {
                                        var entries = storeCapacities
                                            .Where(x => x.Year == year)
                                            .OrderByDescending(x => x.ModifiedOn ?? x.CreatedOn)
                                            .ToList();

                                        var latestEntry = entries.FirstOrDefault();

                                        var lastModifyDate = latestEntry != null
                                            ? (latestEntry.ModifiedOn ?? latestEntry.CreatedOn)?.ToString("yyyy-MMMM-dd")
                                            : Resource.lblHyphen;

                                        return new
                                        {
                                            Year = year,
                                            EncryptedYear = _farmDataProtector.Protect(year.ToString()),
                                            Label = Resource.lblUpdate,
                                            LastModifyDate = lastModifyDate
                                        };
                                    })
                                        )
                            .OrderByDescending(x => x.Year)
                            .ToList();

                            ViewBag.FinalYearList = finalYearList;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorOnStorageCapacityManagement"] = ex.Message;
                    _logger.LogTrace("StorageCapacity Controller : StorageCapacityManagement() get action called");
                    return RedirectToAction("FarmSummary", "Farm", new
                    {
                        id = q
                    });
                }
            }
            _logger.LogTrace("StorageCapacity Controller : StorageCapacityManagement() get action called");
            return View();
        }
        private List<int> GetReportYearsList(int previousYears = 4)
        {
            int currentYear = DateTime.Now.Year;
            List<int> years = new List<int>();

            // Next year
            years.Add(currentYear + 1);

            // Current year
            years.Add(currentYear);

            // Previous years
            for (int i = 1; i <= previousYears; i++)
            {
                years.Add(currentYear - i);
            }

            return years;
        }

    }
}
