using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationReportViewModel
    {
        public MannerEstimationReportViewModel()
        {
            MannerFieldAndCropDetails = new MannerEstimationDetailsViewModel();
            MannerEstimationApplicationDetails = new List<MannerEstimationApplicationDetailsViewModel>();
            MannerEstimationConditions = new List<MannerEstimationStep32ViewModel>();
            ManureAnalyses = new List<MannerManureAnalysisViewModel>();
            MannerNpkResults = new List<MannerNpkResultViewModel>();
            ApplicationWarnings = new List<MannerEstimationApplicationWarningViewModel>();
        }
        public int? FarmRB209CountryID { get; set; }
        public string? EncryptedMannerEstimateId { get; set; }
        public MannerEstimationDetailsViewModel MannerFieldAndCropDetails { get; set; }
        public List<MannerEstimationApplicationDetailsViewModel> MannerEstimationApplicationDetails { get; set; }

        public List<MannerEstimationStep32ViewModel> MannerEstimationConditions { get; set; }
        public List<MannerManureAnalysisViewModel> ManureAnalyses { get; set; }
        public List<MannerNpkResultViewModel> MannerNpkResults { get; set; }
        public List<MannerEstimationApplicationWarningViewModel> ApplicationWarnings { get; set; } 
    }
}
