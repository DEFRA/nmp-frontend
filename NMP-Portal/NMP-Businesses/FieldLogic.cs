using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Interfaces;
namespace NMP.Businesses;
public class FieldLogic(ILogger<FieldLogic> logger, IFieldService fieldService) : IFieldLogic
{
    private readonly ILogger<FieldLogic> _logger = logger;
    private readonly IFieldService _fieldService = fieldService;
    public async Task<(Field, Error)> AddFieldAsync(FieldData fieldData, int farmId, string farmName)
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
        return await _fieldService.FetchAllCropTypes();
    }

    public async Task<(CropAndFieldReportResponse, Error)> FetchCropAndFieldReportById(string fieldId, int year)
    {
        _logger.LogTrace("Fetching crop and field report for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchCropAndFieldReportById(fieldId, year);
    }

    public async Task<string> FetchCropGroupById(int cropGroupId)
    {
        _logger.LogTrace("Fetching crop group by ID: {CropGroupId}", cropGroupId);
        return await _fieldService.FetchCropGroupById(cropGroupId);
    }

    public async Task<List<CropGroupResponse>> FetchCropGroups()
    {
        _logger.LogTrace("Fetching crop groups");
        return await _fieldService.FetchCropGroups();
    }

    public async Task<string> FetchCropTypeById(int cropTypeId)
    {
        _logger.LogTrace("Fetching crop type by ID: {CropTypeId}", cropTypeId);
        return await _fieldService.FetchCropTypeById(cropTypeId);
    }

    public async Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId)
    {
       _logger.LogTrace("Fetching crop types for CropGroupId: {CropGroupId}", cropGroupId);
        return await _fieldService.FetchCropTypes(cropGroupId);
    }

    public async Task<(Error, List<Field>)> FetchFieldByFarmId(int farmId, string shortSummary)
    {
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldByFarmId(farmId, shortSummary);
    }

    public async Task<Field> FetchFieldByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetching field by FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldByFieldId(fieldId);
    }

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching field count for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldCountByFarmIdAsync(farmId);
    }

    public async Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm)
    {
        _logger.LogTrace("Fetching field detail for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _fieldService.FetchFieldDetailByFieldIdAndHarvestYear(fieldId, year, confirm);
    }

    public async Task<List<Field>> FetchFieldsByFarmId(int farmId)
    {            
        _logger.LogTrace("Fetching fields for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldsByFarmId(farmId);
    }

    public async Task<(FieldResponse, Error)> FetchFieldSoilAnalysisAndSnsById(int fieldId)
    {
        _logger.LogTrace("Fetching field soil analysis and SNS for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchFieldSoilAnalysisAndSnsById(fieldId);
    }

    public async Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync()
    {
        _logger.LogTrace("Fetching nutrients");
        return await _fieldService.FetchNutrientsAsync();
    }

    public async Task<List<SeasonResponse>> FetchSeasons()
    {
       _logger.LogTrace("Fetching seasons");
        return await _fieldService.FetchSeasons();
    }

    public async Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching SNS Category ID for CropTypeId: {CropTypeId}", cropTypeId);
        return await _fieldService.FetchSNSCategoryIdByCropTypeId(cropTypeId);
    }

    public async Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData)
    {
        _logger.LogTrace("Fetching SNS Index by measurement method");
        return await _fieldService.FetchSNSIndexByMeasurementMethodAsync(measurementData);
    }

    public async Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary)
    {
        _logger.LogTrace("Fetching soil analysis for FieldId: {FieldId}", fieldId);
        return await _fieldService.FetchSoilAnalysisByFieldId(fieldId, shortSummary);
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        _logger.LogTrace("Fetching soil type by ID: {SoilTypeId}", soilTypeId);
        return await _fieldService.FetchSoilTypeById(soilTypeId);
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        _logger.LogTrace("Fetching soil types");
        return await _fieldService.FetchSoilTypes();
    }

    public async Task<List<CommonResponse>> GetGrassManagementOptions()
    {
        _logger.LogTrace("Fetching grass management options");
        return await _fieldService.GetGrassManagementOptions();
    }

    public async Task<List<CommonResponse>> GetGrassTypicalCuts()
    {
        _logger.LogTrace("Fetching grass typical cuts");
        return await _fieldService.GetGrassTypicalCuts();
    }

    public async Task<List<CommonResponse>> GetSoilNitrogenSupplyItems()
    {
        _logger.LogTrace("Fetching soil nitrogen supply items");
        return await _fieldService.GetSoilNitrogenSupplyItems();
    }

    public async Task<bool> IsFieldExistAsync(int farmId, string name)
    {
        _logger.LogTrace("Checking if field exists with Name: {FieldName} in FarmId: {FarmId}", name, farmId);
        return await _fieldService.IsFieldExistAsync(farmId, name);
    }

    public async Task<(Field, Error)> UpdateFieldAsync(FieldData field, int fieldId)
    {
        _logger.LogTrace("Updating field with ID: {FieldId}", fieldId);
        return await _fieldService.UpdateFieldAsync(field, fieldId);
    }
}
