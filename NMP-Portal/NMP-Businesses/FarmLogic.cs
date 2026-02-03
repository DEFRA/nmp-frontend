using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FarmLogic(ILogger<FarmLogic> logger, IFarmService farmService, IFieldService fieldService) : IFarmLogic
{
    private readonly ILogger<FarmLogic> _logger = logger;
    private readonly IFarmService _farmService = farmService;
    private readonly IFieldService _fieldService = fieldService;

    public async Task<(ExcessRainfalls, Error)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated)
    {
        _logger.LogTrace("Adding excess winter rainfall for FarmId: {FarmId}, Year: {Year}", farmId, year);
        return await _farmService.AddExcessWinterRainfallAsync(farmId, year, excessWinterRainfallData, isUpdated);
    }

    public async Task<(Farm?, Error?)> AddFarmAsync(FarmData farmData)
    {
        _logger.LogTrace("Adding new farm: {FarmName}", farmData.Farm.Name);
        return await _farmService.AddFarmAsync(farmData);
    }

    public async Task<(string, Error)> DeleteFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("Deleting farm with ID: {FarmId}", farmId);
        return await _farmService.DeleteFarmByIdAsync(farmId);
    }

    public async Task<List<Country>> FetchCountryAsync()
    {
        _logger.LogTrace("Fetching list of countries");
        (List<Country> countryList, Error error) = await _farmService.FetchCountryAsync();
        return countryList.OrderBy(c => c.Name).ToList();

    }

    public async Task<(ExcessRainfalls, Error)> FetchExcessRainfallsAsync(int farmId, int year)
    {
        _logger.LogTrace("Fetching excess rainfalls for FarmId: {FarmId}, Year: {Year}", farmId, year);
        return await _farmService.FetchExcessRainfallsAsync(farmId, year);
    }

    public async Task<(List<CommonResponse>, Error)> FetchExcessWinterRainfallOptionAsync()
    {
        _logger.LogTrace("Fetching excess winter rainfall options");
        return await _farmService.FetchExcessWinterRainfallOptionAsync();
    }

    public async Task<(CommonResponse, Error)> FetchExcessWinterRainfallOptionByIdAsync(int id)
    {
        _logger.LogTrace("Fetching excess winter rainfall option by ID: {Id}", id);
        return await _farmService.FetchExcessWinterRainfallOptionByIdAsync(id);
    }

    public async Task<(FarmResponse, Error)> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching farm with ID: {FarmId}", farmId);
        return await _farmService.FetchFarmByIdAsync(farmId);
    }

    public async Task<(List<Farm>, Error)> FetchFarmByOrgIdAsync(Guid orgId)
    {
        _logger.LogTrace("Fetching farms for Organization ID: {OrgId}", orgId);
        return await _farmService.FetchFarmByOrgIdAsync(orgId);
    }

    public async Task<decimal> FetchRainfallAverageAsync(string postcode)
    {
        _logger.LogTrace("Fetching rainfall average for Postcode: {Postcode}", postcode);
        return await _farmService.FetchRainfallAverageAsync(postcode);
    }

    public async Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id)
    {
        _logger.LogTrace("Checking existence of farm: {FarmName} and Postcode: {Postcode} with ID: {Id}", farmName, postcode, Id);
        return await _farmService.IsFarmExistAsync(farmName, postcode, Id);
    }

    public async Task<(Farm?, Error?)> UpdateFarmAsync(FarmData farmData)
    {
        _logger.LogTrace("Updating farm: {FarmName}", farmData.Farm.Name);
        return await _farmService.UpdateFarmAsync(farmData);
    }

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching field count for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldCountByFarmIdAsync(farmId);
    }
}
