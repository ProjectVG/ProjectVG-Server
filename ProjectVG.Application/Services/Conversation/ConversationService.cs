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

        /// <summary>
        /// ConversationService 인스턴스를 생성하고 필수 의존성(대화 저장소와 로거)을 주입합니다.
        /// </summary>
        public ConversationService(IConversationRepository conversationRepository, ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        /// <summary>
        /// 사용자의 대화에 새 메시지를 추가하고 저장된 ConversationHistory를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 입력된 내용은 비어있거나 공백일 수 없으며 최대 길이 1000자를 초과할 수 없습니다. 저장 시 CreatedAt은 UTC 시간으로 설정됩니다.
        /// </remarks>
        /// <param name="userId">메시지를 보낸 사용자 식별자.</param>
        /// <param name="characterId">대화에 연결된 캐릭터 식별자.</param>
        /// <param name="role">메시지의 발화자 역할(예: 사용자, 시스템 등).</param>
        /// <param name="content">메시지 본문. 비어있을 수 없고 최대 1000자.</param>
        /// <returns>저장된 ConversationHistory 엔티티.</returns>
        /// <exception cref="ValidationException">content가 비어있거나( ErrorCode.MESSAGE_EMPTY ), 길이가 1000자를 초과하는 경우( ErrorCode.MESSAGE_TOO_LONG ).</exception>
        public async Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, ChatRole role, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) {
                throw new ValidationException(ErrorCode.MESSAGE_EMPTY, content);
            }

            if (content.Length > 1000) {
                throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, content.Length);
            }

            var message = new ConversationHistory {
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            var addedMessage = await _conversationRepository.AddAsync(message);
            return addedMessage;
        }

        /// <summary>
        /// 지정된 사용자와 캐릭터에 대한 최근 대화 기록을 최대 <paramref name="count"/>개까지 가져옵니다.
        /// </summary>
        /// <param name="userId">대화의 소유자 사용자의 식별자.</param>
        /// <param name="characterId">대화에 연관된 캐릭터의 식별자.</param>
        /// <param name="count">가져올 항목 수(1~100). 기본값은 10입니다.</param>
        /// <returns>요청한 범위만큼의 ConversationHistory 열거형(최신 항목 우선).</returns>
        /// <exception cref="ValidationException">count가 1보다 작거나 100보다 큰 경우(ErrorCode.VALIDATION_FAILED).</exception>
        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int count = 10)
        {
            if (count <= 0 || count > 100) {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, count);
            }

            var history = await _conversationRepository.GetByUserIdAsync(userId, characterId, count);
            return history;
        }

        /// <summary>
        /// 지정한 사용자와 캐릭터에 대한 대화 기록(세션)을 삭제합니다.
        /// </summary>
        /// <param name="userId">대화 기록을 삭제할 사용자의 식별자.</param>
        /// <param name="characterId">대화 기록을 삭제할 캐릭터의 식별자.</param>
        public async Task ClearConversationAsync(Guid userId, Guid characterId)
        {
            await _conversationRepository.ClearSessionAsync(userId, characterId);
        }

        /// <summary>
        /// 지정된 사용자와 캐릭터 쌍에 대한 저장된 메시지의 총 개수를 비동기적으로 반환합니다.
        /// </summary>
        /// <param name="userId">대화 소유자(사용자)의 식별자.</param>
        /// <param name="characterId">대화 대상 캐릭터의 식별자.</param>
        /// <returns>해당 사용자·캐릭터 세션에 저장된 메시지 수를 나타내는 정수.</returns>
        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var count = await _conversationRepository.GetMessageCountAsync(userId, characterId);
            return count;
        }
    }
}
