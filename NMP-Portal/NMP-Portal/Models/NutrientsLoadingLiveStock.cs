namespace NMP.Portal.Models
{
    public class NutrientsLoadingLiveStock
    {
        public int? ID { get; set; }
        public int? FarmID { get; set; }
        public int? CalendarYear { get; set; }
        public int? LiveStockTypeID { get; set; }
        //public string? LiveStockType { get; set; }
        public decimal? Units { get; set; }
        //public decimal? NitrogenStandard { get; set; }
        public decimal? NByUnit { get; set; }
        public decimal? TotalNProduced { get; set; }
        public int? Occupancy { get; set; }
        public decimal? PByUnit { get; set; }
        public int? TotalPProduced { get; set; }
        public int? Jan { get; set; }
        public int? Feb { get; set; }
        public int? Mar { get; set; }
        public int? Apr { get; set; }
        public int? May { get; set; }
        public int? June { get; set; }
        public int? July { get; set; }
        public int? Aug { get; set; }
        public int? Sep { get; set; }
        public int? Oct { get; set; }
        public int? Nov { get; set; }
        public int? Dec { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; } = null;
        public int? ModifiedByID { get; set; } = null;
    }
}
