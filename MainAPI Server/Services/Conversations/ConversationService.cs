using MainAPI_Server.Models.Domain.Chats;

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
            Console.WriteLine($"[ConversationService] 메시지 추가: 세션ID={message.SessionId}, 역할={message.Role}");
            Console.WriteLine($"[ConversationService] 현재 저장된 세션 수: {_conversations.Count}");
            
            if (!_conversations.ContainsKey(message.SessionId))
            {
                _conversations[message.SessionId] = new List<ChatMessage>();
                Console.WriteLine($"[ConversationService] 새 세션 생성: {message.SessionId}");
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
            Console.WriteLine($"[ConversationService] 세션 {message.SessionId}의 메시지 수: {messages.Count}");
        }

        public List<ChatMessage> GetConversationHistory(string sessionId, int count = 5)
        {
            Console.WriteLine($"[ConversationService] 대화 기록 요청: 세션ID={sessionId}");
            Console.WriteLine($"[ConversationService] 현재 저장된 세션 수: {_conversations.Count}");
            
            if (_conversations.TryGetValue(sessionId, out var messages)) {
                Console.WriteLine($"[ConversationService] 세션 {sessionId}에서 {messages.Count}개 메시지 발견, {count}개 반환");
                return messages.TakeLast(count).ToList();
            }
            Console.WriteLine($"[ConversationService] 세션 {sessionId}를 찾을 수 없음");
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