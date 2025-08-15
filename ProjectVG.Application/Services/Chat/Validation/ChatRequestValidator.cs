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

        public async Task<ChatValidationResult> ValidateAsync(ProcessChatCommand command)
        {
            if (!string.IsNullOrEmpty(command.SessionId))
            {
                var sessionExists = await _sessionStorage.ExistsAsync(command.SessionId);
                if (!sessionExists)
                {
                    _logger.LogWarning("세션 ID 검증 실패: {SessionId}", command.SessionId);
                    return ChatValidationResult.Failure("유효하지 않은 세션 ID입니다.", "INVALID_SESSION_ID");
                }
            }

            var userExists = await _userService.UserExistsAsync(command.UserId);
            if (!userExists)
            {
                _logger.LogWarning("사용자 ID 검증 실패: {UserId}", command.UserId);
                return ChatValidationResult.Failure("존재하지 않는 사용자 ID입니다.", "INVALID_USER_ID");
            }

            var characterExists = await _characterService.CharacterExistsAsync(command.CharacterId);
            if (!characterExists)
            {
                _logger.LogWarning("캐릭터 ID 검증 실패: {CharacterId}", command.CharacterId);
                return ChatValidationResult.Failure("존재하지 않는 캐릭터 ID입니다.", "INVALID_CHARACTER_ID");
            }

            _logger.LogDebug("채팅 요청 검증 성공: 세션 {SessionId}, 사용자 {UserId}, 캐릭터 {CharacterId}", 
                command.SessionId, command.UserId, command.CharacterId);
            return ChatValidationResult.Success();
        }
    }
}
