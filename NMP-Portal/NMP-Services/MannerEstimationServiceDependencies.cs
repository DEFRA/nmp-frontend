using Microsoft.Extensions.DependencyInjection;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Services
{
    [Service(ServiceLifetime.Scoped)]
    public class MannerEstimationServiceDependencies(
    IMannerEstimationService mannerEstimationService, IMannerService mannerService, IFieldService fieldService, IFarmService farmService, ICropService cropService) : IMannerEstimationServiceDependencies
    {
        public IMannerEstimationService MannerEstimationService { get; } = mannerEstimationService;
        public IMannerService MannerService { get; } = mannerService;

        public IFieldService FieldService { get; } = fieldService;

        public IFarmService FarmService { get; } = farmService;

        public ICropService CropService { get; } = cropService;

    }
    
}
