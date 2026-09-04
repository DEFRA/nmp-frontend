using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IRb209Service 
    {
        Task<List<SoilTypesResponse>> FetchSoilTypesAsync();
        Task<(List<NutrientResponseWrapper>, Error)> FetchNutrientsAsync();
        Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, decimal? nutrientValue, int methodologyId, int countryId);
        Task<List<CropGroupResponse>> FetchCropGroupsAsync();
        Task<List<CropTypeResponse>> FetchCropTypesAsync(int cropGroupId);
        Task<string> FetchSoilTypeById(int soilTypeId);
        Task<string> FetchCropGroupByIdAsync(int cropGroupId);
        Task<string> FetchCropTypeByIdAsync(int cropTypeId);
        Task<List<PotatoVarietyResponse>> FetchPotatoVarietiesAsync();
        Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeIdAsync(int cropTypeId);
        Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeIdAsync();
        Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1IdAsync(int cropTypeId, int cropInfo1Id);
        Task<string> FetchCropInfo2NameByCropInfo2IdAsync(int cropInfo2Id);
        Task<List<CropTypeResponse>> FetchAllCropTypesAsync();
        Task<string> FetchSoilTypeByIdAsync(int soilTypeId);
        Task<List<SeasonResponse>> FetchSeasonsAsync();
        Task<(SnsResponse, Error)> FetchSNSIndexByMeasurementMethodAsync(MeasurementData measurementData);
        Task<(SnsResponseForScotland, Error)> FetchSNSIndexByMeasurementMethodForScotlandAsync(MeasurementDataForScotland measurementDataForScotland);
        Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId);
        Task<(List<SoilMethologiesResponse>?, Error?)> FetchSoilMethodologies(int nutrientId, int countryId);
        Task<(SoilMethologiesResponse?, Error?)> FetchSoilMethodologyNameByNutrientIdAndMethodologyId(int nutrientId, int methodologyId);
        Task<List<GrassSeasonResponse>> FetchGrassSeasonsAsync();
        Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCutAsync(int swardTypeId, int swardManagementId, int numberOfCut, bool isNewSward, int countryId);
        Task<(List<PotentialCutResponse>, Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementIdAsync(int swardTypeId, int swardManagementId);
        Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementsAsync();
        Task<(List<SwardTypeResponse>, Error)> FetchSwardTypesServiceByCountryAsync(int countryId);
        Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdAsync(int sequenceId, int grassGrowthClassId);
        Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesByIdAsync(int defoliationId);
        Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeIdAsync(int swardTypeId);
        Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeIdAsync(int swardTypeId);
        Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementIdAsync(int swardManagementId);
        Task<List<NvzActionProgramResponse>> FetchNvzActionProgramsByCountryIdAsync(int countryId);

    }
}
