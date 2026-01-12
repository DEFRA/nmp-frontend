using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;

public interface IAddressLookupService : IService
{
    Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset);
}
