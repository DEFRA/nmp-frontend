using NMP.Portal.Models;

namespace NMP.Portal.Helpers
{
    public static class APIURLHelper
    {
        public const string GetToken = "Token";
        public const string AddressLookupAPI = "vendors/address-lookup/addresses?postcode={0}&offset={1}";
        public const string AddFarmAPI = "farms/createFarm";
        public const string IsFarmExist = "farms/exists?Name={0}&Postcode={1}&Id={2}"; 
        public const string FetchFarmByUserIdAPI = "farms/users/{0}";
        public const string FetchFarmByOrgIdAPI = "farms/organisations/{0}";
        public const string FetchFarmByIdAPI = "farms/{0}";
        public const string FetchFieldCountByFarmIdAPI = "fields/farms/{0}/count";
        public const string FetchRainfallAverageAsyncAPI = "climates/rainfall-average/{0}";
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
        public const string FetchPlanSummaryByFarmIdAsyncAPI = "crops/plans?farmId={0}&type={1}";
        public const string FetchHarvestYearPlansByFarmIdAsyncAPI = "crops/plans/{0}?farmId={1}";
        public const string FetchCropsOrganicinorganicdetailsByYearFarmIdAsyncAPI = "crops/organic-inorganic-details/{0}?farmId={1}";
        public const string AddCropNutrientManagementPlanAsyncAPI = "crops/plans";
        public const string FetchRecommendationByFieldIdAndYearAsyncAPI = "recommendations?fieldId={0}&harvestYear={1}";
        public const string FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAsyncAPI = "vendors/rb209/Arable/CropInfo1/{0}/{1}";
        public const string FetchCropInfo2NameByCropInfo2IdAsyncAPI = "vendors/rb209/Arable/CropInfo2/{0}";
        public const string FetchAllCropTypeAsyncAPI = "vendors/rb209/Arable/CropTypes";
        public const string AddOrUpdateUserAsyncAPI = "users";
        public const string FetchSoilTypeBySoilTypeIdAsyncAPI = "vendors/rb209/Soil/SoilType/{0}";
        public const string FetchSoilAnalysisByFieldIdAsyncAPI = "soil-analyses/fields/{0}?shortSummary={1}";
        public const string FetchCropTypeByFarmIdAndHarvestYearAsyncAPI = "crops/plans/crop-types/{0}?farmId={1}";
        public const string FetchFieldByFarmIdAndHarvestYearAsyncAPI = "crops/plans/fields/{0}?farmId={1}";
        public const string FetchFieldByFarmIdAndHarvestYearAndCropTypeIdAsyncAPI = "crops/plans/fields/{0}?cropTypeId={1}&farmId={2}";
        public const string FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeIdAsyncAPI = "crops/plans/management-periods/{0}?cropTypeId={1}&fieldIds={2}&cropOrder={3}";
        public const string FetchManagementIdsByFieldIdAndHarvestYearAsyncAPI = "crops/plans/management-periods/{0}?fieldIds={1}";
        public const string FetchManureGroupListAsyncAPI = "manure-groups";
        public const string FetchManureTypeListByGroupIdAsyncAPI = "manure-types/manure-groups/{0}?countryId={1}";
        public const string FetchManureGroupByIdAsyncAPI = "manure-groups/{0}";
        public const string FetchManureTypeByManureTypeIdAsyncAPI = "manure-types/{0}";

        public const string FetchApplicationMethodsByApplicableForAsyncAPI = "application-method?fieldType={0}&applicableFor={1}";
        public const string FetchIncorporationMethodsByApplicationIdAsyncAPI = "incorporation-methods/application-methods/{0}?fieldType={1}&applicableFor={2}";


        public const string FetchCropsByFieldIdAsyncAPI = "crops/fields/{0}";

        public const string FetchIncorporationDelaysByMethodIdAndApplicableForAsyncAPI = "incorporation-delays/incorporation-methods/{0}?applicableFor={1}";
        public const string FetchFieldDetailByFieldIdAndHarvestYearAsyncAPI = "fields/info/{0}?year={1}&confirm={2}";
        public const string FetchIncorporationDelayByIdAsyncAPI = "incorporation-delays/{0}";
        public const string FetchIncorporationMethodByIdAsyncAPI = "incorporation-methods/{0}";
        public const string FetchApplicationMethodByIdAsyncAPI = "application-methods/{0}";


        public const string AddOrganicManuresAsyncAPI = "organic-manures";
        public const string FetchRainfallByPostcodeAndDateRange = "climates/total-rainfall?postcode={0}&startDate={1}&endDate={2}";
        
        public const string FetchWindspeedDataDefault = "windspeeds/default";
        public const string FetchMoisterTypeDefaultByApplicationDate = "moisture-types/default/{0}";
        public const string FetchRainTypeDefault = "rain-types/default";
        public const string FetchRainTypesAsyncAPI = "rain-types";
        public const string FetchWindspeedsAsyncAPI = "windspeeds";
        public const string FetchMoisterTypesAsyncAPI = "moisture-types";
        public const string FetchCropTypeYieldByCropTypeIdAsyncAPI = "manner-crop-types/cropTypeYield/{0}";

        public const string UpdateFarmAsyncAPI = "farms/updateFarm";
        public const string FetchInOrganicManureDurationsAsyncAPI = "inorganic-manure-durations";
        public const string FetchInOrganicManureDurationsByIdAsyncAPI = "inorganic-manure-durations/{0}";

