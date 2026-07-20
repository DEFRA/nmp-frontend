using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep20ViewModel
    {
        public string? FieldName { get; set; }
        public string? CropTypeName { get; set; }
        public DateTime? SowingDate { get; set; }
        public bool IsCropTypeChange { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
    }
}
