using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISessionStorage _sessionStorage;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IServiceScopeFactory scopeFactory,
            ISessionStorage sessionStorage,
            IUserService userService,
            ICharacterService characterService,
            ILogger<ChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _sessionStorage = sessionStorage;
            _userService = userService;
            _characterService = characterService;
            _logger = logger;
        }

        public async Task<ChatValidationResult> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            var validationResult = await ValidateChatRequestAsync(command);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ChatOrchestrator>();
                
                try
                {
                    await orchestrator.ProcessChatRequestAsync(command);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", command.SessionId);
                }
            });

            return ChatValidationResult.Success();
        }

        private async Task<ChatValidationResult> ValidateChatRequestAsync(ProcessChatCommand command)
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

            return ChatValidationResult.Success();
        }
    }
} 