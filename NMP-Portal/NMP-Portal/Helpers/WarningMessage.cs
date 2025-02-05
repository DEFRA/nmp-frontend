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
                closedPeriod = string.Format(Resource.lbl1Septo31Dec,Resource.lblSeptember,Resource.lblDecember);
            }
            else if (!isSandyShallowSoil && isFieldTypeGrass)
            {
                closedPeriod = string.Format(Resource.lbl15Octto31Jan,Resource.lblOctober, Resource.lblJanuary);
            }
            if (!isPerennial)
            {
                if (isSandyShallowSoil && isFieldTypeArable && (fieldDetail.SowingDate >= september16 || fieldDetail.SowingDate == null))
                {
                    closedPeriod = string.Format(Resource.lbl1Augto31Dec,Resource.lblAugust,Resource.lblDecember);
                }
                else if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate < september16)
                {
                    closedPeriod = string.Format(Resource.lbl16Septo31Dec,Resource.lblSeptember,Resource.lblDecember);
                }
                else if (isFieldTypeArable && !isSandyShallowSoil)
                {
                    closedPeriod = string.Format(Resource.lbl1Octto31Jan,Resource.lblOctober,Resource.lblJanuary);
                }
            }
            else
            {
                if (isSandyShallowSoil && isFieldTypeArable && (fieldDetail.SowingDate >= september16 || fieldDetail.SowingDate == null))
                {
                    closedPeriod = string.Format(Resource.lbl1Augto31Dec,Resource.lblAugust,Resource.lblDecember);
                }
                else if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate < september16)
                {
                    closedPeriod = string.Format(Resource.lbl16Septo31Dec,Resource.lblSeptember,Resource.lblDecember);
                }
                else if (isFieldTypeArable && !isSandyShallowSoil)
                {
                    closedPeriod = string.Format(Resource.lbl1Octto31Jan,Resource.lblOctober,Resource.lblJanuary);
                }
                if (fieldDetail.SowingDate != null)
                {
                    if (isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                    {
                        closedPeriod = string.Format(Resource.lbl16Septo31Dec,Resource.lblSeptember,Resource.lblDecember);
                    }
                    else if (!isSandyShallowSoil && isFieldTypeArable && fieldDetail.SowingDate.Value.Year < harvestYear)
                    {
                        closedPeriod = string.Format(Resource.lbl1Octto31Jan,Resource.lblOctober,Resource.lblJanuary);
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
                    ? string.Format(Resource.lbl1Novto31Dec,Resource.lblNovember,Resource.lblDecember)
                    : string.Format(Resource.lbl1Novto15Jan,Resource.lblNovember,Resource.lblJanuary);
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
                            closedPeriod = string.Format(Resource.lbl1Novto31Dec,Resource.lblNovember,Resource.lblDecember);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            closedPeriod = string.Format(Resource.lbl1Novto31Dec,Resource.lblNovember,Resource.lblDecember);
                        }
                        if (!isSandyShallowSoil)
                        {
                            closedPeriod = string.Format(Resource.lbl1Novto31Dec, Resource.lblNovember, Resource.lblDecember);
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
                    dtfi.Add(0, Resource.lblJanuary);
                    dtfi.Add(1, Resource.lblFebruary);
                    dtfi.Add(2, Resource.lblMarch);
                    dtfi.Add(3, Resource.lblApril);
                    dtfi.Add(4, Resource.lblMay);
                    dtfi.Add(5, Resource.lblJune);
                    dtfi.Add(6, Resource.lblJuly);
                    dtfi.Add(7, Resource.lblAugust);
                    dtfi.Add(8, Resource.lblSeptember);
                    dtfi.Add(9, Resource.lblOctober);
                    dtfi.Add(10, Resource.lblNovember);
                    dtfi.Add(11, Resource.lblDecember);
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

                    //DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
                    Dictionary<int, string> dtfi = new Dictionary<int, string>();
                    dtfi.Add(0, Resource.lblJanuary);
                    dtfi.Add(1, Resource.lblFebruary);
                    dtfi.Add(2, Resource.lblMarch);
                    dtfi.Add(3, Resource.lblApril);
                    dtfi.Add(4, Resource.lblMay);
                    dtfi.Add(5, Resource.lblJune);
                    dtfi.Add(6, Resource.lblJuly);
                    dtfi.Add(7, Resource.lblAugust);
                    dtfi.Add(8, Resource.lblSeptember);
                    dtfi.Add(9, Resource.lblOctober);
                    dtfi.Add(10, Resource.lblNovember);
                    dtfi.Add(11, Resource.lblDecember);
                    int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1;
                    int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;
                    string endMonthFullName = dtfi[endMonth - 1];

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

                    //DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
                    //int startMonth = Array.IndexOf(dtfi.MonthNames, startMonthStr) + 1;
                    //int endMonth = Array.IndexOf(dtfi.MonthNames, endMonthStr) + 1;
                    //string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                    Dictionary<int, string> dtfi = new Dictionary<int, string>();
                    dtfi.Add(0, Resource.lblJanuary);
                    dtfi.Add(1, Resource.lblFebruary);
                    dtfi.Add(2, Resource.lblMarch);
                    dtfi.Add(3, Resource.lblApril);
                    dtfi.Add(4, Resource.lblMay);
                    dtfi.Add(5, Resource.lblJune);
                    dtfi.Add(6, Resource.lblJuly);
                    dtfi.Add(7, Resource.lblAugust);
                    dtfi.Add(8, Resource.lblSeptember);
                    dtfi.Add(9, Resource.lblOctober);
                    dtfi.Add(10, Resource.lblNovember);
                    dtfi.Add(11, Resource.lblDecember);
                    int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1;
                    int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;
                    string endMonthFullName = dtfi[endMonth - 1];

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
                    ? string.Format(Resource.lbl1Septo31Oct,Resource.lblSeptember,Resource.lblOctober)
                    : string.Format(Resource.lbl15Octto31Oct,Resource.lblOctober,Resource.lblOctober);
            }
            else if (isFieldTypeArable)
            {
                switch (cropTypeId)
                {
                    case (int)NMP.Portal.Enums.CropTypes.Asparagus:             //Asparagus
                    case (int)NMP.Portal.Enums.CropTypes.BulbOnions:            //Bulb Onions
                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            WarningPeriod = string.Format(Resource.lbl1Augto28Feb,Resource.lblAugust, lastDayOfFeb, Resource.lblFebruary);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = string.Format(Resource.lbl16Septo28Feb,Resource.lblSeptember,lastDayOfFeb,Resource.lblFebruary); 
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = string.Format(Resource.lbl1Octto28Feb,Resource.lblOctober ,lastDayOfFeb,Resource.lblFebruary);
                        }
                        break;

                    case (int)NMP.Portal.Enums.CropTypes.SaladOnions:            //Salad Onions
                        
                        if(cropInfo1 == 12)                                       // cropInfo1Id==12 for Overwintered
                        {
                            if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                            {
                                WarningPeriod =  string.Format(Resource.lbl1Augto28Feb,Resource.lblAugust ,lastDayOfFeb, Resource.lblFebruary);
                            }
                            if (isSandyShallowSoil && (sowingDate < september16))
                            {
                                WarningPeriod =  string.Format(Resource.lbl16Septo28Feb, Resource.lblSeptember,lastDayOfFeb, Resource.lblFebruary);
                            }
                            if (!isSandyShallowSoil)
                            {
                                WarningPeriod = string.Format(Resource.lbl1Octto28Feb,Resource.lblOctober ,lastDayOfFeb,Resource.lblFebruary);
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
                            WarningPeriod = string.Format(Resource.lbl1Augto28Feb,Resource.lblAugust ,lastDayOfFeb, Resource.lblFebruary);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = string.Format(Resource.lbl16Septo28Feb, Resource.lblSeptember ,lastDayOfFeb, Resource.lblFebruary);
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = string.Format(Resource.lbl1Octto28Feb,Resource.lblOctober ,lastDayOfFeb, Resource.lblFebruary);
                        }

                        break;

                    case (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape:      // Winter oilseed rape

                        if (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null))
                        {
                            WarningPeriod = string.Format(Resource.lbl1Augto31Oct,Resource.lblAugust,Resource.lblOctober);
                        }
                        if (isSandyShallowSoil && (sowingDate < september16))
                        {
                            WarningPeriod = string.Format(Resource.lbl16Septo31Oct,Resource.lblSeptember,Resource.lblOctober);
                        }
                        if (!isSandyShallowSoil)
                        {
                            WarningPeriod = string.Format(Resource.lbl1Octto31Oct,Resource.lblOctober,Resource.lblOctober);
                        }
                        break;

                    default:
                        WarningPeriod = null;

                        break;
                }
            }
            return WarningPeriod;
        }

        public string? ClosedPeriodForFertiliser(int cropTypeId)
        {
            string? closedPeriod = null;
            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                closedPeriod = string.Format(Resource.lbl15SeptemberTo15January, Resource.lblSeptember, Resource.lblJanuary);

            }
            else
            {
                closedPeriod = string.Format(Resource.lbl1SeptemberTo15January, Resource.lblSeptember, Resource.lblJanuary);

            }

            return closedPeriod;

        }
        public bool IsFertiliserApplicationWithinWarningPeriod(DateTime applicationDate, string warningPeriod)
        {
            bool isWithinWarningPeriod = false;

            string pattern = @"(\d{1,2})\s(\w+)\s*to\s*(\d{1,2})\s(\w+)";
            Regex regex = new Regex(pattern);
            if (warningPeriod != null)
            {
                Match match = regex.Match(warningPeriod);
                if (match.Success)
                {
                    int startDay = int.Parse(match.Groups[1].Value);
                    string startMonthStr = match.Groups[2].Value;
                    int endDay = int.Parse(match.Groups[3].Value);
                    string endMonthStr = match.Groups[4].Value;

                    Dictionary<int, string> dtfi = new Dictionary<int, string>();
                    dtfi.Add(0, Resource.lblJanuary);
                    dtfi.Add(1, Resource.lblFebruary);
                    dtfi.Add(2, Resource.lblMarch);
                    dtfi.Add(3, Resource.lblApril);
                    dtfi.Add(4, Resource.lblMay);
                    dtfi.Add(5, Resource.lblJune);
                    dtfi.Add(6, Resource.lblJuly);
                    dtfi.Add(7, Resource.lblAugust);
                    dtfi.Add(8, Resource.lblSeptember);
                    dtfi.Add(9, Resource.lblOctober);
                    dtfi.Add(10, Resource.lblNovember);
                    dtfi.Add(11, Resource.lblDecember);
                    int startMonth = dtfi.FirstOrDefault(v => v.Value == startMonthStr).Key + 1; // Array.IndexOf(dtfi.Values, startMonthStr) + 1;
                    int endMonth = dtfi.FirstOrDefault(v => v.Value == endMonthStr).Key + 1;//Array.IndexOf(dtfi.AbbreviatedMonthNames, endMonthStr) + 1;

                    DateTime warningPeriodStart = new DateTime(applicationDate.Year, startMonth, startDay);
                    DateTime warningPeriodEnd = new DateTime(applicationDate.Year, endMonth, endDay);

                    int applicationMonth = applicationDate.Month;
                    int applicationDay = applicationDate.Day;

                    if (startMonth < endMonth)
                    {
                        if (applicationMonth >= startMonth && applicationMonth <= endMonth)
                        {
                            if (applicationDate >= warningPeriodStart && applicationDate <= warningPeriodEnd)
                            {
                                isWithinWarningPeriod = true;
                            }
                        }
                    }
                    if (startMonth > endMonth)
                    {
                        if (applicationDate >= warningPeriodEnd)
                        {
                            DateTime closedPeriodEndNextYear = new DateTime(applicationDate.Year + 1, endMonth, endDay);
                            if (applicationDate >= warningPeriodStart && applicationDate <= closedPeriodEndNextYear)
                            {
                                isWithinWarningPeriod = true;
                            }
                        }
                        if (applicationDate <= warningPeriodEnd)
                        {
                            DateTime closedPeriodStartPreviousYear = new DateTime(applicationDate.Year - 1, startMonth, startDay);

                            if (applicationDate >= closedPeriodStartPreviousYear && applicationDate <= warningPeriodEnd)
                            {
                                isWithinWarningPeriod = true;
                            }
                        }


                    }
                    return isWithinWarningPeriod;
                }
            }

            return isWithinWarningPeriod;
        }

    }
}