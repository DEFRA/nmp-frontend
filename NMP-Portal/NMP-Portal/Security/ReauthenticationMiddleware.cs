using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Principal;

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
                try
                {
                    if (!_tokenAcquisitionService.IsTokenValid(context.User, out DateTime expiration))
                    {
                        var identity = context.User.Identity as ClaimsIdentity;
                        var refreshToken = identity?.FindFirst("refresh_token")?.Value;
                        string issuer = identity?.FindFirst("issuer")?.Value;
                        OAuthTokenResponse oauthTokenResponse = await _tokenAcquisitionService.AcquireTokenByRefreshTokenAsync(refreshToken, issuer);
                        if (oauthTokenResponse != null)
                        {
                            // Optionally, update the authentication cookie with the new access token                            
                            identity?.RemoveClaim(identity.FindFirst("access_token"));
                            identity?.AddClaim(new Claim("access_token", oauthTokenResponse.AccessToken));
                            identity?.RemoveClaim(identity.FindFirst("refresh_token"));
                            identity?.AddClaim(new Claim("refresh_token", oauthTokenResponse.RefeshToken));
                            identity?.RemoveClaim(identity.FindFirst("access_token_expiry"));
                            //identity?.AddClaim(new Claim("access_token_expiry", DateTimeOffset.UtcNow.AddMilliseconds(60).ToUnixTimeSeconds().ToString()));
                            identity?.AddClaim(new Claim("access_token_expiry", oauthTokenResponse.ExpiresOn));

                            //var authProperties = new AuthenticationProperties
                            //{
                            //    IsPersistent = true,
                            //    RedirectUri = context.Request.Path,
                            //    AllowRefresh = true
                            //};
                            //await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
                        }
                        else
                        {
                            throw new MsalUiRequiredException("401", "Token expired, need to re login");
                        }
                    }
                }
                catch (MsalUiRequiredException)
                {
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        RedirectUri = context.Request.Path,
                        AllowRefresh = true
                    };
                    await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, authProperties);
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
