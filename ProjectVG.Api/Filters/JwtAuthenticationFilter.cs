using Microsoft.AspNetCore.Mvc.Filters;
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
