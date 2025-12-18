using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IWarningService
    {
        Task<(List<WarningCodeResponse>, Error)> FetchWarningCodeByFieldIdAndYear(string fieldIds, int harvestYear);
    }
}
