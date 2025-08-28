using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models;

namespace ProjectVG.Infrastructure.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IJwtProvider _jwtProvider;
        private readonly IRefreshTokenStorage _refreshTokenStorage;
        private readonly ILogger<TokenService> _logger;

        /// <summary>
        /// TokenService의 새 인스턴스를 생성하고 내부에서 사용할 JwtProvider, RefreshTokenStorage 및 Logger를 주입하여 초기화합니다.
        /// </summary>
        public TokenService(IJwtProvider jwtProvider, IRefreshTokenStorage refreshTokenStorage, ILogger<TokenService> logger)
        {
            _jwtProvider = jwtProvider;
            _refreshTokenStorage = refreshTokenStorage;
            _logger = logger;
        }

        /// <summary>
        /// 지정된 사용자 ID에 대해 액세스 토큰과 리프레시 토큰을 생성하고 리프레시 토큰을 저장한 뒤 토큰과 만료 시각을 반환합니다.
        /// </summary>
        /// <param name="userId">토큰을 발급할 대상 사용자의 고유 식별자(Guid).</param>
        /// <returns>
        /// 생성된 액세스 토큰과 리프레시 토큰, 각각의 만료 시각을 포함한 <see cref="TokenResponse"/>.
        /// 액세스 토큰 만료 시각은 생성 시점 기준 UTC +15분, 리프레시 토큰 만료 시각은 UTC +24시간으로 설정됩니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">리프레시 토큰을 저장하지 못했을 경우 발생합니다.</exception>
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

        /// <summary>
        /// 주어진 리프레시 토큰을 검증하고 유효하면 새로운 액세스 토큰을 발급하여 토큰 응답을 반환합니다.
        /// </summary>
        /// <param name="refreshToken">클라이언트가 제공한 리프레시 토큰 문자열.</param>
        /// <returns>
        /// 유효한 경우 새 액세스 토큰과 기존 리프레시 토큰 및 각각의 만료 시간을 담은 <see cref="TokenResponse"/>를 반환합니다.
        /// 토큰이 형식상 유효하지 않거나 타입이 "refresh"가 아니거나 저장소 검증에 실패하면 null을 반환합니다.
        /// </returns>
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

            // 새로운 Access Token만 생성 (Refresh Token은 기존 것 유지)
            var newAccessToken = _jwtProvider.GenerateAccessToken(userId.Value);
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            // 기존 Refresh Token의 만료 시간 조회
            var refreshTokenExpiresAt = await _refreshTokenStorage.GetRefreshTokenExpiresAtAsync(refreshToken);
            if (!refreshTokenExpiresAt.HasValue)
            {
                _logger.LogWarning("Refresh token expiration time not found");
                return null;
            }

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = refreshToken, // 기존 Refresh Token 유지
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt.Value
            };
        }

        /// <summary>
        /// 지정한 리프레시 토큰을 저장소에서 제거(해제)합니다.
        /// </summary>
        /// <param name="refreshToken">제거할 리프레시 토큰 문자열.</param>
        /// <returns>토큰이 존재하여 성공적으로 제거되면 true, 그렇지 않으면 false.</returns>
        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            return await _refreshTokenStorage.RemoveRefreshTokenAsync(refreshToken);
        }

        /// <summary>
        /// 주어진 문자열형 리프레시 토큰이 유효한 리프레시 토큰인지 검사합니다.
        /// </summary>
        /// <param name="refreshToken">검사할 JWT 리프레시 토큰 문자열.</param>
        /// <returns>토큰이 서명 및 클레임 검증에 통과하고 저장소에 유효한 토큰으로 존재하면 true, 그렇지 않으면 false를 반환합니다.</returns>
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

        /// <summary>
        /// 주어진 JWT가 유효한 액세스 토큰인지 검증합니다.
        /// </summary>
        /// <param name="accessToken">검사할 JWT 문자열(액세스 토큰).</param>
        /// <returns>토큰이 유효하고 내부 클레임 `token_type`이 `"access"`이면 true, 그렇지 않으면 false를 반환하는 비동기 작업.</returns>
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

        /// <summary>
        /// 전달된 JWT에서 사용자 ID를 추출하여 Guid로 반환합니다.
        /// </summary>
        /// <param name="token">확인할 JWT 문자열.</param>
        /// <returns>
        /// 토큰에서 추출된 사용자 ID(Guid). 토큰에 사용자 ID가 없거나 Guid로 변환할 수 없으면 null을 반환합니다.
        /// </returns>
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
