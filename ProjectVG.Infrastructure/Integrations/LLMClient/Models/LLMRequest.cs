using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.LLMClient.Models
{
    public class LLMRequest
    {
        /// <summary>
        /// 요청 ID
        /// </summary>
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; } = "";

        /// <summary>
        /// 시스템 프롬프트
        /// </summary>
        [JsonPropertyName("system_prompt")]
        public string? SystemPrompt { get; set; } = "";

        /// <summary>
        /// 사용자 메시지
        /// </summary>
        [JsonPropertyName("user_prompt")]
        public string UserPrompt { get; set; } = "";

        /// <summary>
        /// 추가 지시사항
        /// </summary>
        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; } = "";

        /// <summary>
        /// 대화 기록
        /// </summary>
        [JsonPropertyName("conversation_history")]
        public List<History>? ConversationHistory { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } = 1000;

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; } = 0.7f;

        [JsonPropertyName("model")]
        public string? Model { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// 사용자 제공 API Key
        /// </summary>
        [JsonPropertyName("openai_api_key")]
        public string? OpenAiApiKey { get; set; } = "";

        /// <summary>
        /// 사용자 API Key 사용 여부
        /// </summary>
        [JsonPropertyName("use_user_api_key")]
        public bool? UseUserApiKey { get; set; } = false;
    }
} 