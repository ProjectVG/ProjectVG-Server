using System.Text;
using System.Text.Json;
using MainAPI_Server.Models.External.MemorySrore;

namespace MainAPI_Server.Clients
{
    public class VectorMemoryClient : IMemoryStoreClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<VectorMemoryClient> _logger;

        public VectorMemoryClient(HttpClient httpClient, ILogger<VectorMemoryClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> AddMemoryAsync(string text, Dictionary<string, string>? metadata = null)
        {
            var request = new MemoryAddRequest {
                Text = text,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try {
                var response = await _httpClient.PostAsync("/insert", content);
                if (!response.IsSuccessStatusCode) {
                    _logger.LogWarning("[MemoryStore] AddMemory 실패: {StatusCode}", response.StatusCode);
                    return false;
                }

                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[MemoryStore] AddMemory 예외 발생");
                return false;
            }
        }

        public async Task<List<MemorySearchResult>> SearchAsync(string query, int topK = 3)
        {
            var request = new MemorySearchRequest {
                Query = query,
                TopK = topK
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try {
                var response = await _httpClient.PostAsync("/search", content);
                if (!response.IsSuccessStatusCode) {
                    _logger.LogWarning("[MemoryStore] Search 실패: {StatusCode}", response.StatusCode);
                    return new List<MemorySearchResult>();
                }

                var body = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<MemorySearchResult>>(body, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("[MemoryStore] Search 결과 {Count}개:", results.Count);
                foreach (var result in results) {
                    _logger.LogInformation(" - Text: {Text}, Score: {Score:F4}", result.Text, result.Score);
                }

                return results ?? new List<MemorySearchResult>();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[MemoryStore] Search 예외 발생");
                return new List<MemorySearchResult>();
            }
        }
    }
}
