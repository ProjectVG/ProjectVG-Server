using ProjectVG.Infrastructure.Services.Session;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Repositories.InMemory
{
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly Dictionary<string, ClientConnection> _sessions = new();
        private readonly ILogger<InMemorySessionRepository> _logger;

        public InMemorySessionRepository(ILogger<InMemorySessionRepository> logger)
        {
            _logger = logger;
        }

        public Task<ClientConnection?> GetAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var connection);
            return Task.FromResult(connection);
        }

        public Task<IEnumerable<ClientConnection>> GetAllAsync()
        {
            return Task.FromResult(_sessions.Values.AsEnumerable());
        }

        public Task<ClientConnection> CreateAsync(ClientConnection connection)
        {
            if (_sessions.ContainsKey(connection.SessionId))
            {
                throw new InvalidOperationException($"세션 ID '{connection.SessionId}'가 이미 존재합니다.");
            }

            _sessions[connection.SessionId] = connection;
            _logger.LogDebug("세션을 생성했습니다: {SessionId}", connection.SessionId);
            
            return Task.FromResult(connection);
        }

        public Task<ClientConnection> UpdateAsync(ClientConnection connection)
        {
            if (!_sessions.ContainsKey(connection.SessionId))
            {
                throw new KeyNotFoundException($"세션 ID {connection.SessionId}를 찾을 수 없습니다.");
            }

            _sessions[connection.SessionId] = connection;
            _logger.LogDebug("세션을 수정했습니다: {SessionId}", connection.SessionId);
            
            return Task.FromResult(connection);
        }

        public Task DeleteAsync(string sessionId)
        {
            if (_sessions.Remove(sessionId))
            {
                _logger.LogDebug("세션을 삭제했습니다: {SessionId}", sessionId);
            }
            else
            {
                _logger.LogWarning("세션 ID {SessionId}를 삭제하려 했지만 세션을 찾을 수 없습니다", sessionId);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string sessionId)
        {
            return Task.FromResult(_sessions.ContainsKey(sessionId));
        }

        public Task<int> GetActiveSessionCountAsync()
        {
            return Task.FromResult(_sessions.Count);
        }

        public Task<IEnumerable<ClientConnection>> GetSessionsByUserIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(Enumerable.Empty<ClientConnection>());
            }

            var userSessions = _sessions.Values
                .Where(s => s.UserId == userId)
                .AsEnumerable();

            return Task.FromResult(userSessions);
        }
    }
} 