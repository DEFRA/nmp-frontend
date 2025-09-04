using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Enums;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using NMP.Portal.ViewModels;
using Error = NMP.Portal.ServiceResponses.Error;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            _logger.LogTrace($"StorageCapacity Controller : ManageStorageCapacity() action called");
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
            _logger.LogTrace("StorageCapacity Controller : OrganicMaterialStorageNotAvailable() action called");
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in OrganicMaterialStorageNotAvailable() action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnYear"] = ex.Message;
                return RedirectToAction("Year", "Report");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MaterialStates()
        {
            _logger.LogTrace("StorageCapacity Controller : MaterialStates() action called");
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in MaterialStates() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StoreName()
        {
            _logger.LogTrace("StorageCapacity Controller : StoreName() action called");
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
                _logger.LogTrace($"StorageCapacity Controller : Exception in StoreName() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return RedirectToAction("MaterialStates");

            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StoreName(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : StoreName() post action called");
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
                if (model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.StorageBag)
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
                if (model.StorageTypeId != (int)NMP.Portal.Enums.StorageTypes.StorageBag)
                {
                    if (model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
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
                        if (model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.SquareOrRectangularTank || model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
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
                        if (model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.CircularTank)
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
                                }
                                else
                                {
                                    if (model.Diameter == null)
                                    {
                                        ModelState.AddModelError("Diameter", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDiameter.ToLower()));
                                    }
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

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if(model.MaterialStateId==(int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    return RedirectToAction("WeightCapacity");
                }
                if(model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage || model.MaterialStateId == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    if(model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
                    {
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
                (SolidManureTypeResponse solidManureTypeResponse, Error error) = await _storageCapacityService.FetchSolidManureTypeById(model.StorageTypeId.Value);
                if (error == null)
                {
                    model.SolidManureDensity = solidManureTypeResponse.Density;
                    model.WeightCapacity = Math.Round((model.Length * model.Width * model.Depth) * (solidManureTypeResponse.Density) ?? 0);  //solid manure weight capacity calculation
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
                if (model.WeightCapacity == null)
                {
                    ModelState.AddModelError("WeightCapacity", Resource.MsgEnterTheWeightCapacityBeforeContinuing);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                if (model.WeightCapacity != Math.Round((model.Length * model.Width * model.Depth) * (model.SolidManureDensity) ?? 0))
                {
                    model.Length = null;
                    model.Width = null;
                    model.Depth = null;
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

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if(model.IsSlopeEdge==false)
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
                if (model.BankSlopeAngleId == null)
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
                (BankSlopeAnglesResponse bankSlopeAngle, error) = await _storageCapacityService.FetchBankSlopeAngleById(model.BankSlopeAngleId??0);
                if (error == null)
                {
                    model.BankSlopeAngleName = bankSlopeAngle.Name;
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
                if(model.MaterialStateId==(int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    model.CapacityVolume = model.WeightCapacity;
                }
                else
                {
                    if (model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.StorageBag)
                    {
                        model.CapacityVolume = model.StorageBagCapacity;
                    }
                    else
                    {
                        decimal freeBoard = 0;
                        (StorageTypeResponse storageTypeResponse, error) = await _storageCapacityService.FetchStorageTypeById(model.StorageTypeId.Value);
                        if (error == null)
                        {
                            freeBoard = storageTypeResponse.FreeBoardHeight;
                        }
                       

                        if(model.StorageTypeId==(int)NMP.Portal.Enums.StorageTypes.SquareOrRectangularTank)
                        {
                            model.CapacityVolume = Math.Round((model.Length * model.Width * (model.Depth - freeBoard)) ?? 0);
                            model.SurfaceArea= Math.Round((model.Length * model.Width)??0);
                        }
                        else if (model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.CircularTank)
                        {
                            decimal radius = 0;
                            if (model.Diameter != null)
                            {
                                radius = model.Diameter ?? 0 / 2;
                            }
                            if(model.Circumference != null)
                            {
                                radius = (model.Circumference ?? 0) / (2 * (22 / 7));
                            }
                            decimal area = (22 / 7) * (radius * radius);
                            model.CapacityVolume = area * (model.Depth - freeBoard) ?? 0;
                            model.SurfaceArea = area;
                        }
                        else if(model.StorageTypeId == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
                        {

                        }
                    }
                }
                model.SurfaceArea = Math.Round((model.Length * model.Width) ?? 0);//check on planet
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
    }
}
