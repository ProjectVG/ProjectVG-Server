using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Infrastructure.Cache;
using Xunit;

namespace ProjectVG.Tests.Cache;

public class TokenBlocklistServiceTests
{
    private readonly Mock<IRedisCacheService> _mockRedis;
    private readonly Mock<ILogger<TokenBlocklistService>> _mockLogger;
    private readonly TokenBlocklistService _service;

    public TokenBlocklistServiceTests()
    {
        _mockRedis = new Mock<IRedisCacheService>();
        _mockLogger = new Mock<ILogger<TokenBlocklistService>>();
        _service = new TokenBlocklistService(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task BlockRefreshTokenAsync_ShouldBlockToken()
    {
        // Arrange
        var tokenHash = "test-token-hash";
        var expiry = TimeSpan.FromHours(1);

        _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), expiry))
            .Returns(Task.CompletedTask);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, null))
            .ReturnsAsync(1);

        // Act
        await _service.BlockRefreshTokenAsync(tokenHash, expiry);

        // Assert
        _mockRedis.Verify(r => r.SetStringAsync(
            "refresh:blocked:test-token-hash", 
            It.IsAny<string>(), 
            expiry), Times.Once);
        _mockRedis.Verify(r => r.IncrementAsync("tokens:blocked:count", 1, null), Times.Once);
    }

    [Fact]
    public async Task IsRefreshTokenBlockedAsync_ShouldReturnTrue_WhenTokenIsBlocked()
    {
        // Arrange
        var tokenHash = "blocked-token-hash";
        _mockRedis.Setup(r => r.ExistsAsync("refresh:blocked:blocked-token-hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsRefreshTokenBlockedAsync(tokenHash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRefreshTokenBlockedAsync_ShouldReturnFalse_WhenTokenIsNotBlocked()
    {
        // Arrange
        var tokenHash = "valid-token-hash";
        _mockRedis.Setup(r => r.ExistsAsync("refresh:blocked:valid-token-hash"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsRefreshTokenBlockedAsync(tokenHash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BlockAccessTokenAsync_ShouldBlockToken()
    {
        // Arrange
        var jti = "test-jti";
        var expiry = TimeSpan.FromMinutes(30);

        _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), expiry))
            .Returns(Task.CompletedTask);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, null))
            .ReturnsAsync(1);

        // Act
        await _service.BlockAccessTokenAsync(jti, expiry);

        // Assert
        _mockRedis.Verify(r => r.SetStringAsync(
            "access:blocked:test-jti", 
            It.IsAny<string>(), 
            expiry), Times.Once);
        _mockRedis.Verify(r => r.IncrementAsync("tokens:blocked:count", 1, null), Times.Once);
    }

    [Fact]
    public async Task UnblockRefreshTokenAsync_ShouldUnblockToken()
    {
        // Arrange
        var tokenHash = "test-token-hash";
        _mockRedis.Setup(r => r.DeleteAsync("refresh:blocked:test-token-hash"))
            .ReturnsAsync(true);
        _mockRedis.Setup(r => r.DecrementAsync(It.IsAny<string>(), 1, null))
            .ReturnsAsync(0);

        // Act
        await _service.UnblockRefreshTokenAsync(tokenHash);

        // Assert
        _mockRedis.Verify(r => r.DeleteAsync("refresh:blocked:test-token-hash"), Times.Once);
        _mockRedis.Verify(r => r.DecrementAsync("tokens:blocked:count", 1, null), Times.Once);
    }

    [Fact]
    public async Task GetBlockedTokenCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetStringAsync("tokens:blocked:count"))
            .ReturnsAsync("42");

        // Act
        var result = await _service.GetBlockedTokenCountAsync();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetBlockedTokenCountAsync_ShouldReturnZero_WhenCountNotSet()
    {
        // Arrange
        _mockRedis.Setup(r => r.GetStringAsync("tokens:blocked:count"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetBlockedTokenCountAsync();

        // Assert
        Assert.Equal(0, result);
    }
}
