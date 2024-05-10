using NMP.Portal.Models;

namespace NMP.Portal.Helpers
{
    public static class APIURLHelper
    {
        public const string GetToken = "Token";
        public const string AddressLookupAPI = "vendors/address-lookup/addresses?postcode={0}&offset={1}";
        public const string AddFarmAPI = "farms";
        public const string IsFarmExist = "farms/exists?Name={0}&Postcode={1}";
        public const string FetchFarmByUserIdAPI = "farms/users/{0}";
        public const string FetchFarmByIdAPI = "farms/{0}";
        public const string FetchFieldCountByFarmIdAPI = "fields/farms/{0}/count";
        public const string FetchRainfallAverageAsyncAPI = "vendors/rb209/RainFall/RainfallAverage/{0}";
        public const string FetchSoilTypesAsyncAPI = "vendors/rb209/Soil/SoilTypes";
        public const string FetchNutrientsAsyncAPI = "vendors/rb209/Field/Nutrients";
        public const string FetchSoilNutrientIndexAsyncAPI = "vendors/rb209/Soil/NutrientIndex/{0}/{1}/{2}";
        public const string FetchCropGroupsAsyncAPI = "vendors/rb209/Arable/CropGroups";
        public const string FetchCropTypesAsyncAPI = "vendors/rb209/Arable/CropTypes/{0}";
        public const string FetchSoilTypeByIdAsyncAPI = "vendors/rb209/Soil/SoilType/{0}";
        public const string FetchCropGroupByIdAsyncAPI = "vendors/rb209/Arable/CropGroup/{0}";
        public const string FetchCropTypeByIdAsyncAPI = "vendors/rb209/Arable/CropType/{0}";
        public const string AddFieldAsyncAPI = "fields/farms/{0}";
        public const string IsFieldExistAsyncAPI = "fields/farms/{0}/exists?Name={1}";
        public const string FetchFieldsByFarmIdAsyncAPI = "fields/farms/{0}";
        public const string FetchPotatoVarietiesAsyncAPI = "vendors/rb209/Arable/PotatoVarieties";
        public const string FetchFieldByFieldIdAsyncAPI = "fields/{0}";
        public const string FetchCropInfoOneByCropTypeIdAsyncAPI = "vendors/rb209/Arable/CropInfo1s/{0}";
        public const string FetchCropInfoTwoByCropTypeIdAsyncAPI = "vendors/rb209/Arable/CropInfo2s";
        public const string AddCropNutrientManagementPlanAsyncAPI = "crops/fields/{0}";
    }
}
