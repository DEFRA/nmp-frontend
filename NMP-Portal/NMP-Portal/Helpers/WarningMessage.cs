using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public string ClosedPeriodWarningMessage(DateTime applicationDate, string closedPeriod, string cropType, FieldDetailResponse fieldDetail)
        {
            string message = string.Empty;
            int day = applicationDate.Day;
            int month = applicationDate.Month;


            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(closedPeriod);
            if (match.Success)
            {
                int startDay = int.Parse(match.Groups[1].Value);
                string startMonthStr = match.Groups[2].Value;
                int endDay = int.Parse(match.Groups[3].Value);
                string endMonthStr = match.Groups[4].Value;

                DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
                int startMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, startMonthStr) + 1;
                int endMonth = Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;

                if (month >= startMonth && month <= endMonth)
                {
                    if (day >= startDay && day <= endDay)
                    {
                        //TempData["ClosedPeriodWarning"] = Resource.MsgApplicationDateEnteredIsInsideClosedPeriod;
                        message = string.Format(Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetail, cropType, fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);

                        return message;
                    }
                }
            }
            return message;
        }

        public string ClosedPeriodForFertiliserWarningMessage(DateTime applicationDate, int cropType, bool isFarmOragnic)
        {
            string message = string.Empty;
            int day = applicationDate.Day;
            int month = applicationDate.Month;
            if (cropType == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropType == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if ((month > 10 || (month == 10 && day >= 31)) || (month < 1 || (month == 1 && day <= 15)))
                {

                }
            }
            else if (cropType != (int)NMP.Portal.Enums.CropTypes.Asparagus || cropType != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts || cropType != (int)NMP.Portal.Enums.CropTypes.Cabbage ||
                cropType != (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropType != (int)NMP.Portal.Enums.CropTypes.Calabrese ||
                cropType != (int)NMP.Portal.Enums.CropTypes.BulbOnions || cropType != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
            {
                if ((month > 9 || (month == 9 && day >= 1)) || (month < 1 || (month == 1 && day <= 15)))
                {

                }
            }
            return message;
        }
        public string NitrogenLimitForFertiliserWarningMessage(DateTime applicationDate, int cropType, decimal totalNitrogen, decimal fourWeekNitrogen, decimal nitrogenOfSingleApp)
        {
            string message = string.Empty;
            int day = applicationDate.Day;
            int month = applicationDate.Month;
            if (cropType == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
            {
                if (totalNitrogen > 30.0m)
                {

                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Asparagus)
            {
                if (totalNitrogen > 50.0m)
                {

                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts || cropType == (int)NMP.Portal.Enums.CropTypes.Cabbage ||
                cropType == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropType == (int)NMP.Portal.Enums.CropTypes.Calabrese)
            {
                if (totalNitrogen > 100)
                {
                    // Exceeds max 100kg/ha during the closed period
                }

                else if (totalNitrogen > 50 || fourWeekNitrogen + totalNitrogen > 50)
                {
                    // Exceeds 50kg limit in any 4-week period
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.SaladOnions || cropType == (int)NMP.Portal.Enums.CropTypes.BulbOnions)
            {
                if (totalNitrogen > 40.0m)
                {

                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if (totalNitrogen > 80.0m)
                {

                }

                if (nitrogenOfSingleApp > 40.0m)
                {

                }
            }
            return message;
        }

    }
}