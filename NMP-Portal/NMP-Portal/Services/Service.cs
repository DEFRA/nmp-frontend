using Microsoft.Identity.Client;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Security;
using System.Security.Claims;
using System.Security.Principal;

namespace NMP.Portal.Services
{
    public abstract class Service : IService
    {
        public readonly IHttpClientFactory _clientFactory;
        public readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TokenAcquisitionService _tokenAcquisitionService;

        public Service(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenAcquisitionService tokenAcquisitionService)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
            _tokenAcquisitionService = tokenAcquisitionService;
        }

        public async Task<HttpClient> GetNMPAPIClient()
        {
            ClaimsPrincipal? user = _httpContextAccessor?.HttpContext?.User;
            var identity = user?.Identity as ClaimsIdentity;
            if (user!= null && !_tokenAcquisitionService.IsTokenValid(user, out DateTime expiration))
            {
                
                var refreshToken = identity?.FindFirst("refresh_token")?.Value;
                var issuer = identity?.FindFirst("issuer")?.Value;
                if(string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(issuer))
                {
                    throw new MsalUiRequiredException("401", "Token expired, need to re login");
                }
                OAuthTokenResponse oauthTokenResponse = await _tokenAcquisitionService.AcquireTokenByRefreshTokenAsync(refreshToken, issuer);
                if (oauthTokenResponse != null)
                {
                    // Optionally, update the authentication cookie with the new access token                            
                    identity?.RemoveClaim(identity.FindFirst("access_token"));
                    identity?.AddClaim(new Claim("access_token", oauthTokenResponse.AccessToken));
                    identity?.RemoveClaim(identity.FindFirst("refresh_token"));
                    identity?.AddClaim(new Claim("refresh_token", oauthTokenResponse.RefeshToken));
                    identity?.RemoveClaim(identity.FindFirst("access_token_expiry"));                    
                    identity?.AddClaim(new Claim("access_token_expiry", oauthTokenResponse.ExpiresOn));
                }                
            }
            var accessToken = identity?.FindFirst("access_token")?.Value; // _httpContextAccessor?.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
            HttpClient httpClient = _clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
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
