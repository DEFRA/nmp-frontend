using NMP.Portal.Models;
using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels
{
    public class StorageCapacityViewModel:StoreCapacity
    {
        public string EncryptedFarmID { get; set; } = string.Empty;
        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? LastModifiedOn { get; set; }
        public string? FarmName { get; set; } = string.Empty;
        public bool? IsComingFromPlan { get; set; } = false;
        public int? ReportOption { get; set; }
        public int? FieldAndPlanReportOption { get; set; }
        public int? NVZReportOption { get; set; }
        public string? ReportTypeName { get; set; } = string.Empty;
        public string? StorageTypeName { get; set; } = string.Empty;
        public string? MaterialStateName { get; set; }
        public bool? IsCircumference { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? SolidManureDensity { get; set; }

        [Range(0, 9999, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And9999))]
        public decimal? StorageBagCapacity { get; set; }
        public bool? IsSlopeEdge { get; set; }
        public string? BankSlopeAngleName { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public decimal? Slope { get; set; }
        public decimal? FreeBoardHeight { get; set; }
        public bool IsStoreCapacityExist { get; set; } = false;
        public Farm? Farm { get; set; }
        public string? EncryptedMaterialStateID { get; set; }
        public string? IsComingFromManageToHubPage { get; set; }
        public string? IsComingFromMaterialToHubPage { get; set; }
        public bool IsMaterialTypeChange { get; set; } = false;
        public bool IsStorageTypeChange { get; set; } = false;
        public bool? IsCancel { get; set; }
        public bool? IsCopyExistingManureStorage { get; set; }
    }
}
