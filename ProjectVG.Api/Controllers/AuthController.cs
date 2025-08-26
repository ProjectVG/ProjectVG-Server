using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = GetRefreshTokenFromHeader();
            var result = await _authService.RefreshTokenAsync(refreshToken);
            
            return Ok(new
            {
                success = true,
                tokens = result.Tokens,
                user = result.User
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = GetRefreshTokenFromHeader();
            var success = await _authService.LogoutAsync(refreshToken);
            
            return Ok(new
            {
                success = success,
                message = success ? "Logout successful" : "Logout failed"
            });
        }

        [HttpPost("guest-login")]
        public async Task<IActionResult> GuestLogin([FromBody] string guestId)
        {
            if (string.IsNullOrEmpty(guestId))
            {
                throw new ValidationException(ErrorCode.GUEST_ID_INVALID);
            }

            var result = await _authService.LoginWithOAuthAsync("guest", guestId);
            
            return Ok(new
            {
                success = true,
                tokens = result.Tokens,
                user = result.User
            });
        }

        private string GetRefreshTokenFromHeader()
        {
            return Request.Headers["X-Refresh-Token"].FirstOrDefault() ?? string.Empty;
        }
    }
} 