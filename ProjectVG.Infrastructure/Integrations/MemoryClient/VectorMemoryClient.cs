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

        /// <summary>
        /// VectorMemoryClient의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 생성된 인스턴스는 주입된 HttpClient로 메모리 서비스와의 HTTP 통신을 수행하고, ILogger로 로깅을 처리합니다.
        /// HttpClient와 ILogger는 DI 컨테이너에서 제공되어야 합니다.
        /// </remarks>
        public VectorMemoryClient(HttpClient httpClient, ILogger<VectorMemoryClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 텍스트를 자동 분류(Episodic/Semantic)하여 삽입한다.
        /// <summary>
        /// 자동 분류(episodic/semantic)로 메모리를 삽입하기 위해 메모리 API에 JSON 페이로드를 POST하고 응답을 MemoryInsertResponse로 매핑합니다.
        /// </summary>
        /// <param name="request">삽입할 메모리의 내용(text, userId, speaker, 선택적 emotion, context, importanceScore)을 포함하는 요청 객체.</param>
        /// <returns>서버가 반환한 삽입 결과를 포함하는 MemoryInsertResponse.</returns>
        /// <exception cref="HttpRequestException">HTTP 응답이 성공(2xx)이 아닌 경우, 응답 상태와 본문 상세를 포함하여 발생합니다.</exception>
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
        /// <summary>
        /// 에피소드(episodic) 메모리를 서버에 삽입합니다.
        /// </summary>
        /// <param name="request">삽입할 에피소드 메모리의 내용(텍스트, 사용자 ID, 발화자, 선택적 감정, 컨텍스트 및 중요도 점수)을 포함한 요청 객체.</param>
        /// <returns>서버 응답을 파싱한 MemoryInsertResponse 객체.</returns>
        /// <exception cref="HttpRequestException">HTTP 응답이 성공(2xx)이 아닐 때, 서버 응답 내용과 상태 코드를 함께 포함하여 발생합니다.</exception>
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
        /// <summary>
        /// 지정된 의미(semantic) 메모리를 원격 메모리 서비스의 `/api/memory/semantic` 엔드포인트에 삽입합니다.
        /// </summary>
        /// <param name="request">삽입할 의미 메모리의 내용(텍스트, 사용자 ID, fact 타입, 신뢰도·중요도 점수, 선택적 마지막 수정 시각)을 포함하는 요청 객체입니다.</param>
        /// <returns>서버 응답을 파싱하여 구성한 <see cref="MemoryInsertResponse"/>를 반환합니다.</returns>
        /// <remarks>
        /// - 요청 페이로드는 JSON으로 직렬화되어 `text`, `user_id`, `fact_type`, `confidence_score`, `importance_score`, `last_updated`(있을 경우 ISO 8601 문자열) 필드를 전송합니다.
        /// - HTTP 응답의 본문을 파싱하여 내부적으로 <c>MapInsertResponse</c>를 통해 결과를 매핑합니다.
        /// </remarks>
        /// <exception cref="System.Net.Http.HttpRequestException">서버가 성공(2xx) 응답을 반환하지 않을 경우 상태 코드와 응답 본문을 포함한 예외를 던집니다.</exception>
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
        /// <summary>
        /// 지정한 메모리 유형(episodic 또는 semantic)에 대해 쿼리를 실행하여 유사한 메모리들을 반환합니다.
        /// </summary>
        /// <param name="memoryType">검색 대상 메모리 유형(episodic 또는 semantic).</param>
        /// <param name="query">검색할 텍스트 쿼리.</param>
        /// <param name="userId">요청하는 사용자 ID — 내부 요청에 `X-User-ID` 헤더로 전달됩니다.</param>
        /// <param name="limit">최대 반환 항목 수(기본값: 10).</param>
        /// <param name="similarityThreshold">0보다 클 경우 쿼리 문자열에 추가되어 유사도 기준으로 필터링합니다(기본값: 0.0).</param>
        /// <returns>검색 결과의 리스트. HTTP 오류나 예외 발생 시 빈 리스트를 반환합니다.</returns>
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
        /// <summary>
        /// 지정된 사용자에 대한 메모리 통계 정보를 원격 API에서 조회합니다.
        /// 실패하거나 예외가 발생하면 UserId만 설정된 빈 UserStatsResponse를 반환합니다.
        /// </summary>
        /// <param name="userId">조회할 사용자의 식별자(요청 시 URL에 안전하게 이스케이프됨).</param>
        /// <returns>사용자 통계가 채워된 UserStatsResponse; 오류 시 UserId만 설정된 기본 응답.</returns>
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
        /// <summary>
        /// 시스템 전체 메모리 통계를 비동기로 조회합니다.
        /// </summary>
        /// <returns>
        /// 성공 시 API에서 반환된 값을 매핑한 <see cref="SystemStatsResponse"/>를 반환합니다. 요청 실패나 예외가 발생하면 빈 필드가 채워진 새 <see cref="SystemStatsResponse"/> 인스턴스를 반환합니다.
        /// </returns>
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
        /// JSON 엘리먼트에서 MemoryInsertResponse로 매핑합니다.
        /// </summary>
        /// <remarks>
        /// 입력 JsonElement의 필드들을 안전하게 읽어 다음 프로퍼티로 변환합니다:
        /// - id, memory_type, collection_name, user_id: 문자열로 읽어 비어있지 않으면 설정(없거나 null이면 빈 문자열).
        /// - timestamp: 문자열일 경우 DateTimeOffset으로 파싱 시도(파싱 실패 시 설정하지 않음).
        /// - classification_confidence: 숫자일 경우 double로 설정.
        /// - classification_explanation: 문자열로 설정.
        /// 누락된 필드는 기본값(문자열은 빈 문자열, nullable 타입은 미설정)으로 남습니다.
        /// </remarks>
        /// <returns>맵핑된 MemoryInsertResponse 인스턴스.</returns>
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

        /// <summary>
        /// JSON 응답(JsonElement)을 UserStatsResponse로 변환합니다.
        /// </summary>
        /// <param name="root">사용자 통계 정보를 담은 JSON 루트 객체(예: API 응답의 최상위 요소).</param>
        /// <returns>JSON에서 추출한 값으로 채워진 UserStatsResponse(존재하지 않거나 형식이 맞지 않는 필드는 기본값 유지).</returns>
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

        /// <summary>
        /// JSON 루트에서 시스템 통계 값을 추출하여 SystemStatsResponse 객체로 변환합니다.
        /// </summary>
        /// <param name="root">API 응답의 JSON 루트 요소. 예상되는 필드는 `total_users`(정수), `total_memories`(정수), `uptime_since`(문자열, ISO 8601 또는 파싱 가능한 날짜/시간)입니다.</param>
        /// <returns>추출된 값으로 채워진 SystemStatsResponse. JSON에 필드가 없거나 타입이 일치하지 않으면 해당 속성은 기본값을 유지합니다.</returns>
        private static SystemStatsResponse MapSystemStats(JsonElement root)
        {
            var res = new SystemStatsResponse();
            if (root.TryGetProperty("total_users", out var tu) && tu.ValueKind == JsonValueKind.Number) res.TotalUsers = tu.GetInt32();
            if (root.TryGetProperty("total_memories", out var tm) && tm.ValueKind == JsonValueKind.Number) res.TotalMemories = tm.GetInt64();
            if (root.TryGetProperty("uptime_since", out var up) && up.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(up.GetString(), out var dto)) res.UptimeSince = dto;
            return res;
        }
    }
} 