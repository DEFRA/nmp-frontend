using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using NMP.Portal.Helpers;
using System.Net;
using System.Security.Claims;

namespace NMP.Portal.Security
{
    
    public static class CustomerIdentity
    {     
        

        public static IServiceCollection AddDefraCustomerIdentity(this IServiceCollection services, WebApplicationBuilder builder )
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            options.Instance = builder.Configuration["CustomerIdentityInstance"];
            options.ClientId = builder.Configuration["CustomerIdentityClientId"];
            options.ClientSecret = builder.Configuration["CustomerIdentityClientSecret"];
            options.TenantId = builder.Configuration["CustomerIdentityTenantId"];
            options.Domain = builder.Configuration["CustomerIdentityDomain"];
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
            options.Events.OnAuthenticationFailed += OnAuthenticationFailed;
            options.Events.OnRemoteSignOut += OnRemoteSignOut;
            

        })
        .Services.AddTokenAcquisition()
        .AddInMemoryTokenCaches();

            return services;
        }

        private static async Task OnRemoteSignOut(RemoteSignOutContext context)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnAuthenticationFailed(AuthenticationFailedContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnSignedOutCallbackRedirect(RemoteSignOutContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            //.ProtocolMessage.IdToken = context.HttpContext.Session.GetString("JwtToken");
            //context.ProtocolMessage.IdTokenHint = context.HttpContext.Session.GetString("JwtToken");
            context.Request.Method = "POST";
            //context.ProtocolMessage.Parameters.Add("login_redirect_url", context.Request.PathBase);
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
            IEnumerable<Claim> claims = context.SecurityToken.Claims;
            if (claims.Any())
            {
                var userIdentifier = claims.FirstOrDefault(c => c.Type == "sub").Value;
                var firstName = claims.FirstOrDefault(c => c.Type == "firstName").Value;
                var surname = claims.FirstOrDefault(c => c.Type == "lastName").Value; ;
                var email = claims.FirstOrDefault(c => c.Type == "email").Value; //currentRelationshipId"];
                var relationShips = claims.FirstOrDefault(c => c.Type == "relationships").Value;
                string[] relationshipClaimArray = relationShips.Split(":");
                var organisationName = relationshipClaimArray[5] == "Employee" ? relationshipClaimArray[2] : $"{firstName} {surname}";
                var organisationId = relationshipClaimArray[5] == "Employee" ? relationshipClaimArray[1] : relationshipClaimArray[0];
                context.HttpContext.Items.Add("UserId", 1);
                context.HttpContext.Session.SetInt32("UserId", 1);
            }

            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }                
    }
}
