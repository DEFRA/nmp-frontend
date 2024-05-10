using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class PlanViewModel
    {
        public int? CropTypeID { get; set; }
        public string? Variety { get; set; }
        public string? OtherCropName { get; set; }
        public int? CropInfo1 { get; set; }
        public int? CropInfo2 { get; set; }
        public bool IsEnglishRules { get; set; }
        public string EncryptedFarmId { get; set; } = string.Empty;
        public int? CropGroupId { get; set; }
        public string? CropGroup { get; set; }
        public string? CropType { get; set; }
        public string? FieldName { get; set; }
        public int? FieldID { get; set; }
        public new int? Year { get; set; }
        public DateTime SowingDate { get; set; }
        public List<string>? FieldList { get; set; }
        public List<Crop>? Crops { get; set; }
        public int? SowingDateQuestion { get; set; }
        public int? YieldQuestion { get; set; }
        public int SowingDateCurrentCounter { get; set; } = 0;
        public string? SowingDateEncryptedCounter { get; set; }
        public int YieldCurrentCounter { get; set; } = 0;
        public string? YieldEncryptedCounter { get; set; }
        public decimal? Yield { get; set; }
        public decimal? Length { get; set; }
    }
}
