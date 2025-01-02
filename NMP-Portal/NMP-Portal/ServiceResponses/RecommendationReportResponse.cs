using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class RecommendationReportResponse:ManagementPeriod
    {
        public RecommendationReportHeaderResponse? Recommendation { get; set; }
        //public List<RecommendationComment>? RecommendationComments { get; set; }
        public ManagementPeriod? ManagementPeriod { get; set; }
        public List<OrganicManureData>? OrganicManures { get; set; }
        public List<FertiliserManure>? FertiliserManures { get; set; }
    }
}
