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

        /// <summary>
        /// 텍스트를 자동 분류(Episodic/Semantic)하여 삽입한다.
        /// </summary>
        public async Task<MemoryInsertResponse> InsertAutoAsync(MemoryInsertRequest request)
        {
            var payload = new Dictionary<string, object?>
            {
                { "text", request.Text },
                { "user_id", request.UserId },
                { "speaker", request.Speaker },
                { "emotion", request.Emotion is null ? null : new Dictionary<string, object?>
                    {
                        { "valence", request.Emotion.Valence },
                        { "arousal", request.Emotion.Arousal },
                        { "labels", request.Emotion.Labels },
                        { "intensity", request.Emotion.Intensity }
                    }
                },
                { "context", request.Context },
                { "importance_score", request.ImportanceScore }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/api/memory", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] InsertAuto 실패: {StatusCode}", response.StatusCode);
                    var detail = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Memory insert failed: {(int)response.StatusCode} - {detail}");
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapInsertResponse(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] InsertAuto 예외 발생");
                throw;
            }
        }

        /// <summary>
        /// Episodic 메모리를 수동 타입으로 삽입한다.
        /// </summary>
        public async Task<MemoryInsertResponse> InsertEpisodicAsync(EpisodicInsertRequest request)
        {
            var payload = new Dictionary<string, object?>
            {
                { "text", request.Text },
                { "user_id", request.UserId },
                { "speaker", request.Speaker },
                { "emotion", request.Emotion is null ? null : new Dictionary<string, object?>
                    {
                        { "valence", request.Emotion.Valence },
                        { "arousal", request.Emotion.Arousal },
                        { "labels", request.Emotion.Labels },
                        { "intensity", request.Emotion.Intensity }
                    }
                },
                { "context", request.Context },
                { "importance_score", request.ImportanceScore }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/api/memory/episodic", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] InsertEpisodic 실패: {StatusCode}", response.StatusCode);
                    var detail = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Episodic insert failed: {(int)response.StatusCode} - {detail}");
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapInsertResponse(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] InsertEpisodic 예외 발생");
                throw;
            }
        }

        /// <summary>
        /// Semantic 메모리를 수동 타입으로 삽입한다.
        /// </summary>
        public async Task<MemoryInsertResponse> InsertSemanticAsync(SemanticInsertRequest request)
        {
            var payload = new Dictionary<string, object?>
            {
                { "text", request.Text },
                { "user_id", request.UserId },
                { "fact_type", request.FactType },
                { "confidence_score", request.ConfidenceScore },
                { "importance_score", request.ImportanceScore },
                { "last_updated", request.LastUpdated?.ToString("o") }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/api/memory/semantic", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] InsertSemantic 실패: {StatusCode}", response.StatusCode);
                    var detail = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Semantic insert failed: {(int)response.StatusCode} - {detail}");
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapInsertResponse(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] InsertSemantic 예외 발생");
                throw;
            }
        }

        /// <summary>
        /// 특정 타입(Episodic/Semantic)의 메모리에서 검색한다.
        /// </summary>
        public async Task<List<MemorySearchResult>> SearchAsync(MemoryType memoryType, string query, string userId, int limit = 10, double similarityThreshold = 0.0)
        {
            var typeSegment = memoryType == MemoryType.Episodic ? "episodic" : "semantic";
            var uri = $"/api/memory/{typeSegment}/search?query={Uri.EscapeDataString(query)}&limit={limit}";
            if (similarityThreshold > 0)
            {
                uri += $"&similarity_threshold={similarityThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("X-User-ID", userId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] Search 실패: {StatusCode}", response.StatusCode);
                    return new List<MemorySearchResult>();
                }

                var body = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<MemorySearchResult>>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return results ?? new List<MemorySearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] Search 예외 발생");
                return new List<MemorySearchResult>();
            }
        }

        /// <summary>
        /// 사용자 메모리 통계를 조회한다.
        /// </summary>
        public async Task<UserStatsResponse> GetUserStatsAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/user/{Uri.EscapeDataString(userId)}/stats");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] GetUserStats 실패: {StatusCode}", response.StatusCode);
                    return new UserStatsResponse { UserId = userId };
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapUserStats(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] GetUserStats 예외 발생");
                return new UserStatsResponse { UserId = userId };
            }
        }

        /// <summary>
        /// 시스템 전체 통계를 조회한다.
        /// </summary>
        public async Task<SystemStatsResponse> GetSystemStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/system/stats");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] GetSystemStats 실패: {StatusCode}", response.StatusCode);
                    return new SystemStatsResponse();
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapSystemStats(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] GetSystemStats 예외 발생");
                return new SystemStatsResponse();
            }
        }

        /// <summary>
        /// 여러 메모리 타입에서 동시 검색한다.
        /// </summary>
        public async Task<MultiSearchResponse> SearchMultiAsync(string query, string userId, int limit = 10, double similarityThreshold = 0.0)
        {
            var uri = $"/api/memory/search/multi?query={Uri.EscapeDataString(query)}&limit={limit}";
            if (similarityThreshold > 0)
            {
                uri += $"&similarity_threshold={similarityThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("X-User-ID", userId);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MemoryClient] SearchMulti 실패: {StatusCode}", response.StatusCode);
                    return new MultiSearchResponse { Query = query };
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return MapMultiSearchResponse(doc.RootElement, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MemoryClient] SearchMulti 예외 발생");
                return new MultiSearchResponse { Query = query };
            }
        }

        private static MemoryInsertResponse MapInsertResponse(JsonElement root)
        {
            var res = new MemoryInsertResponse();

            if (root.TryGetProperty("id", out var idProp))
                res.Id = idProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("memory_type", out var mtProp))
                res.MemoryType = mtProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("collection_name", out var cnProp))
                res.CollectionName = cnProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("user_id", out var uidProp))
                res.UserId = uidProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(tsProp.GetString(), out var dto))
                    res.Timestamp = dto;
            }
            if (root.TryGetProperty("classification_confidence", out var ccProp) && ccProp.ValueKind == JsonValueKind.Number)
                res.ClassificationConfidence = ccProp.GetDouble();
            if (root.TryGetProperty("classification_explanation", out var ceProp))
                res.ClassificationExplanation = ceProp.GetString();

            return res;
        }

        private static UserStatsResponse MapUserStats(JsonElement root)
        {
            var res = new UserStatsResponse();
            if (root.TryGetProperty("user_id", out var uid)) res.UserId = uid.GetString() ?? string.Empty;
            if (root.TryGetProperty("total_memories", out var tm) && tm.ValueKind == JsonValueKind.Number) res.TotalMemories = tm.GetInt32();
            if (root.TryGetProperty("episodic_count", out var ec) && ec.ValueKind == JsonValueKind.Number) res.EpisodicCount = ec.GetInt32();
            if (root.TryGetProperty("semantic_count", out var sc) && sc.ValueKind == JsonValueKind.Number) res.SemanticCount = sc.GetInt32();
            if (root.TryGetProperty("oldest_memory", out var om) && om.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(om.GetString(), out var dto1)) res.OldestMemory = dto1;
            if (root.TryGetProperty("newest_memory", out var nm) && nm.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(nm.GetString(), out var dto2)) res.NewestMemory = dto2;
            if (root.TryGetProperty("daily_average", out var da) && da.ValueKind == JsonValueKind.Number) res.DailyAverage = da.GetDouble();
            if (root.TryGetProperty("most_active_day", out var mad)) res.MostActiveDay = mad.GetString();
            return res;
        }

        private static SystemStatsResponse MapSystemStats(JsonElement root)
        {
            var res = new SystemStatsResponse();
            if (root.TryGetProperty("total_users", out var tu) && tu.ValueKind == JsonValueKind.Number) res.TotalUsers = tu.GetInt32();
            if (root.TryGetProperty("total_memories", out var tm) && tm.ValueKind == JsonValueKind.Number) res.TotalMemories = tm.GetInt64();
            if (root.TryGetProperty("uptime_since", out var up) && up.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(up.GetString(), out var dto)) res.UptimeSince = dto;
            return res;
        }

        private static MultiSearchResponse MapMultiSearchResponse(JsonElement root, string query)
        {
            var res = new MultiSearchResponse { Query = query };
            
            if (root.TryGetProperty("episodic_results", out var episodicProp) && episodicProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in episodicProp.EnumerateArray())
                {
                    var result = new MemorySearchResult();
                    if (item.TryGetProperty("text", out var textProp)) result.Text = textProp.GetString() ?? string.Empty;
                    if (item.TryGetProperty("score", out var scoreProp) && scoreProp.ValueKind == JsonValueKind.Number) result.Score = scoreProp.GetSingle();
                    res.EpisodicResults.Add(result);
                }
            }
            
            if (root.TryGetProperty("semantic_results", out var semanticProp) && semanticProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in semanticProp.EnumerateArray())
                {
                    var result = new MemorySearchResult();
                    if (item.TryGetProperty("text", out var textProp)) result.Text = textProp.GetString() ?? string.Empty;
                    if (item.TryGetProperty("score", out var scoreProp) && scoreProp.ValueKind == JsonValueKind.Number) result.Score = scoreProp.GetSingle();
                    res.SemanticResults.Add(result);
                }
            }
            
            if (root.TryGetProperty("total_results", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number) 
                res.TotalResults = totalProp.GetInt32();
            else
                res.TotalResults = res.EpisodicResults.Count + res.SemanticResults.Count;
                
            return res;
        }
    }
} 