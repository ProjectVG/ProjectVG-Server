using ProjectVG.Infrastructure.Services.Session;

namespace ProjectVG.Infrastructure.Repositories
{
    public interface ISessionRepository
    {
        Task<ClientConnection?> GetAsync(string sessionId);
        Task<IEnumerable<ClientConnection>> GetAllAsync();
        Task<ClientConnection> CreateAsync(ClientConnection connection);
        Task<ClientConnection> UpdateAsync(ClientConnection connection);
        Task DeleteAsync(string sessionId);
        Task<bool> ExistsAsync(string sessionId);
        Task<int> GetActiveSessionCountAsync();
        Task<IEnumerable<ClientConnection>> GetSessionsByUserIdAsync(string? userId);
    }
} 