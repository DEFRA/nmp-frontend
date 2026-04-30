using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class BindNmaxResponseForScotlandContext
    {
        // Report
        public ReportViewModel Model { get; set; }

        // Crop-related
        public Crop Crop { get; set; }
        public string PreviousCrop { get; set; }
        public decimal? DefaultYield { get; set; }

        // Field-related
        public Field Field { get; set; }
        public List<FieldDetails> FieldDetails { get; set; }

        // Calculation inputs
        public List<ScotlandNMaxValue>? ScotlandNMaxValues { get; set; }
        public bool IsAutumn { get; set; }
    }
}
