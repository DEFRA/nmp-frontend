using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IReportLogicDependencies
    {
        IFarmLogic FarmLogic { get; }

        IFieldLogic FieldLogic { get; }

        ICropLogic CropLogic { get; }

        IOrganicManureLogic OrganicManureLogic { get; }

        IMannerLogic MannerLogic { get; }

        IScotlandNMaxValueLogic ScotlandNMaxValueLogic { get; }

        IWarningLogic WarningLogic { get; }
    }
}
