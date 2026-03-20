using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep13ViewModel
    {
        public DateTime? ApplicationDate { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ManureTypeName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public int FarmRB209CountryId { get; set; }
    }
}
