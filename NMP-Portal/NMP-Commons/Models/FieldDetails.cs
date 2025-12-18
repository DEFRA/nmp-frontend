namespace NMP.Commons.Models;

public class FieldDetails
{
    public string EncryptedFieldId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public DateTime? PlantingDate { get; set; }
    public decimal? Yield { get; set; }
    public string? Variety { get; set; } = string.Empty;
    public string? Management { get; set; } = string.Empty;
}
