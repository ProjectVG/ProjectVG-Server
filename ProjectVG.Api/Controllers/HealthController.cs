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
                message = "ProjectVG API가 정상적으로 실행 중입니다."
            });
        }
    }
} 