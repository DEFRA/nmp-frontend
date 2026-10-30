using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class SoilAnalysisLogic(ILogger<SoilAnalysisLogic> logger, ISoilAnalysisService soilAnalysisService) : ISoilAnalysisLogic
{
    private readonly ILogger<SoilAnalysisLogic> _logger = logger;
    private readonly ISoilAnalysisService _soilAnalysisService = soilAnalysisService;
    private const string _soilAnalysisDataBeforeUpdateKey = "SoilAnalysisDataBeforeUpdate";
    private const string _soilAnalysisDataKey = "SoilAnalysisData";
    public async Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData)
    {
        _logger.LogTrace("Add soil analysis");
        return await _soilAnalysisService.AddSoilAnalysisAsync(soilAnalysisData);
    }

    public async Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId)
    {
        _logger.LogTrace("Delete soil analysis by Id");
        return await _soilAnalysisService.DeleteSoilAnalysisByIdAsync(soilAnalysisId);
    }

    public async Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id)
    {
        _logger.LogTrace("Fetching soil analysis by Id");
        (SoilAnalysis soilAnalysis, Error error) = await _soilAnalysisService.FetchSoilAnalysisById(id);
        soilAnalysis.Date = soilAnalysis.Date.Value.ToLocalTime();
        return (soilAnalysis,error);
    }

    public async Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData)
    {
        _logger.LogTrace("Update soil analysis");
        return await _soilAnalysisService.UpdateSoilAnalysisAsync(id, soilData);
    }

    public SoilAnalysisViewModel? GetSoilAnalysisFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _soilAnalysisDataKey;
        return _soilAnalysisService.LoadSoilAnalysisDataFromSession(cacheKey).GetAwaiter().GetResult();
    }

    public void SetSoilAnalysisDataToSession(SoilAnalysisViewModel soilAnalysis)
    {
        string cacheKey = soilAnalysis.SessionId + " " + _soilAnalysisDataKey;
        _soilAnalysisService.SetSoilAnalysisDataToSession(soilAnalysis, cacheKey).GetAwaiter().GetResult();
    }
    public void RemoveSoilAnalysisDataFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _soilAnalysisDataKey;
        _soilAnalysisService.RemoveSoilAnalysisDataFromSession(cacheKey).GetAwaiter().GetResult();

    }
    public SoilAnalysisViewModel? GetSoilAnalysisBeforeUpdateFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _soilAnalysisDataBeforeUpdateKey;
        return _soilAnalysisService.LoadSoilAnalysisDataFromSession(cacheKey).GetAwaiter().GetResult();
    }

    public void SetSoilAnalysisBeforeUpdateToSession(SoilAnalysisViewModel soilAnalysis)
    {
        string cacheKey = soilAnalysis.SessionId + " " + _soilAnalysisDataBeforeUpdateKey;
        _soilAnalysisService.SetSoilAnalysisDataToSession(soilAnalysis, cacheKey).GetAwaiter().GetResult();
    }
    public void RemoveSoilAnalysisBeforeUpdateFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _soilAnalysisDataBeforeUpdateKey;
        _soilAnalysisService.RemoveSoilAnalysisDataFromSession(cacheKey).GetAwaiter().GetResult();

    }
}
