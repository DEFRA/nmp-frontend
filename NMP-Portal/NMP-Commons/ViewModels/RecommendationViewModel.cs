using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Commons.ViewModels;
public class RecommendationViewModel
{
    public List<Recommendation>? Recommendations { get; set; }
    public List<RecommendationComment>? RecommendationComments { get; set; }
    public List<CropViewModel>? Crops { get; set; }
    public List<ManagementPeriodViewModel>? ManagementPeriods { get; set; }
    public int? CropGroupID { get; set; }
    public string? FieldName { get; set; }
    public string? FarmName { get; set; }
    public string? EncryptedFarmId { get; set; }

    public string? EncryptedHarvestYear { get; set; }
    public string? EncryptedFieldId { get; set; } = string.Empty;
    public List<OrganicManureDataViewModel> OrganicManures { get; set; }
    public List<FertiliserManureDataViewModel> FertiliserManures { get; set; }        
    public List<NutrientResponseWrapper> Nutrients { get; set; }
    public PKBalance? PKBalance { get; set; }
    public int? FarmRB209CountryID { get; set; }
}
