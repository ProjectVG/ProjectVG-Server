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

        private static string GetKey(Guid userId, Guid characterId) => $"{userId}:{characterId}";

        public Task<IEnumerable<ConversationHistory>> GetByUserIdAsync(Guid userId, Guid characterId, int count = 10)
        {
            var key = GetKey(userId, characterId);
            if (_conversations.TryGetValue(key, out var messages))
            {
                _logger.LogDebug("유저 {UserId}, 캐릭터 {CharacterId}에서 메시지 {Count}개를 조회했습니다", userId, characterId, messages.Count);
                return Task.FromResult(messages.TakeLast(count).AsEnumerable());
            }
            _logger.LogDebug("유저 {UserId}, 캐릭터 {CharacterId}에서 메시지를 찾을 수 없습니다", userId, characterId);
            return Task.FromResult(Enumerable.Empty<ConversationHistory>());
        }

        public Task<ConversationHistory> AddAsync(ConversationHistory message)
        {
            var key = GetKey(message.UserId, message.CharacterId);
            if (!_conversations.ContainsKey(key))
            {
                _conversations[key] = new List<ConversationHistory>();
                _logger.LogDebug("새로운 대화 세션을 생성했습니다: {UserId}:{CharacterId}", message.UserId, message.CharacterId);
            }
            var messages = _conversations[key];
            var insertIndex = messages.FindIndex(m => m.CreatedAt > message.CreatedAt);
            if (insertIndex == -1)
            {
                messages.Add(message);
            }
            else
            {
                messages.Insert(insertIndex, message);
            }
            if (messages.Count > MAX_CONVERSATION_MESSAGES)
            {
                messages.RemoveRange(0, messages.Count - MAX_CONVERSATION_MESSAGES);
            }
            _logger.LogDebug("유저 {UserId}, 캐릭터 {CharacterId}에 메시지를 추가했습니다. 총 메시지 수: {Count}", message.UserId, message.CharacterId, messages.Count);
            return Task.FromResult(message);
        }

        public Task ClearSessionAsync(Guid userId, Guid characterId)
        {
            var key = GetKey(userId, characterId);
            if (_conversations.ContainsKey(key))
            {
                _conversations[key].Clear();
                _logger.LogInformation("대화 세션을 삭제했습니다: {UserId}:{CharacterId}", userId, characterId);
            }
            return Task.CompletedTask;
        }

        public Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var key = GetKey(userId, characterId);
            if (_conversations.TryGetValue(key, out var messages))
            {
                return Task.FromResult(messages.Count);
            }
            return Task.FromResult(0);
        }
    }
} 