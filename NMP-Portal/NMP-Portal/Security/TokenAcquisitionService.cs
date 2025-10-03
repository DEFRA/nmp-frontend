using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Claims;

namespace NMP.Portal.Security
{
    public class TokenAcquisitionService
    {
        
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        public TokenAcquisitionService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OAuthTokenResponse> AcquireTokenByRefreshTokenAsync(string refreshToken, string issuer)
        {
            try
            {
                OAuthTokenResponse oauthTokenResponse = await GetAccessTokenByRefreshTokenAsync(refreshToken, issuer);
                return oauthTokenResponse;
            }
            catch (MsalUiRequiredException)
            {
                // This exception means you need to prompt the user for re-authentication
                throw;
            }
            catch (MsalServiceException ex)
            {
                // Handle exception (e.g., logging, rethrowing, etc.)
                throw;
            }
        }

        public bool IsTokenValid(ClaimsPrincipal user, out DateTime expiration)
        {
            var expClaim = user.FindFirst("access_token_expiry");
            if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
            {
                expiration = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                return expiration > DateTime.UtcNow.AddMinutes(20); // Check if token is valid for at least another 5 minutes
            }

            expiration = DateTime.MinValue;
            return false;
        }

        private async Task<OAuthTokenResponse> GetAccessTokenByRefreshTokenAsync(string refreshToken, string issuer)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                string scopes = "openid profile offline_access " + _configuration["CustomerIdentityClientId"];

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("redirect_uri", ""),
                    new KeyValuePair<string, string>("client_id", _configuration["CustomerIdentityClientId"]),
                    new KeyValuePair<string, string>("client_secret", _configuration["CustomerIdentityClientSecret"]),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("scope", scopes)
                });
                Uri uri = new Uri(issuer);
                var url = $"https://{uri.Authority}/{_configuration["CustomerIdentityTenantId"]}/{_configuration["CustomerIdentityPolicyId"]}/oauth2/v2.0/token";

                var response = await client.PostAsync(url, formData);
                var json = await response.Content.ReadAsStringAsync();
                var oauthTokenResponse = JsonConvert.DeserializeObject<OAuthTokenResponse>(json);
                return oauthTokenResponse;
            }
        }
    }
}
