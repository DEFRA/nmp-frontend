namespace NMP.Portal.Security
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;

    public class RedisTokenProvider
    {
        private readonly TokenCredential _credential;
        private readonly string[] _scopes = new[] { "https://redis.azure.com/.default" };

        private AccessToken _currentToken;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RedisTokenProvider()
        {
            _credential = new DefaultAzureCredential();
        }

        public async Task<string> GetTokenAsync()
        {
            // If token is expiring within 5 minutes, refresh it.
            if (DateTimeOffset.UtcNow >= _currentToken.ExpiresOn - TimeSpan.FromMinutes(5))
            {
                await RefreshTokenAsync();
            }

            return _currentToken.Token;
        }

        private async Task RefreshTokenAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Double-check to avoid duplicate refresh
                if (DateTimeOffset.UtcNow < _currentToken.ExpiresOn - TimeSpan.FromMinutes(5))
                    return;

                _currentToken = await _credential.GetTokenAsync(
                    new TokenRequestContext(_scopes),
                    CancellationToken.None
                );
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
