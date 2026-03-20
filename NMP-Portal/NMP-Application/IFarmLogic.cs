using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Application;

public interface IFarmLogic
{
    Task<(List<Farm>, Error?)> FetchFarmByOrgIdAsync(Guid orgId);
    Task<(Farm?, Error?)> AddFarmAsync(FarmData farmData, Guid orgId);
    Task<(FarmResponse?, Error?)> FetchFarmByIdAsync(int farmId);
    Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id, Guid orgId);

    Task<decimal> FetchRainfallAverageAsync(string postcode);
    Task<(Farm?, Error?)> UpdateFarmAsync(FarmData farmData, Guid orgId);
    Task<(string, Error)> DeleteFarmByIdAsync(int farmId);
    Task<List<Country>> FetchCountryAsync();
    Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year);
    Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync();
    Task<(ExcessRainfalls?, Error?)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated);
    Task<(CommonResponse, Error)> FetchExcessWinterRainfallOptionByIdAsync(int id);
    Task<int> FetchFieldCountByFarmIdAsync(int farmId);
    Task<List<NvzActionProgramResponse>> FetchNvzActionProgramsByCountryIdAsync(int countryId);
    Task<(FarmAndFarmsNvzResponse?, Error?)> FetchFarmAndFarmsNvzByFarmIdAsync(int farmId);
}
