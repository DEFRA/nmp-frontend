namespace NMP.Commons.Models;
public class AutumnCropNitrogenUptakeDetail
{
    public string? EncryptedFieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int CropTypeId { get; set; }
    public string? CropTypeName { get; set; } = string.Empty;
    public int AutumnCropNitrogenUptake { get; set; }
}
