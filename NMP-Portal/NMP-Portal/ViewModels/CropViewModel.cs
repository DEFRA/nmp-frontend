using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class CropViewModel:Crop
    {
        
        public string? CropInfo1Name { get; set; }
        public string? CropInfo2Name { get; set; }
        public string? CropTypeName { get; set; }
        public string? EncryptedCropTypeName { get; set; } = string.Empty;
        public string? EncryptedFieldName { get; set; } = string.Empty;
        public string? EncryptedFieldId { get; set; } = string.Empty;
        public string? EncryptedCropOrder { get; set; } = string.Empty;
        public string? EncryptedCropId { get; set; }
        public bool? IsSnsExist { get; set; } = false;
        public SnsAnalysis? SnsAnalysisData { get; set; }
        public string? SwardTypeName { get; set; }
        public string? SwardManagementName { get; set; }
        public string? EstablishmentName { get; set; }
        public string? DefoliationSequenceName { get; set; }
    }
}
