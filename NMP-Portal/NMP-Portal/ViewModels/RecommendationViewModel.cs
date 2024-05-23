using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class RecommendationViewModel
    {
        public List<Recommendation>? Recommendations { get; set; }
        public List<Crop>? Crops { get; set; }
        public string? FieldName { get; set; }
        public string? FarmName { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? CropTypeName { get; set; }
        public string? EncryptedYear { get; set; }
    }
}
