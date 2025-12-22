using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IWarningLogic
    {
        Task<List<WarningHeaderResponse>> FetchWarningHeaderByFieldIdAndYearAsync(string fieldIds, int harvestYear);
        Task<WarningResponse> FetchWarningByCountryIdAndWarningKeyAsync(int countryId, string warningKey);

    }
}
