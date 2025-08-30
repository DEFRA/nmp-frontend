using NMP.Portal.Models;
using System.Net.NetworkInformation;

namespace NMP.Portal.Enums
{
    public enum NonGrazingManureType
    {
        PigFarmyardManureFresh = 2,
        PigFarmyardManureOld = 3,
        DuckFarmyardManureFresh = 6,
        DuckFarmyardManureOld = 7,
        PoultryManure = 8,
        PigSlurry = 12,
        SeparatedPigSlurrySolidPortion=17,
	SeparatedPigSlurryLiquidPortion=18
    }
}
