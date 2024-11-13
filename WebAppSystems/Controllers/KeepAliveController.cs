using Microsoft.AspNetCore.Mvc;

namespace WebAppSystems.Controllers
{
    public class KeepAliveController : Controller
    {

        [Route("KeepAlive")]
        [HttpGet]
        public IActionResult KeepAlive()
        {
            return Ok();
        }


    }
}
