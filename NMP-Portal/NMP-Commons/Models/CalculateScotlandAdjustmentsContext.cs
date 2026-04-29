using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class CalculateScotlandAdjustmentsContext
    {
        public Crop Crop { get; set; }
        public Field Field { get; set; }
        public ExcessRainfalls? Rain { get; set; }
        public Recommendation Recommendation { get; set; }
        public ReportViewModel Model { get; set; }

        public decimal? DefaultYield { get; set; }
        public List<ScotlandNMaxValue>? ScotlandNMaxValues { get; set; }

        public bool IsAutumn { get; set; }
    }
}
