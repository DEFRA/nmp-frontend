using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep18ViewModel
    {
        public int? CountryId { get; set; }
        public int? TopSoilId { get; set; }
        public string? FieldName { get; set; }
        public string? EncryptedMannerEstimateId { get; set; } = string.Empty;
    }
}
