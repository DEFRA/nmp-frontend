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
public class FieldLogic(ILogger<FieldLogic> logger, IFieldService fieldService, ICropService cropService,ISoilService soilService) : IFieldLogic
{
    private readonly ILogger<FieldLogic> _logger = logger;
    private readonly IFieldService _fieldService = fieldService;
    private readonly ICropService _cropService = cropService;
    private readonly ISoilService _soilService = soilService;
    public async Task<(Field?, Error?)> AddFieldAsync(FieldData fieldData, int farmId, string farmName)
    {
        _logger.LogTrace("Adding new field: {FieldName} to FarmId: {FarmId}", fieldData.Field.Name, farmId);
        return await _fieldService.AddFieldServiceAsync(fieldData, farmId, farmName);
    }

    public async Task<(string, Error)> DeleteFieldByIdAsync(int fieldId)
    {
        _logger.LogTrace("Deleting field with ID: {FieldId}", fieldId);
        return await _fieldService.DeleteFieldByIdServiceAsync(fieldId);
    }

    public async Task<List<CropTypeResponse>> FetchAllCropTypes()
    {
        _logger.LogTrace("Fetching all crop types");
        return await _fieldService.FetchAllCropTypesServiceAsync();
    }

    public async Task<List<CropGroupResponse>> FetchArableCropGroups()
    {
        _logger.LogTrace("Fetching arable crop groups");
        var cropGroups = await _fieldService.FetchCropGroupsServiceAsync();
        return [.. cropGroups.Where(x => x.CropGroupId != (int)NMP.Commons.Enums.CropGroup.Grass).OrderBy(x => x.CropGroupName)];
    }

    public async Task<(CropAndFieldReportResponse?, Error?)> FetchCropAndFieldReportById(string fieldId, int year)
    {
        _logger.LogTrace("Fetching crop and field report for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchCropAndFieldReportByIdServiceAsync(fieldId, year);
    }

    public async Task<string> FetchCropGroupById(int cropGroupId)
    {
        _logger.LogTrace("Fetching crop group by ID: {CropGroupId}", cropGroupId);
        return await _fieldService.FetchCropGroupByIdServiceAsync(cropGroupId);
    }

    public async Task<List<CropGroupResponse>> FetchCropGroups()
    {
        _logger.LogTrace("Fetching crop groups");
        return await _fieldService.FetchCropGroupsServiceAsync();
    }

    public async Task<string> FetchCropTypeById(int cropTypeId)
    {
        _logger.LogTrace("Fetching crop type by ID: {CropTypeId}", cropTypeId);
        return await _fieldService.FetchCropTypeByIdServiceAsync(cropTypeId);
    }

    public async Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId, int? farmRB209CountryID)
    {
        _logger.LogTrace("Fetching crop types for CropGroupId: {CropGroupId}", cropGroupId);
        List<CropTypeResponse> cropTypeList = await _fieldService.FetchCropTypesServiceAsync(cropGroupId);
        if (farmRB209CountryID.HasValue)
        {
            cropTypeList = cropTypeList.Where(x => x.CountryId == farmRB209CountryID.Value || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).ToList();
        }
        return cropTypeList;
    }

    public async Task<(Error, List<Field>)> FetchFieldByFarmId(int farmId, string shortSummary)
    {
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldByFarmIdServiceAsync(farmId, shortSummary);
    }

