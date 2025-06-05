namespace NMP.Portal.ServiceResponses
{
    public class NMaxLimitReportResponse
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; }
        public string CropTypeName { get; set; }
        public decimal CropArea { get; set; }
        public decimal? CropYield { get; set; }
        public decimal SoilTypeAdjustment { get; set; }
        public decimal YieldAdjustment { get; set; }
        public decimal MillingWheat { get; set; }
        public decimal PaperCrumbleOrStrawMulch { get; set; }
        public decimal AdjustedNMaxLimit { get; set; }

        public decimal MaximumLimitForNApplied { get; set; }
        public decimal AdjustmentForThreeOrMoreCuts { get; set; }
}
}
