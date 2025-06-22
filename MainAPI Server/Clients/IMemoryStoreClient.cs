public interface IMemoryStoreClient
{
    Task<bool> AddMemoryAsync(string text, Dictionary<string, string>? metadata = null);
    Task<List<MemorySearchResult>> SearchAsync(string query, int topK = 3);
}
