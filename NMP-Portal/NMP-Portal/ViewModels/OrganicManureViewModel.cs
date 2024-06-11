using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class OrganicManureViewModel
    {
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public List<string>? FieldList { get; set; }

        public int? ManureGroupId { get; set; }
        public string? ManureGroup { get; set; }
        public int? ManureTypeId { get; set; }
        public string? ManureType { get; set; }
        public bool isEnglishRules { get; set; }

        public List<OrganicManure>? OrganicManures { get; set; }



    }
}
