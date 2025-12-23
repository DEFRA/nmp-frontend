using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Application;

public interface IFarmLogic
{
    Task<(List<Farm>, Error)> FetchFarmByOrgIdAsync(Guid orgId);
    Task<(Farm, Error)> AddFarmAsync(FarmData farmData);
    Task<(Farm, Error)> FetchFarmByIdAsync(int farmId);
    Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id);

    Task<decimal> FetchRainfallAverageAsync(string postcode);
    Task<(Farm, Error)> UpdateFarmAsync(FarmData farmData);
    Task<(string, Error)> DeleteFarmByIdAsync(int farmId);
    Task<(List<Country>, Error)> FetchCountryAsync();
    Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year);
    Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync();
    Task<(ExcessRainfalls, Error)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated);
    Task<(CommonResponse, Error)> FetchExcessWinterRainfallOptionByIdAsync(int id);
}
