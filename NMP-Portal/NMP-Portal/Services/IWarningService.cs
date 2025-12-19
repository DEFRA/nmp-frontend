using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IWarningService
    {
        Task<(List<WarningHeaderResponse>, Error)> FetchWarningHeaderByFieldIdAndYear(string fieldIds, int harvestYear);
    }
}
