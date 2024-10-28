﻿using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface ISoilAnalysisService
    {
        Task<(SoilAnalysis, Error)> FetchSoilAnalysisById(int id);
        Task<(SoilAnalysis, Error)> UpdateSoilAnalysisAsync(int id, string soilData);
    }
}