using MainAPI_Server.Models.Chat;

namespace MainAPI_Server.Services.Conversation
{
    public class ConversationService : IConversationService
    {
        private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
        private const int MAX_CONVERSATION_MESSAGES = 50; // 최대 대화 기록 개수

        public void AddMessage(ChatMessage message)
        {
            if (!_conversations.ContainsKey(message.SessionId))
            {
                _conversations[message.SessionId] = new List<ChatMessage>();
            }
            
            _conversations[message.SessionId].Add(message);
            
            if (_conversations[message.SessionId].Count > MAX_CONVERSATION_MESSAGES)
            {
                _conversations[message.SessionId] = _conversations[message.SessionId].TakeLast(MAX_CONVERSATION_MESSAGES).ToList();
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