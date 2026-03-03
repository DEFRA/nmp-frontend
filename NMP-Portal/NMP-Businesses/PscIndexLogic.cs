using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses
{
    [Business(ServiceLifetime.Transient)]
    public class PscIndexLogic(ILogger<PscIndexLogic> logger, IPscIndexService pscService, ISnsAnalysisService snsAnalysisService) : IPscIndexLogic
    {
        private readonly ILogger<PscIndexLogic> _logger = logger;
        private readonly IPscIndexService _pscService = pscService;
        public async Task<List<CommonResponse>> FetchPscIndex()
        {
            _logger.LogTrace("Fetch psc index");
            return await _pscService.FetchPscIndex();
        }
    }
}
