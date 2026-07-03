using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep27ViewModel
    {
        public bool? IsManureTypeLiquid { get; set; }
        public int? ManureTypeId { get; set; }
        public string? ManureTypeName { get; set; }
        public decimal? ApplicationRate { get; set; }
        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsOrgManureNfieldLimitWarning { get; set; } = false;

        public string? NFieldLimitWarningHeader { get; set; } = string.Empty;
        public string? NFieldLimitWarningPara1 { get; set; } = string.Empty;
        public string? NFieldLimitWarningPara2 { get; set; } = string.Empty;
        public string? NFieldLimitWarningPara3 { get; set; } = string.Empty;
        public int NFieldLimitWarningCodeID { get; set; }
        public int NFieldLimitWarningLevelID { get; set; }

        public int CountryId { get; set; }
        public int FarmRB209CountryId { get; set; }
        public int? CropTypeId { get; set; }
        public int? CropGroupId { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int? ManureGroupId { get; set; }
        public int? MannerEstimationId { get; set; }
        public int? MannerEstimationApplicationsId { get; set; }

        public int? UpdatedMannerAppId { get; set; }
    }
}
