namespace NMP.Portal.Helpers
{
    public static class APIURLHelper
    {
        public const string GetToken = "Token";
        public const string AddressLookupAPI = "vendors/address-lookup/addresses?postcode={0}&offset={1}";
        public const string AddFarmAPI = "farm";
        public const string IsFarmExist = "farm/exists?Name={0}&Postcode={1}";
        public const string FetchFarmByUserIdAPI = "farm/user-id/{0}";
        public const string FetchFarmByIdAPI = "farm/{0}";
        public const string FetchFieldCountByFarmIdAPI = "field/farm/{0}/count";
        public const string FetchRainfallAverageAsyncAPI = "vendors/rb209/RainFall/RainfallAverage/{0}";
    }
}
