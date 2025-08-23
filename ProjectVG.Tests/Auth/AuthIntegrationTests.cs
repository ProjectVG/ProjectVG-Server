using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Xunit;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Persistence.EfCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using ProjectVG.Infrastructure.Cache;

namespace ProjectVG.Tests.Auth;

[Trait("Category", "IntegrationTest")]
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing SQL Server DbContext
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ProjectVGDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Disable Model State validation for integration tests
                services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

                // Add In-Memory database
                services.AddDbContext<ProjectVGDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AuthTestDb");
                });

                // Ensure database is created for In-Memory provider
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ProjectVGDbContext>();
                dbContext.Database.EnsureCreated();

                // Mock Redis services
                var mockRedisCacheService = new Mock<IRedisCacheService>();
                var mockTokenBlocklistService = new Mock<ITokenBlocklistService>();
                
                // Mock WebSocket service
                var mockWebSocketManager = new Mock<ProjectVG.Application.Services.WebSocket.IWebSocketManager>();
                
                // Mock IKeyStore
                var mockKeyStore = new Mock<ProjectVG.Infrastructure.Auth.IKeyStore>();
                var testKey = System.Security.Cryptography.RSA.Create(2048);
                var rsaSecurityKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(testKey) { KeyId = "test-key" };
                
                mockKeyStore.Setup(x => x.GetCurrentSigningKeyAsync())
                    .ReturnsAsync(rsaSecurityKey);
                mockKeyStore.Setup(x => x.GetValidationKeysAsync())
                    .ReturnsAsync(new[] { rsaSecurityKey });

                // Setup default behaviors
                mockRedisCacheService.Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                    .Returns(Task.CompletedTask);
                mockRedisCacheService.Setup(x => x.GetStringAsync(It.IsAny<string>()))
                    .ReturnsAsync((string?)null);
                mockRedisCacheService.Setup(x => x.DeleteAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);

                mockTokenBlocklistService.Setup(x => x.IsRefreshTokenBlockedAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);
                mockTokenBlocklistService.Setup(x => x.IsAccessTokenBlockedAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

                // Remove existing services and add mocks
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisCacheService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(mockRedisCacheService.Object);

                var blocklistDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenBlocklistService));
                if (blocklistDescriptor != null)
                {
                    services.Remove(blocklistDescriptor);
                }
                services.AddSingleton(mockTokenBlocklistService.Object);

                var webSocketDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ProjectVG.Application.Services.WebSocket.IWebSocketManager));
                if (webSocketDescriptor != null)
                {
                    services.Remove(webSocketDescriptor);
                }
                services.AddSingleton(mockWebSocketManager.Object);

                var keyStoreDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ProjectVG.Infrastructure.Auth.IKeyStore));
                if (keyStoreDescriptor != null)
                {
                    services.Remove(keyStoreDescriptor);
                }
                services.AddSingleton(mockKeyStore.Object);
            });
            
            // Override environment to avoid WebSocket middleware issues
            builder.UseEnvironment("Test");
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var request = new RefreshRequest
        {
            RefreshToken = "test_refresh_token",
            ClientId = "unity-client"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/refresh", content);

        // Assert
        Console.WriteLine($"Refresh endpoint response status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Refresh endpoint response content: {responseContent}");
        
        // 엔드포인트가 접근 가능한지 확인 (실제 토큰이 없으므로 에러 응답이 정상)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                   response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                   response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var request = new RevokeRequest
        {
            Token = "test_token"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/revoke", content);

        // Assert
        Console.WriteLine($"Revoke endpoint response status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Revoke endpoint response content: {responseContent}");
        
        // OAuth2 스펙에 따라 revoke는 항상 200 OK 반환
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LogoutEndpoint_WithoutAuth_ShouldReturnError()
    {
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        Console.WriteLine($"Logout endpoint response status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Logout endpoint response content: {responseContent}");
        
        // Authorization 헤더가 없으므로 에러 응답 예상 (Unauthorized 또는 BadRequest)
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                   response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }
}
