using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

namespace ProjectVG.Infrastructure.Integrations.MemoryClient
{
    public interface IMemoryClient
    {
        /// <summary>
        /// 텍스트를 자동 분류(Episodic/Semantic)하여 삽입한다.
        /// </summary>
        Task<MemoryInsertResponse> InsertAutoAsync(MemoryInsertRequest request);

        /// <summary>
        /// Episodic 메모리를 수동 타입으로 삽입한다.
        /// </summary>
        Task<MemoryInsertResponse> InsertEpisodicAsync(EpisodicInsertRequest request);

        /// <summary>
        /// Semantic 메모리를 수동 타입으로 삽입한다.
        /// </summary>
        Task<MemoryInsertResponse> InsertSemanticAsync(SemanticInsertRequest request);

        /// <summary>
        /// 특정 타입(Episodic/Semantic)의 메모리에서 검색한다.
        /// </summary>
        Task<List<MemorySearchResult>> SearchAsync(MemoryType memoryType, string query, string userId, int limit = 10, double similarityThreshold = 0.0);

        /// <summary>
        /// 사용자 메모리 통계를 조회한다.
        /// </summary>
        Task<UserStatsResponse> GetUserStatsAsync(string userId);

        /// <summary>
        /// 시스템 전체 통계를 조회한다.
        /// </summary>
        Task<SystemStatsResponse> GetSystemStatsAsync();

        /// <summary>
        /// 여러 메모리 타입에서 동시 검색한다.
        /// </summary>
        Task<MultiSearchResponse> SearchMultiAsync(string query, string userId, int limit = 10, double similarityThreshold = 0.0);
    }
}

namespace ProjectVG.Infrastructure.Integrations.MemoryClient.Models
{
    public enum MemoryType
    {
        Episodic,
        Semantic
    }

    public class MemoryInsertRequest
    {
        public string Text { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? Speaker { get; set; }
        public EmotionInfo? Emotion { get; set; }
        public Dictionary<string, object>? Context { get; set; }
        public double? ImportanceScore { get; set; }
    }

    public class EpisodicInsertRequest : MemoryInsertRequest
    {
    }

    public class SemanticInsertRequest : MemoryInsertRequest
    {
        public string? FactType { get; set; }
        public double? ConfidenceScore { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }

    public class EmotionInfo
    {
        public string? Valence { get; set; }
        public string? Arousal { get; set; }
        public List<string>? Labels { get; set; }
        public double? Intensity { get; set; }
    }

    public class MemoryInsertResponse
    {
        public string Id { get; set; } = string.Empty;
        public string MemoryType { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public double? ClassificationConfidence { get; set; }
        public string? ClassificationExplanation { get; set; }
    }

    public class UserStatsResponse
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalMemories { get; set; }
        public int EpisodicCount { get; set; }
        public int SemanticCount { get; set; }
        public DateTimeOffset? OldestMemory { get; set; }
        public DateTimeOffset? NewestMemory { get; set; }
        public double? DailyAverage { get; set; }
        public string? MostActiveDay { get; set; }
    }

    public class SystemStatsResponse
    {
        public int TotalUsers { get; set; }
        public long TotalMemories { get; set; }
        public DateTimeOffset? UptimeSince { get; set; }
    }

    public class MultiSearchResponse
    {
        public List<MemorySearchResult> EpisodicResults { get; set; } = new();
        public List<MemorySearchResult> SemanticResults { get; set; } = new();
        public int TotalResults { get; set; }
        public string Query { get; set; } = string.Empty;
    }
} 