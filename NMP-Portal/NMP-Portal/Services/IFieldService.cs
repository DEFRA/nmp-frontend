using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFieldService : IService
    {
        Task<int> FetchFieldCountByFarmIdAsync(int farmId);
        Task<List<SoilTypesResponse>> FetchSoilTypes();
        Task<List<NutrientResponseWrapper>> FetchNutrientsAsync();
        Task<List<CropGroupResponse>> FetchCropGroups();
        Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId);
        Task<string> FetchCropGroupById(int cropGroupId);
        Task<string> FetchCropTypeById(int cropTypeId);
        Task<(Field, Error)> AddFieldAsync(FieldData fieldData, int farmId,string farmName);
        Task<bool> IsFieldExistAsync(int farmId, string name);
        Task<List<Field>> FetchFieldsByFarmId(int farmId);
    }
}
