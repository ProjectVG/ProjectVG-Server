using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProjectVG.Infrastructure.Auth
{
    public class RedisRefreshTokenStorage : IRefreshTokenStorage
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisRefreshTokenStorage> _logger;
        private const string KeyPrefix = "refresh_token:";

        /// <summary>
        /// Redis 기반으로 리프레시 토큰을 저장·관리하는 RedisRefreshTokenStorage 인스턴스를 생성합니다.
        /// </summary>
        public RedisRefreshTokenStorage(IConnectionMultiplexer redis, ILogger<RedisRefreshTokenStorage> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// 주어진 리프레시 토큰을 Redis에 사용자 ID와 만료 시간(TTL)으로 저장합니다.
        /// </summary>
        /// <param name="refreshToken">저장할 리프레시 토큰 문자열(키의 일부로 사용됨).</param>
        /// <param name="userId">토큰에 연관된 사용자 식별자(Guid)로, Redis 값으로 저장됩니다.</param>
        /// <param name="expiresAt">토큰의 만료 시각(UTC). 현재 UTC 시각과의 차이를 TTL로 설정합니다.</param>
        /// <returns>저장이 성공하면 true, 실패하거나 예외가 발생하면 false를 반환합니다.</returns>
        public async Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var expiry = expiresAt - DateTime.UtcNow;
                
                return await db.StringSetAsync(key, userId.ToString(), expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 주어진 리프레시 토큰에 연관된 사용자 ID를 Redis에서 조회하여 반환합니다.
        /// </summary>
        /// <param name="refreshToken">조회할 원시 리프레시 토큰 문자열(키에는 내부적으로 "refresh_token:" 접두사가 붙습니다).</param>
        /// <returns>
        /// 토큰이 존재하고 저장된 값이 유효한 GUID인 경우 해당 사용자 ID를 반환합니다.
        /// 토큰이 없거나 값이 GUID로 파싱되지 않거나 조회 중 오류가 발생하면 null을 반환합니다.
        /// </returns>
        public async Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var value = await db.StringGetAsync(key);
                
                if (value.HasValue && Guid.TryParse(value, out var userId))
                {
                    return userId;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID from refresh token");
                return null;
            }
        }

        /// <summary>
        /// 지정된 리프레시 토큰에 대응하는 Redis 키를 삭제합니다.
        /// </summary>
        /// <param name="refreshToken">삭제할 리프레시 토큰(키는 내부적으로 "refresh_token:{refreshToken}" 형태로 사용됩니다).</param>
        /// <returns>
        /// 삭제가 성공적으로 수행되어 키가 제거되면 true를 반환합니다. 키가 존재하지 않거나(또는 내부 오류 발생 시) false를 반환합니다.
        /// </returns>
        public async Task<bool> RemoveRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove refresh token");
                return false;
            }
        }

        /// <summary>
        /// 지정된 리프레시 토큰이 저장소에 존재하는지(유효한지) 확인합니다.
        /// </summary>
        /// <param name="refreshToken">접두사 없이 저장된 리프레시 토큰 문자열.</param>
        /// <returns>
        /// 토큰이 존재하면 true, 존재하지 않거나(만료 포함) 확인 중 오류가 발생하면 false를 반환합니다.
        /// </returns>
        public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check refresh token validity");
                return false;
            }
        }

        /// <summary>
        /// 지정된 리프레시 토큰의 만료 시각을 조회합니다.
        /// </summary>
        /// <param name="refreshToken">조회할 리프레시 토큰 문자열(저장된 키는 "refresh_token:{refreshToken}" 형식입니다).</param>
        /// <returns>
        /// 토큰이 Redis에 존재하고 TTL이 설정되어 있으면 해당 TTL을 현재 UTC 시간에 더한 만료 시각(DateTime)을 반환합니다.
        /// 존재하지 않거나 TTL이 없으면 null을 반환합니다.
        /// </returns>
        public async Task<DateTime?> GetRefreshTokenExpiresAtAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var timeToLive = await db.KeyTimeToLiveAsync(key);
                
                if (timeToLive.HasValue)
                {
                    return DateTime.UtcNow.Add(timeToLive.Value);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get refresh token expiration time");
                return null;
            }
        }
    }
}
