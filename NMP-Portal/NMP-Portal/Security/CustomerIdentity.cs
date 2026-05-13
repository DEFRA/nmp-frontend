using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
            string nmpCookieScheme = "NMP-Portal";
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = nmpCookieScheme;
                options.DefaultSignInScheme = nmpCookieScheme;
                options.DefaultAuthenticateScheme = nmpCookieScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(nmpCookieScheme, cookieOptions =>
            {
                cookieOptions.Cookie.Name = "NMP-Portal.Auth";
                // How long your app's cookie is valid
                cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(60); // e.g. 8 hours
                cookieOptions.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = nmpCookieScheme;
                options.MetadataAddress = configuration?["CustomerIdentityMetaDataUrl"];
                options.ClientId = configuration?["CustomerIdentityClientId"];
                options.ClientSecret = configuration?["CustomerIdentityClientSecret"];                
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
                options.Scope.Add(configuration?["CustomerIdentityClientId"] ?? string.Empty);                
                options.ResponseType = "code";
                options.CallbackPath = "/signin-oidc";                
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
            services.AddDistributedTokenCaches();
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
                
        private static async Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
        {
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
                context.ProtocolMessage.RedirectUri = configuration["CustomerIdentityReturnURI"]?.ToString();                
            }
            context.ProtocolMessage.Parameters["serviceId"] = configuration?["CustomerIdentityServiceId"]?.ToString() ?? string.Empty;
            context.ProtocolMessage.Parameters["forceReselection"] = "true";
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

                if (identity != null && !string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
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
                var response = await httpClient.PostAsync("users", new StringContent(jsonData, Encoding.UTF8, "application/json"));
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
            List<string> relationShipsArray = new List<string>();
            List<string> rolesArray = new List<string>();
            List<Organisation> organisations = new List<Organisation>();
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
                    case "relationships":
                        ParseRelationshipsData(claim, ref relationShipsArray);                       
                        break;
                    case "roles":
                        ParseRolesData(claim, ref rolesArray);
                        break;
                    default:
                        break;
                }
            }

            ParseOrganisations(identity, userData, currentRelationShipId, ref relationShipsArray, ref organisations);
            
            ParseRole(identity, currentRelationShipId, ref rolesArray);
        }

        private static void ParseRelationshipsData(Claim claim, ref List<string> relationShipsArray)
        {            
            if (claim.Value.GetType().IsArray)
            {
                relationShipsArray.AddRange(claim.Value.Split(","));
            }
            else
            {
                relationShipsArray.Add(claim.Value);
            }
        }

        private static void ParseRolesData( Claim claim, ref List<string> rolesArray)
        {  
            if (claim.Value.GetType().IsArray)
            {
                rolesArray.AddRange(claim.Value.Split(","));
            }
            else
            {
                rolesArray.Add(claim.Value);
            }            
        }

        private static void ParseRole(ClaimsIdentity? identity, string currentRelationShipId, ref List<string> rolesArray)
        {
            List<string> roleDetails = new List<string>();
            var rd = rolesArray.FirstOrDefault(r => r.Contains(currentRelationShipId));
            if (rd != null)
            {
                roleDetails.AddRange(rd.Split(":"));
                identity?.AddClaim(new Claim("roleName", roleDetails[1]));
                identity?.AddClaim(new Claim("roleStatus", roleDetails[2]));
            }
        }

        private static void ParseOrganisations(ClaimsIdentity? identity, UserData userData, string currentRelationShipId,  ref List<string> relationShipsArray, ref List<Organisation> organisations)
        {
            Guid organisationId;
            string organisationName;
            List<string> relationShipDetails = new List<string>();
            foreach (var item in relationShipsArray)
            {
                var relationshipArray = item.Split(":");
                if (relationshipArray[4] == "Citizen")
                {
                    organisations.Add(new Organisation { ID = Guid.Parse(relationshipArray[0]), Name = $"{userData.User.GivenName} {userData.User.Surname}" });
                }
                else
                {
                    organisations.Add(new Organisation { ID = Guid.Parse(relationshipArray[1]), Name = relationshipArray[2] });
                }
            }

            string serializedOrganisations = JsonConvert.SerializeObject(organisations);
            identity?.AddClaim(new Claim("organisations", serializedOrganisations));            
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
