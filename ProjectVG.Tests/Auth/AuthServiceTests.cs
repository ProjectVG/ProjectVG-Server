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
        public async Task GuestLoginAsync_ExistingUser_ShouldReturnSuccessResult()
        {
            // Arrange
            var guestId = "guest123";
            var userId = Guid.NewGuid();

            var existingUser = new UserDto
            {
                Id = userId,
                Username = "guest_user",
                Email = "guest@guest.local",
                Status = AccountStatus.Active,
                Provider = "guest",
                ProviderId = guestId
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
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _authService.GuestLoginAsync(guestId);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be("guest");
            result.User.ProviderId.Should().Be(guestId);
            result.User.Status.Should().Be(AccountStatus.Active);

            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GuestLoginAsync_NewUser_ShouldCreateUserAndReturnSuccessResult()
        {
            // Arrange
            var guestId = "guest456";
            var userId = Guid.NewGuid();

            var newUser = new UserDto
            {
                Id = userId,
                Username = $"guest_{guestId}",
                Email = $"guest@guest{guestId}.local",
                Status = AccountStatus.Active,
                Provider = "guest",
                ProviderId = guestId
            };

            var tokenResponse = new TokenResponse
            {
                AccessToken = "access.token.here",
                RefreshToken = "refresh.token.here",
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };

            _mockUserService.Setup(x => x.TryGetByProviderAsync("guest", guestId)).ReturnsAsync((UserDto?)null);
            _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>())).ReturnsAsync(newUser);
            _mockTokenService.Setup(x => x.GenerateTokensAsync(userId)).ReturnsAsync(tokenResponse);
            _mockTokenManagementService.Setup(x => x.GrantInitialCreditsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _authService.GuestLoginAsync(guestId);

            // Assert
            result.Should().NotBeNull();
            result.Tokens.Should().Be(tokenResponse);
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be(userId);
            result.User.Provider.Should().Be("guest");
            result.User.ProviderId.Should().Be(guestId);

            _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateCommand>()), Times.Once);
            _mockTokenService.Verify(x => x.GenerateTokensAsync(userId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GrantInitialCreditsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GuestLoginAsync_EmptyGuestId_ShouldThrowValidationException()
        {
            // Arrange
            var guestId = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _authService.GuestLoginAsync(guestId)
            );

            exception.ErrorCode.Should().Be(ErrorCode.GUEST_ID_INVALID);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ValidRefreshToken_ShouldReturnSuccessResult()
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
                Username = "test_user",
                Email = "test@example.com",
                Status = AccountStatus.Active,
                Provider = "guest",
                ProviderId = "guest123"
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
        }

        [Fact]
        public async Task LogoutAsync_ValidRefreshToken_ShouldReturnTrue()
        {
            // Arrange
            var refreshToken = "refresh.token.here";
            var userId = Guid.NewGuid();

            _mockTokenService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken)).ReturnsAsync(true);
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(refreshToken)).ReturnsAsync(userId);

            // Act
            var result = await _authService.LogoutAsync(refreshToken);

            // Assert
            result.Should().BeTrue();
            _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(refreshToken), Times.Once);
        }
    }
}