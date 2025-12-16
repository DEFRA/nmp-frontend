using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IStorageCapacityService
    {
        Task<(List<StoreCapacityResponse>, Error)> FetchStoreCapacityByFarmIdAndYear(int farmId, int? year);
        Task<(List<CommonResponse>, Error)> FetchMaterialStates();
        Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes();
        Task<(CommonResponse, Error)> FetchMaterialStateById(int id);
        Task<(StorageTypeResponse, Error)> FetchStorageTypeById(int id);
        Task<(List<SolidManureTypeResponse>, Error)> FetchSolidManureType();
        Task<(SolidManureTypeResponse, Error)> FetchSolidManureTypeById(int id);
        Task<(List<BankSlopeAnglesResponse>, Error)> FetchBankSlopeAngles();
        Task<(BankSlopeAnglesResponse, Error)> FetchBankSlopeAngleById(int id);
        Task<(StoreCapacity, Error)> AddStoreCapacityAsync(StoreCapacity storeCapacityData);
        Task<(bool, Error)> IsStoreNameExistAsync(int farmId, int year, string storeName, int? ID);
        Task<(StoreCapacity, Error)> FetchStoreCapacityByIdAsync(int id);
        Task<(List<StoreCapacityResponse>, Error)> CopyExistingStorageCapacity(string copyStorageManureCapacityData);
        Task<(string, Error)> RemoveStorageCapacity(int id);
        Task<(StoreCapacity, Error)> UpdateStoreCapacityAsync(StoreCapacity storeCapacityData);
    }
}
