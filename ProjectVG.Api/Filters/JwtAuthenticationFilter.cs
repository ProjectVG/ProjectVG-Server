using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Auth;
using System.Security.Claims;

namespace ProjectVG.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthenticationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthenticationAttribute>>();
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
            
            // 디버그: 모든 헤더 로깅
            logger.LogInformation("=== JWT 인증 디버그 시작 ===");
            logger.LogInformation("요청 경로: {Path}", context.HttpContext.Request.Path);
            logger.LogInformation("요청 메서드: {Method}", context.HttpContext.Request.Method);
            logger.LogInformation("원격 IP: {RemoteIP}", context.HttpContext.Connection.RemoteIpAddress);
            
            // 모든 헤더 로깅
            foreach (var header in context.HttpContext.Request.Headers)
            {
                logger.LogInformation("헤더: {Key} = {Value}", header.Key, header.Value);
            }
            
            var token = ExtractTokenFromHeader(context.HttpContext.Request, logger);
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("인증 헤더가 없거나 유효하지 않음");
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Authorization header is missing or invalid",
                    debug = new
                    {
                        headers = context.HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                        hasAuthorization = context.HttpContext.Request.Headers.ContainsKey("Authorization"),
                        authorizationValue = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()
                    }
                });
                return;
            }

            logger.LogInformation("토큰 추출 성공. 토큰 길이: {TokenLength}", token.Length);
            logger.LogInformation("토큰 미리보기: {TokenPreview}", token.Length > 20 ? token.Substring(0, 20) + "..." : token);

            var isValid = await tokenService.ValidateAccessTokenAsync(token);
            logger.LogInformation("토큰 검증 결과: {IsValid}", isValid);
            
            if (!isValid)
            {
                logger.LogWarning("토큰 검증 실패");
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Invalid or expired access token",
                    debug = new
                    {
                        tokenLength = token.Length,
                        tokenPreview = token.Length > 20 ? token.Substring(0, 20) + "..." : token
                    }
                });
                return;
            }

            var userId = await tokenService.GetUserIdFromTokenAsync(token);
            logger.LogInformation("추출된 사용자 ID: {UserId}", userId);
            
            if (!userId.HasValue)
            {
                logger.LogWarning("토큰에서 사용자 ID를 추출할 수 없음");
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Unable to extract user information from token"
                });
                return;
            }

            // ClaimsPrincipal 생성하여 HttpContext에 설정
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim("user_id", userId.Value.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            
            context.HttpContext.User = principal;
            logger.LogInformation("JWT 인증 성공 - 사용자: {UserId}", userId.Value);
        }

        private string? ExtractTokenFromHeader(HttpRequest request, ILogger logger)
        {
            // Nginx 관련 헤더들도 확인
            var possibleHeaders = new[]
            {
                "Authorization",
                "X-Forwarded-Authorization",
                "X-Original-Authorization",
                "HTTP_AUTHORIZATION"
            };

            foreach (var headerName in possibleHeaders)
            {
                var headerValue = request.Headers[headerName].FirstOrDefault();
                logger.LogInformation("헤더 확인 중 '{HeaderName}': {HeaderValue}", headerName, headerValue);
                
                if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer "))
                {
                    var token = headerValue.Substring("Bearer ".Length);
                    logger.LogInformation("헤더 '{HeaderName}'에서 토큰 발견: {TokenLength} 문자", headerName, token.Length);
                    return token;
                }
            }

            // Authorization 헤더가 없거나 Bearer로 시작하지 않는 경우
            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            logger.LogWarning("유효한 Authorization 헤더를 찾을 수 없음. 원본 Authorization: {AuthHeader}", authHeader);
            return null;
        }
    }
}
