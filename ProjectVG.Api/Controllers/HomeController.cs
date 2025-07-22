using Microsoft.AspNetCore.Mvc;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            return Ok(new
            {
                message = "ProjectVG API Server",
                version = "1.0.0",
                endpoints = new
                {
                    swagger = "/swagger",
                    characters = "/api/v1/character",
                    chat = "/api/v1/chat",
                    health = "/health"
                }
            });
        }

        [HttpGet("/health")]
        [HttpGet("/api/health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "ok",
                serverTime = DateTime.UtcNow,
                message = "ProjectVG API가 정상적으로 실행 중입니다."
            });
        }
    }
} 