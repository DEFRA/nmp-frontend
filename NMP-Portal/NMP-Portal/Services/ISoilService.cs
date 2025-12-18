using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ISoilService
    {
        Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId);
        Task<string> FetchSoilTypeById(int soilTypeId);        
    }
}
