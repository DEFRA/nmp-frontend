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
            return Ok();
        }
    } 
}
