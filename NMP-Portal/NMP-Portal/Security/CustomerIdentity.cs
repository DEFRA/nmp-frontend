using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using NMP.Portal.Services;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Runtime.ConstrainedExecution;
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
                        extraQueryParameters.Add("forceReselection", value: "true");
                        options.ExtraQueryParameters = extraQueryParameters;
                        options.CallbackPath = "/signin-oidc";
                        //options.CallbackPath = builder.Configuration["CustomerIdentityCallbackPath"]; // "/signin-oidc";
                        //options.SignedOutCallbackPath = builder.Configuration["CustomerIdentitySignedOutCallbackPath"];
                        options.SignUpSignInPolicyId = builder.Configuration["CustomerIdentityPolicyId"];
                        options.ErrorPath = "/Error/Index";
                    },
                    cookieOptions =>
                    {
                        // How long your app's cookie is valid
                        cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(10); // e.g. 8 hours
                        cookieOptions.SlidingExpiration = true;
                        // optionally:
                        //cookieOptions.Cookie.MaxAge = cookieOptions.ExpireTimeSpan;
                    })
                    .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "openid", "profile", "offline_access", builder.Configuration["CustomerIdentityClientId"] })
                    .AddInMemoryTokenCaches();

            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                //options.ResponseType = OpenIdConnectResponseType.CodeToken;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;  // Save tokens in the authentication session
                options.Scope.Add("openid profile offline_access");
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnAuthorizationCodeReceived += OnAuthorizationCodeReceived;
                options.Events.OnRedirectToIdentityProvider += OnRedirectToIdentityProvider;
                options.Events.OnAccessDenied += OnAccessDenied;
                options.Events.OnRedirectToIdentityProviderForSignOut += OnRedirectToIdentityProviderForSignOut;
                options.Events.OnTokenValidated += OnTokenValidated;
                options.Events.OnSignedOutCallbackRedirect += OnSignedOutCallbackRedirect;
                options.Events.OnAuthenticationFailed += OnAuthenticationFailed;
                options.Events.OnRemoteSignOut += OnRemoteSignOut;

            });
            //services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            //{
            //    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Set cookie expiration time
            //    options.SlidingExpiration = true; // Enable sliding expiration
            //    options.Cookie.MaxAge = options.ExpireTimeSpan;

            //});
            services.AddTokenAcquisition();
            services.AddInMemoryTokenCaches();
            services.AddSingleton<TokenAcquisitionService>();
            return services;
        }

        private static async Task OnRemoteSignOut(RemoteSignOutContext context)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnAuthenticationFailed(AuthenticationFailedContext context)
        {
            var errorViewModel = new ErrorViewModel();
            errorViewModel.Message = context.Exception.Message;
            errorViewModel.Stack = context.Exception.StackTrace ?? string.Empty;
            context.HttpContext.Session.SetObjectAsJson("Error", errorViewModel);
            context.Response.Redirect("/Error");
            context.HandleResponse(); // Suppress the exception
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
            //var logoutUri = configuration?["CustomerIdentityInstance"] + configuration?["CustomerIdentityDomain"] + "/" + configuration?["CustomerIdentityPolicyId"] + "/signout";
            //var postLogoutUri = configuration?["CustomerIdentitySignedOutCallbackPath"]; //context.Properties.RedirectUri;
            //if (!string.IsNullOrEmpty(postLogoutUri))
            //{
            //    if (postLogoutUri.StartsWith("/"))
            //    {
            //        var request = context.Request;
            //        postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
            //    }
            //    logoutUri += "?post_logout_redirect_uri=" + Uri.EscapeDataString(postLogoutUri);
            //}
            //context.Response.Redirect(logoutUri);
            //context.HandleResponse();
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
            if (!string.IsNullOrWhiteSpace(configuration?["CustomerIdentityReturnURI"]?.ToString()))
            {
                context.ProtocolMessage.RedirectUri = configuration?["CustomerIdentityReturnURI"]?.ToString(); // "https://your-gateway.com/signin-oidc";
            }

            //context.ProtocolMessage.Parameters.Add("serviceId", configuration["CustomerIdentityServiceId"].ToString());
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
            var accessToken = context?.TokenEndpointResponse?.AccessToken;
            var refreshToken = context?.TokenEndpointResponse?.RefreshToken;
            var identity = context?.Principal?.Identity as ClaimsIdentity;
            try
            {
                if (identity != null)
                {
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        identity?.AddClaim(new Claim("access_token", accessToken));
                    }
                    else
                    {
                        throw new AuthenticationFailureException(Resource.MsgAccessTokenNotReceived);
                    }

                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        identity?.AddClaim(new Claim("refresh_token", refreshToken));
                    }
                    else
                    {
                        throw new AuthenticationFailureException(Resource.MsgRrefreshTokenNotReceived);
                    }

                }
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;

                    if (jsonToken != null)
                    {
                        int userId = 0;

                        Guid? userIdentifier = null;
                        string firstName = string.Empty;
                        string lastName = string.Empty;
                        string email = string.Empty;
                        string currentRelationShipId = string.Empty;
                        string organisationName = string.Empty;
                        Guid? organisationId = null;

                        foreach (var claim in jsonToken.Claims)
                        {
                            // Add the claims to the ClaimsIdentity
                            switch (claim.Type)
                            {
                                case "iss":
                                    identity?.AddClaim(new Claim("issuer", claim.Value));
                                    break;
                                case "exp":
                                    identity?.AddClaim(new Claim("access_token_expiry", claim.Value));
                                    break;
                                case "sub":
                                    userIdentifier = Guid.Parse(claim.Value);
                                    //identity?.AddClaim(claim);
                                    break;
                                case "firstName":
                                    firstName = claim.Value;
                                    //identity?.AddClaim(claim);
                                    break;
                                case "lastName":
                                    lastName = claim.Value;
                                    //identity?.AddClaim(claim);
                                    break;
                                case "email":
                                    email = claim.Value;
                                    //identity?.AddClaim(claim);
                                    break;
                                case "currentRelationshipId":
                                    currentRelationShipId = claim.Value;
                                    //identity?.AddClaim(claim);
                                    break;
                                case "enrolmentCount":
                                    identity?.AddClaim(new Claim("enrolmentCount", claim.Value));
                                    break;
                                case "relationships":
                                    List<string> relationShipsArray = new List<string>();
                                    List<string> relationShipDetails = new List<string>();
                                    if (claim.Value.GetType().IsArray)
                                    {
                                        relationShipsArray.AddRange(claim.Value.Split(","));
                                    }
                                    else
                                    {
                                        relationShipsArray.Add(claim.Value);
                                    }
                                    var rs = relationShipsArray.FirstOrDefault(r => r.Contains(currentRelationShipId));
                                    if (rs != null)
                                    {
                                        relationShipDetails.AddRange(rs.Split(":"));
                                        if (relationShipDetails[4] == "Citizen")
                                        {
                                            organisationName = $"{firstName} {lastName}";
                                            organisationId = Guid.Parse(relationShipDetails[0]);
                                        }
                                        else
                                        {
                                            organisationName = relationShipDetails[2];
                                            organisationId = Guid.Parse(relationShipDetails[1]);
                                        }
                                        //identity?.AddClaim(new Claim("isCitizen", isCitizen.ToString()));
                                        identity?.AddClaim(new Claim("organisationName", organisationName));
                                        identity?.AddClaim(new Claim("organisationId", organisationId.ToString() ?? string.Empty));
                                    }
                                    //identity?.RemoveClaim(claim);
                                    break;

                                case "roles":
                                    List<string> rolesArray = new List<string>();
                                    List<string> roleDetails = new List<string>();
                                    if (claim.Value.GetType().IsArray)
                                    {
                                        rolesArray.AddRange(claim.Value.Split(","));
                                    }
                                    else
                                    {
                                        rolesArray.Add(claim.Value);
                                    }
                                    var rd = rolesArray.FirstOrDefault(r => r.Contains(currentRelationShipId));
                                    if (rd != null)
                                    {
                                        roleDetails.AddRange(rd.Split(":"));
                                        identity?.AddClaim(new Claim(ClaimTypes.Role, roleDetails[1]));
                                        identity?.AddClaim(new Claim("roleStatus", roleDetails[2]));
                                    }
                                    //identity?.RemoveClaim(claim);
                                    break;

                                default:
                                    //identity?.AddClaim(claim);
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


                        string jsonData = JsonConvert.SerializeObject(userData);
                        if (configuration != null)
                        {
                            using HttpClient httpClient = new HttpClient();
                            httpClient.Timeout = TimeSpan.FromMinutes(5);
                            httpClient.BaseAddress = new Uri(configuration["NMPApiUrl"] ?? "http://localhost:3000/");
                            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                            var response = await httpClient.PostAsync(APIURLHelper.AddOrUpdateUserAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));

                            if (response.IsSuccessStatusCode)
                            {
                                string result = await response.Content.ReadAsStringAsync();
                                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                                if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
                                {
                                    userId = responseWrapper?.Data?["UserID"];
                                    identity?.AddClaim(new Claim("userId", userId.ToString()));
                                }
                                else
                                {
                                    if (responseWrapper != null && responseWrapper?.Error != null)
                                    {
                                        throw new Exception(Resource.MsgNmptServiceNotAvailable);
                                    }
                                }

                            }
                            else if (response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                throw new Exception(Resource.MsgNmptApiServiceBlockedAccess);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(Resource.MsgInvalidAuthentication);
                    }
                }
                else
                {
                    throw new AuthenticationFailureException(Resource.MsgAccessTokenNotReceived);
                }

            }
            catch (HttpRequestException ex)
            {
                var errorViewModel = new ErrorViewModel();
                //errorViewModel.Code= (int)ex.StatusCode;
                errorViewModel.Message = Resource.MsgNmptServiceNotAvailable;
                errorViewModel.Stack = string.Empty;
                context?.HttpContext.Session.SetObjectAsJson("Error", errorViewModel);
                context?.Response.Redirect("/Error");
                context?.HandleResponse(); // Suppress the exception 
            }
            catch (Exception ex)
            {
                var errorViewModel = new ErrorViewModel();
                errorViewModel.Message = ex.Message;
                errorViewModel.Stack = string.Empty;
                context?.HttpContext.Session.SetObjectAsJson("Error", errorViewModel);
                context?.Response.Redirect("/Error");
                context?.HandleResponse(); // Suppress the exception                    
            }
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
