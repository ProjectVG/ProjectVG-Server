using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// 컨트롤러에 인증 서비스 의존성을 주입하여 초기화합니다.
        /// </summary>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 요청 헤더의 리프레시 토큰으로 액세스/리프레시 토큰을 갱신하고 사용자 정보를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 요청 헤더 "X-Refresh-Token"에서 리프레시 토큰을 읽어 IAuthService.RefreshTokenAsync를 호출합니다.
        /// 응답은 { success = true, tokens = ..., user = ... } 형태의 200 OK 입니다.
        /// </remarks>
        /// <returns>갱신된 토큰과 사용자 정보를 포함한 200 OK IActionResult.</returns>
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

        /// <summary>
        /// 요청 헤더의 "X-Refresh-Token"에서 리프레시 토큰을 읽어 해당 토큰의 로그아웃(무효화)을 수행하고 결과를 반환합니다.
        /// </summary>
        /// <returns>
        /// HTTP 200 응답을 반환합니다. 본문은 익명 객체로 { success: bool, message: string } 형태이며,
        /// success는 로그아웃 성공 여부, message는 "Logout successful" 또는 "Logout failed"입니다.
        /// </returns>
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

        /// <summary>
        /// 게스트 식별자(guestId)를 사용해 게스트 OAuth 로그인으로 사용자와 토큰을 발급하여 반환합니다.
        /// </summary>
        /// <param name="guestId">클라이언트에서 전달된 게스트 고유 식별자(빈 값이면 유효하지 않음).</param>
        /// <returns>성공 시 HTTP 200 응답으로 { success = true, tokens, user } 형태의 페이로드를 반환합니다.</returns>
        /// <exception cref="ValidationException">guestId가 null 또는 빈 문자열인 경우 ErrorCode.GUEST_ID_INVALID와 함께 던져집니다.</exception>
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

        /// <summary>
        /// HTTP 요청 헤더 "X-Refresh-Token"에서 리프레시 토큰을 읽어 반환합니다.
        /// </summary>
        /// <returns>헤더에 지정된 첫 번째 토큰 값 또는 헤더가 없을 경우 빈 문자열.</returns>
        private string GetRefreshTokenFromHeader()
        {
            return Request.Headers["X-Refresh-Token"].FirstOrDefault() ?? string.Empty;
        }
    }
} 