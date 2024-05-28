using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class RecommendationViewModel
    {
        public List<Recommendation>? Recommendations { get; set; }
        public List<RecommendationComment>? RecommendationComments { get; set; }
        public List<CropViewModel>? Crops { get; set; }
        public List<ManagementPeriod>? ManagementPeriods { get; set; }
        public int? CropGroupID { get; set; }
        public string? FieldName { get; set; }        
        public string? FarmName { get; set; }
        public string? EncryptedFarmId { get; set; }
       
        public string? EncryptedHarvestYear { get; set; }
    }
}
