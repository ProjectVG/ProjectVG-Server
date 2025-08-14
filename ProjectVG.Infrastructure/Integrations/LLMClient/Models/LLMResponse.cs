using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.LLMClient.Models
{
    public class LLMResponse
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = default!;

        /// <summary>
        /// 응답
        /// </summary>
        [JsonPropertyName("response_text")]
        public string Response { get; set; } = default!;

        /// <summary>
        /// 사용된 토큰 수
        /// </summary>
        [JsonPropertyName("total_tokens_used")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// 처리 시간 (ms)
        /// </summary>
        [JsonPropertyName("response_time")]
        public double ResponseTime { get; set; }

        /// <summary>
        /// 사용된 LLMClient 모델
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }
} 