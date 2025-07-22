namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 처리 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class ProcessChatCommand
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid CharacterId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Action { get; set; }
        public string? Instruction { get; set; }
    }
} 