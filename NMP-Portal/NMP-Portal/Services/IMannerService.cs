using NMP.Commons.Models;

namespace NMP.Portal.Services
{
    public interface IMannerService
    {
        Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);

        Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    }
}
