using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class RecommendationReportResponse: ManagementPeriodViewModel
    {
        public RecommendationReportHeaderResponse? Recommendation { get; set; }
        //public List<RecommendationComment>? RecommendationComments { get; set; }
        public ManagementPeriod? ManagementPeriod { get; set; }
        public List<OrganicManureData>? OrganicManures { get; set; }
        public List<FertiliserManure>? FertiliserManures { get; set; }
    }
}
