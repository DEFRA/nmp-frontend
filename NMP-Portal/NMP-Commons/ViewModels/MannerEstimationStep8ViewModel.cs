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
        public bool IsCheckAnswer { get; set; } = false;
        public string CropGroupName { get; set; } = string.Empty;
    }
}
