namespace NMP.Core.ServiceResponses;
public class NMaxReportResponse
{
    public string CropTypeName { get; set; } = string.Empty;
    public int NmaxLimit { get; set; }
    public bool IsComply { get; set; } = false;
    public string GroupName { get; set; } = string.Empty;
    public List<NMaxLimitReportResponse> NMaxLimitReportResponse { get; set; }
    public List<NitrogenApplicationsForNMaxReportResponse> NitrogenApplicationsForNMaxReportResponse { get; set; }
}
