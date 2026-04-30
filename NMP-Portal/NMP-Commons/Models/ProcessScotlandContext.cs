using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class ProcessScotlandContext
    {
        // Report
        public ReportViewModel Model { get; set; }

        // Crop data
        public HarvestYearPlanResponse CropData { get; set; }
        public string PreviousCrop { get; set; }

        // Field data
        public List<FieldDetails> FieldDetails { get; set; }

        // NMax related
        public int NMaxLimitForCropType { get; set; }
        public List<NMaxLimitReportResponse> NMaxList { get; set; }
        public List<ScotlandNMaxValue>? ScotlandNMaxValues { get; set; }

        // Flags
        public bool IsAutumn { get; set; }
    }
}
