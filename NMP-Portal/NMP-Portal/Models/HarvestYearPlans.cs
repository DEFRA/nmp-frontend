using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Models
{
    public class HarvestYearPlans
    {
        public List<HarvestYearPlanFields>? FieldData { get; set; }
        public List<OrganicManureResponse>? OrganicManureList { get; set; }
        public List<InorganicFertiliserResponse>? InorganicFertiliserList { get; set; }
    }
}
