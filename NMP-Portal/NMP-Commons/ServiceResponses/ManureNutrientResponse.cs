using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class ManureNutrientResponse
    {
        public int id { get; set; }
        public decimal dryMatter { get; set; }
        public decimal totalN { get; set; }
        public decimal nH4N { get; set; }
        public decimal uric { get; set; }
        public decimal nO3N { get; set; }
        public decimal p2O5 { get; set; }
        public decimal k2O { get; set; }
        public decimal sO3 { get; set; }
        public decimal mgO { get; set; }
    }
}
