using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IOrganicManureService
    {
        Task<(List<OrganicManureCropTypeResponse>,Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId,int harvestYear);
        Task<(List<OrganicManureFieldResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId( int harvestYear, int farmId, string? cropTypeId);
    }
}
