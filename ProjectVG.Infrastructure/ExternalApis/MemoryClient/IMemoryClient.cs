using ProjectVG.Infrastructure.ExternalApis.MemoryClient.Models;

namespace ProjectVG.Infrastructure.ExternalApis.MemoryClient
{
    public interface IMemoryClient
    {
        Task<bool> AddMemoryAsync(string text, Dictionary<string, string>? metadata = null);
        Task<List<MemorySearchResult>> SearchAsync(string query, int topK = 3);
    }
} 