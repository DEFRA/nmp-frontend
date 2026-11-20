using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IPreviousCroppingService : IService
{
    Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
    Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year);
    Task<(bool, Error)> MergePreviousCropping(string jsonData);

    Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId);
    Task<(List<PreviousGrassResponse>?, Error?)> FetchPreviousGrassListAsync();
    Task<(PreviousGrassResponse?, Error?)> FetchPreviousGrassByIdAsync(int id);
    Task<PreviousCroppingViewModel?> GetPreviousCroppingFromSessionAsync(string cacheKey);
    Task SetPreviousCroppingToSessionAsync(PreviousCroppingViewModel previousCroppingViewModel, string cacheKey);
    Task RemovePreviousCroppingSessionAsync(string cacheKey);
}
