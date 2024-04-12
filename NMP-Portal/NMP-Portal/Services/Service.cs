using NMP.Portal.Helpers;
using NMP.Portal.Models;

namespace NMP.Portal.Services
{
    public abstract class Service : IService
    {
        public readonly IHttpClientFactory _clientFactory;
        public readonly IHttpContextAccessor _httpContextAccessor;
        
        public Service(IHttpContextAccessor httpContextAccessor,IHttpClientFactory clientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
        }

        public async Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null)
        {
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");            
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = httpClient.PostAsJsonAsync(url, model);
            return await response;
        }

        public async Task<HttpResponseMessage> GetDataAsync(string url)
        {
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");            
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = httpClient.GetAsync(url);
            return await response;
        }
    }
}
