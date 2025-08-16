using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat
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
