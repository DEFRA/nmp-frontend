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
        public int? ManureQuantity { get; set; }
        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public int? ManureGroupId { get; set; }
        public bool IsApplicationRateMethodChange { get; set; } = false;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;

    }
}
