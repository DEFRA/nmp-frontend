using NMP.Commons.ViewModels;

namespace NMP.Commons.Models;

public class RecommendationData
{
    public Recommendation? Recommendation { get; set; }
    public List<RecommendationComment>? RecommendationComments { get; set; }
    public ManagementPeriod? ManagementPeriod { get; set; }
    public List<OrganicManureDataViewModel>? OrganicManures { get; set; }
    public List<FertiliserManure>? FertiliserManures { get; set; }
    public int? CropGroupID { get; set; }
    public int? CropTypeID { get; set; }
    public int? ManagementTableIndex  { get; set; }
    public string? DefoliationSequenceName { get; set; }
}
