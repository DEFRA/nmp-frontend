using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IMannerEstimationLogic
    {
        MannerEstimationStep1ViewModel GetMannerEstimationStep1();
        MannerEstimationStep1ViewModel SetMannerEstimationStep1(MannerEstimationStep1ViewModel mannerEstimationStep1);
        MannerEstimationStep2ViewModel GetMannerEstimationStep2();
        Task<MannerEstimationStep2ViewModel> SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2);
        MannerEstimationStep3ViewModel GetMannerEstimationStep3();
        Task<MannerEstimationStep3ViewModel> SetMannerEstimationStep3(MannerEstimationStep3ViewModel mannerEstimationStep3);
        Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4();
        Task<MannerEstimationStep4ViewModel> SetMannerEstimationStep4(MannerEstimationStep4ViewModel mannerEstimationStep4);

        MannerEstimationStep5ViewModel GetMannerEstimationStep5();
        MannerEstimationStep5ViewModel SetMannerEstimationStep5(MannerEstimationStep5ViewModel mannerEstimationStep5);

        MannerEstimationStep6ViewModel GetMannerEstimationStep6();
        MannerEstimationStep6ViewModel SetMannerEstimationStep6(MannerEstimationStep6ViewModel mannerEstimationStep6);
        MannerEstimationStep7ViewModel GetMannerEstimationStep7();
        MannerEstimationStep7ViewModel SetMannerEstimationStep7(MannerEstimationStep7ViewModel mannerEstimationStep7);

        MannerEstimationStep8ViewModel GetMannerEstimationStep8();
        MannerEstimationStep8ViewModel SetMannerEstimationStep8(MannerEstimationStep8ViewModel mannerEstimationStep8);

        MannerEstimationStep9ViewModel GetMannerEstimationStep9();
        Task<MannerEstimationStep9ViewModel> SetMannerEstimationStep9(MannerEstimationStep9ViewModel mannerEstimationStep9);
        MannerEstimationStep10ViewModel GetMannerEstimationStep10();
        MannerEstimationStep10ViewModel SetMannerEstimationStep10(MannerEstimationStep10ViewModel mannerEstimationStep10);
        MannerEstimationStep11ViewModel GetMannerEstimationStep11();
        MannerEstimationStep11ViewModel SetMannerEstimationStep11(MannerEstimationStep11ViewModel mannerEstimationStep11);
        MannerEstimationStep12ViewModel GetMannerEstimationStep12();
        MannerEstimationStep12ViewModel SetMannerEstimationStep12(MannerEstimationStep12ViewModel mannerEstimationStep12);
        MannerEstimationStep13ViewModel GetMannerEstimationStep13();
        MannerEstimationStep13ViewModel SetMannerEstimationStep13(MannerEstimationStep13ViewModel mannerEstimationStep13);
        MannerEstimationStep14ViewModel GetMannerEstimationStep14();
        MannerEstimationStep14ViewModel SetMannerEstimationStep14(MannerEstimationStep14ViewModel mannerEstimationStep14);
        MannerEstimationStep15ViewModel GetMannerEstimationStep15();
        MannerEstimationStep15ViewModel SetMannerEstimationStep15(MannerEstimationStep15ViewModel mannerEstimationStep15);
        MannerEstimationStep16ViewModel GetMannerEstimationStep16();
        MannerEstimationStep16ViewModel SetMannerEstimationStep16(MannerEstimationStep16ViewModel mannerEstimationStep16);
        MannerEstimationStep17ViewModel GetMannerEstimationStep17();
        MannerEstimationStep17ViewModel SetMannerEstimationStep17(MannerEstimationStep17ViewModel mannerEstimationStep17);
        MannerEstimationStep18ViewModel GetMannerEstimationStep18();
        MannerEstimationStep18ViewModel SetMannerEstimationStep18(MannerEstimationStep18ViewModel mannerEstimationStep18);
        MannerEstimationStep19ViewModel GetMannerEstimationStep19();
        MannerEstimationStep19ViewModel SetMannerEstimationStep19(MannerEstimationStep19ViewModel mannerEstimationStep19);
        MannerEstimationStep20ViewModel GetMannerEstimationStep20();
        Task<MannerEstimationStep20ViewModel> SetMannerEstimationStep20(MannerEstimationStep20ViewModel mannerEstimationStep20);
        MannerEstimationStep23ViewModel GetMannerEstimationStep23();
        Task<MannerEstimationStep23ViewModel> SetMannerEstimationStep23(MannerEstimationStep23ViewModel mannerEstimationStep23);
        ManureType? GetAndApplyManureType(int manureTypeId, List<ManureType> manureTypeList);
        Task<MannerEstimationStep24ViewModel> GetMannerEstimationStep24();
        Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(MannerEstimationStep24ViewModel mannerEstimationStep24);
        Task<MannerEstimationStep25ViewModel> GetMannerEstimationStep25();
        Task<MannerEstimationStep25ViewModel> SetMannerEstimationStep25(MannerEstimationStep25ViewModel mannerEstimationStep25);
        Task<MannerEstimationStep26ViewModel> GetMannerEstimationStep26();
        Task<MannerEstimationStep26ViewModel> SetMannerEstimationStep26(MannerEstimationStep26ViewModel mannerEstimationStep26);
        Task<MannerEstimationStep27ViewModel> GetMannerEstimationStep27();
        Task<MannerEstimationStep27ViewModel> SetMannerEstimationStep27(MannerEstimationStep27ViewModel mannerEstimationStep27);
        Task<MannerEstimationStep28ViewModel> GetMannerEstimationStep28();
        Task<MannerEstimationStep28ViewModel> SetMannerEstimationStep28(MannerEstimationStep28ViewModel mannerEstimationStep28);
        Task<Error?> CopiedFarmAndFieldData(int farmId, int fieldId);


        MannerEstimationStep21ViewModel GetMannerEstimationStep21();
        MannerEstimationStep21ViewModel SetMannerEstimationStep21(MannerEstimationStep21ViewModel mannerEstimationStep21);

        MannerEstimationStep22ViewModel GetMannerEstimationStep22();
        MannerEstimationStep22ViewModel SetMannerEstimationStep22(MannerEstimationStep22ViewModel mannerEstimationStep22);
        Task<(List<MannerEstimation>, Error?)> FetchMannerEstimationsList(Guid orgId);
        MannerEstimationStep29ViewModel GetMannerEstimationStep29();
        MannerEstimationStep29ViewModel SetMannerEstimationStep29(MannerEstimationStep29ViewModel mannerEstimationStep29);

        MannerEstimationStep30ViewModel GetMannerEstimationStep30();
        MannerEstimationStep30ViewModel SetMannerEstimationStep30(MannerEstimationStep30ViewModel mannerEstimationStep30);
        Task<bool> FetchIsExistMannerEstimationsByOrgIdAndName(Guid organisationId, string name);
        MannerEstimationStep31ViewModel GetMannerEstimationStep31();
        MannerEstimationStep31ViewModel SetMannerEstimationStep31(MannerEstimationStep31ViewModel mannerEstimationStep31);
        MannerEstimationStep32ViewModel GetMannerEstimationStep32();
        MannerEstimationStep32ViewModel SetMannerEstimationStep32(MannerEstimationStep32ViewModel mannerEstimationStep32);
        Task<(bool, Error?)> AddMannerEstimation();
    }
}
