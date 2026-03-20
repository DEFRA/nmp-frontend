using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IAboutMannerLogic
    {
        Task<bool> UpdateShowAboutMannerAsync(bool doNotShowAboutManner);
        Task<bool> CheckDoNotShowAboutManner();
    }
}
