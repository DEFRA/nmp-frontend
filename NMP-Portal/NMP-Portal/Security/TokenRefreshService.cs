using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System.Security.Claims;

namespace NMP.Portal.Security
{
    public class TokenRefreshService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public TokenRefreshService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<string> RefreshUserAccessTokenAsync(HttpContext context)
        {
            var refreshToken = await context.GetTokenAsync("refresh_token");
            if (refreshToken == null)
            {
                return null;
            }

            ClaimsPrincipal? user = context.User;
            var identity = user?.Identity as ClaimsIdentity;
            var issuer = identity?.FindFirst("issuer")?.Value;
            OAuthTokenResponse tokens;
            using (var client = _httpClientFactory.CreateClient())
            {
                string scopes = "openid profile offline_access " + _config["CustomerIdentityClientId"];

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("redirect_uri", ""),
                    new KeyValuePair<string, string>("client_id", _config["CustomerIdentityClientId"]),
                    new KeyValuePair<string, string>("client_secret", _config["CustomerIdentityClientSecret"]),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("scope", scopes)
                });
                Uri uri = new Uri(issuer);
                var url = $"https://{uri.Authority}/{_config["CustomerIdentityTenantId"]}/{_config["CustomerIdentityPolicyId"]}/oauth2/v2.0/token";

                var response = await client.PostAsync(url, formData);
                var json = await response.Content.ReadAsStringAsync();
                tokens = JsonConvert.DeserializeObject<OAuthTokenResponse>(json);

                if(tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
                {
                    throw new Exception("Failed to refresh access token.");                    
                }
                // Update authentication session
                var auth = await context.AuthenticateAsync();
                auth?.Properties?.UpdateTokenValue("access_token", tokens.AccessToken);
                auth?.Properties?.UpdateTokenValue("refresh_token", tokens.RefreshToken);
                await context.SignInAsync(auth.Principal, auth.Properties);

            }

            return tokens.AccessToken;
        }
    }
}
