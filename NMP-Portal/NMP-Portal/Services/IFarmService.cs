using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFarmService:IService
    {
        Task<(Farm,Error)> AddFarmAsync(FarmData farmData);
        Task<(Farm, Error)> FetchFarmByIdAsync(int farmId);
        Task<bool> IsFarmExistAsync(string farmName, string postcode);

        Task<decimal> FetchRainfallAverageAsync(string postcode);
    }
}
