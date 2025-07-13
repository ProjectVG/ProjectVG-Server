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
                    characters = "/api/character",
                    chat = "/api/chat",
                    health = "/health"
                }
            });
        }

        [HttpGet("/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
} 