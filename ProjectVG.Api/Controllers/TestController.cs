using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProjectVG.Api.Filters;

namespace ProjectVG.Api.Controllers
{
    [JwtAuthentication]
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult Me()
        {
            // JWT 검증 후 User ID 가져오기
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userIdClaim = User.FindFirst("user_id")?.Value;

            return Ok(new
            {
                success = true,
                userId = userId,
                userIdClaim = userIdClaim,
                message = "JWT authentication successful"
            });
        }

        [HttpGet("protected")]
        public IActionResult Protected()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return Ok(new
            {
                success = true,
                message = "This is a protected endpoint",
                userId = userId,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("user-info")]
        public IActionResult UserInfo()
        {
            var claims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            }).ToList();

            return Ok(new
            {
                success = true,
                claims = claims,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                authenticationType = User.Identity?.AuthenticationType
            });
        }
    }
}
