namespace NMP.Portal.Models
{
    public class RecommendationData
    {
        public Recommendation? Recommendation { get; set; }
        public List<RecommendationComment>? RecommendationComments { get; set; }
        public ManagementPeriod? ManagementPeriod { get; set; }
    }
}
