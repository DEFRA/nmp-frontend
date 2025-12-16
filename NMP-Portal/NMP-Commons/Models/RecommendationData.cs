namespace NMP.Commons.Models;

//TODO:Neded to revirew OrganicManures property
public class RecommendationData
{
    public Recommendation? Recommendation { get; set; }
    public List<RecommendationComment>? RecommendationComments { get; set; }
    public ManagementPeriod? ManagementPeriod { get; set; }
    //public List<OrganicManureDataViewModel>? OrganicManures { get; set; }
    public List<FertiliserManure>? FertiliserManures { get; set; }        
}
