namespace NMP.Portal.Models
{
    public class PreviousGrasse
    {
        public bool? HasGrassInLastThreeYear { get; set; }
        public int? HarvestYear { get; set; }
        public int? GrassManagementOptionID { get; set; }
        public int? GrassTypicalCutID { get; set; }
        public int? HasGreaterThan30PercentClover { get; set; }
        public int? SoilNitrogenSupplyItemID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
