using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep9ViewModel
    {
        public int? CropTypeId { get; set; }
        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public string CropGroupName { get; set; } = string.Empty;
        public string CropTypeName { get; set; } = string.Empty;
        public int? MannerCropTypeId { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsCropGroupChange { get; set; } = false;
        public bool IsCropTypeChange { get; set; } = false;
    }
}
