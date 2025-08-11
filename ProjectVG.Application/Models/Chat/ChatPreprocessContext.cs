using Microsoft.Extensions.Logging;

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

        public override string ToString()
        {
            return $"ChatPreprocessContext(SessionId={SessionId}, UserId={UserId}, CharacterId={CharacterId}, UserMessage='{UserMessage}', MemoryContext.Count={MemoryContext.Count}, ConversationHistory.Count={ConversationHistory.Count})";
        }

        public string GetDetailedInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ChatPreprocessContext ===");
            info.AppendLine($"SessionId: {SessionId}");
            info.AppendLine($"UserId: {UserId}");
            info.AppendLine($"CharacterId: {CharacterId}");
            info.AppendLine($"Action: {Action ?? "N/A"}");
            info.AppendLine($"VoiceName: {VoiceName}");
            info.AppendLine($"MemoryStore: {MemoryStore}");
            info.AppendLine($"UserMessage: {UserMessage}");
            info.AppendLine($"SystemMessage: {SystemMessage}");
            info.AppendLine($"Instructions: {Instructions}");
            
            info.AppendLine($"MemoryContext ({MemoryContext.Count} items):");
            for (int i = 0; i < MemoryContext.Count; i++)
            {
                info.AppendLine($"  [{i}]: {MemoryContext[i]}");
            }
            
            info.AppendLine($"ConversationHistory ({ConversationHistory.Count} items):");
            for (int i = 0; i < ConversationHistory.Count; i++)
            {
                info.AppendLine($"  [{i}]: {ConversationHistory[i]}");
            }
            info.AppendLine("=== End ChatPreprocessContext ===");
            
            return info.ToString();
        }
    }
} 