using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class OrganicManureViewModel:OrganicManure
    {
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public List<string>? FieldList { get; set; }
    }
}
