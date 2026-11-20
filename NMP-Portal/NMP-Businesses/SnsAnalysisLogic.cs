using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class SnsAnalysisLogic(ILogger<SnsAnalysisLogic> logger, ISnsAnalysisService snsAnalysisService) : ISnsAnalysisLogic
{
    private readonly ILogger<SnsAnalysisLogic> _logger = logger;
    private readonly ISnsAnalysisService _snsAnalysisService = snsAnalysisService;
    private const string _snsAnalysisSessionKey = "SnsData";  

    public async Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData)
    {
        _logger.LogTrace("SnsAnalysisLogic : AddSnsAnalysisAsync() called");
        return await _snsAnalysisService.AddSnsAnalysisAsync(snsData);
    }

    public async Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId)
    {
        _logger.LogTrace("SnsAnalysisLogic : FetchSnsAnalysisByCropIdAsync() called");
        return await _snsAnalysisService.FetchSnsAnalysisByCropIdAsync(cropId);
    }

    public async Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId)
    {
        _logger.LogTrace("SnsAnalysisLogic : RemoveSnsAnalysisAsync() called");
        return await _snsAnalysisService.RemoveSnsAnalysisAsync(snsAnalysisId);
    }
    public SnsAnalysisViewModel? GetSnsFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _snsAnalysisSessionKey;
        return _snsAnalysisService.GetSnsFromSessionAsync(cacheKey).GetAwaiter().GetResult();
    }

    public void SetSnsToSession(SnsAnalysisViewModel snsAnalysisViewModel)
    {
        string cacheKey = snsAnalysisViewModel.SessionId + " " + _snsAnalysisSessionKey;
        _snsAnalysisService.SetSnsToSessionAsync(snsAnalysisViewModel, cacheKey).GetAwaiter().GetResult();
    }
    public void RemoveSnsSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _snsAnalysisSessionKey;
        _snsAnalysisService.RemoveSnsSessionAsync(cacheKey).GetAwaiter().GetResult();

    }
}
