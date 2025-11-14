using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IWarningService
    {
        Task<(List<WarningCodeResponse>, Error)> FetchWarningCodeByFieldIdAndYear(string FieldIds, int harvestYear);
    }
}
