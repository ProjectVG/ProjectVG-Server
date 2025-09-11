using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Repositories;
using Microsoft.Extensions.Logging;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

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

        public async Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, string role, string content, DateTime timestamp, string? conversationId = null)
        {

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ValidationException(ErrorCode.MESSAGE_EMPTY, content);
            }

            if (content.Length > 10000) // 메시지 길이 제한 확장
            {
                throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, content.Length);
            }

            if (!ChatRole.IsValid(role))
            {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, $"Invalid role: {role}");
            }

            var message = new ConversationHistory
            {
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                Content = content,
                Timestamp = timestamp, // 사용자가 요청한 실제 시간 사용
                ConversationId = conversationId
            };

            var addedMessage = await _conversationRepository.AddAsync(message);
            
            return addedMessage;
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int page = 1, int pageSize = 10)
        {

            if (page <= 0)
            {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, $"Page must be greater than 0, but was: {page}");
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, $"PageSize must be between 1 and 100, but was: {pageSize}");
            }

            var history = await _conversationRepository.GetConversationHistoryAsync(userId, characterId, page, pageSize);
            
            return history;
        }

        public async Task DeleteConversationAsync(Guid userId, Guid characterId)
        {
            await _conversationRepository.DeleteConversationAsync(userId, characterId);
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var count = await _conversationRepository.GetMessageCountAsync(userId, characterId);
            return count;
        }

        public async Task<IEnumerable<ConversationHistory>> GetByConversationIdAsync(string conversationId, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, "ConversationId cannot be empty");
            }

            return await _conversationRepository.GetByConversationIdAsync(conversationId, userId);
        }

        public async Task DeleteMessageAsync(Guid messageId, Guid userId)
        {
            // TODO: 사용자 권한 확인 로직 추가
            // 메시지의 UserId가 현재 사용자와 일치하는지 확인
            
            await _conversationRepository.DeleteMessageAsync(messageId);
        }
    }
}
