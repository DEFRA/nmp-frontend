namespace NMP.Commons.Models;
public class DefoliationList
{
    public int ManagementPeriodID { get; set; }
    public int CropID { get; set; }
    public int? Defoliation { get; set; }
    public string? DefoliationName { get; set; }
    public int FieldID { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int Counter { get; set; }
    public string EncryptedCounter { get; set; } = string.Empty;
}
