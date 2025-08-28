using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.LLMClient.Models
{
    public class LLMResponse
    {
        /// <summary> OpenAI Response ID </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        /// <summary> 요청 ID </summary>
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = default!;

        /// <summary> 응답 객체 타입 </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "response";

        /// <summary> 생성 시간 </summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary> 응답 상태 (completed, failed) </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        /// <summary> 사용된 OpenAI 모델 </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        /// <summary> AI 응답 텍스트 </summary>
        [JsonPropertyName("output_text")]
        public string OutputText { get; set; } = default!;

        /// <summary> 입력 토큰 수 </summary>
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        /// <summary> 출력 토큰 수 </summary>
        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }

        /// <summary> 총 토큰 수 </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary> 캐시된 토큰 수 </summary>
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }

        /// <summary> 추론 토큰 수 (o-series) </summary>
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }

        /// <summary> 텍스트 형식 타입</summary>
        [JsonPropertyName("text_format_type")]
        public string TextFormatType { get; set; } = "text";

        /// <summary> 비용 </summary>
        [JsonPropertyName("cost")]
        public int? Cost { get; set; }

        /// <summary> 응답 시간 (초) </summary>
        [JsonPropertyName("response_time")]
        public double? ResponseTime { get; set; }

        /// <summary> 성공 여부 </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary> 에러 메시지 </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary> 사용자 API Key 사용 여부 </summary>
        [JsonPropertyName("use_user_api_key")]
        public bool UseUserApiKey { get; set; } = false;

    }
} 