using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class RecommendationReportHeaderResponse:Recommendation
    {
        public List<RecommendationComment>? RecommendationComments { get; set; }
    }
}
