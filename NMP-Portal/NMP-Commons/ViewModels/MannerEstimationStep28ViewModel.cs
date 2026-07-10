using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep28ViewModel : MannerEstimationNWarningViewModel
    {
        public bool? IsManureTypeLiquid { get; set; }
        public string? ManureTypeName { get; set; }
        public decimal? AreaSpread { get; set; }
        public decimal? ManureQuantity { get; set; }
        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public int? ManureGroupId { get; set; }
        public int? MannerEstimationApplicationsId { get; set; }
        public bool IsApplicationRateMethodChange { get; set; } = false;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;

    }
}
