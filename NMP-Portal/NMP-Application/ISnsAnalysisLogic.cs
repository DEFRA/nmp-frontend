using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;

namespace NMP.Application;
public interface ISnsAnalysisLogic
{
    Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId);
    Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData);
    Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId);
    SnsAnalysisViewModel? GetSnsFromSession(Guid? sessionId);
    void SetSnsToSession(SnsAnalysisViewModel snsAnalysisViewModel);
    void RemoveSnsSession(Guid? sessionId);
}
