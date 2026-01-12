namespace NMP.Core.Interfaces;
public interface IMannerService
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
}
