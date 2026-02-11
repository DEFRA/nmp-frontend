using Microsoft.AspNetCore.Mvc.ModelBinding;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface IMannerLogic
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    MannerEstimationStep1ViewModel GetMannerEstimationStep1();
    MannerEstimationStep1ViewModel SetMannerEstimationStep1(MannerEstimationStep1ViewModel mannerEstimationStep1);
    MannerEstimationStep2ViewModel GetMannerEstimationStep2();
    MannerEstimationStep2ViewModel SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2);
    MannerEstimationStep3ViewModel GetMannerEstimationStep3();
    Task<MannerEstimationStep3ViewModel> SetMannerEstimationStep3(MannerEstimationStep3ViewModel mannerEstimationStep3);
    Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4();
    Task<MannerEstimationStep4ViewModel> SetMannerEstimationStep4(MannerEstimationStep4ViewModel mannerEstimationStep4);

    MannerEstimationStep5ViewModel GetMannerEstimationStep5();
    MannerEstimationStep5ViewModel SetMannerEstimationStep5(MannerEstimationStep5ViewModel mannerEstimationStep5);

    MannerEstimationStep6ViewModel GetMannerEstimationStep6();
    MannerEstimationStep6ViewModel SetMannerEstimationStep6(MannerEstimationStep6ViewModel mannerEstimationStep6);

    Task<decimal> FetchRainfallAverageAsync(string postcode);
}
