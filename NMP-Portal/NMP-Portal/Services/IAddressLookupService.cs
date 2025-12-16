using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IAddressLookupService : IService
    {
        Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset);
    }
}
