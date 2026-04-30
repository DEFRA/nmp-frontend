using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Enums
{
    public enum OrganicManureWizardSteps
    {
        FieldGroup = 1,
        Fieldlist = 2,
        ManureGroup = 3, 
        ManureType = 4,
        OtherMaterialName =5,
        IsSameDefoliationForAll =6,
        DoubleCrop = 7, 
        Defoliation = 8, 
        ApplicationDate = 9, 
        ApplicationMethod = 10,
        DefaultNutrient=11,
        ManualNutrientValue=12,
        ApplicationRateMethod = 13,
        AreaQuantity=14,
        ManualApplicationRate =15,
        IncorporationMethod=16,
        IncorporationDelay=17,
        ConditionsAffectingNutrients=18,
        AutumnCropNitrogenUptakeDetail=19,
        AutumnCropNitrogenUptake=20,
        EffectiveRainfallManual=21,
        EffectiveRainfall= 22,
        RainfallWithinSixHour = 23,
        SoilDrainageEndDate = 24,
        TopsoilMoisture = 25,
        Windspeed = 26,
    }
}
