using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FertiliserManureViewModel
    {
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public string? FarmName { get; set; }

        public bool isEnglishRules { get; set; }

        public List<string>? FieldList { get; set; }
        public bool IsComingFromRecommendation { get; set; } = false;
        public bool IsCheckAnswer { get; set; } = false;
        public string? FieldGroupName { get; set; }

        public string? CropTypeName { get; set; }
        public List<FertiliserManures>? FertiliserManures { get; set; }
    }
}
