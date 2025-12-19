using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IPreviousCroppingService : IService
{
    Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
    Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year);
    Task<(bool, Error)> MergePreviousCropping(string jsonData);

    Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId);
}
