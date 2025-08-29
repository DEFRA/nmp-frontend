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

    }
}
