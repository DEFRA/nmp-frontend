﻿namespace NMP.Portal.Models
{
    public class HarvestYearPlanFields
    {
        public int? CropTypeID { get; set; }
        public string? CropTypeName { get; set; }
        public string? CropGroupName { get; set; }
        public string? EncryptedCropTypeName { get; set; }
        public string? EncryptedCropGroupName { get; set; }
        public List<FieldDetails> FieldData { get; set; } = new List<FieldDetails>();
    }

    public class FieldDetails
    {
        public string EncryptedFieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public DateTime? PlantingDate { get; set; }
        public decimal? Yield { get; set; }
        public string? Variety { get; set; } = string.Empty;
        public string? Management { get; set; } = string.Empty;
    }
}
