using Microsoft.AspNetCore.Mvc;

namespace ProjectVG.Api.Controllers
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
                message = "ProjectVG API is running."
            });
        }
    }
} 