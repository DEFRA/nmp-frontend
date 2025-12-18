namespace NMP.Commons.Models;
public class ExcessRainfalls
{
    public int? FarmID { get; set; }
    public int? Year { get; set; }
    public int? ExcessRainfall { get; set; }
    public int? WinterRainfall { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? CreatedByID { get; set; }
    public DateTime? ModifiedOn { get; set; } = null;
    public int? ModifiedByID { get; set; } = null;
}
