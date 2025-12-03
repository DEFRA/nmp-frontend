using Microsoft.AspNetCore.DataProtection;
using NMP.Portal.Models;
using NMP.Portal.Services;

namespace NMP.Portal.Security
{
    public class FarmContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFarmService _farmService;
        private readonly IDataProtector _dataProtector;
        public FarmContextMiddleware(RequestDelegate next, IFarmService farmService, IDataProtectionProvider dataProtectionProvider)
        {
            _next = next;
            _farmService = farmService;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
        }

        public async Task Invoke(HttpContext context, FarmContext farmContext)
        {
            var encryptedfarmId = context.GetRouteValue("id")?.ToString();

            if (!string.IsNullOrEmpty(encryptedfarmId))
            {
                string farmId= _dataProtector.Unprotect(encryptedfarmId);                
                (var farm, var error) = await _farmService.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                farmContext.EncryptedFarmId = encryptedfarmId;
                farmContext.FarmId = farm.ID;
                farmContext.FarmName = farm.Name;
            }

            await _next(context);
        }
    }
}
