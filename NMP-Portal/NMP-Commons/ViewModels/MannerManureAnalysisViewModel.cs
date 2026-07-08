using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerManureAnalysisViewModel
    {
        public decimal? DryMatterContent { get; set; }
        public decimal? TotalNitrogen { get; set; }
        public decimal? AmmoniumNitrogen { get; set; }
        public decimal? UricAcidNitrogen { get; set; }
        public decimal? NitrateNitrogen { get; set; }
        public decimal? TotalPhosphate { get; set; }
        public decimal? TotalPotash { get; set; }
        public decimal? TotalSulphur { get; set; }
        public decimal? TotalMagnesium { get; set; }
    }
}
