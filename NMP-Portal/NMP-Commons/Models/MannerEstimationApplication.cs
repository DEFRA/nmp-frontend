using NMP.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class MannerEstimationApplication
    {
        public int? ID { get; set; }
        public int? MannerEstimationID { get; set; }
        public int? ManureTypeID { get; set; }
        public DateTime ApplicationDate { get; set; }
        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? DryMatterPercent { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }

        public decimal UricAcid { get; set; }
        public decimal? ApplicationRate { get; set; }
        public decimal? AreaSpread { get; set; }

        public decimal? ManureQuantity { get; set; }
        public int? ApplicationMethodID { get; set; }
        public int? IncorporationMethodID { get; set; }
        public int? IncorporationDelayID { get; set; }
        public int? WindspeedID { get; set; }
        public int? RainfallWithinSixHoursID { get; set; }
        public int? MoistureID { get; set; }
        public int? AutumnCropNitrogenUptake { get; set; }
        public DateTime? EndOfDrainageDate { get; set; }
        public int? RainfallPostApplication { get; set; }
        public int TotalN { get; set; } = 0;
        public int CropAvailableNCurrentCrop { get; set; } = 0;
        public int CropAvailableNitrogenFollowingCropYearTwo { get; set; } = 0;
        public int NextGrassNitrogenCropCurrentYear { get; set; } = 0;
        public int TotalP2O5 { get; set; } = 0;
        public int CropAvailableP2O5 { get; set; } = 0;
        public int TotalSO3 { get; set; } = 0;
        public int TotalMgO { get; set; } = 0;
        public int TotalK2O { get; set; } = 0;
        public int CropAvailableK2O { get; set; } = 0;
        public int CropAvailableSO3 { get; set; } = 0;
        public int NitrogenUseEfficiency { get; set; } = 0;
        public int MineralisedNitrogenLosses { get; set; } = 0;
        public int LostNitrateLosses { get; set; } = 0;
        public int LostAmmonia { get; set; } = 0;
        public int LostDenitrified { get; set; } = 0;
        public int NitrogenValue { get; set; }
        public int PhosphateValue { get; set; }
        public int PotashValue { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }

    }
}
