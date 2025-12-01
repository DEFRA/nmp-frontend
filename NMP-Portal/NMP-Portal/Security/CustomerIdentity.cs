using AspNetCoreGeneratedDocument;
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
using OpenTelemetry.Trace;
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

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(options =>
            {
                options.Instance = builder.Configuration?["CustomerIdentityInstance"] ?? string.Empty;
                options.ClientId = builder.Configuration?["CustomerIdentityClientId"];
                options.ClientSecret = builder.Configuration?["CustomerIdentityClientSecret"];
                options.TenantId = builder.Configuration?["CustomerIdentityTenantId"];
                options.Domain = builder.Configuration?["CustomerIdentityDomain"];
                var extraQueryParameters = new Dictionary<string, string>();
                extraQueryParameters.Add("serviceId", value: builder.Configuration["CustomerIdentityServiceId"].ToString());
                extraQueryParameters.Add("forceReselection", value: "true");
                options.ExtraQueryParameters = extraQueryParameters;
                options.CallbackPath = "/signin-oidc";
                options.SignUpSignInPolicyId = builder.Configuration["CustomerIdentityPolicyId"];
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
            },
            cookieOptions =>
            {
                // How long your app's cookie is valid
                cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(60); // e.g. 8 hours
                cookieOptions.SlidingExpiration = true;
            })
            .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "openid", "profile", "offline_access", builder.Configuration["CustomerIdentityClientId"] })
            .AddInMemoryTokenCaches();

            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
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
                options.Events.OnRemoteFailure += OnRemoteFailure;

            });
            services.AddTokenAcquisition();
            services.AddInMemoryTokenCaches();
            services.AddSingleton<TokenRefreshService>();
            services.AddSingleton<TokenAcquisitionService>();
            return services;
        }

        private static async Task OnRemoteFailure(RemoteFailureContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            string traceId = context.HttpContext.TraceIdentifier;
            string path = context.HttpContext.Request.Path;
            string ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            string error = context.Failure?.Message ?? "Unknown";
            string errorCode = GetOidcErrorCode(error);
            logger.LogWarning("OIDC Remote Failure:{Code} | Path:{Path} | Trace:{TraceId} | IP:{IP}",
                errorCode, path, traceId, ip);            
            context.Response.Redirect($"/Error/503");
            context.HandleResponse(); // Prevents throwing exception
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static string GetOidcErrorCode(string src)
        {
            if (src.Contains("login_required", StringComparison.OrdinalIgnoreCase))
            {
                return "login_required";
            }
            else if (src.Contains("consent_required", StringComparison.OrdinalIgnoreCase))
            {
                return "consent_required";
            }
            else if (src.Contains("interaction_required", StringComparison.OrdinalIgnoreCase))
            {
                return "interaction_required";
            }
            else if (src.Contains("access_denied", StringComparison.OrdinalIgnoreCase))
            {
                return "access_denied";
            }
            else if (src.Contains("invalid_request", StringComparison.OrdinalIgnoreCase))
            {
                return "invalid_request";
            }
            else if (src.Contains("server_error", StringComparison.OrdinalIgnoreCase))
            {
                return "server_error";
            }
            else
            {
                return "unknown";
            }
        }

        private static async Task OnRemoteSignOut(RemoteSignOutContext context)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnAuthenticationFailed(AuthenticationFailedContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            string traceId = context.HttpContext.TraceIdentifier;
            string path = context.HttpContext.Request.Path;
            string ip = context.HttpContext.Connection.RemoteIpAddress.ToString();
            string error = context.Exception?.Message ?? "Unknown";
            string errorCode = GetOidcErrorCode(error);
            logger.LogWarning("OIDC Authentication Failed:{Code} | Path:{Path} | Trace:{TraceId} | IP:{IP}",
                errorCode, path, traceId, ip);
            context.Response.Redirect("/Error/503");
            context.HandleResponse(); // Suppress the exception
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task OnSignedOutCallbackRedirect(RemoteSignOutContext context)
        {
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// /* var logoutUri = configuration?["CustomerIdentityInstance"] + configuration?["CustomerIdentityDomain"] + "/" + configuration?["CustomerIdentityPolicyId"] + "/signout";
        //        var postLogoutUri = configuration?["CustomerIdentitySignedOutCallbackPath"]; //context.Properties.RedirectUri;
        //            if (!string.IsNullOrEmpty(postLogoutUri))
        //            {
        //                if (postLogoutUri.StartsWith("/"))
        //                {
        //                    var request = context.Request;
        //        postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
        //                }
        //    logoutUri += "?post_logout_redirect_uri=" + Uri.EscapeDataString(postLogoutUri);
        //            }
        //context.Response.Redirect(logoutUri);
        //context.HandleResponse(); */
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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
            if (!string.IsNullOrWhiteSpace(configuration?["CustomerIdentityReturnURI"]?.ToString()))
            {
                context.ProtocolMessage.RedirectUri = configuration?["CustomerIdentityReturnURI"]?.ToString(); // "https://your-gateway.com/signin-oidc";
            }
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
                    if (string.IsNullOrEmpty(accessToken))
                    { 
                        throw new AuthenticationFailureException(Resource.MsgAccessTokenNotReceived);
                    }

                    if (string.IsNullOrEmpty(refreshToken))                    
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
                                //case "exp":
                                //    identity?.AddClaim(new Claim("access_token_expiry", claim.Value));
                                //    break;
                                case "sub":
                                    userIdentifier = Guid.Parse(claim.Value);
                                    break;
                                case "firstName":
                                    firstName = claim.Value;
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
                                        identity?.AddClaim(new Claim("organisationName", organisationName));
                                        identity?.AddClaim(new Claim("organisationId", organisationId.ToString() ?? string.Empty));
                                    }
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
                                    break;

                                default:
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
                errorViewModel.Message = Resource.MsgNmptServiceNotAvailable;
                errorViewModel.Stack = string.Empty;
                context?.HttpContext.Session.SetObjectAsJson("Error", errorViewModel);
                context?.Response.Redirect("/Error/");
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
