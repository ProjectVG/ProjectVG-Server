using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Models;
using ProjectVG.Application.Models.User;
using ProjectVG.Domain.Entities.Users;
using Xunit;

namespace ProjectVG.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserService.Object,
                _mockTokenService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task LoginWithOAuthAsync_ValidTestProvider_ShouldReturnSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "test";
            var accessToken = userId.ToString();

            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, accessToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be(provider);
            result.User.ProviderId.Should().Be(accessToken);
            result.User.Status.Should().Be(AccountStatus.Active);

            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_InvalidProvider_ShouldReturnFailureResult()
        {
            // Arrange
            var provider = "invalid";
            var accessToken = Guid.NewGuid().ToString();

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, accessToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Unsupported OAuth provider: invalid");
        }

        [Fact]
        public async Task LoginWithOAuthAsync_InvalidUserIdFormat_ShouldReturnFailureResult()
        {
            // Arrange
            var provider = "test";
            var accessToken = "invalid-guid-format";

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, accessToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid test user ID format");
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidRefreshToken_ShouldReturnSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";

            var tokenResponse = new TokenResponse
            {
                AccessToken = "new.access.token.here",
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            var user = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Provider = "test",
                ProviderId = userId.ToString(),
                Status = AccountStatus.Active
            };

            _mockTokenService.Setup(x => x.RefreshAccessTokenAsync(refreshToken)).ReturnsAsync(tokenResponse);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(refreshToken)).ReturnsAsync(userId);
            _mockUserService.Setup(x => x.TryGetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().Be(user);

            _mockTokenService.Verify(x => x.RefreshAccessTokenAsync(refreshToken), Times.Once);
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(refreshToken), Times.Once);
            _mockUserService.Verify(x => x.TryGetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_NewGuestUser_ShouldCreateAndReturnSuccessResult()
        {
            // Arrange
            var provider = "guest";
            var guestId = "guest123";
            var userId = Guid.NewGuid();
            var createdUser = new UserDto
            {
                Id = userId,
                Username = $"guest_{guestId}",
                Email = $"guest_{guestId}@guest.local",
                Provider = provider,
                ProviderId = guestId,
                Status = AccountStatus.Active
            };
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockUserService.Setup(x => x.TryGetByProviderAsync("guest", guestId)).ReturnsAsync((UserDto?)null);
            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>())).ReturnsAsync(createdUser);
            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be(provider);
            result.User.ProviderId.Should().Be(guestId);
            result.User.Status.Should().Be(AccountStatus.Active);

            _mockUserService.Verify(x => x.TryGetByProviderAsync("guest", guestId), Times.Once);
            _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>()), Times.Once);
            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_ExistingGuestUser_ShouldReturnSuccessResult()
        {
            // Arrange
            var provider = "guest";
            var guestId = "guest123";
            var userId = Guid.NewGuid();
            var existingUser = new UserDto
            {
                Id = userId,
                Username = $"guest_{guestId}",
                Email = $"guest_{guestId}@guest.local",
                Provider = provider,
                ProviderId = guestId,
                Status = AccountStatus.Active
            };
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockUserService.Setup(x => x.TryGetByProviderAsync("guest", guestId)).ReturnsAsync(existingUser);
            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().Be(existingUser);

            _mockUserService.Verify(x => x.TryGetByProviderAsync("guest", guestId), Times.Once);
            _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>()), Times.Never);
            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidRefreshToken_ShouldReturnFailureResult()
        {
            // Arrange
            var refreshToken = "invalid.refresh.token";
            _mockTokenService.Setup(x => x.RefreshAccessTokenAsync(refreshToken)).ReturnsAsync((TokenResponse?)null);

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid or expired refresh token");

            _mockTokenService.Verify(x => x.RefreshAccessTokenAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ValidRefreshToken_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";

            _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken)).ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(refreshToken)).ReturnsAsync(userId);

            // Act
            var result = await _authService.LogoutAsync(refreshToken);

            // Assert
            result.Should().BeTrue();

            _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(refreshToken), Times.Once);
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_InvalidRefreshToken_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = "invalid.refresh.token";
            _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken)).ReturnsAsync(false);

            // Act
            var result = await _authService.LogoutAsync(refreshToken);

            // Assert
            result.Should().BeFalse();

            _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task ValidateAccessTokenAsync_ValidToken_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accessToken = "access.token.here";

            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken)).ReturnsAsync(userId);

            // Act
            var result = await _authService.ValidateAccessTokenAsync(accessToken);

            // Assert
            result.Should().BeTrue();

            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(accessToken), Times.Once);
        }

        [Fact]
        public async Task ValidateAccessTokenAsync_InvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var accessToken = "invalid.access.token";
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken)).ReturnsAsync((Guid?)null);

            // Act
            var result = await _authService.ValidateAccessTokenAsync(accessToken);

            // Assert
            result.Should().BeFalse();

            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(accessToken), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_ValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accessToken = "access.token.here";

            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken)).ReturnsAsync(userId);

            // Act
            var result = await _authService.GetCurrentUserIdAsync(accessToken);

            // Assert
            result.Should().Be(userId);

            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(accessToken), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_InvalidToken_ShouldReturnNull()
        {
            // Arrange
            var accessToken = "invalid.access.token";
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken)).ReturnsAsync((Guid?)null);

            // Act
            var result = await _authService.GetCurrentUserIdAsync(accessToken);

            // Assert
            result.Should().BeNull();

            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(accessToken), Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_ExceptionThrown_ShouldReturnFailureResult()
        {
            // Arrange
            var provider = "test";
            var accessToken = Guid.NewGuid().ToString();

            _mockTokenService.Setup(x => x.GenerateTokensAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _authService.LoginWithOAuthAsync(provider, accessToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("OAuth authentication failed due to internal error");
        }

        [Fact]
        public async Task RefreshTokenAsync_ExceptionThrown_ShouldReturnFailureResult()
        {
            // Arrange
            var refreshToken = "refresh.token.here";

            _mockTokenService.Setup(x => x.RefreshAccessTokenAsync(refreshToken))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Token refresh failed due to internal error");
        }

        [Fact]
        public async Task LogoutAsync_ExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = "refresh.token.here";

            _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _authService.LogoutAsync(refreshToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateAccessTokenAsync_ExceptionThrown_ShouldReturnFalse()
        {
            // Arrange
            var accessToken = "access.token.here";

            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _authService.ValidateAccessTokenAsync(accessToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCurrentUserIdAsync_ExceptionThrown_ShouldReturnNull()
        {
            // Arrange
            var accessToken = "access.token.here";

            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(accessToken))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _authService.GetCurrentUserIdAsync(accessToken);

            // Assert
            result.Should().BeNull();
        }
    }
}
