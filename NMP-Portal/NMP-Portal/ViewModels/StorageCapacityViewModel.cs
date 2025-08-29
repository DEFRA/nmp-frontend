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

    }
}
