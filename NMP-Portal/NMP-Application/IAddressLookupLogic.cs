using NMP.Commons.ServiceResponses;
namespace NMP.Application;

public interface IAddressLookupLogic
{
    Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset);
}
