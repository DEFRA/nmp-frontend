using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Enums
{
    public enum WarningKey
    {
        [EnumMember(Value = "ORGANICMANURENFIELDLIMIT")]
        OrganicManureNFieldLimit,

        [EnumMember(Value = "ORGANICMANURENFIELDLIMITCOMPOST")]
        OrganicManureNFieldLimitCompost,

        [EnumMember(Value = "ORGANICMANURENFIELDLIMITCOMPOSTMULCH")]
        OrganicManureNFieldLimitCompostMulch,

        [EnumMember(Value = "NMAXLIMIT")]
        NMaxLimit,

        [EnumMember(Value = "HIGHNORGANICMANURECLOSEDPERIOD")]
        HighNOrganicManureClosedPeriod,

        [EnumMember(Value = "HIGHNORGANICMANURECLOSEDPERIODORGANICFARM")]
        HighNOrganicManureClosedPeriodOrganicFarm,

        [EnumMember(Value = "HIGHNORGANICMANUREMAXRATE")]
        HighNOrganicManureMaxRate,

        [EnumMember(Value = "HIGHNORGANICMANUREMAXRATEWEEKS")]
        HighNOrganicManureMaxRateWeeks,

        [EnumMember(Value = "HIGHNORGANICMANUREMAXRATEGRASS")]
        HighNOrganicManureMaxRateGrass,

        [EnumMember(Value = "HIGHNORGANICMANUREMAXRATEOSR")]
        HighNOrganicManureMaxRateOSR,

        [EnumMember(Value = "HIGHNORGANICMANUREDATEONLY")]
        HighNOrganicManureDateOnly,

        [EnumMember(Value = "SLURRYMAXRATE")]
        SlurryMaxRate,

        [EnumMember(Value = "POULTRYMANUREMAXAPPLICATIONRATE")]
        PoultryManureMaxApplicationRate,

        [EnumMember(Value = "ALLOWWEEKSBETWEENSLURRYPOULTRYAPPLICATIONS")]
        AllowWeeksBetweenSlurryPoultryApplications,

        [EnumMember(Value = "NITROFERTCLOSEDPERIOD")]
        NitroFertClosedPeriod,

        [EnumMember(Value = "INORGNMAXRATE")]
        InorgNMaxRate,

        [EnumMember(Value = "INORGNMAXRATEBRASSICA")]
        InorgNMaxRateBrassica,

        [EnumMember(Value = "INORGNMAXRATEOSR")]
        InorgNMaxRateOSR,

        [EnumMember(Value = "INORGNMAXRATEGRASS")]
        InorgNMaxRateGrass,

        [EnumMember(Value = "INORGFERTDATEONLY")]
        InorgFertDateOnly,

        [EnumMember(Value = "MANURESLURRYMAXAPPLICATIONRATE")]
        ManureSlurryMaxApplicationRate,

        [EnumMember(Value = "MANURESPOULTRYMANUREMAXAPPLICATIONRATE")]
        ManuresPoultryManureMaxApplicationRate
    }

}
