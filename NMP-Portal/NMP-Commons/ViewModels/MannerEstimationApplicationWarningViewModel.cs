using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationApplicationWarningViewModel
    {
        public int? ApplicationId { get; set; }
        public List<WarningItemViewModel> Warnings { get; set; } = new List<WarningItemViewModel>();
    }
}
