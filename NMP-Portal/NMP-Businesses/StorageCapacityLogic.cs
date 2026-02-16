using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class StorageCapacityLogic(ILogger<StorageCapacityLogic> logger, IStorageCapacityService storageCapacityService) : IStorageCapacityLogic
{
    private readonly ILogger<StorageCapacityLogic> _logger = logger;
    private readonly IStorageCapacityService _storageCapacityService = storageCapacityService;
    public async Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        _logger.LogTrace("Add store capacity");
        return await _storageCapacityService.AddStoreCapacityAsync(storeCapacityData);
    }

    public async Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData)
    {
        _logger.LogTrace("Copy existing storage capacity");
        return await _storageCapacityService.CopyExistingStorageCapacity(copyStorageManureCapacityData);
    }

    public async Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id)
    {
        _logger.LogTrace("Fetching bank slope angle by Id");
        return await _storageCapacityService.FetchBankSlopeAngleById(id);
    }

    public async Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles()
    {
        _logger.LogTrace("Fetching bank slop angles");
        return await _storageCapacityService.FetchBankSlopeAngles();
    }

    public async Task<(CommonResponse, Error)> FetchMaterialStateById(int id)
    {
        _logger.LogTrace("Fetching material state by Id");
        return await _storageCapacityService.FetchMaterialStateById(id);
    }

    public async Task<(List<CommonResponse>, Error)> FetchMaterialStates()
    {
        _logger.LogTrace("Fetching material states");
        return await _storageCapacityService.FetchMaterialStates();
    }

    public async Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType()
    {
        _logger.LogTrace("Fetching solid manure type");
        return await _storageCapacityService.FetchSolidManureType();
    }

    public async Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id)
    {
        _logger.LogTrace("Fetch solid Manure Type by Id");
        return await _storageCapacityService.FetchSolidManureTypeById(id);
    }

    public async Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id)
    {
        _logger.LogTrace("Fetching storage type by Id");
        return await _storageCapacityService.FetchStorageTypeById(id);
    }

    public async Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes()
    {
        _logger.LogTrace("Fetchinh storage types");
        return await _storageCapacityService.FetchStorageTypes();
    }

    public async Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching store capacity by FarmId and Year");
        return await _storageCapacityService.FetchStoreCapacityByFarmId(farmId);
    }

    public async Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id)
    {
        _logger.LogTrace("Fetching store capacity by Id");
        return await _storageCapacityService.FetchStoreCapacityByIdAsync(id);
    }

    public async Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID)
    {
        _logger.LogTrace("Is store name exist");
        return await _storageCapacityService.IsStoreNameExistAsync(farmId, storeName, ID);
    }

    public async Task<(string, Error)> RemoveStorageCapacity(int id)
    {
        _logger.LogTrace("Remove storage capacity");
        return await _storageCapacityService.RemoveStorageCapacity(id);
    }

    public async Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData)
    {
        _logger.LogTrace("Update store capacity");
        return await _storageCapacityService.UpdateStoreCapacityAsync(storeCapacityData);
    }
}
