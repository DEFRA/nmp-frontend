using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Reflection;

namespace NMP.Portal.Helpers
{
    public class WarningMessage
    {
        public string ClosedPeriod(FieldDetailResponse fieldDetail, int harvestYear,bool isOrganic, bool isPerennial) 
        {
            string closedPeriod = string.Empty;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Sand || fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilType.Shallow;
            var isFieldTypeGrass = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass;
            var isFieldTypeArable = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable;
            if (!isOrganic)
            {
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
            return closedPeriod;
        }
    }
}
