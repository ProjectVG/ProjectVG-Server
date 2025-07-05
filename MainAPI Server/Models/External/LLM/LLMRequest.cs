using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.External.LLM
{
    public class LLMRequest
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = default!;

        /// <summary>
        /// 추가 시스템 프롬포트 메시지 (System Prompt)
        /// </summary>
        [JsonPropertyName("system_message")]
        public string SystemMessage { get; set; } = default!;

        /// <summary>
        /// 매인 쿼리, 유저 메시지 (User Prompt)
        /// </summary>
        [JsonPropertyName("user_message")]
        public string UserMessage { get; set; } = default!;

        /// <summary>
        /// 최근 대화 내역 (User Prompt에 추가됨)
        /// </summary>
        [JsonPropertyName("conversation_history")]
        public List<string> ConversationHistory { get; set; } = new();

        /// <summary>
        /// 장기 기억 Context (System Prompt에 추가됨)
        /// </summary>
        [JsonPropertyName("memory_context")]
        public List<string> MemoryContext { get; set; } = new();

        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;

        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4o-mini";
    }
} 