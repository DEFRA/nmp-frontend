using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class PKBalanceLogic(ILogger<PKBalanceLogic> logger, IPKBalanceService pKBalanceService) : IPKBalanceLogic
{
    private readonly ILogger<PKBalanceLogic> logger = logger;
    private readonly IPKBalanceService _pkBalanceService = pKBalanceService;
    public async Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year, int fieldId)
    {
        logger.LogTrace("Fetching PK balance by Year and FieldId");
        return await _pkBalanceService.FetchPKBalanceByYearAndFieldId(year, fieldId);
    }
}
