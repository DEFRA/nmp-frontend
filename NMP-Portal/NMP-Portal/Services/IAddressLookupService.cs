using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IAddressLookupService : IService
    {
        Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset);
    }
}
