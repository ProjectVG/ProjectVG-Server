using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Chat
{
    public class ChatMessage : BaseEntity
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Action { get; set; }
        public Guid? CharacterId { get; set; }
    }
} 