using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.Server;
using ProjectVG.Domain.Services.Server;
using StackExchange.Redis;
using System.Text.Json;

namespace ProjectVG.Infrastructure.Services.Server
{
    public class RedisServerRegistrationService : IServerRegistrationService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisServerRegistrationService> _logger;
        private readonly string _serverId;

        private const string SERVER_KEY_PREFIX = "server:";
        private const string USER_SERVER_KEY_PREFIX = "user:server:";
        private const string ACTIVE_SERVERS_SET = "servers:active";

        private static readonly TimeSpan SERVER_TTL = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan USER_SERVER_TTL = TimeSpan.FromMinutes(35); // 세션보다 5분 더 길게

        public RedisServerRegistrationService(
            IConnectionMultiplexer redis,
            ILogger<RedisServerRegistrationService> logger)
        {
            _redis = redis;
            _database = redis.GetDatabase();
            _logger = logger;
            _serverId = GenerateServerId();
        }

        public async Task RegisterServerAsync()
        {
            try
            {
                var serverInfo = new ServerInfo
                {
                    ServerId = _serverId,
                    StartedAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    Status = "healthy",
                    ActiveConnections = 0
                };

                var serverKey = GetServerKey(_serverId);
                var json = JsonSerializer.Serialize(serverInfo);

                // 서버 정보 저장 (TTL 5분)
                await _database.StringSetAsync(serverKey, json, SERVER_TTL);

                // 활성 서버 세트에 추가
                await _database.SetAddAsync(ACTIVE_SERVERS_SET, _serverId);

                _logger.LogInformation("서버 등록 완료: {ServerId}, TTL={TTL}분", _serverId, SERVER_TTL.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 등록 실패: {ServerId}", _serverId);
                throw;
            }
        }

        public async Task UnregisterServerAsync()
        {
            try
            {
                var serverKey = GetServerKey(_serverId);

                // 서버 정보 삭제
                await _database.KeyDeleteAsync(serverKey);

                // 활성 서버 세트에서 제거
                await _database.SetRemoveAsync(ACTIVE_SERVERS_SET, _serverId);

                // 이 서버에 연결된 모든 사용자 매핑 정리
                await CleanupServerUserMappingsAsync(_serverId);

                _logger.LogInformation("서버 등록 해제 완료: {ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 등록 해제 실패: {ServerId}", _serverId);
                throw;
            }
        }

        public async Task SendHeartbeatAsync()
        {
            try
            {
                var serverKey = GetServerKey(_serverId);
                var existingJson = await _database.StringGetAsync(serverKey);

                ServerInfo serverInfo;
                if (existingJson.HasValue)
                {
                    serverInfo = JsonSerializer.Deserialize<ServerInfo>(existingJson!) ?? new ServerInfo();
                }
                else
                {
                    serverInfo = new ServerInfo
                    {
                        ServerId = _serverId,
                        StartedAt = DateTime.UtcNow,
                        Status = "healthy"
                    };
                }

                serverInfo.LastHeartbeat = DateTime.UtcNow;
                serverInfo.ServerId = _serverId; // 확실히 설정

                var json = JsonSerializer.Serialize(serverInfo);
                await _database.StringSetAsync(serverKey, json, SERVER_TTL);

                _logger.LogDebug("하트비트 전송: {ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "하트비트 전송 실패: {ServerId}", _serverId);
                throw;
            }
        }

        public string GetServerId()
        {
            return _serverId;
        }

        public async Task<IEnumerable<ServerInfo>> GetActiveServersAsync()
        {
            try
            {
                var serverIds = await _database.SetMembersAsync(ACTIVE_SERVERS_SET);
                var servers = new List<ServerInfo>();

                foreach (var serverId in serverIds)
                {
                    var serverKey = GetServerKey(serverId!);
                    var json = await _database.StringGetAsync(serverKey);

                    if (json.HasValue)
                    {
                        var server = JsonSerializer.Deserialize<ServerInfo>(json!);
                        if (server != null)
                        {
                            servers.Add(server);
                        }
                    }
                }

                _logger.LogDebug("활성 서버 조회: {Count}개", servers.Count);
                return servers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "활성 서버 조회 실패");
                return Enumerable.Empty<ServerInfo>();
            }
        }

        public async Task<string?> GetUserServerAsync(string userId)
        {
            try
            {
                var userServerKey = GetUserServerKey(userId);
                var serverId = await _database.StringGetAsync(userServerKey);

                if (!serverId.HasValue)
                {
                    _logger.LogDebug("사용자 서버 매핑을 찾을 수 없음: {UserId}", userId);
                    return null;
                }

                _logger.LogDebug("사용자 서버 조회: {UserId} -> {ServerId}", userId, serverId);
                return serverId!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 서버 조회 실패: {UserId}", userId);
                return null;
            }
        }

        public async Task SetUserServerAsync(string userId, string serverId)
        {
            try
            {
                var userServerKey = GetUserServerKey(userId);
                await _database.StringSetAsync(userServerKey, serverId, USER_SERVER_TTL);

                _logger.LogDebug("사용자 서버 매핑 설정: {UserId} -> {ServerId}, TTL={TTL}분",
                    userId, serverId, USER_SERVER_TTL.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 서버 매핑 설정 실패: {UserId} -> {ServerId}", userId, serverId);
                throw;
            }
        }

        public async Task RemoveUserServerAsync(string userId)
        {
            try
            {
                var userServerKey = GetUserServerKey(userId);
                var deleted = await _database.KeyDeleteAsync(userServerKey);

                if (deleted)
                {
                    _logger.LogDebug("사용자 서버 매핑 제거: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("제거할 사용자 서버 매핑을 찾을 수 없음: {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 서버 매핑 제거 실패: {UserId}", userId);
                throw;
            }
        }

        public async Task CleanupOfflineServersAsync()
        {
            try
            {
                var allServerIds = await _database.SetMembersAsync(ACTIVE_SERVERS_SET);
                var offlineServers = new List<string>();

                foreach (var serverId in allServerIds)
                {
                    var serverKey = GetServerKey(serverId!);
                    var exists = await _database.KeyExistsAsync(serverKey);

                    if (!exists)
                    {
                        offlineServers.Add(serverId!);
                    }
                }

                // 오프라인 서버들을 활성 세트에서 제거
                foreach (var offlineServerId in offlineServers)
                {
                    await _database.SetRemoveAsync(ACTIVE_SERVERS_SET, offlineServerId);
                    await CleanupServerUserMappingsAsync(offlineServerId);

                    _logger.LogInformation("오프라인 서버 정리: {ServerId}", offlineServerId);
                }

                if (offlineServers.Any())
                {
                    _logger.LogInformation("오프라인 서버 정리 완료: {Count}개", offlineServers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오프라인 서버 정리 실패");
            }
        }

        private async Task CleanupServerUserMappingsAsync(string serverId)
        {
            try
            {
                // 해당 서버에 연결된 사용자 매핑들을 찾아서 정리
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var userServerKeys = server.Keys(pattern: USER_SERVER_KEY_PREFIX + "*");

                var cleanupTasks = new List<Task>();
                foreach (var key in userServerKeys)
                {
                    cleanupTasks.Add(CleanupUserMappingIfMatchesServer(key, serverId));
                }

                await Task.WhenAll(cleanupTasks);
                _logger.LogDebug("서버 사용자 매핑 정리 완료: {ServerId}", serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 사용자 매핑 정리 실패: {ServerId}", serverId);
            }
        }

        private async Task CleanupUserMappingIfMatchesServer(RedisKey userServerKey, string targetServerId)
        {
            try
            {
                var mappedServerId = await _database.StringGetAsync(userServerKey);
                if (mappedServerId.HasValue && mappedServerId == targetServerId)
                {
                    await _database.KeyDeleteAsync(userServerKey);
                    var userId = ExtractUserIdFromKey(userServerKey!);
                    _logger.LogDebug("유령 사용자 매핑 정리: {UserId} -> {ServerId}", userId, targetServerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 매핑 정리 실패: {Key}", userServerKey);
            }
        }

        private string ExtractUserIdFromKey(string key)
        {
            return key.Substring(USER_SERVER_KEY_PREFIX.Length);
        }

        private string GetServerKey(string serverId)
        {
            return SERVER_KEY_PREFIX + serverId;
        }

        private string GetUserServerKey(string userId)
        {
            return USER_SERVER_KEY_PREFIX + userId;
        }

        private string GenerateServerId()
        {
            // 환경 변수에서 서버 ID를 가져오거나 자동 생성
            var envServerId = Environment.GetEnvironmentVariable("SERVER_ID");
            if (!string.IsNullOrEmpty(envServerId))
            {
                return envServerId;
            }

            // 호스트명 + 프로세스 ID + 타임스탬프로 고유 ID 생성
            var hostname = Environment.MachineName;
            var processId = Environment.ProcessId;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return $"api-server-{hostname}-{processId}-{timestamp}";
        }
    }
}