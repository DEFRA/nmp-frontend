using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Collections.Generic;
using System.Reflection;
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
        List<WarningResponse> warningList = await FetchAllWarningAsync();
        if (warningList != null && warningList.Count > 0)
        {
            WarningResponse? warning = warningList.FirstOrDefault(x => x.CountryID == countryId
                            && string.Equals(x.WarningKey.Trim(), warningKey, StringComparison.OrdinalIgnoreCase));
            if (warning != null)
            {
                return warning;
            }
        }
        return await _warningService.FetchWarningByCountryIdAndWarningKeyAsync(countryId, warningKey);
    }

    public async Task<List<WarningResponse>> FetchAllWarningAsync()
    {
        _logger.LogTrace("WarningLogic : FetchAllWarningAsync() called");

        var session = _httpContextAccessor.HttpContext?.Session;
        var warningList = session?.GetObjectFromJson<List<WarningResponse>>(_warningListSessionKey);

        if (warningList != null && warningList.Count > 0)
            return warningList;

        warningList = await _warningService.FetchAllWarningAsync();
        session?.SetObjectAsJson(_warningListSessionKey, warningList);

        return warningList;
    }

}
