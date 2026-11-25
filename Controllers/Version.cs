using Microsoft.AspNetCore.Mvc;

namespace QuickBalance.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                version = "4.000"
            });
        }
    }
}