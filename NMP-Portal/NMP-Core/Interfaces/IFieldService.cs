using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IFieldService : IService
{
    Task<int> FetchFieldCountByFarmIdServiceAsync(int farmId);
    Task<List<SoilTypesResponse>> FetchSoilTypesServiceAsync();
    Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsServiceAsync();
    Task<List<CropGroupResponse>> FetchCropGroupsServiceAsync();
    Task<List<CropTypeResponse>> FetchCropTypesServiceAsync(int cropGroupId);
    Task<string> FetchCropGroupByIdServiceAsync(int cropGroupId);
    Task<string> FetchCropTypeByIdServiceAsync(int cropTypeId);
    Task<(Field?, Error?)> AddFieldServiceAsync(FieldData fieldData, int farmId,string farmName);
    Task<bool> IsFieldExistServiceAsync(int farmId, string name, int? fieldId=null);
    Task<List<Field>> FetchFieldsByFarmIdServiceAsync(int farmId);
    Task<Field> FetchFieldByFieldIdServiceAsync(int fieldId);
    Task<List<CropTypeResponse>> FetchAllCropTypesServiceAsync();
    Task<string> FetchSoilTypeByIdServiceAsync(int soilTypeId);
    Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldIdServiceAsync(int fieldId, string shortSummary);

    Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYearServiceAsync(int fieldId, int year, bool confirm);
    Task<int> FetchSNSCategoryIdByCropTypeIdServiceAsync(int cropTypeId);
    Task<List<SeasonResponse>> FetchSeasonsServiceAsync();

    Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodServiceAsync(MeasurementData measurementData);
    Task<(SnsResponseForScotland, Error)> FetchSNSIndexByMeasurementMethodForScotlandServiceAsync(MeasurementDataForScotland measurementDataForScotland);
    Task<(Field, Error)> UpdateFieldServiceAsync(FieldData fieldData, int fieldId);
    Task<(string, Error)> DeleteFieldByIdServiceAsync(int fieldId);
    Task<List<CommonResponse>> GetGrassManagementOptionsServiceAsync();
    Task<List<CommonResponse>> GetGrassTypicalCutsServiceAsync();
    Task<List<CommonResponse>> GetSoilNitrogenSupplyItemsServiceAsync();
    Task<(Error, List<Field>)> FetchFieldByFarmIdServiceAsync(int farmId, string shortSummary);
    Task<(FieldResponse?, Error?)> FetchFieldSoilAnalysisAndSnsByIdServiceAsync(int fieldId);
    Task<(CropAndFieldReportResponse?, Error?)> FetchCropAndFieldReportByIdServiceAsync(string fieldId,int year);
    Task<(Field?, Error)> UpdateFieldDataServiceAsync(Field field);
    Task<List<CommonResponse>> FetchPscIndexServiceAsync();
    Task<CommonResponse?> FetchPscIndexByIdServiceAsync(int id);
}
