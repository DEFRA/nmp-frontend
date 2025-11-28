using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using NMP.Portal.Models;
using NMP.Portal.Security;
using System.IdentityModel.Tokens.Jwt;
namespace NMP.Portal.Services
{
    public abstract class Service : IService
    {
        public readonly IHttpClientFactory _clientFactory;
        public readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TokenRefreshService _tokenRefreshService;

        protected Service(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefresh)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
            _tokenRefreshService = tokenRefresh;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null)
            {
                return null;
            }

            var token = await ctx.GetTokenAsync("access_token");
            return token;
        }

        private bool JwtExpired(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            return token.ValidTo < DateTime.UtcNow.AddMinutes(-5);
        }

        public async Task<HttpClient> GetNMPAPIClient()
        {
            var accessToken = await GetAccessTokenAsync();

            if (JwtExpired(accessToken))
            {
                accessToken = await _tokenRefreshService.RefreshUserAccessTokenAsync(context: _httpContextAccessor.HttpContext);
            }

            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");            
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);            
            return await Task.FromResult(httpClient).ConfigureAwait(false);

        }

        public async Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null)
        {            
            HttpClient httpClient = await GetNMPAPIClient();            
            var response = await httpClient.PostAsJsonAsync(url, model);
            return response;
        }

        public async Task<HttpResponseMessage> GetDataAsync(string url)
        {            
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(url);
            return response;
        }
    }
}
