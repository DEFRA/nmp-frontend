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
    public class CropServiceDependencies(ICropService cropService, ISnsAnalysisService snsAnalysisService, IRecommendationService recommendationService, IRb209Service rb209Service) : ICropServiceDependencies
    {
        public ICropService CropService { get; } = cropService;
        public ISnsAnalysisService SnsAnalysisService { get; } = snsAnalysisService;
        public IRecommendationService RecommendationService { get; } = recommendationService;
        public IRb209Service Rb209Service { get; } = rb209Service;
    }
}

