using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FieldLogic(ILogger<FieldLogic> logger, IFieldService fieldService,IRb209Service rb209Service, ICropService cropService) : IFieldLogic
{
    private readonly ILogger<FieldLogic> _logger = logger;
    private readonly IFieldService _fieldService = fieldService;
    private readonly IRb209Service _rb209Service = rb209Service;
    private readonly ICropService _cropService = cropService;
    public async Task<(Field?, Error?)> AddFieldAsync(FieldData fieldData, int farmId, string farmName)
    {
        _logger.LogTrace("Adding new field: {FieldName} to FarmId: {FarmId}", fieldData.Field.Name, farmId);
        return await _fieldService.AddFieldAsync(fieldData, farmId, farmName);
    }

    public async Task<(string, Error)> DeleteFieldByIdAsync(int fieldId)
    {
        _logger.LogTrace("Deleting field with ID: {FieldId}", fieldId);
        return await _fieldService.DeleteFieldByIdAsync(fieldId);
    }

    public async Task<List<CropTypeResponse>> FetchAllCropTypes()
    {
        _logger.LogTrace("Fetching all crop types");
        return await _rb209Service.FetchAllCropTypesAsync();
    }

    public async Task<List<CropGroupResponse>> FetchArableCropGroups()
    {
        _logger.LogTrace("Fetching arable crop groups");
        var cropGroups = await _rb209Service.FetchCropGroupsAsync();
        return [.. cropGroups.Where(x => x.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName)];
    }

    public async Task<(CropAndFieldReportResponse?, Error?)> FetchCropAndFieldReportById(string fieldId, int year)
    {
        _logger.LogTrace("Fetching crop and field report for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchCropAndFieldReportByIdAsync(fieldId, year);
    }

    public async Task<string> FetchCropGroupById(int cropGroupId)
    {
        _logger.LogTrace("Fetching crop group by ID: {CropGroupId}", cropGroupId);
        return await _rb209Service.FetchCropGroupByIdAsync(cropGroupId);
    }

    public async Task<List<CropGroupResponse>> FetchCropGroups()
    {
        _logger.LogTrace("Fetching crop groups");
        return await _rb209Service.FetchCropGroupsAsync();
    }

    public async Task<string> FetchCropTypeById(int cropTypeId)
    {
        _logger.LogTrace("Fetching crop type by ID: {CropTypeId}", cropTypeId);
        return await _rb209Service.FetchCropTypeByIdAsync(cropTypeId);
    }

    public async Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId, int? farmRB209CountryID)
    {
        _logger.LogTrace("Fetching crop types for CropGroupId: {CropGroupId}", cropGroupId);
        List<CropTypeResponse> cropTypeList = await _rb209Service.FetchCropTypesAsync(cropGroupId);
        if (farmRB209CountryID.HasValue)
        {
            cropTypeList = cropTypeList.Where(x => x.CountryId == farmRB209CountryID.Value || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).ToList();
        }
        return cropTypeList;
    }

    public async Task<(Error, List<Field>)> FetchFieldByFarmId(int farmId, string shortSummary)
    {
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldByFarmIdAsync(farmId, shortSummary);
    }

    public async Task<Field> FetchFieldByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetching field by FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldByFieldIdAsync(fieldId);
    }

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching field count for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldCountByFarmIdAsync(farmId);
    }

    public async Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("Fetching field detail for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchFieldDetailByFieldIdAndHarvestYearAsync(fieldId, year, confirm);
    }

    public async Task<List<Field>> FetchFieldsByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldsByFarmIdAsync(farmId);
    }

    public async Task<(FieldResponse?, Error?)> FetchFieldSoilAnalysisAndSnsById(int fieldId)
    {
        _logger.LogTrace("Fetching field soil analysis and SNS for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldSoilAnalysisAndSnsByIdAsync(fieldId);
    }

    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
        _logger.LogTrace("Fetching nutrients");
        return await _rb209Service.FetchNutrientsAsync();
    }

    public async Task<List<SeasonResponse>> FetchSeasons()
    {
        _logger.LogTrace("Fetching seasons");
        return await _rb209Service.FetchSeasonsAsync();
    }

    public async Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching SNS Category ID for CropTypeId: {CropTypeId}", cropTypeId);
        return await _fieldService.FetchSNSCategoryIdByCropTypeIdAsync(cropTypeId);
    }

    public async Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData)
    {
        _logger.LogTrace("Fetching SNS Index by measurement method");
        return await _rb209Service.FetchSNSIndexByMeasurementMethodAsync(measurementData);
    }
    public async Task<(SnsResponseForScotland, Error)> FetchSNSIndexByMeasurementMethodForScotlandAsync(MeasurementDataForScotland measurementDataForScotland)
    {
        _logger.LogTrace("Fetching SNS Index by measurement for scotland method");
        return await _rb209Service.FetchSNSIndexByMeasurementMethodForScotlandAsync(measurementDataForScotland);
    }

    public async Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary)
    {
        _logger.LogTrace("Fetching soil analysis for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchSoilAnalysisByFieldIdAsync(fieldId, shortSummary);
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        _logger.LogTrace("Fetching soil type by ID: {SoilTypeId}", soilTypeId);
        return await _rb209Service.FetchSoilTypeByIdAsync(soilTypeId);
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        _logger.LogTrace("Fetching soil types");
        return await _rb209Service.FetchSoilTypesAsync();
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypesByRB209CountryId(int rb209CountryId)
    {
        _logger.LogTrace("Fetching soil types by RB209 Country Id");
        List<SoilTypesResponse> soilTypes = await _rb209Service.FetchSoilTypesAsync();
        return [.. soilTypes.Where(x => x.CountryId == rb209CountryId)];
    }


    public async Task<List<CommonResponse>> GetGrassManagementOptions()
    {
        _logger.LogTrace("Fetching grass management options");
        return await _fieldService.GetGrassManagementOptionsAsync();
    }

    public async Task<List<CommonResponse>> GetGrassTypicalCuts()
    {
        _logger.LogTrace("Fetching grass typical cuts");
        return await _fieldService.GetGrassTypicalCutsAsync();
    }

    public async Task<List<CommonResponse>> GetSoilNitrogenSupplyItems()
    {
        _logger.LogTrace("Fetching soil nitrogen supply items");
        return await _fieldService.GetSoilNitrogenSupplyItemsAsync();
    }

    public async Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId = null)
    {
        _logger.LogTrace("Checking if field exists with Name: {FieldName} in FarmId: {FarmId}", name, farmId);
        return await _fieldService.IsFieldExistAsync(farmId, name, fieldId);
    }

    public async Task<(Field, Error)> UpdateFieldAsync(FieldData field, int fieldId)
    {
        _logger.LogTrace("Updating field with ID: {FieldId}", fieldId);
        return await _fieldService.UpdateFieldAsync(field, fieldId);
    }
    public async Task<(Field?, Error)> UpdateFieldDataAsync(Field field)
    {
        _logger.LogTrace("Updating field : {Field}", field);
        return await _fieldService.UpdateFieldDataAsync(field);
    }
    public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetch crop By field ID: {FieldId}", fieldId);
        return await _cropService.FetchCropsByFieldIdAsync(fieldId);
    }
    public async Task<List<CommonResponse>> FetchPscIndex()
    {
        _logger.LogTrace("Fetch Psc index");
        return await _fieldService.FetchPscIndexAsync();
    }
    public async Task<CommonResponse?> FetchPscIndexById(int id)
    {
        _logger.LogTrace("Fetch Psc index by id");
        return await _fieldService.FetchPscIndexByIdAsync(id);
    }
    public async Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId)
    {
        _logger.LogTrace("Fetch Soil nutrient status list by methodologyId");
        return await _rb209Service.FetchSoilNutrientStatusList(methodologyId);
    }
}
