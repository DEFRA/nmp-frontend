using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class MannerFarmEstimationApplicationResponse
    {
        public MannerFarm? MannerFarm { get; set; }

        public MannerEstimation? MannerEstimation { get; set; }

        public MannerEstimationApplication? MannerEstimationApplication { get; set; }
    }
}
