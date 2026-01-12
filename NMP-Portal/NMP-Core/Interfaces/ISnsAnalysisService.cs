using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface ISnsAnalysisService:IService
{
    Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId);
    Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData);
    Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId);
}
