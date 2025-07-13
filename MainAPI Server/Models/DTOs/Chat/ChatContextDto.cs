using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.DTOs.Chat
{
    /// <summary>
    /// LLM 서비스에 전달할 채팅 컨텍스트 정보
    /// </summary>
    public class ChatContextDto
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 메시지
        /// </summary>
        [JsonPropertyName("user_message")]
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 액터 정보
        /// </summary>
        [JsonPropertyName("actor")]
        public string Actor { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 액션 정보
        /// </summary>
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        /// <summary>
        /// 시스템 메시지 (LLM 설정에서 가져옴)
        /// </summary>
        [JsonPropertyName("system_message")]
        public string SystemMessage { get; set; } = string.Empty;

        /// <summary>
        /// 대화 기록 컨텍스트
        /// </summary>
        [JsonPropertyName("conversation_context")]
        public List<string> ConversationContext { get; set; } = new();

        /// <summary>
        /// 메모리 검색 결과 컨텍스트
        /// </summary>
        [JsonPropertyName("memory_context")]
        public List<string> MemoryContext { get; set; } = new();

        /// <summary>
        /// LLM 설정 정보
        /// </summary>
        [JsonPropertyName("llm_settings")]
        public LLMSettingsDto LLMSettings { get; set; } = new();
    }

    /// <summary>
    /// LLM 설정 정보
    /// </summary>
    public class LLMSettingsDto
    {
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;
    }
} 