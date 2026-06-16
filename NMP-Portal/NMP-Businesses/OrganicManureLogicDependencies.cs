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
    public class OrganicManureLogicDependencies(
    IOrganicManureLogic organicManureLogic,
    IFarmLogic farmLogic,
    ICropLogic cropLogic,
    IFieldLogic fieldLogic,
    IMannerLogic mannerLogic,
    IFertiliserManureLogic fertiliserManureLogic,
    IWarningLogic warningLogic) : IOrganicManureLogicDependencies
    {
        public IOrganicManureLogic OrganicManureLogic { get; } = organicManureLogic;
        public IFarmLogic FarmLogic { get; } = farmLogic;

        public ICropLogic CropLogic { get; } = cropLogic;

        public IFieldLogic FieldLogic { get; } = fieldLogic;

        public IMannerLogic MannerLogic { get; } = mannerLogic;

        public IFertiliserManureLogic FertiliserManureLogic { get; } = fertiliserManureLogic;

        public IWarningLogic WarningLogic { get; } = warningLogic;
    }
}
