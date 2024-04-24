﻿using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFieldService : IService
    {
        Task<int> FetchFieldCountByFarmIdAsync(int farmId);
        Task<List<SoilTypesResponse>> FetchSoilTypes();
        Task<List<FieldResponseWapper>> FetchNutrientsAsync();
    }
}
