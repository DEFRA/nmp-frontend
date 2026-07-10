using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep30ViewModel
    {
        public int? IncorporationDelayId { get; set; }
        public int? IncorporationMethodId { get; set; }
        public string ManureTypeName { get; set; } = string.Empty;
        public int? ManureTypeId { get; set; }
        public string? OtherMaterialName { get; set; }
        public bool IsIncorporationMethodChange { get; set; } = false;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;

    }
}
