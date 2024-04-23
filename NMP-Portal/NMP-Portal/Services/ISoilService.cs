using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ISoilService
    {
        Task<(int, Error)> FetchSoilNutrientIndex(int nutrientId, int? nutrientValue, int methodologyId);
    }
}
