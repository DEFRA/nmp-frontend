namespace NMP.Portal.Helpers
{
    public static class APIURLHelper
    {
        public const string GetToken = "Token";

        public const string AddressLookupAPI = "vendors/address-lookup/addresses?postcode={0}&offset={1}";
        public const string AddFarmAPI = "farm";
        public const string IsFarmExist = "farm/exists?Name={0}&PostCode={1}";
    }
}
