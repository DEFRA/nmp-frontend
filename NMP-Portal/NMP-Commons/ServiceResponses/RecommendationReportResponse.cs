using NMP.Commons.Models;
using NMP.Commons.ViewModels;
namespace NMP.Commons.ServiceResponses;
public class RecommendationReportResponse : ManagementPeriodViewModel
{
    public RecommendationReportHeaderResponse? Recommendation { get; set; }
    public ManagementPeriod? ManagementPeriod { get; set; }
    public List<OrganicManureDataViewModel>? OrganicManures { get; set; }
    public List<FertiliserManure>? FertiliserManures { get; set; }
}
