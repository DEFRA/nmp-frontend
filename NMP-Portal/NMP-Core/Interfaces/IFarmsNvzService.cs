using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMP.Commons.Models;

namespace NMP.Core.Interfaces
{
    public interface IFarmsNvzService
    {
        Task<(List<FarmsNvz>, Error?)> FetchFarmNVZByID(int farmId);
    }
}
