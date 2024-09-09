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

        public string? ClosedPeriodOrganicFarm(FieldDetailResponse fieldDetail, int harvestYear, int cropTypeId, int? cropInfo1)
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

                        closedPeriod = (isSandyShallowSoil && (sowingDate >= september16 || sowingDate == null)) ||
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

        public bool ClosedPeriodWarningMessage(DateTime applicationDate, string closedPeriod, string cropType, FieldDetailResponse fieldDetail,bool IsRegisteredOrganic)
        {
            bool isWithinClosedPeriod = false;

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
                            //message = string.Format(IsRegisteredOrganic? Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetailOrganic : Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetail, cropType, fieldDetail.SowingDate == null ? "" : fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);
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
                            //message = string.Format(Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetail, cropType, fieldDetail.SowingDate == null ? "" : fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);
                        }
                    }
                    if (applicationDate <= closedPeriodEnd)
                    {
                        DateTime closedPeriodStartPreviousYear = new DateTime(applicationDate.Year - 1, startMonth, startDay);

                        if (applicationDate >= closedPeriodStartPreviousYear && applicationDate <= closedPeriodEnd)
                        {
                            isWithinClosedPeriod = true;
                            //message = string.Format(Resource.MsgApplicationDateEnteredIsInsideClosedPeriodDetail, cropType, fieldDetail.SowingDate == null ? "" : fieldDetail.SowingDate.Value.Date.ToString("dd MMM yyyy"), fieldDetail.SoilTypeName, closedPeriod);
                        }
                    }
                    
                    
                }
                return isWithinClosedPeriod;
            }
            return isWithinClosedPeriod;
        }

        public string EndClosedPeriodAndFebruaryWarningMessage(DateTime applicationDate, string closedPeriod, decimal? applicationRate, bool isSlurry, bool isPoultryManure)
        {
            string message = string.Empty;
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
                string endMonthFullName = dtfi.MonthNames[endMonth - 1];

                DateTime? endDateFebruary = null;
                endDateFebruary = new DateTime(applicationDate.Year, 3, 1);

                DateTime ClosedPeriodEndDate = new DateTime(applicationDate.Year, endMonth, endDay);
                DateTime endOfFebruaryDate = new DateTime(applicationDate.Year, endDateFebruary.Value.Month, endDateFebruary.Value.Day);


                if (startMonth < endMonth)
                {
                    DateTime ClosedPeriodEndDateMinusOne= new DateTime(applicationDate.Year - 1, endMonth, endDay);
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
                        DateTime endOfFebruaryDatePlusOne = new DateTime(applicationDate.Year , endMonth, endDay);
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
                    message = string.Format(Resource.MsgForFertiliserClosedPeriodWarning, cropType, soilType, "01 Sep to 15 Jan");
                }
            }
            return message;
        }
        public string NitrogenLimitForFertiliserExceptBrassicasWarningMessage(DateTime startDate, DateTime endDate, int cropType, string cropTypeName, decimal totalNitrogen, decimal nitrogenOfSingleApp)
        {
            string message = string.Empty;

            if (cropType == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
            {
                if (totalNitrogen > 30.0m)
                {
                    //message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, startDate.ToString("dd/MMM/yyyy"), endDate.ToString("dd/MMM/yyyy"), nitrogenOfSingleApp);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Asparagus)
            {
                if (totalNitrogen > 50.0m)
                {
                    // message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, startDate.ToString("dd/MMM/yyyy"), endDate.ToString("dd/MMM/yyyy"), nitrogenOfSingleApp);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.SaladOnions || cropType == (int)NMP.Portal.Enums.CropTypes.BulbOnions)
            {
                if (totalNitrogen > 40.0m)
                {
                    //message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, startDate.ToString("dd/MMM/yyyy"), endDate.ToString("dd/MMM/yyyy"), nitrogenOfSingleApp);
                }
            }
            else if (cropType == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if (totalNitrogen > 80.0m)
                {
                    //message = string.Format(Resource.MsgForMaxNitrogenForFertiliserExceptBrassicas, cropTypeName, startDate.ToString("dd/MMM/yyyy"), endDate.ToString("dd/MMM/yyyy"), nitrogenOfSingleApp);
                }
                if (nitrogenOfSingleApp > 40.0m)
                {
                    //message = Resource.MsgForFertiliserForGrassCropType;
                }
            }
            return message;
        }
        public string NitrogenLimitForFertiliserForBrassicasWarningMessage(DateTime startDate, DateTime endDate, string cropTypeName, decimal totalNitrogen, decimal fourWeekNitrogen = 0)
        {
            string message = string.Empty;
            if (totalNitrogen > 100)
            {
                //message = string.Format(Resource.MsgForMaxNitrogenForFertiliserForBrassicas, startDate.ToString("dd/MMM/yyyy"), endDate.ToString("dd/MMM/yyyy"), Resource.MsgForMaxNitrogenForFertiliserForBrassicasAdditionalMsg);
            }

            else if (totalNitrogen > 50 || fourWeekNitrogen + totalNitrogen > 50)
            {
                // Exceeds 50kg limit in any 4-week period
            }

            return message;
        }

        public bool? CheckEndClosedPeriodAndFebruary(DateTime applicationDate, string closedPeriod)
        {
            bool? isWithinClosedPeriodAndFebruary = null;
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
            return isWithinClosedPeriodAndFebruary;
        }

    }
}