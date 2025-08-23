using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Cache;

namespace ProjectVG.Application.Services.Auth;

/// <summary>
/// 토큰 갱신 및 폐기를 위한 고수준 서비스 구현
/// </summary>
public class AuthTokenService : IAuthTokenService
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenService _tokenService;
    private readonly ITokenBlocklistService _blocklistService;
    private readonly ILogger<AuthTokenService> _logger;

    public AuthTokenService(
        IRefreshTokenService refreshTokenService,
        ITokenService tokenService,
        ITokenBlocklistService blocklistService,
        ILogger<AuthTokenService> logger)
    {
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
        _blocklistService = blocklistService;
        _logger = logger;
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // 1. 리프레시 토큰 해시 생성
            var tokenHash = ComputeTokenHash(refreshToken);
            
            // 2. 리프레시 토큰 유효성 검사
            var currentToken = await _refreshTokenService.GetActiveTokenAsync(tokenHash);
            if (currentToken == null)
            {
                _logger.LogWarning("Invalid refresh token provided");
                return new TokenResponse { IsSuccess = false, ErrorMessage = "Invalid refresh token" };
            }

            // 3. 토큰 재사용 감지
            if (await _refreshTokenService.DetectTokenReuseAsync(tokenHash))
            {
                _logger.LogWarning("Token reuse detected for user {UserId}, revoking all tokens", currentToken.UserId);
                await _refreshTokenService.RevokeAllUserTokensAsync(currentToken.UserId);
                return new TokenResponse { IsSuccess = false, ErrorMessage = "Token reuse detected" };
            }

            // 4. 토큰 로테이션 (새로운 리프레시 토큰 생성)
            var newRefreshToken = await _refreshTokenService.RotateTokenAsync(currentToken);
            if (newRefreshToken == null)
            {
                _logger.LogError("Failed to rotate refresh token for user {UserId}", currentToken.UserId);
                return new TokenResponse { IsSuccess = false, ErrorMessage = "Failed to rotate token" };
            }

            // 5. 새로운 액세스 토큰 생성
            var accessToken = await _tokenService.GenerateAccessTokenAsync(currentToken.UserId);

            _logger.LogInformation("Successfully refreshed tokens for user {UserId}", currentToken.UserId);

            return new TokenResponse
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.TokenHash, // 실제로는 원본 토큰 반환
                TokenType = "Bearer",
                ExpiresIn = 900 // 15분
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new TokenResponse { IsSuccess = false, ErrorMessage = "Internal server error" };
        }
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> RevokeTokenAsync(string token)
    {
        try
        {
            // 1. 액세스 토큰인지 확인
            var tokenValidation = await _tokenService.ValidateAccessTokenAsync(token);
            if (tokenValidation.IsValid && tokenValidation.UserId.HasValue)
            {
                // 액세스 토큰이면 블록리스트에 추가 (15분 TTL)
                await _blocklistService.BlockAccessTokenAsync(token, TimeSpan.FromMinutes(15));
                _logger.LogInformation("Access token blocked for user {UserId}", tokenValidation.UserId);
                return (true, null);
            }

            // 2. 리프레시 토큰인지 확인
            var tokenHash = ComputeTokenHash(token);
            var refreshToken = await _refreshTokenService.GetActiveTokenAsync(tokenHash);
            if (refreshToken != null)
            {
                // 리프레시 토큰이면 DB에서 무효화
                await _refreshTokenService.RevokeTokenAsync(refreshToken.Id);
                _logger.LogInformation("Refresh token revoked for user {UserId}", refreshToken.UserId);
                return (true, null);
            }

            // 3. 둘 다 아니면 무시 (OAuth2 스펙에 따라 에러 반환하지 않음)
            _logger.LogInformation("Token not found or already revoked");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return (false, "Internal server error");
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            await _refreshTokenService.RevokeAllUserTokensAsync(userId);
            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 토큰 해시 계산 (RefreshTokenService와 동일한 방식)
    /// </summary>
    private static string ComputeTokenHash(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
