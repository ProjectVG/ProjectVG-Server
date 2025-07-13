using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.DTOs.Common
{
    /// <summary>
    /// 공통 처리 결과 DTO
    /// </summary>
    public class ProcessingResultDto
    {
        /// <summary>
        /// 성공 여부
        /// </summary>
        [JsonPropertyName("is_success")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 오류 메시지
        /// </summary>
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 처리 시간 (밀리초)
        /// </summary>
        [JsonPropertyName("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// 처리 시작 시간
        /// </summary>
        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 처리 종료 시간
        /// </summary>
        [JsonPropertyName("end_time")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 처리된 항목 수
        /// </summary>
        [JsonPropertyName("processed_count")]
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 추가 메타데이터
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
} 