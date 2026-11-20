using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface ISnsAnalysisService:IService
{
    Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId);
    Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData);
    Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId);
    Task<SnsAnalysisViewModel?> GetSnsFromSessionAsync(string cacheKey);
    Task SetSnsToSessionAsync(SnsAnalysisViewModel snsAnalysisViewModel, string cacheKey);
    Task RemoveSnsSessionAsync(string cacheKey);
}
