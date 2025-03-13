using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ISnsAnalysisService:IService
    {
        Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId);
        Task<(SnsAnalysis, Error)> AddSnsAnalysisAsync(SnsAnalysis snsData);
        Task<(string, Error)> RemoveSnsAnalysisAsync(int snsAnalysisId);
    }
}
