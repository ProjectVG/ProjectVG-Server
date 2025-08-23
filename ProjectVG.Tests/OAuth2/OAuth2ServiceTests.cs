using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Cache;

namespace ProjectVG.Tests.OAuth2;

public class OAuth2ServiceTests
{
    private readonly Mock<IRedisCacheService> _mockCache;
    private readonly Mock<ILogger<OAuth2Service>> _mockLogger;
    private readonly OAuth2Service _service;
    private readonly OAuth2Settings _settings;

    public OAuth2ServiceTests()
    {
        _mockCache = new Mock<IRedisCacheService>();
        _mockLogger = new Mock<ILogger<OAuth2Service>>();
        _settings = new OAuth2Settings
        {
            RedirectUris = new List<string> { "http://localhost:3000/callback" },
            Clients = new List<OAuth2Client>
            {
                new OAuth2Client
                {
                    ClientId = "unity-client",
                    ClientName = "Unity Game Client",
                    AllowedGrantTypes = new List<string> { "authorization_code" },
                    RequirePkce = true
                }
            }
        };
        
        _service = new OAuth2Service(_mockCache.Object, _mockLogger.Object, Options.Create(_settings));
    }

    [Fact]
    public async Task GenerateAuthorizationCodeAsync_ShouldGenerateValidCode()
    {
        // Arrange
        var clientId = "unity-client";
        var userId = Guid.NewGuid();
        var codeChallenge = "test-challenge";
        var codeChallengeMethod = "S256";

        _mockCache.Setup(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var code = await _service.GenerateAuthorizationCodeAsync(
            clientId, userId, codeChallenge, codeChallengeMethod);

        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        _mockCache.Verify(c => c.SetStringAsync(
            It.Is<string>(key => key.StartsWith("oauth2:code:")),
            It.IsAny<string>(),
            TimeSpan.FromMinutes(10)), Times.Once);
    }

    [Fact]
    public async Task ValidateClientAsync_ShouldReturnTrue_ForValidClient()
    {
        // Act
        var result = await _service.ValidateClientAsync("unity-client");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateClientAsync_ShouldReturnFalse_ForInvalidClient()
    {
        // Act
        var result = await _service.ValidateClientAsync("invalid-client");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRedirectUriAsync_ShouldReturnTrue_ForValidUri()
    {
        // Act
        var result = await _service.ValidateRedirectUriAsync("unity-client", "http://localhost:3000/callback");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateRedirectUriAsync_ShouldReturnFalse_ForInvalidUri()
    {
        // Act
        var result = await _service.ValidateRedirectUriAsync("unity-client", "http://malicious.com/callback");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidatePkceAsync_ShouldReturnTrue_ForValidPkce()
    {
        // Arrange
        var codeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var codeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"; // SHA256 of verifier

        // Act
        var result = await _service.ValidatePkceAsync(codeChallenge, "S256", codeVerifier);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidatePkceAsync_ShouldReturnFalse_ForInvalidPkce()
    {
        // Arrange
        var codeVerifier = "invalid-verifier";
        var codeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        // Act
        var result = await _service.ValidatePkceAsync(codeChallenge, "S256", codeVerifier);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldReturnNull_ForInvalidCode()
    {
        // Arrange
        _mockCache.Setup(c => c.GetStringAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.ExchangeCodeAsync("invalid-code", "unity-client", "verifier");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeCodeAsync_ShouldDeleteFromCache()
    {
        // Arrange
        var code = "test-code";
        _mockCache.Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.RevokeCodeAsync(code);

        // Assert
        _mockCache.Verify(c => c.DeleteAsync($"oauth2:code:{code}"), Times.Once);
    }
}
