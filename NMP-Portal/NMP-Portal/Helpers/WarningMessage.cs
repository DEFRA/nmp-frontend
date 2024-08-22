using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Globalization;
using System.Reflection;

namespace NMP.Portal.Helpers
{
    public class WarningMessage
    {
        public string ClosedPeriodNonOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, bool isPerennial)
        {
            string closedPeriod = string.Empty;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand ||
                                     fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow;
            var isFieldTypeGrass = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass;
            var isFieldTypeArable = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable;
            DateTime? sowingDate = fieldDetail.SowingDate?.ToLocalTime();

            if (isSandyShallowSoil && isFieldTypeGrass)
            {
                closedPeriod = Resource.lbl1Septo31Dec;
            }
            else if (!isSandyShallowSoil && isFieldTypeGrass)
            {
                closedPeriod = Resource.lbl15Octto31Jan;
            }
            if (!isPerennial)
            {
                if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate >= september16)
                {
                    closedPeriod = Resource.lbl1Augto31Dec;
                }
                else if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate < september16)
                {
                    closedPeriod = Resource.lbl16Septo31Dec;
                }
                else if (isFieldTypeArable && !isSandyShallowSoil)
                {
                    closedPeriod = Resource.lbl1Octto31Jan;
                }
            }
            else
            {
                if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate >= september16)
                {
                    closedPeriod = Resource.lbl1Augto31Dec;
                }
                else if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate < september16)
                {
                    closedPeriod = Resource.lbl16Septo31Dec;
                }
                else if (isFieldTypeArable && !isSandyShallowSoil)
                {
                    closedPeriod = Resource.lbl1Octto31Jan;
                }
                if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                {
                    closedPeriod = Resource.lbl16Septo31Dec;
                }
                else if (!isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                {
                    closedPeriod = Resource.lbl1Octto31Jan;
                }

            }

            return closedPeriod;

        }

        public string? ClosedPeriodOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, int cropTypeId, int? cropInfo1)
        {
            string? closedPeriod = string.Empty;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand ||
                                     fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow;
            var isFieldTypeGrass = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass;
            var isFieldTypeArable = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable;
            DateTime? sowingDate = fieldDetail.SowingDate?.ToLocalTime();

            if (isFieldTypeGrass)
            {
                closedPeriod = isSandyShallowSoil
                    ? Resource.lbl1Novto31Dec
                    : Resource.lbl1Novto15Jan;
            }
            else if (isFieldTypeArable)
            {
                switch (cropTypeId)
                {
                    case (int)NMP.Portal.Enums.CropTypes.Asparagus:             //Asparagus
                    case (int)NMP.Portal.Enums.CropTypes.BulbOnions:            //Bulb Onions
                        closedPeriod = null;
                        break;

                    case (int)NMP.Portal.Enums.CropTypes.SaladOnions:            //Salad Onions
                        closedPeriod = cropInfo1 == 12 ? null : closedPeriod;      // cropInfo1Id==12 for Overwintered
                        break;

                    //Brassica is a crop group. under this below crop type comes..
                    case (int)NMP.Portal.Enums.CropTypes.BrusselSprouts:         //Brussel Sprouts
                    case (int)NMP.Portal.Enums.CropTypes.Cabbage:                //Cabbage
                    case (int)NMP.Portal.Enums.CropTypes.Cauliflower:            //Cauliflower
                    case (int)NMP.Portal.Enums.CropTypes.Calabrese:              //Calabrese

                        closedPeriod = (isSandyShallowSoil && sowingDate >= september16) ||
                                       (!isSandyShallowSoil) ? null : closedPeriod;
                        break;

                    case (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape:      // Winter oilseed rape
                        closedPeriod = isSandyShallowSoil || !isSandyShallowSoil ?
                                       Resource.lbl1Novto31Dec : closedPeriod;
                        break;

                    default:
                        closedPeriod = null;
                        break;
                }
            }
            return closedPeriod;
        }
    }
}
