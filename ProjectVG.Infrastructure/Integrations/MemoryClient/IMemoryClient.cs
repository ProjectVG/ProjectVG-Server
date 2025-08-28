using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

namespace ProjectVG.Infrastructure.Integrations.MemoryClient
{
    public interface IMemoryClient
    {
        /// <summary>
        /// 텍스트를 자동 분류(Episodic/Semantic)하여 삽입한다.
        /// <summary>
/// 입력된 텍스트를 받아 자동으로 에피소드(Episodic) 또는 시맨틱(Semantic)으로 분류해 비동기적으로 저장합니다.
/// </summary>
/// <param name="request">저장할 메모리의 내용과 메타데이터(텍스트, 사용자 ID, 화자, 감정, 컨텍스트 등)를 포함한 요청 객체.</param>
/// <returns>삽입된 메모리의 식별자·타입·컬렉션명·타임스탬프 등 정보를 담은 <see cref="MemoryInsertResponse"/>를 비동기로 반환합니다.</returns>
        Task<MemoryInsertResponse> InsertAutoAsync(MemoryInsertRequest request);

        /// <summary>
        /// Episodic 메모리를 수동 타입으로 삽입한다.
        /// <summary>
/// 사용자의 에피소드성 메모리를 비동기적으로 저장합니다.
/// </summary>
/// <param name="request">저장할 에피소드 메모리 정보(텍스트, UserId 및 선택적 메타데이터 포함)를 가진 요청 객체.</param>
/// <returns>삽입된 메모리의 식별자, 컬렉션명, 타임스탬프 및 분류 관련 메타데이터를 포함하는 <see cref="MemoryInsertResponse"/>를 반환하는 비동기 작업.</returns>
        Task<MemoryInsertResponse> InsertEpisodicAsync(EpisodicInsertRequest request);

        /// <summary>
        /// Semantic 메모리를 수동 타입으로 삽입한다.
        /// <summary>
/// 주어진 SemanticInsertRequest를 사용하여 수동으로 의미적(semantic) 메모리를 비동기적으로 저장합니다.
/// </summary>
/// <param name="request">저장할 의미적 메모리의 내용과 메타데이터(예: Text, UserId, FactType, ConfidenceScore, LastUpdated 등)를 포함한 요청 객체.</param>
/// <returns>생성된 메모리의 정보(식별자, 컬렉션명, 타임스탬프 등)를 담은 MemoryInsertResponse를 비동기적으로 반환합니다.</returns>
        Task<MemoryInsertResponse> InsertSemanticAsync(SemanticInsertRequest request);

        /// <summary>
        /// 특정 타입(Episodic/Semantic)의 메모리에서 검색한다.
        /// <summary>
/// 지정한 사용자에 대해 특정 메모 타입에서 쿼리와 유사한 메모를 비동기적으로 검색합니다.
/// </summary>
/// <param name="memoryType">검색할 메모의 분류(예: Episodic 또는 Semantic).</param>
/// <param name="query">검색할 텍스트 쿼리.</param>
/// <param name="userId">검색 대상 메모가 속한 사용자 식별자.</param>
/// <param name="limit">반환할 최대 결과 수(기본값 10).</param>
/// <param name="similarityThreshold">결과로 포함할 최소 유사도 임계값(0.0–1.0, 기본값 0.0 — 필터 없음).</param>
/// <returns>조건에 맞는 MemorySearchResult 객체 리스트를 비동기적으로 반환합니다.</returns>
        Task<List<MemorySearchResult>> SearchAsync(MemoryType memoryType, string query, string userId, int limit = 10, double similarityThreshold = 0.0);

        /// <summary>
        /// 사용자 메모리 통계를 조회한다.
        /// <summary>
/// 지정한 사용자의 메모 통계를 비동기로 조회합니다.
/// </summary>
/// <param name="userId">통계를 조회할 대상 사용자의 고유 식별자.</param>
/// <returns>해당 사용자의 통계 정보를 담은 <see cref="UserStatsResponse"/>를 반환하는 작업.</returns>
        Task<UserStatsResponse> GetUserStatsAsync(string userId);

        /// <summary>
        /// 시스템 전체 통계를 조회한다.
        /// <summary>
/// 시스템 전체 통계(총 사용자 수, 총 메모리 수, 가동 시각 등)를 비동기적으로 조회합니다.
/// </summary>
/// <returns>시스템 통계 정보를 담은 <see cref="SystemStatsResponse"/> 객체.</returns>
        Task<SystemStatsResponse> GetSystemStatsAsync();
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
} 