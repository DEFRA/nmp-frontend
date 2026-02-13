using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Core.Interfaces;
public interface IMannerService
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode);
    Task<List<SoilTypesResponse>> FetchSoilTypes();
    Task<Country?> FetchCountryById(int id);
}
