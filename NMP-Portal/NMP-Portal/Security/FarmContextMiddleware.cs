using Microsoft.AspNetCore.DataProtection;
using NMP.Application;
using NMP.Commons.Models;

namespace NMP.Portal.Security
{
    public class FarmContextMiddleware(RequestDelegate next, IDataProtectionProvider dataProtectionProvider)
    {
        private readonly RequestDelegate _next = next;
        //private readonly IFarmContextLogic _farmLogic = farmLogic;
        private readonly IDataProtector _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");

        public async Task Invoke(HttpContext context, FarmContext farmContext, IFarmContextLogic farmLogic)
        {
            var encryptedfarmId = context.GetRouteValue("farmId")?.ToString()?? context.GetRouteValue("id")?.ToString();

            if (!string.IsNullOrEmpty(encryptedfarmId))
            {
                string farmId = _dataProtector.Unprotect(encryptedfarmId);
                var farm = await farmLogic.FetchFarmByIdAsync(Convert.ToInt32(farmId));
                if (farm != null)
                {
                    farmContext.EncryptedFarmId = encryptedfarmId;
                    farmContext.FarmId = farm.ID;
                    farmContext.FarmName = farm.Name ?? "Unknown";
                    context.Session.SetString("current_farm_name", farm.Name ?? "Unknown");
                    context.Session.SetString("current_farm_id", encryptedfarmId);
                }
            }
            else
            {
                farmContext.FarmId = 0;
                farmContext.EncryptedFarmId = context.Session.GetString("current_farm_id") ?? string.Empty;
                farmContext.FarmName = context.Session.GetString("current_farm_name")?? "Unknown";
            }

            await _next(context);
        }
    }
}
