namespace NMP.Portal.ServiceResponses
{
    public class HarvestYearResponseHeader
    {
        public List<CropDetailResponse>? CropDetailsList { get; set; }
        public List<OrganicManureResponse>? OrganicManureList { get; set; }
        public List<InorganicFertiliserResponse>? InorganicFertiliserList { get; set; }
    }
}
