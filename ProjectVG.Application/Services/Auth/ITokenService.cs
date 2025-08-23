using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokensAsync(Guid userId, string clientId, string? deviceId = null, string? ipAddress = null, string? userAgent = null);
    Task<TokenResponse> RefreshTokensAsync(string refreshToken, string clientId, string? ipAddress = null, string? userAgent = null);
    Task<bool> RevokeTokenAsync(string refreshToken, string clientId);
    Task<bool> RevokeAllUserTokensAsync(Guid userId, string? exceptTokenId = null);
    Task<bool> ValidateAccessTokenAsync(string accessToken);
    Task<TokenValidationResult> GetTokenClaimsAsync(string accessToken);
}
