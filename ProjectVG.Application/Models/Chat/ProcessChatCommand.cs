namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 처리 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class ProcessChatCommand
    {
        public string SessionId { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Action { get; set; }
        public Guid? CharacterId { get; set; }
    }
} 