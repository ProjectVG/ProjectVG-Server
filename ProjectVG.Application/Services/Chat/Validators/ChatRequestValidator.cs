using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Validators
{
    public class ChatRequestValidator
    {
        private readonly ISessionStorage _sessionStorage;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ILogger<ChatRequestValidator> _logger;

        /// <summary>
        /// ChatRequestValidator의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 필요한 의존성(ISessionStorage, IUserService, ICharacterService, ILogger&lt;ChatRequestValidator&gt;)을 주입받아 내부에서 사용합니다.
        /// </remarks>
        public ChatRequestValidator(
            ISessionStorage sessionStorage,
            IUserService userService,
            ICharacterService characterService,
            ILogger<ChatRequestValidator> logger)
        {
            _sessionStorage = sessionStorage;
            _userService = userService;
            _characterService = characterService;
            _logger = logger;
        }

        /// <summary>
        /// 주어진 채팅 처리 명령(ProcessChatCommand)의 참조 무결성을 검사합니다.
        /// </summary>
        /// <param name="command">검증할 채팅 처리 명령. 선택적 SessionId와 필수 UserId, CharacterId를 사용합니다.</param>
        /// <exception cref="ValidationException">command.SessionId가 비어있지 않으나 존재하지 않는 세션일 경우(ErrorCode.INVALID_SESSION_ID).</exception>
        /// <exception cref="NotFoundException">사용자 또는 캐릭터가 존재하지 않을 경우 각각 ErrorCode.USER_NOT_FOUND 또는 ErrorCode.CHARACTER_NOT_FOUND로 던져집니다.</exception>
        public async Task ValidateAsync(ProcessChatCommand command)
        {
            if (!string.IsNullOrEmpty(command.SessionId)) {
                var sessionExists = await _sessionStorage.ExistsAsync(command.SessionId);
                if (!sessionExists) {
                    _logger.LogWarning("세션 ID 검증 실패: {SessionId}", command.SessionId);
                    throw new ValidationException(ErrorCode.INVALID_SESSION_ID, command.SessionId);
                }
            }

            var userExists = await _userService.UserExistsAsync(command.UserId);
            if (!userExists) {
                _logger.LogWarning("사용자 ID 검증 실패: {UserId}", command.UserId);
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, command.UserId);
            }

            var characterExists = await _characterService.CharacterExistsAsync(command.CharacterId);
            if (!characterExists) {
                _logger.LogWarning("캐릭터 ID 검증 실패: {CharacterId}", command.CharacterId);
                throw new NotFoundException(ErrorCode.CHARACTER_NOT_FOUND, command.CharacterId);
            }
        }
    }
}
