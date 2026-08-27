using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationNWarningViewModel
    {
        public int? ManureTypeId { get; set; }
        public decimal? ApplicationRate { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int? MannerEstimationId { get; set; }
        public int? UpdatedMannerAppId { get; set; }

        public string EncryptedMannerApplicationsId { get; set; } = string.Empty;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;

        public int CountryId { get; set; }
        public int? CropTypeId { get; set; }
        public bool? IsFarmOrganic { get; set; }
        public bool? IsWithinNVZ { get; set; }
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


        public bool IsOrgManureNfieldLimitWarning { get; set; }

        public string? NFieldLimitWarningHeader { get; set; }
        public int? NFieldLimitWarningCodeID { get; set; }
        public int? NFieldLimitWarningLevelID { get; set; }
        public string? NFieldLimitWarningPara1 { get; set; }
        public string? NFieldLimitWarningPara2 { get; set; }
        public string? NFieldLimitWarningPara3 { get; set; }

        public bool IsEndClosedPeriodFebruaryWarning { get; set; }
        public string? EndClosedPeriodEndFebWarningHeader { get; set; }
        public int? EndClosedPeriodEndFebWarningCodeID { get; set; }
        public int? EndClosedPeriodEndFebWarningLevelID { get; set; }
        public string? EndClosedPeriodEndFebWarningPara1 { get; set; }
        public string? EndClosedPeriodEndFebWarningPara2 { get; set; }
        public string? EndClosedPeriodEndFebWarningPara3 { get; set; }

        public bool IsStartClosedPeriodEndFebWarning { get; set; }
        public string? StartClosedPeriodEndFebWarningHeader { get; set; }
        public int? StartClosedPeriodEndFebFebWarningCodeID { get; set; }
        public int? StartClosedPeriodEndFebWarningLevelID { get; set; }
        public string? StartClosedPeriodEndFebWarningPara1 { get; set; }
        public string? StartClosedPeriodEndFebWarningPara2 { get; set; }
        public string? StartClosedPeriodEndFebWarningPara3 { get; set; }

    }
}
