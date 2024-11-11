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
        public string? CropInfo1Name { get; set; }
        public string? CropInfo2Name { get; set; }
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
        public string? FarmName { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public bool IsCropGroupChange { get; set; } = false;
        public bool IsAnyChangeInField { get; set; } = false;
        public bool IsQuestionChange { get; set; } = false;
        public bool IsCropTypeChange { get; set; } = false;

        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? LastModifiedOn { get; set; }
        public List<string>? EncryptedHarvestYearList { get; set; }
        public List<HarvestYearPlans> HarvestYearPlans { get; set; } = new List<HarvestYearPlans>();
        public int FieldCount { get; set; }
        public List<HarvestYear> HarvestYear { get; set; } = new List<HarvestYear>();
        public bool IsAddAnotherCrop { get; set; }
        public bool? IsPlanRecord { get; set; } = false;
        public string? IsSortOragnicListByDate { get; set; } = string.Empty;
        public string? IsSortOragnicListByFieldName { get; set; } = string.Empty;
        public string? IsSortInOragnicListByDate { get; set; } = string.Empty;
        public string? IsSortInOragnicListByFieldName { get; set; } = string.Empty;
    }
}
