using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IMannerEstimationLogicDependencies
    {
        IOrganicManureLogic OrganicManureLogic { get; }
        IFarmLogic FarmLogic { get; }
        IMannerLogic MannerLogic { get; }
        IFieldLogic FieldLogic { get; }
        ICropLogic CropLogic { get; }
        IWarningLogic WarningLogic { get; }
    }
}
