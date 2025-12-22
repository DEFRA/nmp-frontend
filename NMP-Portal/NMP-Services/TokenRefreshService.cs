using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Claims;
using NMP.Core;
using NMP.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class TokenRefreshService(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _config = config;

    public async Task<string> RefreshUserAccessTokenAsync(HttpContext context)
    {
        var refreshToken = await context.GetTokenAsync("refresh_token");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            RedirectUri = context.Request.Path,
            AllowRefresh = true
        };

        if (refreshToken == null)
        {
            await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, authProperties);
        }

        ClaimsPrincipal? user = context.User;
        var identity = user?.Identity as ClaimsIdentity;
        string issuer = identity?.FindFirst("issuer")?.Value;
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
            Uri uri = new Uri(uriString: issuer);
            var url = $"https://{uri.Authority}/{_config["CustomerIdentityTenantId"]}/{_config["CustomerIdentityPolicyId"]}/oauth2/v2.0/token";

            var response = await client.PostAsync(url, formData);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            tokens = JsonConvert.DeserializeObject<OAuthTokenResponse>(json);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
            {
                await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, authProperties);
            }
            // Update authentication session
            var auth = await context.AuthenticateAsync();
            if (auth != null && auth.Principal != null && tokens != null && !string.IsNullOrEmpty(tokens.AccessToken))
            {
                auth.Properties?.UpdateTokenValue("access_token", tokens.AccessToken);
                auth.Properties?.UpdateTokenValue("refresh_token", tokens.RefreshToken);
                await context.SignInAsync(auth.Principal, auth.Properties);
            }
        }

        return tokens?.AccessToken ?? string.Empty;
    }
}
