namespace NMP.Portal.Models
{
    public class SnsAnalysis
    {
        public int CropID { get; set; }
        public int CropTypeID { get; set; }
        public DateTime? SampleDate { get; set; }
        public int? SnsAt0to30cm { get; set; }
        public int? SnsAt30to60cm { get; set; }
        public int? SnsAt60to90cm { get; set; }
        public int? SampleDepth { get; set; }
        public int? SoilMineralNitrogen { get; set; }
        public int? NumberOfShoots { get; set; }
        public decimal? GreenAreaIndex { get; set; }
        public decimal? CropHeight { get; set; }
        public int? SeasonId { get; set; }
        public decimal? PercentageOfOrganicMatter { get; set; }
        public decimal? AdjustmentValue { get; set; }
        public int? SoilNitrogenSupplyValue { get; set; }
        public int? SoilNitrogenSupplyIndex { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
