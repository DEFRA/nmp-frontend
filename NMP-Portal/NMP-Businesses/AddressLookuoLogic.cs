using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class AddressLookuoLogic(ILogger<AddressLookuoLogic> logger, IAddressLookupService addressLookupService) : IAddressLookupLogic
{
    private readonly IAddressLookupService _addressLookupService = addressLookupService;
    private readonly ILogger<AddressLookuoLogic> _logger = logger;

    public Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
    {
        _logger.LogTrace("Entering AddressesAsync method in AddressLookuoLogic");
        return _addressLookupService.AddressesAsync(postcode, offset);
    }
}
