using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace NMP.Portal.Security
{
    public class ReauthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TokenAcquisitionService _tokenAcquisitionService;
        private readonly IConfiguration _configuration;

        public ReauthenticationMiddleware(RequestDelegate next, TokenAcquisitionService tokenAcquisitionService, IConfiguration configuration)
        {
            _next = next;
            _tokenAcquisitionService = tokenAcquisitionService;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var userIdentofier = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var scopes = new string[] { "openid", "offline_access" };
                var tenantId = _configuration["CustomerIdentityTenantId"];
                // "dcidmtest";// "131a35fb-0422-49c9-8753-15217cec5411";
                //var refreshToken = context.User.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value;
                var homeAccountId = $"{userIdentofier}.{tenantId}";
                try
                {
                    if (!string.IsNullOrEmpty(userIdentofier))
                    {
                        if (!_tokenAcquisitionService.IsTokenValid(context.User, out DateTime expiration))
                        {


                            var accessToken = await _tokenAcquisitionService.AcquireTokenSilentAsync(homeAccountId, scopes);
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                // Optionally, update the authentication cookie with the new access token
                                var identity = context.User.Identity as ClaimsIdentity;
                                identity?.RemoveClaim(identity.FindFirst("access_token"));
                                identity?.AddClaim(new Claim("access_token", accessToken));
                                identity?.RemoveClaim(identity.FindFirst("access_token_expiry"));
                                identity?.AddClaim(new Claim("access_token_expiry", DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString()));

                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = true
                                };
                                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
                            }
                            else
                            {
                                throw new MsalUiRequiredException("401", "Token expired, need to re login");
                            }
                        }

                    }
                }
                catch (MsalUiRequiredException)
                {
                    await context.ChallengeAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            await _next(context);
        }
    }
}
