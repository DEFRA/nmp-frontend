﻿using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IFarmService:IService
    {
        Task<Farm> AddFarmAsync(FarmData farmData);
    }
}
