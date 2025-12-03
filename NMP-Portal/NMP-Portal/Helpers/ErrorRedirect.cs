using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace NMP.Portal.Helpers
{
    public static class ErrorRedirect
    {
        public static RedirectToActionResult Redirect(int statusCode)
        {
            return new RedirectToActionResult(
                RouteConstants.HttpStatusCodeHandlerAction,
                RouteConstants.ErrorController,
                new { statusCode }
            );
        }

    }
}
