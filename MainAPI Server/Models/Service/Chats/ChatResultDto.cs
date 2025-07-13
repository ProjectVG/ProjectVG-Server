using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.Service.Chats
{
    /// <summary>
    /// LLM 서비스에서 반환되는 채팅 결과
    /// </summary>
    public class ChatResultDto
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 사용자 원본 메시지
        /// </summary>
        [JsonPropertyName("user_message")]
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// AI 응답 메시지
        /// </summary>
        [JsonPropertyName("ai_response")]
        public string AiResponse { get; set; } = string.Empty;

        /// <summary>
        /// 성공 여부
        /// </summary>
        [JsonPropertyName("is_success")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 사용된 토큰 수
        /// </summary>
        [JsonPropertyName("tokens_used")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 선택된 모델
        /// </summary>
        [JsonPropertyName("selected_model")]
        public string SelectedModel { get; set; } = string.Empty;

        /// <summary>
        /// 처리 시간 (밀리초)
        /// </summary>
        [JsonPropertyName("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// 원본 컨텍스트 정보
        /// </summary>
        [JsonPropertyName("context_info")]
        public ChatContextDto ContextInfo { get; set; } = new();
    }
} 