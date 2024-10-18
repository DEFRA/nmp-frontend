using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ISoilAnalysisService
    {
        Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id);
    }
}
