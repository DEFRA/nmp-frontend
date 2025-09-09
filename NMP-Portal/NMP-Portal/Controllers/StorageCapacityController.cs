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
                    
                    model.FarmName = farm.Name;
                    model.FarmID = decryptedFarmId;
                    model.EncryptedFarmID = q;
                    if (!string.IsNullOrWhiteSpace(y))
                    {
                        model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
                        model.EncryptedHarvestYear = y;
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
                    else
                    {
                        model.IsCheckAnswer = false;
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
                    else
                    {
                        model.IsCheckAnswer = false;
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

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains("StorageCapacityData"))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityData");
                }
                

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
                if(model.MaterialStateID==(int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
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
                if(model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.DirtyWaterStorage || model.MaterialStateID == (int)NMP.Portal.Enums.MaterialState.SlurryStorage)
                {
                    if(model.StorageTypeID == (int)NMP.Portal.Enums.StorageTypes.EarthBankedLagoon)
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
                (BankSlopeAnglesResponse bankSlopeAngle, error) = await _storageCapacityService.FetchBankSlopeAngleById(model.BankSlopeAngleID??0);
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
                if(model.MaterialStateID==(int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    model.CapacityVolume = model.CapacityWeight;
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
            _logger.LogTrace("Report Controller : IsAnyLivestockNumber() post action called");
            try
            {
                Error error = null;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);

                int? solidManureTypeId = null;
                if(model.MaterialStateID==(int)NMP.Portal.Enums.MaterialState.SolidManureStorage)
                {
                    solidManureTypeId = model.StorageTypeID;
                }

                var storeCapacityData = new StoreCapacity()
                {
                    FarmID = model.FarmID,
                    Year = model.Year,
                    StoreName = model.StoreName,
                    MaterialStateID = model.MaterialStateID,
                    StorageTypeID = model.StorageTypeID,
                    SolidManureTypeID = solidManureTypeId,
                    Length = model.Length,
                    Width = model.Width,
                    Depth = model.Depth,
                    Circumference = model.Circumference,
                    Diameter=model.Diameter,
                    BankSlopeAngleID = model.BankSlopeAngleID,
                    IsCovered = model.IsCovered,
                    CapacityVolume = model.CapacityVolume,
                    CapacityWeight = model.CapacityWeight,
                    SurfaceArea=model.SurfaceArea

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
                    bool success = true;
                    string successMsg = Resource.lblYouHaveAddedManureStorage;
                    return RedirectToAction("ManageStorageCapacity", "StorageCapacity", new
                    {
                        q = model.EncryptedFarmID,
                        y = model.EncryptedHarvestYear,
                        r = _reportDataProtector.Protect(successMsg),
                        s = _reportDataProtector.Protect(success.ToString())
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Report Controller : Exception in IsAnyLivestockNumber() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnIsAnyLivestockNumber"] = ex.Message;
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

            //var cattle = (int)NMP.Portal.Enums.LivestockGroup.Cattle;
            //var pigs = (int)NMP.Portal.Enums.LivestockGroup.Pigs;
            //var poultry = (int)NMP.Portal.Enums.LivestockGroup.Poultry;
            //var sheep = (int)NMP.Portal.Enums.LivestockGroup.Sheep;
            //var goatsDeerOrHorses = (int)NMP.Portal.Enums.LivestockGroup.GoatsDeerOrHorses;

            //if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            //{
            //    if (model.LivestockNumberQuestion == (int)NMP.Portal.Enums.LivestockNumberQuestion.AverageNumberForTheYear)
            //    {
            //        return RedirectToAction("AverageNumber");
            //    }
            //    else if (model.LivestockNumberQuestion == (int)NMP.Portal.Enums.LivestockNumberQuestion.ANumberForEachMonth)
            //    {
            //        return RedirectToAction("LivestockNumbersMonthly");
            //    }
            //}
            //else
            //{
            //    if (model.IsGrasslandDerogation == false)
            //    {
            //        if (model.OccupancyAndNitrogenOptions == (int)NMP.Portal.Enums.OccupancyNitrogenOptions.ChangeOccupancy)
            //        {
            //            return RedirectToAction("Occupancy");
            //        }
            //        else if (model.OccupancyAndNitrogenOptions == (int)NMP.Portal.Enums.OccupancyNitrogenOptions.ChangeNitrogen)
            //        {
            //            return RedirectToAction("NitrogenStandard");
            //        }
            //        else if (model.OccupancyAndNitrogenOptions == (int)NMP.Portal.Enums.OccupancyNitrogenOptions.UseDefault)
            //        {
            //            return RedirectToAction("OccupancyAndStandard");
            //        }
            //    }
            //    else
            //    {
            //        return RedirectToAction("NitrogenStandard");
            //    }
            //}


            //return RedirectToAction("AverageNumber");

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
                    if(model.IsCircumference==true)
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
                        capacity= l * w * effDepth;
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
                    capacity = model.StorageBagCapacity??0m;
                    surfaceArea = 0m;
                    break;
            }

            return (capacity, surfaceArea);
        }

    }
}
