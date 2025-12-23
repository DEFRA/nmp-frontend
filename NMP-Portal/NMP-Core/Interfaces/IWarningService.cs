using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IWarningService
{
    Task<List<WarningHeaderResponse>> FetchWarningHeaderByFieldIdAndYear(string fieldIds, int harvestYear);
    Task<WarningResponse> FetchWarningByCountryIdAndWarningKeyAsync(int countryId, string warningKey);
}
