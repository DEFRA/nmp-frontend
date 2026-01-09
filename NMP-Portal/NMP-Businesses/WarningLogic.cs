using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class WarningLogic(ILogger<WarningLogic> logger, IWarningService warningService, IHttpContextAccessor httpContextAccessor) : IWarningLogic
{
    private readonly ILogger<WarningLogic> _logger = logger;
    private readonly IWarningService _warningService = warningService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private const string _warningListSessionKey = "WarningList";
    public async Task<List<WarningHeaderResponse>> FetchWarningHeaderByFieldIdAndYearAsync(string fieldIds, int harvestYear)
    {
        _logger.LogTrace("WarningLogic : FetchWarningHeaderByFieldIdAndYearAsync() called");
        return await _warningService.FetchWarningHeaderByFieldIdAndYear(fieldIds, harvestYear);
    }
    public async Task<WarningResponse> FetchWarningByCountryIdAndWarningKeyAsync(int countryId, string warningKey)
    {
        _logger.LogTrace("WarningLogic : FetchWarningByCountryIdAndWarningKeyAsync() called");
        return await _warningService.FetchWarningByCountryIdAndWarningKeyAsync(countryId, warningKey);
    }

    public async Task<List<WarningResponse>> FetchAllWarningAsync()
    {
        _logger.LogTrace("WarningLogic : FetchAllWarningAsync() called");
        return await _warningService.FetchAllWarningAsync();
    }
}
