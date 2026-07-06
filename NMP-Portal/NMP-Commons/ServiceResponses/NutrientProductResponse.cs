using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class NutrientProductResponse
    {
         public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public int nutrientID { get; set; }
        public decimal nutrientPercentage { get; set; }
        public bool isNutrientDefaultProduct { get; set; }
        public string measurementUnit { get; set; } = string.Empty;
    }
}