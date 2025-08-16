using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.LLMClient.Models
{
    public class LLMResponse
    {
        /// <summary>
        /// OpenAI API 응답 ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

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
        /// 사용된 토큰 수 (총합)
        /// </summary>
        [JsonPropertyName("total_tokens_used")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// 입력 토큰 수 (prompt_tokens)
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// 출력 토큰 수 (completion_tokens)
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

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