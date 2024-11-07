namespace NMP.Portal.Models
{
    public class HarvestYearPlanFields
    {
        public string EncryptedFieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public int? OrganicManureCount { get; set; }
        public int? FertiliserManuresCount { get; set; }
    }
}
