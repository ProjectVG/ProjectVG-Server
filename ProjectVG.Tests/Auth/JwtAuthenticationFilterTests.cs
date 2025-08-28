using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Api.Filters;
using ProjectVG.Application.Services.Auth;
using System.Security.Claims;
using Xunit;

namespace ProjectVG.Tests.Auth
{
    public class JwtAuthenticationFilterTests
    {
        private readonly JwtAuthenticationAttribute _filter;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<JwtAuthenticationAttribute>> _mockLogger;

        public JwtAuthenticationFilterTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<JwtAuthenticationAttribute>>();
            _filter = new JwtAuthenticationAttribute();
        }

        [Fact]
        public async Task OnAuthorizationAsync_ValidBearerToken_ShouldSetUserPrincipal()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.bearer.token";
            var context = CreateAuthorizationFilterContext($"Bearer {token}");

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().BeNull();
            context.HttpContext.User.Should().NotBeNull();
            context.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
            
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(userId.ToString());

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            _mockAuthService.Verify(x => x.GetCurrentUserIdAsync(token), Times.Once);
        }

        [Fact]
        public async Task OnAuthorizationAsync_NoAuthorizationHeader_ShouldReturnUnauthorized()
        {
            // Arrange
            var context = CreateAuthorizationFilterContext(null);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<UnauthorizedObjectResult>();

            var unauthorizedResult = context.Result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().NotBeNull();
            
            // 디버그 정보 확인
            var responseJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult.Value);
            var response = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);
            response.GetProperty("success").GetBoolean().Should().BeFalse();
            response.GetProperty("message").GetString().Should().Be("Authorization header is missing or invalid");
        }

        [Fact]
        public async Task OnAuthorizationAsync_InvalidAuthorizationFormat_ShouldReturnUnauthorized()
        {
            // Arrange
            var context = CreateAuthorizationFilterContext("InvalidFormat");

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<UnauthorizedObjectResult>();

            var unauthorizedResult = context.Result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().NotBeNull();
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult.Value);
            var response = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);
            response.GetProperty("success").GetBoolean().Should().BeFalse();
            response.GetProperty("message").GetString().Should().Be("Authorization header is missing or invalid");
        }

        [Fact]
        public async Task OnAuthorizationAsync_InvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var token = "invalid.token";
            var context = CreateAuthorizationFilterContext($"Bearer {token}");

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(false);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<UnauthorizedObjectResult>();

            var unauthorizedResult = context.Result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().NotBeNull();
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult.Value);
            var response = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);
            response.GetProperty("success").GetBoolean().Should().BeFalse();
            response.GetProperty("message").GetString().Should().Be("Invalid or expired access token");

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task OnAuthorizationAsync_ValidTokenButNoUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            var token = "valid.token";
            var context = CreateAuthorizationFilterContext($"Bearer {token}");

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync((Guid?)null);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().NotBeNull();
            context.Result.Should().BeOfType<UnauthorizedObjectResult>();

            var unauthorizedResult = context.Result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().NotBeNull();
            
            var responseJson = System.Text.Json.JsonSerializer.Serialize(unauthorizedResult.Value);
            var response = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);
            response.GetProperty("success").GetBoolean().Should().BeFalse();
            response.GetProperty("message").GetString().Should().Be("Unable to extract user information from token");

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            _mockAuthService.Verify(x => x.GetCurrentUserIdAsync(token), Times.Once);
        }

        [Fact]
        public async Task OnAuthorizationAsync_XForwardedAuthorizationHeader_ShouldExtractToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.bearer.token";
            var context = CreateAuthorizationFilterContext(null);
            
            // X-Forwarded-Authorization 헤더 추가
            context.HttpContext.Request.Headers["X-Forwarded-Authorization"] = $"Bearer {token}";

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().BeNull();
            context.HttpContext.User.Should().NotBeNull();
            context.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);
        }

        [Fact]
        public async Task OnAuthorizationAsync_XOriginalAuthorizationHeader_ShouldExtractToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.bearer.token";
            var context = CreateAuthorizationFilterContext(null);
            
            // X-Original-Authorization 헤더 추가
            context.HttpContext.Request.Headers["X-Original-Authorization"] = $"Bearer {token}";

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().BeNull();
            context.HttpContext.User.Should().NotBeNull();
            context.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);
        }

        [Fact]
        public async Task OnAuthorizationAsync_HTTPAuthorizationHeader_ShouldExtractToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.bearer.token";
            var context = CreateAuthorizationFilterContext(null);
            
            // HTTP_AUTHORIZATION 헤더 추가
            context.HttpContext.Request.Headers["HTTP_AUTHORIZATION"] = $"Bearer {token}";

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.Result.Should().BeNull();
            context.HttpContext.User.Should().NotBeNull();
            context.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();

            _mockAuthService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);
        }

        [Fact]
        public async Task OnAuthorizationAsync_UserPrincipal_ShouldContainRequiredClaims()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.bearer.token";
            var context = CreateAuthorizationFilterContext($"Bearer {token}");

            _mockAuthService.Setup(x => x.ValidateAccessTokenAsync(token)).ReturnsAsync(true);
            _mockAuthService.Setup(x => x.GetCurrentUserIdAsync(token)).ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(context);

            // Assert
            context.HttpContext.User.Should().NotBeNull();
            
            var claims = context.HttpContext.User!.Claims.ToList();
            claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
            claims.Should().Contain(c => c.Type == "user_id" && c.Value == userId.ToString());
        }

        private AuthorizationFilterContext CreateAuthorizationFilterContext(string? authorizationHeader)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers["Authorization"] = authorizationHeader;
            }

            // 서비스 컨테이너 설정
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IAuthService))).Returns(_mockAuthService.Object);
            serviceProvider.Setup(x => x.GetService(typeof(ILogger<JwtAuthenticationAttribute>))).Returns(_mockLogger.Object);
            
            httpContext.RequestServices = serviceProvider.Object;

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}
