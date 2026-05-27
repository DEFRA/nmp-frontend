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
    public class ReportLogicDependencies(
    IFarmLogic farmLogic,
    IFieldLogic fieldLogic,
    ICropLogic cropLogic,
    IOrganicManureLogic organicManureLogic,
    IMannerLogic mannerLogic,
    IScotlandNMaxValueLogic scotlandNMaxValueLogic,
    IWarningLogic warningLogic)
    : IReportLogicDependencies
    {
        public IFarmLogic FarmLogic { get; } = farmLogic;

        public IFieldLogic FieldLogic { get; } = fieldLogic;

        public ICropLogic CropLogic { get; } = cropLogic;

        public IOrganicManureLogic OrganicManureLogic { get; }
            = organicManureLogic;

        public IMannerLogic MannerLogic { get; } = mannerLogic;

        public IScotlandNMaxValueLogic ScotlandNMaxValueLogic { get; }
            = scotlandNMaxValueLogic;

        public IWarningLogic WarningLogic { get; } = warningLogic;
    }
}
