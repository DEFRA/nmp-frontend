using NMP.Commons.ServiceResponses;
namespace NMP.Application;

public interface ISoilLogic
{
    Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId);
    Task<string> FetchSoilTypeById(int soilTypeId);
    Task<(List<SoilMethologiesResponse>?, Error?)> FetchSoilMethodologies(int nutrientId, int countryId);
    Task<(SoilMethologiesResponse?, Error?)> FetchSoilMethodologyNameByNutrientIdAndMethodologyId(int nutrientId, int methodologyId);
    Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId);
}
