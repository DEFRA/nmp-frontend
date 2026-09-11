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
    public class FieldLogicDependencies(IFarmLogic farmLogic, ISoilLogic soilLogic, IFieldLogic fieldLogic, IPreviousCroppingLogic previousCroppingLogic, IFarmsNvzLogic farmsNvzLogic, ICropLogic cropLogic): IFieldLogicDependencies
    {
        public IFarmLogic FarmLogic { get; } = farmLogic;
        public ISoilLogic SoilLogic { get; } = soilLogic;
        public IFieldLogic FieldLogic { get; } = fieldLogic;
        public IPreviousCroppingLogic PreviousCroppingLogic { get; } = previousCroppingLogic;
        public IFarmsNvzLogic FarmsNvzLogic { get; } = farmsNvzLogic;
        public ICropLogic CropLogic { get; } = cropLogic;
    }
}
