namespace NMP.Portal.Models;
public class WarningMessage
{
    public int? ID { get; set; }
    public int? FieldID { get; set; }
    public int? CropID { get; set; }
    public int? JoiningID { get; set; }
    public string? Header { get; set; }
    public string? Para1 { get; set; }
    public string? Para2 { get; set; }
    public string? Para3 { get; set; }
    public int WarningCodeID { get; set; }
    public int WarningLevelID { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? CreatedByID { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByID { get; set; }
    public int? PreviousID { get; set; }
}
