using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IFieldService : IService
{
    Task<int> FetchFieldCountByFarmIdAsync(int farmId);
    
    Task<(Field?, Error?)> AddFieldAsync(FieldData fieldData, int farmId,string farmName);
    Task<bool> IsFieldExistAsync(int farmId, string name, int? fieldId=null);
    Task<List<Field>> FetchFieldsByFarmIdAsync(int farmId);
    Task<Field> FetchFieldByFieldIdAsync(int fieldId);
    
    Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldIdAsync(int fieldId, string shortSummary);

    Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYearAsync(int fieldId, int year, bool confirm);
    Task<int> FetchSNSCategoryIdByCropTypeIdAsync(int cropTypeId);
   
    Task<(Field, Error)> UpdateFieldAsync(FieldData fieldData, int fieldId);
    Task<(string, Error)> DeleteFieldByIdAsync(int fieldId);
    Task<List<CommonResponse>> GetGrassManagementOptionsAsync();
    Task<List<CommonResponse>> GetGrassTypicalCutsAsync();
    Task<List<CommonResponse>> GetSoilNitrogenSupplyItemsAsync();
    Task<(Error, List<Field>)> FetchFieldByFarmIdAsync(int farmId, string shortSummary);
    Task<(FieldResponse?, Error?)> FetchFieldSoilAnalysisAndSnsByIdAsync(int fieldId);
    Task<(CropAndFieldReportResponse?, Error?)> FetchCropAndFieldReportByIdAsync(string fieldId,int year);
    Task<(Field?, Error)> UpdateFieldDataAsync(Field field);
    Task<List<CommonResponse>> FetchPscIndexAsync();
    Task<CommonResponse?> FetchPscIndexByIdAsync(int id);
}
