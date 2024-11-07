using Microsoft.Identity.Client;
using NMP.Portal.Enums;
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
        public string? ClosedPeriodNonOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, bool isPerennial)
        {
            string? closedPeriod = null;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.LightSand ||
                                     fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow;
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
                if (isSandyShallowSoil && isFieldTypeArable && (fieldDetail.SowingDate >= september16 || fieldDetail.SowingDate == null))
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
                if (isSandyShallowSoil && isFieldTypeArable && (fieldDetail.SowingDate >= september16 || fieldDetail.SowingDate == null))
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
                if (fieldDetail.SowingDate != null)
                {
                    if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                    {
                        closedPeriod = Resource.lbl16Septo31Dec;
                    }
                    else if (!isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                    {
                        closedPeriod = Resource.lbl1Octto31Jan;
                    }
                }

            }

            return closedPeriod;

        }

        public string? ClosedPeriodOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, int cropTypeId, int? cropInfo1, bool isPerennial)
        {
            string? closedPeriod = null;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.LightSand ||
                                     fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow;
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
                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            closedPeriod = null;
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            closedPeriod = null;
                        }
                        if (!isSandyShallowSoil)
                        {
                            closedPeriod = null;
                        }
                        break;

                    case (int)NMP.Portal.Enums.CropTypes.SaladOnions:            //Salad Onions

                        if (cropInfo1 == 12)                                       // cropInfo1Id==12 for Overwintered
                        {
                            if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                            {
                                closedPeriod = null;
                            }
                            if (isSandyShallowSoil && (sowingDate < september16))
                            {
                                closedPeriod = null;
                            }
                            if (!isSandyShallowSoil)
                            {
                                closedPeriod = null;
                            }
                        }
                        break;

                    //Brassica is a crop group. under this below crop type comes..
                    case (int)NMP.Portal.Enums.CropTypes.BrusselSprouts:         //Brussel Sprouts
                    case (int)NMP.Portal.Enums.CropTypes.Cabbage:                //Cabbage
                    case (int)NMP.Portal.Enums.CropTypes.Cauliflower:            //Cauliflower
                    case (int)NMP.Portal.Enums.CropTypes.Calabrese:              //Calabrese
                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            closedPeriod = null;
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            closedPeriod = null;
                        }
                        if (!isSandyShallowSoil)
                        {
                            closedPeriod = null;
                        }

                        break;

                    case (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape:      // Winter oilseed rape

                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            closedPeriod = Resource.lbl1Novto31Dec;
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            closedPeriod = Resource.lbl1Novto31Dec;
                        }
                        if (!isSandyShallowSoil)
                        {
                            closedPeriod = Resource.lbl1Novto31Dec;
                        }
                        break;

                    default:
                        closedPeriod = ClosedPeriodNonOrganicFarm(fieldDetail, harvestYear, isPerennial);

                        break;
                }
            }
            return closedPeriod;
        }

        public bool ClosedPeriodWarningMessage(DateTime applicationDate, string? closedPeriod, string cropType, FieldDetailResponse fieldDetail,bool IsRegisteredOrganic)
        {
            bool isWithinClosedPeriod = false;

            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern);
            if (closedPeriod != null)
            {
                Match match = regex.Match(closedPeriod);
                if (match.Success)
                {
                    int startDay = int.Parse(match.Groups[1].Value);
                    string startMonthStr = match.Groups[2].Value;
                    int endDay = int.Parse(match.Groups[3].Value);
                    string endMonthStr = match.Groups[4].Value;

                    Dictionary<int, string> dtfi = new Dictionary<int, string>();
                    dtfi.Add(0, "Jan");
                    dtfi.Add(1, "Feb");
                    dtfi.Add(2, "Mar");
                    dtfi.Add(3, "Apr");
                    dtfi.Add(4, "May");
                    dtfi.Add(5, "Jun");
                    dtfi.Add(6, "Jul");
                    dtfi.Add(7, "Aug");
                    dtfi.Add(8, "Sep");
                    dtfi.Add(9, "Oct");
                    dtfi.Add(10, "Nov");
                    dtfi.Add(11, "Dec");
                    int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1; // Array.IndexOf(dtfi.Values, startMonthStr) + 1;
                    int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;//Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;

                    DateTime closedPeriodStart = new DateTime(applicationDate.Year, startMonth, startDay);
                    DateTime closedPeriodEnd = new DateTime(applicationDate.Year, endMonth, endDay);

                    int applicationMonth = applicationDate.Month;
                    int applicationDay = applicationDate.Day;

                    if (startMonth < endMonth)
                    {
                        if (applicationMonth >= startMonth && applicationMonth <= endMonth)
                        {
                            if (applicationDate >= closedPeriodStart && applicationDate <= closedPeriodEnd)
                            {
                                isWithinClosedPeriod = true;
                            }
                        }
                    }
                    if (startMonth > endMonth)
                    {
                        if (applicationDate >= closedPeriodEnd)
                        {
                            DateTime closedPeriodEndNextYear = new DateTime(applicationDate.Year + 1, endMonth, endDay);
                            if (applicationDate >= closedPeriodStart && applicationDate <= closedPeriodEndNextYear)
                            {
                                isWithinClosedPeriod = true;
                            }
                        }
                        if (applicationDate <= closedPeriodEnd)
                        {
                            DateTime closedPeriodStartPreviousYear = new DateTime(applicationDate.Year - 1, startMonth, startDay);

                            if (applicationDate >= closedPeriodStartPreviousYear && applicationDate <= closedPeriodEnd)
                            {
                                isWithinClosedPeriod = true;
                            }
                        }


                    }
                    return isWithinClosedPeriod;
                }
            }
            
            return isWithinClosedPeriod;
        }

        public string EndClosedPeriodAndFebruaryWarningMessage(DateTime applicationDate, string? closedPeriod, decimal? applicationRate, bool isSlurry, bool isPoultryManure)
        {
            string message = string.Empty;
            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern);
            if (closedPeriod != null)
            {
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
                    string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                    DateTime? endDateFebruary = null;
                    endDateFebruary = new DateTime(applicationDate.Year, 3, 1);

                    DateTime ClosedPeriodEndDate = new DateTime(applicationDate.Year, endMonth, endDay);
                    DateTime endOfFebruaryDate = new DateTime(applicationDate.Year, endDateFebruary.Value.Month, endDateFebruary.Value.Day);


                    if (startMonth < endMonth)
                    {
                        DateTime ClosedPeriodEndDateMinusOne = new DateTime(applicationDate.Year - 1, endMonth, endDay);
                        if (applicationDate > ClosedPeriodEndDateMinusOne && applicationDate < endOfFebruaryDate)
                        {
                            if (isSlurry)
                            {
                                if (applicationRate > 30)
                                {
                                    message = string.Format(Resource.MsgApplicationRateForSlurryAndPoultryDetail, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                }
                            }
                            if (isPoultryManure)
                            {
                                if (applicationRate > 8)
                                {
                                    message = string.Format(Resource.MsgTheNVZActionProgrammeStatesThatTheARPoultry, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                }
                            }
                        }
                    }
                    if (startMonth > endMonth)
                    {
                        if (applicationDate > ClosedPeriodEndDate)
                        {
                            DateTime endOfFebruaryDatePlusOne = new DateTime(applicationDate.Year, endMonth, endDay);
                            if (applicationDate > ClosedPeriodEndDate && applicationDate < endOfFebruaryDatePlusOne)
                            {
                                if (isSlurry)
                                {
                                    if (applicationRate > 30)
                                    {
                                        message = string.Format(Resource.MsgApplicationRateForSlurryAndPoultryDetail, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                    }
                                }
                                if (isPoultryManure)
                                {
                                    if (applicationRate > 8)
                                    {
                                        message = string.Format(Resource.MsgTheNVZActionProgrammeStatesThatTheARPoultry, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                    }
                                }
                            }

                            DateTime ClosedPeriodEndDateMinusOne = new DateTime(applicationDate.Year - 1, startMonth, startDay);
                            if (applicationDate > ClosedPeriodEndDateMinusOne && applicationDate < endOfFebruaryDate)
                            {
                                if (isSlurry)
                                {
                                    if (applicationRate > 30)
                                    {
                                        message = string.Format(Resource.MsgApplicationRateForSlurryAndPoultryDetail, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                    }
                                }
                                if (isPoultryManure)
                                {
                                    if (applicationRate > 8)
                                    {
                                        message = string.Format(Resource.MsgTheNVZActionProgrammeStatesThatTheARPoultry, string.Format(Resource.lblEndClosedPeriod, endDay, endMonthFullName));
                                    }
                                }
                            }
                        }

                    }

                }
            }
            
            return message;
        }

        public string ClosedPeriodForFertiliserWarningMessage(DateTime applicationDate, int cropTypeId, string soilType, string cropType)
        {
            string message = string.Empty;
            int day = applicationDate.Day;
            int month = applicationDate.Month;
            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if (month > (int)NMP.Portal.Enums.Month.October)
                {
                    message = string.Format(Resource.MsgForGrassAndOilseedRapeClosedPeriodForFertiliser, cropType);
                }
            }
            else if (cropTypeId != (int)NMP.Portal.Enums.CropTypes.Asparagus && cropTypeId != (int)NMP.Portal.Enums.CropTypes.BrusselSprouts && cropTypeId != (int)NMP.Portal.Enums.CropTypes.Cabbage &&
                cropTypeId != (int)NMP.Portal.Enums.CropTypes.Cauliflower && cropTypeId != (int)NMP.Portal.Enums.CropTypes.Calabrese &&
                cropTypeId != (int)NMP.Portal.Enums.CropTypes.BulbOnions && cropTypeId != (int)NMP.Portal.Enums.CropTypes.SaladOnions)
            {
                if ((month >= (int)NMP.Portal.Enums.Month.September) || ((month == (int)NMP.Portal.Enums.Month.January && day <= 15)))
                {
                    message = string.Format(Resource.MsgForFertiliserClosedPeriodWarning, cropType, soilType, Resource.lblOneSepToFifteenJan);
                }
            }
            return message;
        }
        public string NitrogenLimitForFertiliserExceptBrassicasWarningMessage( int cropType, string cropTypeName, decimal totalNitrogen, decimal nitrogenOfSingleApp)
        {
            string message = string.Empty;

            if (cropType == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
            {
                if (totalNitrogen > 30.0m)
                {
                    message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, Resource.lblOneSep, Resource.lblThirtyFirstOct, 30);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Asparagus)
            {
                if (totalNitrogen > 50.0m)
                {
                    message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, Resource.lblOneSep, Resource.lblFifteenJan, 50);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.SaladOnions || cropType == (int)NMP.Portal.Enums.CropTypes.BulbOnions)
            {
                if (totalNitrogen > 40.0m)
                {
                    message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, Resource.lblOneSep, Resource.lblFifteenJan, 40);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if (totalNitrogen > 80.0m)
                {
                    message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, Resource.lblOneSep, Resource.lblThirtyFirstOct, 80);
                }
                if (nitrogenOfSingleApp > 40.0m)
                {
                    message = Resource.MsgForMaxNitrogenForFertiliserGrass;
                }
            }
            return message;
        }
        public string NitrogenLimitForFertiliserForBrassicasWarningMessage(decimal totalNitrogen, decimal nitrogenInOneDuration = 0, decimal singleApplicationNitrogen = 0)
        {
            string message = string.Empty;
            if (totalNitrogen > 100 || singleApplicationNitrogen > 50 || nitrogenInOneDuration > 50)
            {
                message = string.Format(Resource.MsgForMaxNitrogenForFertiliserForBrassicasWarningMsg, Resource.lblOneSep, Resource.lblEndOfFeb);
            }

            return message;
        }

        public bool? CheckEndClosedPeriodAndFebruary(DateTime applicationDate, string? closedPeriod)
        {
            bool? isWithinClosedPeriodAndFebruary = null;
            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern);
            if(closedPeriod != null)
            {
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
                    string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                    DateTime? endDateFebruary = null;
                    endDateFebruary = new DateTime(applicationDate.Year, 3, 1);

                    DateTime ClosedPeriodEndDate = new DateTime(applicationDate.Year, endMonth, endDay);
                    DateTime endOfFebruaryDate = new DateTime(applicationDate.Year, endDateFebruary.Value.Month, endDateFebruary.Value.Day);


                    if (startMonth < endMonth)
                    {
                        DateTime ClosedPeriodEndDateMinusOne = new DateTime(applicationDate.Year - 1, endMonth, endDay);
                        if (applicationDate > ClosedPeriodEndDateMinusOne && applicationDate < endOfFebruaryDate)
                        {
                            isWithinClosedPeriodAndFebruary = true;
                        }
                    }
                    if (startMonth > endMonth)
                    {
                        if (applicationDate > ClosedPeriodEndDate)
                        {
                            DateTime endOfFebruaryDatePlusOne = new DateTime(applicationDate.Year, endMonth, endDay);
                            if (applicationDate > ClosedPeriodEndDate && applicationDate < endOfFebruaryDatePlusOne)
                            {
                                isWithinClosedPeriodAndFebruary = true;
                            }

                            DateTime ClosedPeriodEndDateMinusOne = new DateTime(applicationDate.Year - 1, startMonth, startDay);
                            if (applicationDate > ClosedPeriodEndDateMinusOne && applicationDate < endOfFebruaryDate)
                            {
                                isWithinClosedPeriodAndFebruary = true;
                            }
                        }

                    }

                }
            }
            
            return isWithinClosedPeriodAndFebruary;
        }

        public string? WarningPeriodOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, int cropTypeId, int? cropInfo1, bool isPerennial)
        {
            string? WarningPeriod = null;
            DateTime september16 = new DateTime(harvestYear, 9, 16);

            var isSandyShallowSoil = fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.LightSand ||
                                     fieldDetail.SoilTypeID == (int)NMP.Portal.Enums.SoilTypeEngland.Shallow;
            var isFieldTypeGrass = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Grass;
            var isFieldTypeArable = fieldDetail.FieldType == (int)NMP.Portal.Enums.FieldType.Arable;
            DateTime? sowingDate = fieldDetail.SowingDate?.ToLocalTime();

            DateTime endDateFebruary = new DateTime(harvestYear, 2, 28);
            int lastDayOfFeb = endDateFebruary.Day;

            if (isFieldTypeGrass)
            {
                WarningPeriod = isSandyShallowSoil
                    ? Resource.lbl1Septo31Oct
                    : Resource.lbl15Octto31Oct;
            }
            else if (isFieldTypeArable)
            {
                switch (cropTypeId)
                {
                    case (int)NMP.Portal.Enums.CropTypes.Asparagus:             //Asparagus
                    case (int)NMP.Portal.Enums.CropTypes.BulbOnions:            //Bulb Onions
                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            WarningPeriod = string.Format(Resource.lbl1Augto28Feb, lastDayOfFeb);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = string.Format(Resource.lbl16Septo28Feb,lastDayOfFeb); 
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = string.Format(Resource.lbl1Octto28Feb, lastDayOfFeb);
                        }
                        break;

                    case (int)NMP.Portal.Enums.CropTypes.SaladOnions:            //Salad Onions
                        
                        if(cropInfo1 == 12)                                       // cropInfo1Id==12 for Overwintered
                        {
                            if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                            {
                                WarningPeriod =  string.Format(Resource.lbl1Augto28Feb, lastDayOfFeb);
                            }
                            if (isSandyShallowSoil && (sowingDate < september16))
                            {
                                WarningPeriod =  string.Format(Resource.lbl16Septo28Feb, lastDayOfFeb);
                            }
                            if (!isSandyShallowSoil)
                            {
                                WarningPeriod = string.Format(Resource.lbl1Octto28Feb, lastDayOfFeb);
                            }
                        }
                        break;

                    //Brassica is a crop group. under this below crop type comes..
                    case (int)NMP.Portal.Enums.CropTypes.BrusselSprouts:         //Brussel Sprouts
                    case (int)NMP.Portal.Enums.CropTypes.Cabbage:                //Cabbage
                    case (int)NMP.Portal.Enums.CropTypes.Cauliflower:            //Cauliflower
                    case (int)NMP.Portal.Enums.CropTypes.Calabrese:              //Calabrese
                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            WarningPeriod = string.Format(Resource.lbl1Augto28Feb, lastDayOfFeb);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = string.Format(Resource.lbl16Septo28Feb, lastDayOfFeb);
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = string.Format(Resource.lbl1Octto28Feb, lastDayOfFeb);
                        }

                        break;

                    case (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape:      // Winter oilseed rape

                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            WarningPeriod = Resource.lbl1Augto31Oct;
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = Resource.lbl16Septo31Oct;
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = Resource.lbl1Octto31Oct;
                        }
                        break;

                    default:
                        WarningPeriod = null;

                        break;
                }
            }
            return WarningPeriod;
        }

    }
}