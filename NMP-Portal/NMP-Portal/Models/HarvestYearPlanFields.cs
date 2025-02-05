namespace NMP.Portal.Models
{
    public class HarvestYearPlanFields
    {
        public string? CropTypeName { get; set; }
        public string? CropGroupName { get; set; }
        public List<FieldDetails> FieldData { get; set; } = new List<FieldDetails>();
    }

    public class FieldDetails
    {
        public string EncryptedFieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public DateTime? PlantingDate { get; set; }
        public decimal? Yield { get; set; }
        public string? Variety { get; set; } = string.Empty;
    }
}
