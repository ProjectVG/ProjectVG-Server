namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 처리 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class ProcessChatCommand
    {
        private string _requestId = Guid.NewGuid().ToString();
        public string RequestId
        {
            get => string.IsNullOrEmpty(_requestId) ? (_requestId = Guid.NewGuid().ToString()) : _requestId;
            set => _requestId = value;
        }
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid CharacterId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Action { get; set; }
        public string? Instruction { get; set; }
    }
} 