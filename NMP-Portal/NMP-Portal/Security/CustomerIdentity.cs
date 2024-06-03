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
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
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
                        //options.ClientSecret = builder.Configuration["CustomerIdentityClientSecret"];
                        options.TenantId = builder.Configuration["CustomerIdentityTenantId"];
                        options.Domain = builder.Configuration["CustomerIdentityDomain"];
                        var extraQueryParameters = new Dictionary<string, string>();
                        extraQueryParameters.Add("serviceId", value: builder.Configuration["CustomerIdentityServiceId"].ToString());
                        options.ExtraQueryParameters = extraQueryParameters;
                        options.CallbackPath = builder.Configuration["CustomerIdentityCallbackPath"]; // signin-oidc";
                        //options.SignedOutCallbackPath = builder.Configuration["CustomerIdentitySignedOutCallbackPath"];
                        options.SignUpSignInPolicyId = builder.Configuration["CustomerIdentityPolicyId"];
                        //options.RefreshInterval = TimeSpan.FromMinutes(19);
                       
                        //options.UseTokenLifetime = true;
                        //options.SaveTokens = true;
                        options.ResponseType = "code";
                        options.Scope.Add("openid");
                        options.Scope.Add("offline_access");
                        options.Scope.Add(options.ClientId?? string.Empty);
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
                        options.Events.OnUserInformationReceived += OnUserInformationReceived;
                    })
                    .Services.AddTokenAcquisition()
                    .AddInMemoryTokenCaches();

            return services;
        }

        private static async Task OnUserInformationReceived(UserInformationReceivedContext context)
        {
            await Task.CompletedTask.ConfigureAwait(false);
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
            var code = context.ProtocolMessage.Code;
            //using HttpClient httpClient = new HttpClient();
            //httpClient.BaseAddress = new Uri(context.TokenEndpointRequest.TokenEndpoint);
            //var response = await httpClient.PostAsync(null, new StringContent(jsonData, mediaType: MediaTypeHeaderValue. "application/x-www-form-urlencoded"));
            //string result = await response.Content.ReadAsStringAsync();

            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnTokenValidated(TokenValidatedContext context)
        {
            string token = context.SecurityToken.RawData;
            context.HttpContext.Session.SetString("JwtToken", token);
            //context.HttpContext.Session.SetObjectAsJson("JwtPayload", context.SecurityToken.Payload);
            IEnumerable<Claim> claims = context.SecurityToken.Claims;
            if (claims.Any())
            {
                Guid? userIdentifier= null;
                string firstName = string.Empty;
                string lastName = string.Empty;
                string email = string.Empty;
                string currentRelationShipId = string.Empty;
                string organisationName = string.Empty;
                Guid? organisationId = null;
                List<string> relationShipsArray = new List<string>();
                List<string> relationShipDetails = new List<string>(); 
                foreach (var claim in claims)
                {
                    switch(claim.Type)
                    {
                        case "sub":
                            userIdentifier = Guid.Parse(claim.Value);
                            break;
                        case "firstName":
                            firstName= claim.Value;
                            break;
                        case "lastName":
                            lastName = claim.Value;
                            break;
                        case "email":
                            email = claim.Value;
                            break;
                        case "currentRelationshipId":
                            currentRelationShipId = claim.Value;
                            break;
                        case "relationships":
                            if(claim.Value.GetType().IsArray)
                            {
                                relationShipsArray.AddRange(claim.Value.Split(","));
                            }
                            else
                            {
                                relationShipsArray.Add(claim.Value);
                            }
                            var rs = relationShipsArray.FirstOrDefault(r => r.Contains(currentRelationShipId));
                            if(rs != null)
                            {
                                relationShipDetails.AddRange(rs.Split(":"));
                                if(relationShipDetails[4] == "Citizen")
                                {
                                    organisationName = $"{firstName} {lastName}";
                                    organisationId = Guid.Parse(relationShipDetails[0]);
                                }
                                else
                                {
                                    organisationName = relationShipDetails[2];
                                    organisationId = Guid.Parse(relationShipDetails[1]);
                                } 
                            }                            
                            break;
                    }
                }                
                
                UserData userData = new UserData()
                {
                    User = new User()
                    {
                        GivenName = firstName,
                        Surname = lastName,
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
                    if(configuration != null)
                    {
                        using HttpClient httpClient = new HttpClient();
                        httpClient.BaseAddress = new Uri(configuration["NMPApiUrl"]?? "http://localhost:3000/");
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        var response = await httpClient.PostAsync(APIURLHelper.AddOrUpdateUserAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                        string result = await response.Content.ReadAsStringAsync();
                        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                        if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
                        {
                            userId = responseWrapper?.Data?["UserID"];
                        }
                        else if(response.StatusCode == HttpStatusCode.Unauthorized)
                        {

                        }
                        else
                        {
                            if (responseWrapper != null && responseWrapper?.Error != null)
                            {
                                throw new Exception(responseWrapper?.Error);
                            }
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
