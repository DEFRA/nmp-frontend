using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FarmLogic(ILogger<FarmLogic> logger, IFarmService farmService, IFieldService fieldService, IFarmAverageYieldLogic farmAverageYieldLogic, IRb209Service rb209Service, IAddressLookupLogic addressLookupLogic) : IFarmLogic
{
    private readonly ILogger<FarmLogic> _logger = logger;
    private readonly IFarmService _farmService = farmService;
    private readonly IFieldService _fieldService = fieldService;
    private readonly IFarmAverageYieldLogic _farmAverageYieldLogic = farmAverageYieldLogic;
    private readonly IRb209Service _rb209Service = rb209Service;
    private readonly IAddressLookupLogic _addressLookupLogic = addressLookupLogic;
    private const string _farmDataKey = "FarmData";
    private const string _addressListKey = "AddressList";
    private const string _farmDataBeforeUpdateSessionKey = "FarmDataBeforeUpdate";

    public async Task<(ExcessRainfalls?, Error?)> AddExcessWinterRainfallAsync(int farmId, int year, string excessWinterRainfallData, bool isUpdated)
    {
        _logger.LogTrace("Adding excess winter rainfall for FarmId: {FarmId}, Year: {Year}", farmId, year);
        return await _farmService.AddExcessWinterRainfallAsync(farmId, year, excessWinterRainfallData, isUpdated);
    }

    public async Task<(Farm?, Error?)> AddFarmAsync(FarmData farmData, Guid orgId)
    {
        _logger.LogTrace("Adding new farm: {FarmName}", farmData.Farm.Name);
        return await _farmService.AddFarmAsync(farmData, orgId);
    }

    public async Task<(string, Error)> DeleteFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("Deleting farm with ID: {FarmId}", farmId);
        return await _farmService.DeleteFarmByIdAsync(farmId);
    }

    public async Task<List<Country>> FetchCountryAsync()
    {
        _logger.LogTrace("Fetching list of countries");
        (List<Country> countryList, _) = await _farmService.FetchCountryAsync();
        return countryList;
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

    public async Task<(FarmResponse?, Error?)> FetchFarmByIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching farm with ID: {FarmId}", farmId);
        return await _farmService.FetchFarmByIdAsync(farmId);
    }

    public async Task<(List<Farm>, Error?)> FetchFarmByOrgIdAsync(Guid orgId)
    {
        _logger.LogTrace("Fetching farms for Organization ID: {OrgId}", orgId);
        return await _farmService.FetchFarmByOrgIdAsync(orgId);
    }

    public async Task<decimal> FetchRainfallAverageAsync(string postcode)
    {
        _logger.LogTrace("Fetching rainfall average for Postcode: {Postcode}", postcode);
        return await _farmService.FetchRainfallAverageAsync(postcode);
    }

    public async Task<bool> IsFarmExistAsync(string farmName, string postcode, int Id, Guid orgId)
    {
        _logger.LogTrace("Checking existence of farm: {FarmName} and Postcode: {Postcode} with ID: {Id}", farmName, postcode, Id);
        return await _farmService.IsFarmExistAsync(farmName, postcode, Id, orgId);
    }

    public async Task<(Farm?, Error?)> UpdateFarmAsync(FarmData farmData, Guid orgId)
    {
        _logger.LogTrace("Updating farm: {FarmName}", farmData.Farm.Name);
        return await _farmService.UpdateFarmAsync(farmData, orgId);
    }

    public async Task<int> FetchFieldCountByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching field count for FarmId: {FarmId}", farmId);
        return await _fieldService.FetchFieldCountByFarmIdAsync(farmId);
    }
    public async Task<List<NvzActionProgramResponse>> FetchNvzActionProgramsByCountryIdAsync(int countryId)
    {
        _logger.LogTrace("Fetching Nvz action programs for CountryId: {CountryId}", countryId);
        return await _rb209Service.FetchNvzActionProgramsByCountryIdAsync(countryId);
    }
    public async Task<(FarmAndFarmsNvzResponse?, Error?)> FetchFarmAndFarmsNvzByFarmIdAsync(int farmId)
    {
        _logger.LogTrace("Fetching farm and farms nvz with ID: {FarmId}", farmId);
        return await _farmService.FetchFarmAndFarmsNvzByFarmIdAsync(farmId);
    }

    public async Task<(List<FarmAverageYields>?, Error?)> FetchFarmAverageYieldByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        _logger.LogTrace("Fetch farm average yield by FarmID: {FarmId} and HarvestYear: {HarvestYear}", farmId, harvestYear);
        return await _farmAverageYieldLogic.FetchFarmAverageYieldByFarmIdAndHarvestYear(farmId, harvestYear);
    }

    public async Task<(List<FarmAverageYields>?, Error?)> AddFarmAverageYieldsAsync(List<FarmAverageYields> farmAverageYieldData)
    {
        _logger.LogTrace("Add farm average yields");
        return await _farmAverageYieldLogic.AddFarmAverageYieldsAsync(farmAverageYieldData);
    }

    public async Task<(List<FarmListSummary>, Error?)> FetchAllFarmsWithLastUpdatedDateByOrgIdAsync(Guid orgId)
    {
        _logger.LogTrace("Fetching farms with last updated date for Organization ID: {OrgId}", orgId);
        return await _farmService.FetchAllFarmsWithLastUpdatedDateByOrgIdAsync(orgId);
    }
    public FarmViewModel? GetFarmFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _farmDataKey;
        return _farmService.GetFarmFromSessionAsync(cacheKey).GetAwaiter().GetResult();
    }

    public void SetFarmToSession(FarmViewModel farm)
    {
        string cacheKey = farm.SessionId + " " + _farmDataKey;
        _farmService.SetFarmToSessionAsync(farm, cacheKey).GetAwaiter().GetResult();
    }
    public void RemoveFarmSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _farmDataKey;
        _farmService.RemoveFarmSessionAsync(cacheKey).GetAwaiter().GetResult();

    }
    public FarmViewModel? GetFarmDataBeforeUpdateFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _farmDataBeforeUpdateSessionKey;
        return _farmService.GetFarmFromSessionAsync(cacheKey).GetAwaiter().GetResult();
    }

    public void SetFarmDataBeforeUpdateToSession(FarmViewModel farm)
    {
        string cacheKey = farm.SessionId + " " + _farmDataBeforeUpdateSessionKey;
        _farmService.SetFarmToSessionAsync(farm, cacheKey).GetAwaiter().GetResult();
    }
    public void RemoveFarmDataBeforeUpdateFromSession(Guid? sessionId)
    {
        string cacheKey = sessionId + " " + _farmDataBeforeUpdateSessionKey;
        _farmService.RemoveFarmSessionAsync(cacheKey).GetAwaiter().GetResult();

    }
    public void RemoveNmptSession(string cacheKey)
    {
        _farmService.RemoveNmptSessionAsync(cacheKey).GetAwaiter().GetResult();
    }

}
