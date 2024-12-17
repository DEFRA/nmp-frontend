namespace NMP.Portal.Models
{
    public class PreviousGrass
    {
        public int ID { get; set; }
        public int FieldID { get; set; }
        public bool? HasGrassInLastThreeYear { get; set; }
        public int? HarvestYear { get; set; }
        public int? GrassManagementOptionID { get; set; }
        public int? GrassTypicalCutID { get; set; }
        public bool? HasGreaterThan30PercentClover { get; set; }
        public int? SoilNitrogenSupplyItemID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
