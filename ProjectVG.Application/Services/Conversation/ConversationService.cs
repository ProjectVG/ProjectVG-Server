using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Conversation
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(IConversationRepository conversationRepository, ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, ChatRole role, string content)
        {
            var message = new ConversationHistory
            {
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            var addedMessage = await _conversationRepository.AddAsync(message);
            _logger.LogDebug("대화 메시지 추가 완료: 유저 {UserId}, 역할 {Role}", userId, role);
            
            return addedMessage;
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int count = 10)
        {
            var history = await _conversationRepository.GetByUserIdAsync(userId, characterId, count);
            _logger.LogDebug("대화 기록 조회 완료: 유저 {UserId}, {Count}개 메시지", userId, history.Count());
            
            return history;
        }

        public async Task ClearConversationAsync(Guid userId, Guid characterId)
        {
            await _conversationRepository.ClearSessionAsync(userId, characterId);
            _logger.LogInformation("대화 기록 삭제 완료: 유저 {UserId}", userId);
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var count = await _conversationRepository.GetMessageCountAsync(userId, characterId);
            _logger.LogDebug("메시지 수 조회 완료: 유저 {UserId}, {Count}개", userId, count);
            
            return count;
        }
    }
} 