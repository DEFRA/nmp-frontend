using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IPreviousCroppingService : IService
    {
        Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
        Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year);
        Task<(bool, Error)> MergePreviousCropping(string jsonData);
    }
}
