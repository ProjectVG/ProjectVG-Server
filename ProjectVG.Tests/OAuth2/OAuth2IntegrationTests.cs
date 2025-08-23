using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Xunit;
using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Tests.OAuth2;

public class OAuth2IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OAuth2IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Authorize_GET_ShouldReturnTestPage()
    {
        // Act
        var response = await _client.GetAsync("/oauth2/authorize-test");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("OAuth2 Authorization endpoint is working", content);
    }

    [Fact]
    public async Task Authorize_GET_WithValidParameters_ShouldReturnCode()
    {
        // Arrange
        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = "unity-client",
            ["redirect_uri"] = "http://localhost:3000/callback",
            ["scope"] = "openid profile",
            ["state"] = "test-state",
            ["code_challenge"] = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            ["code_challenge_method"] = "S256"
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        // Act
        var response = await _client.GetAsync($"/oauth2/authorize?{queryString}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(result.TryGetProperty("code", out var codeElement));
        Assert.True(result.TryGetProperty("state", out var stateElement));
        Assert.Equal("test-state", stateElement.GetString());
        Assert.NotNull(codeElement.GetString());
    }

    [Fact]
    public async Task Authorize_GET_WithInvalidClient_ShouldReturnError()
    {
        // Arrange
        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = "invalid-client",
            ["state"] = "test-state",
            ["code_challenge"] = "test-challenge",
            ["code_challenge_method"] = "S256"
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        // Act
        var response = await _client.GetAsync($"/oauth2/authorize?{queryString}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuth2ErrorResponse>(content);
        
        Assert.Equal("invalid_client", result?.Error);
    }

    [Fact]
    public async Task Token_POST_WithInvalidGrantType_ShouldReturnError()
    {
        // Arrange
        var formData = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "invalid_grant"),
            new("code", "test-code"),
            new("client_id", "unity-client"),
            new("code_verifier", "test-verifier")
        };

        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/oauth2/token", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuth2ErrorResponse>(responseContent);
        
        Assert.Equal("unsupported_grant_type", result?.Error);
    }
}
