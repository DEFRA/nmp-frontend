using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class SoilMethologiesResponse
    {
        public int? nutrientId { get; set; }
        public int? methodologyId { get; set; }
        public string? methodology { get; set; }
    }
}
