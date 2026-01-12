using NMP.Commons.Enums;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Commons.ViewModels;
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
    public DateTime? SowingDate { get; set; }
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
    public string? EncryptSortOrganicListOrderByDate { get; set; } = string.Empty;
    public string? SortOrganicListOrderByDate { get; set; } = string.Empty;
    public string? EncryptSortOrganicListOrderByFieldName { get; set; } = string.Empty;
    public string? EncryptSortInOrganicListOrderByDate { get; set; } = string.Empty;
    public string? SortInOrganicListOrderByDate { get; set; } = string.Empty;
    public string? EncryptSortInOrganicListOrderByFieldName { get; set; } = string.Empty;
    public string? EncryptSortOrganicListOrderByCropType { get; set; } = string.Empty;
    public string? SortOrganicListOrderByCropType { get; set; } = string.Empty;
    public string? EncryptSortInOrganicListOrderByCropType { get; set; } = string.Empty;
    public string? SortInOrganicListOrderByCropType { get; set; } = string.Empty;
    public int? AnnualRainfall { get; set; }
    public int? ExcessWinterRainfallValue { get; set; }
    public int? ExcessWinterRainfallId { get; set; }
    public string? ExcessWinterRainfallName { get; set; }
    public string? SortInOrganicListOrderByFieldName { get; set; } = string.Empty;
    public string? SortOrganicListOrderByFieldName { get; set; } = string.Empty;

    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgCropGroupNameShouldNotContainSpecialChar))]
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
    public bool? IsComingFromRecommendation { get; set; }
    public bool? IsExcessWinterRainfallCheckAnswer { get; set; }
    public bool? IsExcessWinterRainfallUpdated { get; set; }
    public string? EncryptedIsCropUpdate { get; set; }
    public string? PreviousCropGroupName { get; set; }

    // grass properties
    public int? CurrentSward { get; set; }
    public int? GrassSeason { get; set; }

    public int GrassGrowthClassCounter { get; set; } = 0;
    public int GrassGrowthClassDistinctCount { get; set; } = 0;
    public string? GrassGrowthClassEncryptedCounter { get; set; }
    public int? GrassGrowthClassQuestion { get; set; }

    public int DryMatterYieldCounter { get; set; } = 0;
    public string? DryMatterYieldEncryptedCounter { get; set; }
    public bool? IsCancel { get; set; }
    public int? SwardTypeId { get; set; }
    public int? SwardManagementId { get; set; }
    public int? PotentialCut { get; set; }
    public int? DefoliationSequenceId { get; set; }
    public string? EncryptedCropOrder { get; set; } = string.Empty;
    public string? EncryptedCropType { get; set; } = string.Empty;
    public string? EncryptedCropGroupName { get; set; } = string.Empty;
    public int? FarmID { get; set; }
    public bool? CopyExistingPlan { get; set; }
    public int? CopyYear { get; set; }        
    public OrganicInorganicCopyOptions? OrganicInorganicCopy { get; set; }
    public string? GrassSeasonName { get; set; }
    public bool? IsFieldToBeRemoved { get; set; }
    public bool IsCurrentSwardChange { get; set; } = false;
}
