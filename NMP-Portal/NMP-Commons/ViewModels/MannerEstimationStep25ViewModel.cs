using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep25ViewModel
    {
        public bool IsCalculateBasedOnDryMatter { get; set; } = false;
        public string? OtherMaterialName { get; set; }
        public bool? IsManureTypeLiquid { get; set; }
        public int? ManureTypeId { get; set; }
        public string? ManureTypeName { get; set; }
        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? DryMatterPercent { get; set; }
        public decimal? UricAcid { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsDefaultValueChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;
    }
}
