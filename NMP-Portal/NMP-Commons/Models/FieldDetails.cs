namespace NMP.Commons.Models;

public class FieldDetails
{
    public string EncryptedFieldId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public DateTime? PlantingDate { get; set; }
    public decimal? Yield { get; set; }
    public string? Variety { get; set; } = string.Empty;
    public string? Management { get; set; } = string.Empty;
    public decimal? CroppedArea { get; set; }
    public string? PreviousCrop { get; set; }
    public string? SoilType { get; set; }
    public int? NitrogenResidueGroup { get; set; }
}
