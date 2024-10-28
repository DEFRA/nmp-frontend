using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFieldService : IService
    {
        Task<int> FetchFieldCountByFarmIdAsync(int farmId);
        Task<List<SoilTypesResponse>> FetchSoilTypes();
        Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync();
        Task<List<CropGroupResponse>> FetchCropGroups();
        Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId);
        Task<string> FetchCropGroupById(int cropGroupId);
        Task<string> FetchCropTypeById(int cropTypeId);
        Task<(Field, Error)> AddFieldAsync(FieldData fieldData, int farmId,string farmName);
        Task<bool> IsFieldExistAsync(int farmId, string name);
        Task<List<Field>> FetchFieldsByFarmId(int farmId);
        Task<Field> FetchFieldByFieldId(int fieldId);
        Task<List<CropTypeResponse>> FetchAllCropTypes();
        Task<string> FetchSoilTypeById(int soilTypeId);
        Task<List<SoilAnalysisResponse>> FetchSoilAnalysisByFieldId(int fieldId, string shortSummary);

        Task<(FieldDetailResponse, Error)> FetchFieldDetailByFieldIdAndHarvestYear(int fieldId, int year, bool confirm);
        Task<int> FetchSNSCategoryIdByCropTypeId(int cropTypeId);
        Task<List<SeasonResponse>> FetchSeasons();

        Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData);
    }
}
