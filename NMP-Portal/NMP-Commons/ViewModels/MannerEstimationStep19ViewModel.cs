using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep19ViewModel
    {
        public int? SubSoilId { get; set; }
        public string? FieldName { get; set; }
        public string? EncryptedMannerEstimateId { get; set; } = string.Empty;
        public string? EncryptedSoilTypeChangeCounter { get; set; }
        public int? ApplicationNo { get; set; }
        public bool IsTopSoilChange { get; set; } = false;
    }
}
