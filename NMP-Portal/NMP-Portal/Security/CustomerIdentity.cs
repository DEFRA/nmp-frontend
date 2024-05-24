using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using NMP.Portal.Helpers;

namespace NMP.Portal.Security
{
    
    public static class CustomerIdentity
    {       

        public static IServiceCollection AddDefraCustomerIdentity(this IServiceCollection services, WebApplicationBuilder builder )
        {
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            options.Instance = builder.Configuration["CustomerIdentityInstance"];
            options.ClientId = builder.Configuration["CustomerIdentityClientId"];
            options.ClientSecret = builder.Configuration["CustomerIdentityClientSecret"];
            options.TenantId = builder.Configuration["CustomerIdentityTenantId"];
            options.Domain = builder.Configuration["CustomerIdentityDomain"]; ;
            var extraQueryParameters = new Dictionary<string, string>();
            extraQueryParameters.Add("serviceId", value: builder.Configuration["CustomerIdentityServiceId"].ToString());
            options.ExtraQueryParameters = extraQueryParameters;
            options.CallbackPath = builder.Configuration["CustomerIdentityCallbackPath"]; // signin-oidc";
            options.SignedOutCallbackPath = builder.Configuration["CustomerIdentitySignedOutCallbackPath"];
            options.SignUpSignInPolicyId = builder.Configuration["CustomerIdentityPolicyId"];
            
            options.ErrorPath = "/Error/index";

            options.Events ??= new OpenIdConnectEvents();
            options.Events.OnAuthorizationCodeReceived += OnAuthorizationCodeReceived;
            options.Events.OnRedirectToIdentityProvider += OnRedirectToIdentityProvider;
            options.Events.OnAccessDenied += OnAccessDenied;
            options.Events.OnRedirectToIdentityProviderForSignOut += OnRedirectToIdentityProviderForSignOut;
            options.Events.OnTokenValidated += OnTokenValidated;
            options.Events.OnSignedOutCallbackRedirect += OnSignedOutCallbackRedirect;
            
        })
        .Services.AddTokenAcquisition()
        .AddInMemoryTokenCaches();

            return services;
        }

        private static async Task OnSignedOutCallbackRedirect(RemoteSignOutContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnAccessDenied(AccessDeniedContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnRedirectToIdentityProvider(RedirectContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnTokenValidated(TokenValidatedContext context)
        {
            context.HttpContext.Session.SetString("JwtToken", context.SecurityToken.RawData);
            context.HttpContext.Session.SetObjectAsJson("JwtPayload", context.SecurityToken.Payload);
            //var jwtPayload = context.SecurityToken.Payload;
            context.HttpContext.Items.Add("UserId", 1);
            context.HttpContext.Session.SetInt32("UserId", 1);

            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }                
    }
}
