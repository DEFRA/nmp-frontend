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
        public bool IsWarningMsgNeedToShow { get; set; } = false;

        public bool IsOrgManureNfieldLimitWarning { get; set; }

        public string? NFieldLimitWarningHeader { get; set; }
        public int? NFieldLimitWarningCodeID { get; set; }
        public int? NFieldLimitWarningLevelID { get; set; }
        public string? NFieldLimitWarningPara1 { get; set; }
        public string? NFieldLimitWarningPara2 { get; set; }
        public string? NFieldLimitWarningPara3 { get; set; }
    }
}
