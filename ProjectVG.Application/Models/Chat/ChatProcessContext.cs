using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessContext
    {
        public string SessionId { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }
        public string UserMessage { get; private set; } = string.Empty;
        public string MemoryStore { get; private set; } = string.Empty;
        public bool UseTTS { get; private set; } = true;
        
        public CharacterDto? Character { get; private set; }
        public IEnumerable<string>? MemoryContext { get; private set; }
        public IEnumerable<ConversationHistory>? ConversationHistory { get; private set; }
        
        public string Response { get; private set; } = string.Empty;
        public double Cost { get; private set; }
        public List<ChatMessageSegment> Segments { get; private set; } = new List<ChatMessageSegment>();
        
        public string FullText => string.Join(" ", Segments.Where(s => s.HasText).Select(s => s.Text));
        public bool HasAudio => Segments.Any(s => s.HasAudio);
        public bool HasText => Segments.Any(s => s.HasText);


        public ChatProcessContext(ProcessChatCommand command)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
        }

        public ChatProcessContext(
            ProcessChatCommand command,
            CharacterDto character,
            IEnumerable<ConversationHistory> conversationHistory,
            IEnumerable<string> memoryContext)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
            
            Character = character;
            ConversationHistory = conversationHistory;
            MemoryContext = memoryContext;
        }

        public void SetResponse(string response, List<ChatMessageSegment> segments, double cost)
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

            return ConversationHistory
                .Take(count)
                .Select(h => $"{h.Role}: {h.Content}");
        }
    }
}
