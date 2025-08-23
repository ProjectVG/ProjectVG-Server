using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Infrastructure.Cache;
using Xunit;

namespace ProjectVG.Tests.Auth;

public class AuthTokenServiceTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ITokenBlocklistService> _blocklistServiceMock;
    private readonly Mock<ILogger<AuthTokenService>> _loggerMock;
    private readonly AuthTokenService _authTokenService;

    public AuthTokenServiceTests()
    {
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _blocklistServiceMock = new Mock<ITokenBlocklistService>();
        _loggerMock = new Mock<ILogger<AuthTokenService>>();

        _authTokenService = new AuthTokenService(
            _refreshTokenServiceMock.Object,
            _tokenServiceMock.Object,
            _blocklistServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var userId = Guid.NewGuid();
        var currentToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "new_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(currentToken);

        _refreshTokenServiceMock
            .Setup(x => x.DetectTokenReuseAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(currentToken, null, null))
            .ReturnsAsync(newRefreshToken);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(userId, null))
            .ReturnsAsync("new_access_token");

        // Act
        var result = await _authTokenService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_hash", result.RefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(900, result.ExpiresIn);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var refreshToken = "invalid_refresh_token";

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _authTokenService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid refresh token", result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithTokenReuse_ShouldRevokeAllTokens()
    {
        // Arrange
        var refreshToken = "reused_token";
        var userId = Guid.NewGuid();
        var currentToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(currentToken);

        _refreshTokenServiceMock
            .Setup(x => x.DetectTokenReuseAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authTokenService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Token reuse detected", result.ErrorMessage);
        _refreshTokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, null), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithAccessToken_ShouldBlockToken()
    {
        // Arrange
        var accessToken = "valid_access_token";
        var userId = Guid.NewGuid();

        var tokenValidation = new TokenValidationResult
        {
            IsValid = true,
            UserId = userId
        };

        _tokenServiceMock
            .Setup(x => x.ValidateAccessTokenAsync(accessToken))
            .ReturnsAsync(tokenValidation);

        // Act
        var result = await _authTokenService.RevokeTokenAsync(accessToken);

        // Assert
        Assert.True(result.IsSuccess);
        _blocklistServiceMock.Verify(x => x.BlockAccessTokenAsync(accessToken, TimeSpan.FromMinutes(15)), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithRefreshToken_ShouldRevokeFromDB()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var userId = Guid.NewGuid();

        var tokenValidation = new TokenValidationResult
        {
            IsValid = false
        };

        var dbRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash"
        };

        _tokenServiceMock
            .Setup(x => x.ValidateAccessTokenAsync(refreshToken))
            .ReturnsAsync(tokenValidation);

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(dbRefreshToken);

        // Act
        var result = await _authTokenService.RevokeTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        _refreshTokenServiceMock.Verify(x => x.RevokeTokenAsync(dbRefreshToken.Id), Times.Once);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldCallRefreshTokenService()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _authTokenService.RevokeAllUserTokensAsync(userId);

        // Assert
        _refreshTokenServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, null), Times.Once);
    }
}
