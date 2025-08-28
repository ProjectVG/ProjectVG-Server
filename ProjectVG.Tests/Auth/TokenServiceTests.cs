using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Models;
using Xunit;

namespace ProjectVG.Tests.Auth
{
    public class TokenServiceTests
    {
        private readonly TokenService _tokenService;
        private readonly Mock<IJwtProvider> _mockJwtProvider;
        private readonly Mock<IRefreshTokenStorage> _mockRefreshTokenStorage;
        private readonly Mock<ILogger<TokenService>> _mockLogger;

        public TokenServiceTests()
        {
            _mockJwtProvider = new Mock<IJwtProvider>();
            _mockRefreshTokenStorage = new Mock<IRefreshTokenStorage>();
            _mockLogger = new Mock<ILogger<TokenService>>();

            _tokenService = new TokenService(
                _mockJwtProvider.Object,
                _mockRefreshTokenStorage.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GenerateTokensAsync_ValidUserId_ShouldReturnTokenResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accessToken = "access.token.here";
            var refreshToken = "refresh.token.here";

            _mockJwtProvider.Setup(x => x.GenerateAccessToken(userId)).Returns(accessToken);
            _mockJwtProvider.Setup(x => x.GenerateRefreshToken(userId)).Returns(refreshToken);
            _mockRefreshTokenStorage.Setup(x => x.StoreRefreshTokenAsync(refreshToken, userId, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            // Act
            var result = await _tokenService.GenerateTokensAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be(accessToken);
            result.RefreshToken.Should().Be(refreshToken);
            result.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
            result.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

            _mockJwtProvider.Verify(x => x.GenerateAccessToken(userId), Times.Once);
            _mockJwtProvider.Verify(x => x.GenerateRefreshToken(userId), Times.Once);
            _mockRefreshTokenStorage.Verify(x => x.StoreRefreshTokenAsync(refreshToken, userId, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task GenerateTokensAsync_StorageFails_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accessToken = "access.token.here";
            var refreshToken = "refresh.token.here";

            _mockJwtProvider.Setup(x => x.GenerateAccessToken(userId)).Returns(accessToken);
            _mockJwtProvider.Setup(x => x.GenerateRefreshToken(userId)).Returns(refreshToken);
            _mockRefreshTokenStorage.Setup(x => x.StoreRefreshTokenAsync(refreshToken, userId, It.IsAny<DateTime>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _tokenService.GenerateTokensAsync(userId));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ValidRefreshToken_ShouldReturnNewTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";
            var newAccessToken = "new.access.token.here";

            var principal = CreateValidPrincipal(userId, "refresh");
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);
            _mockRefreshTokenStorage.Setup(x => x.IsRefreshTokenValidAsync(refreshToken)).ReturnsAsync(true);
            _mockRefreshTokenStorage.Setup(x => x.GetUserIdFromRefreshTokenAsync(refreshToken)).ReturnsAsync(userId);
            _mockJwtProvider.Setup(x => x.GenerateAccessToken(userId)).Returns(newAccessToken);

            // Act
            var result = await _tokenService.RefreshAccessTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be(newAccessToken);
            result.RefreshToken.Should().Be(refreshToken);
            result.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
            result.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_InvalidTokenFormat_ShouldReturnNull()
        {
            // Arrange
            var refreshToken = "invalid.token.here";
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns((System.Security.Claims.ClaimsPrincipal?)null);

            // Act
            var result = await _tokenService.RefreshAccessTokenAsync(refreshToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_NotRefreshToken_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "access.token.here";

            var principal = CreateValidPrincipal(userId, "access"); // access 토큰으로 잘못 설정
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);

            // Act
            var result = await _tokenService.RefreshAccessTokenAsync(refreshToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_InvalidInStorage_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";

            var principal = CreateValidPrincipal(userId, "refresh");
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);
            _mockRefreshTokenStorage.Setup(x => x.IsRefreshTokenValidAsync(refreshToken)).ReturnsAsync(false);

            // Act
            var result = await _tokenService.RefreshAccessTokenAsync(refreshToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ValidToken_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";

            var principal = CreateValidPrincipal(userId, "refresh");
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);
            _mockRefreshTokenStorage.Setup(x => x.IsRefreshTokenValidAsync(refreshToken)).ReturnsAsync(true);

            // Act
            var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_InvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = "invalid.token.here";
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns((System.Security.Claims.ClaimsPrincipal?)null);

            // Act
            var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_NotRefreshToken_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "access.token.here";

            var principal = CreateValidPrincipal(userId, "access");
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);

            // Act
            var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_InvalidInStorage_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var refreshToken = "refresh.token.here";

            var principal = CreateValidPrincipal(userId, "refresh");
            _mockJwtProvider.Setup(x => x.ValidateToken(refreshToken)).Returns(principal);
            _mockRefreshTokenStorage.Setup(x => x.IsRefreshTokenValidAsync(refreshToken)).ReturnsAsync(false);

            // Act
            var result = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ValidToken_ShouldReturnTrue()
        {
            // Arrange
            var refreshToken = "refresh.token.here";
            _mockRefreshTokenStorage.Setup(x => x.RemoveRefreshTokenAsync(refreshToken)).ReturnsAsync(true);

            // Act
            var result = await _tokenService.RevokeRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeTrue();
            _mockRefreshTokenStorage.Verify(x => x.RemoveRefreshTokenAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_StorageFails_ShouldReturnFalse()
        {
            // Arrange
            var refreshToken = "refresh.token.here";
            _mockRefreshTokenStorage.Setup(x => x.RemoveRefreshTokenAsync(refreshToken)).ReturnsAsync(false);

            // Act
            var result = await _tokenService.RevokeRefreshTokenAsync(refreshToken);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserIdFromTokenAsync_ValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid.token.here";

            _mockJwtProvider.Setup(x => x.GetUserIdFromToken(token)).Returns(userId.ToString());

            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(token);

            // Assert
            result.Should().Be(userId);
            _mockJwtProvider.Verify(x => x.GetUserIdFromToken(token), Times.Once);
        }

        [Fact]
        public async Task GetUserIdFromTokenAsync_InvalidToken_ShouldReturnNull()
        {
            // Arrange
            var token = "invalid.token.here";
            _mockJwtProvider.Setup(x => x.GetUserIdFromToken(token)).Returns((string?)null);

            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(token);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserIdFromTokenAsync_InvalidGuid_ShouldReturnNull()
        {
            // Arrange
            var token = "valid.token.here";
            _mockJwtProvider.Setup(x => x.GetUserIdFromToken(token)).Returns("invalid-guid");

            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(token);

            // Assert
            result.Should().BeNull();
        }

        private static System.Security.Claims.ClaimsPrincipal CreateValidPrincipal(Guid userId, string tokenType)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
                new System.Security.Claims.Claim("token_type", tokenType)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Bearer");
            return new System.Security.Claims.ClaimsPrincipal(identity);
        }
    }
}
