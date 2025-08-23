using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Persistence.Repositories.Auth;
using ProjectVG.Infrastructure.Cache;

namespace ProjectVG.Tests.Auth;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRepository;
    private readonly Mock<ITokenBlocklistService> _mockBlocklistService;
    private readonly Mock<ILogger<RefreshTokenService>> _mockLogger;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        _mockRepository = new Mock<IRefreshTokenRepository>();
        _mockBlocklistService = new Mock<ITokenBlocklistService>();
        _mockLogger = new Mock<ILogger<RefreshTokenService>>();
        _jwtSettings = Options.Create(new JwtSettings
        {
            RefreshTokenLifetimeDays = 30
        });
        
        _service = new RefreshTokenService(_mockRepository.Object, _mockBlocklistService.Object, _mockLogger.Object, _jwtSettings);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateRefreshToken_WithValidParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clientId = "test-client";
        var deviceId = "test-device";
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var ipAddress = "127.0.0.1";
        var userAgent = "Test User Agent";

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Returns<RefreshToken>(token => Task.FromResult(token));

        // Act
        var result = await _service.CreateAsync(userId, clientId, deviceId, expiresAt, ipAddress, userAgent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(deviceId, result.DeviceId);
        Assert.Equal(ipAddress, result.IpAddress);
        Assert.Equal(userAgent, result.UserAgent);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveTokenAsync_ShouldReturnNull_WhenTokenNotFound()
    {
        // Arrange
        var tokenHash = "invalid-token";
        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _service.GetActiveTokenAsync(tokenHash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTokenAsync_ShouldReturnNull_WhenTokenIsInactive()
    {
        // Arrange
        var tokenHash = "valid-token";
        var inactiveToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClientId = "test-client",
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = DateTime.UtcNow.AddDays(-1), // 이미 폐기됨
            LastUsedAt = DateTime.UtcNow.AddDays(-2)
        };

        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(inactiveToken);

        // Act
        var result = await _service.GetActiveTokenAsync(tokenHash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTokenAsync_ShouldReturnToken_WhenTokenIsActive()
    {
        // Arrange
        var tokenHash = "valid-token";
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClientId = "test-client",
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = null, // 활성 상태
            LastUsedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(activeToken);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _mockBlocklistService.Setup(b => b.IsRefreshTokenBlockedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetActiveTokenAsync(tokenHash);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(activeToken.Id, result.Id);
        Assert.True(result.LastUsedAt >= activeToken.LastUsedAt); // LastUsedAt이 업데이트되어야 함
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateTokenAsync_ShouldReturnNull_WhenCurrentTokenIsInactive()
    {
        // Arrange
        var inactiveToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClientId = "test-client",
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = DateTime.UtcNow.AddDays(-1), // 이미 폐기됨
            LastUsedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var result = await _service.RotateTokenAsync(inactiveToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DetectTokenReuseAsync_ShouldReturnTrue_WhenRevokedTokenIsUsed()
    {
        // Arrange
        var tokenHash = "revoked-token";
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClientId = "test-client",
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = DateTime.UtcNow.AddDays(-1), // 이미 폐기됨
            LastUsedAt = DateTime.UtcNow.AddDays(-2)
        };

        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(revokedToken);
        _mockRepository.Setup(r => r.GetTokenFamilyAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<RefreshToken> { revokedToken });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DetectTokenReuseAsync(tokenHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DetectTokenReuseAsync_ShouldReturnFalse_WhenTokenNotFound()
    {
        // Arrange
        var tokenHash = "non-existent-token";
        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _service.DetectTokenReuseAsync(tokenHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DetectTokenReuseAsync_ShouldReturnFalse_WhenTokenIsActive()
    {
        // Arrange
        var tokenHash = "active-token";
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ClientId = "test-client",
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            RevokedAt = null, // 활성 상태
            LastUsedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(activeToken);

        // Act
        var result = await _service.DetectTokenReuseAsync(tokenHash);

        // Assert
        Assert.False(result);
    }
}
