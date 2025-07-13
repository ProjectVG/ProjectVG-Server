using ProjectVG.Domain.Entities.ConversationHistory;
using ProjectVG.Infrastructure.Repositories;
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

        public async Task<ConversationHistory> AddMessageAsync(string sessionId, ChatRole role, string content)
        {
            try
            {
                var message = new ConversationHistory
                {
                    SessionId = sessionId,
                    Role = role,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                var addedMessage = await _conversationRepository.AddAsync(message);
                _logger.LogDebug("대화 메시지 추가 완료: 세션 {SessionId}, 역할 {Role}", sessionId, role);
                
                return addedMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 메시지 추가 중 오류 발생: 세션 {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(string sessionId, int count = 10)
        {
            try
            {
                var history = await _conversationRepository.GetBySessionIdAsync(sessionId, count);
                _logger.LogDebug("대화 기록 조회 완료: 세션 {SessionId}, {Count}개 메시지", sessionId, history.Count());
                
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 조회 중 오류 발생: 세션 {SessionId}", sessionId);
                throw;
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            try
            {
                await _conversationRepository.ClearSessionAsync(sessionId);
                _logger.LogInformation("대화 기록 삭제 완료: 세션 {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 삭제 중 오류 발생: 세션 {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<int> GetMessageCountAsync(string sessionId)
        {
            try
            {
                var count = await _conversationRepository.GetMessageCountAsync(sessionId);
                _logger.LogDebug("메시지 수 조회 완료: 세션 {SessionId}, {Count}개", sessionId, count);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 수 조회 중 오류 발생: 세션 {SessionId}", sessionId);
                throw;
            }
        }
    }
} 