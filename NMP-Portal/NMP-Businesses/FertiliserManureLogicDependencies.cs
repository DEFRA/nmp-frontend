using Microsoft.Extensions.DependencyInjection;
using NMP.Application;
using NMP.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses
{
    [Business(ServiceLifetime.Transient)]
    public class FertiliserManureLogicDependencies(
    IFarmLogic farmLogic,
    IFertiliserManureLogic fertiliserManureLogic,
    ICropLogic cropLogic,
    IFieldLogic fieldLogic,
    IOrganicManureLogic organicManureLogic,
    IWarningLogic warningLogic): IFertiliserManureLogicDependencies
    {
        public IFarmLogic FarmLogic { get; } = farmLogic;
        public IFertiliserManureLogic FertiliserManureLogic { get; } = fertiliserManureLogic;
        public ICropLogic CropLogic { get; } = cropLogic;
        public IFieldLogic FieldLogic { get; } = fieldLogic;
        public IOrganicManureLogic OrganicManureLogic { get; } = organicManureLogic;
        public IWarningLogic WarningLogic { get; } = warningLogic;
    }
}
