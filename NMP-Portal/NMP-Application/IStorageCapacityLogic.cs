using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Application;

public interface IStorageCapacityLogic
{
    Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmId(int farmId);
    Task<(List<CommonResponse>, Error)> FetchMaterialStates();
    Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes();
    Task<(CommonResponse, Error)> FetchMaterialStateById(int id);
    Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id);
    Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType();
    Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id);
    Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles();
    Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id);
    Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData);
    Task<(bool, Error)> IsStoreNameExistAsync(int farmId, string storeName, int? ID);
    Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id);
    Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData);
    Task<(string, Error)> RemoveStorageCapacity(int id);
    Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData);
    StorageCapacityViewModel? GetStorageCapacityFromSession(Guid? sessionId);
    void SetStorageCapacityToSession(StorageCapacityViewModel storageCapacityViewModel);
    void RemoveStorageCapacitySession(Guid? sessionId);
    StorageCapacityViewModel? GetStorageCapacityBeforeUpdateFromSession(Guid? sessionId);
    void SetStorageCapacityBeforeUpdateToSession(StorageCapacityViewModel storageCapacityViewModel);
    void RemoveStorageCapacityBeforeUpdateSession(Guid? sessionId);
}
