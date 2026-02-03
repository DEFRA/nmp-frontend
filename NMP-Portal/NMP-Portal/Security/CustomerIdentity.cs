using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using NMP.Portal.Helpers;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using NMP.Commons.ViewModels;

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
            .AddCookie("NMPCookie", cookieOptions =>
            {
                // How long your app's cookie is valid
                cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(60); // e.g. 8 hours
                cookieOptions.SlidingExpiration = true;
            })
            .AddMicrosoftIdentityWebApp(options =>
            {
                options.Instance = configuration?["CustomerIdentityInstance"] ?? string.Empty;
                options.ClientId = configuration?["CustomerIdentityClientId"];
                options.ClientSecret = configuration?["CustomerIdentityClientSecret"];
                options.TenantId = configuration?["CustomerIdentityTenantId"];
                options.Domain = configuration?["CustomerIdentityDomain"];
                var extraQueryParameters = new Dictionary<string, string>();
                string serviceId = configuration?["CustomerIdentityServiceId"] ?? string.Empty;
                extraQueryParameters.Add("serviceId", value: serviceId);
                extraQueryParameters.Add("forceReselection", value: "true");
                options.ExtraQueryParameters = extraQueryParameters;
                options.CallbackPath = "/signin-oidc";
                options.SignUpSignInPolicyId = configuration?["CustomerIdentityPolicyId"];
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ErrorPath = "/Error/index";
            },
            cookieOptions =>
            {
                // How long your app's cookie is valid
                cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(60); // e.g. 8 hours
                cookieOptions.SlidingExpiration = true;
            })
            .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "openid", "profile", "offline_access", configuration?["CustomerIdentityClientId"]?? string.Empty })
            .AddDistributedTokenCaches();                        

            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;  // Save tokens in the authentication session                
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
            services.AddDistributedTokenCaches();            
            services.AddSingleton<TokenRefreshService>();
            services.AddSingleton<TokenAcquisitionService>();
            return services;
        }

        private static async Task OnRemoteFailure(RemoteFailureContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            string traceId = context.HttpContext.TraceIdentifier;
            string path = context.HttpContext.Request.Path;
            string? ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            string error = context.Failure?.Message ?? "Unknown";
            string errorCode = GetOidcErrorCode(error);
            logger.LogError("OIDC Remote Failure:{Code} | Path:{Path} | Trace:{TraceId} | IP:{IP}", errorCode, path, traceId, ip);            
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
            string? ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            string error = context.Exception?.Message ?? "Unknown";
            string errorCode = GetOidcErrorCode(error);
            logger.LogError("OIDC Authentication Failed:{Code} | Path:{Path} | Trace:{TraceId} | IP:{IP}", errorCode, path, traceId, ip);
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
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            string traceId = context.HttpContext.TraceIdentifier;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            string path = context.HttpContext.Request.Path;
            string? ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();            
            try
            {
                var accessToken = context?.TokenEndpointResponse?.AccessToken;
                var refreshToken = context?.TokenEndpointResponse?.RefreshToken;
                var identity = context?.Principal?.Identity as ClaimsIdentity;

                CheckTokens(accessToken, refreshToken);

                if (identity != null)
                {
                    await RecordRequiredClaims(accessToken, identity);
                }
            }
            catch (HttpRequestException hre)
            {
                string error = hre.Message ?? Resource.MsgNmptServiceNotAvailable;                
                logger.LogError(hre,"NMPT service error :{Error} | Path:{Path} | Trace:{TraceId} | IP:{IP}",error, path, traceId, ip);                                
                context?.Response.Redirect("/Error/503");
                context?.HandleResponse(); // Suppress the exception 
            }
            catch (Exception ex)
            {                
                string error = ex.Message;
                logger.LogError(ex, "Error in login process :{Error} | Path:{Path} | Trace:{TraceId} | IP:{IP}", error, path, traceId, ip);
                context?.Response.Redirect("/Error/503");
                context?.HandleResponse(); // Suppress the exception                    
            }
            // Don't remove this line
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task RecordRequiredClaims(string? accessToken, ClaimsIdentity? identity)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                JwtSecurityToken? jwtToken = ParseAccessToken(accessToken);

                if (jwtToken != null)
                {
                    UserData userData = new UserData();
                    userData.User = new User();
                    userData.Organisation = new Organisation();

                    AddClaimsToIdentity(identity, jwtToken, userData);
                    
                    await SaveUserDetails(accessToken, identity, userData);
                }
                else
                {
                    throw new AuthenticationFailureException(Resource.MsgInvalidAuthentication);
                }
            }
            else
            {
                throw new AuthenticationFailureException(Resource.MsgAccessTokenNotReceived);
            }
        }

        private static async Task SaveUserDetails(string accessToken, ClaimsIdentity? identity, UserData userData)
        { 
            string jsonData = JsonConvert.SerializeObject(userData);
            if (configuration != null)
            {
                const string dafaultlocalUrl = $"http://localhost:3000/";
                using HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                httpClient.BaseAddress = new Uri(configuration["NMPApiUrl"] ?? dafaultlocalUrl);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.PostAsync(APIURLHelper.AddOrUpdateUserAsyncAPI, new StringContent(jsonData, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                    if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
                    {
                        var userId = responseWrapper?.Data?["UserID"];
                        identity?.AddClaim(new Claim("userId", userId?.ToString()));
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HttpRequestException(Resource.MsgNmptApiServiceBlockedAccess);
                }
            }
        }

        private static void AddClaimsToIdentity(ClaimsIdentity? identity, JwtSecurityToken jwtToken, UserData userData)
        {
            userData.User = new User();
            userData.Organisation = new Organisation();
            string currentRelationShipId = string.Empty;
            string organisationName = string.Empty;
            Guid? organisationId = null;

            foreach (var claim in jwtToken.Claims)
            {
                // Add the claims to the ClaimsIdentity
                switch (claim.Type)
                {
                    case "iss":
                        identity?.AddClaim(new Claim("issuer", claim.Value));
                        break;
                    case "sub":
                        userData.User.UserIdentifier = Guid.Parse(claim.Value);
                        break;
                    case "firstName":
                        userData.User.GivenName = claim.Value;
                        break;
                    case "lastName":
                        userData.User.Surname = claim.Value;
                        break;
                    case "email":
                        userData.User.Email = claim.Value;
                        break;
                    case "currentRelationshipId":
                        currentRelationShipId = claim.Value;
                        break;
                    case "enrolmentCount":
                        identity?.AddClaim(new Claim("enrolmentCount", claim.Value));
                        break;
                    case "relationships":
                        ParseOrganisationData(identity, userData, currentRelationShipId, ref organisationName, ref organisationId, claim);
                        break;
                    case "roles":
                        ParseRolesData(identity, currentRelationShipId, claim);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void ParseRolesData(ClaimsIdentity? identity, string currentRelationShipId, Claim claim)
        {
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
        }

        private static void ParseOrganisationData(ClaimsIdentity? identity, UserData userData, string currentRelationShipId, ref string organisationName, ref Guid? organisationId, Claim claim)
        {
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
                    organisationName = $"{userData.User.GivenName} {userData.User.Surname}";
                    organisationId = Guid.Parse(relationShipDetails[0]);
                }
                else
                {
                    organisationName = relationShipDetails[2];
                    organisationId = Guid.Parse(relationShipDetails[1]);
                }
                userData.Organisation.ID = organisationId;
                userData.Organisation.Name = organisationName;
                identity?.AddClaim(new Claim("organisationName", organisationName));
                identity?.AddClaim(new Claim("organisationId", organisationId.ToString() ?? string.Empty));
            }
        }

        private static JwtSecurityToken? ParseAccessToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;
            return jsonToken;
        }

        private static void CheckTokens(string? accessToken, string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new AuthenticationFailureException(Resource.MsgAccessTokenNotReceived);
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new AuthenticationFailureException(Resource.MsgRrefreshTokenNotReceived);
            }
        }
    }
}
