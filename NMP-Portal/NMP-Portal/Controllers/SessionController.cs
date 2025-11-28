using Microsoft.AspNetCore.Mvc;

namespace NMP.Portal.Controllers
{
    [Route("Session/[action]")]
    public class SessionController : Controller
    {
        [HttpGet]
        public IActionResult KeepAlive()
        {
            HttpContext.Session.SetString("LastKeepAlive", DateTime.Now.ToString());
            HttpContext.Session.SetString("LastActive", DateTime.UtcNow.ToString("O"));
            return Ok();
        }

        //[HttpPost]
        ////[ValidateAntiForgeryToken]
        //public IActionResult KeepAlive(bool ping)
        //{
        //    HttpContext.Session.SetString("LastActive", DateTime.UtcNow.ToString("O"));
        //    return Ok(new { Refreshed = true });
        //}
    }
}
