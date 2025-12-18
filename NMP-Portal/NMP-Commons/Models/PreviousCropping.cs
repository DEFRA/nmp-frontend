namespace NMP.Commons.Models;
public class PreviousCropping
{
    public int? ID { get; set; }
    public int FieldID { get; set; }
    public int? CropGroupID { get; set; }
    public int? CropTypeID { get; set; }
    public bool? HasGrassInLastThreeYear { get; set; }
    public int? HarvestYear { get; set; }
    public int? LayDuration { get; set; }
    public int? GrassManagementOptionID { get; set; }
    public bool? HasGreaterThan30PercentClover { get; set; }
    public int? SoilNitrogenSupplyItemID { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? CreatedByID { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByID { get; set; }
}
