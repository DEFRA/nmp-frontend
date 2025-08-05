using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class NutrientsLoadingFarmDetail
    {
        public int? FarmID { get; set; }
        public int? CalendarYear { get; set; }
        public decimal? LandInNVZ { get; set; }
        public decimal? LandNotNVZ { get; set; }
        public decimal? TotalFarmed { get; set; }
        public bool? ManureTotal { get; set; }
        public bool? Derogation { get; set; }
        public int? GrassPercentage { get; set; }
        public bool? ContingencyPlan { get; set; } = false;
        public DateTime CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; } = null;
        public int? ModifiedByID { get; set; } = null;

    }
}
