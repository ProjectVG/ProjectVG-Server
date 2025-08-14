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

        public async Task<ConversationHistory> AddMessageAsync(Guid userId, Guid chracterId, ChatRole role, string content)
        {
            try
            {
                var message = new ConversationHistory
                {
                    UserId = userId,
                    CharacterId = chracterId,
                    Role = role,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                var addedMessage = await _conversationRepository.AddAsync(message);
                _logger.LogDebug("대화 메시지 추가 완료: 유저 {userId}, 역할 {Role}", userId, role);
                
                return addedMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 메시지 추가 중 오류 발생: 유저 {userId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid chracterId, int count = 10)
        {
            try
            {
                var history = await _conversationRepository.GetByUserIdAsync(userId, chracterId, count);
                _logger.LogDebug("대화 기록 조회 완료: 유저 {userId}, {Count}개 메시지", userId, history.Count());
                
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 조회 중 오류 발생: 유저 {userId}", userId);
                throw;
            }
        }

        public async Task ClearConversationAsync(Guid userId, Guid chracterId)
        {
            try
            {
                await _conversationRepository.ClearSessionAsync(userId, chracterId);
                _logger.LogInformation("대화 기록 삭제 완료: 유저 {userId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 삭제 중 오류 발생: 유저 {userId}", userId.ToString());
                throw;
            }
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid chracterId)
        {
            try
            {
                var count = await _conversationRepository.GetMessageCountAsync(userId, chracterId);
                _logger.LogDebug("메시지 수 조회 완료: 유저 {userId}, {Count}개", userId, count);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 수 조회 중 오류 발생: 유저 {userId}", userId.ToString());
                throw;
            }
        }
    }
} 