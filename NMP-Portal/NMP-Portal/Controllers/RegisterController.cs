using Microsoft.AspNetCore.Mvc;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    public class RegisterController : Controller
    {
        public IActionResult Index()
        {
            FarmsViewModel model = new FarmsViewModel();
            //need to fetch user farms
            if (model.Farms.Count > 0)
            {
                ViewBag.IsUserHaveAnyFarms = true;
                return RedirectToAction("FarmList", "Farm",model);
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = false;
                return RedirectToAction("Name", "Farm");
            }

        }
    }
}
