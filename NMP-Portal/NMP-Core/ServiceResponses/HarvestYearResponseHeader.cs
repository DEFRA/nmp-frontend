using NMP.Commons.Models;
namespace NMP.Core.ServiceResponses;
public class HarvestYearResponseHeader
{
    public Farm? farmDetails { get; set; }
    public List<CropDetailResponse>? CropDetails { get; set; }
    public List<OrganicManureResponse>? OrganicMaterial { get; set; }
    public List<InorganicFertiliserResponse>? InorganicFertiliserApplication { get; set; }
}
