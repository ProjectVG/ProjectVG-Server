using ProjectVG.Domain.Entities.Session;

namespace ProjectVG.Infrastructure.Repositories.InMemory
{
    public class InMemoryClientSessionRepository : IClientSessionRepository
    {
        private readonly Dictionary<string, ClientSession> _sessions = new();

        public Task<ClientSession?> GetBySessionIdAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task<ClientSession?> GetByUserIdAsync(Guid userId)
        {
            var session = _sessions.Values.FirstOrDefault(s => s.UserId == userId);
            return Task.FromResult(session);
        }

        public Task<ClientSession?> GetByConnectionIdAsync(string connectionId)
        {
            var session = _sessions.Values.FirstOrDefault(s => s.ConnectionId == connectionId);
            return Task.FromResult(session);
        }

        public Task<ClientSession> CreateAsync(ClientSession session)
        {
            if (string.IsNullOrEmpty(session.SessionId))
            {
                session.SessionId = $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
            }
            
            _sessions[session.SessionId] = session;
            return Task.FromResult(session);
        }

        public Task<ClientSession> UpdateAsync(ClientSession session)
        {
            if (_sessions.ContainsKey(session.SessionId))
            {
                _sessions[session.SessionId] = session;
            }
            return Task.FromResult(session);
        }

        public Task DeleteAsync(string sessionId)
        {
            _sessions.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task DeleteByUserIdAsync(Guid userId)
        {
            var sessionsToRemove = _sessions.Values.Where(s => s.UserId == userId).ToList();
            foreach (var session in sessionsToRemove)
            {
                _sessions.Remove(session.SessionId);
            }
            return Task.CompletedTask;
        }
    }
} 