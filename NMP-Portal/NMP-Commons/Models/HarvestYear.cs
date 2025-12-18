namespace NMP.Commons.Models;
public class HarvestYear
{
    public int Year { get; set; }
    public string EncryptedYear { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public bool IsAnyPlan { get; set; }
    public bool IsThisOldYear { get; set; } = false;
}
