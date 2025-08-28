using Microsoft.AspNetCore.Mvc.Filters;
using ProjectVG.Infrastructure.Auth;
using System.Security.Claims;

namespace ProjectVG.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthenticationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>
        /// 요청에서 Bearer JWT를 추출·검증하고 성공 시 HttpContext.User에 ClaimsPrincipal을 설정하여 인증을 적용합니다.
        /// </summary>
        /// <param name="context">현재 요청의 컨텍스트(요청 헤더에서 토큰을 읽고, HttpContext.User를 설정하는 데 사용).</param>
        /// <returns>비동기 작업을 나타내는 Task.</returns>
        /// <exception cref="AuthenticationException">다음 조건 중 하나일 때 발생:
        /// <list type="bullet">
        /// <item><description>ErrorCode.TOKEN_MISSING: 토큰이 헤더에 없거나 비어 있을 때.</description></item>
        /// <item><description>ErrorCode.TOKEN_INVALID: 토큰이 유효하지 않을 때.</description></item>
        /// <item><description>ErrorCode.AUTHENTICATION_FAILED: 토큰에서 사용자 ID를 얻을 수 없을 때.</description></item>
        /// </list>
        /// </exception>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthenticationAttribute>>();
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();

            var token = ExtractToken(context.HttpContext.Request);
            if (string.IsNullOrEmpty(token)) {
                throw new AuthenticationException(ErrorCode.TOKEN_MISSING);
            }

            if (!await tokenService.ValidateAccessTokenAsync(token)) {
                throw new AuthenticationException(ErrorCode.TOKEN_INVALID);
            }

            var userId = await tokenService.GetUserIdFromTokenAsync(token);
            if (!userId.HasValue) {
                throw new AuthenticationException(ErrorCode.AUTHENTICATION_FAILED);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim("user_id", userId.Value.ToString())
            };

            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
            logger.LogInformation("JWT 인증 성공 - 사용자: {UserId}", userId.Value);
        }

        /// <summary>
        /// 요청 헤더들에서 Bearer 토큰을 찾아 토큰 문자열을 반환합니다.
        /// </summary>
        /// <param name="request">토큰을 추출할 HTTP 요청.</param>
        /// <returns>헤더에서 찾은 토큰 문자열(접두사 "Bearer " 제거) 또는 찾지 못하면 null.</returns>
        private string? ExtractToken(HttpRequest request)
        {
            var possibleHeaders = new[]
            {
                "Authorization",
                "X-Forwarded-Authorization",
                "X-Original-Authorization",
                "HTTP_AUTHORIZATION"
            };

            foreach (var headerName in possibleHeaders) {
                var headerValue = request.Headers[headerName].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer ")) {
                    return headerValue.Substring("Bearer ".Length).Trim();
                }
            }

            return null;
        }
    }
}
