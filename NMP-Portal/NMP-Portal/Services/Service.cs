using NMP.Portal.Helpers;
using NMP.Portal.Models;

namespace NMP.Portal.Services
{
    public abstract class Service : IService
    {
        public readonly IHttpClientFactory _clientFactory;
        public readonly IHttpContextAccessor _httpContextAccessor;

        public Service(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
        }

        public async Task<HttpClient> GetNMPAPIClient()
        {
            string? jwtToken = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", jwtToken);
            return await Task.FromResult(httpClient).ConfigureAwait(false);

        }

        public async Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null)
        {
            string? jwtToken = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");            
            httpClient.DefaultRequestHeaders.Add("Authorization", jwtToken);
            var response = await httpClient.PostAsJsonAsync(url, model);
            return response;
        }

        public async Task<HttpResponseMessage> GetDataAsync(string url)
        {
            string? jwtToken = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");            
            httpClient.DefaultRequestHeaders.Add("Authorization", jwtToken);
            var response = await httpClient.GetAsync(url);
            return response;
        }
    }
}
