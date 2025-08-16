using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

namespace ProjectVG.Infrastructure.Integrations.MemoryClient
{
    public class VectorMemoryClient : IMemoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<VectorMemoryClient> _logger;

        public VectorMemoryClient(HttpClient httpClient, ILogger<VectorMemoryClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> AddMemoryAsync(string collection, string text,  Dictionary<string, string>? metadata = null)
        {
            var request = new MemoryAddRequest {
                Text = text,
                Metadata = metadata ?? new Dictionary<string, string>(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                Collection = collection
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

        /// <summary>
        /// 지정한 컬렉션에서 쿼리에 대한 벡터 유사도 검색을 수행하고 상위 결과를 반환합니다.
        /// </summary>
        /// <param name="collection">검색 대상 컬렉션의 이름.</param>
        /// <param name="query">검색할 텍스트 쿼리.</param>
        /// <param name="topK">반환할 최대 결과 개수(기본값: 3).</param>
        /// <returns>
        /// 검색 결과의 리스트. 원격 메모리 저장소 호출이 실패하거나 예외가 발생하면 빈 리스트를 반환합니다.
        /// </returns>
        /// <remarks>
        /// 요청은 원격 /search 엔드포인트에 POST로 전송되며, 실패 시 예외를 던지지 않고 빈 리스트로 처리합니다.
        /// </remarks>
        public async Task<List<MemorySearchResult>> SearchAsync(string collection, string query, int topK = 3)
        {
            var request = new MemorySearchRequest {
                Query = query,
                TopK = topK,
                TimeWeight = 0.3f,
                ReferenceTime = DateTime.UtcNow.ToString("o"),
                Collection = collection
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

                _logger.LogInformation("[MemoryStore] Search 결과 {Count}개:", results?.Count ?? 0);
                if (results != null)
                {
                    foreach (var result in results) {
                        _logger.LogInformation(" - Text: {Text}, Score: {Score:F4}", result.Text, result.Score);
                    }
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