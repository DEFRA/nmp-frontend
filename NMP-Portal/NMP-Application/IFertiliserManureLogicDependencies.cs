using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Application
{
    public interface IFertiliserManureLogicDependencies
    {
        IFarmLogic FarmLogic { get; } 
        IFertiliserManureLogic FertiliserManureLogic { get; }
        ICropLogic CropLogic { get; }
        IFieldLogic FieldLogic { get; }
        IOrganicManureLogic OrganicManureLogic { get; }
        IWarningLogic WarningLogic { get; }
    }
}