    public async Task<Field> FetchFieldByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetching field by FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldByFieldIdServiceAsync(fieldId);
    }

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching field count for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldCountByFarmIdServiceAsync(farmId);
    }

    public async Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("Fetching field detail for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchFieldDetailByFieldIdAndHarvestYearServiceAsync(fieldId, year, confirm);
    }

    public async Task<List<Field>> FetchFieldsByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldsByFarmIdServiceAsync(farmId);
    }

    public async Task<(FieldResponse?, Error?)> FetchFieldSoilAnalysisAndSnsById(int fieldId)
    {
        _logger.LogTrace("Fetching field soil analysis and SNS for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldSoilAnalysisAndSnsByIdServiceAsync(fieldId);
    }

    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
        _logger.LogTrace("Fetching nutrients");
        return await _fieldService.FetchNutrientsServiceAsync();
    }

    public async Task<List<SeasonResponse>> FetchSeasons()
    {
        _logger.LogTrace("Fetching seasons");
        return await _fieldService.FetchSeasonsServiceAsync();
    }

    public async Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching SNS Category ID for CropTypeId: {CropTypeId}", cropTypeId);
        return await _fieldService.FetchSNSCategoryIdByCropTypeIdServiceAsync(cropTypeId);
    }

    public async Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData)
    {
        _logger.LogTrace("Fetching SNS Index by measurement method");
        return await _fieldService.FetchSNSIndexByMeasurementMethodServiceAsync(measurementData);
    }
    public async Task<(SnsResponseForScotland, Error)> FetchSNSIndexByMeasurementMethodForScotlandAsync(MeasurementDataForScotland measurementDataForScotland)
    {
        _logger.LogTrace("Fetching SNS Index by measurement for scotland method");
        return await _fieldService.FetchSNSIndexByMeasurementMethodForScotlandServiceAsync(measurementDataForScotland);
    }

    public async Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary)
    {
        _logger.LogTrace("Fetching soil analysis for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchSoilAnalysisByFieldIdServiceAsync(fieldId, shortSummary);
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        _logger.LogTrace("Fetching soil type by ID: {SoilTypeId}", soilTypeId);
        return await _fieldService.FetchSoilTypeByIdServiceAsync(soilTypeId);
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        _logger.LogTrace("Fetching soil types");
        return await _fieldService.FetchSoilTypesServiceAsync();
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypesByRB209CountryId(int rb209CountryId)
    {
        _logger.LogTrace("Fetching soil types by RB209 Country Id");
        List<SoilTypesResponse> soilTypes = await _fieldService.FetchSoilTypesServiceAsync();
        return [.. soilTypes.Where(x => x.CountryId == rb209CountryId)];
    }


    public async Task<List<CommonResponse>> GetGrassManagementOptions()
    {
        _logger.LogTrace("Fetching grass management options");
        return await _fieldService.GetGrassManagementOptionsServiceAsync();
    }

    public async Task<List<CommonResponse>> GetGrassTypicalCuts()
    {
        _logger.LogTrace("Fetching grass typical cuts");
        return await _fieldService.GetGrassTypicalCutsServiceAsync();
    }

    public async Task<List<CommonResponse>> GetSoilNitrogenSupplyItems()
    {
        _logger.LogTrace("Fetching soil nitrogen supply items");
        return await _fieldService.GetSoilNitrogenSupplyItemsServiceAsync();
    }

    public async Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId = null)
    {
        _logger.LogTrace("Checking if field exists with Name: {FieldName} in FarmId: {FarmId}", name, farmId);
        return await _fieldService.IsFieldExistServiceAsync(farmId, name, fieldId);
    }

    public async Task<(Field, Error)> UpdateFieldAsync(FieldData field, int fieldId)
    {
        _logger.LogTrace("Updating field with ID: {FieldId}", fieldId);
        return await _fieldService.UpdateFieldServiceAsync(field, fieldId);
    }
    public async Task<(Field?, Error)> UpdateFieldDataAsync(Field field)
    {
        _logger.LogTrace("Updating field : {Field}", field);
        return await _fieldService.UpdateFieldDataServiceAsync(field);
    }
    public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetch crop By field ID: {FieldId}", fieldId);
        return await _cropService.FetchCropsByFieldIdServiceAsync(fieldId);
    }
    public async Task<List<CommonResponse>> FetchPscIndex()
    {
        _logger.LogTrace("Fetch Psc index");
        return await _fieldService.FetchPscIndexServiceAsync();
    }
    public async Task<CommonResponse?> FetchPscIndexById(int id)
    {
        _logger.LogTrace("Fetch Psc index by id");
        return await _fieldService.FetchPscIndexByIdServiceAsync(id);
    }
    public async Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId)
    {
        _logger.LogTrace("Fetch Soil nutrient status list by methodologyId");
        return await _soilService.FetchSoilNutrientStatusList(methodologyId);
    }
}
