using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NMP.Core.Interfaces;
namespace NMP.Services;

public class HomeService(ILogger<HomeService> logger, IHttpClientFactory clientFactory, IConfiguration configuration) : IHomeService
{
    private readonly ILogger<HomeService> _logger = logger;
    private readonly IHttpClientFactory _clientFactory = clientFactory;
    private readonly IConfiguration _configuration = configuration;
    public async Task<bool> IsDefraCustomerIdentifyConfigurationWorkingAsync()
    {
        _logger.LogTrace($"Home Service : IsDefraCustomerIdentifyConfigurationWorkingAsync() method called");
        HttpClient client = _clientFactory.CreateClient("DefraIdentityConfiguration");
        var uri = new Uri($"{_configuration["CustomerIdentityInstance"]}{_configuration["CustomerIdentityDomain"]}/{_configuration["CustomerIdentityPolicyId"]}/v2.0/.well-known/openid-configuration");
        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return response != null && response.IsSuccessStatusCode;  
    }

    public async Task<bool> IsNmptServiceWorkingAsync()
    {
        _logger.LogTrace($"Home Service : IsNmptServiceWorkingAsync() method called");
        HttpClient nmptServiceClient = _clientFactory.CreateClient("NMPApi");
        var response = await nmptServiceClient.GetAsync(new Uri($"{_configuration["NMPApiUrl"]}"));
        response.EnsureSuccessStatusCode();
        return response != null && response.IsSuccessStatusCode;
    }
}
