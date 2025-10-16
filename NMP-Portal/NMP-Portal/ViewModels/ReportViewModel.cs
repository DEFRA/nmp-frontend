using Microsoft.AspNetCore.Cors;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels
{
    public class ReportViewModel
    {
        public List<string>? FieldList { get; set; }
        public string EncryptedFarmId { get; set; } = string.Empty;
        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? LastModifiedOn { get; set; }
        public int? Year { get; set; }
        public int? FarmId { get; set; }
        public string? FarmName { get; set; } = string.Empty;

        public int? ReportType { get; set; }
        public CropAndFieldReportResponse? CropAndFieldReport { get; set; }
        public List<NutrientResponseWrapper>? Nutrients { get; set; }
        public List<string>? CropTypeList { get; set; }
        public Farm? Farm { get; set; }
        public List<NMaxReportResponse>? NMaxLimitReport { get; set; }
        public int? ReportOption { get; set; }
        public int? FieldAndPlanReportOption { get; set; }
        public int? NVZReportOption { get; set; }
        public string? ReportTypeName { get; set; } = string.Empty;
        public bool? IsGrasslandDerogation { get; set; }

        //[Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheTotalFarmArea))]
        public decimal? TotalFarmArea { get; set; }

        //[Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheTotalAreaInAnNVZ))]
        public decimal? TotalAreaInNVZ { get; set; }
        public decimal? LivestockNumbers { get; set; }
        public bool? IsAnyLivestockImportExport { get; set; }
        public decimal? ImportsExportsOfLivestockManure { get; set; }
        public bool IsCheckList { get; set; } = false;
        public int? ImportExport { get; set; }
        public int? ManureTypeId { get; set; }
        public string? ManureTypeName { get; set; }
        public bool IsDefaultValueChange { get; set; } = false;
        public DateTime? LivestockImportExportDate { get; set; }
        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterAnQuantityBetweenValue))]
        public int? LivestockQuantity { get; set; }
        public List<HarvestYear>? HarvestYear { get; set; }
        public string? DefaultNutrientValue { get; set; }
        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? DryMatterPercent { get; set; }
        public decimal? UricAcid { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }
        public bool? IsDefaultNutrient { get; set; } = false;
        public ManureType ManureType { get; set; }
        public DateTime? DefaultFarmManureValueDate { get; set; }
        public bool? IsThisDefaultValueOfRB209 { get; set; }
        public bool? IsManureTypeLiquid { get; set; }
        public bool IsAnyNeedToStoreNutrientValueForFuture { get; set; } = false;
        public string? ReceiverName { get; set; }
        //[Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblTownOrCity))]
        public string? Address1 { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblAddressLine2))]
        public string? Address2 { get; set; }

        //[Display(ResourceType = typeof(Resource), Name = nameof(string.Format("{0} {1}",Resource.lblTownOrCity,Resource.lblOptional)))]
        public string? Address3 { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblCounty))]
        public string? Address4 { get; set; }
        [StringLength(8, MinimumLength = 6, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]
        [RegularExpression(@"^[A-Za-z]{1,2}\d{1,2}[A-Za-z]?\s*\d[A-Za-z]{2}$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]
        //[Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheFarmPostcode))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheFarmPostcode))]
        public string? Postcode { get; set; } = string.Empty;
        public string? Comment { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public bool IsManageImportExport { get; set; } = false;
        public string? IsComingFromImportExportOverviewPage { get; set; } = string.Empty;
        public bool IsDefaultNutrientChange { get; set; } = false;
        public bool IsManureTypeChange { get; set; } = false;
        public bool? IsCancel { get; set; }
        public bool? IsImport { get; set; }
        public bool? IsComingFromPlan { get; set; } = false;
        public bool? IsAnyLivestockNumber { get; set; }
        public int? LivestockGroupId { get; set; }
        public string? LivestockGroupName { get; set; } = string.Empty;
        public string? EncryptedId { get; set; } = string.Empty;
        public bool? IsComingFromSuccessMsg { get; set; } = false;
        public int? LivestockTypeId { get; set; }
        public string? LivestockTypeName { get; set; } = string.Empty;
        public int? LivestockNumberQuestion { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? AverageNumber { get; set; }
        public int? ManureGroupId { get; set; }
        public string? ManureGroupName { get; set; }
        public int? ManureGroupIdForFilter { get; set; }
        public string? OtherMaterialName { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInJanuary { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInFebruary { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInMarch { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInApril { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInMay { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInJune { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInJuly { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInAugust { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInSeptember { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInOctober { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInNovember { get; set; }

        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? NumbersInDecember { get; set; }
        public bool? IsDeleteLivestockImportExport { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? AverageNumberOfPlaces { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheAverageOccupancy))]
        [Range(0, int.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public int? AverageOccupancy { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? NitrogenStandard { get; set; }
        public decimal? PhosphateStandard { get; set; }
        public bool IsManageLivestock { get; set; } = false;
        public bool IsLivestockCheckAnswer { get; set; } = false;

        //[Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatPercentageOfTheLandIsFarmedAsGrass))]
        [Range(80, 100, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgToHaveADerogationAtLeast80PercentOfYourFarm))]
        public int? GrassPercentage { get; set; }
        public int? OccupancyAndNitrogenOptions { get; set; }

        public string? EncryptedNLLivestockID { get; set; } = string.Empty;
        public bool? IsDeleteNLLivestock { get; set; }
        public bool IsLivestockGroupChange { get; set; } = false;
    }
}
