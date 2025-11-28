using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class PreviousCroppingViewModel:PreviousCropping
    {
        public string? EncryptedFarmID { get; set; }
        public string? EncryptedFieldID { get; set; }
        public string? EncryptedYear { get; set; }
        public bool? IsPreviousYearGrass { get; set; }
        public string? FieldName { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public List<int>? PreviousGrassYears { get; set; }
        public string? EncryptedCurrentYear { get; set; }
        public string? CropTypeName { get; set; }
        public bool? HasGrassInLastThreeYear { get; set; }
        public bool IsHasGrassInLastThreeYearChange { get; set; } = false;
        public bool? IsCancel { get; set; }
    }
}
