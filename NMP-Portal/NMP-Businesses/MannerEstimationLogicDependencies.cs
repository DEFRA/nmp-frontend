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
    public class MannerEstimationLogicDependencies(
    IOrganicManureLogic organicManureLogic,
    IFarmLogic farmLogic,
    IMannerLogic mannerLogic,
    IFieldLogic fieldLogic,
    ICropLogic cropLogic,
    IWarningLogic warningLogic) : IMannerEstimationLogicDependencies
    {
        public IOrganicManureLogic OrganicManureLogic { get; } = organicManureLogic;
        public IFarmLogic FarmLogic { get; } = farmLogic;

        public IMannerLogic MannerLogic { get; } = mannerLogic;

        public IFieldLogic FieldLogic { get; } = fieldLogic;

        public ICropLogic CropLogic { get; } = cropLogic;

        public IWarningLogic WarningLogic { get; } = warningLogic;
    }
}
