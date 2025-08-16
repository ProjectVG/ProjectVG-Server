using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

namespace ProjectVG.Infrastructure.Integrations.MemoryClient
{
    public interface IMemoryClient
    {
        Task<bool> AddMemoryAsync(string collection, string text,  Dictionary<string, string>? metadata = null);
        Task<List<MemorySearchResult>> SearchAsync(string collection, string query,  int topK = 3);
    }
} 