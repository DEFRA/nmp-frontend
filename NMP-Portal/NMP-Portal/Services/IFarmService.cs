using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFarmService:IService
    {
        Task<(List<Farm>, Error)> FetchFarmByOrgIdAsync(Guid orgId);
        Task<(Farm,Error)> AddFarmAsync(FarmData farmData);
        Task<(Farm, Error)> FetchFarmByIdAsync(int farmId);
        Task<bool> IsFarmExistAsync(string farmName, string postcode,int Id);

        Task<decimal> FetchRainfallAverageAsync(string postcode);
        Task<(Farm, Error)> UpdateFarmAsync(FarmData farmData);
        Task<(string, Error)> DeleteFarmByIdAsync(int farmId);
        Task<(List<Country>, Error)> FetchCountryAsync();
        Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year);
    }
}
