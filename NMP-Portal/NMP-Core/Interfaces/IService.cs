using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IService
    {
        Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null);
        Task<HttpResponseMessage> GetDataAsync(string url);
    }
}
