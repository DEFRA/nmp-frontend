using NMP.Commons.Models;
namespace NMP.Commons.ServiceResponses;

public class RecommendationReportHeaderResponse  :Recommendation
{
   public List<RecommendationComment>? RecommendationComments { get; set; }
}
