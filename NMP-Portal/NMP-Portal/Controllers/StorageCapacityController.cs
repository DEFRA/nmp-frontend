using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NMP.Application;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using Error = NMP.Commons.ServiceResponses.Error;

namespace NMP.Portal.Controllers
{
    public class StorageCapacityController(ILogger<StorageCapacityController> logger,
        IDataProtectionProvider dataProtectionProvider,
        IFarmLogic farmLogic,
        IStorageCapacityLogic storageCapacityLogic,
        IHttpContextAccessor httpContextAccessor) : Controller
    {
        private readonly ILogger<StorageCapacityController> _logger = logger;
        private readonly IDataProtector _reportDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.ReportController");
        private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        private readonly IDataProtector _storageCapacityProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.StorageCapacityController");
        private readonly IFarmLogic _farmLogic = farmLogic;
        private readonly IStorageCapacityLogic _storageCapacityLogic = storageCapacityLogic;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private const string _storageCapacityActionName = "StorageCapacity";
        private const string _storageCapacityDataSessionKey = "StorageCapacityData";
        private const string _farmSummaryActionName = "FarmSummary";
        [HttpGet]
        public async Task<IActionResult> ManageStorageCapacity(string q, string? r, string? s, string? isPlan, string? t, string? u)
        {
            _logger.LogTrace("StorageCapacity Controller : ManageStorageCapacity() action called");

            var model = new StorageCapacityViewModel();
            ClearStorageSessions();

            if (string.IsNullOrWhiteSpace(q))
                return View(model);

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            var (farm, farmError) = await _farmLogic.FetchFarmByIdAsync(farmId);

            if (!string.IsNullOrWhiteSpace(farmError.Message) || farm == null)
                return RedirectToFarmSummary(q, farmError.Message);

            PopulateFarmDetails(model, farm, farmId, q, isPlan, t);

            var (storeCapacityList, storeError) =
                await _storageCapacityLogic.FetchStoreCapacityByFarmId(farmId);

            HandleSuccessMessages(r, s, model, storeCapacityList);

            if (string.IsNullOrWhiteSpace(storeError.Message) && storeCapacityList.Any())
                await PopulateStorageLists(storeCapacityList);

            PopulateTotals(storeCapacityList);
            PopulateEncryptedStateIds();

            var storageCapacityList =
                await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID!.Value)
                .ContinueWith(t => t.Result.Item1);

            //copy logic write here...

            if (!string.IsNullOrWhiteSpace(u))
                model.IsRemovedRecently = u;

            if (storeCapacityList.Any() || !string.IsNullOrWhiteSpace(u))
            {
                ViewBag.StorageCapacityList = storageCapacityList.Any() ? storageCapacityList : null;
                return View(model);
            }

            return RedirectToAction(
                "OrganicMaterialStorageNotAvailable",
                _storageCapacityActionName,
                new { f = q, isPlan });
        }


