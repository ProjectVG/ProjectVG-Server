using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectVG.Application.Services.Auth;
using System.Security.Claims;

namespace ProjectVG.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthenticationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            
            var token = ExtractTokenFromHeader(context.HttpContext.Request);
            if (string.IsNullOrEmpty(token))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Authorization header is missing or invalid"
                });
                return;
            }

            var isValid = await authService.ValidateAccessTokenAsync(token);
            if (!isValid)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Invalid or expired access token"
                });
                return;
            }

            var userId = await authService.GetCurrentUserIdAsync(token);
            if (!userId.HasValue)
            {
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
        }

        private string? ExtractTokenFromHeader(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            return authHeader.Substring("Bearer ".Length);
        }
    }
}
