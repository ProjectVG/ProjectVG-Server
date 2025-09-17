using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Services.Session;
using ProjectVG.Common.Models.Session;
using StackExchange.Redis;
using System.Text.Json;

namespace ProjectVG.Infrastructure.Session
{
    /// <summary>
    /// Redis 기반 분산 세션 관리자 구현
    /// </summary>
    public class RedisDistributedSessionManager : IDistributedSessionManager
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisDistributedSessionManager> _logger;

        // Redis Key Patterns
        private const string SESSION_KEY_PREFIX = "session:user:";           // session:user:{userId} -> SessionInfo
        private const string SERVER_USERS_KEY_PREFIX = "server:users:";     // server:users:{serverId} -> Set of userIds
        private const string USER_SERVER_KEY_PREFIX = "user:server:";       // user:server:{userId} -> serverId
        private const string SERVER_INFO_KEY_PREFIX = "server:info:";       // server:info:{serverId} -> ServerInfo
        private const string ACTIVE_SERVERS_KEY = "servers:active";         // Set of active server IDs
        private const string SESSION_COUNT_KEY = "sessions:count";          // Total active session count

        public RedisDistributedSessionManager(
            IConnectionMultiplexer redis,
            ILogger<RedisDistributedSessionManager> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task RegisterSessionAsync(string userId, string serverId, SessionInfo sessionInfo)
        {
            try
            {
                var transaction = _database.CreateTransaction();

                // 1. 기존 세션이 있다면 정리
                var existingServerId = await GetUserServerAsync(userId);
                if (!string.IsNullOrEmpty(existingServerId))
                {
                    await UnregisterSessionInternalAsync(userId, existingServerId);
                }

                // 2. 새 세션 등록
                var sessionJson = JsonSerializer.Serialize(sessionInfo);
                _ = transaction.StringSetAsync(SESSION_KEY_PREFIX + userId, sessionJson, TimeSpan.FromHours(24));
                _ = transaction.SetAddAsync(SERVER_USERS_KEY_PREFIX + serverId, userId);
                _ = transaction.StringSetAsync(USER_SERVER_KEY_PREFIX + userId, serverId, TimeSpan.FromHours(24));
                _ = transaction.StringIncrementAsync(SESSION_COUNT_KEY);

                await transaction.ExecuteAsync();

                _logger.LogInformation("세션 등록 완료: UserId={UserId}, ServerId={ServerId}", userId, serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 등록 실패: UserId={UserId}, ServerId={ServerId}", userId, serverId);
                throw;
            }
        }

        public async Task UnregisterSessionAsync(string userId)
        {
            try
            {
                var serverId = await GetUserServerAsync(userId);
                if (string.IsNullOrEmpty(serverId))
                {
                    _logger.LogWarning("해제할 세션을 찾을 수 없음: UserId={UserId}", userId);
                    return;
                }

                await UnregisterSessionInternalAsync(userId, serverId);
                _logger.LogInformation("세션 해제 완료: UserId={UserId}, ServerId={ServerId}", userId, serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 해제 실패: UserId={UserId}", userId);
                throw;
            }
        }

        private async Task UnregisterSessionInternalAsync(string userId, string serverId)
        {
            var transaction = _database.CreateTransaction();

            _ = transaction.KeyDeleteAsync(SESSION_KEY_PREFIX + userId);
            _ = transaction.SetRemoveAsync(SERVER_USERS_KEY_PREFIX + serverId, userId);
            _ = transaction.KeyDeleteAsync(USER_SERVER_KEY_PREFIX + userId);
            _ = transaction.StringDecrementAsync(SESSION_COUNT_KEY);

            await transaction.ExecuteAsync();
        }

        public async Task<string?> GetUserServerAsync(string userId)
        {
            try
            {
                var serverId = await _database.StringGetAsync(USER_SERVER_KEY_PREFIX + userId);
                return serverId.HasValue ? serverId.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 서버 조회 실패: UserId={UserId}", userId);
                return null;
            }
        }

        public async Task<SessionInfo?> GetSessionInfoAsync(string userId)
        {
            try
            {
                var sessionJson = await _database.StringGetAsync(SESSION_KEY_PREFIX + userId);
                if (!sessionJson.HasValue)
                    return null;

                return JsonSerializer.Deserialize<SessionInfo>(sessionJson.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 정보 조회 실패: UserId={UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetServerUsersAsync(string serverId)
        {
            try
            {
                var userIds = await _database.SetMembersAsync(SERVER_USERS_KEY_PREFIX + serverId);
                return userIds.Select(userId => userId.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 사용자 목록 조회 실패: ServerId={ServerId}", serverId);
                return Enumerable.Empty<string>();
            }
        }

        public async Task<bool> IsSessionActiveAsync(string userId)
        {
            try
            {
                return await _database.KeyExistsAsync(SESSION_KEY_PREFIX + userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 활성 상태 확인 실패: UserId={UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetActiveSessionCountAsync()
        {
            try
            {
                var count = await _database.StringGetAsync(SESSION_COUNT_KEY);
                return count.HasValue ? (int)count : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "활성 세션 수 조회 실패");
                return 0;
            }
        }

        public async Task RegisterServerAsync(string serverId, ServerInfo serverInfo)
        {
            try
            {
                var transaction = _database.CreateTransaction();

                var serverJson = JsonSerializer.Serialize(serverInfo);
                transaction.StringSetAsync(SERVER_INFO_KEY_PREFIX + serverId, serverJson, TimeSpan.FromHours(1));
                transaction.SetAddAsync(ACTIVE_SERVERS_KEY, serverId);

                await transaction.ExecuteAsync();

                _logger.LogInformation("서버 등록 완료: ServerId={ServerId}, Host={Host}", serverId, serverInfo.HostName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 등록 실패: ServerId={ServerId}", serverId);
                throw;
            }
        }

        public async Task UnregisterServerAsync(string serverId)
        {
            try
            {
                // 1. 해당 서버의 모든 세션 정리
                var userIds = await GetServerUsersAsync(serverId);
                var tasks = userIds.Select(userId => UnregisterSessionInternalAsync(userId, serverId));
                await Task.WhenAll(tasks);

                // 2. 서버 정보 정리
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(SERVER_INFO_KEY_PREFIX + serverId);
                transaction.KeyDeleteAsync(SERVER_USERS_KEY_PREFIX + serverId);
                transaction.SetRemoveAsync(ACTIVE_SERVERS_KEY, serverId);

                await transaction.ExecuteAsync();

                _logger.LogInformation("서버 해제 완료: ServerId={ServerId}, 정리된 세션 수={SessionCount}",
                    serverId, userIds.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 해제 실패: ServerId={ServerId}", serverId);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActiveServersAsync()
        {
            try
            {
                var serverIds = await _database.SetMembersAsync(ACTIVE_SERVERS_KEY);
                return serverIds.Select(serverId => serverId.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "활성 서버 목록 조회 실패");
                return Enumerable.Empty<string>();
            }
        }

        public async Task CleanupExpiredSessionsAsync(TimeSpan expirationTime)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - expirationTime;
                var cleanedCount = 0;

                // 모든 활성 서버의 세션을 확인
                var serverIds = await GetActiveServersAsync();
                foreach (var serverId in serverIds)
                {
                    var userIds = await GetServerUsersAsync(serverId);
                    foreach (var userId in userIds)
                    {
                        var sessionInfo = await GetSessionInfoAsync(userId);
                        if (sessionInfo != null && sessionInfo.ConnectedAt < cutoffTime)
                        {
                            await UnregisterSessionAsync(userId);
                            cleanedCount++;
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("만료된 세션 정리 완료: {CleanedCount}개", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "만료된 세션 정리 실패");
            }
        }
    }
}