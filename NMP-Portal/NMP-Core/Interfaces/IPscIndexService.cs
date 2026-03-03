using NMP.Commons.ServiceResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IPscIndexService
    {
        Task<List<CommonResponse>> FetchPscIndex();
    }
}
