using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IMannerEstimationLogic
    {
        Task<MannerEstimationViewModel?> GetMannerEstimationFromSessionabc(string sessionId);
        Task<string> SetMannerEstimationToSessionabc(MannerEstimationViewModel mannerEstimationViewModel, string sessionId);
        Task<MannerEstimationStep1ViewModel> GetMannerEstimationStep1(string? sid);
        Task<MannerEstimationStep1ViewModel> SetMannerEstimationStep1(MannerEstimationStep1ViewModel mannerEstimationStep1,string? sid);
        Task<MannerEstimationStep2ViewModel> GetMannerEstimationStep2(string? sid);
        Task<MannerEstimationStep2ViewModel> SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2,string sessionId);
        Task<MannerEstimationStep3ViewModel> GetMannerEstimationStep3(string? sid);
        Task<MannerEstimationStep3ViewModel> SetMannerEstimationStep3(MannerEstimationStep3ViewModel mannerEstimationStep3,string? sid);
        Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4(string? sid);
        Task<MannerEstimationStep4ViewModel> SetMannerEstimationStep4(MannerEstimationStep4ViewModel mannerEstimationStep4,string? sid);

        MannerEstimationStep5ViewModel GetMannerEstimationStep5(string? sid);
        MannerEstimationStep5ViewModel SetMannerEstimationStep5(MannerEstimationStep5ViewModel mannerEstimationStep5,string? sid);

        MannerEstimationStep6ViewModel GetMannerEstimationStep6(string? sid);
        MannerEstimationStep6ViewModel SetMannerEstimationStep6(MannerEstimationStep6ViewModel mannerEstimationStep6,string? sid);
        MannerEstimationStep7ViewModel GetMannerEstimationStep7(string? sid);
        MannerEstimationStep7ViewModel SetMannerEstimationStep7(MannerEstimationStep7ViewModel mannerEstimationStep7,string? sid);

        MannerEstimationStep8ViewModel GetMannerEstimationStep8(string? sid);
        MannerEstimationStep8ViewModel SetMannerEstimationStep8(MannerEstimationStep8ViewModel mannerEstimationStep8,string? sid);

        MannerEstimationStep9ViewModel GetMannerEstimationStep9(string? sid);
        Task<MannerEstimationStep9ViewModel> SetMannerEstimationStep9(MannerEstimationStep9ViewModel mannerEstimationStep9,string? sid);
        //MannerEstimationStep10ViewModel GetMannerEstimationStep10(string? sid);
        //MannerEstimationStep10ViewModel SetMannerEstimationStep10(MannerEstimationStep10ViewModel mannerEstimationStep10,string? sid);
        MannerEstimationStep11ViewModel GetMannerEstimationStep11(string? sid);
        Task<MannerEstimationStep11ViewModel> SetMannerEstimationStep11(MannerEstimationStep11ViewModel mannerEstimationStep11,string? sid);
        MannerEstimationStep12ViewModel GetMannerEstimationStep12(string? sid);
        MannerEstimationStep12ViewModel SetMannerEstimationStep12(MannerEstimationStep12ViewModel mannerEstimationStep12,string? sid);
        MannerEstimationStep13ViewModel GetMannerEstimationStep13(string? sid);
        MannerEstimationStep13ViewModel SetMannerEstimationStep13(MannerEstimationStep13ViewModel mannerEstimationStep13,string? sid);
        MannerEstimationStep14ViewModel GetMannerEstimationStep14(string? sid);
        MannerEstimationStep14ViewModel SetMannerEstimationStep14(MannerEstimationStep14ViewModel mannerEstimationStep14,string? sid);
        MannerEstimationStep15ViewModel GetMannerEstimationStep15(string? sid);
        MannerEstimationStep15ViewModel SetMannerEstimationStep15(MannerEstimationStep15ViewModel mannerEstimationStep15,string? sid);
        MannerEstimationStep16ViewModel GetMannerEstimationStep16(string? sid);
        MannerEstimationStep16ViewModel SetMannerEstimationStep16(MannerEstimationStep16ViewModel mannerEstimationStep16,string? sid);
        MannerEstimationStep17ViewModel GetMannerEstimationStep17(string? sid);
        MannerEstimationStep17ViewModel SetMannerEstimationStep17(MannerEstimationStep17ViewModel mannerEstimationStep17,string? sid);
        MannerEstimationStep18ViewModel GetMannerEstimationStep18(string? sid);
        MannerEstimationStep18ViewModel SetMannerEstimationStep18(MannerEstimationStep18ViewModel mannerEstimationStep18,string? sid);
        MannerEstimationStep19ViewModel GetMannerEstimationStep19(string? sid);
        MannerEstimationStep19ViewModel SetMannerEstimationStep19(MannerEstimationStep19ViewModel mannerEstimationStep19,string? sid);
        MannerEstimationStep20ViewModel GetMannerEstimationStep20(string? sid);
        Task<MannerEstimationStep20ViewModel> SetMannerEstimationStep20(MannerEstimationStep20ViewModel mannerEstimationStep20,string? sid);
        MannerEstimationStep23ViewModel GetMannerEstimationStep23(string? sid);
        Task<MannerEstimationStep23ViewModel> SetMannerEstimationStep23(MannerEstimationStep23ViewModel mannerEstimationStep23,string? sid);
        ManureType? GetAndApplyManureType(int manureTypeId, List<ManureType> manureTypeList);
        Task<MannerEstimationStep24ViewModel> GetMannerEstimationStep24(string? sid);
        Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(MannerEstimationStep24ViewModel mannerEstimationStep24,string? sid);
        Task<MannerEstimationStep25ViewModel> GetMannerEstimationStep25(bool isDefault,string? sid);
        Task<MannerEstimationStep25ViewModel> SetMannerEstimationStep25(MannerEstimationStep25ViewModel mannerEstimationStep25, bool isDefault,string? sid);
        Task<MannerEstimationStep26ViewModel> GetMannerEstimationStep26(string? sid);
        Task<MannerEstimationStep26ViewModel> SetMannerEstimationStep26(MannerEstimationStep26ViewModel mannerEstimationStep26,string? sid);
        Task<MannerEstimationStep27ViewModel> GetMannerEstimationStep27(string? sid);
        Task<MannerEstimationStep27ViewModel> SetMannerEstimationStep27(MannerEstimationStep27ViewModel mannerEstimationStep27,string? sid);
        Task<MannerEstimationStep28ViewModel> GetMannerEstimationStep28(string? sid);
        Task<MannerEstimationStep28ViewModel> SetMannerEstimationStep28(MannerEstimationStep28ViewModel mannerEstimationStep28,string? sid);
        Task<Error?> CopiedFarmAndFieldData(int farmId, int fieldId, string? sid = null);


        MannerEstimationStep21ViewModel GetMannerEstimationStep21(string? sid);
        MannerEstimationStep21ViewModel SetMannerEstimationStep21(MannerEstimationStep21ViewModel mannerEstimationStep21, string? sid);

        MannerEstimationStep22ViewModel GetMannerEstimationStep22(string? sid);
        MannerEstimationStep22ViewModel SetMannerEstimationStep22(MannerEstimationStep22ViewModel mannerEstimationStep22, string? sid);
        Task<(List<MannerEstimationDetailsViewModel>, Error?)> FetchMannerEstimationsList(Guid orgId);
        MannerEstimationStep29ViewModel GetMannerEstimationStep29(string? sid);
        MannerEstimationStep29ViewModel SetMannerEstimationStep29(MannerEstimationStep29ViewModel mannerEstimationStep29, string? sid);

        MannerEstimationStep30ViewModel GetMannerEstimationStep30(string? sid);
        MannerEstimationStep30ViewModel SetMannerEstimationStep30(MannerEstimationStep30ViewModel mannerEstimationStep30, string? sid);
        Task<bool> FetchIsExistMannerEstimationsByMannerFarmIdAndName(int mannerFarmId, string name);
        MannerEstimationStep31ViewModel GetMannerEstimationStep31(string? sid);
        MannerEstimationStep31ViewModel SetMannerEstimationStep31(MannerEstimationStep31ViewModel mannerEstimationStep31, string? sid);
        Task<MannerEstimationStep32ViewModel> GetMannerEstimationStep32(string? sid);
        Task<MannerEstimationStep32ViewModel> SetMannerEstimationStep32(MannerEstimationStep32ViewModel mannerEstimationStep32, string? sid);
        Task<(MannerEstimationApplication?, Error?)> AddMannerEstimation(Guid organisationId);
        Task<(MannerFarmEstimationApplicationResponse?, Error?)> AddMannerFarmEstimation(Guid organisationId);
        Task<(int?, Error?)> FetchSoilTypeSoilTextureByTopSoilSubSoilId(int topSoilId, int subSoilId);
        Task<(List<MannerEstimationApplication>, Error?)> FetchMannerApplicationsByMannerEstimationId(int mannerEstimationId);
        Task<(MannerEstimationApplication, Error?)> FetchMannerApplicationById(int mannerApplicationId);
        Task<(MannerEstimationResultResponse?, Error?)> FetchMannerApplicationResultById(int mannerEstimationId);
        Task<(int, Error?)> CopyMannerEstimation(int id, string estimationName);
        Task<bool> FetchDefaultNutrientValue(int manureTypeId, MannerEstimationApplication mannerEstimationApplication);
        Task<(bool, int)> FetchApplicationRateOptionValue(int manureTypeId, MannerEstimationApplication mannerEstimationApplication, MannerEstimation mannerEstimation);
         Task<bool> FetchIsManureLiquid(int manureTypeId);

        MannerEstimationStep33ViewModel GetMannerEstimationStep33(string sid);
        MannerEstimationStep33ViewModel SetMannerEstimationStep33(MannerEstimationStep33ViewModel mannerEstimationStep33,string sid);
       Task<MannerEstimationStep34ViewModel> GetMannerEstimationStep34(string sid);
        Task<MannerEstimationStep34ViewModel> SetMannerEstimationStep34(MannerEstimationStep34ViewModel mannerEstimationStep34,string sid);
        MannerEstimationStep35ViewModel GetMannerEstimationStep35(string sid);
        MannerEstimationStep35ViewModel SetMannerEstimationStep35(MannerEstimationStep35ViewModel mannerEstimationStep35,string sid);

        Task<(List<NutrientProductResponse>, Error?)> FetchNutrientProductByNutrientId(int nurteintId);
        Task<(MannerEstimation?, Error?)> FetchMannerEstimateById(int mannerEstimateId);
        MannerEstimationViewModel? GetMannerEstimationFromSession(string? sessionId = null);
        string SetMannerEstimationToSession(MannerEstimationViewModel mannerEstimationViewModel, string sid);
        string? GetCurrentSessionId();
        Task<(MannerEstimation?, Error?)> UpdateMannerEstimation(int MannerEstimationId);
        MannerEstimationStep36ViewModel GetMannerEstimationStep36(string sid);
        MannerEstimationStep36ViewModel SetMannerEstimationStep36(MannerEstimationStep36ViewModel mannerEstimationStep36,string sid);
        Task<MannerEstimationStep37ViewModel> GetMannerEstimationStep37(string sid);
        Task<MannerEstimationStep37ViewModel> SetMannerEstimationStep37(MannerEstimationStep37ViewModel mannerEstimationStep37,string sid);

        MannerEstimationStep38ViewModel GetMannerEstimationStep38(string sid);
        MannerEstimationStep38ViewModel SetMannerEstimationStep38(MannerEstimationStep38ViewModel mannerEstimationStep38,string sid);
        Task<MannerEstimationStep39ViewModel> GetMannerEstimationStep39(string sid);
        Task<MannerEstimationStep39ViewModel> SetMannerEstimationStep39(MannerEstimationStep39ViewModel mannerEstimationStep39,string sid);

        MannerEstimationStep40ViewModel GetMannerEstimationStep40(string sid);
        MannerEstimationStep40ViewModel SetMannerEstimationStep40(MannerEstimationStep40ViewModel mannerEstimationStep40,string sid);

        Task<(decimal, Error)> FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(int mannerEstimationId, DateTime startDate, DateTime endDate, bool isGreenFoodCompost, int? mannerApplicationId);

        Task<(decimal, Error)> FetchTotalNByMannerEstimationIdAppDate(int mannerEstimationId, DateTime startDate, DateTime endDate, int? mannerApplicationId);

        Task<(bool, Error)> CheckMannerGreenCompostExistanceByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId);
        Task<Error?> BindMannerEstimationDataForUpdate(int mannerEstimateId, string? sid = null);
        Task<(MannerEstimation?, Error?)> UpdateFarmFieldAndCropData(int mannerEstimationId, string? sid = null);
        Task<(MannerEstimationApplication?, Error?)> FetchMannerEstimateApplicationById(int mannerEstimateApplicationId);
        Task<Error?> BindApplicationDetailForUpdate(int mannerEstimateApplicationId, string? sid = null);
        Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimationApplicationData(string? sid = null);
        Task<int?> GetCropGroupByCropTypeId(int? cropTypeId);
        Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationApplication();

        Task<Error?> RemoveMannerEstimations(string mannerEstimationIds);
        MannerEstimationStep41ViewModel GetMannerEstimationStep41(string sid);
        MannerEstimationStep41ViewModel SetMannerEstimationStep41(MannerEstimationStep41ViewModel mannerEstimationStep41,string sid);
        Task<(string, Error?)> DeleteMannerEstimateApplicationById(int mannerEstimationId);

        Task<(MannerFarmViewModel?, Error?)> FetchMannerFarmById(int mannerFarmId);
        Task<(List<MannerFarmViewModel>, Error?)> FetchMannerFarmListByOrgId(Guid orgId);
        Task<(List<MannerEstimationSummaryViewModel>, Error?)> FetchMannerEstimateByFarmId(int mannerFarmId);
        Task<(MannerEstimationApplication?, Error?)> AddNewMannerEstimation();
        bool CheckSandyShallowByTopSoilSubSoilId(int topSoilId, int subSoilId, int countryId);  
        Task BindFarmDataForMannerEstimateUpdateOrCreate(int mannerFarmId,string sid);
        MannerEstimationStep42ViewModel GetMannerEstimationStep42(string sid);
        MannerEstimationStep42ViewModel SetMannerEstimationStep42(MannerEstimationStep42ViewModel mannerEstimationStep42,string sid);
        Task<Error?> RemoveMannerFarms(string mannerFarmIds);
        Task<bool> FetchIsExistMannerFarmByOrgIdAndName(Guid organisationId, string farmName);
        Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId, bool isPoultry);
        Task<(MannerEstimation?, Error?)> UpdateMannerEstimationByIdWithApplication(string sid);
        Task<MannerEstimationViewModel> MapApplicationDetailToViewModel(MannerEstimationViewModel mannerEstimationViewModel, MannerEstimationApplication mannerEstimateApplication, string sid);
        Task BindConditionAffectingNutrientValues(MannerEstimationViewModel mannerEstimationViewModel);
        void BindApplicationRateMethodIfSoilOrCropTypeChange(MannerEstimationViewModel mannerEstimationViewModel, ManureType manureType);
    }
}
