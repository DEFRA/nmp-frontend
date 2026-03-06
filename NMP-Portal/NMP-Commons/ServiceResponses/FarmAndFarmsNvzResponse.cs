using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class FarmAndFarmsNvzResponse
    {
        public FarmResponse Farm { get; set; }
        public List<FarmsNvz> FarmsNvz { get; set; }
    }
}
