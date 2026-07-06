using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep29ViewModel
    {
        public int? CropGroupId { get; set; }
        public int? ApplicationMethodId { get; set; }
        public int? IncorporationMethodId { get; set; }
        public string ManureTypeName { get; set; } = string.Empty;
        public int? ManureTypeId { get; set; }
        public string? OtherMaterialName { get; set; }
        public int? ApplicationRateMethod { get; set; }
    }
}
