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

        public decimal UricAcid { get; set; }
        public decimal? ApplicationRate { get; set; }
        public decimal? AreaSpread { get; set; }

        public decimal? ManureQuantity { get; set; }
        public int? IncorporationMethodID { get; set; }
        public int? IncorporationDelayID { get; set; }
        public int? WindspeedID { get; set; }
        public int? RainfallWithinSixHoursID { get; set; }
        public int? MoistureID { get; set; }
        public int? AutumnCropNitrogenUptake { get; set; }
        public DateTime? EndOfDrainageDate { get; set; }
        public int? RainfallPostApplication { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }

    }
}
