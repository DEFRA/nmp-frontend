using NMP.Commons.Models;
using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class MannerEstimationResultResponse
    {
        public MannerEstimationDetailsViewModel? MannerEstimation { get; set; }
        public List<MannerEstimationApplicationDetailsViewModel>? MannerEstimationApplication { get; set; }
        public DateTime? LastUpdatedOn { get; set; }

    }
}
