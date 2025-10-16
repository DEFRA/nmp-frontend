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
        //private readonly ILogger<ErrorController> _logger;

        //public ErrorController(ILogger<ErrorController> logger)
        //{
        //    _logger = logger;
        //}

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            string viewName = "Error";
            //var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            switch (statusCode)
            {
                case 404:
                    string originalPath = "unknown";
                    if (HttpContext.Items.ContainsKey("originalPath"))
                    {
                        originalPath = HttpContext.Items["originalPath"] as string;
                    }

                    ViewBag.Path = originalPath;
                    viewName = "PageNotFound";

                    break;
                case 500:
                    //var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                    //_telemetryClient.TrackException(exception: exceptionHandlerPathFeature?.Error);
                    //_telemetryClient.TrackEvent("Error.ServerError", new Dictionary<string, string>
                    //{
                    //    ["originalPath"] = exceptionHandlerPathFeature.Path,
                    //    ["error"] = exceptionHandlerPathFeature.Error.Message
                    //});
                    // ViewBag.Path = exceptionHandlerPathFeature.Path;
                    viewName = "Error";
                    break;
            }
            //ViewBag.Path = exceptionHandlerPathFeature.Path;

            return View(viewName, new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
