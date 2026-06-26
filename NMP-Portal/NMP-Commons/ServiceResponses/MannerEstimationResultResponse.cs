using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class MannerEstimationResultResponse
    {
        public MannerEstimation? MannerEstimation { get; set; }
        public List<MannerEstimationApplication>? MannerApplications { get; set; }
    }
}
