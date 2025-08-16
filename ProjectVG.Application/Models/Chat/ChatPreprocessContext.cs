using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessContext
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        
        public string? Action { get; set; }
        public List<string> MemoryContext { get; set; } = new();
        public IEnumerable<ConversationHistory> ConversationHistory { get; set; } = new List<ConversationHistory>();

        public string MemoryStore { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
        public CharacterDto? Character { get; set; }

        public ChatPreprocessContext(
            ProcessChatCommand command,
            List<string> memoryContext,
            IEnumerable<ConversationHistory> conversationHistory)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            Action = command.Action;
            MemoryContext = memoryContext ?? new List<string>();
            ConversationHistory = conversationHistory ?? new List<ConversationHistory>();
            
            // Character는 반드시 존재한다고 가정
            Character = command.Character!;
            VoiceName = command.Character!.VoiceId;
        }

        public List<string> ParseConversationHistory()
        {
            return ConversationHistory?.Select(c => $"{c.Role}: {c.Content}").ToList() ?? new List<string>();
        }

        public List<string> ParseConversationHistory(int takeCount)
        {
            return ConversationHistory?.Take(takeCount).Select(c => $"{c.Role}: {c.Content}").ToList() ?? new List<string>();
        }

        public override string ToString()
        {
            return $"ChatPreprocessContext(SessionId={SessionId}, UserId={UserId}, CharacterId={CharacterId}, UserMessage='{UserMessage}', MemoryContext.Count={MemoryContext.Count}, ConversationHistory.Count={ConversationHistory.Count()})";
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
            info.AppendLine($"Character: {(Character != null ? Character.Name : "Not Loaded")}");
            
            info.AppendLine($"MemoryContext ({MemoryContext.Count} items):");
            for (int i = 0; i < MemoryContext.Count; i++)
            {
                info.AppendLine($"  [{i}]: {MemoryContext[i]}");
            }
            
            info.AppendLine($"ConversationHistory ({ConversationHistory.Count()} items):");
            var parsedHistory = ParseConversationHistory();
            for (int i = 0; i < parsedHistory.Count; i++)
            {
                info.AppendLine($"  [{i}]: {parsedHistory[i]}");
            }
            info.AppendLine("=== End ChatPreprocessContext ===");
            
            return info.ToString();
        }
    }
} 