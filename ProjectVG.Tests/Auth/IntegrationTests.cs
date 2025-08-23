using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Cache;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Auth;

namespace ProjectVG.Tests.Auth;

public class IntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ProjectVGDbContext _context;

    public IntegrationTests()
    {
        var services = new ServiceCollection();
        
        // In-Memory 데이터베이스 설정
        services.AddDbContext<ProjectVGDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // 필요한 서비스 등록
        services.AddScoped<IRefreshTokenRepository, SqlServerRefreshTokenRepository>();
        
        // Mock ITokenBlocklistService for testing
        var mockBlocklistService = new Mock<ITokenBlocklistService>();
        mockBlocklistService.Setup(b => b.IsRefreshTokenBlockedAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        mockBlocklistService.Setup(b => b.BlockRefreshTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton(mockBlocklistService.Object);
        
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddLogging();
        services.Configure<JwtSettings>(opts =>
        {
            opts.RefreshTokenLifetimeDays = 30;
        });

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ProjectVGDbContext>();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task RefreshToken_FullWorkflow_ShouldWork()
    {
        // Arrange
        var refreshTokenService = _serviceProvider.GetRequiredService<IRefreshTokenService>();
        var userId = Guid.NewGuid();
        var clientId = "test-client";
        var deviceId = "test-device";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act & Assert

        // 1. 토큰 생성
        var token1 = await refreshTokenService.CreateAsync(userId, clientId, deviceId, expiresAt);
        Assert.NotNull(token1);
        
        // 실제 토큰 값 저장 (CreateAsync에서 반환된 값)
        var tokenValue1 = token1.TokenHash;

        // 2. 토큰 조회
        var foundToken = await refreshTokenService.GetActiveTokenAsync(tokenValue1);
        Assert.NotNull(foundToken);
        Assert.Equal(userId, foundToken.UserId);

        // 3. 토큰 로테이션
        var token2 = await refreshTokenService.RotateTokenAsync(foundToken);
        Assert.NotNull(token2);
        Assert.NotEqual(token1.Id, token2.Id);
        
        var tokenValue2 = token2.TokenHash;

        // 4. 이전 토큰은 비활성화되어야 함
        var oldToken = await refreshTokenService.GetActiveTokenAsync(tokenValue1);
        Assert.Null(oldToken);

        // 5. 새 토큰은 활성화되어야 함
        var newToken = await refreshTokenService.GetActiveTokenAsync(tokenValue2);
        Assert.NotNull(newToken);

        // 6. 재사용 감지 테스트 - 폐기된 토큰을 다시 사용하려고 시도
        var reuseDetected = await refreshTokenService.DetectTokenReuseAsync(tokenValue1);
        Assert.True(reuseDetected);

        // 7. 토큰 폐기
        var revoked = await refreshTokenService.RevokeTokenAsync(token2.Id);
        Assert.True(revoked);

        // 8. 폐기된 토큰은 조회되지 않아야 함
        var revokedToken = await refreshTokenService.GetActiveTokenAsync(tokenValue2);
        Assert.Null(revokedToken);
    }

    [Fact]
    public async Task RefreshToken_MultipleDevices_ShouldWorkIndependently()
    {
        // Arrange
        var refreshTokenService = _serviceProvider.GetRequiredService<IRefreshTokenService>();
        var userId = Guid.NewGuid();
        var clientId = "test-client";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        var device1Token = await refreshTokenService.CreateAsync(userId, clientId, "device-1", expiresAt);
        var device2Token = await refreshTokenService.CreateAsync(userId, clientId, "device-2", expiresAt);

        var device1TokenValue = device1Token.TokenHash;
        var device2TokenValue = device2Token.TokenHash;

        // Assert
        var token1 = await refreshTokenService.GetActiveTokenAsync(device1TokenValue);
        var token2 = await refreshTokenService.GetActiveTokenAsync(device2TokenValue);

        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1.Id, token2.Id);
        Assert.Equal("device-1", token1.DeviceId);
        Assert.Equal("device-2", token2.DeviceId);

        // 한 디바이스의 토큰을 폐기해도 다른 디바이스는 영향받지 않아야 함
        await refreshTokenService.RevokeTokenAsync(token1.Id);

        var revokedToken1 = await refreshTokenService.GetActiveTokenAsync(device1TokenValue);
        var activeToken2 = await refreshTokenService.GetActiveTokenAsync(device2TokenValue);

        Assert.Null(revokedToken1);
        Assert.NotNull(activeToken2);
    }

    [Fact]
    public async Task RefreshToken_RevokeAllUserTokens_ShouldRevokeAllExceptSpecified()
    {
        // Arrange
        var refreshTokenService = _serviceProvider.GetRequiredService<IRefreshTokenService>();
        var userId = Guid.NewGuid();
        var clientId = "test-client";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var token1 = await refreshTokenService.CreateAsync(userId, clientId, "device-1", expiresAt);
        var token2 = await refreshTokenService.CreateAsync(userId, clientId, "device-2", expiresAt);
        var token3 = await refreshTokenService.CreateAsync(userId, clientId, "device-3", expiresAt);

        // Act - token2는 제외하고 모든 토큰 폐기
        var revoked = await refreshTokenService.RevokeAllUserTokensAsync(userId, token2.Id);

        // Assert
        Assert.True(revoked);

        var token1Value = token1.TokenHash;
        var token2Value = token2.TokenHash;
        var token3Value = token3.TokenHash;

        var activeToken1 = await refreshTokenService.GetActiveTokenAsync(token1Value);
        var activeToken2 = await refreshTokenService.GetActiveTokenAsync(token2Value);
        var activeToken3 = await refreshTokenService.GetActiveTokenAsync(token3Value);

        Assert.Null(activeToken1); // 폐기됨
        Assert.NotNull(activeToken2); // 예외로 활성 상태 유지
        Assert.Null(activeToken3); // 폐기됨
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
