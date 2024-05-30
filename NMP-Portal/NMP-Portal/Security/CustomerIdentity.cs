using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace NMP.Portal.Security
{

    public static class CustomerIdentity
    {
        private static IConfiguration? configuration = null;
        public static IServiceCollection AddDefraCustomerIdentity(this IServiceCollection services, WebApplicationBuilder builder)
        {
            configuration = builder.Configuration;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(options =>
                    {
                        options.Instance = builder.Configuration?["CustomerIdentityInstance"];
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
            //ProtocolMessage.IdToken = context.HttpContext.Session.GetString("JwtToken");
            //context.ProtocolMessage.IdTokenHint = context.HttpContext.Session.GetString("JwtToken");
            //context.Request.Method = "POST";
            //context.ProtocolMessage.Parameters.Add("post_logout_redirect_uri", context.Request.PathBase);
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
            string token = context.SecurityToken.RawData;
            context.HttpContext.Session.SetString("JwtToken", token);
            context.HttpContext.Session.SetObjectAsJson("JwtPayload", context.SecurityToken.Payload);
            IEnumerable<Claim> claims = context.SecurityToken.Claims;
            if (claims.Any())
            {
                Guid userIdentifier = Guid.Parse(claims.FirstOrDefault(c => c.Type == "sub").Value);
                var firstName = claims.FirstOrDefault(c => c.Type == "firstName").Value;
                var surname = claims.FirstOrDefault(c => c.Type == "lastName").Value;
                var email = claims.FirstOrDefault(c => c.Type == "email").Value; //currentRelationshipId"];
                var relationShips = claims.FirstOrDefault(c => c.Type == "relationships").Value;
                string[] relationshipClaimArray = relationShips.Split(":");
                var organisationName = relationshipClaimArray[5] == "Employee" ? relationshipClaimArray[2] : $"{firstName} {surname}";
                Guid organisationId = relationshipClaimArray[5] == "Employee" ? Guid.Parse(relationshipClaimArray[1]) : Guid.Parse(relationshipClaimArray[0]);
                UserData userData = new UserData()
                {
                    User = new User()
                    {
                        GivenName = firstName,
                        Surname = surname,
                        Email = email,
                        UserIdentifier = userIdentifier
                    },
                    Organisation = new Organisation()
                    {
                        ID = organisationId,
                        Name = organisationName
                    }
                };

                int userId = 0;

                try
                {
                    string jsonData = JsonConvert.SerializeObject(userData);
                    using HttpClient httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(configuration["NMPApiUrl"]);
                    httpClient.DefaultRequestHeaders.Add("Authorization", token);
                    var response = await httpClient.PostAsync(APIURLHelper.AddOrUpdateUserAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
                    {
                        userId = responseWrapper?.Data?["UserID"];
                    }
                    else
                    {
                        if (responseWrapper != null && responseWrapper?.Error != null)
                        {
                            throw new Exception(responseWrapper?.Error);
                        }
                    }
                }
                catch (HttpRequestException hre)
                {
                    throw new Exception(hre.Message);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                context.HttpContext.Items.Add("UserId", userId);
                context.HttpContext.Session.SetInt32("UserId", userId);
            }

            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
