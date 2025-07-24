namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessContext
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public string SystemMessage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
        
        public string? Action { get; set; }
        public List<string> MemoryContext { get; set; } = new();
        public List<string> ConversationHistory { get; set; } = new();

        public string MemoryStore { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
    }
} 