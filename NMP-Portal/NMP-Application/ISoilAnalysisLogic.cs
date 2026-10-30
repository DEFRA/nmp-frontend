using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface ISoilAnalysisLogic
{
    Task<(SoilAnalysis?, Error?)> FetchSoilAnalysisById(int id);
    Task<(SoilAnalysis?, Error?)> UpdateSoilAnalysisAsync(int id, string soilData);
    Task<(SoilAnalysis?, Error?)> AddSoilAnalysisAsync(string soilAnalysisData);
    Task<(string, Error?)> DeleteSoilAnalysisByIdAsync(int soilAnalysisId);
    SoilAnalysisViewModel? GetSoilAnalysisFromSession(Guid? sessionId);

    void SetSoilAnalysisDataToSession(SoilAnalysisViewModel soilAnalysis);
    void RemoveSoilAnalysisDataFromSession(Guid? sessionId);

    SoilAnalysisViewModel? GetSoilAnalysisBeforeUpdateFromSession(Guid? sessionId);

    void SetSoilAnalysisBeforeUpdateToSession(SoilAnalysisViewModel soilAnalysis);
    void RemoveSoilAnalysisBeforeUpdateFromSession(Guid? sessionId);
}
