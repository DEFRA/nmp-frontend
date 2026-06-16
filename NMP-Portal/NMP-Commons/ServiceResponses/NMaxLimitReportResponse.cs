namespace NMP.Commons.ServiceResponses;
public class NMaxLimitReportResponse
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string CropTypeName { get; set; } = string.Empty;
    public decimal CropArea { get; set; }
    public decimal? CropYield { get; set; }
    public decimal SoilTypeAdjustment { get; set; }
    public decimal? YieldAdjustment { get; set; }
    public decimal MillingWheat { get; set; }
    public decimal PaperCrumbleOrStrawMulch { get; set; }
    public decimal? StandardRate { get; set; }
    public decimal? MarketAdjustment { get; set; }
    public decimal? WinterRainfallAdjustment { get; set; }
    public int AdjustedNMaxLimit { get; set; }
    public int? MaximumLimitForNApplied { get; set; }
    public decimal AdjustmentForThreeOrMoreCuts { get; set; }
}
