using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class SoilNutrientIndiceResponse
    {
        public int? indexId { get; set; }
        public string index { get; set; } = string.Empty;
        public double? minRange { get; set; }
        public double? maxRange { get; set; }
    }
}
