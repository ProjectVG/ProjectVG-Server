namespace ProjectVG.Api.Models.Conversation.Response
{
    public class ConversationHistoryResponse
    {
        /// <summary>
        /// 메시지 ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 캐릭터 ID
        /// </summary>
        public Guid CharacterId { get; set; }

        /// <summary>
        /// 메시지 역할 (user, assistant, system)
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 메시지 내용
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 메시지 생성 시각
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 대화 세션 ID (선택사항)
        /// </summary>
        public string? ConversationId { get; set; }

        /// <summary>
        /// 메시지 생성일시
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}