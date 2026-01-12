using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class PreviousCroppingLogic(ILogger<PreviousCroppingLogic> logger, IPreviousCroppingService previousCroppingService) : IPreviousCroppingLogic
{
    private readonly ILogger<PreviousCroppingLogic> _logger = logger;
    private readonly IPreviousCroppingService _previousCroppingService = previousCroppingService;
    public async Task<(List<PreviousCroppingData>, Error)> FetchDataByFieldId(int fieldId, int? year)
    {
        _logger.LogTrace("Fetching previous cropping data for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _previousCroppingService.FetchDataByFieldId(fieldId, year);
    }

    public async Task<(PreviousCropping, Error)> FetchDataByFieldIdAndYear(int fieldId, int year)
    {
        _logger.LogTrace("Fetching previous cropping data for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _previousCroppingService.FetchDataByFieldIdAndYear(fieldId, year);
    }

    public async Task<(int?, Error)> FetchPreviousCroppingYearByFarmdId(int farmId)
    {
        _logger.LogTrace("Fetching previous cropping year for FarmId: {FarmId}", farmId);
        return await _previousCroppingService.FetchPreviousCroppingYearByFarmdId(farmId);
    }

    public async Task<(bool, Error)> MergePreviousCropping(string jsonData)
    {
       _logger.LogTrace("Merging previous cropping data");
        return await _previousCroppingService.MergePreviousCropping(jsonData);
    }
}
