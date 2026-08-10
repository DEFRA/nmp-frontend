using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Enums
{
    public enum SubSoil
    {
        Sand = 1,
        LoamySand = 2,
        SandyLoam = 3,
        FineSandyLoam = 4,
        SandySiltLoam = 5,
        SiltLoam = 6,
        SiltyClayLoam = 7,
        SandyClayLoam = 8,
        ClayLoam = 9,
        SandyClay = 10,
        SiltyClay = 11,
        Clay = 12,
        [Description("Organic (10-20% organic matter)")]
        Organic = 13,
        Peaty = 14,
        Peat = 15,
        Chalk = 16,
        [Description("Rock (not chalk)")]
        Rock = 17
    }
}
