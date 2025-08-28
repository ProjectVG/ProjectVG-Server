using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.Users;
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
        /// 지정된 채팅 명령의 식별자들을 검증합니다.
        /// </summary>
        /// <remarks>
        /// - command.SessionId가 null 또는 빈 문자열이 아니면 세션 존재 여부를 확인합니다.
        /// - command.UserId와 command.CharacterId의 존재 여부를 확인합니다.
        /// 검증에 실패하면 적절한 예외를 던집니다.
        /// </remarks>
        /// <param name="command">검증할 채팅 명령; 사용되는 필드: <c>SessionId</c> (선택적), <c>UserId</c>, <c>CharacterId</c>.</param>
        /// <exception cref="ProjectVG.Common.Exceptions.ValidationException">제공된 세션 ID가 유효하지 않을 때 (ErrorCode.INVALID_SESSION_ID).</exception>
        /// <exception cref="ProjectVG.Common.Exceptions.NotFoundException">사용자 또는 캐릭터가 존재하지 않을 때 각각 (ErrorCode.USER_NOT_FOUND, ErrorCode.CHARACTER_NOT_FOUND).</exception>
        public async Task ValidateAsync(ProcessChatCommand command)
        {
            if (!string.IsNullOrEmpty(command.SessionId)) {
                var sessionExists = await _sessionStorage.ExistsAsync(command.SessionId);
                if (!sessionExists) {
                    _logger.LogWarning("세션 ID 검증 실패: {UserId}", command.SessionId);
                    throw new ValidationException(ErrorCode.INVALID_SESSION_ID, command.SessionId);
                }
            }

            var userExists = await _userService.ExistsByIdAsync(command.UserId);
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
