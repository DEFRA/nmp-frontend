using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Application;
public interface ISnsAnalysisLogic
{
    Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId);
    Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData);
    Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId);
}
