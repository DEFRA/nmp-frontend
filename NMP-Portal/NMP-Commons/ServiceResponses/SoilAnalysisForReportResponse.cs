namespace NMP.Commons.ServiceResponses;
public class SoilAnalysisForReportResponse
{
    public DateTime? Date { get; set; }
    public string? PH { get; set; }
    public string? PhosphorusIndex { get; set; }
    public string? PotassiumIndex { get; set; }
    public string? MagnesiumIndex { get; set; }
    public int? OrganicMatter { get; set; }
    public int? PhosphorusMethodologyID { get; set; }
    public string? PhosphorusStatus { get; set; }
    public string? PotassiumStatus { get; set; }
    public string? MagnesiumStatus { get; set; }
}