        public const string DeleteFarmByIdAPI = "farms/{0}";
        public const string AddFertiliserManuresAsyncAPI = "fertiliser-manures";
        public const string FetchIsPerennialByCropTypeIdAsyncAPI = "manner-crop-types/isPerennial/{0}";
        public const string FetchTotalNBasedOnManIdAndAppDateAsyncAPI = "organic-manures/total-nitrogen/{0}?fromDate={1}&toDate={2}&confirm={3}";        
        public const string FetchCropTypeByFieldIdAndHarvestYearAsyncAPI = "crops/crop-type/{0}?year={1}&confirm={2}";
        public const string FetchCropTypeLinkingByCropTypeIdAsyncAPI = "crop-type-linkings/{0}";
        public const string FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManureAsyncAPI = "organic-manures/manure-type/{0}?year={1}&confirm={2}";
        public const string FetchTotalNBasedOnManIdFromOrgManureAndFertiliserAsyncAPI = "fertiliser-manures/organic-manures/total-nitrogen/{0}?confirm={1}";
        public const string FetchTotalNFromFertiliserBasedOnManIdAndAppDateAsyncAPI = "fertiliser-manures/total-nitrogen/{0}?fromDate={1}&toDate={2}&confirm={3}";        
        public const string FetchCropTypeLinkingsByCropTypeIdAsyncAPI = "crop-type-linkings/{0}";
        public const string FetchOrganicManureExistanceByDateRangeAsyncAPI = "organic-manures/check-existence?dateFrom={0}&dateTo={1}&confirm={2}";
        public const string FetchSeasonsAsyncAPI = "vendors/rb209/Measurement/Seasons";
        public const string FetchSNSIndexByMeasurementMethodAsyncAPI = "vendors/rb209/Measurement/MeasurementMethod";
        public const string FetchSecondCropListByFirstCropIdAsyncAPI = "second-crop-linkings/{0}";
        public const string FetchSoilAnalysisByIdAsyncAPI = "soil-analyses/{0}";
        public const string UpdateSoilAnalysisAsyncAPI = "soil-analyses/{0}";
        public const string UpdateFieldAsyncAPI = "fields/{0}";
        public const string DeleteFieldByIdAPI = "fields/{0}";
        public const string AddPKBalanceAsyncAPI = "pk-Balance";
        public const string UpdatePKBalanceAsyncAPI = "pk-Balance/{0}&Field={1}";
        public const string FetchPKBalanceByYearAndFieldIdAsyncAPI = "pk-balance/{0}?fieldId={1}";

        //Manner APi Url
        public const string FetchMannerApplicationMethodByIdAsyncAPI = "vendors/manner/application-methods/{0}";
        public const string FetchMannerIncorporationDelaysByMethodIdAndApplicableForAsyncAPI = "vendors/manner/incorporation-delays/by-incorp-method-and-applicable-for/{0}?applicableFor={1}";//    vendors/manner/incorporation-delays/by-incorp-method/{0}";
        public const string FetchMannerIncorporationDelaysByIdAsyncAPI = "vendors/manner/incorporation-delays/{0}";
        public const string FetchMannerIncorporationMethodByIdAsyncAPI = "vendors/manner/incorporation-methods/{0}";
        public const string FetchMannerIncorporationMethodsByApplicationIdAsyncAPI = "vendors/manner/incorporation-methods/by-app-method-and-applicable-for/{0}?applicableFor={1}"; //"vendors/manner/incorporation-methods/by-app-method/{0}";
        public const string FetchMannerApplicationMethodsByApplicableForAsyncAPI = "vendors/manner/application-methods?isLiquid={0}&fieldType={1}";
        public const string FetchMannerWindspeedsAsyncAPI = "vendors/manner/windspeeds";
        public const string FetchMannerWindspeedByIdAsyncAPI = "vendors/manner/windspeeds/{0}";
        public const string FetchMannerRainTypesAsyncAPI = "vendors/manner/rain-types";
        public const string FetchMannerRainTypeByIdAsyncAPI = "vendors/manner/rain-types/{0}";
        public const string FetchMannerMoistureTypesAsyncAPI = "vendors/manner/moisture-types";
        public const string FetchMannerMoistureTypeByIdAsyncAPI = "vendors/manner/moisture-types/{0}";
        public const string FetchMannerManureGroupListAsyncAPI = "vendors/manner/manure-groups";
        public const string FetchMannerManureGroupByIdAsyncAPI = "vendors/manner/manure-groups/{0}";
        public const string FetchMannerAutumnCropNitrogenUptakeAsyncAPI = "vendors/manner/autumn-crop-nitrogen-uptake";
        public const string FetchMannerRainfallByPostcodeAndDateRangeAsyncAPI = "vendors/manner/rainfall-post-application";
        public const string FetchMannerManureTypeListByGroupIdAndCountryAsyncAPI = "vendors/manner/manure-types?manureGroupId={0}&countryId={1}";
        public const string FetchMannerManureTypeByManureTypeIdAsyncAPI = "vendors/manner/manure-types/{0}";
        public const string FetchMannerRainfallAverageAsyncAPI = "vendors/manner/climates/avarage-annual-rainfall/{0}";        
    }
}
