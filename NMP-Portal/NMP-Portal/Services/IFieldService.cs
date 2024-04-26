using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFieldService : IService
    {
        Task<int> FetchFieldCountByFarmIdAsync(int farmId);
        Task<List<SoilTypesResponse>> FetchSoilTypes();
        Task<List<FieldResponseWapper>> FetchNutrientsAsync();
        Task<List<CropGroupResponse>> FetchCropGroups();
        Task<List<CropTypeResponse>> FetchCropTypes(int cropGroupId);
        Task<(Field, Error)> AddFieldAsync(FieldData fieldData, int farmId,string farmName);
        Task<bool> IsFieldExistAsync(int fieldId, string name);
    }
}
