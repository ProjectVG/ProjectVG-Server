using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Models.Chat
{
    public record ChatProcessContext
    {
        public Guid RequestId { get; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }
        public string UserMessage { get; private set; } = string.Empty;
        public string MemoryStore { get; private set; } = string.Empty;
        public DateTime UserRequestAt { get; private set; } = DateTime.Now;
        public bool UseTTS { get; private set; } = true;
        
        public CharacterDto? Character { get; private set; }
        public IEnumerable<string>? MemoryContext { get; private set; }
        public IEnumerable<ConversationHistory>? ConversationHistory { get; private set; }
        
        public string Response { get; private set; } = string.Empty;
        public double Cost { get; private set; }
        public List<ChatSegment> Segments { get; private set; } = new List<ChatSegment>();

        public ChatProcessContext(ChatRequestCommand command)
        {
            RequestId = command.Id;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.UserPrompt;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
            UserRequestAt = command.UserRequestAt;
        }

        public ChatProcessContext(
            ChatRequestCommand command,
            CharacterDto character,
            IEnumerable<ConversationHistory> conversationHistory,
            IEnumerable<string> memoryContext)
        {
            RequestId = command.Id;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.UserPrompt;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
            UserRequestAt = command.UserRequestAt;
            
            Character = character;
            ConversationHistory = conversationHistory;
            MemoryContext = memoryContext;
        }

        public void SetResponse(string response, List<ChatSegment> segments, double cost)
        {
            Response = response;
            Segments = segments;
            Cost = cost;
        }

        public void AddCost(double additionalCost)
        {
            Cost += additionalCost;
        }

        public IEnumerable<string> ParseConversationHistory(int count = 5)
        {
            if (ConversationHistory == null) return Enumerable.Empty<string>();

            return ConversationHistory.Take(count).Select(h => $"{h.Role}: {h.Content}");
        }

        public string ToDebugString()
        {
            var sb = new System.Text.StringBuilder();
            
            // Request 기본 정보
            sb.AppendLine($"[ChatProcessContext Debug Info]");
            sb.AppendLine($"=== REQUEST INFO ===");
            sb.AppendLine($"SessionId: {RequestId}");
            sb.AppendLine($"UserId: {UserId}");
            sb.AppendLine($"CharacterId: {CharacterId}");
            sb.AppendLine($"UserMessage: \"{UserMessage}\"");
            sb.AppendLine($"UseTTS: {UseTTS}");
            sb.AppendLine($"UserRequestAt: {UserRequestAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Character: {Character?.Name ?? "null"}");
            
            // LLM 전처리 정보
            sb.AppendLine($"=== LLM PREPROCESSING INFO ===");
            
            // ConversationHistory 전체 내용
            sb.AppendLine($"ConversationHistory ({ConversationHistory?.Count() ?? 0} items):");
            if (ConversationHistory != null && ConversationHistory.Any())
            {
                foreach (var history in ConversationHistory)
                {
                    sb.AppendLine($"  - {history.Role}: \"{history.Content}\"");
                }
            }
            else
            {
                sb.AppendLine("  (No conversation history)");
            }
            
            // MemoryContext 전체 내용
            sb.AppendLine($"MemoryContext ({MemoryContext?.Count() ?? 0} items):");
            if (MemoryContext != null && MemoryContext.Any())
            {
                foreach (var memory in MemoryContext)
                {
                    sb.AppendLine($"  - \"{memory}\"");
                }
            }
            else
            {
                sb.AppendLine("  (No memory context)");
            }
            
            // 결과 정보
            sb.AppendLine($"=== RESULT INFO ===");
            sb.AppendLine($"Response: \"{Response}\"");
            sb.AppendLine($"Cost: {Cost:F4}");
            
            // Segments 전체 내용
            sb.AppendLine($"Segments ({Segments?.Count ?? 0} items):");
            if (Segments != null && Segments.Any())
            {
                for (int i = 0; i < Segments.Count; i++)
                {
                    var segment = Segments[i];
                    sb.AppendLine($"  [{i}] Content: \"{segment.Content}\", Emotion: {segment.Emotion}, Actions: [{(segment.Actions != null ? string.Join(", ", segment.Actions) : "")}]");
                }
            }
            else
            {
                sb.AppendLine("  (No segments)");
            }
            
            return sb.ToString();
        }
    }
}
