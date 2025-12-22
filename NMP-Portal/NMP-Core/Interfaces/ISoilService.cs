using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface ISoilService
{
    Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId);
    Task<string> FetchSoilTypeById(int soilTypeId);        
}
