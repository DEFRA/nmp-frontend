using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Enums
{
    public enum CropWizardSteps
    {
        HarvestYearForPlan=1,
        CropGroup =1,
        CropType = 2,
        CropField = 3,
        CropGroupName = 4,
        Variety = 5,
        SowingDateQuestion = 6,
        SowingDate = 7,
        YieldQuestion = 8,
        Yield = 9,
        CropInfoOne = 10,
        CropInfoTwo = 11,
        CurrentSward=12,
        Defoliation = 13,
        DefoliationSequence = 14,
        DryMatterYield=15,
        GrassGrowthClass=16,
        GrassManagement=17,
        GrassSeason=18,
        SwardType=19,
        RemoveCrop=20,
        AnotherCrop=21,
        AddOrRemoveField=22,
        CopyCheckAnswer=23,
        CopyExistingPlan=24,
        CopyOrganicInorganicApplications=25
            CopyPlanYears=26
    }
}
