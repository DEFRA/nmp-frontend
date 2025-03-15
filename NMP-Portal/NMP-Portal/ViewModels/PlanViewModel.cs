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
        public HarvestYearPlans? HarvestYearPlans { get; set; }
        public int FieldCount { get; set; }
        public List<HarvestYear>? HarvestYear { get; set; } = new List<HarvestYear>();
        public bool IsAddAnotherCrop { get; set; }
        public bool? IsPlanRecord { get; set; } = false;
        public string? encryptSortOrganicListOrderByDate { get; set; } = string.Empty;
        public string? sortOrganicListOrderByDate { get; set; } = string.Empty;
        public string? encryptSortOrganicListOrderByFieldName { get; set; } = string.Empty;
        public string? encryptSortInOrganicListOrderByDate { get; set; } = string.Empty;
        public string? sortInOrganicListOrderByDate { get; set; } = string.Empty;
        public string? encryptSortInOrganicListOrderByFieldName { get; set; } = string.Empty;
        public int? AnnualRainfall { get; set; }
        public int? ExcessWinterRainfallValue { get; set; }
        public int? ExcessWinterRainfallId { get; set; }
        public string? ExcessWinterRainfallName { get; set; }
        public string? SortInOrganicListOrderByFieldName { get; set; } = string.Empty;
        public string? SortOrganicListOrderByFieldName { get; set; } = string.Empty;
        public string? CropGroupName { get; set; } = string.Empty;
        public bool? RemoveCrop { get; set; }
        public bool? DeletePlanOrganicAndFertiliser { get; set; }
        public string? EncryptedId { get; set; } = string.Empty;
        public string? DeletedAction { get; set; } = string.Empty;
        public string? EncryptedFieldName { get; set; } = string.Empty;
        public string? EncryptedFieldId { get; set; } = string.Empty;
        public string? ManureType { get; set; } = string.Empty;
        public int? CropOrder { get; set; }
        public List<int>? organicManureIds { get; set; }
        public List<string>? SelectedField { get; set; }
        public bool? isComingFromRecommendation { get; set; }
        public bool? IsExcessWinterRainfallCheckAnswer { get; set; }
        public bool? IsExcessWinterRainfallUpdated { get; set; }
        public string? EncryptedIsCropUpdate { get; set; }
        public string? PreviousCropGroupName { get; set; }
    }
}
