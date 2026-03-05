using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class SoilAnalysisLogic(ILogger<SoilAnalysisLogic> logger, ISoilAnalysisService soilAnalysisService) : ISoilAnalysisLogic
{
    private readonly ILogger<SoilAnalysisLogic> _logger = logger;
    private readonly ISoilAnalysisService _soilAnalysisService = soilAnalysisService;
    public async Task<(SoilAnalysis, Error)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        _logger.LogTrace("Add soil analysis");
        return await _soilAnalysisService.AddSoilAnalysisAsync(soilAnalysisData);
    }

    public async Task<(string, Error)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        _logger.LogTrace("Delete soil analysis by Id");
        return await _soilAnalysisService.DeleteSoilAnalysisByIdAsync(soilAnalysisId);
    }

    public async Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id)
    {
        _logger.LogTrace("Fetching soil analysis by Id");
        return await _soilAnalysisService.FetchSoilAnalysisById(id);
    }

    public async Task<(SoilAnalysis, Error)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        _logger.LogTrace("Update soil analysis");
        return await _soilAnalysisService.UpdateSoilAnalysisAsync(id, soilData);
    }
}
