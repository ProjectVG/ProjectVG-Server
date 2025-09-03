using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Api.Filters;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Infrastructure.Auth;
using System.Security.Claims;
using Xunit;

namespace ProjectVG.Tests.Api.Filters
{
    public class JwtAuthenticationFilterTests
    {
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<JwtAuthenticationAttribute>> _mockLogger;
        private readonly JwtAuthenticationAttribute _filter;
        private readonly ServiceProvider _serviceProvider;

        public JwtAuthenticationFilterTests()
        {
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<JwtAuthenticationAttribute>>();
            
            var services = new ServiceCollection();
            services.AddSingleton(_mockTokenService.Object);
            services.AddSingleton(_mockLogger.Object);
            _serviceProvider = services.BuildServiceProvider();

            _filter = new JwtAuthenticationAttribute();
        }

        private AuthorizationFilterContext CreateFilterContext(string? headerName = null, string? headerValue = null)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(headerValue))
            {
                httpContext.Request.Headers[headerName] = headerValue;
            }

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        #region Token Extraction Tests

        [Theory]
        [InlineData("Authorization", "Bearer valid-token-123")]
        [InlineData("X-Forwarded-Authorization", "Bearer forwarded-token-456")]
        [InlineData("X-Original-Authorization", "Bearer original-token-789")]
        [InlineData("HTTP_AUTHORIZATION", "Bearer http-token-abc")]
        public async Task OnAuthorizationAsync_ValidTokenInDifferentHeaders_ShouldAuthenticate(string headerName, string headerValue)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedToken = headerValue.Substring("Bearer ".Length);
            var filterContext = CreateFilterContext(headerName, headerValue);
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(expectedToken))
                .ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(expectedToken))
                .ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(filterContext);

            // Assert
            filterContext.HttpContext.User.Should().NotBeNull();
            filterContext.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
            filterContext.HttpContext.User.Identity.AuthenticationType.Should().Be("Bearer");
            
            var userIdClaim = filterContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(userId.ToString());

            var customUserIdClaim = filterContext.HttpContext.User.FindFirst("user_id");
            customUserIdClaim.Should().NotBeNull();
            customUserIdClaim!.Value.Should().Be(userId.ToString());
        }

        [Fact]
        public async Task OnAuthorizationAsync_TokenWithExtraSpaces_ShouldTrimAndUse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "token-with-spaces";
            var filterContext = CreateFilterContext("Authorization", $"Bearer   {token}   ");
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(token))
                .ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(filterContext);

            // Assert
            _mockTokenService.Verify(x => x.ValidateAccessTokenAsync(token), Times.Once);
            filterContext.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
        }

        #endregion

        #region Token Missing Tests

        [Fact]
        public async Task OnAuthorizationAsync_NoAuthorizationHeader_ShouldThrowTokenMissingException()
        {
            // Arrange
            var filterContext = CreateFilterContext();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_MISSING);
        }

        [Fact]
        public async Task OnAuthorizationAsync_EmptyAuthorizationHeader_ShouldThrowTokenMissingException()
        {
            // Arrange
            var filterContext = CreateFilterContext("Authorization", "");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_MISSING);
        }

        [Fact]
        public async Task OnAuthorizationAsync_BearerWithoutToken_ShouldThrowTokenMissingException()
        {
            // Arrange
            var filterContext = CreateFilterContext("Authorization", "Bearer");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_MISSING);
        }

        [Fact]
        public async Task OnAuthorizationAsync_BearerWithEmptyToken_ShouldThrowTokenMissingException()
        {
            // Arrange
            var filterContext = CreateFilterContext("Authorization", "Bearer ");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_MISSING);
        }

        #endregion

        #region Token Validation Tests

        [Fact]
        public async Task OnAuthorizationAsync_InvalidToken_ShouldThrowTokenInvalidException()
        {
            // Arrange
            var invalidToken = "invalid-token";
            var filterContext = CreateFilterContext("Authorization", $"Bearer {invalidToken}");
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(invalidToken))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_INVALID);
        }

        #endregion

        #region User ID Extraction Tests

        [Fact]
        public async Task OnAuthorizationAsync_ValidTokenButNoUserId_ShouldThrowAuthenticationFailedException()
        {
            // Arrange
            var validToken = "valid-token-no-user";
            var filterContext = CreateFilterContext("Authorization", $"Bearer {validToken}");
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(validToken))
                .ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(validToken))
                .ReturnsAsync((Guid?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AuthenticationException>(
                () => _filter.OnAuthorizationAsync(filterContext)
            );

            exception.ErrorCode.Should().Be(ErrorCode.AUTHENTICATION_FAILED);
        }

        #endregion

        #region Successful Authentication Tests

        [Fact]
        public async Task OnAuthorizationAsync_ValidTokenAndUserId_ShouldSetUserWithCorrectClaims()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var validToken = "valid-token-123";
            var filterContext = CreateFilterContext("Authorization", $"Bearer {validToken}");
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(validToken))
                .ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(validToken))
                .ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(filterContext);

            // Assert
            var user = filterContext.HttpContext.User;
            user.Should().NotBeNull();
            user.Identity!.IsAuthenticated.Should().BeTrue();
            user.Identity.AuthenticationType.Should().Be("Bearer");

            var nameIdentifierClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            nameIdentifierClaim.Should().NotBeNull();
            nameIdentifierClaim!.Value.Should().Be(userId.ToString());

            var userIdClaim = user.FindFirst("user_id");
            userIdClaim.Should().NotBeNull();
            userIdClaim!.Value.Should().Be(userId.ToString());

            user.Claims.Should().HaveCount(2);
        }

        [Fact]
        public async Task OnAuthorizationAsync_CompleteValidFlow_ShouldNotSetResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var validToken = "complete-valid-token";
            var filterContext = CreateFilterContext("Authorization", $"Bearer {validToken}");
            
            _mockTokenService.Setup(x => x.ValidateAccessTokenAsync(validToken))
                .ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(validToken))
                .ReturnsAsync(userId);

            // Act
            await _filter.OnAuthorizationAsync(filterContext);

            // Assert
            filterContext.Result.Should().BeNull(); // No result means continue with request
            filterContext.HttpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
        }

        #endregion
    }
}