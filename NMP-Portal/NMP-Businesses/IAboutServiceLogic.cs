using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses
{
    public interface IAboutServiceLogic
    {
        bool UpdateShowAboutServiceAsync(bool doNotShowAboutThisService);
        bool HasDoNotShowAboutThisService();
    }
}
