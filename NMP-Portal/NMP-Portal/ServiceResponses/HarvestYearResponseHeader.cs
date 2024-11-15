using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class HarvestYearResponseHeader
    {
        public Farm? farmDetails { get; set; }
        public List<CropDetailResponse>? CropDetails { get; set; }
        public List<OrganicManureResponse>? OrganicMaterial { get; set; }
        public List<InorganicFertiliserResponse>? InorganicFertiliserApplication { get; set; }
    }
}
