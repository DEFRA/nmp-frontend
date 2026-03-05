using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Application;

public interface ISoilAnalysisLogic
{
    Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id);
    Task<(SoilAnalysis, Error)> UpdateSoilAnalysisAsync(int id, string soilData);
    Task<(SoilAnalysis, Error)> AddSoilAnalysisAsync(string soilAnalysisData);
    Task<(string, Error)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId);
}
