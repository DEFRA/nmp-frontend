using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Controllers
{
    public class RegisterController : Controller
    {
        public RegisterController()
        {

        }
        public IActionResult Index()
        {

            FarmsViewModel model = new FarmsViewModel();
            if (model.Farms.Count > 0)
            {

                ViewBag.IsUserHaveAnyFarms = true;

                return View("~/Views/Farm/Index.cshtml", model);
            }
            else
            {
                ViewBag.IsUserHaveAnyFarms = false;
                return View("~/Views/Farm/Name.cshtml");
            }

        }
    }
}
