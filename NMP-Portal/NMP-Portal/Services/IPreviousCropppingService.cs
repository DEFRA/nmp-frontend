using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IPreviousCropppingService : IService
    {
        Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year);
    }
}
