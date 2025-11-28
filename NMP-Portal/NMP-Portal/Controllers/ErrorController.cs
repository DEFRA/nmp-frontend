using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;
using System.Diagnostics;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]

    public class ErrorController : Controller
    {        
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            ErrorViewModel errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            string viewName = "Error";
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            switch (statusCode)
            {
                case 503:
                    errorViewModel.Code = 503;
                    errorViewModel.Message = "OIDC service unavailable.";
                    viewName = "Error";
                    break;
                case 404:
                    errorViewModel.Code= 404;
                    string? originalPath = "unknown";
                    if (HttpContext.Items.ContainsKey("originalPath"))
                    {
                        originalPath = HttpContext.Items["originalPath"] as string;
                    }
                    errorViewModel.Message = "Page not found";
                    ViewBag.Path = originalPath;
                    viewName = "PageNotFound";
                    break;
                case 500:                    
                    viewName = "Error";
                    errorViewModel.Message = "Server Error!";
                    errorViewModel.Code = 500;
                    break;
            }
           
            return View(viewName, errorViewModel);
        }

        public IActionResult Index()
        {
            var error = new ErrorViewModel();
            if (HttpContext.Session.Keys.Contains("Error"))
            {
                error = HttpContext.Session.GetObjectFromJson<ErrorViewModel>("Error");
            }

            Console.WriteLine("Error: " + error?.Message ?? "Unknown error");
            return View("Error", error);
        }
    }
}
