using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IFieldLogicDependencies
    {
        IFarmLogic FarmLogic { get; }
        ISoilLogic SoilLogic { get; }
        IFieldLogic FieldLogic { get; }
        IPreviousCroppingLogic PreviousCroppingLogic { get; }
        IFarmsNvzLogic FarmsNvzLogic { get; }
        ICropLogic CropLogic { get; }
    }
}
