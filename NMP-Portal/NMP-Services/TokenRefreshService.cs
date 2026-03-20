using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using NMP.Core;
using NMP.Core.Attributes;
using System.Security.Claims;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class TokenRefreshService(IHttpClientFactory httpClientFactory, IConfiguration config, IOptionsMonitor<OpenIdConnectOptions> openIdConnectOptions)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _config = config;
    private const string refreshTokenKey = "refresh_token";
    private readonly IOptionsMonitor<OpenIdConnectOptions> _openIdConnectOptions = openIdConnectOptions;
    public async Task<string> RefreshUserAccessTokenAsync(HttpContext context)
    {
        var refreshToken = await context.GetTokenAsync(refreshTokenKey);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            RedirectUri = context.Request.Path,
            AllowRefresh = true
        };

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, authProperties);
        }
               
        OAuthTokenResponse tokens;
        using (var client = _httpClientFactory.CreateClient())
        {
            string scopes = "openid profile offline_access " + _config["CustomerIdentityClientId"];

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(refreshTokenKey, refreshToken?? string.Empty),
                new KeyValuePair<string, string>("redirect_uri", ""),
                new KeyValuePair<string, string>("client_id", _config["CustomerIdentityClientId"]?? string.Empty),
                new KeyValuePair<string, string>("client_secret", _config["CustomerIdentityClientSecret"] ?? string.Empty),
                new KeyValuePair<string, string>("grant_type", refreshTokenKey),
                new KeyValuePair<string, string>("scope", scopes)
            });

            IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = GetConfigurationManager();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var metadata = await configurationManager?.GetConfigurationAsync(CancellationToken.None);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var url = metadata.TokenEndpoint;
            var response = await client.PostAsync(url, formData);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            tokens = JsonConvert.DeserializeObject<OAuthTokenResponse>(json);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
            {
                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
            }
            // Update authentication session
            var auth = await context.AuthenticateAsync();
            if (auth != null && auth.Principal != null && tokens != null && !string.IsNullOrEmpty(tokens.AccessToken))
            {
                auth.Properties?.UpdateTokenValue("access_token", tokens.AccessToken);
                auth.Properties?.UpdateTokenValue(refreshTokenKey, tokens.RefreshToken);
                await context.SignInAsync(auth.Principal, auth.Properties);
            }
        }

        return tokens?.AccessToken ?? string.Empty;
    }

    private IConfigurationManager<OpenIdConnectConfiguration>? GetConfigurationManager()
    {
        // Use the SAME scheme name you registered
        var options = _openIdConnectOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);

        return options.ConfigurationManager;
    }
}
