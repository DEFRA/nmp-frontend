using Microsoft.AspNetCore.Mvc.ModelBinding;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface IMannerLogic
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    MannerEstimationStep1 GetMannerEstimationStep1();
    MannerEstimationStep1 SetMannerEstimationStep1(MannerEstimationStep1 mannerEstimationStep1);
    MannerEstimationStep2 GetMannerEstimationStep2();
    MannerEstimationStep2 SetMannerEstimationStep2(MannerEstimationStep2 mannerEstimationStep2);
    MannerEstimationStep3 GetMannerEstimationStep3();
    MannerEstimationStep3 SetMannerEstimationStep3(MannerEstimationStep3 mannerEstimationStep3);

}
