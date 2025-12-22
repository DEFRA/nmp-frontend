using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface ISoilAnalysisService
{
    Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id);
    Task<(SoilAnalysis, Error)> UpdateSoilAnalysisAsync(int id, string soilData);
    Task<(SoilAnalysis, Error)> AddSoilAnalysisAsync(string soilAnalysisData);
    Task<(string, Error)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId);
}