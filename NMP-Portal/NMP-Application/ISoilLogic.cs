using NMP.Commons.ServiceResponses;
namespace NMP.Application;

public interface ISoilLogic
{
    Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId);
    Task<string> FetchSoilTypeById(int soilTypeId);
}
