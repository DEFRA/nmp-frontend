using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IStorageCapacityService
    {
        Task<(List<StoreCapacity>, Error)> FetchStoreCapacityByFarmIdAndYear(int farmId, int year);
        Task<(List<CommonResponse>, Error)> FetchMaterialStates();
        Task<(List<StorageTypeResponse>, Error)> FetchStorageTypes();
    }
}
