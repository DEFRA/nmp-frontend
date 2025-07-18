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

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheTotalFarmArea))]
        public decimal? TotalFarmArea { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheTotalAreaInAnNVZ))]
        public decimal? TotalAreaInNVZ { get; set; }
        public decimal? LivestockNumbers { get; set; }
        public bool? LivestockImportExportQuestion { get; set; }
        public decimal? ImportsExportsOfLivestockManure { get; set; }
        public bool IsCheckList { get; set; } = false;
        public int? ImportExport { get; set; }
        public int? ManureTypeId { get; set; }
        public string? ManureTypeName { get; set; }
        public bool IsDefaultValueChange { get; set; } = false;
        public DateTime? LivestockImportExportDate { get; set; }
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
    }
}
