using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.External.LLM
{
    public class LLMResponse
    {
        /// <summary>
        /// 응답
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; } = default!;

        /// <summary>
        /// 사용된 토큰 수
        /// </summary>
        [JsonPropertyName("tokens_used")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// 처리 시간 (ms)
        /// </summary>
        [JsonPropertyName("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// 사용된 LLM 모델
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }
} 
