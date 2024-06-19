using Microsoft.Identity.Client;
using System.Security.Claims;

namespace NMP.Portal.Security
{
    public class TokenAcquisitionService
    {
        private readonly IConfidentialClientApplication _confidentialClientApp;
        private readonly IConfiguration _configuration;

        public TokenAcquisitionService(IConfiguration configuration)
        {
            _configuration = configuration;
            var authority = $"{_configuration["CustomerIdentityInstance"]}/{_configuration["CustomerIdentityDomain"]}/{_configuration["CustomerIdentityPolicyId"]}/V2.0/";

            confidentialClientApplication = confidentialClientApplication
                .Create(_configuration["CustomerIdentityClientId"])
                .WithClientSecret(_configuration["CustomerIdentityClientSecret"])
                .WithB2CAuthority(authority)
                .Build();
            TokenCacheHelper.EnableSerialization(_confidentialClientApp.AppTokenCache);
        }

        public async Task<string> AcquireTokenSilentAsync(string userId, IEnumerable<string> scopes)
        {
            try
            {
                var account = await _confidentialClientApp.GetAccountAsync(userId);
                //var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == userId);
                if (account == null)
                {
                    // Handle account not found scenario
                    throw new MsalUiRequiredException("account_not_found", "User account was not found in the token cache.");
                }

                var result = await _confidentialClientApp.AcquireTokenSilent(scopes, account)
                                                         .ExecuteAsync();

                return result.AccessToken;
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
