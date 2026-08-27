using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerNpkResultViewModel
    {
        // Total and crop available nitrogen
        public decimal? TotalNitrogen { get; set; }
        public decimal? CropAvailableNitrogenCurrentCrop { get; set; }
        public decimal? CropAvailableNitrogenFollowingCropYear2 { get; set; }
        public decimal? NitrogenUseEfficiency { get; set; }

        // Nitrogen mineralization and losses
        public decimal? MineralisedNitrogen { get; set; }
        public decimal? LostNitrateNitrogen { get; set; }
        public decimal? LostAmmoniaNitrogen { get; set; }
        public decimal? LostDenitrifiedNitrogen { get; set; }

        // Phosphate, potash, sulphur and magnesium
        public decimal? TotalPhosphate { get; set; }
        public decimal? CropAvailablePhosphate { get; set; }
        public decimal? TotalPotash { get; set; }
        public decimal? CropAvailablePotash { get; set; }
        public decimal? TotalSulphur { get; set; }
        public decimal? CropAvailableSulphur { get; set; }
        public decimal? TotalMagnesium { get; set; }

        // Organic material value
        public decimal? PotentialFinancialValuePerHectare { get; set; }

        // Value breakdown
        public decimal? NitrogenValue { get; set; }
        public decimal? PhosphateValue { get; set; }
        public decimal? PotashValue { get; set; }
        public decimal? TotalValue { get; set; }
    }
}
