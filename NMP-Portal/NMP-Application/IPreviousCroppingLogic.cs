using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Application;
public interface IPreviousCroppingLogic
{
    Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
    Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year);
    Task<(bool, Error)> MergePreviousCropping(string jsonData);

    Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId);
    Task<(List<PreviousGrassResponse>?, Error?)> FetchPreviousGrassList();
    Task<(PreviousGrassResponse?, Error?)> FetchPreviousGrassById(int id);
    PreviousCroppingViewModel? GetPreviousCroppingFromSession(Guid? sessionId);
    void SetPreviousCroppingToSession(PreviousCroppingViewModel previousCroppingViewModel);
    void RemovePreviousCroppingSession(Guid? sessionId);
}
