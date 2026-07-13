using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep13ViewModel
    {
        public DateTime? ApplicationDate { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ManureTypeName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public int FarmRB209CountryId { get; set; }
        public int? CropTypeId { get; set; }
        public int? CropGroupId { get; set; }
        public int? TopSoilId { get; set; }
        public int? SubSoilId { get; set; }
        public DateTime? SowingDate { get; set; }
        public bool? IsWithinNVZ { get; set; }

        public bool? IsFarmOrganic { get; set; }
        public int? ManureTypeId { get; set; }
        public int? ManureGroupId { get; set; }

        public string? ClosedPeriod { get; set; }

        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsClosedPeriodWarning { get; set; } = false;
        public string? ClosedPeriodWarningHeader { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara1 { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara2 { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara3 { get; set; } = string.Empty;
        public int ClosedPeriodWarningCodeID { get; set; }
        public int ClosedPeriodWarningLevelID { get; set; }


        public bool IsApplicationJulyToSeptWarning { get; set; } = false;
        public string? ApplicationJulyToSeptPara1 { get; set; } = string.Empty;
        public string? ApplicationJulyToSeptPara2 { get; set; } = string.Empty;
        public string? ApplicationJulyToSeptPara3 { get; set; } = string.Empty;
        public string? ApplicationJulyToSeptHeader { get; set; } = string.Empty;
        public int ApplicationJulyToSeptCodeID { get; set; }
        public int ApplicationJulyToSeptLevelID { get; set; }


        public bool IsEndClosedPeriodFebruaryExistWithinThreeWeeks { get; set; } = false;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 { get; set; } = string.Empty;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 { get; set; } = string.Empty;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksPara3 { get; set; } = string.Empty;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksHeader { get; set; } = string.Empty;
        public int EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID { get; set; }
        public int EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID { get; set; }

        public int? MannerEstimationId { get; set; }
        public int? MannerEstimationApplicationsId { get; set; }
        public bool IsApplicationDateChange { get; set; } = false;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;

    }
}
