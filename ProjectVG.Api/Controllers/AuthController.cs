using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Models.User;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("test-login")]
        public async Task<IActionResult> TestLogin([FromBody] TestLoginRequest request)
        {
            try
            {
                var result = await _authService.LoginWithOAuthAsync("test", request.UserId.ToString());
                
                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        tokens = result.Tokens,
                        user = result.User
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                
                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        tokens = result.Tokens,
                        user = result.User
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                var success = await _authService.LogoutAsync(request.RefreshToken);
                
                return Ok(new
                {
                    success = success,
                    message = success ? "Logout successful" : "Logout failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }

    public class TestLoginRequest
    {
        public Guid UserId { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
} 