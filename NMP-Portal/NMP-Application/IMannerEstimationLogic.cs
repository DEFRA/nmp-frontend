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
        MannerEstimationViewModel GetMannerEstimationViewModel();
        MannerEstimationStep1ViewModel GetMannerEstimationStep1(string sessionId);
        MannerEstimationStep1ViewModel SetMannerEstimationStep1(string sessionId,MannerEstimationStep1ViewModel mannerEstimationStep1);
        MannerEstimationStep2ViewModel GetMannerEstimationStep2(string sessionId);
        Task<MannerEstimationStep2ViewModel> SetMannerEstimationStep2(string sessionId,MannerEstimationStep2ViewModel mannerEstimationStep2);
        MannerEstimationStep3ViewModel GetMannerEstimationStep3(string sessionId);
        Task<MannerEstimationStep3ViewModel> SetMannerEstimationStep3(string sessionId,MannerEstimationStep3ViewModel mannerEstimationStep3);
        Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4(string sessionId);
        Task<MannerEstimationStep4ViewModel> SetMannerEstimationStep4(string sessionId,MannerEstimationStep4ViewModel mannerEstimationStep4);

        MannerEstimationStep5ViewModel GetMannerEstimationStep5(string sessionId);
        MannerEstimationStep5ViewModel SetMannerEstimationStep5(string sessionId,MannerEstimationStep5ViewModel mannerEstimationStep5);

        MannerEstimationStep6ViewModel GetMannerEstimationStep6(string sessionId);
        MannerEstimationStep6ViewModel SetMannerEstimationStep6(string sessionId,MannerEstimationStep6ViewModel mannerEstimationStep6);
        MannerEstimationStep7ViewModel GetMannerEstimationStep7(string sessionId);
        MannerEstimationStep7ViewModel SetMannerEstimationStep7(string sessionId,MannerEstimationStep7ViewModel mannerEstimationStep7);

        MannerEstimationStep8ViewModel GetMannerEstimationStep8(string sessionId);
        MannerEstimationStep8ViewModel SetMannerEstimationStep8(string sessionId,MannerEstimationStep8ViewModel mannerEstimationStep8);

        MannerEstimationStep9ViewModel GetMannerEstimationStep9(string sessionId);
        Task<MannerEstimationStep9ViewModel> SetMannerEstimationStep9(string sessionId,MannerEstimationStep9ViewModel mannerEstimationStep9);
        MannerEstimationStep10ViewModel GetMannerEstimationStep10(string sessionId);
        MannerEstimationStep10ViewModel SetMannerEstimationStep10(string sessionId,MannerEstimationStep10ViewModel mannerEstimationStep10);
        MannerEstimationStep11ViewModel GetMannerEstimationStep11(string sessionId);
        Task<MannerEstimationStep11ViewModel> SetMannerEstimationStep11(string sessionId,MannerEstimationStep11ViewModel mannerEstimationStep11);
        MannerEstimationStep12ViewModel GetMannerEstimationStep12(string sessionId);
        MannerEstimationStep12ViewModel SetMannerEstimationStep12(string sessionId,MannerEstimationStep12ViewModel mannerEstimationStep12);
        MannerEstimationStep13ViewModel GetMannerEstimationStep13(string sessionId);
        MannerEstimationStep13ViewModel SetMannerEstimationStep13(string sessionId,MannerEstimationStep13ViewModel mannerEstimationStep13);
        MannerEstimationStep14ViewModel GetMannerEstimationStep14(string sessionId);
        MannerEstimationStep14ViewModel SetMannerEstimationStep14(string sessionId,MannerEstimationStep14ViewModel mannerEstimationStep14);
        MannerEstimationStep15ViewModel GetMannerEstimationStep15(string sessionId);
        MannerEstimationStep15ViewModel SetMannerEstimationStep15(string sessionId,MannerEstimationStep15ViewModel mannerEstimationStep15);
        MannerEstimationStep16ViewModel GetMannerEstimationStep16(string sessionId);
        MannerEstimationStep16ViewModel SetMannerEstimationStep16(string sessionId,MannerEstimationStep16ViewModel mannerEstimationStep16);
        MannerEstimationStep17ViewModel GetMannerEstimationStep17(string sessionId);
        MannerEstimationStep17ViewModel SetMannerEstimationStep17(string sessionId,MannerEstimationStep17ViewModel mannerEstimationStep17);
        MannerEstimationStep18ViewModel GetMannerEstimationStep18(string sessionId);
        MannerEstimationStep18ViewModel SetMannerEstimationStep18(string sessionId,MannerEstimationStep18ViewModel mannerEstimationStep18);
        MannerEstimationStep19ViewModel GetMannerEstimationStep19(string sessionId);
        MannerEstimationStep19ViewModel SetMannerEstimationStep19(string sessionId,MannerEstimationStep19ViewModel mannerEstimationStep19);
        MannerEstimationStep20ViewModel GetMannerEstimationStep20(string sessionId);
        Task<MannerEstimationStep20ViewModel> SetMannerEstimationStep20(string sessionId,MannerEstimationStep20ViewModel mannerEstimationStep20);
        MannerEstimationStep23ViewModel GetMannerEstimationStep23(string sessionId);
        Task<MannerEstimationStep23ViewModel> SetMannerEstimationStep23(string sessionId,MannerEstimationStep23ViewModel mannerEstimationStep23);
        ManureType? GetAndApplyManureType(int manureTypeId, List<ManureType> manureTypeList);
        Task<MannerEstimationStep24ViewModel> GetMannerEstimationStep24(string sessionId);
        Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(string sessionId,MannerEstimationStep24ViewModel mannerEstimationStep24);
        Task<MannerEstimationStep25ViewModel> GetMannerEstimationStep25(string sessionId, bool isDefault);
        Task<MannerEstimationStep25ViewModel> SetMannerEstimationStep25(string sessionId,MannerEstimationStep25ViewModel mannerEstimationStep25, bool isDefault);
        Task<MannerEstimationStep26ViewModel> GetMannerEstimationStep26(string sessionId);
        Task<MannerEstimationStep26ViewModel> SetMannerEstimationStep26(string sessionId,MannerEstimationStep26ViewModel mannerEstimationStep26);
        Task<MannerEstimationStep27ViewModel> GetMannerEstimationStep27(string sessionId);
        Task<MannerEstimationStep27ViewModel> SetMannerEstimationStep27(string sessionId,MannerEstimationStep27ViewModel mannerEstimationStep27);
        Task<MannerEstimationStep28ViewModel> GetMannerEstimationStep28(string sessionId);
        Task<MannerEstimationStep28ViewModel> SetMannerEstimationStep28(string sessionId,MannerEstimationStep28ViewModel mannerEstimationStep28);
        Task<Error?> CopiedFarmAndFieldData(int farmId, int fieldId, string? sid = null);


        MannerEstimationStep21ViewModel GetMannerEstimationStep21(string sessionId);
        MannerEstimationStep21ViewModel SetMannerEstimationStep21(string sessionId,MannerEstimationStep21ViewModel mannerEstimationStep21);

        MannerEstimationStep22ViewModel GetMannerEstimationStep22(string sessionId);
        MannerEstimationStep22ViewModel SetMannerEstimationStep22(string sessionId,MannerEstimationStep22ViewModel mannerEstimationStep22);
        Task<(List<MannerEstimationDetailsViewModel>, Error?)> FetchMannerEstimationsList(Guid orgId);
        MannerEstimationStep29ViewModel GetMannerEstimationStep29(string sessionId);
        MannerEstimationStep29ViewModel SetMannerEstimationStep29(string sessionId,MannerEstimationStep29ViewModel mannerEstimationStep29);

        MannerEstimationStep30ViewModel GetMannerEstimationStep30(string sessionId);
        MannerEstimationStep30ViewModel SetMannerEstimationStep30(string sessionId,MannerEstimationStep30ViewModel mannerEstimationStep30);
        Task<bool> FetchIsExistMannerEstimationsByMannerFarmIdAndName(int mannerFarmId, string name);
        MannerEstimationStep31ViewModel GetMannerEstimationStep31(string sessionId);
        MannerEstimationStep31ViewModel SetMannerEstimationStep31(string sessionId,MannerEstimationStep31ViewModel mannerEstimationStep31);
        Task<MannerEstimationStep32ViewModel> GetMannerEstimationStep32(string sessionId);
        Task<MannerEstimationStep32ViewModel> SetMannerEstimationStep32(string sessionId,MannerEstimationStep32ViewModel mannerEstimationStep32);
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

        MannerEstimationStep33ViewModel GetMannerEstimationStep33(string sessionId);
        MannerEstimationStep33ViewModel SetMannerEstimationStep33(string sessionId,MannerEstimationStep33ViewModel mannerEstimationStep33);
       Task<MannerEstimationStep34ViewModel> GetMannerEstimationStep34(string sessionId);
        Task<MannerEstimationStep34ViewModel> SetMannerEstimationStep34(string sessionId,MannerEstimationStep34ViewModel mannerEstimationStep34);
        MannerEstimationStep35ViewModel GetMannerEstimationStep35(string sessionId);
        MannerEstimationStep35ViewModel SetMannerEstimationStep35(string sessionId,MannerEstimationStep35ViewModel mannerEstimationStep35    );

        Task<(List<NutrientProductResponse>, Error?)> FetchNutrientProductByNutrientId(int nurteintId);
        Task<(MannerEstimation?, Error?)> FetchMannerEstimateById(int mannerEstimateId);
        MannerEstimationViewModel? GetMannerEstimationFromSession(string sessionId);
        string SetMannerEstimationToSession(MannerEstimationViewModel mannerEstimationViewModel);
        string? GetCurrentSessionId();
        Task<(MannerEstimation?, Error?)> UpdateMannerEstimation(int MannerEstimationId, string sessionId);
        MannerEstimationStep36ViewModel GetMannerEstimationStep36(string sessionId);
        MannerEstimationStep36ViewModel SetMannerEstimationStep36(string sessionId,MannerEstimationStep36ViewModel mannerEstimationStep36);
        Task<MannerEstimationStep37ViewModel> GetMannerEstimationStep37(string sessionId);
        Task<MannerEstimationStep37ViewModel> SetMannerEstimationStep37(string sessionId,MannerEstimationStep37ViewModel mannerEstimationStep37);

        MannerEstimationStep38ViewModel GetMannerEstimationStep38(string sessionId);
        MannerEstimationStep38ViewModel SetMannerEstimationStep38(string sessionId,MannerEstimationStep38ViewModel mannerEstimationStep38);
        Task<MannerEstimationStep39ViewModel> GetMannerEstimationStep39(string sessionId);
        Task<MannerEstimationStep39ViewModel> SetMannerEstimationStep39(string sessionId,MannerEstimationStep39ViewModel mannerEstimationStep39);

        MannerEstimationStep40ViewModel GetMannerEstimationStep40(string sessionId);
        MannerEstimationStep40ViewModel SetMannerEstimationStep40(string sessionId,MannerEstimationStep40ViewModel mannerEstimationStep40);

        Task<(decimal, Error)> FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(int mannerEstimationId, DateTime startDate, DateTime endDate, bool isGreenFoodCompost, int? mannerApplicationId);

        Task<(decimal, Error)> FetchTotalNByMannerEstimationIdAppDate(int mannerEstimationId, DateTime startDate, DateTime endDate, int? mannerApplicationId);

        Task<(bool, Error)> CheckMannerGreenCompostExistanceByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId);
        Task<Error?> BindMannerEstimationDataForUpdate(int mannerEstimateId, string? sid = null);
        Task<(MannerEstimation?, Error?)> UpdateFarmFieldAndCropData(int mannerEstimationId, string? sid = null);
        Task<(MannerEstimationApplication?, Error?)> FetchMannerEstimateApplicationById(int mannerEstimateApplicationId);
        Task<Error?> BindApplicationDetailForUpdate(int mannerEstimateApplicationId, string? sid = null);
        Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimationApplicationData(string? sid = null);
        Task<int?> GetCropGroupByCropTypeId(int? cropTypeId);
        Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationApplication(string sessionId);

        Task<Error?> RemoveMannerEstimations(string mannerEstimationIds);
        MannerEstimationStep41ViewModel GetMannerEstimationStep41(string sessionId);
        MannerEstimationStep41ViewModel SetMannerEstimationStep41(string sessionId,MannerEstimationStep41ViewModel mannerEstimationStep41);
        Task<(string, Error?)> DeleteMannerEstimateApplicationById(int mannerEstimationId);

        Task<(MannerFarmViewModel?, Error?)> FetchMannerFarmById(int mannerFarmId);
        Task<(List<MannerFarmViewModel>, Error?)> FetchMannerFarmListByOrgId(Guid orgId);
        Task<(List<MannerEstimationSummaryViewModel>, Error?)> FetchMannerEstimateByFarmId(int mannerFarmId);
        Task<(MannerEstimationApplication?, Error?)> AddNewMannerEstimation(string sessionId);
        bool CheckSandyShallowByTopSoilSubSoilId(int topSoilId, int subSoilId, int countryId);  
        Task BindFarmDataForMannerEstimateUpdateOrCreate(int mannerFarmId,string sid);
        MannerEstimationStep42ViewModel GetMannerEstimationStep42(string sessionId);
        MannerEstimationStep42ViewModel SetMannerEstimationStep42(string sessionId,MannerEstimationStep42ViewModel mannerEstimationStep42);
        Task<Error?> RemoveMannerFarms(string mannerFarmIds);
        Task<bool> FetchIsExistMannerFarmByOrgIdAndName(Guid organisationId, string farmName);
        Task<(decimal?, Error?)> FetchTotalApplicationRateByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId, bool isPoultry);
    }
}
