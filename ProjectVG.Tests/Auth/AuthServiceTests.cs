using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Models;
using ProjectVG.Application.Models.User;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;
using Xunit;

namespace ProjectVG.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ICreditManagementService> _mockTokenManagementService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockTokenManagementService = new Mock<ICreditManagementService>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockUserService.Object,
                _mockTokenService.Object,
                _mockTokenManagementService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task LoginWithOAuthAsync_ValidTestProvider_ShouldReturnSuccessResult()
        {
            // Arrange
            var provider = "test";
            var accessToken = Guid.NewGuid().ToString();
            var userId = Guid.Parse(accessToken);

            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, accessToken);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be(provider);
            result.User.ProviderId.Should().Be(accessToken);
            result.User.Status.Should().Be(AccountStatus.Active);

            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_InvalidProvider_ShouldThrowValidationException()
        {
            // Arrange
            var provider = "invalid";
            var accessToken = Guid.NewGuid().ToString();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.SignInWithOAuthAsync(provider, accessToken)
            );

            exception.ErrorCode.Should().Be(ErrorCode.INVALID_INPUT);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_InvalidUserIdFormat_ShouldThrowValidationException()
        {
            // Arrange
            var provider = "test";
            var accessToken = "invalid-guid-format";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.SignInWithOAuthAsync(provider, accessToken)
            );

            exception.ErrorCode.Should().Be(ErrorCode.INVALID_INPUT);
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
            var result = await _authService.RefreshAccessTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
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
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be(provider);
            result.User.ProviderId.Should().Be(guestId);
            result.User.Status.Should().Be(AccountStatus.Active);

            _mockUserService.Verify(x => x.TryGetByProviderAsync("guest", guestId), Times.Once);
            _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>()), Times.Once);
            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
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
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().Be(existingUser);

            _mockUserService.Verify(x => x.TryGetByProviderAsync("guest", guestId), Times.Once);
            _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>()), Times.Never);
            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidRefreshToken_ShouldThrowValidationException()
        {
            // Arrange
            var refreshToken = "invalid.refresh.token";
            _mockTokenService.Setup(x => x.RefreshAccessTokenAsync(refreshToken)).ReturnsAsync((TokenResponse?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.RefreshAccessTokenAsync(refreshToken)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_INVALID);

            _mockTokenService.Verify(x => x.RefreshAccessTokenAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_EmptyRefreshToken_ShouldThrowValidationException()
        {
            // Arrange
            var refreshToken = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.RefreshAccessTokenAsync(refreshToken)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_INVALID);
        }

        [Fact]
        public async Task LogoutAsync_EmptyRefreshToken_ShouldThrowValidationException()
        {
            // Arrange
            var refreshToken = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.LogoutAsync(refreshToken)
            );

            exception.ErrorCode.Should().Be(ErrorCode.TOKEN_INVALID);
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
        public async Task LoginWithOAuthAsync_ExceptionThrown_ShouldPropagateException()
        {
            // Arrange
            var provider = "test";
            var accessToken = Guid.NewGuid().ToString();

            _mockTokenService.Setup(x => x.GenerateTokensAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _authService.SignInWithOAuthAsync(provider, accessToken)
            );
        }

        [Fact]
        public async Task RefreshTokenAsync_ExceptionThrown_ShouldPropagateException()
        {
            // Arrange
            var refreshToken = "refresh.token.here";

            _mockTokenService.Setup(x => x.RefreshAccessTokenAsync(refreshToken))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _authService.RefreshAccessTokenAsync(refreshToken)
            );
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

        #region Token Granting Tests

        [Fact]
        public async Task LoginWithOAuthAsync_NewUser_ShouldGrantInitialTokensAndLogSuccess()
        {
            // Arrange
            var provider = "guest";
            var guestId = "new_guest_user";
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

            _mockUserService.Setup(x => x.TryGetByProviderAsync(provider, guestId)).ReturnsAsync((UserDto?)null);
            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>())).ReturnsAsync(createdUser);
            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.User.Should().Be(createdUser);

            // Verify token granting was attempted
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);

            // Verify success logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initial tokens (5000) granted successfully") && 
                                                v.ToString()!.Contains(userId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_ExistingUser_ShouldNotGrantTokensAndLogAlreadyGranted()
        {
            // Arrange
            var provider = "guest";
            var guestId = "existing_guest_user";
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

            _mockUserService.Setup(x => x.TryGetByProviderAsync(provider, guestId)).ReturnsAsync(existingUser);
            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, guestId);

            // Assert
            result.Should().NotBeNull();
            result.User.Should().Be(existingUser);

            // Verify token granting was attempted
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);

            // Verify already granted logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initial tokens already granted or grant failed") && 
                                                v.ToString()!.Contains(userId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_TokenGrantingFails_ShouldStillCompleteLoginAndLogFailure()
        {
            // Arrange
            var provider = "google";
            var providerId = "google_user_123";
            var userId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _authService.SignInWithOAuthAsync(provider, providerId);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);

            // Verify token granting was attempted
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);

            // Verify failure logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initial tokens already granted or grant failed") && 
                                                v.ToString()!.Contains(userId.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LoginWithOAuthAsync_TokenGrantingThrowsException_ShouldNotFailLoginProcess()
        {
            // Arrange
            var provider = "test";
            var accessToken = Guid.NewGuid().ToString();
            var userId = Guid.Parse(accessToken);
            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId))
                .ThrowsAsync(new Exception("Credit granting service unavailable"));

            // Act & Assert - Should not throw exception
            var result = await _authService.SignInWithOAuthAsync(provider, accessToken);

            // Verify login still completed successfully
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User!.Id.Should().Be(userId);

            // Verify token granting was attempted
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
        }

        #endregion

    }
}
