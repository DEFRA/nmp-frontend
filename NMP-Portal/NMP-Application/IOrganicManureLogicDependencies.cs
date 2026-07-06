using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IOrganicManureLogicDependencies
    {
        IOrganicManureLogic OrganicManureLogic { get; }
        IFarmLogic FarmLogic { get; }

        ICropLogic CropLogic { get; }

        IFieldLogic FieldLogic { get; }

        IMannerLogic MannerLogic { get; }

        IFertiliserManureLogic FertiliserManureLogic { get; }

        IWarningLogic WarningLogic { get; }
    }
}
