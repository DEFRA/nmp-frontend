using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

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
                
                var scopes = new string[] { "openid", "offline_access", _configuration["CustomerIdentityClientId"].ToString() };
                
                var refreshToken = context.User.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value; 
                
                try
                {
                    
                        if (!_tokenAcquisitionService.IsTokenValid(context.User, out DateTime expiration))
                        {
                            string accessToken = await _tokenAcquisitionService.AcquireTokenByRefreshTokenAsync(refreshToken);
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
                                    IsPersistent = true,
                                    //RedirectUri = context.Request.Path,
                                    //AllowRefresh = true
                                };
                                await context.SignInAsync(OpenIdConnectDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);
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
                    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
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