        [HttpGet]
        public async Task<IActionResult> OrganicMaterialStorageNotAvailable(string? f, string? isPlan)
        {
            _logger.LogTrace("StorageCapacity Controller : OrganicMaterialStorageNotAvailable() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (!string.IsNullOrWhiteSpace(f))
                {
                    int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(f));
                    (FarmResponse farm, Error error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.FarmID = decryptedFarmId;
                        model.EncryptedFarmID = f;
                        if (!string.IsNullOrWhiteSpace(isPlan))
                        {
                            model.IsComingFromPlan = isPlan;
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
        public async Task<IActionResult> MaterialStates(string? f, string? isPlan, string? x, string? v, string? w)
        {
            _logger.LogTrace("StorageCapacity Controller : MaterialStates() action called");

            StorageCapacityViewModel model = GetOrCreateSessionModel();

            try
            {
                await LoadMaterialStatesAsync();
                await PopulateFarmDetailsAsync(model, f, isPlan, w);
                PopulateNavigationFlags(model, x, v);

                await CheckStoreCapacityAsync(model);

                SaveSessionModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Exception in MaterialStates(): {ex.Message}, {ex.StackTrace}");
                return await HandleMaterialStatesExceptionAsync(model, ex, f);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MaterialStates(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : MaterialStates() post action called");

            try
            {
                ValidateMaterialState(model);

                if (!ModelState.IsValid)
                {
                    await LoadMaterialStatesAsync();
                    return View(model);
                }

                var storageModel = GetStorageCapacityFromSession();

                if (ShouldRedirectToCheckAnswer(model, storageModel))
                {
                    return RedirectToAction("CheckAnswer");
                }

                HandleMaterialTypeChange(model, storageModel);

                await PopulateMaterialStateNameAsync(model);

                SaveStorageCapacityToSession(model);

                return RedirectToAction("StoreName");
            }
            catch (Exception ex)
            {
                _logger.LogTrace(
                    $"StorageCapacity Controller : Exception in MaterialStates() post action : {ex.Message}, {ex.StackTrace}");

                TempData["ErrorOnMaterialStates"] = ex.Message;
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> StoreName(string? f, string? isPlan, string? q, string? x, string? w)
        {
            _logger.LogTrace("StorageCapacity Controller : StoreName() action called");

            StorageCapacityViewModel model = GetSessionModel();

            try
            {
                await PopulateFarmAndStoreCapacityAsync(model, f, isPlan, x, w);
                await PopulateMaterialStateAsync(model, q);

                SaveSessionModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"Exception in StoreName(): {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnMaterialStates"] = ex.Message;
                return RedirectToAction("MaterialStates");
            }
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
                if (!string.IsNullOrWhiteSpace(model.StoreName))
                {
                    int? Id = !string.IsNullOrWhiteSpace(model.EncryptedStoreCapacityId) ? Convert.ToInt32(_storageCapacityProtector.Unprotect(model.EncryptedStoreCapacityId)) : null;

                    (bool isStoreNameExists, Error error) = await _storageCapacityLogic.IsStoreNameExistAsync(model.FarmID ?? 0, model.StoreName, Id);

                    if (error == null)
                    {
                        if (isStoreNameExists)
                        {
                            ModelState.AddModelError("StoreName", Resource.MsgStoreAlreadyExists);
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (model.IsCheckAnswer)
                {
                    if (!model.IsMaterialTypeChange)
                    {
                        return RedirectToAction("CheckAnswer");
                    }

                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage ||
                    model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                {
                    (List<StorageTypeResponse> storageTypes, Error error) = await _storageCapacityLogic.FetchStorageTypes();
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
                    (List<SolidManureTypeResponse> solidManureTypeList, Error error) = await _storageCapacityLogic.FetchSolidManureType();
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
                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                    {
                        (List<StorageTypeResponse> storageTypes, error) = await _storageCapacityLogic.FetchStorageTypes();
                        if (error == null)
                        {
                            ViewBag.StorageTypes = storageTypes;
                        }
                    }
                    else
                    {
                        (List<SolidManureTypeResponse> solidManureTypeList, error) = await _storageCapacityLogic.FetchSolidManureType();
                        if (error == null)
                        {
                            ViewBag.SolidManureTypeList = solidManureTypeList;
                        }

                    }
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                if (model.IsCheckAnswer)
                {
                    if (model.StorageTypeID == storageModel.StorageTypeID && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                    else
                    {
                        model.IsStorageTypeChange = true;
                    }
                }

                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                {
                    (StorageTypeResponse storageTypeResponse, error) = await _storageCapacityLogic.FetchStorageTypeById(model.StorageTypeID.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = storageTypeResponse.Name;
                        model.FreeBoardHeight = storageTypeResponse.FreeBoardHeight;
                    }
                }
                else
                {
                    (SolidManureTypeResponse solidManureTypeResponse, error) = await _storageCapacityLogic.FetchSolidManureTypeById(model.StorageTypeID.Value);
                    if (error == null)
                    {
                        model.StorageTypeName = solidManureTypeResponse.Name;
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                {
                    model.Length = null;
                    model.Width = null;
                    model.Depth = null;
                    model.IsCovered = null;
                    model.Circumference = null;
                    model.Diameter = null;
                    model.IsCircumference = null;
                    model.BankSlopeAngleID = null;
                    model.BankSlopeAngleName = null;
                    model.IsSlopeEdge = null;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
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
                if (model.StorageTypeID != (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                {
                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
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
                        if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.SquareOrRectangularTank || model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
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
                        if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.CircularTank)
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
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                                }
                                else
                                {
                                    if (model.Diameter == null)
                                    {
                                        ModelState.AddModelError("Diameter", string.Format(Resource.MsgEnterTheDimensionOfYourStorageBeforeContinuing, Resource.lblDiameter.ToLower()));
                                    }
                                    model.Circumference = null;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
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

                if (model.StorageTypeID != (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                {
                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage ||
                        model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.SquareOrRectangularTank ||
                        model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                    {
                        if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblLength))
                        {

                            var lengthError = ModelState[Resource.lblLength]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblLength]?.Errors[0].ErrorMessage.ToString() : null;

                            if (lengthError != null && lengthError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblLength]?.RawValue, Resource.lblLength)))
                            {
                                ModelState[Resource.lblLength]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblLength]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblLength]?.Errors.Add(lengthError);
                                }
                                else
                                {
                                    ModelState[Resource.lblLength]?.Errors.Add(Resource.MsgEnterAValueBetween0And999);
                                }
                            }
                        }
                        if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblDepth))
                        {

                            var depthError = ModelState[Resource.lblDepth]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblDepth]?.Errors[0].ErrorMessage.ToString() : null;

                            if (depthError != null && depthError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblDepth]?.RawValue, Resource.lblDepth)))
                            {
                                ModelState[Resource.lblDepth]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblDepth]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblDepth]?.Errors.Add(depthError);
                                }
                                else
                                {
                                    ModelState[Resource.lblDepth]?.Errors.Add(Resource.MsgEnterAValueBetween0And99);
                                }
                            }
                        }
                        if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblWidth))
                        {

                            var widthError = ModelState[Resource.lblWidth]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblWidth]?.Errors[0].ErrorMessage.ToString() : null;

                            if (widthError != null && widthError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblWidth]?.RawValue, Resource.lblWidth)))
                            {
                                ModelState[Resource.lblWidth]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblWidth]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblWidth]?.Errors.Add(widthError);
                                }
                                else
                                {
                                    ModelState[Resource.lblWidth]?.Errors.Add(Resource.MsgEnterAValueBetween0And999);
                                }
                            }
                        }
                    }
                    else if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.CircularTank && model.IsCircumference.HasValue)
                    {
                        if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblCircumference) && model.IsCircumference.Value)
                        {

                            var circumferenceError = ModelState[Resource.lblCircumference]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblCircumference]?.Errors[0].ErrorMessage.ToString() : null;

                            if (circumferenceError != null && circumferenceError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblCircumference]?.RawValue, Resource.lblCircumference)))
                            {
                                ModelState[Resource.lblCircumference]?.Errors.Clear();
                                ModelState[Resource.lblDiameter]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblCircumference]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblCircumference]?.Errors.Add(circumferenceError);
                                }
                                else
                                {
                                    ModelState[Resource.lblCircumference]?.Errors.Add(Resource.MsgEnterAValueBetween0And999);
                                }
                            }
                        }
                        else if (!model.IsCircumference.Value && (!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblDiameter))
                        {
                            var diameterError = ModelState[Resource.lblDiameter]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblDiameter]?.Errors[0].ErrorMessage.ToString() : null;

                            if (diameterError != null && diameterError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblDiameter]?.RawValue, Resource.lblDiameter)))
                            {
                                ModelState[Resource.lblCircumference]?.Errors.Clear();
                                ModelState[Resource.lblDiameter]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblDiameter]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblDiameter]?.Errors.Add(diameterError);
                                }
                                else
                                {
                                    ModelState[Resource.lblDiameter]?.Errors.Add(Resource.MsgEnterAValueBetween0And999);
                                }
                            }
                        }
                        if ((!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblDepth))
                        {

                            var depthError = ModelState[Resource.lblDepth]?.Errors.Count > 0 ?
                                            ModelState[Resource.lblDepth]?.Errors[0].ErrorMessage.ToString() : null;

                            if (depthError != null && depthError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblDepth]?.RawValue, Resource.lblDepth)))
                            {
                                ModelState[Resource.lblDepth]?.Errors.Clear();
                                decimal decimalValue;
                                if (decimal.TryParse(ModelState[Resource.lblDepth]?.RawValue?.ToString(), out decimalValue))
                                {
                                    ModelState[Resource.lblDepth]?.Errors.Add(depthError);
                                }
                                else
                                {
                                    ModelState[Resource.lblDepth]?.Errors.Add(Resource.MsgEnterAValueBetween0And99);
                                }
                            }
                        }
                    }
                }




                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }


                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                {
                    if (model.IsCheckAnswer)
                    {
                        if (model.Length == storageModel.Length && model.Width == storageModel.Width && model.Depth == storageModel.Depth && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
                        {
                            return RedirectToAction("CheckAnswer");
                        }
                    }
                    return RedirectToAction("WeightCapacity");
                }
                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage || model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                {
                    if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                    {
                        if (model.IsCheckAnswer)
                        {
                            if (model.Length == storageModel.Length && model.Width == storageModel.Width && model.Depth == storageModel.Depth && model.IsCovered == storageModel.IsCovered && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }
                (SolidManureTypeResponse solidManureTypeResponse, Error error) = await _storageCapacityLogic.FetchSolidManureTypeById(model.StorageTypeID.Value);
                if (error == null)
                {
                    model.SolidManureDensity = solidManureTypeResponse.Density;
                    model.CapacityWeight = Math.Round((model.Length * model.Width * model.Depth) * (solidManureTypeResponse.Density) ?? 0);  //solid manure weight capacity calculation
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
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
                ValidateWeightCapacity(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                if (model.IsCheckAnswer)
                {
                    if (model.CapacityWeight == storageModel.CapacityWeight && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }

                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in WeightCapacity() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnWeightCapacity"] = ex.Message;
                return View(model);
            }
        }

        private void ValidateWeightCapacity(StorageCapacityViewModel model)
        {
            if (model.CapacityWeight == null)
            {
                ModelState.AddModelError("WeightCapacity", Resource.MsgEnterTheWeightCapacityBeforeContinuing);
            }

            if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage &&
                (!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblCapacityWeight))
            {

                var capacityWeightError = ModelState[Resource.lblCapacityWeight]?.Errors.Count > 0 ?
                                ModelState[Resource.lblCapacityWeight]?.Errors[0].ErrorMessage.ToString() : null;

                if (capacityWeightError != null && capacityWeightError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblCapacityWeight]?.RawValue, Resource.lblCapacityWeight)))
                {
                    ModelState[Resource.lblCapacityWeight]?.Errors.Clear();
                    decimal decimalValue;
                    if (decimal.TryParse(ModelState[Resource.lblCapacityWeight]?.RawValue?.ToString(), out decimalValue))
                    {
                        ModelState[Resource.lblCapacityWeight]?.Errors.Add(capacityWeightError);
                    }
                    else
                    {
                        ModelState[Resource.lblCapacityWeight]?.Errors.Add(Resource.MsgEnterAValueBetween0And9999999999);
                    }
                }

            }
        }
        [HttpGet]
        public async Task<IActionResult> StorageBagCapacity()
        {
            _logger.LogTrace("StorageCapacity Controller : StorageBagCapacity() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
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
                ValidateStorageBagcapacity(model);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                StorageCapacityViewModel storageModel = new StorageCapacityViewModel();
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                if (model.IsCheckAnswer)
                {
                    if (model.StorageBagCapacity == storageModel.StorageBagCapacity && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
                    {
                        return RedirectToAction("CheckAnswer");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

                return RedirectToAction("CheckAnswer");
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in StorageBagCapacity() post action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnStorageBagCapacity"] = ex.Message;
                return View(model);
            }
        }

        private void ValidateStorageBagcapacity(StorageCapacityViewModel model)
        {
            if (model.StorageBagCapacity == null)
            {
                ModelState.AddModelError("StorageBagCapacity", Resource.MsgEnterTheTotalCapacityOfYourStorage);
            }

            if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag &&
                (!ModelState.IsValid) && ModelState.ContainsKey(Resource.lblStorageBagCapacity))
            {
                var storageBagCapacityError = ModelState[Resource.lblStorageBagCapacity]?.Errors.Count > 0 ?
                                ModelState[Resource.lblStorageBagCapacity]?.Errors[0].ErrorMessage.ToString() : null;

                if (storageBagCapacityError != null && storageBagCapacityError.Equals(string.Format(Resource.lblEnterNumericValue, ModelState[Resource.lblStorageBagCapacity]?.RawValue, Resource.lblStorageBagCapacity)) &&
                    !decimal.TryParse(ModelState[Resource.lblStorageBagCapacity]?.RawValue?.ToString(), out _))
                {
                    ModelState[Resource.lblStorageBagCapacity]?.Errors.Clear();
                    ModelState[Resource.lblStorageBagCapacity]?.Errors.Add(Resource.MsgEnterAValueBetween0And9999);
                }
            }
        }

        [HttpGet]
        public IActionResult SlopeQuestion()
        {
            _logger.LogTrace($"StorageCapacity Controller : SlopeQuestion() action called");
            StorageCapacityViewModel? model = new StorageCapacityViewModel();
            if (HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
            {
                model = HttpContext.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    storageModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                if (model.IsCheckAnswer)
                {
                    if (model.IsSlopeEdge == storageModel.IsSlopeEdge && !model.IsMaterialTypeChange && !model.IsStorageTypeChange)
                    {
                        _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                        return RedirectToAction("CheckAnswer");
                    }
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (model.IsSlopeEdge == false)
                {
                    model.BankSlopeAngleID = null;
                    model.Slope = null;
                    model.BankSlopeAngleName = null;
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
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
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

                (List<BankSlopeAnglesResponse> bankSlopeAngles, Error error) = await _storageCapacityLogic.FetchBankSlopeAngles();
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
                    (List<BankSlopeAnglesResponse> bankSlopeAngles, error) = await _storageCapacityLogic.FetchBankSlopeAngles();
                    if (error == null)
                    {
                        ViewBag.BankSlopeAngles = bankSlopeAngles;
                    }
                    return View(model);
                }
                (BankSlopeAnglesResponse bankSlopeAngle, error) = await _storageCapacityLogic.FetchBankSlopeAngleById(model.BankSlopeAngleID ?? 0);
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
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

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
        public async Task<IActionResult> CheckAnswer(string? storeCapId, string? q, string? r)
        {
            _logger.LogTrace("StorageCapacity Controller : CheckAnswer() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                Error error = null;
                if (string.IsNullOrWhiteSpace(storeCapId))
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                    {
                        model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                    }
                    else
                    {
                        return RedirectToAction("FarmList", "Farm");
                    }

                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                    {
                        model.CapacityVolume = model.Length * model.Width * model.Depth;
                        model.SurfaceArea = model.Length * model.Width;
                    }
                    else
                    {
                        (decimal CapacityVolume, decimal? SurfaceArea) = CalculateCapacityAndArea(model);
                        model.CapacityVolume = Math.Round(CapacityVolume);
                        model.SurfaceArea = SurfaceArea != null ? Math.Round(SurfaceArea ?? 0) : null;

                    }
                }
                else
                {
                    int storeCapacityId = Convert.ToInt32(_storageCapacityProtector.Unprotect(storeCapId));
                    (StoreCapacity storeCapacity, error) = await _storageCapacityLogic.FetchStoreCapacityByIdAsync(storeCapacityId);

                    model = new StorageCapacityViewModel
                    {
                        ID = storeCapacity.ID,
                        FarmID = storeCapacity.FarmID,
                        StoreName = storeCapacity.StoreName,
                        MaterialStateID = storeCapacity.MaterialStateID,
                        StorageTypeID = storeCapacity.StorageTypeID,
                        SolidManureTypeID = storeCapacity.SolidManureTypeID,
                        Length = storeCapacity.Length,
                        Width = storeCapacity.Width,
                        Depth = storeCapacity.Depth,
                        Circumference = storeCapacity.Circumference,
                        Diameter = storeCapacity.Diameter,
                        BankSlopeAngleID = storeCapacity.BankSlopeAngleID,
                        IsSlopeEdge = storeCapacity.BankSlopeAngleID != null ? true : null,
                        IsCovered = storeCapacity.IsCovered,
                        CapacityVolume = storeCapacity.CapacityVolume,
                        CapacityWeight = storeCapacity.CapacityWeight,
                        SurfaceArea = storeCapacity.SurfaceArea,
                        CreatedOn = storeCapacity.CreatedOn,
                        CreatedByID = storeCapacity.CreatedByID,
                        ModifiedOn = storeCapacity.ModifiedOn,
                        ModifiedByID = storeCapacity.ModifiedByID,
                    };

                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        model.IsComingFromManageToHubPage = q;
                    }
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        model.IsComingFromPlan = r;
                    }
                    (FarmResponse farm, error) = await _farmLogic.FetchFarmByIdAsync(storeCapacity.FarmID ?? 0);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.EncryptedFarmID = _farmDataProtector.Protect(storeCapacity.FarmID.ToString() ?? string.Empty);
                    }

                    (CommonResponse materialState, error) = await _storageCapacityLogic.FetchMaterialStateById(storeCapacity.MaterialStateID.Value);
                    if (error == null)
                    {
                        model.MaterialStateName = materialState.Name;
                    }

                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage ||
                   model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                    {
                        (StorageTypeResponse storageTypeResponse, error) = await _storageCapacityLogic.FetchStorageTypeById(model.StorageTypeID.Value);
                        if (error == null)
                        {
                            model.StorageTypeName = storageTypeResponse.Name;
                            model.FreeBoardHeight = storageTypeResponse.FreeBoardHeight;
                        }
                        if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                        {
                            model.StorageBagCapacity = storeCapacity.CapacityVolume;
                        }
                    }
                    else
                    {
                        model.StorageTypeID = storeCapacity.SolidManureTypeID;
                        (SolidManureTypeResponse solidManureTypeResponse, error) = await _storageCapacityLogic.FetchSolidManureTypeById(model.StorageTypeID.Value);
                        if (error == null)
                        {
                            model.StorageTypeName = solidManureTypeResponse.Name;
                        }
                    }
                    model.IsCircumference = storeCapacity.Circumference != null ? true : false;

                    model.EncryptedStoreCapacityId = storeCapId;

                }
                if (model.StorageTypeID != (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                {
                    model.IsSlopeEdge = null;
                    model.BankSlopeAngleID = null;
                    model.BankSlopeAngleName = null;
                    model.Slope = null;
                }
                if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                {
                    model.IsSlopeEdge = model.BankSlopeAngleID != null ? true : false;
                    if (model.BankSlopeAngleID != null)
                    {
                        (BankSlopeAnglesResponse bankSlopeAngle, error) = await _storageCapacityLogic.FetchBankSlopeAngleById(model.BankSlopeAngleID ?? 0);
                        if (error == null)
                        {
                            model.BankSlopeAngleName = bankSlopeAngle.Name;
                            model.Slope = bankSlopeAngle.Slope;
                        }
                    }

                }
                else if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                {
                    model.SurfaceArea = null;
                    model.Length = null;
                    model.Width = null;
                    model.Depth = null;
                    model.Circumference = null;
                    model.Diameter = null;
                    model.IsCircumference = null;
                    model.IsCovered = null;

                }
                else if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                {
                    model.IsCovered = null;
                }

                model.IsCheckAnswer = true;
                model.IsMaterialTypeChange = false;
                model.IsStorageTypeChange = false;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

                if (!string.IsNullOrWhiteSpace(storeCapId))
                {
                    _httpContextAccessor.HttpContext?.Session.SetObjectAsJson("StorageCapacityDataBeforeUpdate", model);

                }
                var previousModel = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>("StorageCapacityDataBeforeUpdate");

                bool isDataChanged = false;

                if (previousModel != null)
                {
                    string oldJson = JsonConvert.SerializeObject(previousModel);
                    string newJson = JsonConvert.SerializeObject(model);

                    isDataChanged = !string.Equals(oldJson, newJson, StringComparison.Ordinal);
                }
                ViewBag.IsDataChange = isDataChanged;

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

                //Validation start
                if (model.MaterialStateID == null)
                {
                    ModelState.AddModelError("MaterialStateId", Resource.MsgWhatKindOFManureStorageDoYouWantToAddNotSet);
                }
                if (string.IsNullOrWhiteSpace(model.StoreName))
                {
                    ModelState.AddModelError("StoreName", Resource.MsgWhatDoYouWantToCallThisManureStoreNotSet);
                }
                if (model.StorageTypeID == null)
                {
                    ModelState.AddModelError("StorageTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (model.StorageTypeID != (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                {
                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                    {
                        if (model.Length == null)
                        {
                            ModelState.AddModelError("Length", Resource.MsgWhatIsTheLengthNotSet);
                        }
                        if (model.Width == null)
                        {
                            ModelState.AddModelError("Width", Resource.MsgWhatIsTheWidthNotSet);
                        }
                        if (model.Depth == null)
                        {
                            ModelState.AddModelError("Depth", Resource.MsgWhatIsTheDepthNotSet);
                        }
                    }
                    else
                    {
                        if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.SquareOrRectangularTank || model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                        {
                            if (model.Length == null)
                            {
                                ModelState.AddModelError("Length", Resource.MsgWhatIsTheLengthNotSet);
                            }
                            if (model.Width == null)
                            {
                                ModelState.AddModelError("Width", Resource.MsgWhatIsTheWidthNotSet);
                            }
                            if (model.Depth == null)
                            {
                                ModelState.AddModelError("Depth", Resource.MsgWhatIsTheDepthNotSet);
                            }
                            if (model.IsCovered == null)
                            {
                                ModelState.AddModelError("IsCovered", string.Format(Resource.MsgIsCoveredNotSet, model.StoreName));
                            }
                        }
                        if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.CircularTank)
                        {
                            if (model.IsCircumference == null)
                            {
                                ModelState.AddModelError("IsCircumference", Resource.MsgDoYouWantToEnterTheCircumferenceOrDiameterNotSet);
                            }
                            else
                            {
                                if (model.IsCircumference == true)
                                {
                                    if (model.Circumference == null)
                                    {
                                        ModelState.AddModelError("Circumference", Resource.MsgWhatIsTheCircumferenceNotSet);
                                    }
                                    model.Diameter = null;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                                }
                                else
                                {
                                    if (model.Diameter == null)
                                    {
                                        ModelState.AddModelError("Diameter", Resource.MsgWhatIsTheDiameterNotSet);
                                    }
                                    model.Circumference = null;
                                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                                }
                            }
                            if (model.Depth == null)
                            {
                                ModelState.AddModelError("Depth", Resource.MsgWhatIsTheDepthNotSet);
                            }
                            if (model.IsCovered == null)
                            {
                                ModelState.AddModelError("IsCovered", string.Format(Resource.MsgIsCoveredNotSet, model.StoreName));
                            }
                        }
                    }

                }

                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage && model.CapacityWeight == null)
                {
                    ModelState.AddModelError("CapacityWeight", string.Format(Resource.MsgWhatIsTheWeightCapacityOfNotSet, model.StoreName));
                }

                if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag && model.StorageBagCapacity == null)
                {
                    ModelState.AddModelError("StorageBagCapacity", string.Format(Resource.MsgWhatIsTheTotalCapacityOfNotSet, model.StoreName));
                }
                if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
                {
                    if (model.IsSlopeEdge == null)
                    {
                        ModelState.AddModelError("IsSlopeEdge", string.Format(Resource.MsgDoesHaveSlopedEdgesNotSet, model.StoreName));
                    }
                    if (model.BankSlopeAngleID == null && model.IsSlopeEdge == true)
                    {
                        ModelState.AddModelError("BankSlopeAngleId", Resource.MsgWhatIsTheEstimatedAngleOfTheBankNotSet);
                    }
                }
                //validation end

                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);

                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                {
                    model.SolidManureTypeID = model.StorageTypeID;
                    model.StorageTypeID = null;
                }
                else
                {
                    model.SolidManureTypeID = null;
                }

                var storeCapacityData = new StoreCapacity()
                {
                    ID = !string.IsNullOrWhiteSpace(model.EncryptedStoreCapacityId) ? Convert.ToInt32(_storageCapacityProtector.Unprotect(model.EncryptedStoreCapacityId)) : null,
                    FarmID = model.FarmID,
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
                if (string.IsNullOrWhiteSpace(model.EncryptedStoreCapacityId))
                {
                    (StoreCapacity StoreCapacityData, error) = await _storageCapacityLogic.AddStoreCapacityAsync(storeCapacityData);
                }
                else
                {
                    (StoreCapacity StoreCapacityData, error) = await _storageCapacityLogic.UpdateStoreCapacityAsync(storeCapacityData);
                }
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["ErrorOnCheckAnswer"] = error.Message;
                    return RedirectToAction("CheckAnswer");
                }
                else
                {
                    HttpContext?.Session.Remove(_storageCapacityDataSessionKey);
                    bool success = true;
                    string successMsg = string.IsNullOrWhiteSpace(model.EncryptedStoreCapacityId) ? Resource.lblYouHaveAddedManureStorage : Resource.lblYouHaveUpdatedManureStorage;

                    var tabId = "";
                    if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                    {
                        tabId = "slurryStorageList";
                    }
                    else if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage)
                    {
                        tabId = "dirtyWaterList";
                    }
                    else if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                    {
                        tabId = "solidManureStorageList";
                    }

                    return RedirectToAction(
                           actionName: "ManageStorageCapacity",
                           controllerName: _storageCapacityActionName,
                           routeValues: new
                           {
                               q = model.EncryptedFarmID,
                               r = _reportDataProtector.Protect(successMsg),
                               s = _reportDataProtector.Protect(success.ToString()),
                               isPlan = string.IsNullOrWhiteSpace(model.IsComingFromPlan) ? null : model.IsComingFromPlan,
                               t = model.IsComingFromManageToHubPage
                           },
                           fragment: tabId
                       );

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
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
            {
                model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
            if (string.IsNullOrWhiteSpace(model.EncryptedStoreCapacityId))
            {
                if (model.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                {
                    return RedirectToAction("WeightCapacity");
                }
                else
                {
                    if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.StorageBag)
                    {
                        return RedirectToAction("StorageBagCapacity");
                    }
                    else if (model.StorageTypeID == (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon)
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
            else
            {
                return RedirectToAction("ManageStorageCapacity", new
                {
                    q = model.EncryptedFarmID
                });
            }

        }

        public static (decimal CapacityVolume, decimal? SurfaceArea) CalculateCapacityAndArea(StorageCapacityViewModel model)
        {
            int typeId = model.StorageTypeID ?? 0;
            decimal l = model.Length ?? 0m;
            decimal w = model.Width ?? 0m;
            decimal d = model.Depth ?? 0m;
            decimal diameter = model.Diameter ?? 0m;
            decimal circumference = model.Circumference ?? 0m;
            decimal slope = model.Slope ?? 0m;
            decimal freeboardDefault = model.FreeBoardHeight ?? 0m;

            //decimal freeboardToUse = covered ? 0m : freeboardDefault;// not using 
            decimal effDepth = d - freeboardDefault;
            if (effDepth < 0m) effDepth = 0m;

            decimal capacity = 0m;
            decimal? surfaceArea = 0m;

            switch (typeId)
            {
                case (int)NMP.Commons.Enums.StorageTypes.SquareOrRectangularTank:
                    capacity = l * w * effDepth;
                    surfaceArea = l * w;
                    break;
                case (int)NMP.Commons.Enums.StorageTypes.CircularTank:
                    if (model.IsCircumference == true)
                    {
                        diameter = circumference / (decimal)Math.PI;
                    }
                    decimal r = diameter / 2m;
                    capacity = (decimal)Math.PI * r * r * effDepth;
                    surfaceArea = (decimal)Math.PI * r * r;
                    break;
                case (int)NMP.Commons.Enums.StorageTypes.EarthBankedLagoon:
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
                case (int)NMP.Commons.Enums.StorageTypes.StorageBag:
                    capacity = model.StorageBagCapacity ?? 0m;
                    surfaceArea = null;
                    break;
            }

            return (capacity, surfaceArea);
        }

        [HttpGet]
        public async Task<IActionResult> StorageCapacityReport(string q, string? x, string v)
        {
            StorageCapacityViewModel model = new StorageCapacityViewModel();

            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return View(model);

                PopulateNavigationFlagsReport(model, x, v);

                model.FarmID = Convert.ToInt32(_farmDataProtector.Unprotect(q));

                var (storeCapacities, error) =
                    await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID.Value);

                if (!IsValidResult(error, storeCapacities))
                    return View(model);

                var (farm, farmError) =
                    await _farmLogic.FetchFarmByIdAsync(model.FarmID.Value);

                if (!IsValidResult(farmError, farm))
                    return View(model);

                model.Farm = farm;
                model.EncryptedFarmID = q;

                PopulateStoreCapacitiesByMaterialState(storeCapacities);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorOnManageStorageCapacity"] = ex.Message;
                _logger.LogTrace("StorageCapacity Controller : StorageCapacityReport() get action called");

                return RedirectToAction("ManageStorageCapacity", new
                {
                    q = q,
                    t = x
                });
            }
            finally
            {
                _logger.LogTrace("StorageCapacity Controller : StorageCapacityReport() get action called");
            }
        }


        [HttpGet]
        public IActionResult Cancel()
        {
            _logger.LogTrace("StorageCapacity Controller : Cancel() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in Cancel() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckAnswer"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> StorageCapacityManagement(string q, string? r)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                try
                {
                    if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                    {
                        HttpContext?.Session.Remove(_storageCapacityDataSessionKey);
                    }
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        TempData["succesMsgContent1"] = _storageCapacityProtector.Unprotect(r);
                    }
                    ViewBag.EncryptedFarmId = q;
                    int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
                    List<int> fixedYearList = GetReportYearsList();
                    (FarmResponse farm, Error error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        ViewBag.FarmName = farm.Name;
                    }
                    (List<StoreCapacityResponse> storeCapacities, error) = await _storageCapacityLogic.FetchStoreCapacityByFarmId(decryptedFarmId);

                    if (string.IsNullOrWhiteSpace(error.Message) && storeCapacities.Count > 0)
                    {

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
                                ? (latestEntry.ModifiedOn ?? latestEntry.CreatedOn).ToString("dd MMMM yyyy")
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
                                        ? (latestEntry.ModifiedOn ?? latestEntry.CreatedOn).ToString("dd MMMM yyyy")
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
                    else
                    {
                        var finalYearList = fixedYearList.Select(year =>
                        {
                            return new
                            {
                                Year = year,
                                EncryptedYear = _farmDataProtector.Protect(year.ToString()),
                                Label = Resource.lblAdd,
                                LastModifyDate = Resource.lblHyphen
                            };
                        }).ToList();

                        ViewBag.FinalYearList = finalYearList;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorOnStorageCapacityManagement"] = ex.Message;
                    _logger.LogTrace("StorageCapacity Controller : StorageCapacityManagement() get action called");
                    return RedirectToAction(_farmSummaryActionName, "Farm", new
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : Cancel() post action called");
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
                return RedirectToAction("CheckAnswer");
            }
            else
            {

                if (model.IsStoreCapacityExist == true)
                {

                    return RedirectToAction("ManageStorageCapacity", _storageCapacityActionName, new
                    {
                        q = model.EncryptedFarmID
                    });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(model.IsComingFromPlan))
                    {

                        return RedirectToAction("HarvestYearOverview", "Crop", new
                        {
                            id = model.EncryptedFarmID,
                            year = model.EncryptedHarvestYear
                        });
                    }
                    else
                    {
                        return RedirectToAction(_farmSummaryActionName, "Farm", new
                        {
                            id = model.EncryptedFarmID,
                        });
                    }
                }

            }
        }

        [HttpGet]
        public async Task<IActionResult> CopyExistingManureStorage(string q, string? isPlan, string? x, string? v)
        {
            _logger.LogTrace("StorageCapacity Controller : CopyExistingManureStorage() action called");

            StorageCapacityViewModel model = GetSessionModel();

            try
            {
                await PopulateFarmForCopyAsync(model, q, isPlan, v);
                SaveSessionModel(model);

                return View(model);
            }
            catch (Exception ex)
            {
                return HandleCopyExistingException(ex, model, q);
            }
        }

        [HttpPost]
        public IActionResult CopyExistingManureStorage(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : CopyExistingManureStorage() action called");

            try
            {
                if (model.IsCopyExistingManureStorage == null)
                {
                    ModelState.AddModelError("IsCopyExistingManureStorage", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                if (!model.IsCopyExistingManureStorage.Value)
                {
                    return RedirectToAction("MaterialStates");
                }
                else
                {
                    return RedirectToAction("CopyExistingManureStorageYearList");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in CopyExistingManureStorage() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCopyExistingManureStorage"] = ex.Message;
                return View(model);
            }
            //return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CopyExistingManureStorageYearList()
        {
            _logger.LogTrace("StorageCapacity Controller : CopyExistingManureStorageYearList() action called");

            if (_httpContextAccessor.HttpContext == null ||
                !_httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
            {
                return RedirectToAction("FarmList", "Farm");
            }

            var model = _httpContextAccessor.HttpContext.Session
                .GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);

            if (model == null || !model.FarmID.HasValue)
            {
                return RedirectToAction("FarmList", "Farm");
            }

            try
            {
                var (storageCapacityList, error) =
                    await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID.Value);

                if (string.IsNullOrWhiteSpace(error?.Message) && storageCapacityList.Any())
                {
                    ViewBag.YearList = storageCapacityList
                        .Select(x => x.Year)
                        .Distinct()
                        .OrderByDescending(x => x.Value)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "StorageCapacity Controller : Exception in CopyExistingManureStorageYearList");

                TempData["ErrorOnCopyExistingManureStorage"] = ex.Message;
                return RedirectToAction("CopyExistingManureStorage");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CopyExistingManureStorageYearList(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : CopyExistingManureStorageYearList() action called");

            try
            {
                if (model.YearToCopyFrom == null)
                {
                    ModelState.AddModelError("YearToCopyFrom", Resource.MsgSelectAnOptionBeforeContinuing);
                }
                (List<StoreCapacityResponse> storageCapacityList, Error error) = await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && storageCapacityList.Count > 0)
                {
                    ViewBag.YearList = storageCapacityList.Select(x => x.Year).Distinct().OrderByDescending(x => x.Value).ToList();
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
                var data = new
                {
                    FarmID = model.FarmID.Value,
                    CopyYear = model.YearToCopyFrom.Value
                };

                string jsonData = JsonConvert.SerializeObject(data);
                storageCapacityList = storageCapacityList.Where(x => x.Year == model.YearToCopyFrom).ToList();
                (List<StoreCapacityResponse> storeCapacities, error) = await _storageCapacityLogic.CopyExistingStorageCapacity(jsonData);
                if (string.IsNullOrWhiteSpace(error.Message) && storeCapacities.Count > 0)
                {
                    string successMsgContent = Resource.lblYouHaveAddedManureStorage;
                    var tabId = "slurryStorageList";
                    if (storageCapacityList != null && storageCapacityList.Select(x => x.MaterialStateID).Distinct().Count() == 1)
                    {
                        if (storageCapacityList?.FirstOrDefault()?.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage)
                        {
                            tabId = "dirtyWaterList";
                        }
                        else if (storageCapacityList?.FirstOrDefault()?.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                        {
                            tabId = "solidManureStorageList";
                        }
                    }

                    return RedirectToAction("ManageStorageCapacity", _storageCapacityActionName, routeValues: new
                    {
                        q = model.EncryptedFarmID,
                        r = _reportDataProtector.Protect(successMsgContent.ToString()),
                        s = _reportDataProtector.Protect(Resource.lblTrue),
                        isPlan = string.IsNullOrWhiteSpace(model.IsComingFromPlan) ? null : model.IsComingFromPlan,
                        t = model.IsComingFromManageToHubPage
                    }, fragment: tabId);
                }
                else
                {
                    TempData["ErrorOnCopyExistingManureStorageYearList"] = error.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in CopyExistingManureStorageYearList() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCopyExistingManureStorageYearList"] = ex.Message;
                return View(model);
            }
            //return View(model);
        }


        [HttpGet]
        public IActionResult RemoveStorageCapacity()
        {
            _logger.LogTrace("StorageCapacity Controller : RemoveStorageCapacity() action called");
            StorageCapacityViewModel model = new StorageCapacityViewModel();
            try
            {
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
                {
                    model = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
                }
                else
                {
                    return RedirectToAction("FarmList", "Farm");
                }

            }
            catch (Exception ex)
            {
                _logger.LogTrace($"StorageCapacity Controller : Exception in RemoveStorageCapacity() action : {ex.Message}, {ex.StackTrace}");
                TempData["ErrorOnCheckAnswer"] = ex.Message;
                return RedirectToAction("CheckAnswer");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStorageCapacity(StorageCapacityViewModel model)
        {
            _logger.LogTrace("StorageCapacity Controller : RemoveStorageCapacity() post action called");
            if (model.IsDelete == null)
            {
                ModelState.AddModelError("IsDelete", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View("RemoveStorageCapacity", model);
            }
            try
            {
                if (model.IsDelete.HasValue && (!model.IsDelete.Value))
                {
                    return RedirectToAction("CheckAnswer");
                }
                else
                {

                    (string message, Error error) = await _storageCapacityLogic.RemoveStorageCapacity(model.ID.Value);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        (List<StoreCapacityResponse> storeCapacityList, error) = await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            return RedirectToAction("ManageStorageCapacity", _storageCapacityActionName, new
                            {
                                q = model.EncryptedFarmID,
                                r = _reportDataProtector.Protect(Resource.lblRemove),
                                isPlan = string.IsNullOrWhiteSpace(model.IsComingFromPlan) ? null : model.IsComingFromPlan,
                                t = model.IsComingFromManageToHubPage,
                                u = storeCapacityList.Count == 0 ? _reportDataProtector.Protect(Resource.lblTrue) : null
                            });
                        }
                        else
                        {
                            TempData["ErrorOnRemove"] = error.Message;
                            return View("RemoveStorageCapacity", model);
                        }
                    }
                    else
                    {
                        TempData["ErrorOnRemove"] = error.Message;
                        return View("RemoveStorageCapacity", model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorOnRemove"] = ex.Message;
                return View("RemoveStorageCapacity", model);

            }
        }

        private void ClearStorageSessions()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(_storageCapacityDataSessionKey);
            session?.Remove("StorageCapacityDataBeforeUpdate");
        }
        private void PopulateFarmDetails(StorageCapacityViewModel model, FarmResponse farm, int farmId, string encryptedFarmId, string? isPlan, string? t)
        {
            model.FarmName = farm.Name;
            model.FarmID = farmId;
            model.EncryptedFarmID = encryptedFarmId;

            if (!string.IsNullOrWhiteSpace(isPlan))
            {
                model.IsComingFromPlan = isPlan;
                ViewBag.IsPlan = isPlan;
            }

            if (!string.IsNullOrWhiteSpace(t))
                model.IsComingFromManageToHubPage = t;
        }
        private void HandleSuccessMessages(string? r, string? s, StorageCapacityViewModel model, List<StoreCapacityResponse> storeCapacityList)
        {
            if (string.IsNullOrWhiteSpace(r)) return;

            string successMsg = _reportDataProtector.Unprotect(r);

            if (successMsg == Resource.lblRemove)
            {
                TempData["succesMsgContent1"] =
                    string.Format(Resource.lblYouHaveRemovedJourneyName,
                    Resource.lblManureStorage.ToLower());

                TempData["succesMsgContent2"] = Resource.lblAddMoreManureStorage;

                if (storeCapacityList.Any())
                    TempData["succesMsgContent3"] =
                        Resource.lblCreateAnExistingManureStorageCapacityReport;
            }
            else
            {
                TempData["succesMsgContent1"] = successMsg;
            }

            if (!string.IsNullOrWhiteSpace(s))
            {
                ViewBag.isComingFromSuccessMsg =
                    _reportDataProtector.Protect(Resource.lblTrue);

                TempData["succesMsgContent1Detail"] =
                    string.Format(
                        Resource.MsgToCreateAnExistingManureStorageCapacityReportEnterAllThe,
                        model.FarmName);

                TempData["succesMsgContent2"] = Resource.lblAddMoreManureStorage;
                TempData["succesMsgContent3"] =
                    Resource.lblCreateAnExistingManureStorageCapacityReport;
            }
        }
        private async Task PopulateStorageLists(List<StoreCapacityResponse> storeCapacityList)
        {
            var (materialStates, _) = await _storageCapacityLogic.FetchMaterialStates();
            if (!materialStates.Any()) return;

            PopulateViewBagList(storeCapacityList, materialStates,
                NMP.Commons.Enums.MaterialState.DirtyWaterStorage,
                "DirtyWaterList");

            PopulateViewBagList(storeCapacityList, materialStates,
                NMP.Commons.Enums.MaterialState.SlurryStorage,
                "SlurryStorageList");

            PopulateViewBagList(storeCapacityList, materialStates,
                NMP.Commons.Enums.MaterialState.SolidManureStorage,
                "SolidManureStorageList");
        }
        private void PopulateViewBagList(List<StoreCapacityResponse> list, List<CommonResponse> materialStates, NMP.Commons.Enums.MaterialState state, string viewBagName)
        {
            if (!list.Any(x => x.MaterialStateID == (int)state))
                return;

            var materialStateName = materialStates
                .FirstOrDefault(m => m.Id == (int)state)?.Name;

            ViewData[viewBagName] = list
                .Where(x => x.MaterialStateID == (int)state)
                .Select(x => new
                {
                    MaterialStateName = materialStateName,
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
                    x.SolidManureTypeName,
                    EncryptedStoreCapacityId =
                        _storageCapacityProtector.Protect(
                            Convert.ToString(x.ID) ?? string.Empty)
                })
                .ToList();
        }

        private void PopulateTotals(List<StoreCapacityResponse> list)
        {
            ViewBag.TotalLiquidCapacity =
                list.Where(x =>
                    x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage ||
                    x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage)
                .Sum(x => x.CapacityVolume);

            ViewBag.TotalSolidCapacity =
                list.Where(x =>
                    x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                .Sum(x => x.CapacityVolume);

            ViewBag.TotalSolidWeightCapacity =
                list.Where(x =>
                    x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage)
                .Sum(x => x.CapacityWeight);

            ViewBag.TotalSurfaceCapacity =
                list.Where(x => x.IsCovered == false)
                .Sum(x => x.SurfaceArea);
        }

        private void PopulateEncryptedStateIds()
        {
            ViewBag.EncryptedSolidStateId =
                _storageCapacityProtector.Protect(((int)NMP.Commons.Enums.MaterialState.SolidManureStorage).ToString());

            ViewBag.EncryptedSlurryStateId =
                _storageCapacityProtector.Protect(((int)NMP.Commons.Enums.MaterialState.SlurryStorage).ToString());

            ViewBag.EncryptedDirtyWaterStateId =
                _storageCapacityProtector.Protect(((int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage).ToString());
        }

        private IActionResult RedirectToFarmSummary(string q, string error)
        {
            TempData["Error"] = error;
            return RedirectToAction(_farmSummaryActionName, "Farm", new { q });
        }
        private StorageCapacityViewModel GetOrCreateSessionModel()
        {
            if (_httpContextAccessor.HttpContext?.Session.Keys.Contains(_storageCapacityDataSessionKey) == true)
            {
                return _httpContextAccessor.HttpContext!
                    .Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey)
                    ?? new StorageCapacityViewModel();
            }

            var model = new StorageCapacityViewModel();
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
            return model;
        }

        private void SaveSessionModel(StorageCapacityViewModel model)
        {
            _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_storageCapacityDataSessionKey, model);
        }
        private async Task LoadMaterialStatesAsync()
        {
            var (materialStates, error) = await _storageCapacityLogic.FetchMaterialStates();
            if (error == null)
            {
                materialStates.RemoveAll(x =>
                x.Id == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage);

                ViewBag.MaterialStateList = materialStates;
            }
        }
        private async Task PopulateFarmDetailsAsync(StorageCapacityViewModel model, string? encryptedFarmId, string? isPlan, string? removedRecently)
        {
            if (string.IsNullOrWhiteSpace(encryptedFarmId))
                return;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedFarmId));
            var (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);

            if (!string.IsNullOrWhiteSpace(error?.Message) || farm == null)
                return;

            model.FarmName = farm.Name;
            model.FarmID = farmId;
            model.EncryptedFarmID = encryptedFarmId;
            model.IsComingFromPlan = isPlan;
            model.IsRemovedRecently = removedRecently;
        }
        private static void PopulateNavigationFlags(StorageCapacityViewModel model, string? manageToHub, string? materialToHub)
        {
            if (!string.IsNullOrWhiteSpace(manageToHub))
                model.IsComingFromManageToHubPage = manageToHub;

            if (!string.IsNullOrWhiteSpace(materialToHub))
            {
                model.IsComingFromMaterialToHubPage = materialToHub;
                model.IsComingFromManageToHubPage = materialToHub;
            }
        }
        private async Task CheckStoreCapacityAsync(StorageCapacityViewModel model)
        {
            int farmId = model.FarmID ?? 0;

            var (storeCapacityList, error) =
                await _storageCapacityLogic.FetchStoreCapacityByFarmId(farmId);

            if (string.IsNullOrWhiteSpace(error?.Message) && storeCapacityList.Any())
            {
                model.IsStoreCapacityExist = true;
            }
        }
        private async Task<IActionResult> HandleMaterialStatesExceptionAsync(StorageCapacityViewModel model, Exception ex, string? encryptedFarmId)
        {
            if (model == null)
                return RedirectToAction(_farmSummaryActionName, "Farm", new { q = encryptedFarmId });

            if (model.IsStoreCapacityExist)
            {
                var (list, _) =
                    await _storageCapacityLogic.FetchStoreCapacityByFarmId(model.FarmID ?? 0);

                if (!list.Any())
                {
                    TempData["ErrorOnCopyExistingManureStorage"] = ex.Message;
                    return RedirectToAction("CopyExistingManureStorage");
                }

                TempData["ErrorOnOrganicMaterialStorageNotAvailable"] = ex.Message;
                return RedirectToAction("OrganicMaterialStorageNotAvailable");
            }

            if (!string.IsNullOrWhiteSpace(model.IsComingFromMaterialToHubPage))
            {
                TempData["ErrorOnStorageCapacityManagement"] = ex.Message;
                return RedirectToAction("StorageCapacityManagement", new { q = model.EncryptedFarmID });
            }

            TempData["ErrorOnYear"] = ex.Message;
            return RedirectToAction("Year", "Report");
        }
        private StorageCapacityViewModel GetSessionModel()
        {
            if (_httpContextAccessor.HttpContext?.Session.Keys.Contains(_storageCapacityDataSessionKey) == true)
            {
                return _httpContextAccessor.HttpContext!
                    .Session.GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey)
                    ?? new StorageCapacityViewModel();
            }

            return new StorageCapacityViewModel();
        }

        
        private async Task PopulateFarmAndStoreCapacityAsync(StorageCapacityViewModel model, string? encryptedFarmId, string? isPlan, string? manageToHub, string? removedRecently)
        {
            if (string.IsNullOrWhiteSpace(encryptedFarmId))
                return;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedFarmId));
            var (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);

            if (!string.IsNullOrWhiteSpace(error?.Message) || farm == null)
                return;

            model.FarmName = farm.Name;
            model.FarmID = farmId;
            model.EncryptedFarmID = encryptedFarmId;
            model.IsComingFromPlan = isPlan;
            model.IsComingFromManageToHubPage = manageToHub;
            model.IsRemovedRecently = removedRecently;

            var (storeCapacityList, storeError) =
                await _storageCapacityLogic.FetchStoreCapacityByFarmId(farmId);

            if (string.IsNullOrWhiteSpace(storeError?.Message) && storeCapacityList.Any())
            {
                ViewBag.StoreCapacityList = storeCapacityList;
                model.IsStoreCapacityExist = true;
            }
        }

        private async Task PopulateMaterialStateAsync(StorageCapacityViewModel model, string? encryptedMaterialStateId)
        {
            if (string.IsNullOrWhiteSpace(encryptedMaterialStateId))
                return;

            model.EncryptedMaterialStateID = encryptedMaterialStateId;
            model.MaterialStateID =
                Convert.ToInt32(_storageCapacityProtector.Unprotect(encryptedMaterialStateId));

            var (materialState, error) =
                await _storageCapacityLogic.FetchMaterialStateById(model.MaterialStateID.Value);

            if (error == null && materialState != null)
            {
                model.MaterialStateName = materialState.Name;
            }
        }
        private void PopulateNavigationFlagsReport(StorageCapacityViewModel model, string? manageToHub, string? isPlan)
        {
            if (!string.IsNullOrWhiteSpace(manageToHub))
            {
                model.IsComingFromManageToHubPage = manageToHub;
            }

            if (!string.IsNullOrWhiteSpace(isPlan))
            {
                model.IsComingFromPlan = isPlan;
                ViewBag.IsPlan = isPlan;
            }
        }
        private static bool IsValidResult<T>(Error error, T result) where T : class
        {
            return string.IsNullOrWhiteSpace(error?.Message) && result != null;
        }


        private static bool IsValidResult<T>(Error error, List<T> result)
        {
            return string.IsNullOrWhiteSpace(error?.Message) && result.Any();
        }
        private void PopulateStoreCapacitiesByMaterialState(List<StoreCapacityResponse> storeCapacities)
        {
            List<StoreCapacityResponse> solidStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SolidManureStorage).ToList();
            if (solidStoreCapacities.Count > 0)
            {
                ViewBag.SolidStoreCapacities = solidStoreCapacities;
            }
            List<StoreCapacityResponse> dirtyWaterStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage).ToList();
            if (dirtyWaterStoreCapacities.Count > 0)
            {
                ViewBag.DirtyWaterStoreCapacities = dirtyWaterStoreCapacities;
            }
            List<StoreCapacityResponse> slurryStoreCapacities = storeCapacities.Where(x => x.MaterialStateID == (int)NMP.Commons.Enums.MaterialState.SlurryStorage).ToList();
            if (slurryStoreCapacities.Count > 0)
            {
                ViewBag.SlurryStoreCapacities = slurryStoreCapacities;
            }
        }
        
        private async Task PopulateFarmForCopyAsync(StorageCapacityViewModel model, string encryptedFarmId, string? isPlan, string? materialToHub)
        {
            if (string.IsNullOrWhiteSpace(encryptedFarmId))
                return;

            int farmId = Convert.ToInt32(_farmDataProtector.Unprotect(encryptedFarmId));
            var (farm, error) = await _farmLogic.FetchFarmByIdAsync(farmId);

            if (!string.IsNullOrWhiteSpace(error?.Message) || farm == null)
                return;

            model.FarmName = farm.Name;
            model.FarmID = farmId;
            model.EncryptedFarmID = encryptedFarmId;
            model.IsComingFromPlan = isPlan;

            if (!string.IsNullOrWhiteSpace(materialToHub))
            {
                model.IsComingFromMaterialToHubPage = materialToHub;
                model.IsComingFromManageToHubPage = materialToHub;
            }
        }
        private IActionResult HandleCopyExistingException(Exception ex, StorageCapacityViewModel model, string q)
        {
            if (model != null)
            {
                if (!string.IsNullOrWhiteSpace(model.IsComingFromPlan))
                {
                    TempData["ErrorOnHarvestYearOverview"] = ex.Message;
                    return RedirectToAction("HarvestYearOverview", "Crop", new
                    {
                        id = model.EncryptedFarmID,
                        year = model.EncryptedHarvestYear
                    });
                }

                if (!string.IsNullOrWhiteSpace(model.IsComingFromMaterialToHubPage))
                {
                    TempData["ErrorOnStorageCapacityManagement"] = ex.Message;
                    return RedirectToAction(
                        "StorageCapacityManagement",
                        new { q = model.EncryptedFarmID });
                }

                TempData["ErrorOnYear"] = ex.Message;
                return RedirectToAction("Year", "Report");
            }

            TempData["Error"] = ex.Message;
            return RedirectToAction(_farmSummaryActionName, "Farm", new { q });
        }
        private static bool ShouldRedirectToCheckAnswer(StorageCapacityViewModel model, StorageCapacityViewModel storageModel)
        {
            return model.IsCheckAnswer &&
                   model.MaterialStateID == storageModel.MaterialStateID;
        }
        private void HandleMaterialTypeChange(StorageCapacityViewModel model, StorageCapacityViewModel storageModel)
        {
            if (!model.IsCheckAnswer)
                return;

            if (IsMaterialTypeChanged(
                model.MaterialStateID,
                storageModel.MaterialStateID))
            {
                model.StorageTypeID = null;
                model.StorageTypeName = null;
                model.IsMaterialTypeChange = true;
            }
        }
        private static bool IsMaterialTypeChanged(int? current, int? previous)
        {
            int dirtyWater = (int)NMP.Commons.Enums.MaterialState.DirtyWaterStorage;
            int slurry = (int)NMP.Commons.Enums.MaterialState.SlurryStorage;
            int solid = (int)NMP.Commons.Enums.MaterialState.SolidManureStorage;

            return
                (current == solid && (previous == dirtyWater || previous == slurry)) ||
                ((current == dirtyWater || current == slurry) && previous == solid);
        }
        private void ValidateMaterialState(StorageCapacityViewModel model)
        {
            if (model.MaterialStateID == null)
            {
                ModelState.AddModelError(
                    "MaterialStateId",
                    Resource.MsgSelectAnOptionBeforeContinuing);
            }
        }
        private StorageCapacityViewModel GetStorageCapacityFromSession()
        {
            if (_httpContextAccessor.HttpContext != null &&
                _httpContextAccessor.HttpContext.Session.Keys.Contains(_storageCapacityDataSessionKey))
            {
                return _httpContextAccessor.HttpContext
                    .Session
                    .GetObjectFromJson<StorageCapacityViewModel>(_storageCapacityDataSessionKey);
            }

            return new StorageCapacityViewModel();
        }
        
        private async Task PopulateMaterialStateNameAsync(StorageCapacityViewModel model)
        {
            var (materialState, error) =
                await _storageCapacityLogic.FetchMaterialStateById(model.MaterialStateID.Value);

            if (error == null)
            {
                model.MaterialStateName = materialState.Name;
            }
        }
        private void SaveStorageCapacityToSession(StorageCapacityViewModel model)
        {
            _httpContextAccessor.HttpContext
                ?.Session
                .SetObjectAsJson(_storageCapacityDataSessionKey, model);
        }

    }
}
