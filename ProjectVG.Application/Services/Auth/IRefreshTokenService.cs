using ProjectVG.Domain.Entities.Auth;

namespace ProjectVG.Application.Services.Auth;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(Guid userId, string clientId, string? deviceId, DateTime expiresAt, string? ipAddress = null, string? userAgent = null);
    Task<RefreshToken?> GetActiveTokenAsync(string tokenHash);
    Task<RefreshToken?> RotateTokenAsync(RefreshToken currentToken, string? ipAddress = null, string? userAgent = null);
    Task<bool> RevokeTokenAsync(Guid tokenId);
    Task<bool> RevokeAllUserTokensAsync(Guid userId, Guid? exceptTokenId = null);
    Task<bool> IsTokenValidAsync(string tokenHash);
    Task<bool> DetectTokenReuseAsync(string tokenHash);
    Task CleanupExpiredTokensAsync();
}
