namespace NMP.Commons.Models;
public class ApplicationForFertiliserManure
{
    public DateTime? ApplicationDate { get; set; }
    public int? ApplicationRate { get; set; }
    public decimal? N { get; set; }
    public decimal? P2O5 { get; set; }
    public decimal? K2O { get; set; }
    public decimal? SO3 { get; set; }
    public decimal? Lime { get; set; }
    public string? EncryptedCounter { get; set; }
    public int? Counter { get; set; }
    public int? InOrgnaicManureDurationId { get; set; }
    public bool? QuestionForSpreadInorganicFertiliser { get; set; }
}
