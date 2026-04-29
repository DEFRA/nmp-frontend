using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class ScotlandAdjustmentContext
    {
        public Crop Crop { get; set; }
        public int? WinterRainfall { get; set; }
        public int? NResidueGroup { get; set; }
        public int SoilTypeId { get; set; }
        public decimal? StandardYield { get; set; }
        public List<ScotlandNMaxValue>? ScotlandNMaxValues { get; set; }
        public int FarmId { get; set; }
        public bool IsAutumn { get; set; }
    }
}
