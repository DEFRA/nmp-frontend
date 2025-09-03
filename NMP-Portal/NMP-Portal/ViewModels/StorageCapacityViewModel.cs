using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels
{
    public class StorageCapacityViewModel
    {
        public string EncryptedFarmId { get; set; } = string.Empty;
        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? LastModifiedOn { get; set; }
        public int? Year { get; set; }
        public int? FarmId { get; set; }
        public string? FarmName { get; set; } = string.Empty;
        public bool? IsComingFromPlan { get; set; } = false;
        public int? ReportOption { get; set; }
        public int? FieldAndPlanReportOption { get; set; }
        public int? NVZReportOption { get; set; }
        public string? ReportTypeName { get; set; } = string.Empty;

        public int? StorageTypeId { get; set; }
        public string? StorageTypeName { get; set; } = string.Empty;
        public int? MaterialStateId { get; set; }
        public string? MaterialStateName { get; set; }
        public string? StoreName { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Length { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Depth { get; set; }
        public bool? IsCovered { get; set; }

        public decimal? Circumference { get; set; }
        public decimal? Diameter { get; set; }
        public bool? IsCircumference { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? WeightCapacity { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? SolidManureDensity { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? StorageBagCapacity { get; set; }
        public bool? IsSlopeEdge { get; set; }
        public int? BankSlopeAngleId { get; set; }
        public string? BankSlopeAngleName { get; set; }

        //total capacity in cubic meters (Length * Width * (Depth - FreeBoard))
        public decimal? CapacityVolume { get; set; } 
        public decimal? SurfaceArea { get; set; }
    }
}
