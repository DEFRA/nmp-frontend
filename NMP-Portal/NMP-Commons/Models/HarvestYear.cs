namespace NMP.Commons.Models;
public class HarvestYear
{
    public int Year { get; set; }
    public string EncryptedYear { get; set; } = string.Empty;
    public DateTime? LastModifiedOn { get; set; }
    public bool IsAnyPlan { get; set; }
    public bool IsThisOldYear { get; set; } = false;
}
