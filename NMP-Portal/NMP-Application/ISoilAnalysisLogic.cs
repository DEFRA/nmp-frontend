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

    void SetSoilAnalysisDataToSession(SoilAnalysisViewModel  field);
    void RemoveSoilAnalysisDataFromSession(Guid? sessionId);

    SoilAnalysisViewModel? GetSoilAnalysisBeforeUpdateFromSession(Guid? sessionId);

    void SetSoilAnalysisBeforeUpdateToSession(SoilAnalysisViewModel field);
    void RemoveSoilAnalysisBeforeUpdateFromSession(Guid? sessionId);
}
