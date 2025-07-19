using ProjectVG.Domain.Entities.ConversationHistory;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Repositories.InMemory
{
    public class InMemoryConversationRepository : IConversationRepository
    {
        private readonly Dictionary<string, List<ConversationHistory>> _conversations = new();
        private readonly ILogger<InMemoryConversationRepository> _logger;
        private const int MAX_CONVERSATION_MESSAGES = 50;

        public InMemoryConversationRepository(ILogger<InMemoryConversationRepository> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<ConversationHistory>> GetBySessionIdAsync(string sessionId, int count = 10)
        {
            if (_conversations.TryGetValue(sessionId, out var messages))
            {
                _logger.LogDebug("세션 {SessionId}에서 메시지 {Count}개를 조회했습니다", sessionId, messages.Count);
                return Task.FromResult(messages.TakeLast(count).AsEnumerable());
            }
            
            _logger.LogDebug("세션 {SessionId}에서 메시지를 찾을 수 없습니다", sessionId);
            return Task.FromResult(Enumerable.Empty<ConversationHistory>());
        }

        public Task<ConversationHistory> AddAsync(ConversationHistory message)
        {
            if (!_conversations.ContainsKey(message.SessionId))
            {
                _conversations[message.SessionId] = new List<ConversationHistory>();
                _logger.LogDebug("새로운 대화 세션을 생성했습니다: {SessionId}", message.SessionId);
            }

            var messages = _conversations[message.SessionId];
            
            // 시간순 정렬을 위해 적절한 위치에 삽입
            var insertIndex = messages.FindIndex(m => m.CreatedAt > message.CreatedAt);
            if (insertIndex == -1)
            {
                messages.Add(message);
            }
            else
            {
                messages.Insert(insertIndex, message);
            }

            // 최대 메시지 수 제한
            if (messages.Count > MAX_CONVERSATION_MESSAGES)
            {
                messages.RemoveRange(0, messages.Count - MAX_CONVERSATION_MESSAGES);
            }

            _logger.LogDebug("세션 {SessionId}에 메시지를 추가했습니다. 총 메시지 수: {Count}", 
                message.SessionId, messages.Count);

            return Task.FromResult(message);
        }

        public Task ClearSessionAsync(string sessionId)
        {
            if (_conversations.ContainsKey(sessionId))
            {
                _conversations[sessionId].Clear();
                _logger.LogInformation("대화 세션을 삭제했습니다: {SessionId}", sessionId);
            }
            
            return Task.CompletedTask;
        }

        public Task<int> GetMessageCountAsync(string sessionId)
        {
            if (_conversations.TryGetValue(sessionId, out var messages))
            {
                return Task.FromResult(messages.Count);
            }
            
            return Task.FromResult(0);
        }
    }
} 