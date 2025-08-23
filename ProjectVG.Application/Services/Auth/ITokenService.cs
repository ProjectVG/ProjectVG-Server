using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokensAsync(Guid userId, string clientId, string? deviceId = null, string? ipAddress = null, string? userAgent = null);
    Task<Models.Auth.TokenValidationResult> ValidateAccessTokenAsync(string token);
    Task<string> GenerateAccessTokenAsync(Guid userId, string? scope = null);
}