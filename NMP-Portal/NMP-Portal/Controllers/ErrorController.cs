using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using System.Diagnostics;
using System.Net;
using NMP.Commons.ViewModels;

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
            string statusKey = ((HttpStatusCode)statusCode).ToString();
            switch (statusCode)
            {
                case 400:
                    errorViewModel.Code = 400;
                    errorViewModel.Message = "Bad Request. The server could not process your request.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 401:
                    errorViewModel.Code = 401;
                    errorViewModel.Message = "Unauthorized. Please log in before accessing this page.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 403:
                    errorViewModel.Code = 403;
                    errorViewModel.Message = "Forbidden. You don’t have permission to view this resource.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 404:
                    errorViewModel.Code = 404;
                    errorViewModel.Message = "Page not found.";
                    errorViewModel.StatusCode = statusKey;

                    string originalPath = "unknown";
                    if (HttpContext.Items.ContainsKey("originalPath"))
                    {
                        originalPath = HttpContext.Items["originalPath"] as string;
                    }
                    ViewBag.Path = originalPath;
                    viewName = "PageNotFound";
                    break;

                case 408:
                    errorViewModel.Code = 408;
                    errorViewModel.Message = "Request Timeout. The server timed out waiting for your request.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 409:
                    errorViewModel.Code = 409;
                    errorViewModel.Message = "Conflict. The request conflicts with the current state of the server. May be session data are missing.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 500:
                    errorViewModel.Code = 500;
                    errorViewModel.Message = "Internal Server Error. Something went wrong on the server.";
                    errorViewModel.StatusCode = statusKey;

                    if (exceptionHandlerPathFeature != null)
                    {
                        ViewBag.ExceptionMessage = exceptionHandlerPathFeature.Error.Message;
                    }
                    break;

                case 502:
                    errorViewModel.Code = 502;
                    errorViewModel.Message = "Bad Gateway. The server received an invalid response.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 503:
                    errorViewModel.Code = 503;
                    errorViewModel.Message = "Service Unavailable. OIDC or backend service is unavailable.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                case 504:
                    errorViewModel.Code = 504;
                    errorViewModel.Message = "Gateway Timeout. The upstream service did not respond in time.";
                    errorViewModel.StatusCode = statusKey;
                    break;

                default:
                    // Catch-all for unknown status codes
                    errorViewModel.Code = statusCode;
                    errorViewModel.Message = "Unexpected error occurred.";
                    errorViewModel.StatusCode = statusKey;
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
