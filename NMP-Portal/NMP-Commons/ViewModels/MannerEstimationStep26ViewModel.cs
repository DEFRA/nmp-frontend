using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep26ViewModel: MannerEstimationNWarningViewModel
    {
        public int? ApplicationRateMethod { get; set; }
        public bool? IsManureTypeLiquid { get; set; }
        public string? ManureTypeName { get; set; }
        public int? ApplicationRateArable { get; set; }
        public string? OtherMaterialName { get; set; }

        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public int? ManureGroupId { get; set; }
        public int? MannerEstimationApplicationsId { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;

    }
}
