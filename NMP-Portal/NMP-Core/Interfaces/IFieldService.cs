using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IFieldService : IService
{
    Task<int> FetchFieldCountByFarmIdAsync(int farmId);
    Task<List<SoilTypesResponse>> FetchSoilTypes();
    Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync();
    Task<List<CropGroupResponse>> FetchCropGroups();
    Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId);
    Task<string> FetchCropGroupById(int cropGroupId);
    Task<string> FetchCropTypeById(int cropTypeId);
    Task<(Field?, Error?)> AddFieldAsync(FieldData fieldData, int farmId,string farmName);
    Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId=null);
    Task<List<Field>> FetchFieldsByFarmId(int farmId);
    Task<Field> FetchFieldByFieldId(int fieldId);
    Task<List<CropTypeResponse>> FetchAllCropTypes();
    Task<string> FetchSoilTypeById(int soilTypeId);
    Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary);

    Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm);
    Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId);
    Task<List<SeasonResponse>> FetchSeasons();

    Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData);
    Task<(Field, Error)> UpdateFieldAsync(FieldData fieldData, int fieldId);
    Task<(string, Error)> DeleteFieldByIdAsync(int fieldId);
    Task<List<CommonResponse>> GetGrassManagementOptions();
    Task<List<CommonResponse>> GetGrassTypicalCuts();
    Task<List<CommonResponse>> GetSoilNitrogenSupplyItems();
    Task<(Error, List<Field>)> FetchFieldByFarmId(int farmId, string shortSummary);
    Task<(FieldResponse, Error)> FetchFieldSoilAnalysisAndSnsById(int fieldId);
    Task<(CropAndFieldReportResponse, Error)> FetchCropAndFieldReportById(string fieldId,int year);
    Task<(Field?, Error)> UpdateFieldDataAsync(Field field);
}
