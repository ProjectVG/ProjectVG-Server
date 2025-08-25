using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models;

namespace ProjectVG.Infrastructure.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IJwtProvider _jwtProvider;
        private readonly IRefreshTokenStorage _refreshTokenStorage;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IJwtProvider jwtProvider, IRefreshTokenStorage refreshTokenStorage, ILogger<TokenService> logger)
        {
            _jwtProvider = jwtProvider;
            _refreshTokenStorage = refreshTokenStorage;
            _logger = logger;
        }

        public async Task<TokenResponse> GenerateTokensAsync(Guid userId)
        {
            var accessToken = _jwtProvider.GenerateAccessToken(userId);
            var refreshToken = _jwtProvider.GenerateRefreshToken(userId);

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440);

            var stored = await _refreshTokenStorage.StoreRefreshTokenAsync(refreshToken, userId, refreshTokenExpiresAt);
            if (!stored)
            {
                _logger.LogError("Failed to store refresh token for user {UserId}", userId);
                throw new InvalidOperationException("Failed to store refresh token");
            }

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }

        public async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken)
        {
            var principal = _jwtProvider.ValidateToken(refreshToken);
            if (principal == null)
            {
                _logger.LogWarning("Invalid refresh token format");
                return null;
            }

            var tokenType = principal.FindFirst("token_type")?.Value;
            if (tokenType != "refresh")
            {
                _logger.LogWarning("Token is not a refresh token");
                return null;
            }

            var isValid = await _refreshTokenStorage.IsRefreshTokenValidAsync(refreshToken);
            if (!isValid)
            {
                _logger.LogWarning("Refresh token not found in storage");
                return null;
            }

            var userId = await _refreshTokenStorage.GetUserIdFromRefreshTokenAsync(refreshToken);
            if (!userId.HasValue)
            {
                _logger.LogWarning("User ID not found for refresh token");
                return null;
            }

            var newAccessToken = _jwtProvider.GenerateAccessToken(userId.Value);
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(1440)
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            return await _refreshTokenStorage.RemoveRefreshTokenAsync(refreshToken);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var principal = _jwtProvider.ValidateToken(refreshToken);
            if (principal == null)
            {
                return false;
            }

            var tokenType = principal.FindFirst("token_type")?.Value;
            if (tokenType != "refresh")
            {
                return false;
            }

            return await _refreshTokenStorage.IsRefreshTokenValidAsync(refreshToken);
        }

        public Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            var principal = _jwtProvider.ValidateToken(accessToken);
            if (principal == null)
            {
                return Task.FromResult(false);
            }

            var tokenType = principal.FindFirst("token_type")?.Value;
            return Task.FromResult(tokenType == "access");
        }

        public Task<Guid?> GetUserIdFromTokenAsync(string token)
        {
            var userId = _jwtProvider.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                return Task.FromResult<Guid?>(null);
            }
            return Task.FromResult<Guid?>(parsedUserId);
        }
    }
}
