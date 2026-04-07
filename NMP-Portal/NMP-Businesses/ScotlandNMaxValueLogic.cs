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
    public class ScotlandNMaxValueLogic(ILogger<ScotlandNMaxValueLogic> logger, IScotlandNMaxValueService scotlandNMaxValueService) : IScotlandNMaxValueLogic
    {
        private readonly ILogger<ScotlandNMaxValueLogic> _logger = logger;
        private readonly IScotlandNMaxValueService _scotlandNMaxValueService = scotlandNMaxValueService;
        public async Task<(List<ScotlandNMaxValue>?,Error?)> FetchAllScotlandNMaxValue()
        {
            _logger.LogTrace("Fetch all ScotlandNMaxValue");
            return await _scotlandNMaxValueService.FetchAllScotlandNMaxValue();
        }
    }
}
