using Microsoft.AspNetCore.Mvc;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class HomeController : ControllerBase
    {
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                messageType = "json",
                supportedFormats = new[] { "json", "binary" },
                audioFormat = "wav",
                version = "1.0.0"
            });
        }
    }
} 