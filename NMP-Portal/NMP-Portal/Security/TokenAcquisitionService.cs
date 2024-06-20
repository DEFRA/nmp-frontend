using Microsoft.Identity.Client;
using System.Net.Http;
using System.Security.Claims;

namespace NMP.Portal.Security
{
    public class TokenAcquisitionService
    {
        private readonly IConfidentialClientApplication _confidentialClientApp;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfidentialClientApplication _confidentialClientApplication;
        public TokenAcquisitionService(IConfiguration configuration, IHttpClientFactory httpClientFactory, IConfidentialClientApplication confidentialClientApplication)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            var authority = $"{_configuration["CustomerIdentityInstance"]}/{_configuration["CustomerIdentityDomain"]}/{_configuration["CustomerIdentityPolicyId"]}/V2.0/";

            _confidentialClientApplication = confidentialClientApplication;
            TokenCacheHelper.EnableSerialization(_confidentialClientApplication.AppTokenCache);
        }

        public async Task<string> AcquireTokenByRefreshTokenAsync(string refreshToken)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("");
                //var result = await _confidentialClientApplication.AcquireTokenByRefreshToken(new[] { "https://<your-tenant-name>.onmicrosoft.com/<your-api-id>/access_as_user" }, refreshToken)
                //.ExecuteAsync();

                //return result.AccessToken;
                return null;
            }
            catch (MsalUiRequiredException)
            {
                // This exception means you need to prompt the user for re-authentication
                throw;
            }
            catch (MsalServiceException ex)
            {
                // Handle exception (e.g., logging, rethrowing, etc.)
                throw;
            }
        }

        public bool IsTokenValid(ClaimsPrincipal user, out DateTime expiration)
        {
            var expClaim = user.FindFirst("access_token_expiry");
            if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
            {
                expiration = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                return expiration > DateTime.UtcNow.AddMinutes(5); // Check if token is valid for at least another 5 minutes
            }

            expiration = DateTime.MinValue;
            return false;
        }
    }
}
