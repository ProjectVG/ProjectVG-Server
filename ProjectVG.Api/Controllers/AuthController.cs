using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Services.Auth;

namespace ProjectVG.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthTokenService authTokenService,
        ILogger<AuthController> logger)
    {
        _authTokenService = authTokenService;
        _logger = logger;
    }

    /// <summary>
    /// 토큰 갱신
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authTokenService.RefreshTokenAsync(request.RefreshToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Token refresh failed: {Error}", result.ErrorMessage);
                return BadRequest(new { error = "invalid_grant", error_description = result.ErrorMessage });
            }

            return Ok(new
            {
                access_token = result.AccessToken,
                refresh_token = result.RefreshToken,
                token_type = result.TokenType,
                expires_in = result.ExpiresIn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }

    /// <summary>
    /// 토큰 폐기
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authTokenService.RevokeTokenAsync(request.Token);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Token revocation failed: {Error}", result.ErrorMessage);
                // OAuth2 스펙에 따라 revoke는 항상 200 OK 반환
                return Ok();
            }

            _logger.LogInformation("Token revoked successfully");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            // OAuth2 스펙에 따라 revoke는 항상 200 OK 반환
            return Ok();
        }
    }

    /// <summary>
    /// 현재 사용자의 모든 토큰 폐기 (로그아웃)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new { error = "invalid_token", error_description = "Invalid user token" });
            }

            await _authTokenService.RevokeAllUserTokensAsync(userId);
            
            _logger.LogInformation("User {UserId} logged out successfully", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }
}