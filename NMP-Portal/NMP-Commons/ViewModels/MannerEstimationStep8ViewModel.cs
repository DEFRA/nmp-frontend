using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep8ViewModel
    {
        public int? CropGroupId { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public string CropGroupName { get; set; } = string.Empty;
        public bool IsFarmCopied { get; set; } = false;
        public bool IsCropGroupChange { get; set; } = false;
    }
}
