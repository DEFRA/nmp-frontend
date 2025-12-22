using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IWarningService
{
    Task<(List<WarningHeaderResponse>, Error)> FetchWarningHeaderByFieldIdAndYear(string fieldIds, int harvestYear);
}
