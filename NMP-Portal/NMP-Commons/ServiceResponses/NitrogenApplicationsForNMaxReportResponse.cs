namespace NMP.Commons.ServiceResponses;
public class NitrogenApplicationsForNMaxReportResponse
{
    public int FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string CropTypeName { get; set; } = string.Empty;
    public decimal CropArea { get; set; }
    public int? InorganicNRate { get; set; }
    public int? InorganicNTotal { get; set; }
    public int? OrganicCropAvailableNRate { get; set; }
    public int? OrganicCropAvailableNTotal { get; set; }
    public int? NRate { get; set; }
    public int? NTotal { get; set; }
}
