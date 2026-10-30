using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface ISoilAnalysisService
{
    Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id);
    Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData);
    Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData);
    Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId);
    Task<SoilAnalysisViewModel?> LoadSoilAnalysisDataFromSession(string cacheKey);
    Task SetSoilAnalysisDataToSession(SoilAnalysisViewModel soilAnalysis, string cacheKey);
    Task RemoveSoilAnalysisDataFromSession(string cacheKey);
}