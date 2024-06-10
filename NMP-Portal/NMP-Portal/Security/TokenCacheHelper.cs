namespace NMP.Portal.Security
{
    using Microsoft.Identity.Client.Extensions.Msal;
    using Microsoft.Identity.Client;

    public static class TokenCacheHelper
    {
        public static void EnableSerialization(ITokenCache tokenCache)
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                "msal.cache",
                MsalCacheHelper.UserRootDirectory)
                .WithUnprotectedFile()
                .Build();

            var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();
            cacheHelper.RegisterCache(tokenCache);
        }
    }
}
