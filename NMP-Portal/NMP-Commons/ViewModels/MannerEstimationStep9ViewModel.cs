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
        public bool IsCheckAnswer { get; set; } = false;
        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public string CropGroupName { get; set; } = string.Empty;
    }
}
