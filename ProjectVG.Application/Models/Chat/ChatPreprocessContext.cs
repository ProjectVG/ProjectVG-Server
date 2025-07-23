namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserMessage { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;

        public string UserMemory { get; set; } = string.Empty;
        public string? Action { get; set; }
        public Guid? CharacterId { get; set; }
        public List<string> MemoryContext { get; set; } = new();
        public List<string> ConversationHistory { get; set; } = new();
        public string SystemMessage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
        public string[] AllowedEmotions { get; set; } = Array.Empty<string>();
    }
} 