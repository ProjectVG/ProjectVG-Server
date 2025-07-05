using MainAPI_Server.Models.Chat;

namespace MainAPI_Server.Services.Conversation
{
    public class ConversationService : IConversationService
    {
        
        private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
        private const int MAX_CONVERSATION_MESSAGES = 50; // 최대 대화 기록 개수
        private readonly ChatMessageComparer _comparer = new();

        public void AddMessage(string SesstionId, MessageRole role, string content)
        {
            ChatMessage message = new ChatMessage() {
                SessionId = SesstionId,
                Role = role,
                Content = content
            };
            AddMessage(message);
        }

        public void AddMessage(ChatMessage message)
        {
            if (!_conversations.ContainsKey(message.SessionId))
            {
                _conversations[message.SessionId] = new List<ChatMessage>();
            }
            
            var messages = _conversations[message.SessionId];

            // 저장 위치 안정성 보장 - 최근 일수록 높은 인덱스에 위치 (log(n))
            var insertIndex = messages.BinarySearch(message, _comparer);
            if (insertIndex < 0)
            {
                insertIndex = ~insertIndex;
            }
            messages.Insert(insertIndex, message);
            
            if (messages.Count > MAX_CONVERSATION_MESSAGES)
            {
                messages.RemoveRange(0, messages.Count - MAX_CONVERSATION_MESSAGES);
            }
        }

        public List<ChatMessage> GetConversationHistory(string sessionId, int count = 5)
        {
            if (_conversations.TryGetValue(sessionId, out var messages)) {
                return messages.TakeLast(count).ToList();
            }
            return new List<ChatMessage>();
        }

        public void ClearConversation(string sessionId)
        {
            if (_conversations.ContainsKey(sessionId))
            {
                _conversations[sessionId].Clear();
            }
        }

    }
} 