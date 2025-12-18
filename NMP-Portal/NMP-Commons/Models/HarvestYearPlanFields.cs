namespace NMP.Commons.Models;
public class HarvestYearPlanFields
{
    public int? CropTypeID { get; set; }
    public string? CropTypeName { get; set; }
    public string? CropGroupName { get; set; }
    public string? EncryptedCropTypeName { get; set; }
    public string? EncryptedCropGroupName { get; set; }
    public List<FieldDetails> FieldData { get; set; } = new List<FieldDetails>();
}
