using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NMP.Commons.Enums;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System.Globalization;
using Enums = NMP.Commons.Enums;
using NMP.Application;
using Error = NMP.Commons.ServiceResponses.Error;
namespace NMP.Portal.Controllers;

[Authorize]
public class ReportController(ILogger<ReportController> logger, IDataProtectionProvider dataProtectionProvider, IFarmLogic farmLogic,
    IFieldLogic fieldLogic, ICropLogic cropLogic, IOrganicManureLogic organicManureLogic,
    IFertiliserManureLogic fertiliserManureLogic, IReportLogic reportLogic, IStorageCapacityLogic storageCapacityLogic, IWarningLogic warningLogic) : Controller
{
    private readonly ILogger<ReportController> _logger = logger;
    private readonly IDataProtector _reportDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.ReportController");
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");        
    private readonly IFarmLogic _farmLogic = farmLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
    private readonly IFertiliserManureLogic _fertiliserManureLogic = fertiliserManureLogic;
    private readonly IReportLogic _reportLogic = reportLogic;
    private readonly IStorageCapacityLogic _storageCapacityLogic = storageCapacityLogic;
    private readonly IWarningLogic _warningLogic = warningLogic;

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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            Error error = null;
            ViewBag.EncryptedYear = _farmDataProtector.Protect(model.Year.Value.ToString());
            if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
            {
                (error, List<Field> fields) = await _fieldLogic.FetchFieldByFarmId(model.FarmId.Value, Resource.lblTrue);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (fields.Count > 0)
                    {
                        int fieldCount = 0;
                        foreach (var field in fields)
                        {
                            List<Crop> cropList = await _cropLogic.FetchCropsByFieldId(field.ID.Value);

                            cropList = cropList.Where(x => x.Year == model.Year).ToList();
                            if (cropList.Count == 0)
                            {
                                fieldCount++;
                            }
                        }
                        if (fields.Count == fieldCount)
                        {
                            ViewBag.NoPlan = string.Format(Resource.lblYouHaveNotEnteredAnyCropInformation, model.Year);

                        }
                    }
                    else
                    {
                        ViewBag.NoField = string.Format(Resource.lblYouHaveNotEnteredAnyField, model.Year);

                    }

                }
            }
            if (ViewBag.NoPlan == null && ViewBag.NoField == null)
            {
                if (model.FieldAndPlanReportOption != null && model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                {
                    (List<HarvestYearPlanResponse> fieldList, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
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
                else if (model.NVZReportOption != null && model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.NmaxReport)
                {
                    (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                    {
                        (List<HarvestYearPlanResponse> cropTypeList, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                        if (string.IsNullOrWhiteSpace(error.Message) && cropTypeList != null && cropTypeList.Count > 0)
                        {
                            List<HarvestYearPlanResponse> filteredList = new List<HarvestYearPlanResponse>();

                            foreach (var cropType in cropTypeList)
                            {
                                Field field = await _fieldLogic.FetchFieldByFieldId(cropType.FieldID);
                                if (field != null && (!field.IsWithinNVZ.Value))
                                {
                                    filteredList.Add(cropType);
                                }
                            }
                            if (filteredList.Count > 0)
                            {
                                // Remove all matching cropTypes from cropTypeList
                                cropTypeList.RemoveAll(ct => filteredList.Contains(ct));
                            }
                            if (cropTypeList.Count > 0)
                            {
                                (List<CropTypeLinkingResponse> cropTypeLinking, error) = await _cropLogic.FetchCropTypeLinking();
                                if (error == null && cropTypeLinking != null && cropTypeLinking.Count > 0)
                                {
                                    if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.England)
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
                                        //grouping of same type crops into one crop for nmax reporting

                                        var cropGroups = GetNmaxReportCropGroups();
                                        List<CropTypeResponse> cropTypes = await _fieldLogic.FetchAllCropTypes();

                                        var cropTypeMap = cropTypes.ToDictionary(c => c.CropTypeId, c => c.CropType);

                                        if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.England)
                                        {
                                            // Group 1
                                            var group1List = cropGroups.ContainsKey(Resource.lblGroup1Vegetables)
                                                ? cropGroups[Resource.lblGroup1Vegetables]
                                                    .Where(id => cropTypeMap.ContainsKey(id))
                                                    .Select(id => cropTypeMap[id])
                                                    .OrderBy(name => name)
                                                    .ToList()
                                                : new List<string>();

                                            if (group1List.Count > 1)
                                            {
                                                for (int i = 1; i < group1List.Count; i++)
                                                {
                                                    group1List[i] = group1List[i].ToLower();
                                                }
                                            }

                                            // Group 2
                                            var group2List = cropGroups.ContainsKey(Resource.lblGroup2Vegetables)
                                                ? cropGroups[Resource.lblGroup2Vegetables]
                                                    .Where(id => cropTypeMap.ContainsKey(id))
                                                    .Select(id => cropTypeMap[id])
                                                    .OrderBy(name => name)
                                                    .ToList()
                                                : new List<string>();

                                            if (group2List.Count > 1)
                                            {
                                                for (int i = 1; i < group2List.Count; i++)
                                                {
                                                    group2List[i] = group2List[i].ToLower();
                                                }
                                            }

                                            // Group 3
                                            var group3List = cropGroups.ContainsKey(Resource.lblGroup3Vegetables)
                                                ? cropGroups[Resource.lblGroup3Vegetables]
                                                    .Where(id => cropTypeMap.ContainsKey(id))
                                                    .Select(id => cropTypeMap[id])
                                                    .OrderBy(name => name)
                                                    .ToList()
                                                : new List<string>();

                                            if (group3List.Count > 1)
                                            {
                                                for (int i = 1; i < group3List.Count; i++)
                                                {
                                                    group3List[i] = group3List[i].ToLower();
                                                }
                                            }
                                            ViewBag.Group1VegetablesHint = string.Join(", ", group1List);
                                            ViewBag.Group2VegetablesHint = string.Join(", ", group2List);
                                            ViewBag.Group3VegetablesHint = string.Join(", ", group3List);
                                        }
                                        if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.Wales)
                                        {
                                            cropGroups.Remove(Resource.lblGroup1Vegetables);
                                            cropGroups.Remove(Resource.lblGroup2Vegetables);
                                            cropGroups.Remove(Resource.lblGroup3Vegetables);
                                        }

                                        var list = new List<SelectListItem>();

                                        foreach (var group in cropGroups)
                                        {
                                            // Find which IDs from this group exist in cropTypeList
                                            var available = cropTypeList
                                                .Where(c => group.Value.Contains(c.CropTypeID))
                                                .Select(c => c.CropTypeID)
                                                .ToList();

                                            if (available.Count == 0)
                                            {
                                                continue;
                                            }
                                            // Pick the first matching ID according to the defined group order
                                            int chosenId = group.Value.First(id => available.Contains(id));

                                            list.Add(new SelectListItem
                                            {
                                                Value = chosenId.ToString(),
                                                Text = group.Key
                                            });
                                        }

                                        // Handle crops not in groups
                                        var groupedIds = cropGroups.Values.SelectMany(g => g).ToHashSet();

                                        var remainingCrops = cropTypeList
                                            .Where(c => !groupedIds.Contains(c.CropTypeID))
                                            .Select(c => new SelectListItem
                                            {
                                                Value = c.CropTypeID.ToString(),
                                                Text = c.CropTypeName
                                            });

                                        list.AddRange(remainingCrops);

                                        // Final sorted distinct list
                                        ViewBag.CropTypeList = list
                                            .DistinctBy(x => x.Value)   // avoid duplicates by ID
                                            .OrderBy(x => x.Text)
                                            .ToList();

                                        //var SelectListItem = cropTypeList.Select(f => new SelectListItem
                                        //{
                                        //    Value = f.CropTypeID.ToString(),
                                        //    Text = f.CropTypeName
                                        //}).ToList();
                                        //ViewBag.CropTypeList = SelectListItem.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                                    }
                                    else
                                    {

                                        if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                                        {
                                            if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FarmAndFieldDetailsForNVZRecord)
                                            {
                                                ViewBag.NMaxReportNotAvailable = true;
                                                return View(model);
                                            }
                                            else
                                            {
                                                ViewBag.Years = GetReportYearsList();
                                                TempData["ErrorOnYear"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                                return RedirectToAction("Year");
                                            }
                                        }
                                        else
                                        {
                                            if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
                                            {
                                                TempData["ErrorOnReportOptions"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                                return RedirectToAction("ReportOptions");
                                            }
                                            else
                                            {
                                                ViewBag.NMaxReportNotAvailable = true;
                                                return View(model);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {

                                if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                                {
                                    if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FarmAndFieldDetailsForNVZRecord)
                                    {
                                        ViewBag.NMaxReportNotAvailable = true;
                                        ViewBag.FarmOrFieldNotInNVZ = true;
                                        return View(model);
                                    }
                                    else
                                    {
                                        ViewBag.Years = GetReportYearsList();
                                        TempData["ErrorOnYear"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                        return View("Year", model);
                                    }
                                }
                                else
                                {
                                    if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
                                    {
                                        TempData["ErrorOnReportOptions"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                        return RedirectToAction("ReportOptions");
                                    }
                                    else
                                    {
                                        ViewBag.NMaxReportNotAvailable = true;
                                        ViewBag.FarmOrFieldNotInNVZ = true;
                                        return View(model);
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
                if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
                {
                    TempData["ErrorOnReportOptions"] = ex.Message;
                    return RedirectToAction("ReportOptions");
                }
                else
                {
                    TempData["ErrorOnNVZComplianceReports"] = ex.Message;
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
            if (model.FieldAndPlanReportOption != null && model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
            {
                (List<HarvestYearPlanResponse> fieldList, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
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
                    if (model.FieldList.Count > 0 && model.FieldList.Contains(Resource.lblSelectAll))
                    {
                        model.FieldList = selectListItem.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
                    }
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                }
                return RedirectToAction("CropAndFieldManagement");

            }
            else if (model.NVZReportOption != null && model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.NmaxReport)
            {
                //fetch crop type
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    (List<HarvestYearPlanResponse> cropTypeList, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message))
                    {
                        List<HarvestYearPlanResponse> filteredList = new List<HarvestYearPlanResponse>();

                        foreach (var cropType in cropTypeList)
                        {
                            Field field = await _fieldLogic.FetchFieldByFieldId(cropType.FieldID);
                            if (field != null && (!field.IsWithinNVZ.Value))
                            {
                                filteredList.Add(cropType);
                            }
                        }
                        if (filteredList.Count > 0)
                        {
                            // Remove all matching cropTypes from cropTypeList
                            cropTypeList.RemoveAll(ct => filteredList.Contains(ct));
                        }
                        if (cropTypeList.Count > 0)
                        {
                            (List<CropTypeLinkingResponse> cropTypeLinking, error) = await _cropLogic.FetchCropTypeLinking();
                            if (error == null && cropTypeLinking != null && cropTypeLinking.Count > 0)
                            {
                                if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.England)
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
                                var list = new List<SelectListItem>();

                                if (cropTypeList.Count > 0)
                                {
                                    //grouping of same type crops into one crop for nmax reporting
                                    var cropGroups = GetNmaxReportCropGroups();

                                    List<CropTypeResponse> cropTypes = await _fieldLogic.FetchAllCropTypes();
                                    var cropTypeMap = cropTypes.ToDictionary(c => c.CropTypeId, c => c.CropType);

                                    if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.England)
                                    {
                                        // Group 1
                                        var group1List = cropGroups.ContainsKey(Resource.lblGroup1Vegetables)
                                            ? cropGroups[Resource.lblGroup1Vegetables]
                                                .Where(id => cropTypeMap.ContainsKey(id))
                                                .Select(id => cropTypeMap[id])
                                                .OrderBy(name => name)
                                                .ToList()
                                            : new List<string>();

                                        if (group1List.Count > 1)
                                        {
                                            for (int i = 1; i < group1List.Count; i++)
                                            {
                                                group1List[i] = group1List[i].ToLower();
                                            }
                                        }

                                        // Group 2
                                        var group2List = cropGroups.ContainsKey(Resource.lblGroup2Vegetables)
                                            ? cropGroups[Resource.lblGroup2Vegetables]
                                                .Where(id => cropTypeMap.ContainsKey(id))
                                                .Select(id => cropTypeMap[id])
                                                .OrderBy(name => name)
                                                .ToList()
                                            : new List<string>();

                                        if (group2List.Count > 1)
                                        {
                                            for (int i = 1; i < group2List.Count; i++)
                                            {
                                                group2List[i] = group2List[i].ToLower();
                                            }
                                        }

                                        // Group 3
                                        var group3List = cropGroups.ContainsKey(Resource.lblGroup3Vegetables)
                                            ? cropGroups[Resource.lblGroup3Vegetables]
                                                .Where(id => cropTypeMap.ContainsKey(id))
                                                .Select(id => cropTypeMap[id])
                                                .OrderBy(name => name)
                                                .ToList()
                                            : new List<string>();

                                        if (group3List.Count > 1)
                                        {
                                            for (int i = 1; i < group3List.Count; i++)
                                            {
                                                group3List[i] = group3List[i].ToLower();
                                            }
                                        }
                                        ViewBag.Group1VegetablesHint = string.Join(", ", group1List);
                                        ViewBag.Group2VegetablesHint = string.Join(", ", group2List);
                                        ViewBag.Group3VegetablesHint = string.Join(", ", group3List);
                                    }
                                    if (farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.Wales)
                                    {
                                        cropGroups.Remove(Resource.lblGroup1Vegetables);
                                        cropGroups.Remove(Resource.lblGroup2Vegetables);
                                        cropGroups.Remove(Resource.lblGroup3Vegetables);
                                    }

                                    foreach (var group in cropGroups)
                                    {
                                        // Find which IDs from this group exist in cropTypeList
                                        var available = cropTypeList
                                            .Where(c => group.Value.Contains(c.CropTypeID))
                                            .Select(c => c.CropTypeID)
                                            .ToList();

                                        if (available.Count == 0)
                                        {
                                            continue;
                                        }
                                        // Pick the first matching ID according to the defined group order
                                        int chosenId = group.Value.First(id => available.Contains(id));

                                        list.Add(new SelectListItem
                                        {
                                            Value = chosenId.ToString(),
                                            Text = group.Key
                                        });

                                    }

                                    // Handle crops not in groups
                                    var groupedIds = cropGroups.Values.SelectMany(g => g).ToHashSet();

                                    var remainingCrops = cropTypeList
                                        .Where(c => !groupedIds.Contains(c.CropTypeID))
                                        .Select(c => new SelectListItem
                                        {
                                            Value = c.CropTypeID.ToString(),
                                            Text = c.CropTypeName
                                        });

                                    list.AddRange(remainingCrops);

                                    // Final sorted distinct list
                                    ViewBag.CropTypeList = list
                                        .DistinctBy(x => x.Value)   // avoid duplicates by ID
                                        .OrderBy(x => x.Text)
                                        .ToList();

                                }

                                if (model.CropTypeList == null || model.CropTypeList.Count == 0)
                                {
                                    ModelState.AddModelError("CropTypeList", string.Format(Resource.MsgSelectANameOfFieldBeforeContinuing, Resource.lblCropType.ToLower()));
                                }
                                if (!ModelState.IsValid)
                                {
                                    return View(model);
                                }
                                if (model.CropTypeList.Count > 0 && model.CropTypeList.Contains(Resource.lblSelectAll))
                                {
                                    model.CropTypeList = list.Where(item => item.Value != Resource.lblSelectAll).Select(item => item.Value).ToList();
                                }
                                HttpContext?.Session.SetObjectAsJson("ReportData", model);
                                ViewBag.CropTypeList = list.DistinctBy(x => x.Text).OrderBy(x => x.Text).ToList();
                            }
                            else
                            {
                                TempData["ErrorOnSelectField"] = error != null ? error.Message : null;
                                return View(model);
                            }
                        }
                        else
                        {

                            if ((model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value)))
                            {
                                if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FarmAndFieldDetailsForNVZRecord)
                                {
                                    ViewBag.NMaxReportNotAvailable = true;
                                    ViewBag.FarmOrFieldNotInNVZ = true;
                                    return View(model);
                                }
                                else
                                {
                                    ViewBag.Years = GetReportYearsList();
                                    TempData["ErrorOnYear"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                    return View("Year", model);
                                }
                            }
                            else
                            {
                                if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
                                {
                                    TempData["ErrorOnReportOptions"] = string.Format(Resource.lblNoCropTypesAvailable, model.Year);
                                    return RedirectToAction("ReportOptions");
                                }
                                else
                                {
                                    ViewBag.NMaxReportNotAvailable = true;
                                    ViewBag.FarmOrFieldNotInNVZ = true;
                                    return View(model);
                                }
                            }
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
        Error error = new Error();
        if (HttpContext.Session.Keys.Contains("ReportData"))
        {
            model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
        }
        else
        {
            return RedirectToAction("FarmList", "Farm");
        }
        string fieldIds = string.Join(",", model.FieldList);

        List<WarningHeaderResponse> warningHeaderResponses = await _warningLogic.FetchWarningHeaderByFieldIdAndYearAsync(fieldIds, model.Year.Value);
        ViewBag.WarningHeaders = warningHeaderResponses;

        (CropAndFieldReportResponse cropAndFieldReportResponse, error) = await _fieldLogic.FetchCropAndFieldReportById(fieldIds, model.Year.Value);
        if (string.IsNullOrWhiteSpace(error.Message))
        {
            model.CropAndFieldReport = cropAndFieldReportResponse;
        }
        else
        {
            TempData["ErrorOnSelectField"] = error.Message;
            return RedirectToAction("ExportFieldsOrCropType");
        }
        (List<NutrientResponseWrapper> nutrients, error) = await _fieldLogic.FetchNutrientsAsync();
        if (error == null && nutrients.Count > 0)
        {
            model.Nutrients = new List<NutrientResponseWrapper>();
            model.Nutrients = nutrients;
        }
        (ExcessRainfalls excessRainfalls, error) = await _farmLogic.FetchExcessRainfallsAsync(model.FarmId.Value, model.Year.Value);
        if (string.IsNullOrWhiteSpace(error.Message) && excessRainfalls != null)
        {
            (List<CommonResponse> excessWinterRainfallOption, error) = await _farmLogic.FetchExcessWinterRainfallOptionAsync();
            if (string.IsNullOrWhiteSpace(error.Message) && excessWinterRainfallOption != null && excessWinterRainfallOption.Count > 0)
            {
                string excessRainfallName = (excessWinterRainfallOption.FirstOrDefault(x => x.Value == excessRainfalls.WinterRainfall)).Name;
                string[] parts = excessRainfallName.Split(new string[] { " - " }, StringSplitOptions.None);
                model.CropAndFieldReport.ExcessWinterRainfall = $"{parts[0]} ({parts[1]})";
            }
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
                model.CropAndFieldReport.Farm.FullAddress += ", " + Enum.GetName(typeof(NMP.Commons.Enums.FarmCountry), model.CropAndFieldReport.Farm.CountryID);
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
                            (List<GrassGrowthClassResponse> grassGrowthClasses, error) = await _cropLogic.FetchGrassGrowthClass(fieldIdsForGrowthClass);
                            if (string.IsNullOrWhiteSpace(error.Message))
                            {

                                if (cropData.SwardTypeID == (int)NMP.Commons.Enums.SwardType.Grass)
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
                                    if (cropData.Establishment != (int)NMP.Commons.Enums.Season.Autumn &&
                                    cropData.Establishment != (int)NMP.Commons.Enums.Season.Spring)
                                    {
                                        cropData.EstablishmentName = Resource.lblExistingSwards;
                                    }
                                    else if (cropData.Establishment == (int)NMP.Commons.Enums.Season.Spring)
                                    {
                                        cropData.EstablishmentName = Resource.lblSpringSown;
                                    }
                                    //else if (cropData.Establishment == (int)NMP.Commons.Enums.Season.Spring)
                                    //{
                                    //    cropData.EstablishmentName = Resource.lblautumn;
                                    //}
                                }

                                //cropData.DefoliationSequenceName = cropData.DefoliationSequenceName;
                                if (cropData.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
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
                                if ((string.IsNullOrWhiteSpace(defolicationName)) && cropData.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass)
                                {
                                    (DefoliationSequenceResponse defResponse, Error grassError) = await _cropLogic.FetchDefoliationSequencesById(cropData.DefoliationSequenceID.Value);
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
                                    foreach (var organic in manData.OrganicManures)
                                    {
                                        (ManureType manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(organic.ManureTypeID);
                                        if (error == null)
                                        {
                                            organic.RateUnit = manureType.IsLiquid.Value ? string.Format("{0} {1}", Resource.lblCubicMeters, Resource.lblPerHectare) : string.Format("{0} {1}", Resource.lbltonnes, Resource.lblPerHectare);
                                        }
                                        else
                                        {
                                            TempData["ErrorOnSelectField"] = error.Message;
                                            return RedirectToAction("ExportFieldsOrCropType");
                                        }
                                    }
                                    defIndex++;
                                }
                            }
                        }
                    }
                    //manData.Recommendation.KIndex != null ? (manData.Recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (manData.Recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : manData.Recommendation.KIndex)) : null;
                    if (fieldData.SoilAnalysis != null && fieldData.SoilAnalysis.Count > 0)
                    {
                        foreach (var soilAnalysis in fieldData.SoilAnalysis)
                        {
                            soilAnalysis.PotassiumIndex = soilAnalysis.PotassiumIndex != null ? (soilAnalysis.PotassiumIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (soilAnalysis.PotassiumIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : soilAnalysis.PotassiumIndex)) : null;
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = new ReportViewModel();
                model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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
                    (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
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
            HttpContext.Session.SetObjectAsJson("ReportData", model);

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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.NMaxLimitReport = new List<NMaxReportResponse>();
            Error? error = null;
            (model.Farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (model.Farm != null && string.IsNullOrWhiteSpace(error.Message))
            {
                (List<HarvestYearPlanResponse> harvestYearPlanResponse, error) = await _cropLogic.FetchHarvestYearPlansByFarmId(model.Year.Value, model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && harvestYearPlanResponse.Count > 0)
                {
                    // Get your dictionary of groups
                    var cropGroups = GetNmaxReportCropGroups();
                    if (model.Farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.Wales)
                    {
                        cropGroups.Remove(Resource.lblGroup1Vegetables);
                        cropGroups.Remove(Resource.lblGroup2Vegetables);
                        cropGroups.Remove(Resource.lblGroup3Vegetables);
                    }
                    // Build reverse lookup: cropId -> groupIds[]
                    var idToGroup = cropGroups
                        .SelectMany(g => g.Value, (g, id) => new { id, groupIds = g.Value })
                        .ToDictionary(x => x.id, x => x.groupIds);

                    var cropGroupIds = model.CropTypeList?
                    .Select(cropType => int.Parse(cropType)).ToList();
                    //.Where(cropId => idToGroup.ContainsKey(cropId) || !idToGroup.ContainsKey(cropId))

                    List<CropTypeResponse> cropTypes = await _fieldLogic.FetchAllCropTypes();
                    if (cropGroupIds != null)
                    {
                        foreach (int cropGroup in cropGroupIds)
                        {
                            List<int> selectedCropGroupList = idToGroup
                           .Where(x => x.Key == cropGroup).SelectMany(x => x.Value).ToList();

                            if (selectedCropGroupList.Count == 0)
                            {
                                selectedCropGroupList.Add(cropGroup);
                            }
                            string cropTypeName = string.Empty;
                            int nMaxLimit = 0;
                            List<NitrogenApplicationsForNMaxReportResponse> nitrogenApplicationsForNMaxReportResponse = new List<NitrogenApplicationsForNMaxReportResponse>();
                            List<NMaxLimitReportResponse> nMaxLimitReportResponse = new List<NMaxLimitReportResponse>();

                            string groupName = cropGroups
                            .FirstOrDefault(group => group.Value.Contains(cropGroup)).Key;
                            if (string.IsNullOrWhiteSpace(groupName))
                            {
                                groupName = cropTypes.Where(x => x.CropTypeId == cropGroup).Select(x => x.CropType).FirstOrDefault();
                            }

                            (nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, nMaxLimit, error) = await GetNMaxReportData(harvestYearPlanResponse, Convert.ToInt32(cropGroup), model,
                                           nitrogenApplicationsForNMaxReportResponse, nMaxLimitReportResponse, selectedCropGroupList);
                            cropTypeName = cropTypes.Where(x => x.CropTypeId == cropGroup).Select(x => x.CropType).FirstOrDefault();
                            if (error != null && (!string.IsNullOrWhiteSpace(error.Message)))
                            {
                                TempData["ErrorOnSelectField"] = error.Message;
                                return RedirectToAction("ExportFieldsOrCropType");
                            }
                            if (nMaxLimitReportResponse != null && nMaxLimitReportResponse.Count > 0)
                            {
                                var fullReport = new NMaxReportResponse
                                {
                                    CropTypeName = cropTypeName ?? string.Empty,
                                    NmaxLimit = nMaxLimit,
                                    GroupName = groupName ?? string.Empty,
                                    IsComply = (nMaxLimitReportResponse == null && nitrogenApplicationsForNMaxReportResponse == null) ? false : (nMaxLimitReportResponse.Sum(x => x.MaximumLimitForNApplied) >= nitrogenApplicationsForNMaxReportResponse.Sum(x => x.NTotal) ? true : false),
                                    NMaxLimitReportResponse = nMaxLimitReportResponse ?? null,
                                    NitrogenApplicationsForNMaxReportResponse = (nitrogenApplicationsForNMaxReportResponse != null && nitrogenApplicationsForNMaxReportResponse.Count > 0) ? nitrogenApplicationsForNMaxReportResponse : null
                                };
                                model.NMaxLimitReport.Add(fullReport);
                            }

                        }
                    }
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
    private async Task<(List<NitrogenApplicationsForNMaxReportResponse>, List<NMaxLimitReportResponse>, int nMaxLimit, Error?)> GetNMaxReportData(List<HarvestYearPlanResponse> harvestYearPlanResponse, int cropTypeId, ReportViewModel model,
        List<NitrogenApplicationsForNMaxReportResponse> nitrogenApplicationsForNMaxReportResponse, List<NMaxLimitReportResponse> nMaxLimitReportResponse, List<int> selectedCropGroupList)
    {
        List<HarvestYearPlanResponse> cropDetails = harvestYearPlanResponse
        .Where(x => selectedCropGroupList.Contains(x.CropTypeID))
        .ToList();
        Error? error = null;
        int nMaxLimit = 0;
        string cropTypeName = string.Empty;
        string vegetableGroup = string.Empty;
        NMaxReportResponse nMaxLimitReport = new NMaxReportResponse();
        foreach (var cropData in cropDetails)
        {
            (Crop crop, error) = await _cropLogic.FetchCropById(cropData.CropID);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                (CropTypeLinkingResponse cropTypeLinkingResponse, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
                if (error == null && cropTypeLinkingResponse != null)
                {
                    nMaxLimit = model.Farm.CountryID == (int)NMP.Commons.Enums.FarmCountry.England ?
                        ((cropTypeLinkingResponse.NMaxLimitEngland != null) ? cropTypeLinkingResponse.NMaxLimitEngland.Value : 0) :
                        ((cropTypeLinkingResponse.NMaxLimitWales != null) ? cropTypeLinkingResponse.NMaxLimitWales.Value : 0);
                    if (nMaxLimit != null)
                    {
                        cropTypeName = cropData.CropTypeName;
                        Field field = await _fieldLogic.FetchFieldByFieldId(crop.FieldID.Value);
                        if (field != null && field.IsWithinNVZ.Value)
                        {
                            (List<int> currentYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(field.ID.Value), model.Year.Value, false);
                            (List<int> previousYearManureTypeIds, error) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(field.ID.Value), model.Year.Value - 1, false);
                            if (error == null)
                            {
                                bool manureTypeCondition = false;
                                if (currentYearManureTypeIds.Count > 0)
                                {
                                    foreach (var Ids in currentYearManureTypeIds)
                                    {
                                        if (Ids == (int)NMP.Commons.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                                            Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                                        {
                                            manureTypeCondition = true;
                                        }
                                    }
                                }
                                if (previousYearManureTypeIds.Count > 0)
                                {
                                    foreach (var Ids in previousYearManureTypeIds)
                                    {
                                        if (Ids == (int)NMP.Commons.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                                            Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                                        {
                                            manureTypeCondition = true;
                                        }
                                    }
                                }
                                cropTypeName = (await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value));

                                int soilTypeAdjustment = 0;
                                int millingWheat = 0;
                                decimal yieldAdjustment = 0;
                                int paperCrumbleOrStrawMulch = 0;
                                decimal grassCut = 0;

                                if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SugarBeet
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup1 || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup2
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup3 || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup4
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.ForageMaize || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterBeans
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SpringBeans || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Peas
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Asparagus || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Carrots
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Radish || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Swedes
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.CelerySelfBlanching || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Courgettes
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.DwarfBeans || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Lettuce
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.BulbOnions || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SaladOnions
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Parsnips || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.RunnerBeans
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Sweetcorn || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Turnips
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Beetroot || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.BrusselSprouts
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Cabbage || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Calabrese
                                || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Cauliflower || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Leeks)
                                {
                                    if (manureTypeCondition)
                                    {
                                        paperCrumbleOrStrawMulch = 80;
                                    }

                                }
                                else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.Grass)
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
                                else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterWheat ||
                                    crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SpringWheat ||
                                    crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterBarley ||
                                    crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SpringBarley ||
                                    crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape ||
                                    crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropSpringBarley || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropSpringWheat || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropWinterBarley || crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat)
                                {
                                    if (manureTypeCondition)
                                    {
                                        paperCrumbleOrStrawMulch = 80;
                                    }
                                    if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterWheat)
                                    {
                                        if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                                        {
                                            soilTypeAdjustment = 20;
                                        }
                                        if (crop.CropInfo1 != null && crop.CropInfo1 == (int)NMP.Commons.Enums.CropInfoOne.Milling)
                                        {
                                            millingWheat = 40;
                                        }
                                        if (crop.Yield != null && crop.Yield > 8.0m)
                                        {
                                            yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 8.0m) / 0.1m) * 2);
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat)
                                    {
                                        if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                                        {
                                            soilTypeAdjustment = 20;
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SpringWheat)
                                    {
                                        if (crop.CropInfo1 != null && crop.CropInfo1 == (int)NMP.Commons.Enums.CropInfoOne.Milling)
                                        {
                                            millingWheat = 40;
                                        }
                                        if (crop.Yield != null && crop.Yield > 7.0m)
                                        {
                                            yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 7.0m) / 0.1m) * 2);
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterBarley)
                                    {
                                        if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                                        {
                                            soilTypeAdjustment = 20;
                                        }
                                        if (crop.Yield != null && crop.Yield > 6.5m)
                                        {
                                            yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 6.5m) / 0.1m) * 2);
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WholecropWinterBarley)
                                    {
                                        if (field.SoilTypeID != null && field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow)
                                        {
                                            soilTypeAdjustment = 20;
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.SpringBarley)
                                    {
                                        if (crop.Yield != null && crop.Yield > 5.5m)
                                        {
                                            yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 5.5m) / 0.1m) * 2);
                                        }
                                    }
                                    else if (crop.CropTypeID.Value == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape)
                                    {
                                        if (crop.Yield != null && crop.Yield > 3.5m)
                                        {
                                            yieldAdjustment = (int)Math.Round(((crop.Yield.Value - 3.5m) / 0.1m) * 6);
                                        }
                                    }

                                }

                                int nMaxLimitForCropType = nMaxLimit;
                                if (nMaxLimit != null)
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
                                        MaximumLimitForNApplied = (int)Math.Round(nMaxLimitForCropType * field.CroppedArea.Value, 0)
                                    };
                                    nMaxLimitReportResponse.Add(nMaxLimitData);
                                    decimal? totalFertiliserN = null;
                                    decimal? totalOrganicAvailableN = null;
                                    (List<ManagementPeriod> ManPeriodList, error) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, false);
                                    if (string.IsNullOrWhiteSpace(error.Message) && ManPeriodList != null && ManPeriodList.Count > 0)
                                    {
                                        foreach (var managementPeriod in ManPeriodList)
                                        {
                                            (decimal? totalNitrogen, error) = await _fertiliserManureLogic.FetchTotalNByManagementPeriodID(managementPeriod.ID.Value);
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
                                            (decimal? totalNitrogen, error) = await _organicManureLogic.FetchAvailableNByManagementPeriodID(managementPeriod.ID.Value);
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
                                        InorganicNRate = totalFertiliserN != null ? (int)Math.Round(totalFertiliserN.Value, 0) : null,
                                        InorganicNTotal = totalFertiliserN != null ? (int)Math.Round((totalFertiliserN.Value * field.CroppedArea.Value), 0) : null,
                                        OrganicCropAvailableNRate = totalOrganicAvailableN != null ? (int)Math.Round(totalOrganicAvailableN.Value, 0) : null,
                                        OrganicCropAvailableNTotal = (totalOrganicAvailableN != null ? (int)Math.Round((totalOrganicAvailableN.Value * field.CroppedArea.Value), 0) : null),
                                        NRate = (totalFertiliserN == null && totalOrganicAvailableN == null) ? null : (int)Math.Round((totalFertiliserN ?? 0) + (totalOrganicAvailableN ?? 0), 0),
                                        NTotal = (totalFertiliserN == null && totalOrganicAvailableN == null) ? null : (int)Math.Round(((totalFertiliserN ?? 0) + (totalOrganicAvailableN ?? 0)) * field.CroppedArea.Value, 0),
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
    public async Task<IActionResult> ReportOptions(string f, string? h, string? r)
    {
        _logger.LogTrace("Report Controller : ReportOptions() action called");
        ReportViewModel model = null;
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = new ReportViewModel();
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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
                    (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                    if (farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.Country = farm.CountryID;
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
            if (model.FarmId != null && model.Country == null)
            {
                (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (farm != null)
                {
                    model.Country = farm.CountryID;
                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (!string.IsNullOrWhiteSpace(r))
            {
                model.IsManageImportExport = true;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
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
    public async Task<IActionResult> ReportOptions(ReportViewModel model)
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
                if (model.FarmId != null && model.Country == null)
                {
                    (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                    if (farm != null)
                    {
                        model.Country = farm.CountryID;
                    }
                }
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
            {
                model.NVZReportOption = null;
                model.FieldAndPlanReportOption = (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                {
                    return RedirectToAction("ExportFieldsOrCropType");
                }
                else
                {
                    return RedirectToAction("Year");
                }
            }
            if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FarmAndFieldDetailsForNVZRecord)
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = new ReportViewModel();
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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
            if (model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
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
    public async Task<IActionResult> NVZComplianceReports(string? q)//success Msg
    {
        _logger.LogTrace("Report Controller : NVZComplianceReports() action called");
        ReportViewModel model = null;
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = new ReportViewModel();
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            if (model.FarmId != null && model.Country == null)
            {
                (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (farm != null)
                {
                    model.FarmName = farm.Name;
                    model.Country = farm.CountryID;
                }
                HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(q);
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
    public async Task<IActionResult> NVZComplianceReports(ReportViewModel model)
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
                if (model.FarmId != null && model.Country == null)
                {
                    (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                    if (farm != null)
                    {
                        model.FarmName = farm.Name;
                        model.Country = farm.CountryID;
                    }
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                }
                return View(model);
            }
            model.FieldAndPlanReportOption = null;
            model.IsCheckList = false;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.NmaxReport)
            {

                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                {
                    return RedirectToAction("ExportFieldsOrCropType");
                }
                else
                {
                    return RedirectToAction("Year");
                }
            }
            if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.LivestockManureNFarmLimitReport)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                {
                    return RedirectToAction("IsGrasslandDerogation");
                }
                else
                {
                    return RedirectToAction("Year");
                }
            }
            string isComingFromPlan = _reportDataProtector.Protect(model.IsComingFromPlan.ToString());
            if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.ExistingManureStorageCapacityReport)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if ((model.IsComingFromPlan.HasValue && model.IsComingFromPlan.Value))
                {
                    return RedirectToAction("ManageStorageCapacity", "StorageCapacity", new { q = model.EncryptedFarmId, y = model.EncryptedHarvestYear, isPlan = isComingFromPlan });
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
    public async Task<IActionResult> Year(string? q)//success Msg
    {
        _logger.LogTrace("Report Controller : Year() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            List<int> yearList = GetReportYearsList();
            int maxYear = yearList.Max();

            //fetch plan by farmId
            List<PlanSummaryResponse> PlanYearList = await _cropLogic.FetchPlanSummaryByFarmId(model.FarmId.Value, 0);//0=plan
            if (PlanYearList.Count > 0 && PlanYearList.Any(x => x.Year > maxYear))
            {
                List<int> maxYearList = PlanYearList.Where(x => x.Year > maxYear).Select(x => x.Year).ToList();
                yearList.AddRange(maxYearList);
            }

            if (model.FieldAndPlanReportOption != null)
            {
                if (model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport)
                {
                    model.ReportTypeName = Resource.lblFieldRecordsAndNutrientManagementPlanning;
                }
                else if (model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.LivestockNumbersReport)
                {
                    model.ReportTypeName = Resource.lblLivestockNumbers;
                }
                else if (model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.ImportsAndExportsReport)
                {
                    model.ReportTypeName = Resource.lblImportsExports;
                }
            }
            else if (model.NVZReportOption != null)
            {
                if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.NmaxReport)
                {
                    model.ReportTypeName = model.Country == (int)NMP.Commons.Enums.FarmCountry.Wales ? Resource.lblMaximumNitrogenLimit : Resource.lblNMax;

                }
                else if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.LivestockManureNFarmLimitReport)
                {
                    model.ReportTypeName = Resource.lblLivestockManureNitrogenFarmLimit;
                }
                else if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.ExistingManureStorageCapacityReport)
                {
                    model.ReportTypeName = Resource.lblExistingManureStorageCapacityReport;
                }
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(q);
            }
            ViewBag.Years = yearList.OrderByDescending(x => x);

            HttpContext.Session.SetObjectAsJson("ReportData", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in Year() action : {ex.Message}, {ex.StackTrace}");
            if (model.ReportOption == (int)NMP.Commons.Enums.ReportOption.FieldRecordsAndPlan)
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
    public async Task<IActionResult> Year(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : Year() post action called");
        try
        {
            if (model.Year == null)
            {
                ModelState.AddModelError("Year", string.Format(Resource.lblSelectAOptionBeforeContinuing, Resource.lblYear.ToLower()));
            }
            List<int> yearList = GetReportYearsList();
            int maxYear = yearList.Max();
            if ((model.NVZReportOption != null && model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.NmaxReport) ||
                (model.FieldAndPlanReportOption != null && model.FieldAndPlanReportOption == (int)NMP.Commons.Enums.FieldAndPlanReportOption.CropFieldManagementReport))
            {
                List<PlanSummaryResponse> PlanYearList = await _cropLogic.FetchPlanSummaryByFarmId(model.FarmId.Value, 0);//0=plan
                if (PlanYearList.Count > 0 && PlanYearList.Any(x => x.Year > maxYear))
                {
                    List<int> maxYearList = PlanYearList.Where(x => x.Year > maxYear).Select(x => x.Year).ToList();
                    yearList.AddRange(maxYearList);
                }
            }
            if (model.NVZReportOption != null)
            {
                if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.LivestockManureNFarmLimitReport)
                {
                    (List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetail, Error error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmId(model.FarmId.Value);
                    if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingFarmDetail.Count > 0 && nutrientsLoadingFarmDetail.Any(x => x.CalendarYear > maxYear))
                    {
                        List<int> maxYearList = nutrientsLoadingFarmDetail.Where(x => x.CalendarYear > maxYear).Select(x => x.CalendarYear.Value).ToList();
                        yearList.AddRange(maxYearList);
                    }
                }
                else if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.ExistingManureStorageCapacityReport)
                {
                    (List<StoreCapacityResponse> storeCapacities, Error error) = await _storageCapacityLogic.FetchStoreCapacityByFarmIdAndYear(model.FarmId.Value, null);
                    if (string.IsNullOrWhiteSpace(error.Message) && storeCapacities.Count > 0 && storeCapacities.Any(x => x.Year > maxYear))
                    {
                        List<int> maxYearList = storeCapacities.Where(x => x.Year > maxYear).Select(x => x.Year.Value).ToList();
                        yearList.AddRange(maxYearList);
                    }
                }
            }
            ViewBag.Years = yearList.OrderByDescending(x => x);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.IsCheckList = false;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.LivestockManureNFarmLimitReport)
            {
                return RedirectToAction("IsGrasslandDerogation");
            }
            if (model.NVZReportOption == (int)NMP.Commons.Enums.NvzReportOption.ExistingManureStorageCapacityReport)
            {
                //(List<StoreCapacity> storeCapacityList, Error error) = await _reportService.FetchStoreCapacityByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);

                //if (string.IsNullOrWhiteSpace(error.Message) && storeCapacityList.Count > 0)
                //{
                string isComingFromPlan = _reportDataProtector.Protect(model.IsComingFromPlan.ToString());

                model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
                return RedirectToAction("ManageStorageCapacity", "StorageCapacity", new { q = model.EncryptedFarmId, y = model.EncryptedHarvestYear, isPlan = string.Empty });
                //}
                //return RedirectToAction("OrganicMaterialStorageNotAvailable");
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (!model.IsCheckList && model.Country != (int)NMP.Commons.Enums.FarmCountry.Wales)
            {

                (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails, Error error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.FarmId ?? 0, model.Year ?? 0);
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

                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockManureNitrogenReportChecklist", model);
                }
                else
                {
                    model.IsGrasslandDerogation = null;
                    model.TotalFarmArea = null;
                    model.TotalAreaInNVZ = null;
                    model.IsAnyLivestockNumber = null;
                    model.IsAnyLivestockImportExport = null;
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                }


            }
            if(model.Country == (int)NMP.Commons.Enums.FarmCountry.Wales)
            {
                model.IsGrasslandDerogation = false;
                var (savedData, error) = await SaveGrasslandDerogationAsync(model);
                if (savedData == null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["DerogationSaveError"] = error.Message;
                    return View(model);
                }
                HttpContext.Session.SetObjectAsJson("ReportData", model);

                return RedirectToAction("LivestockManureNitrogenReportChecklist");
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
            if (model.IsGrasslandDerogation == false)
            {
                model.GrassPercentage = null;
            }

            var (savedData, error) = await SaveGrasslandDerogationAsync(model);
            if (savedData == null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["DerogationSaveError"] = error.Message;
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

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
    public async Task<IActionResult> LivestockManureNitrogenReportChecklist(string? q, string? r)
    {
        _logger.LogTrace("Report Controller : LivestockManureNitrogenReportChecklist() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            Error error = null;
            if (model.FarmId != null && model.Country == null)
            {
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (farm != null)
                {
                    model.Country = farm.CountryID;
                }
            }
            model.IsCheckList = true;
            model.IsManageImportExport = false;
            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());
            if (!string.IsNullOrWhiteSpace(q))
            {
                model.IsComingFromSuccessMsg = true;
            }
            (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails, error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.FarmId ?? 0, model.Year ?? 0);
            if (nutrientsLoadingFarmDetails != null)
            {
                model.IsGrasslandDerogation = nutrientsLoadingFarmDetails.Derogation;
                model.TotalFarmArea = nutrientsLoadingFarmDetails.TotalFarmed;
                model.TotalAreaInNVZ = nutrientsLoadingFarmDetails.LandInNVZ;
                model.GrassPercentage = nutrientsLoadingFarmDetails.GrassPercentage;
                if (nutrientsLoadingFarmDetails.IsAnyLivestockNumber != null)
                {
                    ViewBag.IsAnyLivestockNumberFromFarmDetail = true;
                }
                if (nutrientsLoadingFarmDetails.IsAnyLivestockImportExport != null)
                {
                    ViewBag.IsAnyLivestockImportExportFromFarmDetail = true;
                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (!string.IsNullOrWhiteSpace(r))
            {
                TempData["succesMsgContent"] = _reportDataProtector.Unprotect(r);
            }
            (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
            if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManuresList.Count > 0)
            {
                nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                if (nutrientsLoadingManuresList.Count > 0)
                {
                    ViewBag.NutrientsLoadingManuresData = nutrientsLoadingManuresList;
                }
            }
            //if(ViewBag.IsAnyLivestockImportExportFromFarmDetail != null&& nutrientsLoadingManuresList.Count==0)
            //{
            //    model.IsAnyLivestockImportExport = false;
            //}
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.NutrientLivestockData = nutrientsLoadingLiveStockList;
            //if (ViewBag.IsAnyLivestockNumberFromFarmDetail != null && nutrientsLoadingLiveStockList.Count == 0)
            //{
            //    model.IsAnyLivestockNumber = false;
            //}

            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            if (model.TotalFarmArea == null || (model.Country == (int)NMP.Commons.Enums.FarmCountry.England && model.TotalAreaInNVZ == null))
            {
                ModelState.AddModelError(string.Empty, string.Format(Resource.MsgFarmAreaForYearMustBeCompleted, model.Year));
            }
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, Error error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.NutrientLivestockData = nutrientsLoadingLiveStockList;
            (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                if (nutrientsLoadingManuresList.Count > 0)
                {
                    nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                    ViewBag.NutrientsLoadingManuresData = nutrientsLoadingManuresList;
                }
            }
            (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails, error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.FarmId ?? 0, model.Year ?? 0);
            if (nutrientsLoadingFarmDetails != null)
            {
                model.IsGrasslandDerogation = nutrientsLoadingFarmDetails.Derogation;
                model.TotalFarmArea = nutrientsLoadingFarmDetails.TotalFarmed;
                model.TotalAreaInNVZ = nutrientsLoadingFarmDetails.LandInNVZ;
                if (nutrientsLoadingFarmDetails.IsAnyLivestockNumber != null || nutrientsLoadingLiveStockList.Count > 0)
                {
                    ViewBag.IsAnyLivestockNumberFromFarmDetail = true;
                }
                if (nutrientsLoadingFarmDetails.IsAnyLivestockImportExport != null || nutrientsLoadingManuresList.Count > 0)
                {
                    ViewBag.IsAnyLivestockImportExportFromFarmDetail = true;
                }
            }
            if (!model.IsAnyLivestockNumber.HasValue && nutrientsLoadingLiveStockList.Count == 0 &&
                ViewBag.IsAnyLivestockNumberFromFarmDetail == null)
            {
                ModelState.AddModelError(string.Empty, string.Format(Resource.MsgLivestockNumbersForYearMustBeCompleted, model.Year));
            }
            if (!model.IsAnyLivestockImportExport.HasValue && nutrientsLoadingManuresList.Count == 0 &&
                ViewBag.IsAnyLivestockImportExportFromFarmDetail == null)
            {
                ModelState.AddModelError(string.Empty, string.Format(Resource.MsgImportsAndExportsOfManureForYearMustBeCompleted, model.Year));
            }
            model.EncryptedHarvestYear = _farmDataProtector.Protect(model.Year.ToString());


            if (!ModelState.IsValid)
            {
                if (model.FarmId != null && model.Country == null)
                {
                    (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                    if (farm != null)
                    {
                        model.Country = farm.CountryID;
                    }
                }
                return View("~/Views/Report/LivestockManureNitrogenReportChecklist.cshtml", model);
            }


            HttpContext.Session.SetObjectAsJson("ReportData", model);
            return RedirectToAction("LivestockManureNFarmLimitReport");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockManureNitrogenReportChecklist() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> FarmAreaForLivestockManure()
    {
        _logger.LogTrace("Report Controller : FarmAreaForLivestockManure() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model.FarmId != null && model.Country == null)
            {
                (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (farm != null)
                {
                    model.Country = farm.CountryID;
                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            Error error = null;
            string totalFarmAreaKey = "TotalFarmArea";
            string totalAreaInNVZKey = "TotalAreaInNVZ";
            if (model.FarmId != null && model.Country == null)
            {
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (farm != null)
                {
                    model.Country = farm.CountryID;
                }
            }
            if (model.TotalFarmArea == null)
            {
                ModelState.AddModelError(totalFarmAreaKey, Resource.MsgEnterTotalFarmArea);
            }
            if (model.TotalAreaInNVZ == null && (model.Country != null && model.Country != (int)NMP.Commons.Enums.FarmCountry.Wales))
            {
                ModelState.AddModelError(totalAreaInNVZKey, Resource.MsgEnterTotalAreaInNVZ);
            }
            if (model.IsGrasslandDerogation == true)
            {
                if (model.GrassPercentage == null)
                {
                    ModelState.AddModelError("GrassPercentage", Resource.MsgEnterThePercentageOfTheLandIsFarmedAsGrass);
                }
            }
            if (model.TotalFarmArea <= 0)
            {
                ModelState.AddModelError(totalFarmAreaKey, Resource.MsgTotalFarmAreaShouldBeGreaterThanZero);
            }
            if (model.TotalAreaInNVZ < 0)
            {
                ModelState.AddModelError(totalAreaInNVZKey, Resource.MsgTotalAreaInNVZShouldNotBeLessThanZero);
            }
            if (model.TotalAreaInNVZ > model.TotalFarmArea)
            {
                ModelState.AddModelError(totalAreaInNVZKey, Resource.MsgTotalAreaInNVZShouldNotBeMoreThanTotalFarmArea);
            }
            if (model.TotalFarmArea != null && (ModelState.ContainsKey(totalFarmAreaKey) && Math.Round(model.TotalFarmArea.Value, 2) != model.TotalFarmArea))
            {
                ModelState.AddModelError(totalFarmAreaKey, string.Format(Resource.lblFarmAreaCanHaveOnlyTwoDecimalPlace, Resource.lblTotalFarmArea.ToLower()));
            }
            if (model.TotalAreaInNVZ != null && (ModelState.ContainsKey(totalAreaInNVZKey) && Math.Round(model.TotalAreaInNVZ.Value, 2) != model.TotalAreaInNVZ))
            {
                ModelState.AddModelError(totalAreaInNVZKey, string.Format(Resource.lblFarmAreaCanHaveOnlyTwoDecimalPlace, Resource.lblTotalAreaInAnNvz));
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.NutrientLivestockData = nutrientsLoadingLiveStockList;
            (List<NutrientsLoadingManures> nutrientsLoadingManures, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
            if (nutrientsLoadingManures.Count > 0)
            {
                nutrientsLoadingManures = nutrientsLoadingManures.Where(x => x.ManureDate.Value.Date.Year == model.Year).ToList();
                ViewBag.NutrientLivestockData = nutrientsLoadingManures;
            }

            if (model.Country == (int)NMP.Commons.Enums.FarmCountry.Wales)
            {
                model.TotalAreaInNVZ = model.TotalFarmArea;
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
                GrassPercentage = model.GrassPercentage,
                ContingencyPlan = false,
                IsAnyLivestockImportExport = (!model.IsAnyLivestockImportExport.HasValue) ?
                null : (nutrientsLoadingManures.Count > 0 ? true : false),
                IsAnyLivestockNumber = (!model.IsAnyLivestockNumber.HasValue) ?
                null : (nutrientsLoadingLiveStockList.Count > 0 ? true : false),
            };
            (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData, error) = await _reportLogic.UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);
            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["FarmDetailsSaveError"] = error.Message;
                return View(model);
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in FarmAreaForLivestockManure() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnFarmAreaForLivestockManure"] = ex.Message;
            return View(model);
        }
    }

    public async Task<IActionResult> BackCheckList()
    {
        _logger.LogTrace("Report Controller : BackCheckList() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckList = false;
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (model.IsComingFromSuccessMsg.Value)
            {
                model.IsComingFromSuccessMsg = false;
                return RedirectToAction("ManageImportExport", new
                {
                    q = model.EncryptedFarmId,
                    y = _farmDataProtector.Protect(model.Year.ToString())
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in BackCheckList() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");

        }

        (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetails, Error error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.FarmId ?? 0, model.Year ?? 0);
        if (!string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        if (nutrientsLoadingFarmDetails != null)
        {
            if (model.IsComingFromPlan.HasValue && (!model.IsComingFromPlan.Value))
            {
                return RedirectToAction("Year");
            }
            else
            {
                return RedirectToAction("NVZComplianceReports");
            }
        }
        else
        {
            return RedirectToAction("IsGrasslandDerogation");
        }

    }

    [HttpGet]
    public IActionResult IsAnyLivestockImportExport()
    {
        _logger.LogTrace("Report Controller : IsAnyLivestockImportExport() action called");
        ReportViewModel? model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in IsAnyLivestockImportExport() action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");

        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IsAnyLivestockImportExport(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : IsAnyLivestockImportExport() post action called");
        try
        {
            if (model.IsAnyLivestockImportExport == null)
            {
                ModelState.AddModelError("IsAnyLivestockImportExport", Resource.MsgSelectYesIfYouHadAnyImportsOrExportsOfLivestockManure);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.IsAnyLivestockImportExport.HasValue && !model.IsAnyLivestockImportExport.Value)
            {
                model.IsCheckAnswer = false;
                (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, Error error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
                ViewBag.NutrientLivestockData = nutrientsLoadingLiveStockList;
                (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (nutrientsLoadingManuresList.Count > 0)
                    {
                        nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                        ViewBag.NutrientsLoadingManuresData = nutrientsLoadingManuresList;
                    }
                }
                if (model.Country == (int)NMP.Commons.Enums.FarmCountry.Wales)
                {
                    model.TotalAreaInNVZ = model.TotalFarmArea;
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
                    GrassPercentage = model.GrassPercentage,
                    ContingencyPlan = false,
                    IsAnyLivestockImportExport = nutrientsLoadingManuresList.Count > 0,
                    IsAnyLivestockNumber = nutrientsLoadingLiveStockList.Count > 0,
                };
                (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData, error) = await _reportLogic.AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
                    return RedirectToAction("LivestockManureNitrogenReportChecklist");
                }
                return RedirectToAction("LivestockManureNitrogenReportChecklist");
            }
            else
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("ImportExportOption");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in IsAnyLivestockImportExport() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnIsAnyLivestockImportExport"] = ex.Message;
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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

            HttpContext.Session.SetObjectAsJson("ReportData", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in ImportExportOption() action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnIsAnyLivestockImportExport"] = ex.Message;
            return RedirectToAction("IsAnyLivestockImportExport");

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
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
            if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
            {
                int manureGroup = model.ManureGroupIdForFilter == null ? (int)NMP.Commons.Enums.ManureGroup.LivestockManure
                    : model.ManureGroupIdForFilter.Value;
                (List<ManureType> ManureTypes, error) = await _organicManureLogic.FetchManureTypeList(manureGroup, farm.CountryID.Value);
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
            if (!string.IsNullOrWhiteSpace(q))
            {
                string import = _reportDataProtector.Unprotect(q);
                if (!string.IsNullOrWhiteSpace(import))
                {
                    if (import == Resource.lblImport)
                    {
                        model.IsImport = true;
                        model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Import;
                    }
                    else
                    {
                        model.IsImport = false;
                        model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Export;
                    }
                }
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in ManureType() action : {ex.Message}, {ex.StackTrace}");
            if (model.IsImport == null)
            {
                TempData["ErrorOnImportExportOption"] = ex.Message;
                return RedirectToAction("ImportExportOption");
            }
            else
            {
                TempData["ManageImportExportError"] = ex.Message;
                return RedirectToAction("ManageImportExport", new
                {
                    q = model.EncryptedFarmId,
                    y = _farmDataProtector.Protect(model.Year.ToString())
                });
            }

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
                (Farm farm, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
                {
                    int manureGroup = model.ManureGroupIdForFilter == null ? (int)NMP.Commons.Enums.ManureGroup.LivestockManure
                    : model.ManureGroupIdForFilter.Value;
                    (List<ManureType> ManureTypes, error) = await _organicManureLogic.FetchManureTypeList(manureGroup, farm.CountryID.Value);
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
            (ManureType manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
            if (error == null && manureType != null)
            {
                model.IsManureTypeLiquid = manureType.IsLiquid.Value;
                model.ManureTypeName = manureType.Name;
                //model.ManureGroupId = manureType.ManureGroupId;
            }
            if (model.ManureGroupIdForFilter.HasValue)
            {
                model.ManureGroupId = model.ManureGroupIdForFilter;
            }
            ReportViewModel reportViewModel = new ReportViewModel();
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                reportViewModel = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            if (reportViewModel != null && reportViewModel.ManureTypeId != model.ManureTypeId)
            {
                model.IsDefaultValueChange = true;
                model.IsManureTypeChange = true;
                if (manureType != null && reportViewModel.ManureTypeId != null)
                {
                    model.ManureType = manureType;
                    model.DryMatterPercent = manureType.DryMatter;
                    model.NH4N = manureType.NH4N;
                    model.NO3N = manureType.NO3N;
                    model.SO3 = manureType.SO3;
                    model.K2O = manureType.K2O;
                    model.MgO = manureType.MgO;
                    model.UricAcid = manureType.Uric;
                    model.N = manureType.TotalN;
                    model.P2O5 = manureType.P2O5;
                }
            }

            (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
            if (error == null && farmManureTypeList.Count > 0)
            {
                FarmManureTypeResponse previousFarmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == reportViewModel.ManureTypeId);
                FarmManureTypeResponse currentFarmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureTypeId);
                if (previousFarmManure != null && currentFarmManure == null)
                {
                    model.DefaultNutrientValue = Resource.lblYes;
                }

            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.ManureTypeId == (int)ManureTypes.OtherSolidMaterials || model.ManureTypeId == (int)ManureTypes.OtherLiquidMaterials)
            {
                return RedirectToAction("OtherMaterialName");
            }
            else
            {
                model.OtherMaterialName = null;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
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

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange))
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            if (ModelState.TryGetValue(nameof(model.LivestockQuantity), out var entry))
            {
                foreach (var error in entry.Errors.ToList())
                {
                    if (error.ErrorMessage.Contains("is not valid for", StringComparison.OrdinalIgnoreCase))
                    {

                        entry.Errors.Remove(error);
                        entry.Errors.Add(new ModelError(Resource.MsgEnterAnQuantityBetweenValue));
                    }
                }
            }
            if (model.LivestockQuantity == null)
            {
                ModelState.AddModelError("LivestockQuantity", Resource.lblEnterTheAmountYouImportedInTonnes);
            }
            else
            {
                if (model.LivestockQuantity < 1 || model.LivestockQuantity > 999999)
                {
                    ModelState.AddModelError("LivestockQuantity", Resource.MsgEnterAnQuantityBetweenValue);
                }
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange))
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
    public async Task<IActionResult> UpdateLivestockImportExport(string q, string? r)//q=FarmId, r=success msg
    {
        _logger.LogTrace($"Report Controller : UpdateLivestockImportExport({q},{r}) action called");
        ReportViewModel model = new ReportViewModel();
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (!string.IsNullOrWhiteSpace(r))
            {
                TempData["succesMsgContent"] = _reportDataProtector.Unprotect(r);
            }
            int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
            if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
            {
                model.FarmName = farm.Name;
                model.FarmId = decryptedFarmId;
                model.EncryptedFarmId = q;
                List<HarvestYear> harvestYearList = new List<HarvestYear>();

                model.IsComingFromImportExportOverviewPage = _reportDataProtector.Protect(Resource.lblTrue);
                (List<NutrientsLoadingFarmDetail> nutrientsLoadingFarmDetailList, error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmId(decryptedFarmId);
                if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingFarmDetailList != null && nutrientsLoadingFarmDetailList.Count > 0)
                {
                    (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(decryptedFarmId);
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
                HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            else
            {
                TempData["Error"] = error.Message;
                return RedirectToAction("FarmSummary", "Farm", new { q = q });
            }

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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

            Error? error = null;
            FarmManureTypeResponse? farmManure = null;
            (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
            if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
            {
                if (model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
                {
                    (ManureType manureType, Error manureTypeError) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    model.ManureType = manureType;

                    if (error == null)
                    {
                        if (farmManureTypeList.Count > 0)
                        {
                            farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter);
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
                                model.DefaultFarmManureValueDate = farmManure.ModifiedOn == null ? farmManure.CreatedOn : farmManure.ModifiedOn;
                            }
                            else
                            {
                                model.DefaultFarmManureValueDate = null;
                            }
                        }
                    }
                    if (manureTypeError == null)
                    {
                        model.ManureType = manureType;
                    }
                    model.IsDefaultNutrient = true;
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                }
                else
                {
                    model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockManualNutrientValue");
                }
            }
            else
            {
                if (error == null)
                {
                    (ManureType manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);

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
            }

            model.IsDefaultNutrient = true;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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

            (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
            if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
            {
                if (model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
                {
                    (ManureType manureType, Error manureTypeError) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    model.ManureType = manureType;
                    if (error == null)
                    {
                        if (farmManureTypeList.Count > 0)
                        {
                            farmManure = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter);
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
                    }
                    if (manureTypeError == null)
                    {
                        model.ManureType = manureType;
                    }
                    model.IsDefaultNutrient = true;

                }
                else
                {
                    model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;

                }
            }
            else
            {
                (ManureType manureType, Error manureTypeError) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
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
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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

            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                reportViewModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }

            if (reportViewModel != null && (!string.IsNullOrWhiteSpace(reportViewModel.DefaultNutrientValue)))
            {
                if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && (model.DefaultNutrientValue == Resource.lblYesUseTheseValues || model.DefaultNutrientValue == Resource.lblYes))
                {
                    (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
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
                            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
                    (ManureType manureType, Error error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    model.ManureType = manureType;

                    model.IsThisDefaultValueOfRB209 = true;
                    if (reportViewModel.DefaultNutrientValue != model.DefaultNutrientValue && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                    {
                        ViewBag.RB209ApiOption = Resource.lblTrue;
                        HttpContext.Session.SetObjectAsJson("ReportData", model);
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

                    (List<FarmManureTypeResponse> farmManureTypeList, Error error1) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
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
                    (ManureType manureType, Error error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                    model.ManureType = manureType;
                    if (!string.IsNullOrWhiteSpace(model.DefaultNutrientValue) && model.DefaultNutrientValue == Resource.lblYesUseTheseStandardNutrientValues)
                    {
                        model.IsThisDefaultValueOfRB209 = true;
                        ViewBag.RB209ApiOption = Resource.lblTrue;
                        HttpContext.Session.SetObjectAsJson("ReportData", model);
                        return View(model);
                    }

                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

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
        if (HttpContext.Session.Keys.Contains("ReportData"))
        {
            model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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
                if (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.PigSlurry ||
                    model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.CattleSlurry)
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

            decimal totalNutrient =
                (model.DryMatterPercent ?? 0) +
                (model.N ?? 0) +
                (model.NH4N ?? 0) +
                (model.UricAcid ?? 0) +
                (model.NO3N ?? 0) +
                (model.P2O5 ?? 0) +
                (model.K2O ?? 0) +
                (model.MgO ?? 0) +
                (model.SO3 ?? 0);

            if (totalNutrient <= 0)
            {
                ModelState.AddModelError("ManureTypeId", Resource.MsgEnterAtLeastOneValue);
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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
            ModelState.AddModelError("ReceiverName", string.Format(Resource.MsgEnterTheNameOfThePersonOrOrganisationYouAreFrom, model.ImportExport == (int)NMP.Commons.Enums.ImportExport.Import ?
                Resource.lblImporting : Resource.lblExporting));
        }

        if (!string.IsNullOrWhiteSpace(model.Address1) && model.Address1.Length > 50)
        {
            ModelState.AddModelError("Address1", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine1, 50));
        }
        if (!string.IsNullOrWhiteSpace(model.Address2) && model.Address2.Length > 50)
        {
            ModelState.AddModelError("Address2", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblAddressLine2ForErrorMsg, 50));
        }
        if (!string.IsNullOrWhiteSpace(model.Address3) && model.Address3.Length > 50)
        {
            ModelState.AddModelError("Address3", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblTownOrCity, 50));
        }
        if (!string.IsNullOrWhiteSpace(model.Address4) && model.Address4.Length > 50)
        {
            ModelState.AddModelError("Address4", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblCounty, 50));
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
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

        if (!string.IsNullOrWhiteSpace(model.Comment) && model.Comment.Length > 100)
        {
            ModelState.AddModelError("Comment", string.Format(Resource.lblModelPropertyCannotBeLongerThanNumberCharacters, Resource.lblComment, 100));
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
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = false;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (!string.IsNullOrWhiteSpace(model.EncryptedId))
            {
                return RedirectToAction("ManageImportExport", new
                {
                    q = model.EncryptedFarmId,
                    y = model.EncryptedHarvestYear
                });
            }
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
    public async Task<IActionResult> LivestockImportExportCheckAnswer(string? i)
    {
        _logger.LogTrace("Report Controller : LivestockImportExportCheckAnswer() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            model.IsCheckAnswer = true;
            model.IsManureTypeChange = false;
            model.IsDefaultValueChange = false;
            model.IsCancel = null;
            Error error = null;

            if (!string.IsNullOrWhiteSpace(i))
            {
                int decryptedId = Convert.ToInt32(_reportDataProtector.Unprotect(i));
                if (decryptedId > 0)
                {
                    (NutrientsLoadingManures nutrientsLoadingManure, error) = await _reportLogic.FetchNutrientsLoadingManuresByIdAsync(decryptedId);
                    if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManure != null)
                    {
                        model.ImportExport = (int)Enum.Parse(typeof(NMP.Commons.Enums.ImportExport), nutrientsLoadingManure.ManureLookupType);
                        model.ManureTypeId = nutrientsLoadingManure.ManureTypeID;
                        model.LivestockImportExportDate = nutrientsLoadingManure.ManureDate.Value.ToLocalTime();
                        model.LivestockQuantity = nutrientsLoadingManure.Quantity.Value;
                        model.ReceiverName = nutrientsLoadingManure.FarmName;
                        model.Address1 = nutrientsLoadingManure.Address1;
                        model.Address2 = nutrientsLoadingManure.Address2;
                        model.Address3 = nutrientsLoadingManure.Address3;
                        model.Address4 = nutrientsLoadingManure.Address4;
                        model.Postcode = nutrientsLoadingManure.PostCode;
                        model.Comment = nutrientsLoadingManure.Comments;
                        model.IsComingFromPlan = false;
                        (ManureType manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureTypeId.Value);
                        if (error == null && manureType != null)
                        {
                            model.IsManureTypeLiquid = manureType.IsLiquid;
                            model.ManureGroupId = manureType.ManureGroupId;
                            model.ManureGroupIdForFilter = manureType.ManureGroupId;
                        }
                        model.ManureTypeName = nutrientsLoadingManure.ManureType;
                        model.EncryptedId = i;
                        model.N = nutrientsLoadingManure.NContent;
                        model.FarmId = nutrientsLoadingManure.FarmID;
                        model.Year = nutrientsLoadingManure.ManureDate.Value.Year;
                        model.P2O5 = nutrientsLoadingManure.PContent;
                        model.ManureType = new ManureType();
                        model.ManureType.TotalN = nutrientsLoadingManure.NContent;
                        model.ManureType.P2O5 = nutrientsLoadingManure.PContent;
                        model.MgO = nutrientsLoadingManure.MgO;
                        model.NH4N = nutrientsLoadingManure.NH4N;
                        model.NO3N = nutrientsLoadingManure.NO3N;
                        model.SO3 = nutrientsLoadingManure.SO3;
                        model.K2O = nutrientsLoadingManure.K2O;
                        model.DryMatterPercent = nutrientsLoadingManure.DryMatterPercent;
                        model.UricAcid = nutrientsLoadingManure.UricAcid;
                        model.ManureType.MgO = nutrientsLoadingManure.MgO;
                        model.ManureType.NH4N = nutrientsLoadingManure.NH4N;
                        model.ManureType.NO3N = nutrientsLoadingManure.NO3N;
                        model.ManureType.SO3 = nutrientsLoadingManure.SO3;
                        model.ManureType.K2O = nutrientsLoadingManure.K2O;
                        model.ManureType.DryMatter = nutrientsLoadingManure.DryMatterPercent;
                        model.ManureType.Uric = nutrientsLoadingManure.UricAcid;
                        (List<FarmManureTypeResponse> farmManureTypeResponse, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId.Value);
                        if (error == null && farmManureTypeResponse != null && farmManureTypeResponse.Count > 0)
                        {
                            FarmManureTypeResponse farmManureType = farmManureTypeResponse.Where(x => x.ManureTypeID == model.ManureTypeId && x.ManureTypeName == model.ManureTypeName).FirstOrDefault();
                            if (farmManureType != null)
                            {
                                if (model.ManureTypeId != null && (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials) &&
                                   farmManureType.ManureTypeName.Equals(nutrientsLoadingManure.ManureType))
                                {
                                    if (farmManureType.TotalN == model.N && farmManureType.P2O5 == model.P2O5 &&
                                    farmManureType.DryMatter == model.DryMatterPercent && farmManureType.Uric == model.UricAcid &&
                                    farmManureType.NH4N == model.NH4N && farmManureType.NO3N == model.NO3N &&
                                    farmManureType.SO3 == model.SO3 && farmManureType.K2O == model.K2O &&
                                    farmManureType.MgO == model.MgO)
                                    {
                                        model.DefaultNutrientValue = Resource.lblYes;
                                    }
                                }
                                else
                                {
                                    if (farmManureType.TotalN == model.N && farmManureType.P2O5 == model.P2O5 &&
                                    farmManureType.DryMatter == model.DryMatterPercent && farmManureType.Uric == model.UricAcid &&
                                    farmManureType.NH4N == model.NH4N && farmManureType.NO3N == model.NO3N &&
                                    farmManureType.SO3 == model.SO3 && farmManureType.K2O == model.K2O &&
                                    farmManureType.MgO == model.MgO)
                                    {

                                        model.DefaultNutrientValue = Resource.lblYesUseTheseValues;
                                    }
                                }
                                if (model.ManureTypeId != null && (model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureTypeId == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials) &&
                                   farmManureType.ManureTypeName.Equals(nutrientsLoadingManure.ManureType))
                                {
                                    model.OtherMaterialName = farmManureType.ManureTypeName;
                                    model.ManureGroupId = nutrientsLoadingManure.ManureTypeID;
                                    model.ManureGroupIdForFilter = nutrientsLoadingManure.ManureTypeID;
                                }
                                model.DefaultFarmManureValueDate = farmManureType.ModifiedOn == null ? farmManureType.CreatedOn : farmManureType.ModifiedOn;
                            }
                            else
                            {
                                model.DefaultNutrientValue = Resource.lblYes;
                            }
                        }
                        else if (farmManureTypeResponse.Count == 0)
                        {
                            model.DefaultNutrientValue = Resource.lblYes;
                        }
                        if (string.IsNullOrWhiteSpace(model.DefaultNutrientValue))
                        {
                            if (manureType.TotalN == model.N && manureType.P2O5 == model.P2O5 &&
                                manureType.DryMatter == model.DryMatterPercent && manureType.Uric == model.UricAcid &&
                                manureType.NH4N == model.NH4N && manureType.NO3N == model.NO3N &&
                                manureType.SO3 == model.SO3 && manureType.K2O == model.K2O &&
                                manureType.MgO == model.MgO)
                            {
                                model.DefaultNutrientValue = Resource.lblYesUseTheseStandardNutrientValues;
                            }
                            else
                            {
                                model.DefaultNutrientValue = Resource.lblIwantToEnterARecentOrganicMaterialAnalysis;
                            }
                        }
                    }
                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (!string.IsNullOrWhiteSpace(i))
            {
                HttpContext?.Session.SetObjectAsJson("LivestockImportExportDataBeforeUpdate", model);

            }
            var previousModel = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("LivestockImportExportDataBeforeUpdate");

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
            _logger.LogTrace($"Report Controller : Exception in LivestockImportExportCheckAnswer() action : {ex.Message}, {ex.StackTrace}");
            if (string.IsNullOrWhiteSpace(model.EncryptedId))
            {
                TempData["ErrorOnLivestockComment"] = ex.Message;
                return RedirectToAction("LivestockComment");
            }
            else
            {
                TempData["ManageImportExportError"] = ex.Message;
                return RedirectToAction("ManageImportExport", new
                {
                    q = model.EncryptedFarmId,
                    y = _farmDataProtector.Protect(model.Year.ToString())
                });
            }

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
            (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId.Value);
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
                model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Import;
            }
            else
            {
                model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Export;
            }
        }
        decimal totalN = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.N.Value : model.ManureType.TotalN.Value;
        decimal totalP = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? model.P2O5.Value : model.ManureType.P2O5.Value;
        NutrientsLoadingManures nutrientsLoadingManure = new NutrientsLoadingManures();
        nutrientsLoadingManure.FarmID = model.FarmId.Value;
        nutrientsLoadingManure.ManureLookupType = Enum.GetName(typeof(NMP.Commons.Enums.ImportExport), model.ImportExport);
        nutrientsLoadingManure.ManureTypeID = model.ManureTypeId.Value;
        nutrientsLoadingManure.ManureType = (string.IsNullOrWhiteSpace(model.OtherMaterialName) ? model.ManureTypeName : model.OtherMaterialName);
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
        if (!string.IsNullOrWhiteSpace(model.EncryptedId))
        {
            nutrientsLoadingManure.ID = Convert.ToInt32(_reportDataProtector.Unprotect(model.EncryptedId));
        }


        var jsonData = new
        {
            NutrientsLoadingManure = nutrientsLoadingManure,
            SaveDefaultForFarm = model.DefaultNutrientValue == Resource.lblIwantToEnterARecentOrganicMaterialAnalysis ? true : false
        };
        string jsonString = JsonConvert.SerializeObject(jsonData);
        NutrientsLoadingManures nutrientsLoadingManureData = null;
        if (!string.IsNullOrWhiteSpace(model.EncryptedId))
        {
            (nutrientsLoadingManureData, error) = await _reportLogic.UpdateNutrientsLoadingManuresAsync(jsonString);
        }
        else
        {
            (nutrientsLoadingManureData, error) = await _reportLogic.AddNutrientsLoadingManuresAsync(jsonString);
        }

        if (nutrientsLoadingManureData != null && string.IsNullOrWhiteSpace(error.Message))
        {
            string successMsg = _reportDataProtector.Protect(string.Format(Resource.MsgImportExportSuccessMsgContent1, string.IsNullOrWhiteSpace(model.EncryptedId) ? Resource.lblAdded : Resource.lblUpdated, model.ImportExport == (int)NMP.Commons.Enums.ImportExport.Import ? Resource.lblImport.ToLower() : Resource.lblExport.ToLower()));
            model = ResetReportDataFromSession(false);
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            return RedirectToAction("ManageImportExport", new
            {
                q = model.EncryptedFarmId,
                y = _farmDataProtector.Protect(model.Year.ToString()),
                r = successMsg,
                s = _reportDataProtector.Protect(Resource.lblTrue)
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
        if (HttpContext.Session.Keys.Contains("LivestockImportExportDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("LivestockImportExportDataBeforeUpdate");
        }
        ReportViewModel model = new ReportViewModel();
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (string.IsNullOrWhiteSpace(model.IsComingFromImportExportOverviewPage))
            {
                model = ResetReportDataFromSession(false);
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                ViewBag.IsManageImportExport = _reportDataProtector.Protect(Resource.lblTrue);
            }
            if (!string.IsNullOrWhiteSpace(model.EncryptedId))
            {
                model.EncryptedId = null;
            }
            model.IsComingFromSuccessMsg = false;
            int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
            if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
            {
                if (!string.IsNullOrWhiteSpace(r))
                {
                    TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(r);
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        ViewBag.isComingFromSuccessMsg = _reportDataProtector.Protect(Resource.lblTrue);
                        TempData["succesMsgContent2"] = Resource.MsgImportExportSuccessMsgContent2;
                        TempData["succesMsgContent3"] = string.Format(Resource.MsgImportExportSuccessMsgContent3, _farmDataProtector.Unprotect(y));
                    }
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
                (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(decryptedFarmId);
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
                            nutrientsLoadingManuresList.ForEach(x => x.EncryptedID = _reportDataProtector.Protect(x.ID.Value.ToString()));
                            ViewBag.ImportList = nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper()).ToList();
                            string unit = "";
                            (Farm farmData, error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
                            if (string.IsNullOrWhiteSpace(error.Message) && farmData != null)
                            {
                                (List<ManureType> ManureTypes, error) = await _organicManureLogic.FetchManureTypeList((int)NMP.Commons.Enums.ManureGroup.LivestockManure, farmData.CountryID.Value);
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
                            decimal? totalImports = (nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper()).Sum(x => x.NTotal));
                            ViewBag.TotalImportsInKg = string.Format("{0:N2}", totalImports);
                            decimal? totalExports = (nutrientsLoadingManuresList.Where(x => x.ManureLookupType?.ToUpper() == Resource.lblExport.ToUpper()).Sum(x => x.NTotal));
                            ViewBag.TotalExportsInKg = string.Format("{0:N2}", totalExports);
                            decimal netTotal = Math.Round((totalImports ?? 0) - (totalExports ?? 0), 0);
                            ViewBag.NetTotal = string.Format("{0}{1}", netTotal > 0 ? "+" : "", string.Format("{0:N0}", netTotal));
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
                        HttpContext.Session.SetObjectAsJson("ReportData", model);
                        return RedirectToAction("IsAnyLivestockImportExport", model);
                    }
                }
                else
                {
                    model.IsManageImportExport = false;
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("IsAnyLivestockImportExport", model);
                }
            }
            else
            {
                TempData["Error"] = error.Message;
                return RedirectToAction("FarmSummary", "Farm", new { q = q });
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
        }
        if (!string.IsNullOrWhiteSpace(y))
        {
            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
            model.EncryptedHarvestYear = y;
        }

        model.IsManageImportExport = true;
        HttpContext.Session.SetObjectAsJson("ReportData", model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> IsAnyLivestockNumber()
    {
        _logger.LogTrace("Report Controller : IsAnyLivestockNumber() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, Error error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.LiveStockList = nutrientsLoadingLiveStockList;

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in IsAnyLivestockNumber() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = ex.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");

        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IsAnyLivestockNumber(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : IsAnyLivestockNumber() post action called");
        try
        {
            if (model.IsAnyLivestockNumber == null)
            {
                ModelState.AddModelError("IsAnyLivestockNumber", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            ReportViewModel reportModel = new ReportViewModel();
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                reportModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            if (model.IsLivestockCheckAnswer)
            {
                if (model.IsAnyLivestockNumber == reportModel.IsAnyLivestockNumber)
                {
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockCheckAnswer");
                }
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (model.IsAnyLivestockNumber == false)
            {
                (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, Error error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
                ViewBag.NutrientLivestockData = nutrientsLoadingLiveStockList;
                (List<NutrientsLoadingManures> nutrientsLoadingManuresList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (nutrientsLoadingManuresList.Count > 0)
                    {
                        nutrientsLoadingManuresList = nutrientsLoadingManuresList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
                        ViewBag.NutrientsLoadingManuresData = nutrientsLoadingManuresList;
                    }
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
                    GrassPercentage = model.GrassPercentage,
                    ContingencyPlan = false,
                    IsAnyLivestockImportExport = nutrientsLoadingManuresList.Count > 0,
                    IsAnyLivestockNumber = nutrientsLoadingLiveStockList.Count > 0,
                };
                (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData, error) = await _reportLogic.AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
                    return RedirectToAction("LivestockManureNitrogenReportChecklist");
                }
                if (nutrientsLoadingLiveStockList.Count > 0)
                {
                    return RedirectToAction("ManageLivestock", new { q = model.EncryptedFarmId, y = model.EncryptedHarvestYear });
                }
                else
                {
                    return RedirectToAction("LivestockManureNitrogenReportChecklist");
                }
            }

            return RedirectToAction("LivestockGroup");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in IsAnyLivestockNumber() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnIsAnyLivestockNumber"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> LivestockGroup()
    {
        _logger.LogTrace("Report Controller : LivestockGroup() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, Error error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.LiveStockList = nutrientsLoadingLiveStockList;
            (List<CommonResponse> livestockGroups, error) = await _reportLogic.FetchLivestockGroupList();
            if (error == null)
            {
                ViewBag.LivestockGroups = livestockGroups;
            }
            else
            {
                TempData["ErrorOnIsAnyLivestock"] = error.Message;
                return RedirectToAction("IsAnyLivestockNumber");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockGroup() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnIsAnyLivestockNumber"] = ex.Message;
            return RedirectToAction("IsAnyLivestockNumber");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivestockGroup(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : LivestockGroup() post action called");
        Error error = new Error();
        try
        {
            if (model.LivestockGroupId == null)
            {
                ModelState.AddModelError("LivestockGroupId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (!ModelState.IsValid)
            {
                (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
                ViewBag.LiveStockList = nutrientsLoadingLiveStockList;
                (List<CommonResponse> livestockGroups, error) = await _reportLogic.FetchLivestockGroupList();
                if (error == null)
                {
                    ViewBag.LivestockGroups = livestockGroups;
                }
                return View(model);
            }
            (CommonResponse livestockGroup, error) = await _reportLogic.FetchLivestockGroupById(model.LivestockGroupId ?? 0);
            if (error == null)
            {
                model.LivestockGroupName = livestockGroup.Name;
            }
            else
            {
                TempData["ErrorOnLivestockGroup"] = error.Message;
                return View(model);
            }

            ReportViewModel reportModel = new ReportViewModel();
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                reportModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            if (model.IsLivestockCheckAnswer)
            {
                if (model.LivestockGroupId == reportModel.LivestockGroupId && !model.IsLivestockGroupChange)
                {
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockCheckAnswer");
                }
                else
                {
                    model.IsLivestockGroupChange = true;
                }
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("LivestockType");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockGroup() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockGroup"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> LivestockType()
    {
        _logger.LogTrace("Report Controller : LivestockType() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            if (error == null)
            {
                ViewBag.LivestockTypes = livestockTypes;
            }
            else
            {
                TempData["ErrorOnLivestockGroup"] = error.Message;
                return RedirectToAction("LivestockGroup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockType() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockGroup"] = ex.Message;
            return RedirectToAction("LivestockGroup");

        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivestockType(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : LivestockType() post action called");
        try
        {
            if (model.LivestockTypeId == null)
            {
                ModelState.AddModelError("LivestockTypeId", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            model.LivestockTypeName = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Name;

            if (!ModelState.IsValid)
            {
                if (error == null)
                {
                    ViewBag.LivestockTypes = livestockTypes;
                }
                return View(model);
            }

            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockCheckAnswer");
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            var cattle = (int)NMP.Commons.Enums.LivestockGroup.Cattle;
            var pigs = (int)NMP.Commons.Enums.LivestockGroup.Pigs;
            var poultry = (int)NMP.Commons.Enums.LivestockGroup.Poultry;
            var sheep = (int)NMP.Commons.Enums.LivestockGroup.Sheep;
            var goatsDeerOrHorses = (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses;

            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                model.AverageOccupancy = null;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockNumberQuestion");
            }
            else
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("NonGrazingLivestockAverageNumber");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockType() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockType"] = ex.Message;
            return View(model);
        }
    }
    [HttpGet]
    public async Task<IActionResult> LivestockNumberQuestion()
    {
        _logger.LogTrace("Report Controller : LivestockNumberQuestion() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            if (model.LivestockGroupId == (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = Resource.lblLivestock;
            }
            else
            {
                ViewBag.LivestockCategory = model.LivestockGroupName.ToLower();
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockNumberQuestion() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockType"] = ex.Message;
            return RedirectToAction("LivestockType");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivestockNumberQuestion(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : LivestockNumberQuestion() post action called");
        try
        {
            if (model.LivestockNumberQuestion == null)
            {
                ModelState.AddModelError("LivestockNumberQuestion", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            if (model.LivestockGroupId == (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = Resource.lblLivestock;
            }
            else
            {
                ViewBag.LivestockCategory = model.LivestockGroupName.ToLower();
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ReportViewModel reportModel = new ReportViewModel();
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                reportModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            if (model.IsLivestockCheckAnswer)
            {
                if (model.LivestockNumberQuestion == reportModel.LivestockNumberQuestion && !model.IsLivestockGroupChange)
                {
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                    return RedirectToAction("LivestockCheckAnswer");
                }
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
            {
                return RedirectToAction("LivestockNumbersMonthly");
            }
            else
            {
                return RedirectToAction("AverageNumber");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockNumberQuestion() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockNumberQuestion"] = ex.Message;
            return View(model);
        }
    }
    [HttpGet]
    public async Task<IActionResult> LivestockNumbersMonthly()
    {
        _logger.LogTrace("Report Controller : AverageNumber() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = model.LivestockGroupName;
            }
            else
            {

                string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                        groupName = groupName + "s";
                }
                ViewBag.LivestockCategory = groupName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in AverageNumber() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockNumberQuestion"] = ex.Message;
            return RedirectToAction("LivestockNumberQuestion");

        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivestockNumbersMonthly(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : LivestockNumbersMonthly() post action called");
        try
        {
            if (model.NumbersInJanuary == null &&
                model.NumbersInFebruary == null &&
                model.NumbersInMarch == null &&
                model.NumbersInApril == null &&
                model.NumbersInMay == null &&
                model.NumbersInJune == null &&
                model.NumbersInJuly == null &&
                model.NumbersInAugust == null &&
                model.NumbersInSeptember == null &&
                model.NumbersInOctober == null &&
                model.NumbersInNovember == null &&
                model.NumbersInDecember == null)
            {
                ModelState.AddModelError("NumbersInJanuary", Resource.MsgEnterAtLeastOneValue);
            }
            if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = model.LivestockGroupName;
            }
            else
            {

                string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                        groupName = groupName + "s";
                }
                ViewBag.LivestockCategory = groupName;
            }

            if (!ModelState.IsValid)
            {

                var monthMappings = new Dictionary<string, string>
                {
                    { "NumbersInJanuary", string.Format(Resource.lblTheMonthsOf,Resource.lblJanuary) },
                    { "NumbersInFebruary", string.Format(Resource.lblTheMonthsOf,Resource.lblFebruary) },
                    { "NumbersInMarch", string.Format(Resource.lblTheMonthsOf,Resource.lblMarch) },
                    { "NumbersInApril", string.Format(Resource.lblTheMonthsOf,Resource.lblApril) },
                    { "NumbersInMay", string.Format(Resource.lblTheMonthsOf,Resource.lblMay) },
                    { "NumbersInJune", string.Format(Resource.lblTheMonthsOf,Resource.lblJune) },
                    { "NumbersInJuly", string.Format(Resource.lblTheMonthsOf,Resource.lblJuly) },
                    { "NumbersInAugust", string.Format(Resource.lblTheMonthsOf,Resource.lblAugust) },
                    { "NumbersInSeptember", string.Format(Resource.lblTheMonthsOf,Resource.lblSeptember) },
                    { "NumbersInOctober", string.Format(Resource.lblTheMonthsOf,Resource.lblOctober) },
                    { "NumbersInNovember", string.Format(Resource.lblTheMonthsOf,Resource.lblNovember) },
                    { "NumbersInDecember", string.Format(Resource.lblTheMonthsOf,Resource.lblDecember) }
                };

                foreach (var mapping in monthMappings)
                {
                    if (ModelState.TryGetValue(mapping.Key, out var entry) &&
                        entry.Errors.Count > 0 &&
                        entry.Errors[0].ErrorMessage.Contains(mapping.Key))
                    {
                        entry.Errors[0] = new ModelError(
                            entry.Errors[0].ErrorMessage.Replace(mapping.Key, mapping.Value));
                    }
                }

                return View(model);
            }

            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockCheckAnswer");
            }

            model.AverageNumber = null;
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("LivestockCheckAnswer");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockNumbersMonthly() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockNumbersMonthly"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> AverageNumber()
    {
        _logger.LogTrace("Report Controller : AverageNumber() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = model.LivestockGroupName;
            }
            else
            {

                string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                        groupName = groupName + "s";
                }
                ViewBag.LivestockCategory = groupName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in AverageNumber() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockNumberQuestion"] = ex.Message;
            return RedirectToAction("LivestockNumberQuestion");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AverageNumber(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : AverageNumber() post action called");
        try
        {
            if (model.AverageNumber == null)
            {
                ModelState.AddModelError("AverageNumber", string.Format(Resource.MsgEnterTheAverageNumberOfThisTypeFor, model.Year));
            }
            if (!ModelState.IsValid)
            {
                if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
                {
                    ViewBag.LivestockCategory = model.LivestockGroupName;
                }
                else
                {

                    string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                    if (!string.IsNullOrWhiteSpace(groupName))
                    {
                        if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                            groupName = groupName + "s";
                    }
                    ViewBag.LivestockCategory = groupName;
                }

                return View(model);
            }
            model.NumbersInJanuary = null;
            model.NumbersInFebruary = null;
            model.NumbersInMarch = null;
            model.NumbersInApril = null;
            model.NumbersInMay = null;
            model.NumbersInJune = null;
            model.NumbersInJuly = null;
            model.NumbersInAugust = null;
            model.NumbersInSeptember = null;
            model.NumbersInOctober = null;
            model.NumbersInNovember = null;
            model.NumbersInDecember = null;

            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockCheckAnswer");
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("LivestockCheckAnswer");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in AverageNumber() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnAverageNumber"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> NonGrazingLivestockAverageNumber()  //pig, poultry
    {
        _logger.LogTrace("Report Controller : NonGrazingLivestockAverageNumber() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);

            model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;

            model.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;

            model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in NonGrazingLivestockAverageNumber() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnLivestockType"] = ex.Message;
            return RedirectToAction("LivestockType");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NonGrazingLivestockAverageNumber(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : NonGrazingLivestockAverageNumber() post action called");
        try
        {
            if (model.AverageNumberOfPlaces == null)
            {
                ModelState.AddModelError("AverageNumberOfPlaces", string.Format(Resource.MsgEnterTheAverageTotalNumberOfThis, model.LivestockGroupName, model.Year));
            }

            if (!ModelState.IsValid)
            {
                (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
                model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
                model.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
                model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
                return View(model);
            }
            model.NumbersInJanuary = null;
            model.NumbersInFebruary = null;
            model.NumbersInMarch = null;
            model.NumbersInApril = null;
            model.NumbersInMay = null;
            model.NumbersInJune = null;
            model.NumbersInJuly = null;
            model.NumbersInAugust = null;
            model.NumbersInSeptember = null;
            model.NumbersInOctober = null;
            model.NumbersInNovember = null;
            model.NumbersInDecember = null;

            model.LivestockNumberQuestion = null;
            model.AverageNumber = null;

            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockCheckAnswer");
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("OccupancyAndStandard");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in NonGrazingLivestockAverageNumber() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnNonGrazingLivestockAverageNumber"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> OccupancyAndStandard()  //pig, poultry
    {
        _logger.LogTrace("Report Controller : OccupancyAndStandard() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);

            ViewBag.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;

            ViewBag.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;

            ViewBag.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in OccupancyAndStandard() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnNonGrazingLivestockAverageNumber"] = ex.Message;
            return RedirectToAction("NonGrazingLivestockAverageNumber");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OccupancyAndStandard(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : OccupancyAndStandard() post action called");
        try
        {
            if (model.OccupancyAndNitrogenOptions == null)
            {
                ModelState.AddModelError("OccupancyAndNitrogenOptions", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            decimal? defaultNitrogenStandard = null;
            int? defaultAverageOccupancy = null;
            decimal? defaultPhosphateStandard = null;
            defaultNitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            defaultAverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
            defaultPhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;

            ViewBag.NitrogenStandard = defaultNitrogenStandard;
            ViewBag.AverageOccupancy = defaultAverageOccupancy;
            ViewBag.PhosphateStandard = defaultPhosphateStandard;

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange && model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.UseDefault)
            {
                model.NitrogenStandard = defaultNitrogenStandard;
                model.AverageOccupancy = defaultAverageOccupancy;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                return RedirectToAction("LivestockCheckAnswer");
            }
            if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeOccupancy)
            {
                return RedirectToAction("Occupancy");
            }
            else if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeNitrogen)
            {
                return RedirectToAction("NitrogenStandard");
            }
            else if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth)
            {
                return RedirectToAction("Occupancy");
            }
            else
            {
                model.AverageOccupancy = defaultAverageOccupancy;
                model.NitrogenStandard = defaultNitrogenStandard;
                HttpContext.Session.SetObjectAsJson("ReportData", model);

                return RedirectToAction("LivestockCheckAnswer");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in OccupancyAndStandard() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnOccupancyAndStandard"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Occupancy()  //pig, poultry
    {
        _logger.LogTrace("Report Controller : Occupancy() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);

            if (model.NitrogenStandard == null)
            {
                model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            }
            if (model.AverageOccupancy == null)
            {
                model.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
            }
            if (model.PhosphateStandard == null)
            {
                model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            }
            else
            {
                model.PhosphateStandard = model.PhosphateStandard;
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in Occupancy() action : {ex.Message}, {ex.StackTrace}");

            TempData["ErrorOnOccupancyAndStandard"] = ex.Message;
            return RedirectToAction("OccupancyAndStandard");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Occupancy(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : Occupancy() post action called");
        try
        {
            List<LivestockTypeResponse> livestockTypes = new List<LivestockTypeResponse>();
            Error error = new Error();
            if (model.AverageOccupancy == null)
            {
                ModelState.AddModelError("AverageOccupancy", Resource.MsgEnterTheAverageOccupancy);
            }

            if (!ModelState.IsValid)
            {
                (livestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
                model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
                model.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
                model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
                return View(model);
            }

            //calculation for N standard and P2O5 standard on default occupancy change
            (livestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            var defaultOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
            var defaultNitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            var defaultPhosphate = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            if (model.AverageOccupancy != defaultOccupancy)
            {
                var nitrogenStandardFor100PercentOccupancy = (defaultNitrogenStandard / defaultOccupancy) * 100;
                var phosphateStandardFor100PercentOccupancy = (defaultPhosphate / defaultOccupancy) * 100;
                decimal nitrogen = (nitrogenStandardFor100PercentOccupancy * model.AverageOccupancy) ?? 0;
                model.NitrogenStandard = Math.Round(nitrogen / 100, 6);
                decimal phosphate = (phosphateStandardFor100PercentOccupancy * model.AverageOccupancy) ?? 0;
                model.PhosphateStandard = Math.Round(phosphate / 100, 6);

                if (model.IsGrasslandDerogation == true)
                {
                    model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth;
                }
                else
                {
                    model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeOccupancy;
                }
            }
            //Calculation end

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                return RedirectToAction("LivestockCheckAnswer");
            }
            if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth)
            {
                return RedirectToAction("NitrogenStandard");
            }

            return RedirectToAction("LivestockCheckAnswer");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in Occupancy() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnOccupancy"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> NitrogenStandard()
    {
        _logger.LogTrace("Report Controller : NitrogenStandard() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in NitrogenStandard() action : {ex.Message}, {ex.StackTrace}");

            if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth)
            {
                TempData["ErrorOnOccupancy"] = ex.Message;
                return RedirectToAction("Occupancy");
            }
            if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeNitrogen)
            {
                TempData["ErrorOnNitrogenStandard"] = ex.Message;
                return RedirectToAction("NitrogenStandard");
            }
            TempData["ErrorOnOccupancyAndStandard"] = ex.Message;
            return RedirectToAction("OccupancyAndStandard");

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NitrogenStandard(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : NitrogenStandard() post action called");
        try
        {
            if (model.NitrogenStandard == null)
            {
                ModelState.AddModelError("NitrogenStandard", Resource.MsgEnterTheNitrogenStandardPerAnimal);
            }
            (List<LivestockTypeResponse> livestockTypes, Error error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            if (!ModelState.IsValid)
            {
                model.NitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
                model.AverageOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
                model.PhosphateStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
                return View(model);
            }

            var defaultNitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            if (model.NitrogenStandard != defaultNitrogenStandard)
            {
                if (model.IsGrasslandDerogation == true)
                {
                    model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth;
                }
                else
                {
                    model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeNitrogen;
                }
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (model.IsLivestockCheckAnswer && !model.IsLivestockGroupChange)
            {
                return RedirectToAction("LivestockCheckAnswer");
            }

            return RedirectToAction("LivestockCheckAnswer");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in NitrogenStandard() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnNitrogenStandard"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> LivestockCheckAnswer(string? livestockId)
    {
        _logger.LogTrace("Report Controller : AverageNumber() action called");
        ReportViewModel model = new ReportViewModel();
        Error error = null;
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            var cattle = (int)NMP.Commons.Enums.LivestockGroup.Cattle;
            var pigs = (int)NMP.Commons.Enums.LivestockGroup.Pigs;
            var poultry = (int)NMP.Commons.Enums.LivestockGroup.Poultry;
            var sheep = (int)NMP.Commons.Enums.LivestockGroup.Sheep;
            var goatsDeerOrHorses = (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses;
            if (!string.IsNullOrWhiteSpace(livestockId))
            {
                model.EncryptedNLLivestockID = livestockId;
                int decryptedLivestockId = Convert.ToInt32(_reportDataProtector.Unprotect(livestockId));

                (NutrientsLoadingLiveStock nutrientsLoadingLiveStock, error) = await _reportLogic.FetchNutrientsLoadingLiveStockByIdAsync(decryptedLivestockId);
                if (nutrientsLoadingLiveStock != null)
                {
                    model.FarmId = nutrientsLoadingLiveStock.FarmID;
                    model.Year = nutrientsLoadingLiveStock.CalendarYear;
                    model.LivestockTypeId = nutrientsLoadingLiveStock.LiveStockTypeID;
                    model.AverageNumber = nutrientsLoadingLiveStock.Units;
                    model.NitrogenStandard = nutrientsLoadingLiveStock.NByUnit;
                    model.AverageOccupancy = nutrientsLoadingLiveStock.Occupancy;
                    model.PhosphateStandard = nutrientsLoadingLiveStock.PByUnit;
                    model.NumbersInJanuary = nutrientsLoadingLiveStock.Jan;
                    model.NumbersInFebruary = nutrientsLoadingLiveStock.Feb;
                    model.NumbersInMarch = nutrientsLoadingLiveStock.Mar;
                    model.NumbersInApril = nutrientsLoadingLiveStock.Apr;
                    model.NumbersInMay = nutrientsLoadingLiveStock.May;
                    model.NumbersInJune = nutrientsLoadingLiveStock.June;
                    model.NumbersInJuly = nutrientsLoadingLiveStock.July;
                    model.NumbersInAugust = nutrientsLoadingLiveStock.Aug;
                    model.NumbersInSeptember = nutrientsLoadingLiveStock.Sep;
                    model.NumbersInOctober = nutrientsLoadingLiveStock.Oct;
                    model.NumbersInNovember = nutrientsLoadingLiveStock.Nov;
                    model.NumbersInDecember = nutrientsLoadingLiveStock.Dec;
                }


                (List<LivestockTypeResponse> livestockList, error) = await _reportLogic.FetchLivestockTypes();
                if (livestockList.Count > 0)
                {
                    model.LivestockGroupId = livestockList.Where(x => x.ID == model.LivestockTypeId).Select(x => x.LivestockGroupID).FirstOrDefault();

                }
                if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
                {
                    if (model.NumbersInJanuary == null && model.NumbersInFebruary == null && model.NumbersInMarch == null &&
                    model.NumbersInApril == null && model.NumbersInMay == null && model.NumbersInJune == null &&
                    model.NumbersInJuly == null && model.NumbersInAugust == null && model.NumbersInSeptember == null &&
                    model.NumbersInOctober == null && model.NumbersInNovember == null && model.NumbersInDecember == null)
                    {
                        model.LivestockNumberQuestion = (int)NMP.Commons.Enums.LivestockNumberQuestion.AverageNumberForTheYear;
                    }
                    else
                    {
                        model.LivestockNumberQuestion = (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth;
                    }
                }
                else
                {
                    model.AverageNumberOfPlaces = model.AverageNumber;
                    model.AverageNumber = null;
                    model.LivestockNumberQuestion = null;

                }

                (CommonResponse livestockGroup, error) = await _reportLogic.FetchLivestockGroupById(model.LivestockGroupId ?? 0);
                if (error == null)
                {
                    model.LivestockGroupName = livestockGroup.Name;
                }
                else
                {
                    TempData["ErrorOnManageLivestock"] = error.Message;
                    return View(model);
                }
                (List<LivestockTypeResponse> livestockType, error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
                model.LivestockTypeName = livestockType.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Name;

                HttpContext.Session.SetObjectAsJson("ReportData", model);
            }
            if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
            {
                ViewBag.LivestockCategory = model.LivestockGroupName;
                ViewBag.LivestockCategoryForLivestockNumber = model.LivestockGroupName.ToLower();
            }
            else
            {
                ViewBag.LivestockCategoryForLivestockNumber = Resource.lblLivestock;
                string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                        groupName = groupName + "s";
                }
                ViewBag.LivestockCategory = groupName;
            }
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.LiveStockList = nutrientsLoadingLiveStockList;

            decimal totalNProduced = 0;
            decimal totalPProduced = 0;
            decimal averageNumberForYear = 0;
            if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
            {
                int sumOfEachMonth = (model.NumbersInJanuary ?? 0) + (model.NumbersInFebruary ?? 0) +
                                     (model.NumbersInMarch ?? 0) + (model.NumbersInApril ?? 0) +
                                     (model.NumbersInMay ?? 0) + (model.NumbersInJune ?? 0) +
                                     (model.NumbersInJuly ?? 0) + (model.NumbersInAugust ?? 0) +
                                     (model.NumbersInSeptember ?? 0) + (model.NumbersInOctober ?? 0) +
                                     (model.NumbersInNovember ?? 0) + (model.NumbersInDecember ?? 0);

                averageNumberForYear = (sumOfEachMonth / 12.0m);
            }
            else if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.AverageNumberForTheYear)
            {
                averageNumberForYear = model.AverageNumber ?? 0;
            }
            else
            {
                averageNumberForYear = (model.AverageNumberOfPlaces ?? 0);
            }
            decimal averageNumberForYearRoundOfValue = Math.Round(averageNumberForYear, 1);

            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                totalNProduced = Math.Round(averageNumberForYearRoundOfValue * model.NitrogenStandard ?? 0);
                totalPProduced = Math.Round(averageNumberForYearRoundOfValue * model.PhosphateStandard ?? 0);
            }
            else
            {
                totalNProduced = Math.Round(averageNumberForYearRoundOfValue * (model.NitrogenStandard ?? 0));
                totalPProduced = Math.Round(averageNumberForYearRoundOfValue * model.PhosphateStandard ?? 0);
            }
            ViewBag.TotalNProduced = totalNProduced;
            ViewBag.TotalPProduced = totalPProduced;
            model.IsLivestockCheckAnswer = true;
            model.IsLivestockGroupChange = false;

            (List<LivestockTypeResponse> livestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            var defaultOccupancy = 0;
            if (livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy != null)
            {
                defaultOccupancy = (int)livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.Occupancy;
            }
            var defaultNitrogenStandard = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            var defaultPhosphate = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            if (!string.IsNullOrWhiteSpace(livestockId))
            {
                if (model.AverageOccupancy != defaultOccupancy || model.NitrogenStandard != defaultNitrogenStandard)
                {
                    if (model.IsGrasslandDerogation == true)
                    {
                        model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.DerogatedFarmChangeBoth;
                    }
                    else
                    {
                        if (model.AverageOccupancy != defaultOccupancy)
                        {
                            model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeOccupancy;
                        }
                        else
                        {
                            model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeNitrogen;
                        }
                    }
                }
                else
                {
                    if (model.OccupancyAndNitrogenOptions == null && !string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID))
                    {
                        model.OccupancyAndNitrogenOptions = (int)NMP.Commons.Enums.OccupancyNitrogenOptions.UseDefault;
                    }
                }
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);

            if (!string.IsNullOrWhiteSpace(livestockId))
            {
                HttpContext.Session.SetObjectAsJson("LivestockDataBeforeUpdate", model);
            }
            var previousModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("LivestockDataBeforeUpdate");

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
            _logger.LogTrace($"Report Controller : Exception in AverageNumber() action : {ex.Message}, {ex.StackTrace}");
            var cattle = (int)Enums.LivestockGroup.Cattle;
            var pigs = (int)NMP.Commons.Enums.LivestockGroup.Pigs;
            var poultry = (int)NMP.Commons.Enums.LivestockGroup.Poultry;
            var sheep = (int)NMP.Commons.Enums.LivestockGroup.Sheep;
            var goatsDeerOrHorses = (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses;
            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.AverageNumberForTheYear)
                {
                    TempData["ErrorOnAverageNumber"] = ex.Message;
                    return RedirectToAction("AverageNumber");
                }
                else if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
                {
                    TempData["ErrorOnLivestockNumbersMonthly"] = ex.Message;
                    return RedirectToAction("LivestockNumbersMonthly");
                }
            }
            else
            {
                if (model.IsGrasslandDerogation == false)
                {
                    if (model.OccupancyAndNitrogenOptions == (int)OccupancyNitrogenOptions.ChangeOccupancy)
                    {
                        TempData["ErrorOnOccupancy"] = ex.Message;
                        return RedirectToAction("Occupancy");
                    }
                    else if (model.OccupancyAndNitrogenOptions == (int)OccupancyNitrogenOptions.ChangeNitrogen)
                    {
                        TempData["ErrorOnNitrogenStandard"] = ex.Message;
                        return RedirectToAction("NitrogenStandard");
                    }
                    else if (model.OccupancyAndNitrogenOptions == (int)OccupancyNitrogenOptions.UseDefault)
                    {
                        TempData["ErrorOnOccupancyAndStandard"] = ex.Message;
                        return RedirectToAction("OccupancyAndStandard");
                    }
                }
                else
                {
                    TempData["ErrorOnNitrogenStandard"] = ex.Message;
                    return RedirectToAction("NitrogenStandard");
                }
            }


        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LivestockCheckAnswer(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : LivestockCheckAnswer() post action called");
        Error error = new Error();
        try
        {
            var cattle = (int)NMP.Commons.Enums.LivestockGroup.Cattle;
            var pigs = (int)NMP.Commons.Enums.LivestockGroup.Pigs;
            var poultry = (int)NMP.Commons.Enums.LivestockGroup.Poultry;
            var sheep = (int)NMP.Commons.Enums.LivestockGroup.Sheep;
            var goatsDeerOrHorses = (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses;
            (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year ?? 0);
            ViewBag.LiveStockList = nutrientsLoadingLiveStockList;
            if (model.LivestockGroupId == null)
            {
                ModelState.AddModelError("LivestockGroupId", string.Format(Resource.MsgLivestockGroupNotSet, model.Year));
            }
            if (model.LivestockTypeId == null)
            {
                ModelState.AddModelError("LivestockTypeId", string.Format(Resource.MsgLivestockTypeNotSet, model.Year));
            }
            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                if (model.LivestockNumberQuestion == null)
                {
                    ModelState.AddModelError("LivestockNumberQuestion", Resource.MsgLivestockNumberQuestionNotSet);
                }
                else
                {
                    if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
                    {
                        if (model.NumbersInJanuary == null &&
                            model.NumbersInFebruary == null &&
                            model.NumbersInMarch == null &&
                            model.NumbersInApril == null &&
                            model.NumbersInMay == null &&
                            model.NumbersInJune == null &&
                            model.NumbersInJuly == null &&
                            model.NumbersInAugust == null &&
                            model.NumbersInSeptember == null &&
                            model.NumbersInOctober == null &&
                            model.NumbersInNovember == null &&
                            model.NumbersInDecember == null)
                        {
                            ModelState.AddModelError("NumbersInJanuary", string.Format(Resource.MsgNumbersForEachMonthNotSet, model.LivestockGroupName, Resource.lblJanuary, model.Year));
                        }

                    }
                    else
                    {
                        if (model.AverageNumber == null)
                        {
                            ModelState.AddModelError("AverageNumber", string.Format(Resource.MsgAverageNumberNotSet, model.Year));
                        }
                    }
                }
            }
            if (model.LivestockGroupId == pigs || model.LivestockGroupId == poultry)
            {
                if (model.AverageNumberOfPlaces == null)
                {
                    ModelState.AddModelError("AverageNumberOfPlaces", string.Format(Resource.MsgAverageNumberOfPlacesNotSet, model.Year));
                }
                if (model.AverageOccupancy == null)
                {
                    ModelState.AddModelError("AverageOccupancy", Resource.MsgAverageOccupancyNotSet);
                }
                if (model.NitrogenStandard == null)
                {
                    ModelState.AddModelError("NitrogenStandard", Resource.MsgNitrogenStandardPer1000PlacesNotSet);
                }
            }


            //(List<LivestockTypeResponse> livestockTypes, error) = await _reportService.FetchLivestockTypesByGroupId(model.LivestockGroupId ?? 0);
            //var nitrogen = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.NByUnit;
            //var phosphorus = livestockTypes.FirstOrDefault(x => x.ID == model.LivestockTypeId)?.P2O5;
            //model.NitrogenStandard = nitrogen;
            //ViewBag.Phosphorus = phosphorus;
            //model.PhosphateStandard = phosphorus;

            if (!ModelState.IsValid)
            {
                if (model.LivestockGroupId != (int)Enums.LivestockGroup.GoatsDeerOrHorses)
                {
                    ViewBag.LivestockCategoryForLivestockNumber = model.LivestockGroupName.ToLower();
                    ViewBag.LivestockCategory = model.LivestockGroupName;
                }
                else
                {
                    ViewBag.LivestockCategoryForLivestockNumber = Resource.lblLivestock;
                    string groupName = model.LivestockTypeName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(',').ToLower();
                    if (!string.IsNullOrWhiteSpace(groupName))
                    {
                        if (groupName.Equals(Resource.lblGoat) || groupName.Equals(Resource.lblHorse))
                            groupName = groupName + "s";
                    }
                    ViewBag.LivestockCategory = groupName;
                }
                return View(model);
            }
            decimal totalNProduced = 0;
            decimal totalPProduced = 0;
            decimal averageNumberForYear = 0;
            if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
            {
                int sumOfEachMonth = (model.NumbersInJanuary ?? 0) + (model.NumbersInFebruary ?? 0) +
                                     (model.NumbersInMarch ?? 0) + (model.NumbersInApril ?? 0) +
                                     (model.NumbersInMay ?? 0) + (model.NumbersInJune ?? 0) +
                                     (model.NumbersInJuly ?? 0) + (model.NumbersInAugust ?? 0) +
                                     (model.NumbersInSeptember ?? 0) + (model.NumbersInOctober ?? 0) +
                                     (model.NumbersInNovember ?? 0) + (model.NumbersInDecember ?? 0);

                averageNumberForYear = (sumOfEachMonth / 12.0m);
            }
            else if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.AverageNumberForTheYear)
            {
                averageNumberForYear = model.AverageNumber ?? 0;
            }
            else
            {
                averageNumberForYear = (model.AverageNumberOfPlaces ?? 0);
            }
            decimal averageNumberForYearRoundOfValue = Math.Round(averageNumberForYear, 1);

            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                totalNProduced = Math.Round(averageNumberForYearRoundOfValue * model.NitrogenStandard ?? 0);
                totalPProduced = Math.Round(averageNumberForYearRoundOfValue * model.PhosphateStandard ?? 0);
            }
            else
            {
                totalNProduced = Math.Round(averageNumberForYearRoundOfValue * (model.NitrogenStandard ?? 0));
                totalPProduced = Math.Round(averageNumberForYearRoundOfValue * model.PhosphateStandard ?? 0);
            }


            var nutrientsLoadingLiveStock = new NutrientsLoadingLiveStock()
            {
                ID = !string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID) ? Convert.ToInt32(_reportDataProtector.Unprotect(model.EncryptedNLLivestockID)) : null,
                FarmID = model.FarmId,
                CalendarYear = model.Year,
                LiveStockTypeID = model.LivestockTypeId,
                Units = averageNumberForYear,
                NByUnit = model.NitrogenStandard,
                TotalNProduced = totalNProduced,
                Occupancy = model.AverageOccupancy,
                PByUnit = model.PhosphateStandard,
                TotalPProduced = (int)totalPProduced,
                Jan = model.NumbersInJanuary,
                Feb = model.NumbersInFebruary,
                Mar = model.NumbersInMarch,
                Apr = model.NumbersInApril,
                May = model.NumbersInMay,
                June = model.NumbersInJune,
                July = model.NumbersInJuly,
                Aug = model.NumbersInAugust,
                Sep = model.NumbersInSeptember,
                Oct = model.NumbersInOctober,
                Nov = model.NumbersInNovember,
                Dec = model.NumbersInDecember
            };


            if (string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID))
            {
                (NutrientsLoadingLiveStock nutrientsLoadingLiveStockData, error) = await _reportLogic.AddNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStock);
            }
            else
            {
                (NutrientsLoadingLiveStock nutrientsLoadingLiveStockData, error) = await _reportLogic.UpdateNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStock);
            }
            HttpContext.Session.SetObjectAsJson("StorageCapacityData", model);
            if (!string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["ErrorOnLivestockCheckAnswer"] = error.Message;
                return RedirectToAction("LivestockCheckAnswer");
            }
            else
            {
                //HttpContext?.Session.Remove("ReportData");
                bool success = true;
                string successMsg = string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID) ? Resource.lblYouHaveAddedLivestock : Resource.lblYouHaveUpdatedLivestock;

                var tabId = "";
                if (model.LivestockGroupId == (int)NMP.Commons.Enums.LivestockGroup.Cattle)
                {
                    tabId = "cattle";
                }
                else if (model.LivestockGroupId == (int)NMP.Commons.Enums.LivestockGroup.Pigs)
                {
                    tabId = "pigs";
                }
                else if (model.LivestockGroupId == (int)NMP.Commons.Enums.LivestockGroup.Poultry)
                {
                    tabId = "poultry";
                }
                else if (model.LivestockGroupId == (int)NMP.Commons.Enums.LivestockGroup.Sheep)
                {
                    tabId = "sheep";
                }
                else if (model.LivestockGroupId == (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses)
                {
                    tabId = "goatsDeerAndHorses";
                }

                return RedirectToAction(
                       actionName: "ManageLivestock",
                       controllerName: "Report",
                       routeValues: new
                       {
                           q = model.EncryptedFarmId,
                           y = model.EncryptedHarvestYear,
                           r = _reportDataProtector.Protect(successMsg),
                           s = _reportDataProtector.Protect(success.ToString())
                       },
                       fragment: tabId
                   );

            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in LivestockCheckAnswer() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockCheckAnswer"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ManageLivestock(string q, string y, string r, string s, string? t)
    {
        _logger.LogTrace($"Report Controller : ManageLivestock() action called");
        ReportViewModel model = new ReportViewModel();
        if (HttpContext.Session.Keys.Contains("LivestockDataBeforeUpdate"))
        {
            HttpContext.Session.Remove("LivestockDataBeforeUpdate");
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            if (string.IsNullOrWhiteSpace(model.IsComingFromImportExportOverviewPage))
            {
                model = ResetReportDataFromSession(true);
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                ViewBag.IsManageImportExport = _reportDataProtector.Protect(Resource.lblTrue);
            }
            if (!string.IsNullOrWhiteSpace(model.EncryptedId))
            {
                model.EncryptedId = null;
            }
            model.IsComingFromSuccessMsg = false;
            int decryptedFarmId = Convert.ToInt32(_farmDataProtector.Unprotect(q));
            (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(decryptedFarmId);
            if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
            {
                if (!string.IsNullOrWhiteSpace(r))
                {
                    TempData["succesMsgContent1"] = _reportDataProtector.Unprotect(r);
                    if (!string.IsNullOrWhiteSpace(s) || !string.IsNullOrWhiteSpace(t))
                    {
                        ViewBag.isComingFromSuccessMsg = _reportDataProtector.Protect(Resource.lblTrue);
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            TempData["succesMsgContent2"] = Resource.lblAddAnotherLivestock;
                        }
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            TempData["RemoveSuccessMsg"] = Resource.lblAddMoreLivestock;
                        }
                        TempData["succesMsgContent3"] = string.Format(Resource.lblCreateALivestockManureNitrogenFarmLimitReport, _farmDataProtector.Unprotect(y));
                    }
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
                (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(decryptedFarmId, model.Year ?? 0);

                if (string.IsNullOrWhiteSpace(error.Message))
                {
                    if (nutrientsLoadingLiveStockList != null && nutrientsLoadingLiveStockList.Count > 0)
                    {
                        (List<CommonResponse> livestockGroups, error) = await _reportLogic.FetchLivestockGroupList();
                        if (livestockGroups != null && livestockGroups.Count > 0)
                        {
                            int? cattleLivestockId = livestockGroups.FirstOrDefault(x => x.Id == (int)NMP.Commons.Enums.LivestockGroup.Cattle).Id;

                            int? pigsLivestockId = livestockGroups.FirstOrDefault(x => x.Id == (int)NMP.Commons.Enums.LivestockGroup.Pigs).Id;

                            int? poultryLivestockId = livestockGroups.FirstOrDefault(x => x.Id == (int)NMP.Commons.Enums.LivestockGroup.Poultry).Id;

                            int? sheepLivestockId = livestockGroups.FirstOrDefault(x => x.Id == (int)NMP.Commons.Enums.LivestockGroup.Sheep).Id;

                            int? goatsDeerOrHorsesLivestockId = livestockGroups.FirstOrDefault(x => x.Id == (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses).Id;

                            (List<LivestockTypeResponse> cattleLivestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(cattleLivestockId ?? 0);

                            (List<LivestockTypeResponse> pigsLivestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(pigsLivestockId ?? 0);

                            (List<LivestockTypeResponse> poultryLivestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(poultryLivestockId ?? 0);

                            (List<LivestockTypeResponse> sheepLivestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(sheepLivestockId ?? 0);

                            (List<LivestockTypeResponse> goatsDeerOrHorsesLivestockTypes, error) = await _reportLogic.FetchLivestockTypesByGroupId(goatsDeerOrHorsesLivestockId ?? 0);

                            var cattleTypeDict = cattleLivestockTypes.ToDictionary(x => x.ID);
                            var pigsTypeDict = pigsLivestockTypes.ToDictionary(x => x.ID);
                            var poultryTypeDict = poultryLivestockTypes.ToDictionary(x => x.ID);
                            var sheepTypeDict = sheepLivestockTypes.ToDictionary(x => x.ID);
                            var goatsDeerOrHorsesTypeDict = goatsDeerOrHorsesLivestockTypes.ToDictionary(x => x.ID);


                            ViewBag.CattleList = nutrientsLoadingLiveStockList
                                .Where(x => x.CalendarYear == model.Year && cattleTypeDict.ContainsKey(x.LiveStockTypeID ?? 0))
                                .Select(x => new
                                {
                                    EncryptedID = _reportDataProtector.Protect(x.ID.ToString()),
                                    LivestockTypeName = cattleTypeDict[x.LiveStockTypeID ?? 0].Name,
                                    x.Units,
                                    x.NByUnit,
                                    x.TotalNProduced,
                                    x.PByUnit,
                                    x.TotalPProduced
                                })
                                .ToList();

                            ViewBag.PigsList = nutrientsLoadingLiveStockList
                                .Where(x => x.CalendarYear == model.Year && pigsTypeDict.ContainsKey(x.LiveStockTypeID ?? 0))
                                .Select(x => new
                                {
                                    EncryptedID = _reportDataProtector.Protect(x.ID.ToString()),
                                    LivestockTypeName = pigsTypeDict[x.LiveStockTypeID ?? 0].Name,
                                    x.Units,
                                    x.Occupancy,
                                    x.NByUnit,
                                    x.TotalNProduced,
                                    x.PByUnit,
                                    x.TotalPProduced
                                })
                                .ToList();

                            ViewBag.PoultryList = nutrientsLoadingLiveStockList
                                .Where(x => x.CalendarYear == model.Year && poultryTypeDict.ContainsKey(x.LiveStockTypeID ?? 0))
                                .Select(x => new
                                {
                                    EncryptedID = _reportDataProtector.Protect(x.ID.ToString()),
                                    LivestockTypeName = poultryTypeDict[x.LiveStockTypeID ?? 0].Name,
                                    x.Units,
                                    x.Occupancy,
                                    x.NByUnit,
                                    x.TotalNProduced,
                                    x.PByUnit,
                                    x.TotalPProduced
                                })
                                .ToList();

                            ViewBag.SheepList = nutrientsLoadingLiveStockList
                                .Where(x => x.CalendarYear == model.Year && sheepTypeDict.ContainsKey(x.LiveStockTypeID ?? 0))
                                .Select(x => new
                                {
                                    EncryptedID = _reportDataProtector.Protect(x.ID.ToString()),
                                    LivestockTypeName = sheepTypeDict[x.LiveStockTypeID ?? 0].Name,
                                    x.Units,
                                    x.NByUnit,
                                    x.TotalNProduced,
                                    x.PByUnit,
                                    x.TotalPProduced
                                })
                                .ToList();

                            ViewBag.GoatsDeerAndHorsesList = nutrientsLoadingLiveStockList
                                .Where(x => x.CalendarYear == model.Year && goatsDeerOrHorsesTypeDict.ContainsKey(x.LiveStockTypeID ?? 0))
                                .Select(x => new
                                {
                                    EncryptedID = _reportDataProtector.Protect(x.ID.ToString()),
                                    LivestockTypeName = goatsDeerOrHorsesTypeDict[x.LiveStockTypeID ?? 0].Name,
                                    x.Units,
                                    x.NByUnit,
                                    x.TotalNProduced,
                                    x.PByUnit,
                                    x.TotalPProduced
                                })
                                .ToList();


                        }

                    }
                }
            }
            else
            {
                TempData["Error"] = error.Message;
                return RedirectToAction("FarmSummary", "Farm", new { q = q });
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);
        }
        if (!string.IsNullOrWhiteSpace(y))
        {
            model.Year = Convert.ToInt32(_farmDataProtector.Unprotect(y));
            model.EncryptedHarvestYear = y;
        }

        model.IsManageLivestock = true;
        HttpContext.Session.SetObjectAsJson("ReportData", model);
        return View(model);
    }

    public IActionResult BackLivestockCheckAnswer()
    {
        _logger.LogTrace($"Farm Controller : BackLivestockCheckAnswer() action called");
        ReportViewModel? model = null;
        if (HttpContext.Session.Keys.Contains("ReportData"))
        {
            model = HttpContext?.Session.GetObjectFromJson<ReportViewModel>("ReportData");
        }
        else
        {
            return RedirectToAction("FarmList", "Farm");
        }
        model.IsLivestockCheckAnswer = false;
        HttpContext.Session.SetObjectAsJson("ReportData", model);

        var cattle = (int)NMP.Commons.Enums.LivestockGroup.Cattle;
        var pigs = (int)NMP.Commons.Enums.LivestockGroup.Pigs;
        var poultry = (int)NMP.Commons.Enums.LivestockGroup.Poultry;
        var sheep = (int)NMP.Commons.Enums.LivestockGroup.Sheep;
        var goatsDeerOrHorses = (int)NMP.Commons.Enums.LivestockGroup.GoatsDeerOrHorses;

        if (!string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID))
        {
            return RedirectToAction("ManageLivestock", "Report", new { q = model.EncryptedFarmId, y = _farmDataProtector.Protect(model.Year.Value.ToString()) });
        }
        else
        {
            if (model.LivestockGroupId == cattle || model.LivestockGroupId == sheep || model.LivestockGroupId == goatsDeerOrHorses)
            {
                if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.AverageNumberForTheYear)
                {
                    return RedirectToAction("AverageNumber");
                }
                else if (model.LivestockNumberQuestion == (int)NMP.Commons.Enums.LivestockNumberQuestion.ANumberForEachMonth)
                {
                    return RedirectToAction("LivestockNumbersMonthly");
                }
            }
            else
            {
                if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeOccupancy)
                {
                    return RedirectToAction("Occupancy");
                }
                else if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.ChangeNitrogen)
                {
                    return RedirectToAction("NitrogenStandard");
                }
                else if (model.OccupancyAndNitrogenOptions == (int)NMP.Commons.Enums.OccupancyNitrogenOptions.UseDefault)
                {
                    return RedirectToAction("OccupancyAndStandard");
                }
            }
        }




        return RedirectToAction("AverageNumber");

    }

    [HttpGet]
    public IActionResult Cancel()
    {
        _logger.LogTrace("Report Controller : Cancel() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
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
            if (model.IsLivestockCheckAnswer)
            {
                return RedirectToAction("LivestockCheckAnswer");
            }
            else
            {
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
        }
        else
        {
            if (model.IsLivestockCheckAnswer)
            {
                model.LivestockGroupId = null;
                model.IsAnyLivestockNumber = null;
                model.LivestockTypeId = null;
                model.LivestockNumberQuestion = null;
                model.AverageNumber = null;
                model.NumbersInJanuary = null;
                model.NumbersInFebruary = null;
                model.NumbersInMarch = null;
                model.NumbersInApril = null;
                model.NumbersInMay = null;
                model.NumbersInJune = null;
                model.NumbersInJuly = null;
                model.NumbersInAugust = null;
                model.NumbersInSeptember = null;
                model.NumbersInOctober = null;
                model.NumbersInNovember = null;
                model.NumbersInDecember = null;
                model.AverageNumberOfPlaces = null;
                model.AverageOccupancy = null;
                model.NitrogenStandard = null;
                model.OccupancyAndNitrogenOptions = null;
                model.IsLivestockCheckAnswer = false;
                HttpContext.Session.SetObjectAsJson("ReportData", model);
                if (model.IsManageLivestock)
                {
                    return RedirectToAction("ManageLivestock", "Report", new { q = model.EncryptedFarmId, y = _farmDataProtector.Protect(model.Year.Value.ToString()) });

                }
                else
                {
                    return RedirectToAction("LivestockManureNitrogenReportChecklist", "Report");

                }
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
                model.IsImport = null;
                model.IsCheckAnswer = false;
                model.IsManureTypeChange = false;
                model.IsAnyLivestockImportExport = null;
                model.ManureGroupId = null;
                model.ManureGroupIdForFilter = null;
                model.ManureGroupName = null;
                model.ManureType = new ManureType();
                model.N = null;
                model.NH4N = null;
                model.DryMatterPercent = null;
                model.NO3N = null;
                model.SO3 = null;
                model.K2O = null;
                model.MgO = null;
                model.P2O5 = null;
                model.UricAcid = null;
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
    [HttpGet]
    public async Task<IActionResult> ManureGroup(string? q)
    {
        _logger.LogTrace("Report Controller : ManureGroup() action called");
        ReportViewModel model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }
            (List<CommonResponse> manureGroup, Error error) = await _organicManureLogic.FetchManureGroupList();
            if (error == null)
            {
                ViewBag.ManureGroups = manureGroup.OrderBy(x => x.SortOrder);
            }
            else
            {
                if (model.IsImport == null)
                {
                    TempData["ErrorOnImportExportOption"] = error.Message;
                    return RedirectToAction("ImportExportOption");
                }
                else
                {
                    TempData["ManageImportExportError"] = error.Message;
                    return RedirectToAction("ManageImportExport");
                }
            }
            (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
            if (error == null)
            {
                if (farmManureTypeList.Count > 0)
                {
                    var filteredFarmManureTypes = farmManureTypeList
                    .Where(farmManureType => farmManureType.ManureTypeID == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials ||
                    farmManureType.ManureTypeID == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
                    .ToList();
                    if (filteredFarmManureTypes != null && filteredFarmManureTypes.Count > 0)
                    {
                        var selectListItems = filteredFarmManureTypes.Select(f => new SelectListItem
                        {
                            Value = f.ManureTypeID.ToString(),
                            Text = f.ManureTypeName
                        }).OrderBy(x => x.Text).ToList();
                        ViewBag.FarmManureTypeList = selectListItems;
                    }
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
                        model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Import;
                    }
                    else
                    {
                        model.IsImport = false;
                        model.ImportExport = (int)NMP.Commons.Enums.ImportExport.Export;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in ManureGroup() action : {ex.Message}, {ex.StackTrace}");

            if (model.IsImport == null)
            {
                TempData["ErrorOnImportExportOption"] = ex.Message;
                return RedirectToAction("ImportExportOption");
            }
            else
            {
                TempData["ManageImportExportError"] = ex.Message;
                return RedirectToAction("ManageImportExport");
            }

        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManureGroup(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : ManureGroup() post action called");
        try
        {
            if (model.ManureGroupIdForFilter == null)
            {
                ModelState.AddModelError("ManureGroupIdForFilter", Resource.MsgSelectAnOptionBeforeContinuing);
            }
            Error error = null;
            if (!ModelState.IsValid)
            {
                (List<CommonResponse> manureGroupList, error) = await _organicManureLogic.FetchManureGroupList();
                if (error == null)
                {
                    ViewBag.ManureGroups = manureGroupList.OrderBy(x => x.SortOrder);
                }
                else
                {
                    if (model.IsImport == null)
                    {
                        TempData["ErrorOnImportExportOption"] = error.Message;
                        return RedirectToAction("ImportExportOption");
                    }
                    else
                    {
                        TempData["ManageImportExportError"] = error.Message;
                        return RedirectToAction("ManageImportExport");
                    }
                }
                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                if (error == null)
                {
                    if (farmManureTypeList.Count > 0)
                    {
                        var filteredFarmManureTypes = farmManureTypeList
                        .Where(farmManureType => farmManureType.ManureTypeID == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials ||
                        farmManureType.ManureTypeID == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
                        .ToList();
                        if (filteredFarmManureTypes != null && filteredFarmManureTypes.Count > 0)
                        {
                            var selectListItems = filteredFarmManureTypes.Select(f => new SelectListItem
                            {
                                Value = f.ManureTypeID.ToString(),
                                Text = f.ManureTypeName
                            }).OrderBy(x => x.Text).ToList();
                            ViewBag.FarmManureTypeList = selectListItems;
                        }
                    }
                }
                return View(model);
            }
            if (model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherLiquidMaterials || model.ManureGroupIdForFilter == (int)NMP.Commons.Enums.ManureTypes.OtherSolidMaterials)
            {
                (List<FarmManureTypeResponse> farmManureTypeList, error) = await _organicManureLogic.FetchFarmManureTypeByFarmId(model.FarmId ?? 0);
                if (error == null)
                {
                    if (farmManureTypeList.Count > 0)
                    {
                        (List<CommonResponse> manureGroupList, error) = await _organicManureLogic.FetchManureGroupList();
                        if (error == null)
                        {
                            model.OtherMaterialName = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter)?.ManureTypeName;
                            model.ManureGroupId = manureGroupList.FirstOrDefault(x => x.Name.Equals(Resource.lblOtherOrganicMaterials, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
                            model.ManureTypeId = model.ManureGroupIdForFilter;
                            model.ManureTypeName = farmManureTypeList.FirstOrDefault(x => x.ManureTypeID == model.ManureGroupIdForFilter)?.ManureTypeName;
                            (ManureType manureType, error) = await _organicManureLogic.FetchManureTypeByManureTypeId(model.ManureGroupIdForFilter.Value);
                            if (error == null)
                            {
                                model.IsManureTypeLiquid = manureType.IsLiquid;
                            }
                            ReportViewModel reportViewModel = new ReportViewModel();
                            if (HttpContext.Session.Keys.Contains("ReportData"))
                            {
                                reportViewModel = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
                            }
                            if (reportViewModel != null && reportViewModel.ManureTypeId != null && reportViewModel.ManureTypeId != model.ManureTypeId)
                            {
                                model.IsManureTypeChange = true;
                            }
                            HttpContext.Session.SetObjectAsJson("ReportData", model);

                            return RedirectToAction("LivestockImportExportDate");
                        }

                    }
                }

            }
            else
            {
                model.OtherMaterialName = null;
            }
            (CommonResponse manureGroup, error) = await _organicManureLogic.FetchManureGroupById(model.ManureGroupIdForFilter.Value);
            if (error == null)
            {
                model.ManureGroupName = manureGroup.Name;
            }
            else
            {
                TempData["ErrorOnManureGroup"] = error.Message;
                return View(model);
            }
            HttpContext.Session.SetObjectAsJson("ReportData", model);

            return RedirectToAction("ManureType");
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in ManureGroup() post action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnManureGroup"] = ex.Message;
            return View(model);
        }
    }
    [HttpGet]
    public async Task<IActionResult> BackActionForManureType()
    {
        _logger.LogTrace($"Report Controller : BackActionForManureType() action called");
        ReportViewModel? model = new ReportViewModel();
        if (HttpContext.Session.Keys.Contains("ReportData"))
        {
            model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
        }
        else
        {
            return RedirectToAction("FarmList", "Farm");
        }

        if (model.IsCheckAnswer)
        {
            model.ManureGroupIdForFilter = model.ManureGroupId;
            HttpContext.Session.SetObjectAsJson("ReportData", model);
            (CommonResponse manureGroup, Error error) = await _organicManureLogic.FetchManureGroupById(model.ManureGroupId.Value);
            if (error == null)
            {
                if (manureGroup != null)
                {
                    model.ManureGroupName = manureGroup.Name;
                    HttpContext.Session.SetObjectAsJson("ReportData", model);
                }
            }
            else
            {
                TempData["ErrorOnManureGroup"] = error.Message;
                return View(model);
            }
        }
        if (model.IsImport != null)
        {
            return RedirectToAction("ManageImportExport", new
            {
                q = model.EncryptedFarmId,
                y = _farmDataProtector.Protect(model.Year.ToString())
            });
        }
        else if (model.IsCheckAnswer)
        {
            return RedirectToAction("LivestockImportExportCheckAnswer");
        }
        else
        {
            return RedirectToAction("ImportExportOption");
        }
    }

    [HttpGet]
    public IActionResult OtherMaterialName()
    {
        _logger.LogTrace("Report Controller : OtherMaterialName() action called");
        ReportViewModel? model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in OtherMaterialName() get action : {ex.Message}, {ex.StackTrace}");
            TempData["ManureTypeError"] = ex.Message;
            return RedirectToAction("ManureTypes");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OtherMaterialName(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : OtherMaterialName() post action called");
        try
        {
            if (model.OtherMaterialName == null)
            {
                ModelState.AddModelError("OtherMaterialName", Resource.MsgEnterNameOfTheMaterial);
            }

            (bool farmManureExist, Error error) = await _organicManureLogic.FetchFarmManureTypeCheckByFarmIdAndManureTypeId(model.FarmId.Value, model.ManureTypeId.Value, model.OtherMaterialName);
            if (string.IsNullOrWhiteSpace(error.Message))
            {
                if (farmManureExist)
                {
                    ModelState.AddModelError("OtherMaterialName", Resource.MsgThisManureTypeNameAreadyExist);
                }
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.SetObjectAsJson("ReportData", model);
            if (model.IsCheckAnswer && (!model.IsManureTypeChange))
            {
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in OtherMaterialName() post action : {ex.Message}, {ex.StackTrace}");
            TempData["OtherMaterialNameError"] = ex.Message;
            return View(model);
        }

        return RedirectToAction("LivestockImportExportDate");
    }
    [HttpGet]
    public IActionResult DeleteLivestockImportExport()
    {
        _logger.LogTrace("Report Controller : DeleteLivestockImportExport() action called");
        ReportViewModel? model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in DeleteLivestockImportExport() get action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnCheckYourAnswers"] = ex.Message;
            return RedirectToAction("LivestockImportExportCheckAnswer");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLivestockImportExport(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : DeleteLivestockImportExport() post action called");
        try
        {
            if (model.IsDeleteLivestockImportExport == null)
            {
                ModelState.AddModelError("IsDeleteLivestockImportExport", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (!model.IsDeleteLivestockImportExport.Value)
            {
                return RedirectToAction("LivestockImportExportCheckAnswer");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.EncryptedId))
                {
                    Error error = null;
                    int id = Convert.ToInt32(_reportDataProtector.Unprotect(model.EncryptedId));
                    (string success, error) = await _reportLogic.DeleteNutrientsLoadingManureByIdAsync(id);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["DeleteLivestockImportExportError"] = error.Message;
                        return View(model);
                    }
                    else
                    {
                        string successMsg = _reportDataProtector.Protect(string.Format(Resource.lblYouHaveRemovedImportExport,
                            model.ImportExport == (int)NMP.Commons.Enums.ImportExport.Import ? Resource.lblImport.ToLower() :
                        Resource.lblExport.ToLower()));
                        (List<NutrientsLoadingManures> nutrientsLoadingManureList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.FarmId.Value);
                        if (!string.IsNullOrWhiteSpace(error.Message))
                        {
                            TempData["DeleteLivestockImportExportError"] = error.Message;
                            return View(model);
                        }
                        else if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManureList.Count > 0)
                        {
                            if (nutrientsLoadingManureList.Any(x => x.ManureDate.Value.Year == model.Year))
                            {
                                return RedirectToAction("ManageImportExport", new
                                {
                                    q = model.EncryptedFarmId,
                                    y = _farmDataProtector.Protect(model.Year.ToString()),
                                    r = successMsg
                                });

                            }
                            else if (!model.IsCheckList)
                            {
                                return RedirectToAction("UpdateLivestockImportExport", new
                                {
                                    q = model.EncryptedFarmId,
                                    r = successMsg,
                                });
                            }
                            else
                            {
                                return RedirectToAction("LivestockManureNitrogenReportChecklist", new { r = successMsg });
                            }
                        }
                        else if (model.IsCheckList)
                        {
                            model = ResetReportDataFromSession(false);
                            HttpContext.Session.SetObjectAsJson("ReportData", model);
                            return RedirectToAction("LivestockManureNitrogenReportChecklist", new { r = successMsg });
                        }
                        else
                        {
                            successMsg = _farmDataProtector.Protect(string.Format(Resource.lblYouHaveRemovedImportExport,
                        model.ImportExport == (int)NMP.Commons.Enums.ImportExport.Import ? Resource.lblImport.ToLower() :
                    Resource.lblExport.ToLower()));
                            return RedirectToAction("FarmSummary", "Farm", new
                            {
                                id = model.EncryptedFarmId,
                                q = _farmDataProtector.Protect(Resource.lblTrue),
                                r = successMsg,
                            });
                        }
                    }

                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in DeleteLivestockImportExport() post action : {ex.Message}, {ex.StackTrace}");
            TempData["DeleteLivestockImportExportError"] = ex.Message;
            return View(model);
        }

        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> LivestockManureNFarmLimitReport()
    {
        ReportViewModel model = new ReportViewModel();
        if (HttpContext.Session.Keys.Contains("ReportData"))
        {
            model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
        }
        else
        {
            return RedirectToAction("FarmList", "Farm");
        }

        int totalLivestockManureCapacity = 0;
        (Farm farm, Error error) = await _farmLogic.FetchFarmByIdAsync(model.FarmId.Value);
        if (string.IsNullOrWhiteSpace(error.Message) && farm != null)
        {
            model.Farm = new Farm();
            model.Farm = farm;
        }
        else if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        (NutrientsLoadingFarmDetail nutrientsLoadingFarmDetail, error) = await _reportLogic.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(model.Farm.ID, model.Year.Value);
        if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingFarmDetail != null)
        {
            model.IsGrasslandDerogation = nutrientsLoadingFarmDetail.Derogation.Value;
            if (nutrientsLoadingFarmDetail.Derogation.Value)
            {
                ViewBag.FarmLimitForGrazing = 250;
                ViewBag.FarmLimitForNonGrazing = 170;
            }
            else
            {
                ViewBag.FarmLimit = 170;
                ViewBag.FarmLimitForLandOutsideNVZ = 250;
            }
            if (nutrientsLoadingFarmDetail.LandInNVZ != null && nutrientsLoadingFarmDetail.LandNotNVZ != null
                && nutrientsLoadingFarmDetail.LandInNVZ > 0 && nutrientsLoadingFarmDetail.LandNotNVZ > 0)
            {
                ViewBag.IsAllInNVZ = true;
                totalLivestockManureCapacity = (int)Math.Round((nutrientsLoadingFarmDetail.LandInNVZ.Value * 170) + (nutrientsLoadingFarmDetail.LandNotNVZ.Value * 250), 0);
            }
            else if (nutrientsLoadingFarmDetail.LandInNVZ != null && nutrientsLoadingFarmDetail.LandInNVZ > 0)
            {
                ViewBag.IsAllNotInNVZ = true;
                totalLivestockManureCapacity = (int)Math.Round(nutrientsLoadingFarmDetail.LandInNVZ.Value * 170, 0);
            }
        }
        else if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        ViewBag.TotalLivestockManureCapacity = totalLivestockManureCapacity;
        ViewBag.AreaInsideNVZ = nutrientsLoadingFarmDetail.LandInNVZ;
        ViewBag.AreaOutsideNVZ = nutrientsLoadingFarmDetail.LandNotNVZ;
        (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.Farm.ID, model.Year.Value);
        //if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingLiveStockList.Count > 0)
        if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        ViewBag.LivestockManureTotalNCapacityForNVZ = nutrientsLoadingFarmDetail.LandInNVZ * 170;
        ViewBag.LivestockManureTotalNCapacityForNotInNVZ = nutrientsLoadingFarmDetail.LandNotNVZ * 250;
        ViewBag.LivestockManureTotalNCapacity = (int)Math.Round(((nutrientsLoadingFarmDetail.LandInNVZ.Value * 170) + ((nutrientsLoadingFarmDetail.LandNotNVZ ?? 0) * 250)), 0);
        decimal totalNImportedLivestock = 0;
        decimal totalNExportedLivestock = 0;
        decimal totalImportedGrazingLivestock = 0;
        decimal totalImportedNonGrazingLivestock = 0;
        decimal totalExportedGrazingLivestock = 0;
        decimal totalExportedNonGrazingLivestock = 0;
        decimal totalQuantityImportedLivestock = 0;
        decimal totalQuantityExportedLivestock = 0;

        (List<NutrientsLoadingManures> nutrientsLoadingManureList, error) = await _reportLogic.FetchNutrientsLoadingManuresByFarmId(model.Farm.ID);
        if (string.IsNullOrWhiteSpace(error.Message) && nutrientsLoadingManureList.Count > 0)
        {
            nutrientsLoadingManureList = nutrientsLoadingManureList.Where(x => x.ManureDate.Value.Year == model.Year).ToList();
            (List<ManureType> selectedManureTypes, error) = await _organicManureLogic.FetchManureTypeList((int)NMP.Commons.Enums.ManureGroup.LivestockManure, model.Farm.CountryID.Value);
            if (error == null && selectedManureTypes != null && selectedManureTypes.Count > 0)
            {
                if (nutrientsLoadingManureList.Count > 0)
                {
                    var selectedManureTypeIds = selectedManureTypes.Select(mt => mt.Id).ToList();

                    totalNImportedLivestock = nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.NTotal.HasValue && x.ManureLookupType == Resource.lblImport)
                    .Sum(x => x.NTotal.Value);

                    ViewBag.TotalPImportedLivestock = (int)Math.Round(
                 nutrientsLoadingManureList
                 .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                 && x.PTotal.HasValue && x.ManureLookupType == Resource.lblImport)
                 .Sum(x => x.PTotal.Value), 0);
                    totalImportedGrazingLivestock = nutrientsLoadingManureList
                   .Where(x => !Enum.GetValues(typeof(NonGrazingManureType))
                   .Cast<NonGrazingManureType>()
                   .Select(e => (int)e)
                   .Contains(x.ManureTypeID)
                   && x.NTotal.HasValue && x.ManureLookupType == Resource.lblImport
                   && selectedManureTypeIds.Contains(x.ManureTypeID))
                   .Sum(x => x.NTotal.Value);

                    ViewBag.TotalNImportedGrazingLivestock = totalImportedGrazingLivestock;

                    totalImportedNonGrazingLivestock = nutrientsLoadingManureList
                    .Where(x => Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.NTotal.HasValue
                    && x.ManureLookupType == Resource.lblImport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.NTotal.Value);

                    ViewBag.TotalNImportedNonGrazingLivestock = totalImportedNonGrazingLivestock;
                    ViewBag.TotalPImportedGrazingLivestock = (int)Math.Round(
                    nutrientsLoadingManureList
                   .Where(x => !Enum.GetValues(typeof(NonGrazingManureType))
                   .Cast<NonGrazingManureType>()
                   .Select(e => (int)e)
                   .Contains(x.ManureTypeID)
                   && x.PTotal.HasValue && x.ManureLookupType == Resource.lblImport
                   && selectedManureTypeIds.Contains(x.ManureTypeID))
                   .Sum(x => x.PTotal.Value), 0);

                    ViewBag.TotalPImportedNonGrazingLivestock = (int)Math.Round(
                    nutrientsLoadingManureList
                    .Where(x => Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.PTotal.HasValue
                    && x.ManureLookupType == Resource.lblImport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.PTotal.Value),
                    0);

                    ViewBag.TotalQuantityImportedLivestock = nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.Quantity.HasValue && x.ManureLookupType == Resource.lblImport)
                    .Sum(x => x.Quantity.Value);

                    var allImportData = nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.ManureLookupType?.ToUpper() == Resource.lblImport.ToUpper())
                    .Select(x => new
                    {
                        Manure = x,
                        Unit = (selectedManureTypes.FirstOrDefault(mt => mt.Id.HasValue && mt.Id.Value == x.ManureTypeID)?.IsLiquid ?? false)
                        ? Resource.lblCubicMeters
                        : Resource.lbltonnes
                    })
                    .ToList();

                    if (allImportData != null && allImportData.Count > 0)
                    {
                        ViewBag.ImportOfLivestockManureList = allImportData;
                    }

                    totalNExportedLivestock = nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.NTotal.HasValue && x.ManureLookupType == Resource.lblExport)
                    .Sum(x => x.NTotal.Value);


                    ViewBag.TotalPExportedLivestock = (int)Math.Round(
                   nutrientsLoadingManureList
                   .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                   && x.PTotal.HasValue && x.ManureLookupType == Resource.lblExport)
                   .Sum(x => x.PTotal.Value), 0);

                    ViewBag.TotalQuantityExportedLivestock = nutrientsLoadingManureList
                   .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                   && x.Quantity.HasValue && x.ManureLookupType == Resource.lblExport)
                   .Sum(x => x.Quantity.Value);

                    totalExportedGrazingLivestock = nutrientsLoadingManureList
                    .Where(x => !Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.NTotal.HasValue && x.ManureLookupType == Resource.lblExport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.NTotal.Value);

                    ViewBag.TotalNExportedGrazingLivestock = totalExportedGrazingLivestock;

                    ViewBag.TotalPExportedGrazingLivestock = (int)Math.Round(
                    nutrientsLoadingManureList
                    .Where(x => !Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.PTotal.HasValue
                    && x.ManureLookupType == Resource.lblExport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.PTotal.Value), 0);

                    totalExportedNonGrazingLivestock = nutrientsLoadingManureList
                    .Where(x => Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.NTotal.HasValue
                    && x.ManureLookupType == Resource.lblExport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.NTotal.Value);
                    ViewBag.TotalNExportedNonGrazingLivestock = totalExportedNonGrazingLivestock;

                    ViewBag.TotalPExportedNonGrazingLivestock = (int)Math.Round(
                    nutrientsLoadingManureList
                    .Where(x => Enum.GetValues(typeof(NonGrazingManureType))
                    .Cast<NonGrazingManureType>()
                    .Select(e => (int)e)
                    .Contains(x.ManureTypeID)
                    && x.PTotal.HasValue
                    && x.ManureLookupType == Resource.lblExport
                    && selectedManureTypeIds.Contains(x.ManureTypeID))
                    .Sum(x => x.PTotal.Value), 0);



                    var allExportData = nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.ManureLookupType?.ToUpper() == Resource.lblExport.ToUpper())
                    .Select(x => new
                    {
                        Manure = x,
                        Unit = (selectedManureTypes.FirstOrDefault(mt => mt.Id.HasValue && mt.Id.Value == x.ManureTypeID)?.IsLiquid ?? false)
                        ? Resource.lblCubicMeters
                        : Resource.lbltonnes
                    })
                    .ToList();
                    if (allExportData != null && allExportData.Count > 0)
                    {
                        ViewBag.ExportOfLivestockManureList = allExportData;
                    }
                    nutrientsLoadingManureList
                    .Where(x => selectedManureTypeIds.Contains(x.ManureTypeID)
                    && x.ManureLookupType == Resource.lblExport);
                }
            }
            else if (error != null && !string.IsNullOrWhiteSpace(error.Message))
            {
                TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
                return RedirectToAction("LivestockManureNitrogenReportChecklist");
            }
        }
        else if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }
        int homeProducedLivestockManures = nutrientsLoadingLiveStockList.Count > 0 ? (int)Math.Round(nutrientsLoadingLiveStockList.Sum(x => x.TotalNProduced.Value), 0) : 0;
        int totalNLoading = (int)Math.Round((homeProducedLivestockManures + totalNImportedLivestock) - (totalNExportedLivestock), 0);
        ViewBag.TotalNImportedLivestock = (int)Math.Round(totalNImportedLivestock, 0);
        ViewBag.HomeProducedLivestockManures = homeProducedLivestockManures;
        ViewBag.TotalNExportedLivestock = (int)Math.Round(totalNExportedLivestock, 0);
        int total = (int)Math.Round(totalNImportedLivestock - totalNExportedLivestock, 0);
        ViewBag.TotalImportExportTotalN = total > 0 ? $"+{total}" : total == 0 ? "0" : string.Format("{0:N0}", total);

        ViewBag.TotalNLoading = totalNLoading;
        ViewBag.AverageLivestockManureTotalNLoading = (int)Math.Round(totalNLoading / (nutrientsLoadingFarmDetail.LandInNVZ.Value + (nutrientsLoadingFarmDetail.LandNotNVZ ?? 0)), 0);
        ViewBag.ComplianceOrNot = totalLivestockManureCapacity >= totalNLoading ? Resource.lblCompliance : Resource.lblNonCompliance;


        List<int> grazingLivestockList = new List<int>();
        List<int> nonGrazingLivestockList = new List<int>();
        (List<LivestockTypeResponse> livestockList, error) = await _reportLogic.FetchLivestockTypes();
        if (error == null && livestockList.Count > 0)
        {
            //if farm is Non derogated then need to set VealCalf is grazing 
            if (nutrientsLoadingFarmDetail.Derogation != null && (!nutrientsLoadingFarmDetail.Derogation.Value))
            {
                livestockList.Where(c => c.ID == (int)NMP.Commons.Enums.Livestock.VealCalf).ToList().ForEach(c => c.IsGrazing = true);

            }
            grazingLivestockList = livestockList.Where(mt => mt.IsGrazing.Value).Select(mt => mt.ID).ToList();
            nonGrazingLivestockList = livestockList.Where(mt => !mt.IsGrazing.Value).Select(mt => mt.ID).ToList();
            if (nutrientsLoadingLiveStockList.Count > 0)
            {
                if (nutrientsLoadingLiveStockList.Any(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)))
                {
                    foreach (var nonGrazing in nutrientsLoadingLiveStockList.Where(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)))
                    {
                        decimal? defaultOccupancy = livestockList.Where(x => x.ID == nonGrazing.LiveStockTypeID).Select(x => x.Occupancy).FirstOrDefault();
                        if (defaultOccupancy != null)
                        {
                            decimal defaultNitrogen = livestockList.Where(x => x.ID == nonGrazing.LiveStockTypeID).Select(x => x.NByUnit.Value).FirstOrDefault();
                            nonGrazing.NitrogenStandard = defaultNitrogen * ((nonGrazing.Occupancy ?? 0) / defaultOccupancy);
                        }
                    }
                }
                if (nutrientsLoadingLiveStockList.Any(x => grazingLivestockList.Contains(x.LiveStockTypeID.Value)))
                {
                    ViewBag.NutrientsLoadingLiveStockGrazingList = nutrientsLoadingLiveStockList.Where(x => grazingLivestockList.Contains(x.LiveStockTypeID.Value)).ToList();
                    ViewBag.NutrientsLoadingLiveStockGrazingTotalN = nutrientsLoadingLiveStockList.Where(x => grazingLivestockList.Contains(x.LiveStockTypeID.Value)).Sum(x => x.TotalNProduced);
                    ViewBag.NutrientsLoadingLiveStockGrazingTotalP = nutrientsLoadingLiveStockList.Where(x => grazingLivestockList.Contains(x.LiveStockTypeID.Value)).Sum(x => x.TotalPProduced);
                }
                if (nutrientsLoadingLiveStockList.Any(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)))
                {
                    ViewBag.NutrientsLoadingLiveStockNonGrazingList = nutrientsLoadingLiveStockList.Where(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)).ToList();
                    ViewBag.NutrientsLoadingLiveStockNonGrazingTotalN = nutrientsLoadingLiveStockList.Where(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)).Sum(x => x.TotalNProduced);
                    ViewBag.NutrientsLoadingLiveStockNonGrazingTotalP = nutrientsLoadingLiveStockList.Where(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)).Sum(x => x.TotalPProduced);
                }
            }
        }
        else if (error != null && !string.IsNullOrWhiteSpace(error.Message))
        {
            TempData["ErrorOnLivestockManureNitrogenReportChecklist"] = error.Message;
            return RedirectToAction("LivestockManureNitrogenReportChecklist");
        }

        // for derogation
        if (nutrientsLoadingFarmDetail != null && nutrientsLoadingFarmDetail.Derogation.Value)
        {
            if (grazingLivestockList.Count > 0 || nonGrazingLivestockList.Count > 0)
            {
                decimal areaReqForGrazingLivestock = nutrientsLoadingLiveStockList
                 .Where(x => grazingLivestockList.Contains(x.LiveStockTypeID.Value)
                 && x.TotalNProduced.HasValue)
                 .Sum(x => x.TotalNProduced.Value);
                areaReqForGrazingLivestock += totalImportedGrazingLivestock;
                areaReqForGrazingLivestock -= totalExportedGrazingLivestock;

                decimal areaReqForNonGrazingLivestock = nutrientsLoadingLiveStockList
                 .Where(x => nonGrazingLivestockList.Contains(x.LiveStockTypeID.Value)
                 && x.TotalNProduced.HasValue)
                 .Sum(x => x.TotalNProduced.Value);

                areaReqForNonGrazingLivestock += totalImportedNonGrazingLivestock;
                areaReqForNonGrazingLivestock -= totalExportedNonGrazingLivestock;
                ViewBag.AreaReqForGrazingLivestock = Math.Round(areaReqForGrazingLivestock / 250, 2);
                ViewBag.AreaReqForNonGrazingLivestock = Math.Round(areaReqForNonGrazingLivestock / 170, 2);
                ViewBag.TotalAreaReqForLivestock = (ViewBag.AreaReqForNonGrazingLivestock != null &&
                ViewBag.AreaReqForGrazingLivestock != null) ? Math.Round(ViewBag.AreaReqForGrazingLivestock + ViewBag.AreaReqForNonGrazingLivestock, 2) : 0;
                if (nutrientsLoadingFarmDetail.LandNotNVZ != null && nutrientsLoadingFarmDetail.LandNotNVZ > 0)
                {
                    decimal capacityOfLandOutside = (nutrientsLoadingFarmDetail.LandNotNVZ ?? 0) * 250;
                    if (capacityOfLandOutside > areaReqForNonGrazingLivestock)
                    {
                        ViewBag.AreaReqForNonGrazingLivestock = Math.Round(areaReqForNonGrazingLivestock / 250, 2);
                    }
                    else
                    {
                        ViewBag.AreaReqForNonGrazingLivestock = Math.Round(nutrientsLoadingFarmDetail.LandNotNVZ.Value + (areaReqForNonGrazingLivestock - capacityOfLandOutside) / 170, 2);
                    }

                    ViewBag.TotalAreaReqForLivestock = (ViewBag.AreaReqForNonGrazingLivestock != null &&
                    ViewBag.AreaReqForGrazingLivestock != null) ? Math.Round(ViewBag.AreaReqForGrazingLivestock + ViewBag.AreaReqForNonGrazingLivestock, 2) : 0;
                }
            }
        }
        _logger.LogTrace("Report Controller : CropAndFieldManagement() post action called");
        return View(model);
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

    private static Dictionary<string, int[]> GetNmaxReportCropGroups()
    {
        return new Dictionary<string, int[]>
        {
            { Resource.lblWinterWheat, new [] { (int)Enums.CropTypes.WinterWheat, (int)Enums.CropTypes.WholecropWinterWheat } },

            { Resource.lblSpringWheat, new [] { (int)Enums.CropTypes.SpringWheat, (int)Enums.CropTypes.WholecropSpringWheat } },

            { Resource.lblWinterBarley, new [] { (int)Enums.CropTypes.WinterBarley, (int)Enums.CropTypes.WholecropWinterBarley } },

            { Resource.lblSpringBarley, new [] { (int)Enums.CropTypes.SpringBarley, (int)Enums.CropTypes.WholecropSpringBarley } },

            { Resource.lblPotatoes, new [] { (int)Enums.CropTypes.PotatoVarietyGroup1, (int)Enums.CropTypes.PotatoVarietyGroup2, (int)Enums.CropTypes.PotatoVarietyGroup3, (int)Enums.CropTypes.PotatoVarietyGroup4 } },

            { Resource.lblFieldBeans, new [] { (int)Enums.CropTypes.WinterBeans, (int)Enums.CropTypes.SpringBeans } },

            { Resource.lblPeas, new [] { (int)Enums.CropTypes.Peas, (int)Enums.CropTypes.MarketPickPeas } },

            { Resource.lblGroup1Vegetables, new [] { (int)Enums.CropTypes.Asparagus, (int)Enums.CropTypes.Carrots, (int)Enums.CropTypes.Radish, (int)Enums.CropTypes.Swedes } },

            { Resource.lblGroup2Vegetables, new [] { (int)Enums.CropTypes.CelerySelfBlanching, (int)Enums.CropTypes.Courgettes, (int)Enums.CropTypes.DwarfBeans, (int)Enums.CropTypes.Lettuce, (int)Enums.CropTypes.BulbOnions, (int)Enums.CropTypes.SaladOnions, (int)Enums.CropTypes.Parsnips, (int)Enums.CropTypes.RunnerBeans, (int)Enums.CropTypes.Sweetcorn, (int)Enums.CropTypes.Turnips, (int)Enums.CropTypes.BabyLeafLettuce } },

            { Resource.lblGroup3Vegetables, new [] { (int)Enums.CropTypes.Beetroot, (int)Enums.CropTypes.BrusselSprouts, (int)Enums.CropTypes.Cabbage, (int)Enums.CropTypes.Calabrese, (int)Enums.CropTypes.Cauliflower, (int)Enums.CropTypes.Leeks } }
        };
    }
    string GetGroupName(int cropId)
    {
        var cropGroups = GetNmaxReportCropGroups();
        foreach (var group in cropGroups)
        {
            if (group.Value.Contains(cropId))
                return group.Key;
        }
        return string.Empty; // not in any group
    }
    [HttpGet]
    public IActionResult DeleteNLLivestock()
    {
        _logger.LogTrace("Report Controller : DeleteNLLivestock() action called");
        ReportViewModel? model = new ReportViewModel();
        try
        {
            if (HttpContext.Session.Keys.Contains("ReportData"))
            {
                model = HttpContext.Session.GetObjectFromJson<ReportViewModel>("ReportData");
            }
            else
            {
                return RedirectToAction("FarmList", "Farm");
            }

        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in DeleteNLLivestock() get action : {ex.Message}, {ex.StackTrace}");
            TempData["ErrorOnLivestockCheckAnswer"] = ex.Message;
            return RedirectToAction("LivestockCheckAnswer");
        }
        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNLLivestock(ReportViewModel model)
    {
        _logger.LogTrace("Report Controller : DeleteNLLivestock() post action called");
        try
        {
            if (model.IsDeleteNLLivestock == null)
            {
                ModelState.AddModelError("IsDeleteNLLivestock", Resource.MsgSelectAnOptionBeforeContinuing);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (!model.IsDeleteNLLivestock.Value)
            {
                return RedirectToAction("LivestockCheckAnswer");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.EncryptedNLLivestockID))
                {
                    Error error = null;
                    int id = Convert.ToInt32(_reportDataProtector.Unprotect(model.EncryptedNLLivestockID));
                    (string success, error) = await _reportLogic.DeleteNutrientsLoadingLivestockByIdAsync(id);
                    if (!string.IsNullOrWhiteSpace(error.Message))
                    {
                        TempData["DeleteNLLivestockError"] = error.Message;
                        return View(model);
                    }
                    else
                    {
                        (List<NutrientsLoadingLiveStockViewModel> nutrientsLoadingLiveStockList, error) = await _reportLogic.FetchLivestockByFarmIdAndYear(model.FarmId.Value, model.Year.Value);
                        if (string.IsNullOrWhiteSpace(error.Message))
                        {
                            string successMsg = _reportDataProtector.Protect(string.Format(Resource.lblYouHaveRemovedJourneyName, model.LivestockGroupName));
                            bool Issuccess = true;
                            if (nutrientsLoadingLiveStockList.Count > 0)
                            {

                                return RedirectToAction("ManageLivestock", "Report", new
                                {
                                    q = model.EncryptedFarmId,
                                    y = model.EncryptedHarvestYear,
                                    r = successMsg,
                                    t = _reportDataProtector.Protect(Issuccess.ToString())
                                });
                            }
                            else
                            {
                                model = ResetReportDataFromSession(true);
                                HttpContext.Session.SetObjectAsJson("ReportData", model);
                                return RedirectToAction("LivestockManureNitrogenReportChecklist", "Report", new
                                {
                                    q = _reportDataProtector.Protect(Issuccess.ToString()),
                                    r = successMsg
                                });
                            }
                        }
                        else
                        {
                            TempData["DeleteNLLivestockError"] = error.Message;
                            return View(model);
                        }
                    }


                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Report Controller : Exception in DeleteNLLivestock() post action : {ex.Message}, {ex.StackTrace}");
            TempData["DeleteNLLivestockError"] = ex.Message;
            return View(model);
        }

        return View(model);
    }

    private ReportViewModel ResetReportDataFromSession(bool isLivestock)
    {
        if (!HttpContext.Session.Keys.Contains("ReportData"))
        {
            return null;
        }

        var model = HttpContext.Session
            .GetObjectFromJson<ReportViewModel>("ReportData");

        if (model == null)
        {
            return null;
        }

        if (isLivestock)
        {
            model.EncryptedNLLivestockID = null;
            model.LivestockGroupId = null;
            model.IsAnyLivestockNumber = null;
            model.LivestockTypeId = null;
            model.LivestockNumberQuestion = null;
            model.AverageNumber = null;
            model.NumbersInJanuary = null;
            model.NumbersInFebruary = null;
            model.NumbersInMarch = null;
            model.NumbersInApril = null;
            model.NumbersInMay = null;
            model.NumbersInJune = null;
            model.NumbersInJuly = null;
            model.NumbersInAugust = null;
            model.NumbersInSeptember = null;
            model.NumbersInOctober = null;
            model.NumbersInNovember = null;
            model.NumbersInDecember = null;
            model.AverageNumberOfPlaces = null;
            model.AverageOccupancy = null;
            model.NitrogenStandard = null;
            model.OccupancyAndNitrogenOptions = null;
            model.IsLivestockCheckAnswer = false;
            model.LivestockGroupName = null;
            model.LivestockTypeName = null;
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
            model.IsImport = null;
            model.IsCheckAnswer = false;
            model.IsManureTypeChange = false;
            model.ManureGroupId = null;
            model.ManureGroupIdForFilter = null;
            model.ManureGroupName = null;
            model.IsAnyLivestockImportExport = null;
            model.ManureType = new ManureType();
            model.N = null;
            model.NH4N = null;
            model.DryMatterPercent = null;
            model.NO3N = null;
            model.SO3 = null;
            model.K2O = null;
            model.MgO = null;
            model.P2O5 = null;
            model.UricAcid = null;
        }
        return model;
    }

    private async Task<(NutrientsLoadingFarmDetail? savedNutrientsLoadingFarmDetailsData, Error error)>
    SaveGrasslandDerogationAsync(ReportViewModel model)
    {
        // Fetch livestock
        var (livestockList, livestockError) =
            await _reportLogic.FetchLivestockByFarmIdAndYear(
                model.FarmId!.Value,
                model.Year ?? 0);

        if (!string.IsNullOrWhiteSpace(livestockError?.Message))
        {
            return (null, livestockError);
        }

        // Fetch manures
        var (manures, manureError) =
            await _reportLogic.FetchNutrientsLoadingManuresByFarmId(
                model.FarmId!.Value);

        if (!string.IsNullOrWhiteSpace(manureError?.Message))
        {
            return (null, manureError);
        }

        if (manures?.Any() == true)
        {
            manures = manures
                .Where(x => x.ManureDate?.Year == model.Year)
                .ToList();
        }

        var NutrientsLoadingFarmDetailsData = new NutrientsLoadingFarmDetail
        {
            FarmID = model.FarmId,
            CalendarYear = model.Year,
            LandInNVZ = model.TotalAreaInNVZ,
            LandNotNVZ = model.TotalFarmArea - model.TotalAreaInNVZ,
            TotalFarmed = model.TotalFarmArea,
            Derogation = model.IsGrasslandDerogation,
            GrassPercentage = model.IsGrasslandDerogation == true
                ? model.GrassPercentage
                : null,
            ContingencyPlan = false,
            IsAnyLivestockImportExport = model.IsAnyLivestockImportExport.HasValue
                ? manures?.Any() == true
                : null,
            IsAnyLivestockNumber = model.IsAnyLivestockNumber.HasValue
                ? livestockList?.Any() == true
                : null
        };

        var (savedNutrientsLoadingFarmDetailsData, saveError) =
            await _reportLogic.AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetailsData);

        if (!string.IsNullOrWhiteSpace(saveError?.Message))
        {
            return (null, saveError);
        }

        return (savedNutrientsLoadingFarmDetailsData, new Error());
    }


}