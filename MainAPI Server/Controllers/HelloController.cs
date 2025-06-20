using Microsoft.AspNetCore.Mvc;

namespace MainAPI_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new {
                status = "ok",
                serverTime = DateTime.UtcNow,
                message = "MainAPIServer API is running."
            });
        }
    }
}
