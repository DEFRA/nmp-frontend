using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Application;
public interface IPreviousCroppingLogic
{
    Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
    Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year);
    Task<(bool, Error)> MergePreviousCropping(string jsonData);

    Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId);
}
