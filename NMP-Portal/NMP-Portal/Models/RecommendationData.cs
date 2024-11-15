using NMP.Portal.ViewModels;

namespace NMP.Portal.Models
{
    public class RecommendationData
    {
        public Recommendation? Recommendation { get; set; }
        public List<RecommendationComment>? RecommendationComments { get; set; }
        public ManagementPeriod? ManagementPeriod { get; set; }
        public List<OrganicManureData>? OrganicManures { get; set; }
        public List<FertiliserManure>? FertiliserManures { get; set; }
    }

}
