using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Infrastructure.Auth.Models;
using ProjectVG.Infrastructure.Persistence.Repositories.Auth;
using System.Security.Cryptography;
using System.Text;

namespace ProjectVG.Application.Services.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        ILogger<RefreshTokenService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _repository = repository;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<RefreshToken> CreateAsync(Guid userId, string clientId, string? deviceId, DateTime expiresAt, string? ipAddress = null, string? userAgent = null)
    {
        var tokenValue = GenerateSecureToken();
        var tokenHash = HashToken(tokenValue);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientId = clientId,
            DeviceId = deviceId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            LastUsedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _repository.CreateAsync(refreshToken);
        
        _logger.LogInformation("Created refresh token for user {UserId}, client {ClientId}, device {DeviceId}", 
            userId, clientId, deviceId);

        // 반환용으로 새로운 객체를 생성하여 실제 토큰 값을 설정
        var result = new RefreshToken
        {
            Id = refreshToken.Id,
            UserId = refreshToken.UserId,
            ClientId = refreshToken.ClientId,
            DeviceId = refreshToken.DeviceId,
            TokenHash = tokenValue, // 실제 토큰 값 (해시되지 않은)
            ExpiresAt = refreshToken.ExpiresAt,
            LastUsedAt = refreshToken.LastUsedAt,
            IpAddress = refreshToken.IpAddress,
            UserAgent = refreshToken.UserAgent,
            RevokedAt = refreshToken.RevokedAt,
            ReplacedByTokenId = refreshToken.ReplacedByTokenId
        };
        
        return result;
    }

    public async Task<RefreshToken?> GetActiveTokenAsync(string tokenHash)
    {
        var hashedToken = HashToken(tokenHash);
        var token = await _repository.GetByTokenHashAsync(hashedToken);

        if (token == null || !token.IsActive)
        {
            return null;
        }

        // 토큰 사용 기록 업데이트
        token.LastUsedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(token);

        return token;
    }

    public async Task<RefreshToken?> RotateTokenAsync(RefreshToken currentToken, string? ipAddress = null, string? userAgent = null)
    {
        if (!currentToken.IsActive)
        {
            _logger.LogWarning("Attempted to rotate inactive token {TokenId}", currentToken.Id);
            return null;
        }

        // 새 토큰 생성
        var newToken = await CreateAsync(
            currentToken.UserId, 
            currentToken.ClientId, 
            currentToken.DeviceId, 
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays), 
            ipAddress, 
            userAgent);

        // 기존 토큰 무효화 및 교체 관계 설정
        currentToken.RevokedAt = DateTime.UtcNow;
        currentToken.ReplacedByTokenId = newToken.Id;
        await _repository.UpdateAsync(currentToken);

        _logger.LogInformation("Rotated refresh token {OldTokenId} to {NewTokenId} for user {UserId}", 
            currentToken.Id, newToken.Id, currentToken.UserId);

        return newToken;
    }

    public async Task<bool> RevokeTokenAsync(Guid tokenId)
    {
        var token = await _repository.GetByIdAsync(tokenId);
        if (token == null)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(token);

        _logger.LogInformation("Revoked refresh token {TokenId} for user {UserId}", tokenId, token.UserId);
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, Guid? exceptTokenId = null)
    {
        var tokens = await _repository.GetActiveTokensByUserIdAsync(userId);
        var tokensToRevoke = exceptTokenId.HasValue 
            ? tokens.Where(t => t.Id != exceptTokenId.Value).ToList()
            : tokens.ToList();

        foreach (var token in tokensToRevoke)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(token);
        }

        _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", tokensToRevoke.Count, userId);
        return tokensToRevoke.Any();
    }

    public async Task<bool> IsTokenValidAsync(string tokenHash)
    {
        var hashedToken = HashToken(tokenHash);
        var token = await _repository.GetByTokenHashAsync(hashedToken);
        return token?.IsActive == true;
    }

    public async Task<bool> DetectTokenReuseAsync(string tokenHash)
    {
        var hashedToken = HashToken(tokenHash);
        var token = await _repository.GetByTokenHashAsync(hashedToken);

        if (token == null)
        {
            return false;
        }

        // 이미 폐기된 토큰이 다시 사용되는 경우 = 재사용 감지
        if (token.IsRevoked)
        {
            _logger.LogWarning("Token reuse detected for token {TokenId}, user {UserId}. Revoking token family.", 
                token.Id, token.UserId);

            // 토큰 계보 전체 무효화
            await RevokeTokenFamilyAsync(token);
            return true;
        }

        return false;
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _repository.GetExpiredTokensAsync(DateTime.UtcNow.AddDays(-30)); // 30일 이전 만료 토큰
        
        foreach (var token in expiredTokens)
        {
            await _repository.DeleteAsync(token.Id);
        }

        if (expiredTokens.Any())
        {
            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count());
        }
    }

    private async Task RevokeTokenFamilyAsync(RefreshToken compromisedToken)
    {
        // 해당 토큰과 연결된 모든 토큰 찾기 (교체 관계 추적)
        var familyTokens = await _repository.GetTokenFamilyAsync(compromisedToken.Id);
        
        foreach (var token in familyTokens)
        {
            if (token.IsActive)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(token);
            }
        }

        _logger.LogWarning("Revoked token family of {Count} tokens due to reuse detection", familyTokens.Count());
    }

    private static string GenerateSecureToken()
    {
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
